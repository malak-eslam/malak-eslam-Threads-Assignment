using _06_OrderProcessing.Phase1.Async;
using _06_OrderProcessing.Phase3.Interfaces;
using _06_OrderProcessing.Phase3.Services;
using _06_OrderProcessing.Phase4.Services;
using _06_OrderProcessing.Shared.AppLogger;
using _06_OrderProcessing.Shared.Models;
using _06_OrderProcessing.Shared.Storage;

namespace _06_OrderProcessing.Phase4;

public class Phase4Runner
{
    public async Task RunAsync(CancellationToken ct)
    {
        Console.WriteLine("\n========== PHASE 4 ==========\n");

        await DataSeeder.ResetAsync(ct);

        Log.Info("Phase4", "Task 10.1: Live checkout progress");
        await new LiveCheckoutRunner().RunAsync(ct);

        var store = new JsonFileStore();

        Log.Info("Phase4", "Task 10.2: Customer dashboard");

        var dashboardService = new CustomerDashboardService(
            store,
            failPaymentsForCustomerIds: new[] { 99 });

        await TestDashboardAsync(dashboardService, 3, ct);
        await TestDashboardAsync(dashboardService, 99, ct);

        Log.Info("Phase4", "Task 10.3: Fastest shipping quote");

        var carriers = new List<IShippingCarrier>
        {
            new FastShip(),
            new CairoExpress(),
            new NileCourier()
        };

        var shippingService = new ShippingQuoteService(carriers);

        List<Order> orders = await store.ReadAsync<Order>(
            DataFiles.IncomingOrders, ct);

        for (int i = 0; i < Math.Min(5, orders.Count); i++)
        {
            try
            {
                ShippingQuote quote =
                    await shippingService.GetFastestQuoteAsync(orders[i], ct);

                Console.WriteLine(
                    $"Order #{orders[i].Id}: {quote.Carrier} - {quote.Price:F2}");
            }
            catch (Exception ex)
            {
                Log.Error("Phase4", $"Order #{orders[i].Id}: {ex.Message}");
            }
        }

        Log.Info("Phase4", "Task 10.4 A: Fraud scoring");

        var fraudService = new FraudScoringService();
        await fraudService.RunAsync(ct);

        Log.Info("Phase4", "Task 10.4 B: Email resend");

        var emailService = new EmailService(store);
        var resendService = new EmailResendService(store, emailService);

        await resendService.RunAsync(ct);

        Log.Success("Phase4", "Phase 4 tasks finished.");
    }

    private static async Task TestDashboardAsync(
        CustomerDashboardService service,
        int customerId,
        CancellationToken ct)
    {
        try
        {
            CustomerDashboard dashboard =
                await service.LoadAsync(customerId, ct);

            Console.WriteLine(
                $"Customer #{customerId}: " +
                $"Orders={dashboard.Orders.Count}, " +
                $"Payments={dashboard.Payments.Count}, " +
                $"Emails={dashboard.Emails.Count}");
        }
        catch (AggregateException ex)
        {
            foreach (Exception error in ex.Flatten().InnerExceptions)
            {
                Log.Error(
                    "Dashboard",
                    $"Customer #{customerId}: {error.Message}");
            }
        }
    }
}
