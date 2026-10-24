using _06_OrderProcessing.Phase1.Async;
using _06_OrderProcessing.Shared.Models;
using _06_OrderProcessing.Shared.Storage;

namespace _06_OrderProcessing.Phase2;

public class RaceConditionDemo
{
    private readonly JsonFileStore _store;
    private readonly ProductRepository _productRepository;

    public RaceConditionDemo(
        JsonFileStore store,
        ProductRepository productRepository)
    {
        _store = store;
        _productRepository = productRepository;
    }

    public async Task RunAsync(CancellationToken ct)
    {
        // Test 1: Unsafe reservation
        Console.WriteLine("\n=== Unsafe Reservation Test ===");

        await SetKeyboardStockAsync(1, ct);

        Task<bool> unsafeTask10 =
            _productRepository.ReserveStockUnsafeAsync(4, 1, ct);

        Task<bool> unsafeTask11 =
            _productRepository.ReserveStockUnsafeAsync(4, 1, ct);

        bool unsafeResult10 = await unsafeTask10;
        bool unsafeResult11 = await unsafeTask11;

        Product? product =
            await _productRepository.GetByIdAsync(4, ct);

        Console.WriteLine($"Order #10 reserved: {unsafeResult10}");
        Console.WriteLine($"Order #11 reserved: {unsafeResult11}");
        Console.WriteLine($"Remaining stock: {product?.StockQuantity}");

        // Test 2: Safe reservation
        Console.WriteLine("\n=== Safe Reservation Test ===");

        await SetKeyboardStockAsync(1, ct);

        Task<bool> safeTask10 =
            _productRepository.ReserveStockAsync(4, 1, ct);

        Task<bool> safeTask11 =
            _productRepository.ReserveStockAsync(4, 1, ct);

        bool safeResult10 = await safeTask10;
        bool safeResult11 = await safeTask11;

        product = await _productRepository.GetByIdAsync(4, ct);

        Console.WriteLine($"Order #10 reserved: {safeResult10}");
        Console.WriteLine($"Order #11 reserved: {safeResult11}");
        Console.WriteLine($"Remaining stock: {product?.StockQuantity}");
    }

    private async Task SetKeyboardStockAsync(
        int quantity,
        CancellationToken ct)
    {
        await _store.UpdateAsync<Product>(
            DataFiles.Products,
            products =>
            {
                foreach (Product product in products)
                {
                    if (product.Id == 4)
                    {
                        product.StockQuantity = quantity;
                        return;
                    }
                }
            },
            ct);
    }
}
