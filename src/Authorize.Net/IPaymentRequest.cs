using System.Collections.Generic;
using System.Threading.Tasks;

namespace Authorize.NET
{
    public interface IPaymentRequest
    {
        void AddCreditCard(string cardNumber, string cardExpiry, string cvv);
        void AddBillingAddress(string firstName, string lastName, string company, string address, string city, string state, string zip, string country);
        void AddShippingAddress(string firstName, string lastName, string company, string address, string city, string state, string zip, string country);
        void AddLineItem(long id, string name, string description, int quantity, decimal unitPrice);
        Task<string> ProcessPaymentRequest();
    }
}