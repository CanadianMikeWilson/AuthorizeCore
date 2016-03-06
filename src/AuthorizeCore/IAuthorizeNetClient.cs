using System.Threading.Tasks;
using AuthorizeCore.Response;

namespace AuthorizeCore
{
    public interface IAuthorizeNetClient
    {
        Task<IPaymentResponse> Authorize();
        IPaymentRequest CreatePaymentRequest(long customerId, long orderId, decimal amount);
    }
}