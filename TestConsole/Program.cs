﻿using Bolt.CircuitBreaker.Abstracts;
using Bolt.CircuitBreaker.PollyImpl;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace TestConsole
{
    class Program
    {
        private static void ConfigureLogger(IServiceProvider sp)
        {
            Log.Logger = new LoggerConfiguration()
              .MinimumLevel.Information()
              .WriteTo.Console()
              .CreateLogger();

            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
            loggerFactory.AddSerilog();
        }


        private static IServiceProvider ConfigureServices()
        {
            var sc = new ServiceCollection();
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

            IConfiguration configuration = builder.Build();
            sc.AddLogging();
            sc.AddPollyCircuitBreaker(configuration);

            return sc.BuildServiceProvider();
        }


        static async Task Main(string[] args)
        {
            var sp = ConfigureServices();
            ConfigureLogger(sp);

            CircuitBreakerLog.Init(sp.GetRequiredService<ILoggerFactory>());

            var cb = sp.GetService<ICircuitBreaker>();
            var input = new CircuitRequest
            {
                CircuitKey = "api-books:get",
                ServiceName = "api-books",
                AppName = "web-books",
                RequestId = Guid.NewGuid().ToString(),
                Retry = 1,
                Timeout = TimeSpan.FromMilliseconds(50)
            };

            for(var i = 0; i < 500; i++)
            {
                var tasks = new List<Task>();
                for(var j = 0; j < 10; j++)
                {
                    tasks.Add(Execute(cb, input));

                }

                await Task.WhenAll(tasks);
            }

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
        private static async Task<string> DoSomething(ICircuitContext context)
        {
            Console.WriteLine($"Start...");

            //var toss = _rnd.Next(1, 5);

            //if (toss > 2) throw new Exception("Toss failed");

            var delay = _rnd.Next(80, 90);
            await Task.Delay(delay);

            return $"Delay {delay}ms";
        }
    }
}
