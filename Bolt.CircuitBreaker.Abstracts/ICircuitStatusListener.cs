using System.Threading.Tasks;

namespace Bolt.CircuitBreaker.Abstracts
{
    public interface ICircuitStatusListener
    {
        Task Notify(ICircuitRequest request, ICircuitResponse response, ICircuitContext context);
    }
}
