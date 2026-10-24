using _06_OrderProcessing.Shared.Models;

namespace _06_OrderProcessing.Phase4;

public interface IShippingCarrier
{
    string Name { get; }

    Task<decimal> GetQuoteAsync(Order order,CancellationToken ct);
}
