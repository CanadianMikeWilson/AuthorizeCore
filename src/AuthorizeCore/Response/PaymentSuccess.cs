namespace AuthorizeCore.Response
{
    public class PaymentSuccess : PaymentResponse
    {
        public PaymentSuccess (string message, string response)
            : base(message, response)
        {
        }
        public override bool Success { get { return true; } }
    }
}