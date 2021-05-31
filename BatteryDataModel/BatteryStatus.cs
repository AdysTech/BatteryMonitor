using System;

namespace BatteryDataModel
{
    [Flags]
    public enum BatteryStatusPowerState
    {
        BatteryPowerOnLine = 0x00000001,
        BatteryDischarging = 0x00000002,
        BatteryCharging = 0x00000004,
        BatteryCritical = 0x00000008
    }

    public class BatteryStatus
    {
        
        public BatteryStatusPowerState PowerState { get; set; }
        [BatteryValueUnit(ValueUnit.mWh)]
        public ulong CurrentCapacity { get; set; }
        [BatteryValueUnit(ValueUnit.mV)]
        public Single Voltage { get; set; }
        [BatteryValueUnit(ValueUnit.mW)]
        public long Rate { get; set; }

        public ulong Temperature { get; set; }

        [BatteryValueUnit(ValueUnit.Seconds)]
        public ulong EstimatedTime { get; set; }


        [BatteryValueUnit(ValueUnit.Percent)]
        public double PercentCharge
        {
             get; internal set;
        }
    }
}