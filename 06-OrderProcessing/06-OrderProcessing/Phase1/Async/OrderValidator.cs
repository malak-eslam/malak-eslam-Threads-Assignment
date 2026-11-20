using _06_OrderProcessing.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _06_OrderProcessing.Phase1.Async;
public class OrderValidator
{
    private readonly CustomerRepository _customerRepository;
    private readonly ProductRepository _productRepository;
    private readonly CouponRepository _couponRepository;
    private readonly OrderRepository _orderRepository;

    public OrderValidator(CustomerRepository customerRepository, 
                          ProductRepository productRepository, 
                          CouponRepository couponRepository, 
                          OrderRepository orderRepository)
    {
        _customerRepository = customerRepository;
        _productRepository = productRepository;
        _couponRepository = couponRepository;
        _orderRepository = orderRepository;
    }

    public async Task ValidateOrderAsync(Order order, CancellationToken ct)
    {
        Customer? customer = await _customerRepository.GetByIdAsync(order.CustomerId, ct);

        // Check 1: Customer exists
        if (customer is null)
        {
            await RejectOrderAsync(order, $"Customer #{order.CustomerId} not found", ct); 
            return;
        }

        // Check 2: Customer is active
        if (!customer.IsActive) 
        {
            await RejectOrderAsync(order, "Customer is inactive", ct); 
            return;
        }


        // Check  3 & 4: Product exists and has sufficient stock
        foreach (OrderItem item in order.Items)
        {
            Product? product = await _productRepository.GetByIdAsync(
                item.ProductId, ct);

            if (product is null) 
            {
                await RejectOrderAsync(order, $"Product #{item.ProductId} not found", ct); 
                return; 
            }

            if (product.StockQuantity < item.Quantity)
            {
                await RejectOrderAsync(order, $"Insufficient stock for '{product.Name}'", ct); 
                return;
            }
        }

        // Check 5 & 6: Coupon exists and is not expired
        if (order.CouponCode is not null)
        {
            Coupon? coupon = await _couponRepository.GetByCodeAsync(
                order.CouponCode, ct);

            if (coupon is null) 
            {
                await RejectOrderAsync(order, $"Coupon '{order.CouponCode}' does not exist", ct); 
                return;
            }

            if (coupon.ExpiryDate < DateTime.Today) 
            { 
                await RejectOrderAsync(order, $"Coupon '{order.CouponCode}' has expired", ct);
                return; 
            }
        }

        // All checks passed
        order.Status = OrderStatus.Validated;
        order.FailureReason = null;

        await _orderRepository.SaveAsync(order, ct);


    }

    private async Task RejectOrderAsync(Order order, string reason, CancellationToken ct) 
    { 
        order.Status = OrderStatus.Rejected; 
        order.FailureReason = reason; 
        await _orderRepository.SaveAsync(order, ct); 
    }
}
