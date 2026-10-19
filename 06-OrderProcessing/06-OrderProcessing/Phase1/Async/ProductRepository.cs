using _06_OrderProcessing.Shared.AppLogger;
using _06_OrderProcessing.Shared.Models;
using _06_OrderProcessing.Shared.Storage;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace _06_OrderProcessing.Phase1.Async;
public class ProductRepository
{
    private readonly JsonFileStore _store;
    public ProductRepository(JsonFileStore store)
    {
        _store = store;
    }
    public async Task<Product?> GetByIdAsync(int id, CancellationToken ct)
    {
        Log.Info("ProductRepository", "Opening connection...");
        await Task.Delay(200, ct);

        Log.Info("ProductRepository", $"Executing query for Product #{id}...");
        await Task.Delay(Random.Shared.Next(300, 601), ct);

        List<Product> products = await _store.ReadAsync<Product>(DataFiles.Products, ct);

        Product? product = null;
        foreach (Product item in products)
        {
            if (item.Id == id)
            {
                product = item;
                break;
            }
        }

        if (product is null)
        {
            Log.Error("ProductRepository", $"Product #{id} not found.");
            return null;
        }

        Log.Success("ProductRepository", $"Product #{id} found ({product.Name}).");

        return product;
    }
        public async Task<bool> ReserveStockAsync(int productId, int qty, CancellationToken ct)
        {
          bool reserved = false;

          await _store.UpdateAsync<Product>(DataFiles.Products, products =>
          {
              foreach (Product product1 in products)
              {
                  if (product1.Id != productId)
                      continue;

                  if(qty > 0 && product1.StockQuantity >= qty)
                  {
                      product1.StockQuantity -= qty;
                      reserved = true;
                  }
                  return;

              }

          },ct);

        return reserved;
        }
}
     






