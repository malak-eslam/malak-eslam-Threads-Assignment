using _06_OrderProcessing.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _06_OrderProcessing.Phase2;
public class OrderPricer
{
    private readonly PricingService _pricingService;

    public OrderPricer(PricingService pricingService)
    {
        _pricingService = pricingService;
    }

    public async Task PriceOrderAsync(Order order,CancellationToken ct)
    {
        decimal subTotal = await _pricingService.CalculateSubTotalAsync(order, ct);

        Task<decimal> discountTask = _pricingService.GetDiscountAsync(order, ct);
        
        Task<decimal> shippingTask =  _pricingService.CalculateShippingCostAsync(order, ct);

       await Task.WhenAll(discountTask, shippingTask);

        decimal discount = await discountTask;
        decimal shipping = await shippingTask;

        order.TotalAmount = Math.Round(subTotal - discount + shipping, 2, MidpointRounding.AwayFromZero);
    }

    public async Task PriceOrderSequentialAsync(Order order,CancellationToken ct)
    {
        decimal subTotal = await _pricingService.CalculateSubTotalAsync(order, ct);

        decimal discount = await _pricingService.GetDiscountAsync(order, ct);

        decimal shipping = await _pricingService.CalculateShippingCostAsync(order, ct);

        order.TotalAmount = Math.Round( subTotal - discount + shipping, 2, MidpointRounding.AwayFromZero);
    }



}
