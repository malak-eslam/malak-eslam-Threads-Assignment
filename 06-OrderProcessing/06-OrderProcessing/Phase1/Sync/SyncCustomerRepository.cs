using _06_OrderProcessing.Shared.AppLogger;
using _06_OrderProcessing.Shared.Models;
using _06_OrderProcessing.Shared.Storage;

namespace _06_OrderProcessing.Phase1;

public class SyncCustomerRepository
{
    private readonly JsonFileStore _store;

    public SyncCustomerRepository(JsonFileStore store)
    {
        _store = store;
    }

    public Customer? GetById(int id)
    {
        Log.Info("SyncCustomerRepository", "Opening connection...");
        Thread.Sleep(200);

        Log.Info("SyncCustomerRepository",
            $"Executing query for Customer #{id}...");
        Thread.Sleep(Random.Shared.Next(300, 601));

        List<Customer> customers =
            _store.Read<Customer>(DataFiles.Customers);

        foreach (Customer customer in customers)
        {
            if (customer.Id == id)
            {
                Log.Success("SyncCustomerRepository",
                    $"Customer #{id} found ({customer.Name}).");

                return customer;
            }
        }

        Log.Error("SyncCustomerRepository",
            $"Customer #{id} not found.");

        return null;
    }
}