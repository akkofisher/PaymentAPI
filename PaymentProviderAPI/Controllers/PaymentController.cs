using Microsoft.AspNetCore.Mvc;
using PaymentProviderAPI.Entity.PaymentProviderAPI.Services;
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
            if (request == null || request.Amount <= 0 || request.AccountId <= 0)
            {
                return BadRequest(new { Message = "Invalid payment request." });
            }

            //check for idempotency

            var existingPayment = _paymentStore.GetTransactionByIdempotencyKey(request.IdempotencyKey);
            if (existingPayment)
            {
                return BadRequest(new { Message = "Payment already processed." });
            }

            var transactionId = Guid.NewGuid();
            _paymentStore.AddPayment(request.IdempotencyKey, true, transactionId);

            return Ok(new { status = "success", TransactionId = transactionId });

            ////if success
            //if (IsSuccess)
            //{
            //    return Ok(new { Message = "Payment processed successfully.", TransactionId = paymentResult.TransactionId });
            //}
            //else
            //{
            //    return StatusCode(500, new { Message = "Payment processing failed.", Error = paymentResult.ErrorMessage });
            //}
        }

        //get payment by transactionid
        [HttpGet("{transactionId}")]
        public IActionResult GetPaymentStatus(Guid transactionId)
        {
            var success = _paymentStore.GetTransactionByTransactionId(transactionId);
            if (success)
            {
                return Ok(new { Message = "Payment found.", TransactionId = transactionId });
            }
            else
            {
                return NotFound(new { Message = "Payment not found.", TransactionId = transactionId });
            }
        }

    }
}
