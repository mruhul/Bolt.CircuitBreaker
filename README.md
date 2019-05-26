# Bolt.CircuitBreaker
A wrapper around polly with reporting

# how to setup this lib to use in my application 

Add `Bolt.CircuitBreaker.PollyImpl` nuget package in your application. In your startup class inside configure services as below:

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddPollyCircuitBreaker();
    }

By default no logging is not enabled. To enable log by this library you need to Initializer a logger used by this lib. Example below:

    public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
    {
        CircuitBreakerLog.Init(loggerFactory);
    }

# How to use the circuitbreaker

    public class BooksApiProxy
    {
        private readonly ICircuitBreaker _cb;

        public BooksApiProxy(ICircuitBreaker cb)
        {
            _cb = cb;
        }

        public IEnumerable<Books> GetAll()
        {
            var cbInput = new CircuitRequest
            {
                CircuitKey = "api-books:get"
            }

            var response = _cb.ExecuteAsync(cbInput, cxt => {
                // call http
                // return IEnumerable<Book>
            });

            return response.Value;
        }
    }