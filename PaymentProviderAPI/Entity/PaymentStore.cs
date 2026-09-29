using PaymentProviderAPI.Models;
using System.Collections.Concurrent;

namespace PaymentProviderAPI.Entity
{
    public enum PaymentStatus
    {
        PENDING,
        PROCESSING,
        COMPLETED,
        FAILED
    }

    public class PaymentEntity
    {
        public Guid IdempotencyKey { get; set; }
        public Guid PaymentId { get; set; }
        public PaymentStatus Status { get; set; } = PaymentStatus.PENDING;
        public DateTime CreatedAt { get; set; }
    }

    public class PaymentStore
    {
        private readonly ConcurrentDictionary<Guid, PaymentEntity> _payments = new();

        //fiqtiuri bazashi chawera
        public PaymentEntity AddPayment(PaymentRequest payment)
        {
            if (_payments.TryGetValue(payment.IdempotencyKey, out var existing))
            {
                RefreshStatus(existing);
                return existing;
            }
            
            var paymentEntity = new PaymentEntity
            {
                IdempotencyKey = payment.IdempotencyKey,
                PaymentId = Guid.NewGuid(),
                Status = payment.Amount > 100 ? PaymentStatus.FAILED : PaymentStatus.PENDING,
                CreatedAt = DateTime.UtcNow
            };

            if (!_payments.TryAdd(payment.IdempotencyKey, paymentEntity))
            {
                existing = _payments[payment.IdempotencyKey];
                RefreshStatus(existing);
                return existing;
            }

            return paymentEntity;
        }

        //fiqtiuri bazidan wakitxva
        public PaymentEntity? GetByPaymentId(Guid paymentId)
        {
            var payment = _payments.Values.FirstOrDefault(p => p.PaymentId == paymentId);
            if (payment == null)
                return null;

            RefreshStatus(payment);

            return payment;
        }

        private static void RefreshStatus(PaymentEntity payment)
        {
            if (payment.Status is PaymentStatus.COMPLETED or PaymentStatus.FAILED)
                return;

            var elapsed = (DateTime.UtcNow - payment.CreatedAt).TotalSeconds;
            if (elapsed < 5)
                payment.Status = PaymentStatus.PENDING;
            else if (elapsed < 12)
                payment.Status = PaymentStatus.PROCESSING;
            else
                payment.Status = PaymentStatus.COMPLETED;
        }
    }
}
