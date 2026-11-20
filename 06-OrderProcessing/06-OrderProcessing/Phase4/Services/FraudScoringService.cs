using System.Collections.Concurrent;
using System.Diagnostics;

namespace _06_OrderProcessing.Phase4.Services;

public class FraudScoringService
{
    private static int Score(int orderId)
    {
        long h = orderId;

        for (int i = 0; i < 100_000; i++)
            h = (h * 31 + i) % 1_000_000_007;

        return (int)(h % 100);
    }

    public async Task RunAsync(CancellationToken ct)
    {
        const int orderCount = 10_000;

        var stopwatch = Stopwatch.StartNew();
        var sequentialResults = new List<int>();

        for (int id = 1; id <= orderCount; id++)
        {
            ct.ThrowIfCancellationRequested();

            if (Score(id) >= 90)
                sequentialResults.Add(id);
        }

        stopwatch.Stop();
        double sequentialMs = stopwatch.Elapsed.TotalMilliseconds;

        var parallelResults = new ConcurrentBag<int>();
        var threadIds = new ConcurrentDictionary<int, byte>();

        stopwatch.Restart();

        await Parallel.ForEachAsync(
            Enumerable.Range(1, orderCount),
            new ParallelOptions
            {
                CancellationToken = ct,
                MaxDegreeOfParallelism = Environment.ProcessorCount
            },
            (id, token) =>
            {
                token.ThrowIfCancellationRequested();

                threadIds.TryAdd(
                    Environment.CurrentManagedThreadId, 0);

                if (Score(id) >= 90)
                    parallelResults.Add(id);

                return ValueTask.CompletedTask;
            });

        stopwatch.Stop();
        double parallelMs = stopwatch.Elapsed.TotalMilliseconds;

        var sequentialSet = sequentialResults.ToHashSet();
        var parallelSet = parallelResults.ToHashSet();

        Console.WriteLine("=== Fraud Scoring ===");
        Console.WriteLine($"Sequential flagged: {sequentialSet.Count}");
        Console.WriteLine($"Parallel flagged: {parallelSet.Count}");
        Console.WriteLine($"Same IDs: {sequentialSet.SetEquals(parallelSet)}");
        Console.WriteLine($"Sequential: {sequentialMs:F0} ms");
        Console.WriteLine($"Parallel: {parallelMs:F0} ms");
        Console.WriteLine(
            $"Speed-up: {(parallelMs > 0 ? sequentialMs / parallelMs : 0):F2}x");
        Console.WriteLine($"Threads observed: {threadIds.Count}");
        Console.WriteLine($"Processor count: {Environment.ProcessorCount}");
    }
}
