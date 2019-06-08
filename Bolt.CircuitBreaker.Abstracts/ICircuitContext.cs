using System.Collections.Generic;

namespace Bolt.CircuitBreaker.Abstracts
{
    public interface ICircuitContext
    {
        void Set(string name, object value);
        object Get(string name);
        bool Remove(string name);
    }

    public class CircuitContext : ICircuitContext
    {
        private Dictionary<string, object> _store;

        public object Get(string name)
        {
            if (_store == null) return null;
            return _store.TryGetValue(name, out var result) ? result : null;
        }

        public void Set(string name, object value)
        {
            if (_store == null) _store = new Dictionary<string, object>();
            _store[name] = value;
        }

        public bool Remove(string name)
        {
            if (_store == null) return false;

            return _store.Remove(name);
        }
    }

    public static class CircuitContextExtensions
    {
        private const string KeyAppName = "__cb:appname";
        private const string KeyServiceName = "__cb:servicename";

        public static T Get<T>(this ICircuitContext context, string name)
        {
            var result = context.Get(name);
            return result == null ? default(T) : (T)result;
        }

        public static void SetAppName(this ICircuitContext context, string appName)
        {
            context.Set(KeyAppName, appName);
        }

        public static string GetAppName(this ICircuitContext context)
        {
            return context.Get<string>(KeyAppName);
        }

        public static void SetServiceName(this ICircuitContext context, string serviceName)
        {
            context.Set(KeyServiceName, serviceName);
        }

        public static string GetServiceName(this ICircuitContext context)
        {
            return context.Get<string>(KeyServiceName);
        }
    }
}
