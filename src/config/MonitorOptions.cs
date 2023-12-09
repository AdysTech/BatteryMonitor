namespace BatteryMonitor.config
{
    public class MonitorOptions
    {
        public const string SectionName = "Monitor";
        public int Frequency { get; set; }
        public int MaxErrorCount { get; set; }
    }
}