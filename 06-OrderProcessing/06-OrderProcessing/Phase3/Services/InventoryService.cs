using _06_OrderProcessing.Phase1.Async;
using _06_OrderProcessing.Shared.Models;

namespace _06_OrderProcessing.Phase3.Services;

public class InventoryService
{
    private readonly ProductRepository _productRepository;

    public InventoryService(ProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<string?> ReserveStockAsync(
        Order order,
        CancellationToken ct)
    {
        List<OrderItem> reservedItems = new();

        try
        {
            foreach (OrderItem item in order.Items)
            {
                ct.ThrowIfCancellationRequested();

                bool reserved = await _productRepository.ReserveStockAsync(
                    item.ProductId,
                    item.Quantity,
                    ct);

                if (!reserved)
                {
                    Product? product =
                        await _productRepository.GetByIdAsync(
                            item.ProductId,
                            CancellationToken.None);

                    foreach (OrderItem reservedItem in reservedItems)
                    {
                        await _productRepository.ReleaseStockAsync(
                            reservedItem.ProductId,
                            reservedItem.Quantity,
                            CancellationToken.None);
                    }

                    return product is null
                        ? $"Product #{item.ProductId} not found"
                        : $"Insufficient stock for '{product.Name}'";
                }

                reservedItems.Add(item);
            }

            return null;
        }
        catch (OperationCanceledException)
        {
            foreach (OrderItem reservedItem in reservedItems)
            {
                await _productRepository.ReleaseStockAsync(
                    reservedItem.ProductId,
                    reservedItem.Quantity,
                    CancellationToken.None);
            }

            throw;
        }
    }

    public async Task ReleaseStockAsync(
        Order order,
        CancellationToken ct)
    {
        foreach (OrderItem item in order.Items)
        {
            await _productRepository.ReleaseStockAsync(
                item.ProductId,
                item.Quantity,
                ct);
        }
    }
}
