namespace AuthorizeCore.Response
{
    public class AuthorizationFailure : PaymentResponse
    {
        public AuthorizationFailure(string message, string response)
            : base(message, response)
        {
            
        }
        public override bool Success { get { return false; } }
    }
}