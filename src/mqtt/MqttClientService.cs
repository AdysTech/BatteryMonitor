using AdysTech.CredentialManager;
using BatteryMonitor.config;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Connecting;
using MQTTnet.Client.Disconnecting;
using MQTTnet.Client.Options;
using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace BatteryMonitor.mqtt
{
    public class MqttClientService : IMqttClientService
    {
        private MQTTnet.Client.IMqttClient mqttClient;
        private IMqttClientOptions options;
        private MqttOptions config;
        private bool gracefulStop;
        private bool isStarted;
        private readonly ILogger<MqttClientService> logger;

        public MqttClientService(ILogger<MqttClientService> logger, IConfiguration configuration)
        {
            config = new MqttOptions();
            if (!configuration.GetSection(MqttOptions.SectionName).Exists())
                throw new InvalidOperationException("Config not loaded");
            configuration.GetSection(MqttOptions.SectionName).Bind(config);
            this.logger = logger;
            var credential = CredentialManager.GetCredentials(config.CredentialKey);
            options = new MqttClientOptionsBuilder()
                .WithClientId(config.Client)
                .WithTcpServer(config.Host,config.Port)
                .WithCredentials(credential.UserName, credential.Password)
                .WithCleanSession()
                .Build();

            mqttClient = new MqttFactory().CreateMqttClient();
            mqttClient.ConnectedHandler = this;
            mqttClient.DisconnectedHandler = this;
            mqttClient.ApplicationMessageReceivedHandler = this;
            isStarted = false;

        }


        public Task HandleApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs eventArgs)
        {
            throw new System.NotImplementedException();
        }

        public async Task HandleConnectedAsync(MqttClientConnectedEventArgs eventArgs)
        {
            logger.LogInformation($"connected to MQTT broker");
        }

        public async Task HandleDisconnectedAsync(MqttClientDisconnectedEventArgs eventArgs)
        {
            if (!gracefulStop)
            {
                logger.LogWarning("Unplanned disconnect from MQTT broker");
                await Task.Delay(TimeSpan.FromSeconds(5));
                try
                {
                    await mqttClient.ReconnectAsync();
                }
                catch (Exception e)
                {
                    logger.LogError(e, "Reconnecting to broker failed");

                }
            }
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation($"Starting MQTT client. Connecting to {config.Host}:{config.Port}");
            await mqttClient.ConnectAsync(options);
            if (!mqttClient.IsConnected)
            {
                await mqttClient.ReconnectAsync();
            }
            isStarted = true;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                gracefulStop = true;
                var disconnectOption = new MqttClientDisconnectOptions
                {
                    ReasonCode = MqttClientDisconnectReason.NormalDisconnection,
                    ReasonString = "NormalDisconnection"
                };
                await mqttClient.DisconnectAsync(disconnectOption, cancellationToken);
            }
            await mqttClient.DisconnectAsync();
        }

        public async Task<bool> PublishAsync<TValue>(TValue value, string Topic, CancellationToken cancellationToken)
        {
            if (!isStarted)
            {
                await StartAsync(cancellationToken);
            }
            //var payload = new MemoryStream();
            //await JsonSerializer.SerializeAsync<TValue>(payload, value, cancellationToken: cancellationToken);
            var payload = JsonSerializer.Serialize<TValue>(value);
            var message = new MqttApplicationMessageBuilder()
                .WithTopic($"{config.TopicPrefix}/{Topic}")
                .WithPayload(payload)
                .WithAtLeastOnceQoS()
                .Build();

            var result = await mqttClient.PublishAsync(message, cancellationToken);
            if (result.ReasonCode == MQTTnet.Client.Publishing.MqttClientPublishReasonCode.Success)
                return true;
            else
            {
                logger.LogWarning($"Publishing failed due to {result.ReasonString}");
                throw new InvalidOperationException($"Publishing failed due to {result.ReasonString}");
            }
        }
    }
}