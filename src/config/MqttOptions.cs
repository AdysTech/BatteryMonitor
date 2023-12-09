namespace BatteryMonitor.config
{
    public class MqttOptions
    {
        public const string SectionName = "Mqtt";
        public string Host { get; set; }
        public int Port { get; set; }
        public string Client { get; set; }
        public string CredentialKey { get; set; }
        public string TopicPrefix { get; set; }
    }
}