using System;
using EventProcessor.Configs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace EventProcessor
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    services.Configure<EventStoreConnectionConfig>(esConfig =>
                    {
                        esConfig.IpAddress = "127.0.0.1";
                        esConfig.IpPort = "1113";
                        esConfig.Username = "admin";
                        esConfig.Password = "changeit";
                    });

                    services.AddSingleton<IEventStoreConnectionConfig>(sp =>
                        sp.GetRequiredService<IOptions<EventStoreConnectionConfig>>().Value
                    );
                    
                    try
                    {
                        services.AddHostedService<EventProcessorWorker>();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                });
    }
}