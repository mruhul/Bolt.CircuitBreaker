namespace Bolt.CircuitBreaker.Abstracts
{
    public interface ICircuitContext
    {
        void Set(string name, object value);
        object Get(string name);
    }
}
