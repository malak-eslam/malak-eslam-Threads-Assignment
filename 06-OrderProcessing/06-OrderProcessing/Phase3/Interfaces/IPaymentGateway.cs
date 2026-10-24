using _06_OrderProcessing.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _06_OrderProcessing.Phase3.Interfaces;
public interface IPaymentGateway
{
    Task<PaymentResult> ProcessPaymentAsync(int orderId,decimal amount , CancellationToken ct);
    Task RefundAsync(string transactionId);
}
