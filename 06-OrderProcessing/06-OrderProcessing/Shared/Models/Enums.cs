using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _06_OrderProcessing.Shared.Models;
public enum LoyaltyTier 
{ 
    Bronze,
    Silver,
    Gold 
}
public enum OrderStatus 
{ 
    Pending,
    Validated, 
    Rejected, 
    PaymentFailed, 
    Completed, 
    Failed }
public enum PaymentStatus 
{
    Captured, 
    Declined, 
    Refunded 
}
