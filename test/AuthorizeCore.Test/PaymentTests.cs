using System.IO;
using System.Threading.Tasks;
using AuthorizeCore.Response;
using Microsoft.Extensions.Configuration;
using Xunit;
using Xunit.Abstractions;

namespace AuthorizeCore.Test
{
    public class PaymentTests
    {
        private readonly IAuthorizeNetClient _client;
        private readonly ITestOutputHelper _output;
        
        private readonly string _expirydate;
        
        public PaymentTests(ITestOutputHelper output)
        {
            _output = output;
            _expirydate = string.Format("{0:MMyy}", System.DateTime.Now.AddYears(3));
            // Set up configuration sources.
            var builder = new ConfigurationBuilder()
                .AddJsonFile(Path.Combine(".", $"config.json"));

            var Configuration = builder.Build();
            _client = new AuthorizeNetClient(Configuration["apiName"], Configuration["apiKey"], false);
        }
        
        [Fact]
        public async Task Payment_ShouldReturnSuccess()
        {
            var paymentRequest = _client.CreatePaymentRequest(77, 555, 1.10m);
            paymentRequest.AddCreditCard("4007000000027", "0517", "123");
            paymentRequest.AddBillingAddress("bfirst", "blast", "bcompany", "baddress", "bcity", "bstate", "bzip", "bcountry");
            paymentRequest.AddShippingAddress("sfirst", "slast", "scompany", "saddress", "sxity", "sstate", "szip", "scountry");
            paymentRequest.AddLineItem(1, "Widget", "Widget", 3, 34.45m);
            paymentRequest.AddLineItem(2, "Bauble", "Bauble", 3, 2.45m);
            paymentRequest.AddLineItem(3, "Thingamajig", "Thingamajig", 3, 12.57m);
            var result = await paymentRequest.ProcessPaymentRequest();
            _output.WriteLine(result.Response);
            _output.WriteLine(result.Message);
            _output.WriteLine(result.ErrorCode);
            Assert.IsType<PaymentSuccess>(result);
            Assert.True(result.Success);
        }
        
        [Fact]
        public async Task Payment_ShouldFailWhenAmountIsZero()
        {
            var paymentRequest = _client.CreatePaymentRequest(77, 555, 0);
            var result = await paymentRequest.ProcessPaymentRequest();
            Assert.IsType<PaymentFailure>(result);
            Assert.Equal("Invalid amount provided", result.Message);
            Assert.False(result.Success);
        }
        
        [Fact]
        public async Task Payment_ShouldFailWhenNoLinesProvided()
        {
            var paymentRequest = _client.CreatePaymentRequest(77, 555, 10);
            var result = await paymentRequest.ProcessPaymentRequest();
            Assert.IsType<PaymentFailure>(result);
            _output.WriteLine(result.Response);
            Assert.Equal("No line items provided.", result.Message);
            Assert.False(result.Success);
        }

        [Fact]
        public async Task Payment_ShouldFailWhenNoCreditCardProvided()
        {
            var paymentRequest = _client.CreatePaymentRequest(77, 555, 10);
            paymentRequest.AddLineItem(1, "name", "description", 5, 5);
            var result = await paymentRequest.ProcessPaymentRequest();
            Assert.IsType<PaymentFailure>(result);
            _output.WriteLine(result.Response);
            Assert.Equal("No credit card number provided.", result.Message);
            Assert.False(result.Success);
        }

        [Fact]
        public async Task Payment_ShouldFailWhenMpExpiryDateProvided()
        {
            var paymentRequest = _client.CreatePaymentRequest(77, 555, 10);
            paymentRequest.AddLineItem(1, "name", "description", 5, 5);
            paymentRequest.AddCreditCard("1234", "", "");
            var result = await paymentRequest.ProcessPaymentRequest();
            Assert.IsType<PaymentFailure>(result);
            _output.WriteLine(result.Response);
            Assert.Equal("No expiration date provided.", result.Message);
            Assert.False(result.Success);
        }

        [Fact]
        public async Task Payment_ShouldFailWhenInvalidExpiryDateProvided()
        {
            var paymentRequest = _client.CreatePaymentRequest(77, 555, 10);
            paymentRequest.AddLineItem(1, "name", "description", 5, 5);
            paymentRequest.AddCreditCard("1234", "444", "");
            var result = await paymentRequest.ProcessPaymentRequest();
            Assert.IsType<PaymentFailure>(result);
            _output.WriteLine(result.Response);
            Assert.Equal("Invalid expiration date provided.", result.Message);
            Assert.False(result.Success);
        }

        [Fact]
        public async Task Payment_ShouldFailWhenNoCvvProvided()
        {
            var paymentRequest = _client.CreatePaymentRequest(77, 555, 10);
            paymentRequest.AddLineItem(1, "name", "description", 5, 5);
            paymentRequest.AddCreditCard("1234", "4444", "");
            var result = await paymentRequest.ProcessPaymentRequest();
            Assert.IsType<PaymentFailure>(result);
            _output.WriteLine(result.Response);
            Assert.Equal("No cvv provided.", result.Message);
            Assert.False(result.Success);
        }

        [Fact]
        public async Task Payment_ShouldFailWhenShortCvvProvided()
        {
            var paymentRequest = _client.CreatePaymentRequest(77, 555, 10);
            paymentRequest.AddLineItem(1, "name", "description", 5, 5);
            paymentRequest.AddCreditCard("1234", "4444", "55");
            var result = await paymentRequest.ProcessPaymentRequest();
            Assert.IsType<PaymentFailure>(result);
            _output.WriteLine(result.Response);
            Assert.Equal("Invalid cvv provided.", result.Message);
            Assert.False(result.Success);
        }

        [Fact]
        public async Task Payment_ShouldFailWhenLongCvvProvided()
        {
            var paymentRequest = _client.CreatePaymentRequest(77, 555, 10);
            paymentRequest.AddLineItem(1, "name", "description", 5, 5);
            paymentRequest.AddCreditCard("1234", "4444", "55555");
            var result = await paymentRequest.ProcessPaymentRequest();
            Assert.IsType<PaymentFailure>(result);
            _output.WriteLine(result.Response);
            Assert.Equal("Invalid cvv provided.", result.Message);
            Assert.False(result.Success);
        }

        [Fact]
        public async Task Payment_ShouldCorrectlyHandleAvsMispatch()
        {
            var paymentRequest = _client.CreatePaymentRequest(77, 555, 27);
            paymentRequest.AddLineItem(1, "name", "description", 5, 5);
            paymentRequest.AddCreditCard("4222222222222", _expirydate, "555");
            paymentRequest.AddBillingAddress("f","l","c","a","c","s","46282","c");
            paymentRequest.AddShippingAddress("f","l","c","a","c","s","46282","c");
            var result = await paymentRequest.ProcessPaymentRequest();
            _output.WriteLine(result.Response);
            Assert.IsType<PaymentFailure>(result);
            Assert.Equal(result.ResponseCode, "2");
            Assert.Equal(result.ErrorCode, "27");
            Assert.False(result.Success);
        }

        [Fact]
        public async Task Payment_ShouldCorrectlyHandleErrorDuringProcessing()
        {
            var paymentRequest = _client.CreatePaymentRequest(77, 555, 26);
            paymentRequest.AddLineItem(1, "name", "description", 5, 5);
            paymentRequest.AddCreditCard("4222222222222", _expirydate, "555");
            paymentRequest.AddBillingAddress("f","l","c","a","c","s","46282","c");
            paymentRequest.AddShippingAddress("f","l","c","a","c","s","46282","c");
            var result = await paymentRequest.ProcessPaymentRequest();
            _output.WriteLine(result.Response);
            Assert.IsType<PaymentFailure>(result);
            Assert.Equal(result.ResponseCode, "3");
            Assert.Equal(result.ErrorCode, "26");
            Assert.False(result.Success);
        }

        [Fact]
        public async Task Payment_ShouldCorrectlyHandleDuplicateTransactionError()
        {
            var paymentRequest = _client.CreatePaymentRequest(77, 555, 11);
            paymentRequest.AddLineItem(1, "name", "description", 5, 5);
            paymentRequest.AddCreditCard("4222222222222", _expirydate, "555");
            paymentRequest.AddBillingAddress("f","l","c","a","c","s","46282","c");
            paymentRequest.AddShippingAddress("f","l","c","a","c","s","46282","c");
            var result = await paymentRequest.ProcessPaymentRequest();
            _output.WriteLine(result.Response);
            Assert.IsType<PaymentFailure>(result);
            Assert.Equal(result.ResponseCode, "3");
            Assert.Equal(result.ErrorCode, "11");
            Assert.False(result.Success);
        }

        // TODO: ?? Should this throw a different error, to be handled by client?
        [Fact]
        public async Task Payment_ShouldCorrectlyHandleMerchantDoesntAcceptCard()
        {
            var paymentRequest = _client.CreatePaymentRequest(77, 555, 17);
            paymentRequest.AddLineItem(1, "name", "description", 5, 5);
            paymentRequest.AddCreditCard("4222222222222", _expirydate, "555");
            paymentRequest.AddBillingAddress("f","l","c","a","c","s","46282","c");
            paymentRequest.AddShippingAddress("f","l","c","a","c","s","46282","c");
            var result = await paymentRequest.ProcessPaymentRequest();
            _output.WriteLine(result.Response);
            Assert.IsType<PaymentFailure>(result);
            Assert.Equal(result.ResponseCode, "3");
            Assert.Equal(result.ErrorCode, "17");
            Assert.False(result.Success);
        }
    }
}