using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using MQTTnet.Client.Connecting;
using MQTTnet.Client.Disconnecting;
using MQTTnet.Client.Receiving;

namespace BatteryMonitor.mqtt
{
    public interface IMqttClientService : IHostedService,
                                          IMqttClientConnectedHandler,
                                          IMqttClientDisconnectedHandler,
                                          IMqttApplicationMessageReceivedHandler
    {
        Task<bool> PublishAsync<TValue>(TValue value,string Topic,  CancellationToken cancellationToken);
    }
}