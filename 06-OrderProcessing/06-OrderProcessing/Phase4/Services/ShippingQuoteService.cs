using System.Diagnostics;
using _06_OrderProcessing.Shared.Exceptions;
using _06_OrderProcessing.Shared.Models;

namespace _06_OrderProcessing.Phase4.Services;

public class ShippingQuoteService
{
    private readonly IReadOnlyList<IShippingCarrier> _carriers;

    public ShippingQuoteService(IEnumerable<IShippingCarrier> carriers)
    {
        _carriers = new List<IShippingCarrier>(carriers);
    }

    public async Task<ShippingQuote> GetFastestQuoteAsync(
        Order order,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        using var timeoutCts =
            CancellationTokenSource.CreateLinkedTokenSource(ct);

        using var deadlineCts =
            CancellationTokenSource.CreateLinkedTokenSource(ct);

        var timer = Stopwatch.StartNew();

        List<Task<(string Carrier, decimal Price, Exception? Error)>> tasks =
            new();

        foreach (IShippingCarrier carrier in _carriers)
        {
            tasks.Add(GetQuoteAsync(carrier, order, timeoutCts.Token));
        }

        Task timeoutTask = Task.Delay(
            TimeSpan.FromMilliseconds(1200),
            deadlineCts.Token);

        try
        {
            while (tasks.Count > 0)
            {
                Task completedTask = await Task.WhenAny(
                    tasks.Cast<Task>().Append(timeoutTask));

                ct.ThrowIfCancellationRequested();

                if (completedTask == timeoutTask)
                {
                    timeoutCts.Cancel();
                    timer.Stop();

                    Console.WriteLine(
                        "Shipping quote timed out. Using flat rate.");

                    return new ShippingQuote(
                        "Flat rate",
                        25.00m,
                        timer.ElapsedMilliseconds);
                }

                var completed =
                    (Task<(string Carrier, decimal Price, Exception? Error)>)
                    completedTask;

                tasks.Remove(completed);

                var result = await completed;

                if (result.Error is not null)
                {
                    Console.WriteLine(
                        $"[{result.Carrier}] failed: {result.Error.Message}");

                    continue;
                }

                deadlineCts.Cancel();
                timeoutCts.Cancel();
                timer.Stop();

                Console.WriteLine(
                    $"[{result.Carrier}] quote received: {result.Price:F2}");

                return new ShippingQuote(
                    result.Carrier,
                    result.Price,
                    timer.ElapsedMilliseconds);
            }

            timer.Stop();

            throw new ShippingProviderUnavailableException(
                "All carriers failed");
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            timeoutCts.Cancel();
            throw;
        }
    }

    private static async Task<(
        string Carrier,
        decimal Price,
        Exception? Error)> GetQuoteAsync(
        IShippingCarrier carrier,
        Order order,
        CancellationToken ct)
    {
        try
        {
            decimal price = await carrier.GetQuoteAsync(order, ct);

            return (carrier.Name, price, null);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            return (carrier.Name, 0m, ex);
        }
    }
}
