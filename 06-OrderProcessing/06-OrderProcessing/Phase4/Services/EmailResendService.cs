using System.Diagnostics;
using System.Threading;
using _06_OrderProcessing.Phase1.Async;
using _06_OrderProcessing.Phase3.Interfaces;
using _06_OrderProcessing.Shared.Models;
using _06_OrderProcessing.Shared.Storage;

namespace _06_OrderProcessing.Phase4.Services;

public class EmailResendService
{
    private readonly JsonFileStore _store;
    private readonly IEmailService _emailService;

    public EmailResendService(
        JsonFileStore store,
        IEmailService emailService)
    {
        _store = store;
        _emailService = emailService;
    }

    public async Task RunAsync(CancellationToken ct)
    {
        var stopwatch = Stopwatch.StartNew();

        List<Order> orders = new();

        for (int i = 1001; i <= 1030; i++)
        {
            orders.Add(new Order
            {
                Id = i,
                CustomerId = 1,
                Items = new List<OrderItem>(),
                Status = OrderStatus.Completed
            });
        }

        List<Customer> customers =
            await _store.ReadAsync<Customer>(DataFiles.Customers, ct);

        Customer? customer = null;

        foreach (Customer item in customers)
        {
            if (item.Id == 1)
            {
                customer = item;
                break;
            }
        }

        if (customer is null)
            throw new InvalidOperationException("Customer #1 not found.");

        int sent = 0;
        int active = 0;
        int maxActive = 0;

        await Parallel.ForEachAsync(
            orders,
            new ParallelOptions
            {
                MaxDegreeOfParallelism = 4,
                CancellationToken = ct
            },
            async (order, token) =>
            {
                int currentActive = Interlocked.Increment(ref active);

                UpdateMaximum(ref maxActive, currentActive);

                try
                {
                    await _emailService.SendWithRetryAsync(
                        order, customer, token);

                    Interlocked.Increment(ref sent);
                }
                finally
                {
                    Interlocked.Decrement(ref active);
                }
            });

        stopwatch.Stop();

        Console.WriteLine("=== Email Resend ===");
        Console.WriteLine($"Emails processed: {sent}");
        Console.WriteLine($"Maximum concurrency: {maxActive}");
        Console.WriteLine($"Elapsed: {stopwatch.Elapsed.TotalSeconds:F2} s");
    }

    private static void UpdateMaximum(ref int maximum, int value)
    {
        int current;

        do
        {
            current = Volatile.Read(ref maximum);

            if (value <= current)
                return;
        }
        while (Interlocked.CompareExchange(
            ref maximum, value, current) != current);
    }
}
