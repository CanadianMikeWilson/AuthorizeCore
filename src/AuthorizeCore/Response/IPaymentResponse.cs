namespace AuthorizeCore.Response
{
    public interface IPaymentResponse
    {
        string Message { get; }
        string Response { get; }
        string ResponseCode { get; set; }
        bool Success { get; }
        string ErrorCode { get; set; }

        string AuthCode { get; }
        string AvsResultCode { get; }
        string CavvResultCode { get; }
        string TransId { get; }
        string TransHash { get; }
        string AccountNumber { get; }
        string AccountType { get; }
    } 
}