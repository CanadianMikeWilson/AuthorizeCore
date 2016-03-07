namespace AuthorizeCore.Response
{
    public abstract class PaymentResponse : IPaymentResponse
    {
        public PaymentResponse()
        {
        }
        public PaymentResponse(string message)
        {
            this.Message = message;
        }
        
        public PaymentResponse(string message, string response)
        {
            this.Message = message;
            this.Response = response;
        }
        
        public string Message { get; set; }
        public string Response { get; set; }
        public virtual bool Success { get; }
        public string ResponseCode { get; set; }
        public string ErrorCode { get; set; }
        public string AuthCode { get; set; }
        public string AvsResultCode { get; set; }
        public string CavvResultCode { get; set; }
        public string TransId { get; set; }
        public string TransHash { get; set; }
        public string AccountNumber { get; set; }
        public string AccountType { get; set; }
    }
}