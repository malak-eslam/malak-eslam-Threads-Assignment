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
         public async Task ReleaseStockAsync(int productId, int qty,CancellationToken ct)
        {
        if (qty <= 0)
            return;

        await _store.UpdateAsync<Product>(
            DataFiles.Products,
            products =>
            {
                foreach (Product product in products)
                {
                    if (product.Id != productId)
                        continue;

                    product.StockQuantity += qty;
                    return;
                }
            },
            ct);
        }
         public async Task<bool> ReserveStockUnsafeAsync(int productId, int qty,CancellationToken ct)
       {
        List<Product> products =
            await _store.ReadAsync<Product>(DataFiles.Products, ct);

        Product? product = null;

        foreach (Product item in products)
        {
            if (item.Id == productId)
            {
                product = item;
                break;
            }
        }

        if (product is null || qty <= 0 || product.StockQuantity < qty)
            return false;

        int currentStock = product.StockQuantity;

        await Task.Delay(200, ct); // Simulate opening a connection
        await Task.Delay(100, ct); // Simulate beginning a transaction
        await Task.Delay(400, ct); // Simulate updating the stock

        await _store.UpdateAsync<Product>(
            DataFiles.Products,
            items =>
            {
                foreach (Product item in items)
                {
                    if (item.Id == productId)
                    {
                        item.StockQuantity = currentStock - qty;
                        return;
                    }
                }
            },
            ct);

        return true;
    }
         public async Task<List<Product>> GetAllAsync(CancellationToken ct)
         {
           Log.Info("ProductRepository", "Loading all products...");

           await Task.Delay(200, ct);
 
            List<Product> products =
            await _store.ReadAsync<Product>(DataFiles.Products, ct);

            Log.Success(
            "ProductRepository",
            $"Loaded {products.Count} products.");

           return products;
         }




}







