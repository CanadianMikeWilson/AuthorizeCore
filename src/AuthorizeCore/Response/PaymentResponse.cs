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
    }
}