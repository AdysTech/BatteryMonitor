using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BatteryDataModel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BatteryMonitor
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly MonitorOptions options;
        public Worker(ILogger<Worker> logger, IConfiguration configuration)
        {
            _logger = logger;
            options = new MonitorOptions();
            configuration.GetSection(MonitorOptions.SectionName).Bind(options);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var watch = Stopwatch.StartNew();
            var Battaries = new Battaries();
            while (!stoppingToken.IsCancellationRequested)
            {
                watch.Restart();
                foreach (var battery in Battaries.AllBattaries)
                    battery.RefreshStatus();
                _logger.LogInformation($"{DateTimeOffset.Now}   Voltage:{Battaries.CurrentBattery.Status.Voltage} @{Battaries.CurrentBattery.Status.PercentCharge}");

                await Task.Delay(TimeSpan.FromSeconds(options.Frequency - watch.Elapsed.TotalSeconds), stoppingToken);
            }
        }
    }
}
