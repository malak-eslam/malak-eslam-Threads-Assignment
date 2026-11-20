using System;
using System.Threading.Tasks;

namespace _06_OrderProcessing.Phase2;

public static class BlockingDemo
{
    public static void Run()
    {
        Console.WriteLine("Starting blocking demo...");

        int result = GetNumberAsync().Result;

        Console.WriteLine($"Result: {result}");
        Console.WriteLine("Demo finished.");
    }

    private static async Task<int> GetNumberAsync()
    {
        await Task.Delay(1000);
        return 10;
    }
}