using _06_OrderProcessing.Phase1;
using _06_OrderProcessing.Phase1.Async;
using _06_OrderProcessing.Phase2;
using _06_OrderProcessing.Phase3;
using _06_OrderProcessing.Phase4;
using _06_OrderProcessing.Shared.Storage;

var store = new JsonFileStore();

using var cts = new CancellationTokenSource();

Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
    Console.WriteLine("\nCancellation requested...");
};

string? mode = args.FirstOrDefault()?.ToLowerInvariant();

if (mode is null)
{
    Console.WriteLine("=== Order Processing System ===");
    Console.WriteLine("1. Phase 1");
    Console.WriteLine("2. Phase 2");
    Console.WriteLine("3. Phase 3");
    Console.WriteLine("4. Phase 4");
    Console.Write("Choose a phase: ");

    mode = Console.ReadLine()?.Trim() switch
    {
        "1" => "phase1",
        "2" => "phase2",
        "3" => "phase3",
        "4" => "phase4",
        _ => "unknown"
    };
}

try
{
    switch (mode)
    {
        case "phase1":
            {
                var productRepository = new ProductRepository(store);
                var customerRepository = new CustomerRepository(store);
                var couponRepository = new CouponRepository(store);
                var orderRepository = new OrderRepository(store);
                var incomingOrderRepository = new IncomingOrderRepository(store);

                var asyncValidator = new OrderValidator(
                    customerRepository,
                    productRepository,
                    couponRepository,
                    orderRepository);

                var syncCustomerRepository = new SyncCustomerRepository(store);
                var syncProductRepository = new SyncProductRepository(store);
                var syncCouponRepository = new SyncCouponRepository(store);

                var syncValidator = new SyncOrderValidator(
                    syncCustomerRepository,
                    syncProductRepository,
                    syncCouponRepository);

                var runner = new Phase1Runner(
                    store,
                    incomingOrderRepository,
                    orderRepository,
                    syncValidator,
                    asyncValidator);

                await runner.RunAsync(cts.Token);
                break;
            }

        case "phase2":
            await new Phase2Runner().RunAsync(cts.Token);
            break;

        case "phase3":
            await new Phase3Runner().RunAsync(cts.Token);
            break;

        case "phase4":
            await new Phase4Runner().RunAsync(cts.Token);
            break;

        case "all":
            await new Phase1Runner(
                store,
                new IncomingOrderRepository(store),
                new OrderRepository(store),
                new SyncOrderValidator(
                    new SyncCustomerRepository(store),
                    new SyncProductRepository(store),
                    new SyncCouponRepository(store)),
                new OrderValidator(
                    new CustomerRepository(store),
                    new ProductRepository(store),
                    new CouponRepository(store),
                    new OrderRepository(store)))
                .RunAsync(cts.Token);

            await new Phase2Runner().RunAsync(cts.Token);
            await new Phase3Runner().RunAsync(cts.Token);
            await new Phase4Runner().RunAsync(cts.Token);
            break;

        default:
            Console.WriteLine("Unknown mode. Use phase1, phase2, phase3, phase4, or all.");
            break;
    }
}
catch (OperationCanceledException)
{
    Console.WriteLine("Operation cancelled.");
}
