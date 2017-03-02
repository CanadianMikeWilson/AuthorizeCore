using System.Threading.Tasks;
using AuthorizeCore.Response;

namespace AuthorizeCore
{
    public interface IPaymentRequest
    {
        void AddCreditCard(string cardNumber, string cardExpiry, string cvv);
        void AddBillingAddress(string firstName, string lastName, string company, string address, string city, string state, string zip, string country);
        void AddShippingAddress(string firstName, string lastName, string company, string address, string city, string state, string zip, string country);
        LineItem AddLineItem(long id, string name, string description, int quantity, decimal unitPrice);
        Task<IPaymentResponse> ProcessPaymentRequest();
    }
}