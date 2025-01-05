using System.Runtime.InteropServices;

/***************************************************************************************
*    Title: Communication with USB Devices using HID Protocol
*    Author: Orphraie
*    Date: 09/26/2023
*    Availability: https://www.codeproject.com/Articles/1244702/How-to-Communicate-with-its-USB-Devices-using-HID
*
***************************************************************************************/
#pragma warning disable CA1401 // P/Invokes should not be visible
#pragma warning disable SYSLIB1054 // Use 'LibraryImportAttribute' instead of 'DllImportAttribute' to generate P/Invoke marshalling code at compile time
#pragma warning disable CA2101 // Specify marshaling for P/Invoke string arguments
#pragma warning disable CA2211
#pragma warning disable IDE0044 // Add readonly modifier
namespace msfs_bouled.USB.API
{
    public class Kernel32
    {
        [DllImport("setupapi.dll")]
        public static extern nint SetupDiGetClassDevs(ref Guid ClassGuid, nint Enumerator, nint hwndParent, int Flags);

        [DllImport("setupapi.dll")]
        public static extern bool SetupDiEnumDeviceInterfaces(nint hDevInfo, nint devInfo, ref Guid interfaceClassGuid, int memberIndex, ref SP_DEVICE_INTERFACE_DATA deviceInterfaceData);

        [DllImport("setupapi.dll")]
        public static extern bool SetupDiGetDeviceInterfaceDetail(nint DeviceInfoSet, ref SP_DEVICE_INTERFACE_DATA DeviceInterfaceData, ref SP_DEVICE_INTERFACE_DETAIL_DATA DeviceInterfaceDetailData, int DeviceInterfaceDetailDataSize, ref int RequiredSize, nint DeviceInfoData);

        [DllImport("setupapi.dll")]
        public static extern bool SetupDiGetDeviceInterfaceDetail(nint DeviceInfoSet, ref SP_DEVICE_INTERFACE_DATA DeviceInterfaceData, nint DeviceInterfaceDetailData, int DeviceInterfaceDetailDataSize, ref int RequiredSize, nint DeviceInfoData);

        [DllImport("setupapi.dll")]
        public static extern bool SetupDiDestroyDeviceInfoList(nint DeviceInfoSet);

        [DllImport("Kernel32.dll")]
        public static extern nint CreateFile(string lpFileName,
                                               [MarshalAs(UnmanagedType.U4)] FileAccess dwDesiredAccess,
                                               [MarshalAs(UnmanagedType.U4)] FileShare dwShareMode,
                                               int lpSecurityAttributes,
                                               [MarshalAs(UnmanagedType.U4)] FileMode dwCreationDisposition,
                                               [MarshalAs(UnmanagedType.U4)] FileOptions dwFlagsAndAttributes,
                                               nint hTemplateFile);

        [DllImport("Kernel32.dll")]
        public static extern bool DeviceIoControl(nint hDevice,
                                                  [MarshalAs(UnmanagedType.U4)] uint dwIoControlCode,
                                                  nint lpInBuffer,
                                                  [MarshalAs(UnmanagedType.U4)] uint nInBufferSize,
                                                  nint lpOutBuffer,
                                                  [MarshalAs(UnmanagedType.U4)] uint nOutBufferSize,
                                                  [MarshalAs(UnmanagedType.U4)] out uint lpBytesReturned,
                                                  nint lpOverlapped);

        [DllImport("kernel32.dll")]
        public static extern bool WriteFile(nint hFile, [Out] byte[] lpBuffer, uint nNumberOfBytesToWrite, ref uint lpNumberOfBytesWritten, nint lpOverlapped);

        [DllImport("kernel32.dll")]
        public static extern bool ReadFile(nint hFile, [Out] byte[] lpBuffer, uint nNumberOfBytesToRead, ref uint lpNumberOfBytesRead, nint lpOverlapped);

        [DllImport("kernel32.dll")]
        public static extern bool CloseHandle(nint hObject);

        //==========================================================================================================================================================================================================
        // DATA      DATA      DATA      DATA      DATA      DATA      DATA      DATA      DATA      DATA      DATA      DATA      DATA      DATA      DATA      DATA      DATA      DATA      DATA      DATA
        //==========================================================================================================================================================================================================
        public static nint hardwareDeviceInfo;

        public const int DIGCF_PRESENT = 0x00000002;
        public const int DIGCF_DEVICEINTERFACE = 0x00000010;

        public const uint GENERIC_READ = 0x80000000;
        public const uint GENERIC_WRITE = 0x40000000;

        public const uint FILE_SHARE_READ = 0x00000001;
        public const uint FILE_SHARE_WRITE = 0x00000002;

        public const uint OPEN_EXISTING = 3;

        public enum FileMapProtection : uint
        {
            PageReadonly = 0x02,
            PageReadWrite = 0x04,
            PageWriteCopy = 0x08,
            PageExecuteRead = 0x20,
            PageExecuteReadWrite = 0x40,
            SectionCommit = 0x8000000,
            SectionImage = 0x1000000,
            SectionNoCache = 0x10000000,
            SectionReserve = 0x4000000,
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct LIST_ENTRY
        {
            public nint Flink;
            public nint Blink;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SP_DEVICE_INTERFACE_DATA
        {
            public int cbSize;
            public Guid interfaceClassGuid;
            public int flags;
            private nuint reserved;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct SP_DEVICE_INTERFACE_DETAIL_DATA
        {
            public int cbSize;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string DevicePath;
        }
        //==========================================================================================================================================================================================================
        // END_DATA      END_DATA      END_DATA      END_DATA      END_DATA      END_DATA      END_DATA      END_DATA      END_DATA      END_DATA      END_DATA      END_DATA      END_DATA      END_DATA
        //==========================================================================================================================================================================================================
    }
}
#pragma warning restore IDE0044 // Add readonly modifier
#pragma warning restore CA2211
#pragma warning restore CA2101 // Specify marshaling for P/Invoke string arguments
#pragma warning restore SYSLIB1054 // Use 'LibraryImportAttribute' instead of 'DllImportAttribute' to generate P/Invoke marshalling code at compile time
#pragma warning restore CA1401 // P/Invok