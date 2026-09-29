namespace PaymentProviderAPI.Models
{
    public class PaymentRequest
    {
        public decimal Amount { get; set; }
        public Guid IdempotencyKey { get; set; }
    }
}
