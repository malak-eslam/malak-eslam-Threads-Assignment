using _06_OrderProcessing.Phase1.Async;
using _06_OrderProcessing.Phase2;
using _06_OrderProcessing.Phase3.Services;
using _06_OrderProcessing.Shared.AppLogger;
using _06_OrderProcessing.Shared.Models;
using _06_OrderProcessing.Shared.Storage;

namespace _06_OrderProcessing.Phase3;

public class Phase3Runner
{
    public async Task RunAsync(CancellationToken ct)
    {
        await DataSeeder.ResetAsync(ct);

        Log.Info("Phase3Runner", "Data reset completed.");

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

        var orderProcessor = new OrderProcessor(
            customerService,
            inventoryService,
            pricingService,
            paymentGateway,
            orderRepository,
            emailService);

        List<Order> incomingOrders = await store.ReadAsync<Order>(
            DataFiles.IncomingOrders, ct);

        Log.Info(
            "Phase3Runner",
            $"Loaded {incomingOrders.Count} incoming orders.");

        List<Task<bool>> tasks = new();
        bool cancelled = false;

        try
        {
            foreach (Order order in incomingOrders)
            {
                ct.ThrowIfCancellationRequested();

                tasks.Add(
                    orderProcessor.ProcessOrderAsync(order, ct));
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            cancelled = true;

            Log.Error(
                "Phase3Runner",
                "Cancellation requested. No more orders will be started.");
        }

        foreach (Task<bool> task in tasks)
        {
            try
            {
                await task;
            }
            catch (OperationCanceledException)
                when (ct.IsCancellationRequested)
            {
                cancelled = true;
            }
            catch (Exception ex)
            {
                Log.Error(
                    "Phase3Runner",
                    $"Unexpected error: {ex.Message}");
            }
        }

        cancelled |= ct.IsCancellationRequested;

        List<Order> processedOrders =
            await orderRepository.GetAllAsync(CancellationToken.None);

        int finishedCount = processedOrders.Count(
            order => order.Status != OrderStatus.Pending);

        int cancelledCount = processedOrders.Count(
            order => order.Status == OrderStatus.Pending &&
                     order.FailureReason == "Cancelled");

        Console.WriteLine();
        Console.WriteLine(
            $"{"Order",-8} {"Status",-15} {"Total",-12} Reason");
        Console.WriteLine(new string('-', 85));

        foreach (Order order in processedOrders.OrderBy(o => o.Id))
        {
            Console.WriteLine(
                $"#{order.Id,-7} {order.Status,-15} " +
                $"{order.TotalAmount,-12:F2} " +
                $"{order.FailureReason ?? "-"}");
        }

        Console.WriteLine();

        if (cancelled)
        {
            Console.WriteLine(
                $"Run cancelled — {finishedCount} orders finished, " +
                $"{cancelledCount} cancelled");
        }
        else
        {
            Log.Success("Phase3Runner", "Phase 3 finished.");
        }
    }
}
