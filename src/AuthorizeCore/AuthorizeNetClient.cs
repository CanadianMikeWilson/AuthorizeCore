using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml;
using AuthorizeCore.Response;

namespace AuthorizeCore
{
    internal static class XmlDocumentExtensions
    {
        public static string SaveToString(this XmlDocument xmlDoc)
        {
            using (var stringWriter = new StringWriter()) {
                using (var xmlTextWriter = XmlWriter.Create(stringWriter)) {
                    xmlDoc.WriteTo(xmlTextWriter);
                    xmlTextWriter.Flush();
                    return stringWriter.GetStringBuilder().ToString();
                }
            }
        }
        
        public static XmlElement CreateTextElement(this XmlDocument xmlDoc, string name, string ns, string text)
        {
            var element = xmlDoc.CreateElement(name, ns);
            element.InnerText = text;
            return element;
        }
    }
    public class AuthorizeNetClient : IAuthorizeNetClient
    {
        private const string baseUrlLive = "https://api.authorize.net";
        private const string baseUrlTest = "https://apitest.authorize.net";
        private readonly string baseUrl;
        private const string xmlns = "AnetApi/xml/v1/schema/AnetApiSchema.xsd";

        private readonly string _apiName;
        private readonly string _apiKey;
        public AuthorizeNetClient(string apiName, string apiKey, bool liveMode)
        {
            _apiName = apiName;
            _apiKey = apiKey;
            baseUrl = (liveMode ? baseUrlLive : baseUrlTest);
        }
        
        public async Task<IPaymentResponse> Authorize()
        {
            var xmlDoc = new XmlDocument();
            var rootNode = xmlDoc.CreateElement("authenticateTestRequest", xmlns);
            xmlDoc.AppendChild(rootNode);
            
            rootNode.AppendChild(CreateMerchantAuthentication(xmlDoc));
            
            using (var request = new HttpClient()) {
                using ( var response = await request.PostAsync(new Uri(baseUrl + "/xml/v1/request.api"), new StringContent(xmlDoc.SaveToString()))) {
                    var xmlString = await response.Content.ReadAsStringAsync();
                    return ParseAuthorizeResponse(xmlString);
                }
            }
        }
        
        private XmlElement CreateMerchantAuthentication(XmlDocument xmlDoc)
        {
            var merchantAuthentication = xmlDoc.CreateElement("merchantAuthentication", xmlns);
            
            var name = xmlDoc.CreateElement("name", xmlns);
            name.InnerText = _apiName;
            merchantAuthentication.AppendChild(name);
            
            var transactionKey = xmlDoc.CreateElement("transactionKey", xmlns);
            transactionKey.InnerText = _apiKey;
            merchantAuthentication.AppendChild(transactionKey);
            
            return merchantAuthentication;
        }
        
        private XmlElement CreatePaymentElement(XmlDocument xmlDoc, CreditCard creditCard)
        {
            var payment = xmlDoc.CreateElement("payment", xmlns);
            
            var creditCardElement = xmlDoc.CreateElement("creditCard", xmlns);
            
            var cardNumber = xmlDoc.CreateElement("cardNumber", xmlns);
            cardNumber.InnerText = creditCard.CardNumber;
            creditCardElement.AppendChild(cardNumber);
            
            var expirationDate = xmlDoc.CreateElement("expirationDate", xmlns);
            expirationDate.InnerText = creditCard.ExpirationDate;
            creditCardElement.AppendChild(expirationDate);
            
            var cardCode = xmlDoc.CreateElement("cardCode", xmlns);
            cardCode.InnerText = creditCard.Cvv;
            creditCardElement.AppendChild(cardCode);
            
            payment.AppendChild(creditCardElement);
            return payment;
        }

        public async Task<IPaymentResponse> ChargeCard(PaymentRequest paymentRequest)
        {
            if ( paymentRequest.Amount <= 0 ) return new PaymentFailure("Invalid amount provided", string.Empty);
            if ( paymentRequest.LineItems.Count == 0 ) return new PaymentFailure("No line items provided.", string.Empty);
            if ( string.IsNullOrEmpty(paymentRequest.CreditCard.CardNumber ) ) return new PaymentFailure("No credit card number provided.", string.Empty);
            if ( string.IsNullOrEmpty(paymentRequest.CreditCard.ExpirationDate ) ) return new PaymentFailure("No expiration date provided.", string.Empty);
            if ( paymentRequest.CreditCard.ExpirationDate.Length < 4 ) return new PaymentFailure("Invalid expiration date provided.", string.Empty);
            if ( string.IsNullOrEmpty(paymentRequest.CreditCard.Cvv ) ) return new PaymentFailure("No cvv provided.", string.Empty);
            if ( paymentRequest.CreditCard.Cvv.Length < 3 ) return new PaymentFailure("Invalid cvv provided.", string.Empty);
            if ( paymentRequest.CreditCard.Cvv.Length > 4 ) return new PaymentFailure("Invalid cvv provided.", string.Empty);
            
            var xmlDoc = new XmlDocument();
            var rootNode = xmlDoc.CreateElement("createTransactionRequest", xmlns);
            xmlDoc.AppendChild(rootNode);
            
            rootNode.AppendChild(CreateMerchantAuthentication(xmlDoc));
            
            var refId = xmlDoc.CreateElement("refId", xmlns);
            refId.InnerText = paymentRequest.RefId;
            
            var transactionRequest = xmlDoc.CreateElement("transactionRequest", xmlns);
            
            var transactionType = xmlDoc.CreateElement("transactionType", xmlns);
            transactionType.InnerText = "authCaptureTransaction";
            transactionRequest.AppendChild(transactionType);
            
            var amount = xmlDoc.CreateElement("amount", xmlns);
            amount.InnerText = string.Format("{0:0.00}", paymentRequest.Amount);
            transactionRequest.AppendChild(amount);
            
            transactionRequest.AppendChild(CreatePaymentElement(xmlDoc, paymentRequest.CreditCard));
            
            var order = xmlDoc.CreateElement("order", xmlns);
            var invoiceNumber = xmlDoc.CreateElement("invoiceNumber", xmlns);
            invoiceNumber.InnerText = paymentRequest.RefId;
            order.AppendChild(invoiceNumber);
            var orderDescription = xmlDoc.CreateElement("description", xmlns);
            orderDescription.InnerText = paymentRequest.OrderDescription;
            order.AppendChild(orderDescription);
            transactionRequest.AppendChild(order);
            
            transactionRequest.AppendChild(CreatePaymentLineItems(xmlDoc, paymentRequest));
            
            var customer = xmlDoc.CreateElement("customer", xmlns);
            customer.AppendChild(xmlDoc.CreateTextElement("id", xmlns, paymentRequest.CustomerId.ToString()));
            transactionRequest.AppendChild(customer);
            
            transactionRequest.AppendChild(CreateAddressElement(xmlDoc, "billTo",
                paymentRequest.BillingFirstName, paymentRequest.BillingLastName, paymentRequest.BillingCompany,
                paymentRequest.BillingAddress, paymentRequest.BillingCity, paymentRequest.BillingState,
                paymentRequest.BillingZip, paymentRequest.BillingCountry));
            transactionRequest.AppendChild(CreateAddressElement(xmlDoc, "shipTo",
                paymentRequest.ShippingFirstName, paymentRequest.ShippingLastName, paymentRequest.ShippingCompany,
                paymentRequest.ShippingAddress, paymentRequest.ShippingCity, paymentRequest.ShippingState,
                paymentRequest.ShippingZip, paymentRequest.ShippingCountry));
            /**
<createTransactionRequest xmlns="AnetApi/xml/v1/schema/AnetApiSchema.xsd">
  <transactionRequest>
    <transactionSettings>
      <setting>
        <settingName>testRequest</settingName>
        <settingValue>false</settingValue>
      </setting>
    </transactionSettings>
    <userFields>
      <userField>
        <name>MerchantDefinedFieldName1</name>
        <value>MerchantDefinedFieldValue1</value>
      </userField>
      <userField>
        <name>favorite_color</name>
        <value>blue</value>
      </userField>
    </userFields>
  </transactionRequest>
</createTransactionRequest>            */
            rootNode.AppendChild(transactionRequest);
            using (var request = new HttpClient()) {
                using ( var response = await request.PostAsync(new Uri(baseUrl + "/xml/v1/request.api"), new StringContent(xmlDoc.SaveToString()))) {
                    var xmlString = await response.Content.ReadAsStringAsync();
                    return ParsePaymentResponse(xmlString);
                }
            }
        }

        private XmlNode CreateAddressElement(XmlDocument xmlDoc, string elementName, string firstName, string lastName, string company, string address, string city, string state, string zip, string country)
        {
            var element = xmlDoc.CreateElement(elementName, xmlns);
            element.AppendChild(xmlDoc.CreateTextElement("firstName", xmlns, firstName));
            element.AppendChild(xmlDoc.CreateTextElement("lastName", xmlns, lastName));
            element.AppendChild(xmlDoc.CreateTextElement("company", xmlns, company));
            element.AppendChild(xmlDoc.CreateTextElement("address", xmlns, address));
            element.AppendChild(xmlDoc.CreateTextElement("city", xmlns, city));
            element.AppendChild(xmlDoc.CreateTextElement("state", xmlns, state));
            element.AppendChild(xmlDoc.CreateTextElement("zip", xmlns, zip));
            element.AppendChild(xmlDoc.CreateTextElement("country", xmlns, country));
            return element;
        }

        private XmlNode CreatePaymentLineItems(XmlDocument xmlDoc, PaymentRequest paymentRequest)
        {
            var lineItems = xmlDoc.CreateElement("lineItems", xmlns);
            foreach ( var line in paymentRequest.LineItems ) {
                var lineItem = xmlDoc.CreateElement("lineItem", xmlns);
                lineItem.AppendChild(xmlDoc.CreateTextElement("itemId", xmlns, line.Id.ToString()));
                lineItem.AppendChild(xmlDoc.CreateTextElement("name", xmlns, line.Name));
                lineItem.AppendChild(xmlDoc.CreateTextElement("description", xmlns, line.Description));
                lineItem.AppendChild(xmlDoc.CreateTextElement("quantity", xmlns, line.Quantity.ToString()));
                lineItem.AppendChild(xmlDoc.CreateTextElement("unitPrice", xmlns, string.Format("{0:0.00}", line.UnitPrice)));
                lineItems.AppendChild(lineItem);
            }
            return lineItems;
        }

        public IPaymentRequest CreatePaymentRequest(long customerId, long orderId, decimal amount)
        {
            var paymentRequest = new PaymentRequest(this);
            paymentRequest.Amount = amount;
            paymentRequest.RefId = orderId.ToString();
            paymentRequest.CustomerId = customerId;
            return paymentRequest;
        }
        
        private IPaymentResponse ParseAuthorizeResponse(string xmlString)
        {
            var responseDoc = new XmlDocument();
            responseDoc.LoadXml(xmlString);
            var resultCode = responseDoc.GetElementsByTagName("resultCode");
            if (resultCode.Count > 0 ) {
                var resultCodeStr = resultCode.Item(0).InnerText;
                if ( resultCodeStr.Equals("Ok")) {
                    return new AuthorizationSuccess(resultCodeStr, xmlString);
                } else {
                    var messageElement = responseDoc.GetElementsByTagName("message");
                    var it = messageElement.GetEnumerator();
                    for ( var i = 0; i < messageElement.Count; i++ ) {
                        var el = messageElement.Item(i);
                        if ( el.Name.Equals("text")) {
                            return new AuthorizationFailure(el.InnerText, xmlString);
                        }
                    }
                }
            }
            return new AuthorizationFailure("Unable to Parse XML", xmlString);
        }

        private IPaymentResponse ParsePaymentTransactinoResponse(string xmlString, XmlNode transactionResponse)
        {
            string responseCode = "";
            string errorText = "";
            string errorCode = "";
            var authCode = "";
            var avsResultCode = "";
            var cavvResultCode = "";
            var transId = "";
            var transHash = "";
            var accountNumber = "";
            var accountType = "";
            for ( var idx = 0; idx < transactionResponse.ChildNodes.Count; idx++ ) {
                var el = transactionResponse.ChildNodes[idx];
                if ( el.Name.Equals("responseCode")) {
                    responseCode = el.InnerText;
                }
                if ( el.Name.Equals("errors") ) {
                    for ( var errorIdx=0; errorIdx < el.ChildNodes.Count; errorIdx++ ) {
                        var error = el.ChildNodes[errorIdx];
                        for ( var eidx=0; eidx < error.ChildNodes.Count; eidx++ ) {
                            var errorEl = error.ChildNodes[eidx];
                            if ( errorEl.Name.Equals("errorCode")) {
                                errorCode = errorEl.InnerText; 
                            }
                            if ( errorEl.Name.Equals("errorText")) {
                                errorText = errorEl.InnerText;
                            }
                        }
                    }
                }
                if ( el.Name.Equals("authCode")) {
                    authCode = el.InnerText;
                }
                if ( el.Name.Equals("avsResultCode")) {
                    avsResultCode = el.InnerText;
                }
                if ( el.Name.Equals("cavvResultCode")) {
                    cavvResultCode = el.InnerText;
                }
                if ( el.Name.Equals("transId")) {
                    transId = el.InnerText;
                }
                if ( el.Name.Equals("transHash")) {
                    transHash = el.InnerText;
                }
                if ( el.Name.Equals("accountNumber")) {
                    accountNumber = el.InnerText;
                }
                if ( el.Name.Equals("accountType")) {
                    accountType = el.InnerText;
                }
            }
            
            PaymentResponse paymentResponse = null;
            if ( responseCode.Equals("1") ) {
                paymentResponse = new PaymentSuccess("This transaction has been approved.", xmlString) {
                    ResponseCode = responseCode,
                    ErrorCode = errorCode
                };
            } else if ( !string.IsNullOrEmpty(errorText)) {
                paymentResponse = new PaymentFailure(errorText, xmlString);
                paymentResponse.ResponseCode = responseCode;
                paymentResponse.ErrorCode = errorCode;
            }
            
            if ( null != paymentResponse ) {
                paymentResponse.AuthCode = authCode;
                paymentResponse.AvsResultCode = avsResultCode;
                paymentResponse.CavvResultCode = cavvResultCode;
                paymentResponse.TransId = transId;
                paymentResponse.TransHash = transHash;
                paymentResponse.AccountNumber = accountNumber;
                paymentResponse.AccountType = accountType;                    
                return paymentResponse;
            }
            return new PaymentFailure("Unable to Parse XML as TransactionResponse", xmlString);
        }

        private IPaymentResponse ParseTransactionErrorResponse(string xmlString)
        {
            var responseDoc = new XmlDocument();
            responseDoc.LoadXml(xmlString);
            var messageElement = responseDoc.GetElementsByTagName("message");
            if ( messageElement.Count > 0 ) {
                var message = messageElement.Item(0);
                for ( var i = 0; i < message.ChildNodes.Count; i++ ) {
                    var el = message.ChildNodes[i];
                    if ( el.Name.Equals("text")) {
                        return new PaymentFailure(el.InnerText, xmlString);
                    }
                }
            }
            return new PaymentFailure("Unable to Parse Payment XML", xmlString);
        }
        
        private IPaymentResponse ParsePaymentResponse(string xmlString)
        {
            var responseDoc = new XmlDocument();
            responseDoc.LoadXml(xmlString);
            var transactionResponseList = responseDoc.GetElementsByTagName("transactionResponse");
            if (transactionResponseList.Count > 0 ) {
                var transactionResponse = transactionResponseList.Item(0);
                return ParsePaymentTransactinoResponse(xmlString, transactionResponse);
            }

            return ParseTransactionErrorResponse(xmlString);
        }
    }
}