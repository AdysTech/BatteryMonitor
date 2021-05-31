using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace BatteryDataModel
{
    public class Battaries
    {
        private ObservableCollection<Battery> battaries;

        public ObservableCollection<Battery> AllBattaries
        {
            get { return battaries; }
        }

        private Battery currentBattery;

        public Battery CurrentBattery
        {
            get { return currentBattery; }
            set { currentBattery = value; }
        }


        //DEFINE_GUID( GUID_DEVICE_BATTERY, 0x72631e54L, 0x78A4, 0x11d0, 0xbc, 0xf7, 0x00, 0xaa, 0x00, 0xb7, 0xb3, 0x2a );
        public Battaries()
        {
            battaries = new ObservableCollection<Battery>();
            var batteryGUID = new Guid(0x72631e54, 0x78a4, 0x11d0, 0xbc, 0xf7, 0x00, 0xaa, 0x00, 0xb7, 0xb3, 0x2a); //new Guid (0x72631e54, 0x78A4, 0x11d0, 0xbc, 0xf7, 0x00, 0xaa, 0x00, 0xb7, 0xb3, 0x2a);
            var hDev = NativeMethods.SetupDiGetClassDevs(ref batteryGUID, IntPtr.Zero, IntPtr.Zero, NativeConstants.GetClassFlags.SupportingDeviceInterface | NativeConstants.GetClassFlags.CurrentlyPresent);
            if (hDev == new IntPtr(-1))
            { throw new Win32Exception(Marshal.GetLastWin32Error()); }
            try
            {
                uint deviceCount = 0;
                do
                {
                    var deviceInterfaceData = new NativeStructs.DeviceInterfaceData();
                    deviceInterfaceData.cbSize = Marshal.SizeOf(deviceInterfaceData);

                    if (NativeMethods.SetupDiEnumDeviceInterfaces(hDev, IntPtr.Zero, ref batteryGUID, deviceCount, ref deviceInterfaceData))
                    {
                        battaries.Add(GetBatteryInfo(hDev, ref deviceInterfaceData));
                    }
                    else
                    {
                        if (Marshal.GetLastWin32Error() == (int)NativeConstants.ErrorCodes.NoMoreItems)
                            break;
                        else
                        { throw new Win32Exception(Marshal.GetLastWin32Error()); }
                    }
                    deviceCount++;
                }
                while (true);
            }
            finally
            {
                NativeMethods.SetupDiDestroyDeviceInfoList(hDev);
            }
            CurrentBattery = AllBattaries.FirstOrDefault();
        }

        private static Battery GetBatteryInfo(IntPtr hDev, ref NativeStructs.DeviceInterfaceData deviceInterfaceData)
        {
            uint requiredSize = 0;
            string devicePath = string.Empty;
            NativeMethods.SetupDiGetDeviceInterfaceDetail(hDev, ref deviceInterfaceData, IntPtr.Zero, 0, out requiredSize, IntPtr.Zero);
            if (Marshal.GetLastWin32Error() != (int)NativeConstants.ErrorCodes.InsufficientBuffer)
                Marshal.ThrowExceptionForHR(Marshal.GetLastWin32Error());
            //Just pass the bufffer instead of SP_DEVICE_INTERFACE_DETAIL_DATA
            IntPtr DeviceInterfaceDetailData = Marshal.AllocHGlobal((int)requiredSize);
            var deviceInterfaceDataStruct = new NativeStructs.DeviceInterfaceDetailData();
            try
            {
                Marshal.WriteInt32(DeviceInterfaceDetailData, Marshal.SizeOf(deviceInterfaceDataStruct));
                if (NativeMethods.SetupDiGetDeviceInterfaceDetail(hDev, ref deviceInterfaceData, DeviceInterfaceDetailData, requiredSize, out requiredSize, IntPtr.Zero))
                {

                    devicePath = Marshal.PtrToStringAuto(IntPtr.Add(DeviceInterfaceDetailData, Marshal.SizeOf(deviceInterfaceDataStruct.cbSize)));
                    var battery = new Battery(devicePath);
                    return battery;
                }
                else
                { throw new Win32Exception(Marshal.GetLastWin32Error()); }
            }
            finally
            {
                Marshal.FreeHGlobal(DeviceInterfaceDetailData);
            }
        }


    }
}
