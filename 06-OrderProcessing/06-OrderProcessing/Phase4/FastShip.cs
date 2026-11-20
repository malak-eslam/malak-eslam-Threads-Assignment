using System.Diagnostics;
using _06_OrderProcessing.Shared.Models;

namespace _06_OrderProcessing.Phase4;

public class FastShip : IShippingCarrier
{
    public string Name => "FastShip";

    public async Task<decimal> GetQuoteAsync(
        Order order,
        CancellationToken ct)
    {
        await Task.Delay(150, ct);
        await Task.Delay(Random.Shared.Next(500, 1501), ct);

        return await ShippingCostCalculator.CalculateAsync(
            order, 1.00m, ct);
    }
}
