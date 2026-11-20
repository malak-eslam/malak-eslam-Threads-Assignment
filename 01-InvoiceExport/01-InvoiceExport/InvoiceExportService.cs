using System.Diagnostics;
using System.Threading.Tasks;

namespace InvoiceExport;

public sealed class InvoiceExportService
{
    readonly OrderDatabase _db;
    readonly CustomerFileStore _customers;
    readonly InvoiceFileWriter _invoices;

    public InvoiceExportService(OrderDatabase db, CustomerFileStore customers, InvoiceFileWriter invoices)
    {
        _db = db;
        _customers = customers;
        _invoices = invoices;
    }

    public async Task ExportAllPendingAsync()
    {
        var watch = Stopwatch.StartNew();
        List<Order> orders =await _db.GetPendingOrders();

        List<Task> tasks = new List<Task>();
        foreach (Order order in orders)
        {
            //Order fresh =await _db.GetOrderById(order.Id)
            //    ?? throw new InvalidOperationException($"Order {order.Id} missing");

            //CustomerProfile customer =await _customers.ReadProfileAsync(fresh.CustomerId);
            //string path = await _invoices.WriteInvoiceAsync(fresh, customer);
            //Console.WriteLine($"Wrote {path}");
            tasks.Add(ExportOneAsync(order));

        }

        foreach (Task task in tasks)
        {
            await task;
        }

        Console.WriteLine($"Export finished in {watch.Elapsed.TotalSeconds:F1} s");
    }

    private async Task ExportOneAsync(Order order)
    {
        Order fresh = await _db.GetOrderById(order.Id)
            ?? throw new InvalidOperationException($"Order {order.Id} missing");

        CustomerProfile customer = await _customers.ReadProfileAsync(fresh.CustomerId);
        string path = await _invoices.WriteInvoiceAsync(fresh, customer);
        Console.WriteLine($"Wrote {path}");
    }
}
