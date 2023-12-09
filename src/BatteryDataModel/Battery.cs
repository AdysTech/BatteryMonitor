using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace BatteryDataModel
{
    public class Battery
    {

        public DateTime ManufactureDate { get; set; }

        public string Path { get; set; }

        public uint Tag { get; set; }
        public string Chemistry { get; set; }
        [BatteryValueUnit(ValueUnit.mWh)]
        public long DesignedCapacity { get; set; }
        [BatteryValueUnit(ValueUnit.mWh)]
        public ulong CriticalLevel { get; set; }
        [BatteryValueUnit(ValueUnit.mWh)]
        public ulong LowLevel { get; set; }
        public ulong CriticalBias { get; set; }

        public string DeviceName { get; set; }

        public string ManufactureName { get; set; }

        public string SerialNumber { get; set; }

        public string UniqueID { get; set; }
        public ulong CycleCount { get; set; }
        [BatteryValueUnit(ValueUnit.mWh)]
        public long FullChargedCapacity { get; set; }
        [BatteryValueUnit(ValueUnit.Percent)]
        public double WearLevel
        {
            get; internal set;
        }
        public BatteryStatus Status { get; private set; }

        public Battery(string devicePath)
        {
            Path = devicePath;
            Status = new BatteryStatus();
            ReadBatteryMetadata();
        }



        private bool ReadBatteryMetadata()
        {

            //var hBattery = NativeMethods.CreateFile (devicePath, FileAccess.ReadWrite, FileShare.ReadWrite, IntPtr.Zero, FileMode.Open, FileAttributes.Normal, IntPtr.Zero);
            var hBattery = NativeMethods.CreateFile(this.Path,
                desiredAccess: FileAccess.Read,
                shareMode: FileShare.ReadWrite,
                securityAttributes: IntPtr.Zero,
                creationDisposition: FileMode.Open,
                flagsAndAttributes: FileAttributes.Normal,
                templateFile: IntPtr.Zero);
            if (hBattery.IsInvalid)
            { throw new Win32Exception(Marshal.GetLastWin32Error()); }
            var queryInfo = new NativeStructs.BatteryQueryInformation();
            var battery = this;

            int timeOut = 0, outSize = 0;
            try
            {

                queryInfo.BatteryTag = QueryBatteryTag(hBattery);
                battery.Tag = queryInfo.BatteryTag;

                #region BatteryInformation
                var batteryInfo = new NativeStructs.BatteryInformation();
                queryInfo.InformationLevel = NativeConstants.BatteryQueryInformationLevel.BatteryInformation;
                if (!NativeMethods.DeviceIoControl(hBattery, NativeConstants.IOControlCode.BatteryQueryInformation, ref queryInfo, Marshal.SizeOf(queryInfo), ref batteryInfo, Marshal.SizeOf(batteryInfo), ref outSize))
                { throw new Win32Exception(Marshal.GetLastWin32Error()); }

                battery.Chemistry = batteryInfo.Chemistry;
                battery.DesignedCapacity = batteryInfo.DesignedCapacity;
                battery.FullChargedCapacity = batteryInfo.FullChargedCapacity;
                battery.CriticalLevel = batteryInfo.DefaultAlert1;
                battery.LowLevel = batteryInfo.DefaultAlert2;
                battery.CriticalBias = batteryInfo.CriticalBias;
                battery.CycleCount = batteryInfo.CycleCount;
                #endregion

                #region string values, BatteryDeviceName, BatteryManufactureName, BatterySerialNumber, BatteryUniqueID

                StringBuilder strBuffer = new StringBuilder(200);
                queryInfo.InformationLevel = NativeConstants.BatteryQueryInformationLevel.BatteryManufactureName;
                if (!NativeMethods.DeviceIoControl(hBattery, NativeConstants.IOControlCode.BatteryQueryInformation, ref queryInfo, Marshal.SizeOf(queryInfo), strBuffer, strBuffer.Capacity, ref outSize))
                { throw new Win32Exception(Marshal.GetLastWin32Error()); }
                battery.ManufactureName = strBuffer.ToString();
                strBuffer.Clear();

                queryInfo.InformationLevel = NativeConstants.BatteryQueryInformationLevel.BatterySerialNumber;
                if (!NativeMethods.DeviceIoControl(hBattery, NativeConstants.IOControlCode.BatteryQueryInformation, ref queryInfo, Marshal.SizeOf(queryInfo), strBuffer, strBuffer.Capacity, ref outSize))
                { throw new Win32Exception(Marshal.GetLastWin32Error()); }
                battery.SerialNumber = strBuffer.ToString();
                strBuffer.Clear();

                queryInfo.InformationLevel = NativeConstants.BatteryQueryInformationLevel.BatteryDeviceName;
                if (!NativeMethods.DeviceIoControl(hBattery, NativeConstants.IOControlCode.BatteryQueryInformation, ref queryInfo, Marshal.SizeOf(queryInfo), strBuffer, strBuffer.Capacity, ref outSize))
                { throw new Win32Exception(Marshal.GetLastWin32Error()); }
                battery.DeviceName = strBuffer.ToString();
                strBuffer.Clear();

                queryInfo.InformationLevel = NativeConstants.BatteryQueryInformationLevel.BatteryUniqueID;
                if (!NativeMethods.DeviceIoControl(hBattery, NativeConstants.IOControlCode.BatteryQueryInformation, ref queryInfo, Marshal.SizeOf(queryInfo), strBuffer, strBuffer.Capacity, ref outSize))
                { throw new Win32Exception(Marshal.GetLastWin32Error()); }
                battery.UniqueID = strBuffer.ToString();
                strBuffer.Clear();
                #endregion

                #region manufature date
                var batteryDate = new NativeStructs.BatteryManufactureDate();
                queryInfo.InformationLevel = NativeConstants.BatteryQueryInformationLevel.BatteryManufactureDate;
                if (NativeMethods.DeviceIoControl(hBattery, NativeConstants.IOControlCode.BatteryQueryInformation, ref queryInfo, Marshal.SizeOf(queryInfo), ref batteryDate, Marshal.SizeOf(batteryDate), ref outSize))
                    battery.ManufactureDate = new DateTime(batteryDate.Year, batteryDate.Month, batteryDate.Day);
                #endregion
                WearLevel = Math.Round(((DesignedCapacity - FullChargedCapacity) / (FullChargedCapacity * 1.0) * 100), 2);

            }
            catch (Exception e)
            {
                throw;
            }
            finally
            {
                hBattery.Close();
            }


            RefreshStatus();

            return true;
        }
        public void RefreshStatus()
        {
            int outSize = 0;
            var queryInfo = new NativeStructs.BatteryQueryInformation();

            #region long, BatteryEstimatedTime,BatteryTemperature

            ulong lnBuffer = 0;

            var hBattery = NativeMethods.CreateFile(this.Path,
                desiredAccess: FileAccess.Read,
                shareMode: FileShare.ReadWrite,
                securityAttributes: IntPtr.Zero,
                creationDisposition: FileMode.Open,
                flagsAndAttributes: FileAttributes.Normal,
                templateFile: IntPtr.Zero);

            if (hBattery.IsInvalid)
            { throw new Win32Exception(Marshal.GetLastWin32Error()); }

            try
            {
                queryInfo.BatteryTag = QueryBatteryTag(hBattery);

                queryInfo.InformationLevel = NativeConstants.BatteryQueryInformationLevel.BatteryTemperature;
                if (NativeMethods.DeviceIoControl(hBattery, NativeConstants.IOControlCode.BatteryQueryInformation, ref queryInfo, Marshal.SizeOf(queryInfo), ref lnBuffer, Marshal.SizeOf(lnBuffer), ref outSize))
                    Status.Temperature = lnBuffer;

                queryInfo.InformationLevel = NativeConstants.BatteryQueryInformationLevel.BatteryEstimatedTime;
                if (NativeMethods.DeviceIoControl(hBattery, NativeConstants.IOControlCode.BatteryQueryInformation, ref queryInfo, Marshal.SizeOf(queryInfo), ref lnBuffer, Marshal.SizeOf(lnBuffer), ref outSize))
                    Status.EstimatedTime = lnBuffer;

                #endregion

                #region status
                var batWaitStatus = new NativeStructs.BatteryWaitStatus();
                batWaitStatus.BatteryTag = queryInfo.BatteryTag;
                //batWaitStatus.Timeout = 0;
                var batteryStatus = new NativeStructs.BatteryStatus();

                if (!NativeMethods.DeviceIoControl(hBattery, NativeConstants.IOControlCode.BatteryQueryStatus, ref batWaitStatus, Marshal.SizeOf(batWaitStatus), ref batteryStatus, Marshal.SizeOf(batteryStatus), ref outSize))
                { throw new Win32Exception(Marshal.GetLastWin32Error()); }

                Status.CurrentCapacity = batteryStatus.Capacity;
                Status.PowerState = (BatteryStatusPowerState)batteryStatus.PowerState;
                Status.Rate = batteryStatus.Rate;
                Status.Voltage = batteryStatus.Voltage;
                #endregion
                Status.PercentCharge = Math.Round((Status.CurrentCapacity / (FullChargedCapacity * 1.0) * 100), 2);

            }
            catch (Exception e)
            {
                throw;
            }
            finally
            {
                hBattery.Close();
            }


        }
        uint QueryBatteryTag(Microsoft.Win32.SafeHandles.SafeFileHandle hBattery)
        {
            int timeOut = 0, outSize = 0;
            uint tag = 0;
            if (!NativeMethods.DeviceIoControl(hBattery, NativeConstants.IOControlCode.BatteryQueryTag, timeOut, Marshal.SizeOf(timeOut), ref tag, Marshal.SizeOf(tag), ref outSize, IntPtr.Zero))
            { throw new Win32Exception(Marshal.GetLastWin32Error()); }
            return tag;
        }
    }
}
