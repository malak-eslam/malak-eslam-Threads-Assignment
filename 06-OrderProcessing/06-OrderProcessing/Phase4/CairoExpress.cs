using _06_OrderProcessing.Shared.Exceptions;
using _06_OrderProcessing.Shared.Models;

namespace _06_OrderProcessing.Phase4;

public class CairoExpress : IShippingCarrier
{
    private readonly HashSet<int> _failForOrderIds;

    public string Name => "CairoExpress";

    public CairoExpress(IEnumerable<int>? failForOrderIds = null)
    {
        _failForOrderIds = new HashSet<int>(
            failForOrderIds ?? Enumerable.Empty<int>());
    }

    public async Task<decimal> GetQuoteAsync(
        Order order,
        CancellationToken ct)
    {
        await Task.Delay(150, ct);

        if (order.Id % 2 == 0 ||
            _failForOrderIds.Contains(order.Id))
        {
            throw new ShippingProviderUnavailableException(
                "CairoExpress unavailable");
        }

        await Task.Delay(Random.Shared.Next(300, 2001), ct);

        return await ShippingCostCalculator.CalculateAsync(
            order, 0.95m, ct);
    }
}
