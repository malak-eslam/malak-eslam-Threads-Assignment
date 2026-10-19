using _06_OrderProcessing.Shared.Models;
using _06_OrderProcessing.Shared.Storage;

namespace _06_OrderProcessing.Phase1.Async;

public class OrderRepository
{
    private readonly JsonFileStore _store;

    public OrderRepository(JsonFileStore store)
    {
        _store = store;
    }

    public async Task SaveAsync(Order order, CancellationToken ct)
    {
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

