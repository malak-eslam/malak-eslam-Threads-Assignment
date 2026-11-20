using _06_OrderProcessing.Phase1.Async;
using _06_OrderProcessing.Shared.Exceptions;
using _06_OrderProcessing.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _06_OrderProcessing.Phase3.Services;
public class CustomerService
{
    private readonly CustomerRepository _customerRepository;

    public CustomerService(CustomerRepository customerRepository)
    {
        _customerRepository = customerRepository;
    }

    public async Task<Customer> GetCustomerAsync(int id , CancellationToken ct)
    {
       Customer? customer = await _customerRepository.GetByIdAsync(id , ct);

        if(customer == null)
        {
            throw new CustomerNotFoundException(
         $"Customer #{id} not found");
        }

        return customer;
    }
}
