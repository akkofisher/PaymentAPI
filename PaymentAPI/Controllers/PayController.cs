using Microsoft.AspNetCore.Mvc;
using PaymentAPI.Models;
using PaymentAPI.Services;

namespace PaymentAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class PayController : ControllerBase
    {
           private readonly PaymentService _paymentService;

        public PayController(PaymentService paymentService)
        {
              _paymentService = paymentService;
        }

        [HttpPost]
        public async Task<IActionResult> CreatePayment([FromBody] PaymentCreate request)
        {
            //validate request
            if (request == null || request.Amount <= 0)
            {
                return BadRequest(new { Message = "Invalid payment request." });
             }

               if (request.IdempotencyKey == Guid.Empty)
            {
                 return BadRequest(new { Message = "IdempotencyKey is Required." });
            }
    
            try
            {
                   var payment = await _paymentService.CreatePaymentAsync(request);
                return Ok(new
                {
                    Message = "Payment created Successfully.",
                    payment.PaymentId,
                    payment.Status,
                    payment.IdempotencyKey
                   });
            }
            catch (Exception ex)
            {
                return StatusCode(502, new { Message = "Payment Processing failed.", Error = ex.Message });
            }
        }

        [HttpGet("{idempotencyKey:guid}")]
          public IActionResult GetPayment(Guid idempotencyKey)
        {
            var payment = _paymentService.GetPayment(idempotencyKey);
            if (payment == null)
             {
                return NotFound(new { Message = "Payment not Found." });
            }

            return Ok(payment);
        }
    }
}
