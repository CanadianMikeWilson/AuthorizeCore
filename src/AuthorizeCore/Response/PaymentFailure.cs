namespace AuthorizeCore.Response
{
    public class PaymentFailure : PaymentResponse
    {
        public PaymentFailure(string message, string response)
            : base(message, response)
        {
        }
        public override bool Success { get { return false; } }
    }
}