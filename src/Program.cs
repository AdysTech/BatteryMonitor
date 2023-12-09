using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BatteryMonitor.mqtt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.EventLog;

namespace BatteryMonitor
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseWindowsService(options =>
                {
                    options.ServiceName = "Battery Monitor Service";
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddLogging(options =>
                    {
                        options.AddSimpleConsole(c =>
                        {
                            c.TimestampFormat = "[yyyy-MM-dd HH:mm:ss] ";
                        });
                    });
                    services.AddHostedService<Worker>()
                    .Configure<EventLogSettings>(config =>
                    {
                        // config.LogName = "BatteryMonitor";
                        config.SourceName = "Battery Monitor Service";
                    });
                    services.AddSingleton<IMqttClientService, MqttClientService>();
                });
    }
}
