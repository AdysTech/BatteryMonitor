using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using BatteryDataModel;
using BatteryMonitor.config;
using BatteryMonitor.mqtt;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BatteryMonitor
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> logger;
        private readonly MonitorOptions options;
        private IMqttClientService mqttClient;
        private int queryError = 0, pubError = 0;

        public Worker(ILogger<Worker> logger, IConfiguration configuration, IMqttClientService mqttClient)
        {
            this.logger = logger;
            options = new MonitorOptions();
            configuration.GetSection(MonitorOptions.SectionName).Bind(options);
            this.mqttClient = mqttClient;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var watch = Stopwatch.StartNew();
            var Battaries = new Battaries();
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    watch.Restart();
                    foreach (var battery in Battaries.AllBattaries)
                    {
                        try
                        {
                            battery.RefreshStatus();
                            queryError = 0;
                        }
                        catch (Exception e)
                        {
                            logger.LogDebug(e, "Exception RefreshStatus");
                            queryError++;
                            if (queryError >= options.MaxErrorCount)
                                throw;
                            await Task.Delay(TimeSpan.FromSeconds(options.Frequency * queryError));
                            continue;
                        }
                        try
                        {
                            await mqttClient.PublishAsync<BatteryStatus>(battery.Status, $"{System.Environment.MachineName}/{battery.DeviceName}", stoppingToken);
                            logger.LogDebug($"{battery.DeviceName} @ {battery.Status.PercentCharge} - {battery.Status.PowerState.ToString()}");
                            pubError = 0;
                        }
                        catch (Exception e)
                        {
                            logger.LogDebug(e, "Exception RefreshStatus");
                            pubError++;
                            if (pubError >= options.MaxErrorCount)
                                throw;
                            await Task.Delay(TimeSpan.FromSeconds(options.Frequency * pubError));
                            continue;
                        }
                    }
                    await Task.Delay(TimeSpan.FromSeconds(options.Frequency - watch.Elapsed.TotalSeconds % options.Frequency), stoppingToken);
                }
                catch (Exception e)
                {
                    logger.LogError(e, "Exception in Battery Monitor");
                }
            }
        }
    }
}
