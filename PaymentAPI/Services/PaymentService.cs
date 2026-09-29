using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using PaymentAPI.Models;

namespace PaymentAPI.Services
{
    public class PaymentService
    {
        private const int MaxRetries = 3;
        private const int MaxRequestsPerSecond = 5;
        private static readonly int[] PollDelaysSeconds = [2, 4, 8, 10, 10];

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly JsonSerializerOptions _jsonOptions;

        private readonly ConcurrentDictionary<Guid, PaymentRecord> _payments = new();
        private readonly ConcurrentDictionary<Guid, SemaphoreSlim> _createLocks = new();
        private readonly ConcurrentDictionary<Guid, byte> _polling = new();

        private readonly SemaphoreSlim _rateLimit = new(MaxRequestsPerSecond, MaxRequestsPerSecond);

        private int _retryCount;
        private int _errorCount;
        private int _successCount;

        public PaymentService(IHttpClientFactory httpClientFactory, IOptions<JsonOptions> jsonOptions)
        {
            _httpClientFactory = httpClientFactory;
            _jsonOptions = jsonOptions.Value.JsonSerializerOptions;
        }

        public async Task<PaymentRecord> CreatePaymentAsync(PaymentCreate request)
        {
            //duplikati idempotency key elodeba
            var gate = _createLocks.GetOrAdd(request.IdempotencyKey, _ => new SemaphoreSlim(1, 1));
            await gate.WaitAsync();
            try
            {
                if (_payments.TryGetValue(request.IdempotencyKey, out var existing))
                {
                    StartPolling(existing);
                    return existing;
                }

                ProviderPaymentResponse created;
                try
                {
                    created = await SendAsync<ProviderPaymentResponse>(
                        () => _httpClientFactory.CreateClient("PaymentProvider").PostAsJsonAsync("Payment", new
                        {
                            request.Amount,
                            request.IdempotencyKey
                        }));
                }
                catch (Exception)
                {
                    Console.WriteLine($"Create failed for {request.IdempotencyKey}. Retries: {_retryCount}, Errors: {_errorCount}, Success: {_successCount}");
                    throw;
                }

                var payment = new PaymentRecord
                {
                    PaymentId = created.PaymentId,
                    IdempotencyKey = request.IdempotencyKey,
                    Amount = request.Amount,
                    Status = created.Status
                };

                _payments[request.IdempotencyKey] = payment;
                StartPolling(payment);
                return payment;
            }
            finally
            {
                gate.Release();
            }
        }

        public PaymentRecord? GetPayment(Guid idempotencyKey)
        {
            _payments.TryGetValue(idempotencyKey, out var payment);
            return payment;
        }

        private void StartPolling(PaymentRecord payment)
        {
            if (IsFinalStatus(payment.Status))
                return;

            if (!_polling.TryAdd(payment.PaymentId, 0))
                return;

            _ = PollAsync(payment);
        }

        private async Task PollAsync(PaymentRecord payment)
        {
            try
            {
                foreach (var seconds in PollDelaysSeconds)
                {
                    await Task.Delay(TimeSpan.FromSeconds(seconds));

                    try
                    {
                        var response = await SendAsync<ProviderPaymentResponse>(
                            () => _httpClientFactory.CreateClient("PaymentProvider").GetAsync($"Payment/{payment.PaymentId}"));

                        payment.Status = response.Status;

                        Console.WriteLine($"Payment {payment.PaymentId}: {payment.Status}. Retries: {_retryCount}, Errors: {_errorCount}, Success: {_successCount}");

                        if (IsFinalStatus(payment.Status))
                            return;
                    }
                    catch (Exception)
                    {
                        Console.WriteLine($"Polling failed for {payment.PaymentId}. Retries: {_retryCount}, Errors: {_errorCount}, Success: {_successCount}");
                    }
                }

                if (!IsFinalStatus(payment.Status))
                    payment.Status = PaymentStatus.TIMEOUT;
            }
            finally
            {
                _polling.TryRemove(payment.PaymentId, out _);
                Console.WriteLine($"Payment {payment.PaymentId}: {payment.Status}. Retries: {_retryCount}, Errors: {_errorCount}, Success: {_successCount}");
            }
        }


        private async Task<T> SendAsync<T>(Func<Task<HttpResponseMessage>> send)
        {
            Exception? lastError = null;

            for (var attempt = 0; attempt <= MaxRetries; attempt++)
            {
                if (attempt > 0)
                {
                    Interlocked.Increment(ref _retryCount);
                    await Task.Delay(TimeSpan.FromMilliseconds(300 * attempt));
                }

                try
                {
                    await WaitRateLimitAsync();

                    using var response = await send();
                    var body = await response.Content.ReadAsStringAsync();
                    var code = (int)response.StatusCode;

                    if (response.IsSuccessStatusCode)
                    {
                        var parsed = JsonSerializer.Deserialize<T>(body, _jsonOptions);
                        if (parsed == null)
                        {
                            Interlocked.Increment(ref _errorCount);
                            throw new InvalidOperationException("Empty provider response.");
                        }

                        Interlocked.Increment(ref _successCount);
                        return parsed;
                    }

                    lastError = new InvalidOperationException($"Provider error {code}: {body}");
                   
                }
                catch (Exception ex) when (ex is not InvalidOperationException)
                {
                    lastError = ex;
                }

                if (attempt == MaxRetries)
                    break;
            }

            Interlocked.Increment(ref _errorCount);
            throw lastError ?? new InvalidOperationException("Provider request failed.");
        }

       
        private async Task WaitRateLimitAsync()
        {
            await _rateLimit.WaitAsync();
            _ = Task.Delay(TimeSpan.FromSeconds(1)).ContinueWith(_ => _rateLimit.Release());
        }

        private static bool IsFinalStatus(PaymentStatus status)
        {
            return status is PaymentStatus.COMPLETED or PaymentStatus.FAILED or PaymentStatus.TIMEOUT;
        }
    }
}
