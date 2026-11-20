using _06_OrderProcessing.Shared.Models;
using _06_OrderProcessing.Shared.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _06_OrderProcessing.Phase2;
public class InvoiceGenerator
{
    private readonly string _invoiceDirectory =
        Path.Combine(
            AppContext.BaseDirectory,
            "Data",
            DataFiles.InvoicesDirectory);

    public async Task GenerateAsync(Order order)
    {

        await Task.Run(() =>
        {
            long sum = 0;

            for (int i = 0; i < 20_000_000; i++)
            {
                sum += i;
            }
        });

        Directory.CreateDirectory(_invoiceDirectory);


        StringBuilder invoice = new();

        invoice.AppendLine($"Invoice #{order.Id}");
        invoice.AppendLine($"Customer ID: {order.CustomerId}");
        invoice.AppendLine($"Status: {order.Status}");
        invoice.AppendLine();

        invoice.AppendLine("Items:");

        foreach (OrderItem item in order.Items)
        {
            invoice.AppendLine(
                $"Product ID: {item.ProductId}, " +
                $"Quantity: {item.Quantity}, " +
                $"Unit Price: {item.UnitPrice:F2}");
        }

        invoice.AppendLine();
        invoice.AppendLine($"Subtotal: {order.SubTotal:F2}");
        invoice.AppendLine($"Discount: {order.Discount:F2}");
        invoice.AppendLine($"Shipping: {order.ShippingCost:F2}");
        invoice.AppendLine($"Total: {order.TotalAmount:F2}");


        string filePath = Path.Combine(_invoiceDirectory,$"invoice_{order.Id}.txt");

        await File.WriteAllTextAsync(filePath,invoice.ToString());
    }
}