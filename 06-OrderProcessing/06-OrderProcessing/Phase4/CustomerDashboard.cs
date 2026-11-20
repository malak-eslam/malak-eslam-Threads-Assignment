using _06_OrderProcessing.Shared.Models;

namespace _06_OrderProcessing.Phase4;

public class CustomerDashboard
{
    public Customer? Customer { get; init; }

    public List<Order> Orders { get; init; } = new();

    public List<PaymentTransaction> Payments { get; init; } = new();

    public List<EmailMessage> Emails { get; init; } = new();
}
