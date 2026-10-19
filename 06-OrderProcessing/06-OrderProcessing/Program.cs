using _06_OrderProcessing.Phase1;
using _06_OrderProcessing.Phase1.Async;
using _06_OrderProcessing.Shared.AppLogger;
using _06_OrderProcessing.Shared.Models;
using _06_OrderProcessing.Shared.Storage;
using System;

#region Reset data to its initial state before testing.
await DataSeeder.ResetAsync(CancellationToken.None);

Console.WriteLine("Data seeding completed.");
#endregion

var store = new JsonFileStore();

#region Test cancellation token.
using var cts = new CancellationTokenSource();
CancellationToken ct = cts.Token;
#endregion

#region Product Test
var productRepository = new ProductRepository(store);

//Console.WriteLine("=== Product Test ===");

//Product? product = await productRepository.GetByIdAsync(1, ct);

//if (product is not null)
//{
//    Console.WriteLine($"Stock before: {product.StockQuantity}");

//    bool reserved = await productRepository.ReserveStockAsync(1, 1, ct);

//    if (reserved)
//        Log.Success("Program", "Reservation successful.");
//    else
//        Log.Error("Program", "Reservation failed.");

//    Product? updatedProduct = await productRepository.GetByIdAsync(1, ct);

//    if (updatedProduct is not null)
//        Console.WriteLine($"Stock after: {updatedProduct.StockQuantity}");
//}
#endregion

# region Test retrieving multiple customers.
var customerRepository = new CustomerRepository(store);

//Console.WriteLine("\n=== Customer Test ===");

//for (int id = 1; id <= 3; id++)
//{
//    Customer? customer = await customerRepository.GetByIdAsync(id, ct);

//    if (customer is not null)
//    {
//        Log.Success("Program", $"Customer found: {customer.Name}");
//    }
//    else
//    {
//        Log.Error("Program", $"Customer #{id} not found.");
//    }
//}
#endregion

# region Test concurrent reservations for the last available stock.
//int productId = 3;
//int quantity = 2;

//Task<bool> customer1 = productRepository.ReserveStockAsync(productId, quantity, ct);
//Task<bool> customer2 = productRepository.ReserveStockAsync(productId, quantity, ct);

//bool result1 = await customer1;
//bool result2 = await customer2;

//Console.WriteLine($"Customer 1 reservation: {result1}");
//Console.WriteLine($"Customer 2 reservation: {result2}");

//Product? finalProduct = await productRepository.GetByIdAsync(productId, ct);

//if (finalProduct is not null)
//    Console.WriteLine($"Remaining stock: {finalProduct.StockQuantity}");


#endregion

#region Phase 1 Setup and Execution
// Create Async Repositories
CouponRepository couponRepository = new(store);
OrderRepository orderRepository = new(store);
IncomingOrderRepository incomingOrderRepository = new(store);

// Create Async Validator
OrderValidator orderValidator = new(
    customerRepository,
    productRepository,
    couponRepository,
    orderRepository);

// Create Sync Repositories
SyncCustomerRepository syncCustomerRepository = new(store);
SyncProductRepository syncProductRepository = new(store);
SyncCouponRepository syncCouponRepository = new(store);

// Create Sync Validator
SyncOrderValidator syncOrderValidator = new(
    syncCustomerRepository,
    syncProductRepository,
    syncCouponRepository);

// Create Phase 1 Runner
Phase1Runner runner = new(
    store,
    incomingOrderRepository,
    orderRepository,
    syncOrderValidator,
    orderValidator);

// Run Phase 1
await runner.RunAsync();

#endregion

