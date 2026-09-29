namespace PaymentAPI.Models
{
    public enum PaymentStatus
    {
        PENDING,
        PROCESSING,
        COMPLETED,
        FAILED,
        TIMEOUT
    }

    public class PaymentCreate
    {
        public decimal Amount { get; set; }
        public Guid IdempotencyKey { get; set; }
    }

    public class PaymentRecord
    {
        public Guid PaymentId { get; set; }
        public Guid IdempotencyKey { get; set; }
        public decimal Amount { get; set; }
        public PaymentStatus Status { get; set; } = PaymentStatus.PENDING;
    }

    public class ProviderPaymentResponse
    {
        public Guid PaymentId { get; set; }
        public PaymentStatus Status { get; set; }
    }
}
