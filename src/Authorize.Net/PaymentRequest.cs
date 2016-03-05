using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Authorize.NET
{
    public class PaymentRequest : IPaymentRequest
    {
        private AuthorizeNetClient _client;
        
        public decimal Amount { get; set; }
        public long CustomerId { get; set; }
        public CreditCard CreditCard { get; set; }
        public ICollection<LineItem> LineItems { get; set; }
        public string OrderDescription { get; set; }
        public string RefId { get; set; }
        public string BillingFirstName { get; set; }
        public string BillingLastName { get; set; }
        public string BillingCompany { get; set; }
        public string BillingAddress { get; set; }
        public string BillingCity { get; set; }
        public string BillingState { get; set; }
        public string BillingZip { get; set; }
        public string BillingCountry { get; set; }
        public string ShippingFirstName { get; set; }
        public string ShippingLastName { get; set; }
        public string ShippingCompany { get; set; }
        public string ShippingAddress { get; set; }
        public string ShippingCity { get; set; }
        public string ShippingState { get; set; }
        public string ShippingZip { get; set; }
        public string ShippingCountry { get; set; }        
        public PaymentRequest(AuthorizeNetClient client)
        {
            _client = client;
            CreditCard = new CreditCard();
            LineItems = new List<LineItem>();
        }
        
        public void AddCreditCard(string cardNumber, string cardExpiry, string cvv)
        {
            CreditCard = new CreditCard {
                CardNumber = cardNumber,
                ExpirationDate = cardExpiry,
                Cvv = cvv
            };
        }
        
        public void AddBillingAddress(string firstName, string lastName, string company, string address, string city, string state, string zip, string country)
        {
            BillingFirstName = firstName;
            BillingLastName = lastName;
            BillingCompany = company;
            BillingAddress = address;
            BillingCity = city;
            BillingState = state;
            BillingZip = zip;
            BillingCountry = country;
        }

        public void AddShippingAddress(string firstName, string lastName, string company, string address, string city, string state, string zip, string country)
        {
            ShippingFirstName = firstName;
            ShippingLastName = lastName;
            ShippingCompany = company;
            ShippingAddress = address;
            ShippingCity = city;
            ShippingState = state;
            ShippingZip = zip;
            ShippingCountry = country;
        }
        
        public void AddLineItem(long id, string name, string description, int quantity, decimal unitPrice)
        {
            var lineItem = new LineItem {
                Id = id,
                Name = name,
                Description = description,
                Quantity = quantity,
                UnitPrice = unitPrice
            };
            LineItems.Add(lineItem);
        }

        public async Task<string> ProcessPaymentRequest()
        {
            return await _client.ChargeCard(this);
        }
    }
}