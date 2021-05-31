using System;

namespace BatteryDataModel
{
    public enum ValueUnit
    {
        mWh,
        mW,
        mV,
        Percent,
        Seconds
    }
    [AttributeUsage(AttributeTargets.Property, Inherited = true)]
    public class BatteryValueUnit : Attribute
    {

        public readonly ValueUnit Unit;

        public BatteryValueUnit(ValueUnit unit)
        {
            Unit = unit;
        }
    }
}