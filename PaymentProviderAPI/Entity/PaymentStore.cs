namespace PaymentProviderAPI.Entity
{
    namespace PaymentProviderAPI.Services
    {
        public class PaymentEntity
        {
            public Guid IdempotencyKey { get; set; }
            public bool Success { get; set; }
            public Guid TransactionId { get; set; }
        }

        public class PaymentStore
        {
            private readonly List<PaymentEntity> _payments = new();

            public bool GetTransactionByTransactionId(Guid transactionId)
            {
                return _payments.FirstOrDefault(p => p.TransactionId == transactionId)?.Success ?? false;
            }

            public bool GetTransactionByIdempotencyKey(Guid idempotencyKey)
            {
                return _payments.FirstOrDefault(p => p.IdempotencyKey == idempotencyKey)?.Success ?? false;
            }

            public void AddPayment(Guid idempotencyKey, bool success, Guid transactionId)
            {
                _payments.Add(new PaymentEntity
                {
                    IdempotencyKey = idempotencyKey,
                    Success = success,
                    TransactionId = transactionId
                });
            }
        }
    }
}
