using _06_OrderProcessing.Shared.Models;
using _06_OrderProcessing.Shared.Storage;

namespace _06_OrderProcessing.Phase1.Async;

public class IncomingOrderRepository
{
    private readonly JsonFileStore _store;

    public IncomingOrderRepository(JsonFileStore store)
    {
        _store = store;
    }

    public async Task<List<Order>> GetAllAsync(CancellationToken ct)
    {
        return await _store.ReadAsync<Order>(
            DataFiles.IncomingOrders, ct);
    }
}

