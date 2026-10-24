using _06_OrderProcessing.Phase1.Async;
using _06_OrderProcessing.Shared.Models;

namespace _06_OrderProcessing.Phase2;

public class OrderProcessorV2
{
    private readonly ProductRepository _productRepository;
    private readonly OrderPricer _orderPricer;
    private readonly InvoiceGenerator _invoiceGenerator;
    private readonly OrderRepository _orderRepository;

    public OrderProcessorV2(
        ProductRepository productRepository,
        OrderPricer orderPricer,
        InvoiceGenerator invoiceGenerator,
        OrderRepository orderRepository)
    {
        _productRepository = productRepository;
        _orderPricer = orderPricer;
        _invoiceGenerator = invoiceGenerator;
        _orderRepository = orderRepository;
    }

    public async Task<bool> ProcessValidatedOrderAsync(
        Order order,
        List<OrderItem> reservedItems,
        CancellationToken ct)
    {
        foreach (OrderItem item in order.Items)
        {
            bool reserved = await _productRepository.ReserveStockAsync(
                item.ProductId,
                item.Quantity,
                ct);

            if (!reserved)
            {
                while (reservedItems.Count > 0)
                {
                    OrderItem reservedItem = reservedItems[0];

                    await _productRepository.ReleaseStockAsync(
                        reservedItem.ProductId,
                        reservedItem.Quantity,
                        ct);

                    reservedItems.RemoveAt(0);
                }

                Product? product = await _productRepository.GetByIdAsync(
                    item.ProductId,
                    ct);

                string productName = product?.Name
                    ?? $"Product #{item.ProductId}";

                order.Status = OrderStatus.Rejected;
                order.FailureReason =
                    $"Insufficient stock for '{productName}'";

                await _orderRepository.SaveAsync(order, ct);

                return false;
            }

            reservedItems.Add(item);
        }

        await _orderPricer.PriceOrderAsync(order, ct);

        await _invoiceGenerator.GenerateAsync(order);

        order.Status = OrderStatus.Completed;
        order.FailureReason = null;

        await _orderRepository.SaveAsync(order, ct);

        return true;
    }
}
