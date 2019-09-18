using Microsoft.Extensions.Logging;
using Observito.Trace.EventSourceLogger;
using System;
using System.Diagnostics.Tracing;
using System.Threading;
using System.Threading.Tasks;

namespace Sample
{
    class Program
    {
        static void Main(string[] args)
        {
            var cts = new CancellationTokenSource();

            // Generate in-process events using the sample-provided event source (Observito-Trace-Echo)
            _ = Task.Run(async () =>
            {
                var c = 0;
                while (!cts.IsCancellationRequested)
                {
                    await Task.Delay(TimeSpan.FromSeconds(1), cts.Token);
                    if (c % 4 == 0)
                        EchoEventSource.Log.EchoMore($"Test call #{c}", DateTime.Today, c);
                    else
                        EchoEventSource.Log.Echo($"Test call #{c}");
                    c++;
                }
            });

            /*
            var provider = new ConsoleLoggerProvider((category, level) => level == LogLevel.Information, true);
            var myConsoleLoggerFactory = new LoggerFactory(new[] { provider });
            */
            var loggerFactory = LoggerFactory.Create(builder => {
                //builder.AddFilter("Microsoft", LogLevel.Warning)
                //       .AddFilter("System", LogLevel.Warning)
                //       .AddFilter("SampleApp.Program", LogLevel.Debug)
                builder.AddConsole();
            });

            //var consoleProvider = new ConsoleLoggerProvider((str, lev) => true/*, includeScopes: false*/);
            //using var consoleLogger = consoleProvider.CreateLogger("test");

            using var logger = new EventSourceLogger(loggerFactory.CreateLogger<Program>());

            logger.EnableEvents(EchoEventSource.Log, EventLevel.Informational);

            Console.WriteLine("Press enter to stop sample...");
            Console.ReadLine();

            cts.Cancel();
        }
    }
}
