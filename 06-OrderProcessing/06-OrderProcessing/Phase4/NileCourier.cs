using _06_OrderProcessing.Shared.Models;

namespace _06_OrderProcessing.Phase4;

public class NileCourier : IShippingCarrier
{
    public string Name => "NileCourier";

    public async Task<decimal> GetQuoteAsync(
        Order order,
        CancellationToken ct)
    {
        await Task.Delay(150, ct);
        await Task.Delay(Random.Shared.Next(800, 1201), ct);

        return await ShippingCostCalculator.CalculateAsync(
            order, 1.10m, ct);
    }
}

