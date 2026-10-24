using System.Diagnostics;
using _06_OrderProcessing.Phase1.Async;
using _06_OrderProcessing.Shared.AppLogger;
using _06_OrderProcessing.Shared.Models;
using _06_OrderProcessing.Shared.Storage;

namespace _06_OrderProcessing.Phase2;

public class Phase2Runner
{
    public async Task RunAsync(CancellationToken ct)
    {
        Stopwatch totalTimer = Stopwatch.StartNew();

        var store = new JsonFileStore();

        var productRepository = new ProductRepository(store);
        var customerRepository = new CustomerRepository(store);
        var couponRepository = new CouponRepository(store);
        var orderRepository = new OrderRepository(store);

        await DataSeeder.ResetAsync(ct);

        var raceDemo = new RaceConditionDemo(store, productRepository);
        await raceDemo.RunAsync(ct);

        await DataSeeder.ResetAsync(ct);

        var pricingService = new PricingService(
            productRepository,
            customerRepository,
            couponRepository,
            new HashSet<int> { 7 });

        var orderPricer = new OrderPricer(pricingService);
        var invoiceGenerator = new InvoiceGenerator();

        var validator = new OrderValidator(
            customerRepository,
            productRepository,
            couponRepository,
            orderRepository);

        var processor = new OrderProcessorV2(
            productRepository,
            orderPricer,
            invoiceGenerator,
            orderRepository);

        List<Order> incomingOrders =
            await store.ReadAsync<Order>(DataFiles.IncomingOrders, ct);

        Log.Info(
            "Phase2Runner",
            $"Loaded {incomingOrders.Count} incoming orders.");


        List<Task> validationTasks = new();

        foreach (Order order in incomingOrders)
        {
            validationTasks.Add(
                validator.ValidateOrderAsync(order, ct));
        }

        foreach (Task task in validationTasks)
        {
            await task;
        }

        List<Order> validatedOrders =
            await orderRepository.GetByStatusAsync(
                OrderStatus.Validated, ct);


        Order? sampleOrder = null;

        foreach (Order order in validatedOrders)
        {
            if (order.Id == 1)
            {
                sampleOrder = order;
                break;
            }
        }

        if (sampleOrder is not null)
        {
            Order parallelOrder = CopyOrder(sampleOrder);
            Order sequentialOrder = CopyOrder(sampleOrder);

            Console.WriteLine("\n=== Pricing Performance Comparison ===");

            Stopwatch pricingTimer = Stopwatch.StartNew();

            await orderPricer.PriceOrderAsync(parallelOrder, ct);

            pricingTimer.Stop();

            Console.WriteLine(
                $"Concurrent pricing: {pricingTimer.ElapsedMilliseconds} ms");

            pricingTimer.Restart();

            await orderPricer.PriceOrderSequentialAsync(
                sequentialOrder, ct);

            pricingTimer.Stop();

            Console.WriteLine(
                $"Sequential pricing: {pricingTimer.ElapsedMilliseconds} ms");
        }


        Console.WriteLine("\n=== Starting Tasks vs Awaiting Sequentially ===");

        Stopwatch taskTimer = Stopwatch.StartNew();

        Task firstTask = Task.Delay(1000, ct);
        Task secondTask = Task.Delay(1000, ct);

        await firstTask;
        await secondTask;

        taskTimer.Stop();

        Console.WriteLine(
            $"Start both, then await: {taskTimer.ElapsedMilliseconds} ms");

        taskTimer.Restart();

        await Task.Delay(1000, ct);
        await Task.Delay(1000, ct);

        taskTimer.Stop();

        Console.WriteLine(
            $"Await sequentially: {taskTimer.ElapsedMilliseconds} ms");

        validatedOrders =
            await orderRepository.GetByStatusAsync(
                OrderStatus.Validated, ct);

        List<Task<bool>> processingTasks = new();
        List<List<OrderItem>> reservations = new();

        foreach (Order order in validatedOrders)
        {
            List<OrderItem> reservedItems = new();
            reservations.Add(reservedItems);

            processingTasks.Add(
                processor.ProcessValidatedOrderAsync(
                    order,
                    reservedItems,
                    ct));
        }

        if (processingTasks.Count > 0)
        {
            Console.WriteLine(
                $"\nFirst task status immediately: {processingTasks[0].Status}");
        }

        await Task.Delay(1000, ct);

        Console.WriteLine("\nTask statuses after 1 second:");

        foreach (Task<bool> task in processingTasks)
        {
            Console.WriteLine(task.Status);
        }

        for (int i = 0; i < processingTasks.Count; i++)
        {
            Task<bool> task = processingTasks[i];
            Order order = validatedOrders[i];

            try
            {
                await task;
            }
            catch (Exception ex)
            {
                Log.Error(
                    "Phase2Runner",
                    $"Order #{order.Id} failed: {ex.Message}");

                Log.Error(
                    "Phase2Runner",
                    $"Exception details: {ex}");


                bool releaseSucceeded = true;

                foreach (OrderItem item in reservations[i])
                {
                    try
                    {
                        await productRepository.ReleaseStockAsync(
                            item.ProductId,
                            item.Quantity,
                            CancellationToken.None);
                    }
                    catch (Exception releaseException)
                    {
                        releaseSucceeded = false;

                        Log.Error(
                            "Phase2Runner",
                            $"Could not release stock for product " +
                            $"#{item.ProductId}: {releaseException.Message}");
                    }
                }

                reservations[i].Clear();

                order.Status = OrderStatus.Failed;
                order.FailureReason = releaseSucceeded
                    ? ex.Message
                    : $"{ex.Message}; Stock rollback requires investigation.";

                await orderRepository.SaveAsync(
                    order,
                    CancellationToken.None);
            }
        }

        totalTimer.Stop();

        Console.WriteLine("\nFinal task statuses:");

        foreach (Task<bool> task in processingTasks)
        {
            Console.WriteLine(task.Status);
        }

        Console.WriteLine(
            $"Total processing time: {totalTimer.ElapsedMilliseconds} ms");

        // Display all saved orders.
        List<Order> allOrders =
            await orderRepository.GetAllAsync(ct);

        Console.WriteLine($"\nOrders saved: {allOrders.Count}");

        foreach (Order order in allOrders)
        {
            Console.WriteLine(
                $"Order #{order.Id} | {order.Status} | " +
                $"Total: {order.TotalAmount:F2} | " +
                $"Reason: {order.FailureReason ?? "None"}");
        }

        // Generate the final report.
        Console.WriteLine("\n=== Report Generation ===");

        var reportGenerator = new ReportGenerator(
            orderRepository,
            productRepository);

        Task<Report> reportTask = reportGenerator.GenerateAsync(ct);

        Console.WriteLine("Generating report...");

        Report report = await reportTask;

        Console.WriteLine($"Total orders: {report.TotalOrders}");
        Console.WriteLine($"Completed: {report.CompletedCount}");
        Console.WriteLine($"Rejected: {report.RejectedCount}");
        Console.WriteLine($"Failed: {report.FailedCount}");
        Console.WriteLine($"Payment failed: {report.PaymentFailedCount}");
        Console.WriteLine($"Total revenue: {report.TotalRevenue:F2}");
        Console.WriteLine(
            $"Average order value: {report.AverageOrderValue:F2}");
        Console.WriteLine($"Top-selling product: {report.TopSellingProduct}");
        Console.WriteLine(
            $"Top-selling quantity: {report.TopSellingQuantity}");
    }

    private static Order CopyOrder(Order source)
    {
        return new Order
        {
            Id = source.Id,
            CustomerId = source.CustomerId,

            Items = source.Items.Select(item => new OrderItem
            {
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice
            }).ToList(),

            CouponCode = source.CouponCode,
            Status = source.Status,
            CreatedAt = source.CreatedAt,
            SubTotal = source.SubTotal,
            Discount = source.Discount,
            ShippingCost = source.ShippingCost,
            TotalAmount = source.TotalAmount,
            FailureReason = source.FailureReason
        };
    }
}
