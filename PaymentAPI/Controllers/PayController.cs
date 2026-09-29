using Microsoft.AspNetCore.Mvc;
using PaymentAPI.Models;

namespace PaymentAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class PayController : Controller
    {
        [HttpPost]
        public IActionResult CreatePayment([FromBody] PaymentCreate request)
        {
            //validate request
            if (request == null || request.Amount <= 0 || request.AccountId <= 0)
            {
                return BadRequest(new { Message = "Invalid payment request." });
            }

            var idempotencyStore = new Dictionary<Guid, Guid>();

            using (var httpClient = new HttpClient())
            {
                var paymentProviderApiUrl = "https://localhost:7081/Payment";
                var paymentRequest = new
                {
                    Amount = request.Amount,
                    AccountId = request.AccountId,
                    IdempotencyKey = Guid.NewGuid()
                };

                var response = httpClient.PostAsJsonAsync(paymentProviderApiUrl, paymentRequest).Result;
                if (!response.IsSuccessStatusCode)
                {
                    return StatusCode((int)response.StatusCode, new { Message = "Payment processing failed." });
                }
            }


            return Ok(new { Message = "Payment Create successfully.", TransactionId = Guid.NewGuid() });
        }
    }
}
