using System.Threading.Tasks;

namespace Authorize.NET
{
    public interface IAuthorizeNetClient
    {
        Task<string> Authorize();
        IPaymentRequest CreatePaymentRequest(long customerId, long orderId, decimal amount);
    }
}