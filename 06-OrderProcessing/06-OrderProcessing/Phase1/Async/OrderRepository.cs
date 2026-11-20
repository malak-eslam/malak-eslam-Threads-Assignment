using _06_OrderProcessing.Shared.Exceptions;
using _06_OrderProcessing.Shared.Models;
using _06_OrderProcessing.Shared.Storage;

namespace _06_OrderProcessing.Phase1.Async;

public class OrderRepository
{
    private readonly JsonFileStore _store;
    private readonly HashSet<int> _failSaveForOrderIds;

    public OrderRepository(JsonFileStore store , IEnumerable<int>? failSaveForOrderIds = null)
    {
        _store = store;
        _failSaveForOrderIds = new HashSet<int>(
            failSaveForOrderIds ?? Enumerable.Empty<int>());
    }

    public async Task SaveAsync(Order order, CancellationToken ct)
    {
        if (order.Status == OrderStatus.Completed &&
            _failSaveForOrderIds.Contains(order.Id))
        {
            throw new DatabaseWriteException("Connection lost during commit");
        }

        await _store.UpdateAsync<Order>(DataFiles.Orders,
            
            orders =>
            {
                for (int i = 0; i < orders.Count; i++)
                {
                    if (orders[i].Id == order.Id)
                    {
                        orders[i] = order;
                        return;
                    }
                }

                orders.Add(order);
            },
            ct);
    }

    public async Task<List<Order>> GetAllAsync(CancellationToken ct)
    {
        return await _store.ReadAsync<Order>(DataFiles.Orders, ct);
    }

    public async Task<List<Order>> GetByStatusAsync(OrderStatus status,CancellationToken ct)
    {
        List<Order> orders = await GetAllAsync(ct);
        List<Order> result = new();

        foreach (Order order in orders)
        {
            if (order.Status == status)
                result.Add(order);
        }

        return result;
    }
}

