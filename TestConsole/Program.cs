using Bolt.CircuitBreaker.Abstracts;
using Bolt.CircuitBreaker.PollyImpl;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Bolt.CircuitBreaker.Listeners.Redis;
using System.Diagnostics;

namespace TestConsole
{
    class Program
    {
        private static IServiceProvider ConfigureServices()
        {
            var sc = new ServiceCollection();
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            var configuration = builder.Build();
            sc.AddLogging(configure => configure.AddConsole())
                .Configure<LoggerFilterOptions>(opt => opt.MinLevel = LogLevel.Trace);
            sc.AddPollyCircuitBreaker(configuration);
            sc.AddRedisListenerForCircuitBreaker(configuration);
            return sc.BuildServiceProvider();
        }


        static async Task Main(string[] args)
        {
            var sp = ConfigureServices();

            CircuitBreakerLog.Init(sp.GetRequiredService<ILoggerFactory>());

            var cb = sp.GetService<ICircuitBreaker>();

            var input = new CircuitRequest
            {
                CircuitKey = "api-books",
                RequestId = Guid.NewGuid().ToString(),
               // Retry = 1,
               // Timeout = TimeSpan.FromMilliseconds(50)
            };
            input.Context.SetAppName("web-bookworm");
            input.Context.SetServiceName("api-search");

            await Execute(cb, input);
            var ts = Stopwatch.StartNew();
            for(var i = 0; i < 500; i++)
            {
                await Execute(cb, input);
            }
            ts.Stop();
            Console.WriteLine($"Total:{ts.ElapsedMilliseconds}ms");

            Console.ReadLine();
        }

        private static async Task Execute(ICircuitBreaker cb, ICircuitRequest request)
        {
            var result = await cb.ExecuteAsync(request, cxt => DoSomething(cxt));
            Console.WriteLine(result.Value);

            WriteStatus(result);
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("******");
            Console.WriteLine();
        }

        private static void WriteStatus(ICircuitResponse response)
        {
            Console.ForegroundColor = response.Status == CircuitStatus.Succeed
                ? ConsoleColor.Green
                : response.Status == CircuitStatus.Timeout
                    ? ConsoleColor.Yellow
                    : response.Status == CircuitStatus.Failed
                        ? ConsoleColor.Red
                        : ConsoleColor.Magenta;

            Console.WriteLine(response.Status);
            Console.ForegroundColor = ConsoleColor.White;
        }

        private static Random _rnd = new Random();
        private static async Task<string> DoSomething(ICircuitRequest request)
        {
            //Console.WriteLine($"Start...");

            //var toss = _rnd.Next(1, 5);

            //if (toss > 2) throw new Exception("Toss failed");

            var delay = _rnd.Next(100, 100);
            await Task.Delay(delay);

            return $"Delay {delay}ms";
        }
    }
}
