using System.Diagnostics;
using _06_OrderProcessing.Phase1.Async;
using _06_OrderProcessing.Shared.Models;
using _06_OrderProcessing.Shared.Storage;

namespace _06_OrderProcessing.Phase4.Services;

public class CustomerDashboardService
{
    private readonly JsonFileStore _store;
    private readonly HashSet<int> _failPaymentsForCustomerIds;

    public CustomerDashboardService(
        JsonFileStore store,
        IEnumerable<int>? failPaymentsForCustomerIds = null)
    {
        _store = store;
        _failPaymentsForCustomerIds =
            new HashSet<int>(failPaymentsForCustomerIds ?? []);
    }

    public async Task<CustomerDashboard> LoadAsync(
        int customerId,
        CancellationToken ct)
    {
        var stopwatch = Stopwatch.StartNew();

        // Start all data-loading operations concurrently.
        Task<Customer?> customerTask = LoadCustomerAsync(customerId, ct);
        Task<List<Order>> ordersTask = LoadOrdersAsync(customerId, ct);
        Task<List<PaymentTransaction>> paymentsTask =
            LoadPaymentsAsync(customerId, ct);
        Task<List<EmailMessage>> emailsTask =
            LoadEmailsAsync(customerId, ct);

        Task[] tasks =
        [
            customerTask,
            ordersTask,
            paymentsTask,
            emailsTask
        ];

        try
        {
            await Task.WhenAll(tasks);
        }
        catch
        {
            // Collect all errors instead of reporting only the first one.
            List<Exception> errors = new();

            foreach (Task task in tasks)
            {
                if (task.IsFaulted && task.Exception is not null)
                {
                    errors.AddRange(
                        task.Exception.Flatten().InnerExceptions);
                }
            }

            if (errors.Count > 0)
                throw new AggregateException(errors);

            throw;
        }

        stopwatch.Stop();

        Console.WriteLine(
            $"Dashboard loaded in {stopwatch.Elapsed.TotalSeconds:F2} s");

        return new CustomerDashboard
        {
            Customer = await customerTask,
            Orders = await ordersTask,
            Payments = await paymentsTask,
            Emails = await emailsTask
        };
    }

    private async Task<Customer?> LoadCustomerAsync(
        int customerId,
        CancellationToken ct)
    {
        await Task.Delay(300, ct);

        List<Customer> customers =
            await _store.ReadAsync<Customer>(DataFiles.Customers, ct);

        foreach (Customer customer in customers)
        {
            if (customer.Id == customerId)
                return customer;
        }

        throw new InvalidOperationException(
            $"Customer #{customerId} not found");
    }

    private async Task<List<Order>> LoadOrdersAsync(
        int customerId,
        CancellationToken ct)
    {
        await Task.Delay(500, ct);

        List<Order> orders =
            await _store.ReadAsync<Order>(DataFiles.Orders, ct);

        List<Order> customerOrders = new();

        foreach (Order order in orders)
        {
            if (order.CustomerId == customerId)
                customerOrders.Add(order);
        }

        return customerOrders;
    }

    private async Task<List<PaymentTransaction>> LoadPaymentsAsync(
        int customerId,
        CancellationToken ct)
    {
        await Task.Delay(400, ct);

        if (_failPaymentsForCustomerIds.Contains(customerId))
            throw new TimeoutException("Payments store timeout");

        List<PaymentTransaction> payments =
            await _store.ReadAsync<PaymentTransaction>(
                DataFiles.Payments, ct);

        List<Order> orders =
            await _store.ReadAsync<Order>(DataFiles.Orders, ct);

        HashSet<int> customerOrderIds = new();

        foreach (Order order in orders)
        {
            if (order.CustomerId == customerId)
                customerOrderIds.Add(order.Id);
        }

        List<PaymentTransaction> customerPayments = new();

        foreach (PaymentTransaction payment in payments)
        {
            if (customerOrderIds.Contains(payment.OrderId))
                customerPayments.Add(payment);
        }

        return customerPayments;
    }

    private async Task<List<EmailMessage>> LoadEmailsAsync(
        int customerId,
        CancellationToken ct)
    {
        await Task.Delay(350, ct);

        List<EmailMessage> emails =
            await _store.ReadAsync<EmailMessage>(DataFiles.Emails, ct);

        List<Order> orders =
            await _store.ReadAsync<Order>(DataFiles.Orders, ct);

        HashSet<int> customerOrderIds = new();

        foreach (Order order in orders)
        {
            if (order.CustomerId == customerId)
                customerOrderIds.Add(order.Id);
        }

        List<EmailMessage> customerEmails = new();

        foreach (EmailMessage email in emails)
        {
            if (customerOrderIds.Contains(email.OrderId))
                customerEmails.Add(email);
        }

        return customerEmails;
    }
}
