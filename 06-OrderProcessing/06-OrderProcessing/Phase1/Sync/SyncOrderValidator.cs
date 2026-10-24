using _06_OrderProcessing.Shared.Models;

namespace _06_OrderProcessing.Phase1;

public class SyncOrderValidator
{
    private readonly SyncCustomerRepository _customerRepository;
    private readonly SyncProductRepository _productRepository;
    private readonly SyncCouponRepository _couponRepository;

    public SyncOrderValidator(
        SyncCustomerRepository customerRepository,
        SyncProductRepository productRepository,
        SyncCouponRepository couponRepository)
    {
        _customerRepository = customerRepository;
        _productRepository = productRepository;
        _couponRepository = couponRepository;
    }

    public void ValidateOrder(Order order)
    {
        // Check 1: Customer exists
        Customer? customer =
            _customerRepository.GetById(order.CustomerId);

        if (customer is null)
        {
            Reject(order, $"Customer #{order.CustomerId} not found");
            return;
        }

        // Check 2: Customer is active
        if (!customer.IsActive)
        {
            Reject(order, "Customer is inactive");
            return;
        }

        // Check 3 & 4: Products exist and stock is sufficient
        foreach (OrderItem item in order.Items)
        {
            Product? product =
                _productRepository.GetById(item.ProductId);

            if (product is null)
            {
                Reject(order, $"Product #{item.ProductId} not found");
                return;
            }

            if (product.StockQuantity < item.Quantity)
            {
                Reject(order,
                    $"Insufficient stock for '{product.Name}'");
                return;
            }
        }

        // Check 5 & 6: Coupon exists and is not expired
        if (order.CouponCode is not null)
        {
            Coupon? coupon =
                _couponRepository.GetByCode(order.CouponCode);

            if (coupon is null)
            {
                Reject(order,
                    $"Coupon '{order.CouponCode}' does not exist");
                return;
            }

            if (coupon.ExpiryDate < DateTime.Today)
            {
                Reject(order,
                    $"Coupon '{order.CouponCode}' has expired");
                return;
            }
        }

        // All checks passed
        order.Status = OrderStatus.Validated;
        order.FailureReason = null;
    }

    private static void Reject(Order order, string reason)
    {
        order.Status = OrderStatus.Rejected;
        order.FailureReason = reason;
    }
}