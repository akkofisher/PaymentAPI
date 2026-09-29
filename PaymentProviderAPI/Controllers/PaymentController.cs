using Microsoft.AspNetCore.Mvc;
using PaymentProviderAPI.Entity;
using PaymentProviderAPI.Models;

namespace PaymentProviderAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class PaymentController : ControllerBase
    {
        private readonly PaymentStore _paymentStore;

        public PaymentController(PaymentStore paymentStore)
        {
            _paymentStore = paymentStore;
        }

        [HttpPost]
        public IActionResult ProcessPayment([FromBody] PaymentRequest request)
        {
            //validate request
            if (request == null || request.Amount <= 0 || request.IdempotencyKey == Guid.Empty)
            {
                return BadRequest(new { Message = "Invalid payment request." });
            }

            // same idempotency key returns the already created payment
            var payment = _paymentStore.AddPayment(request);

            Console.WriteLine($"PaymentId: {payment.PaymentId}, Status: {payment.Status}");

            return Ok(new
            {
                PaymentId = payment.PaymentId,
                Status = payment.Status
            });
            }

        [HttpGet("{paymentId:guid}")]
        public IActionResult GetPaymentStatus(Guid paymentId)
        {
            var payment = _paymentStore.GetByPaymentId(paymentId);
               if (payment == null)
               {
                return NotFound(new { Message = "Payment not found.", PaymentId = paymentId });
            }

               Console.WriteLine($"PaymentId: {payment.PaymentId}, Status: {payment.Status}");

            return Ok(new
            {
                PaymentId = payment.PaymentId,
                Status = payment.Status
            });
        }
    }
}
