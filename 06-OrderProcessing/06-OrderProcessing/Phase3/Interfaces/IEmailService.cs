using _06_OrderProcessing.Shared.Models;

namespace _06_OrderProcessing.Phase3.Interfaces;

public interface IEmailService
{
    Task SendConfirmationAsync(Order order,Customer customer,CancellationToken ct);

    Task SendWithRetryAsync(Order order,Customer customer,CancellationToken ct);
}
