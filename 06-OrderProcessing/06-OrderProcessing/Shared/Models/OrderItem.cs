using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _06_OrderProcessing.Shared.Models;
public class OrderItem 
{ 
    public int ProductId { get; set; } 
    public int Quantity { get; set; } 
    public decimal UnitPrice { get; set; }
}
