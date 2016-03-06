namespace AuthorizeCore.Response
{
    public class AuthorizationSuccess : PaymentResponse
    {
        public AuthorizationSuccess(string message, string response)
            : base(message, response)
        {
            
        }
        public override bool Success { get { return true; } }
    }
}