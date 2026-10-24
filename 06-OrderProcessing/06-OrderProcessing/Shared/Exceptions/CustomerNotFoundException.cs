using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _06_OrderProcessing.Shared.Exceptions;
public class CustomerNotFoundException : Exception
{
    
    public CustomerNotFoundException(string message) : base(message)
    {

    }
}
