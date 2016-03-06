namespace AuthorizeCore.Response
{
    public interface IPaymentResponse
    {
        string Message { get; }
        string Response { get; }
        string ResponseCode { get; set; }
        bool Success { get; }
        string ErrorCode { get; set; }
    }
}