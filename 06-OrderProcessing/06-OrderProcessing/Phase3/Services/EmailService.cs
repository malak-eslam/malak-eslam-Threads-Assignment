using System.Collections.Concurrent;
using _06_OrderProcessing.Phase3.Interfaces;
using _06_OrderProcessing.Shared.AppLogger;
using _06_OrderProcessing.Shared.Exceptions;
using _06_OrderProcessing.Shared.Models;
using _06_OrderProcessing.Shared.Storage;

namespace _06_OrderProcessing.Phase3.Services;

public class EmailService : IEmailService
{
    private readonly JsonFileStore _store;
    private readonly Dictionary<int, int> _failAttempts;
    private readonly ConcurrentDictionary<int, int> _attemptCounts = new();

    public EmailService(
        JsonFileStore store,
        Dictionary<int, int>? failAttempts = null)
    {
        _store = store;
        _failAttempts = failAttempts ?? new Dictionary<int, int>();
    }

    public async Task SendConfirmationAsync(
        Order order,
        Customer customer,
        CancellationToken ct)
    {
        int attempt = _attemptCounts.AddOrUpdate(
            order.Id,
            1,
            (_, previous) => previous + 1);

        if (_failAttempts.TryGetValue(order.Id, out int maxFailures)
            && attempt <= maxFailures)
        {
            throw new SmtpUnavailableException(
                $"Email sending failed for Order #{order.Id}, attempt {attempt}");
        }

        EmailMessage email = new()
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            To = customer.Email,
            Subject = $"Order #{order.Id} Confirmation",
            Body = $"Your order #{order.Id} has been completed. Total: {order.TotalAmount}",
            SentAt = DateTime.Now,
            Attempts = attempt,
            LastError = null
        };

        await _store.UpdateAsync<EmailMessage>(
            DataFiles.Emails,
            emails => emails.Add(email),
            ct);
    }

    public async Task SendWithRetryAsync(
        Order order,
        Customer customer,
        CancellationToken ct)
    {
        int[] delays = { 500, 1000, 2000 };
        string? lastError = null;

        for (int attempt = 1; attempt <= 4; attempt++)
        {
            try
            {
                await SendConfirmationAsync(order, customer, ct);
                return;
            }
            catch (SmtpUnavailableException ex)
            {
                lastError = ex.Message;

                Log.Error(
                    "EmailService",
                    $"Order #{order.Id}, attempt {attempt} failed: {ex.Message}");

                if (attempt < 4)
                    await Task.Delay(delays[attempt - 1], ct);
            }
        }

        EmailMessage retryMessage = new()
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            To = customer.Email,
            Subject = $"Order #{order.Id} Confirmation",
            Body = $"Your order #{order.Id} has been completed.",
            SentAt = DateTime.Now,
            Attempts = 4,
            LastError = lastError
        };

        await _store.UpdateAsync<EmailMessage>(
            DataFiles.EmailRetry,
            messages => messages.Add(retryMessage),
            ct);

        Log.Error(
            "EmailService",
            $"Email for Order #{order.Id} queued for retry later");
    }
}

