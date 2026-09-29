namespace PaymentAPI.Models
{
    public class PaymentCreate
    {
        public decimal Amount { get; set; }
        public int AccountId { get; set; }
    }
}
