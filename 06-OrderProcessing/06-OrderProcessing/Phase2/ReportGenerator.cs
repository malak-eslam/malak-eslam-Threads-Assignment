using _06_OrderProcessing.Phase1.Async;
using _06_OrderProcessing.Shared.Models;
using _06_OrderProcessing.Shared.Storage;
using System;
using System.Text.Json;

namespace _06_OrderProcessing.Phase2;

public class ReportGenerator
{
    private readonly OrderRepository _orderRepository;
    private readonly ProductRepository _productRepository;

    private readonly string _reportDirectory =
        Path.Combine(
            AppContext.BaseDirectory,
            "Data",
            "reports");

    public ReportGenerator(
        OrderRepository orderRepository,
        ProductRepository productRepository)
    {
        _orderRepository = orderRepository;
        _productRepository = productRepository;
    }

    public async Task<Report> GenerateAsync(CancellationToken ct)
    {
        List<Order> orders =
            await _orderRepository.GetAllAsync(ct);

        List<Product> products =
            await ReadProductsAsync(ct);

        List<Order> completedOrders = new();

        int rejected = 0;
        int failed = 0;
        int paymentFailed = 0;

        foreach (Order order in orders)
        {
            switch (order.Status)
            {
                case OrderStatus.Completed:
                    completedOrders.Add(order);
                    break;

                case OrderStatus.Rejected:
                    rejected++;
                    break;

                case OrderStatus.Failed:
                    failed++;
                    break;

                case OrderStatus.PaymentFailed:
                    paymentFailed++;
                    break;
            }
        }

        decimal totalRevenue = 0m;

        foreach (Order order in completedOrders)
        {
        //    Console.WriteLine(
        //$"Order #{order.Id}: {order.TotalAmount:F2}");
            totalRevenue += order.TotalAmount;
        }

        //Console.WriteLine($"Calculated revenue: {totalRevenue:F2}");


        totalRevenue = Math.Round(
            totalRevenue, 2, MidpointRounding.AwayFromZero);

        
        decimal averageOrderValue = completedOrders.Count == 0
            ? 0m
            : Math.Round(
                totalRevenue / completedOrders.Count,
                2,
                MidpointRounding.AwayFromZero);

        Dictionary<int, int> productSales = new();

        foreach (Order order in completedOrders)
        {
            foreach (OrderItem item in order.Items)
            {
                if (!productSales.ContainsKey(item.ProductId))
                    productSales[item.ProductId] = 0;

                productSales[item.ProductId] += item.Quantity;
            }
        }

        int topProductId = 0;
        int topProductQuantity = 0;

        foreach (KeyValuePair<int, int> sale in productSales)
        {
            if (sale.Value > topProductQuantity ||
                (sale.Value == topProductQuantity &&
                 sale.Key < topProductId))
            {
                topProductId = sale.Key;
                topProductQuantity = sale.Value;
            }
        }

        string? topProductName = null;

        foreach (Product product in products)
        {
            if (product.Id == topProductId)
            {
                topProductName = product.Name;
                break;
            }
        }

        Report report = new()
        {
            TotalOrders = orders.Count,
            CompletedCount = completedOrders.Count,
            RejectedCount = rejected,
            FailedCount = failed,
            PaymentFailedCount = paymentFailed,
            TotalRevenue = totalRevenue,
            AverageOrderValue = averageOrderValue,
            TopSellingProduct = topProductName ?? string.Empty,
            TopSellingQuantity = topProductQuantity,
            GeneratedAt = DateTime.Now
        };

        Directory.CreateDirectory(_reportDirectory);

        string fileName =
            $"report_{DateTime.Now:yyyyMMdd_HHmmss}.json";

        string filePath = Path.Combine(_reportDirectory, fileName);

        string json = JsonSerializer.Serialize(
            report,
            new JsonSerializerOptions { WriteIndented = true });

        await File.WriteAllTextAsync(filePath, json, ct);

        return report;
    }

    private async Task<List<Product>> ReadProductsAsync(
        CancellationToken ct)
    {
        return await _productRepository.GetAllAsync(ct);
    }
}
