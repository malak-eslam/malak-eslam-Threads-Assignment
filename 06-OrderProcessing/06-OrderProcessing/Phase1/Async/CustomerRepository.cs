using _06_OrderProcessing.Shared.AppLogger;
using _06_OrderProcessing.Shared.Models;
using _06_OrderProcessing.Shared.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _06_OrderProcessing.Phase1.Async;
public class CustomerRepository
{
    private readonly JsonFileStore _store;
    public CustomerRepository(JsonFileStore store)
    {
        _store = store;
    }

    public async Task<Customer?> GetByIdAsync(int id, CancellationToken ct)
    {
        // Step 1: Open connection
        Log.Info("CustomerRepository", "Opening connection...");

        //Console.WriteLine($"Before await: Thread {Environment.CurrentManagedThreadId}");

        await Task.Delay(200, ct);

        //Console.WriteLine($"After await: Thread {Environment.CurrentManagedThreadId}");

        // Step 2: Execute query
        Log.Info("CustomerRepository", $"Executing query for Customer #{id}...");
        await Task.Delay(Random.Shared.Next(300, 601), ct);

        // Read customers from JSON
        List<Customer> customers = await _store.ReadAsync<Customer>(DataFiles.Customers, ct);

        // Find customer
        Customer? customer = null;
        foreach (Customer item in customers)
        {
            if (item.Id == id) { customer = item; break; }
        }

        // Check result
        if (customer is null)
        {
            Log.Error("CustomerRepository", $"Customer #{id} not found."); return null;
        }

        Log.Success("CustomerRepository", $"Customer #{id} found ({customer.Name}).");

        return customer;
    }
}
