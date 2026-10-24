using System.Diagnostics;
using _06_OrderProcessing.Phase1.Async;
using _06_OrderProcessing.Phase2;
using _06_OrderProcessing.Phase3.Services;
using _06_OrderProcessing.Shared.AppLogger;
using _06_OrderProcessing.Shared.Models;
using _06_OrderProcessing.Shared.Storage;

namespace _06_OrderProcessing.Phase4;

public class LiveCheckoutRunner
{
    public async Task RunAsync(CancellationToken ct)
    {
        var store = new JsonFileStore();
        var productRepository = new ProductRepository(store);
        var customerRepository = new CustomerRepository(store);
        var couponRepository = new CouponRepository(store);

        var orderRepository = new OrderRepository(
            store,
            failSaveForOrderIds: new[] { 13 });

        var customerService = new CustomerService(customerRepository);
        var inventoryService = new InventoryService(productRepository);

        var pricingService = new PricingService(
            productRepository,
            customerRepository,
            couponRepository,
            new HashSet<int>());

        var paymentGateway = new PaymentGateway(
            store,
            timeoutOrderIds: new[] { 5 },
            declineOrderIds: new[] { 8 });

        var emailService = new EmailService(
            store,
            new Dictionary<int, int>
            {
                [3] = 2,
                [17] = int.MaxValue
            });

        var processor = new OrderProcessor(
            customerService,
            inventoryService,
            pricingService,
            paymentGateway,
            orderRepository,
            emailService);

        List<Order> orders = await store.ReadAsync<Order>(
            DataFiles.IncomingOrders, ct);

        var stopwatch = Stopwatch.StartNew();

        var tasks = orders.Select(order =>
            ProcessAsync(processor, order, ct)).ToList();

        int completed = 0;

        await foreach (var task in Task.WhenEach(tasks))
        {
            var result = await task;
            completed++;

            Console.WriteLine(
                $"[{completed,2}/{orders.Count}] Order #{result.Id} " +
                $"-> {result.Status} " +
                $"({result.FailureReason ?? "-"}) " +
                $"after {stopwatch.Elapsed.TotalSeconds:F1} s");
        }

        stopwatch.Stop();

        List<Order> processedOrders =
            await orderRepository.GetAllAsync(ct);

        Console.WriteLine("\n=== Order Status Summary ===");

        foreach (OrderStatus status in Enum.GetValues<OrderStatus>())
        {
            int count = processedOrders.Count(o => o.Status == status);
            Console.WriteLine($"{status}: {count}");
        }
    }

    private static async Task<Order> ProcessAsync(
        OrderProcessor processor,
        Order order,
        CancellationToken ct)
    {
        try
        {
            await processor.ProcessOrderAsync(order, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            Log.Error("LiveCheckout", $"Order #{order.Id} was cancelled.");
        }
        catch (Exception ex)
        {
            Log.Error(
                "LiveCheckout",
                $"Order #{order.Id} failed: {ex.Message}");
        }

        return order;
    }
}
