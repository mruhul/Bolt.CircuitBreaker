namespace Bolt.CircuitBreaker.Abstracts
{
    public enum CircuitStatus
    {
        Unknown,
        Succeed,
        Failed,
        Broken,
        Timeout
    }
}
