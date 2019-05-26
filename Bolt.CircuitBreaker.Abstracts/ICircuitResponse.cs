namespace Bolt.CircuitBreaker.Abstracts
{
    public interface ICircuitResponse
    {
        bool IsSucceed { get; }
        CircuitStatus Status { get; set; }
    }

    public interface ICircuitResponse<T> : ICircuitResponse
    {
        T Value { get; }
    }
}
