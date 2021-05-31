namespace BatteryMonitor
{
    public class MqttConfig
    {
        public string Host { get; set; }
        public int Port { get; set; }
        public string Client { get; set; }
        public string CredentialKey { get; set; }
        public string Topic { get; set; }
    }
}