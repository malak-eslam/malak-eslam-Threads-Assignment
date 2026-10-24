using _06_OrderProcessing.Phase3.Interfaces;
using _06_OrderProcessing.Shared.Models;
using _06_OrderProcessing.Shared.Storage;

namespace _06_OrderProcessing.Phase3.Services;

public class PaymentGateway : IPaymentGateway
{
    private readonly JsonFileStore _store;
    private readonly HashSet<int> _timeoutOrderIds;
    private readonly HashSet<int> _declineOrderIds;

    public PaymentGateway(
        JsonFileStore store,
        IEnumerable<int>? timeoutOrderIds = null,
        IEnumerable<int>? declineOrderIds = null)
    {
        _store = store;
        _timeoutOrderIds = new HashSet<int>(
            timeoutOrderIds ?? Enumerable.Empty<int>());
        _declineOrderIds = new HashSet<int>(
            declineOrderIds ?? Enumerable.Empty<int>());
    }

    public async Task<PaymentResult> ProcessPaymentAsync(
        int orderId,
        decimal amount,
        CancellationToken ct)
    {
        int delay = _timeoutOrderIds.Contains(orderId) ? 5000 : 700;

        await Task.Delay(delay, ct);

        PaymentTransaction transaction = new()
        {
            OrderId = orderId,
            Amount = amount,
            CreatedAt = DateTime.Now,
            TransactionId = "TXN-" +
                Guid.NewGuid().ToString("N")[..12].ToUpper(),
            Status = _declineOrderIds.Contains(orderId)
                ? PaymentStatus.Declined
                : PaymentStatus.Captured
        };

        await _store.UpdateAsync<PaymentTransaction>(
            DataFiles.Payments,
            payments => payments.Add(transaction),
            ct);

        return new PaymentResult
        {
            Success = transaction.Status == PaymentStatus.Captured,
            TransactionId = transaction.TransactionId,
            Status = transaction.Status,
            Message = transaction.Status == PaymentStatus.Declined
                ? "Card declined by issuer"
                : "Payment captured successfully"
        };
    }

    public async Task RefundAsync(string transactionId)
    {
        await _store.UpdateAsync<PaymentTransaction>(
            DataFiles.Payments,
            payments =>
            {
                PaymentTransaction? transaction = null;

                foreach (PaymentTransaction payment in payments)
                {
                    if (payment.TransactionId == transactionId)
                    {
                        transaction = payment;
                        break;
                    }
                }

                if (transaction is null)
                {
                    throw new InvalidOperationException(
                        $"Transaction '{transactionId}' not found.");
                }

                if (transaction.Status != PaymentStatus.Captured)
                {
                    throw new InvalidOperationException(
                        $"Transaction '{transactionId}' cannot be refunded " +
                        $"because its status is {transaction.Status}.");
                }

                transaction.Status = PaymentStatus.Refunded;
            },
            CancellationToken.None);
    }
}