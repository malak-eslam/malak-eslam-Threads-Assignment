using _06_OrderProcessing.Phase1.Async;
using _06_OrderProcessing.Phase2;
using _06_OrderProcessing.Phase3.Interfaces;
using _06_OrderProcessing.Shared.AppLogger;
using _06_OrderProcessing.Shared.Exceptions;
using _06_OrderProcessing.Shared.Models;

namespace _06_OrderProcessing.Phase3.Services;

public class OrderProcessor
{
    private readonly CustomerService _customerService;
    private readonly InventoryService _inventoryService;
    private readonly PricingService _pricingService;
    private readonly IPaymentGateway _paymentGateway;
    private readonly OrderRepository _orderRepository;
    private readonly IEmailService _emailService;

    public OrderProcessor(
        CustomerService customerService,
        InventoryService inventoryService,
        PricingService pricingService,
        IPaymentGateway paymentGateway,
        OrderRepository orderRepository,
        IEmailService emailService)
    {
        _customerService = customerService;
        _inventoryService = inventoryService;
        _pricingService = pricingService;
        _paymentGateway = paymentGateway;
        _orderRepository = orderRepository;
        _emailService = emailService;
    }

    public async Task<bool> ProcessOrderAsync(
        Order order,
        CancellationToken ct)
    {
        DateTime start = DateTime.Now;
        bool stockReserved = false;
        string? transactionId = null;

        try
        {
            ct.ThrowIfCancellationRequested();

            Log.Info("OrderProcessor", $"Starting Order #{order.Id}");

            Customer customer;

            try
            {
                customer = await _customerService.GetCustomerAsync(
                    order.CustomerId, ct);
            }
            catch (CustomerNotFoundException ex)
            {
                order.Status = OrderStatus.Rejected;
                order.FailureReason = ex.Message;

                await _orderRepository.SaveAsync(
                    order, CancellationToken.None);

                return false;
            }

            ct.ThrowIfCancellationRequested();

            if (!customer.IsActive)
            {
                order.Status = OrderStatus.Rejected;
                order.FailureReason = "Customer is inactive";

                await _orderRepository.SaveAsync(
                    order, CancellationToken.None);

                return false;
            }

            string? reservationError =
                await _inventoryService.ReserveStockAsync(order, ct);

            if (reservationError is not null)
            {
                order.Status = OrderStatus.Rejected;
                order.FailureReason = reservationError;

                await _orderRepository.SaveAsync(
                    order, CancellationToken.None);

                return false;
            }

            stockReserved = true;

            ct.ThrowIfCancellationRequested();

            try
            {
                await _pricingService.CalculateTotalAsync(order, ct);
            }
            catch (InvalidCouponException ex)
            {
                await _inventoryService.ReleaseStockAsync(
                    order, CancellationToken.None);

                stockReserved = false;
                order.Status = OrderStatus.Rejected;
                order.FailureReason = ex.Message;

                await _orderRepository.SaveAsync(
                    order, CancellationToken.None);

                return false;
            }

            ct.ThrowIfCancellationRequested();

            PaymentResult paymentResult;

            using (var paymentCts =
                CancellationTokenSource.CreateLinkedTokenSource(ct))
            {
                paymentCts.CancelAfter(TimeSpan.FromSeconds(3));

                try
                {
                    paymentResult =
                        await _paymentGateway.ProcessPaymentAsync(
                            order.Id,
                            order.TotalAmount,
                            paymentCts.Token);
                }
                catch (OperationCanceledException)
                    when (!ct.IsCancellationRequested &&
                          paymentCts.IsCancellationRequested)
                {
                    await _inventoryService.ReleaseStockAsync(
                        order, CancellationToken.None);

                    stockReserved = false;
                    order.Status = OrderStatus.PaymentFailed;
                    order.FailureReason = "Payment gateway timeout (3s)";

                    await _orderRepository.SaveAsync(
                        order, CancellationToken.None);

                    return false;
                }
            }

            // Save the transaction ID before checking cancellation.
            transactionId = paymentResult.TransactionId;
            order.TransactionId = transactionId;

            ct.ThrowIfCancellationRequested();

            if (!paymentResult.Success)
            {
                await _inventoryService.ReleaseStockAsync(
                    order, CancellationToken.None);

                stockReserved = false;
                order.Status = OrderStatus.PaymentFailed;
                order.FailureReason = paymentResult.Message;

                await _orderRepository.SaveAsync(
                    order, CancellationToken.None);

                return false;
            }

            order.Status = OrderStatus.Completed;

            try
            {
                await _orderRepository.SaveAsync(order, ct);
            }
            catch (DatabaseWriteException ex)
            {
                Log.Error(
                    "OrderProcessor",
                    $"Saving Order #{order.Id} failed: {ex.Message}");

                if (transactionId is not null)
                {
                    await _paymentGateway.RefundAsync(transactionId);
                }

                if (stockReserved)
                {
                    await _inventoryService.ReleaseStockAsync(
                        order, CancellationToken.None);

                    stockReserved = false;
                }

                order.Status = OrderStatus.Failed;
                order.FailureReason =
                    "Database save failed after payment; payment was refunded.";

                try
                {
                    await _orderRepository.SaveAsync(
                        order, CancellationToken.None);
                }
                catch (DatabaseWriteException saveEx)
                {
                    Log.Error(
                        "OrderProcessor",
                        $"Could not save failed Order #{order.Id}: " +
                        saveEx.Message);
                }

                return false;
            }

            await _emailService.SendWithRetryAsync(order, customer, ct);

            stockReserved = false;

            Log.Success(
                "OrderProcessor",
                $"Order #{order.Id} completed in " +
                $"{(DateTime.Now - start).TotalMilliseconds:F0} ms");

            return true;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            Log.Error(
                "OrderProcessor",
                $"Order #{order.Id} was cancelled.");

            if (stockReserved)
            {
                await _inventoryService.ReleaseStockAsync(
                    order, CancellationToken.None);

                stockReserved = false;
            }

            if (transactionId is not null)
            {
                await _paymentGateway.RefundAsync(transactionId);
            }

            order.Status = OrderStatus.Pending;
            order.FailureReason = "Cancelled";

            await _orderRepository.SaveAsync(
                order, CancellationToken.None);

            throw;
        }
    }
}
