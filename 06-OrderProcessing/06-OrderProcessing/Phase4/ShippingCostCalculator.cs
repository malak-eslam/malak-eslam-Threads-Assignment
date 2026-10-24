
using _06_OrderProcessing.Shared.Models;
using _06_OrderProcessing.Shared.Storage;

namespace _06_OrderProcessing.Phase4;

public static class ShippingCostCalculator
{
    public static async Task<decimal> CalculateAsync(
        Order order,
        decimal carrierFactor,
        CancellationToken ct)
    {
        if (order.SubTotal >= 1000m)
            return 0m;

        var store = new JsonFileStore();

        List<Product> products =
            await store.ReadAsync<Product>(DataFiles.Products, ct);

        int quantity = 0;
        decimal weight = 0m;

        foreach (OrderItem item in order.Items)
        {
            ct.ThrowIfCancellationRequested();

            Product? product = products.Find(
                p => p.Id == item.ProductId);

            if (product is null)
                throw new InvalidOperationException(
                    $"Product #{item.ProductId} not found.");

            quantity += item.Quantity;
            weight += product.WeightKg * item.Quantity;
        }

        decimal baseCost = 5m + 1.50m * quantity + 2m * weight;

        return Math.Round(
            baseCost * carrierFactor,
            2,
            MidpointRounding.AwayFromZero);
    }
}
