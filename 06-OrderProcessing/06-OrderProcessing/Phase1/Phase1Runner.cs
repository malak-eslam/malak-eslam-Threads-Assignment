using _06_OrderProcessing.Phase1.Async;
using _06_OrderProcessing.Shared.Models;
using _06_OrderProcessing.Shared.Storage;
using System.Diagnostics;

namespace _06_OrderProcessing.Phase1;

public class Phase1Runner
{
    private readonly JsonFileStore _store;
    private readonly IncomingOrderRepository _incomingRepository;
    private readonly OrderRepository _orderRepository;
    private readonly SyncOrderValidator _syncValidator;
    private readonly OrderValidator _asyncValidator;

    public Phase1Runner(
        JsonFileStore store,
        IncomingOrderRepository incomingRepository,
        OrderRepository orderRepository,
        SyncOrderValidator syncValidator,
        OrderValidator asyncValidator)
    {
        _store = store;
        _incomingRepository = incomingRepository;
        _orderRepository = orderRepository;
        _syncValidator = syncValidator;
        _asyncValidator = asyncValidator;
    }

    public async Task RunAsync(CancellationToken ct = default)
    {
        List<Order> incomingOrders =
            await _incomingRepository.GetAllAsync(ct);

        // Sync run: validate without saving results
        Console.WriteLine("\n===== Sync Run =====");

        Stopwatch syncWatch = Stopwatch.StartNew();

        foreach (Order order in incomingOrders)
        {
            _syncValidator.ValidateOrder(order);
        }

        syncWatch.Stop();

        // Reset orders.json
        await _store.UpdateAsync<Order>(
            DataFiles.Orders,
            orders => orders.Clear(),
            ct);

        // Async run: validate and save each order
        Console.WriteLine("\n===== Async Run =====");

        Stopwatch asyncWatch = Stopwatch.StartNew();

        foreach (Order order in incomingOrders)
        {
            await _asyncValidator.ValidateOrderAsync(order, ct);
            await _orderRepository.SaveAsync(order, ct);
        }

        asyncWatch.Stop();

        // Read results back from orders.json
        List<Order> validated =
            await _orderRepository.GetByStatusAsync(
                OrderStatus.Validated, ct);

        List<Order> rejected =
            await _orderRepository.GetByStatusAsync(
                OrderStatus.Rejected, ct);

        Console.WriteLine("\n===== Validation Summary =====");
        Console.WriteLine($"Total Orders: {validated.Count + rejected.Count}");
        Console.WriteLine($"Validated:    {validated.Count}");
        Console.WriteLine($"Rejected:     {rejected.Count}");

        foreach (Order order in rejected)
        {
            Console.WriteLine(
                $"  - Order #{order.Id}: {order.FailureReason}");
        }

        Console.WriteLine($"Sync Time:  {syncWatch.Elapsed.TotalSeconds:F1} seconds");
        Console.WriteLine($"Async Time: {asyncWatch.Elapsed.TotalSeconds:F1} seconds");
        Console.WriteLine($"orders.json now contains {validated.Count + rejected.Count} records.");
    }
}