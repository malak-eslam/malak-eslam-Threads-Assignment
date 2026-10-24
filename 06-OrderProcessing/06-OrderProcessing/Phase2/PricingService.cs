using _06_OrderProcessing.Phase1.Async;
using _06_OrderProcessing.Shared.AppLogger;
using _06_OrderProcessing.Shared.Exceptions;
using _06_OrderProcessing.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _06_OrderProcessing.Phase2;
public class PricingService
{
    private readonly ProductRepository _productRepository;
    private readonly CustomerRepository _customerRepository;
    private readonly CouponRepository _couponRepository;
    private readonly HashSet<int> _failShippingForOrderIds;

    public PricingService(ProductRepository productRepository, CustomerRepository customerRepository, CouponRepository couponRepository, HashSet<int> failShippingForOrderIds)
    {
        _productRepository = productRepository;
        _customerRepository = customerRepository;
        _couponRepository = couponRepository;
        _failShippingForOrderIds = failShippingForOrderIds;
    }

    public async Task<decimal> CalculateSubTotalAsync(Order order, CancellationToken ct)
    {
        decimal SubTotal = 0m;

        foreach (OrderItem item in order.Items)
        {
            Product? product = await _productRepository.GetByIdAsync(item.ProductId, ct);

            if (product == null)
                throw new InvalidOperationException($"Product #{item.ProductId} not found.");


            item.UnitPrice = product.Price;
            decimal itemTotal = product.Price * item.Quantity;

            SubTotal += itemTotal;
        }
        order.SubTotal = Math.Round(SubTotal, 2, MidpointRounding.AwayFromZero);

        return order.SubTotal;
    }

    public async Task<decimal> GetDiscountAsync(Order order, CancellationToken ct)
    {
        Customer? customer = await _customerRepository.GetByIdAsync(order.CustomerId, ct);

        if (customer == null)
            throw new InvalidOperationException($"Customer #{order.CustomerId} not found.");

        int loyaltyDiscount = customer.LoyaltyTier switch
        {
            LoyaltyTier.Bronze => 0,
            LoyaltyTier.Silver => 5,
            LoyaltyTier.Gold => 10,
            _ => 0
        };

        int couponDiscount = 0;

        if (!string.IsNullOrWhiteSpace(order.CouponCode))
        {
            Coupon? coupon = await _couponRepository.GetByCodeAsync(order.CouponCode, ct);

            if (coupon is null)
                throw new InvalidCouponException(
                    $"Coupon '{order.CouponCode}' not found.");

            if (coupon.ExpiryDate.Date < DateTime.Today)
                throw new InvalidCouponException(
                    $"Coupon '{order.CouponCode}' has expired.");

            couponDiscount = coupon.DiscountPercent;

        }

        int totalDiscountPercent =
             Math.Min(loyaltyDiscount + couponDiscount, 30);

        order.Discount =
            Math.Round(order.SubTotal * totalDiscountPercent / 100m, 2, MidpointRounding.AwayFromZero);

        return order.Discount;

    }

    public async Task<decimal> CalculateShippingCostAsync(Order order, CancellationToken ct)
    {
        Log.Info("PricingService", "Starting shipping provider handshake...");
        await Task.Delay(150, ct);

        if (_failShippingForOrderIds.Contains(order.Id))
            throw new ShippingProviderUnavailableException("Shipping provider unavailable");

        Log.Info("PricingService", "Sending shipping request...");
        await Task.Delay(300, ct);

        Log.Info("PricingService", "Waiting for shipping response...");
        await Task.Delay(Random.Shared.Next(500, 1501), ct);

        decimal shippingCost = 0m;

        if (order.SubTotal < 1000m)
        {
            int totalQuantity = 0;
            decimal totalWeight = 0m;

            foreach (OrderItem item in order.Items)
            {
                Product? product = await _productRepository.GetByIdAsync(item.ProductId, ct);

                if (product is null)
                    throw new InvalidOperationException($"Product #{item.ProductId} not found.");

                totalQuantity += item.Quantity;
                totalWeight += product.WeightKg * item.Quantity;
            }

            shippingCost = 5m + 1.50m * totalQuantity + 2m * totalWeight;
        }
        order.ShippingCost = Math.Round(shippingCost, 2, MidpointRounding.AwayFromZero);

        return order.ShippingCost;
    }

    public async Task<decimal> CalculateTotalAsync(Order order, CancellationToken ct)
    {
         await CalculateSubTotalAsync(order, ct);

        Task<decimal> discountTask = GetDiscountAsync(order, ct);
        Task<decimal> shippingTask = CalculateShippingCostAsync(order, ct);

        decimal discount = await discountTask;
        decimal shippingCost = await shippingTask;

        order.Discount = discount;
        order.ShippingCost = shippingCost;

        order.TotalAmount = Math.Round(order.SubTotal - order.Discount + order.ShippingCost, 2,MidpointRounding.AwayFromZero);

        return order.TotalAmount;
    }
}