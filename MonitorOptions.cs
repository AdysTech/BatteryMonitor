namespace BatteryMonitor
{
    public class MonitorOptions
    {
        public const string SectionName = "Monitor";
        public int Frequency { get; set; }
        public MqttConfig MqttConfig { get; set; }
    }
}