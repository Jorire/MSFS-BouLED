using System.Runtime.InteropServices;

#pragma warning disable CA1401 // P/Invokes should not be visible
#pragma warning disable SYSLIB1054 // Use 'LibraryImportAttribute' instead of 'DllImportAttribute' to generate P/Invoke marshalling code at compile time
namespace msfs_bouled.USB.API
{
    public static class HID
    {
        [DllImport("hid.dll")]
        public static extern void HidD_GetHidGuid(ref Guid Guid);

        [DllImport("hid.dll")]
        public static extern bool HidD_GetPreparsedData(nint HidDeviceObject, ref nint PreparsedData);

        [DllImport("hid.dll")]
        public static extern bool HidD_GetAttributes(nint DeviceObject, ref HIDD_ATTRIBUTES Attributes);

        [DllImport("hid.dll")]
        public static extern uint HidP_GetCaps(nint PreparsedData, ref HIDP_CAPS Capabilities);

        [DllImport("hid.dll")]
        public static extern bool HidD_FreePreparsedData(ref nint PreparsedData);

        [DllImport("hid.dll")]
        public static extern bool HidD_GetProductString(nint HidDeviceObject, nint Buffer, uint BufferLength);

        [DllImport("hid.dll")]
        public static extern bool HidD_GetSerialNumberString(nint HidDeviceObject, nint Buffer, int BufferLength);

        [DllImport("hid.dll")]
        public static extern bool HidD_GetManufacturerString(nint HidDeviceObject, nint Buffer, int BufferLength);

        [DllImport("hid.dll")]
        public static extern bool HidD_GetHidReportDescriptor(nint HidDeviceObject, nint ReportDescriptor, int ReportDescriptorLength);
    }

    //==========================================================================================================================================================================================================
    // DATA      DATA      DATA      DATA      DATA      DATA      DATA      DATA      DATA      DATA      DATA      DATA      DATA      DATA      DATA      DATA      DATA      DATA      DATA      DATA
    //==========================================================================================================================================================================================================
    public enum HIDP_REPORT_TYPE : uint
    {
        HidP_Input = 0x00,
        HidP_Output = 0x01,
        HidP_Feature = 0x02,
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct HIDP_CAPS
    {
        public ushort Usage;
        public ushort UsagePage;
        public ushort InputReportByteLength;
        public ushort OutputReportByteLength;
        public ushort FeatureReportByteLength;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 17)]
        public ushort[] Reserved;

        public ushort NumberLinkCollectionNodes;

        public ushort NumberInputButtonCaps;
        public ushort NumberInputValueCaps;
        public ushort NumberInputDataIndices;

        public ushort NumberOutputButtonCaps;
        public ushort NumberOutputValueCaps;
        public ushort NumberOutputDataIndices;

        public ushort NumberFeatureButtonCaps;
        public ushort NumberFeatureValueCaps;
        public ushort NumberFeatureDataIndices;
    };

    [StructLayout(LayoutKind.Sequential)]
    public struct HIDD_ATTRIBUTES
    {
        public uint Size;
        public ushort VendorID;
        public ushort ProductID;
        public ushort VersionNumber;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct HIDP_Range
    {
        public ushort UsageMin, UsageMax;
        public ushort StringMin, StringMax;
        public ushort DesignatorMin, DesignatorMax;
        public ushort DataIndexMin, DataIndexMax;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct HIDP_NotRange
    {
        public ushort Usage, Reserved1;
        public ushort StringIndex, Reserved2;
        public ushort DesignatorIndex, Reserved3;
        public ushort DataIndex, Reserved4;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ButtonData
    {
        public int UsageMin;
        public int UsageMax;
        public int MaxUsageLength;
        public short Usages;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ValueData
    {
        public ushort Usage;
        public ushort Reserved;

        public ulong Value;
        public long ScaledValue;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct HID_DATA
    {
        [FieldOffset(0)]
        public bool IsButtonData;
        [FieldOffset(1)]
        public byte Reserved;
        [FieldOffset(2)]
        public ushort UsagePage;
        [FieldOffset(4)]
        public int Status;
        [FieldOffset(8)]
        public int ReportID;
        [FieldOffset(16)]
        public bool IsDataSet;

        [FieldOffset(17)]
        public ButtonData ButtonData;
        [FieldOffset(17)]
        public ValueData ValueData;
    }

    public struct HID_DEVICE
    {
        public string Manufacturer;
        public string Product;
        public int SerialNumber;
        public ushort VersionNumber;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string DevicePath;
        public nint Handle;

        public bool OpenedForRead;
        public bool OpenedForWrite;
        public bool OpenedOverlapped;
        public bool OpenedExclusive;

        public nint Ppd;
        public HIDP_CAPS Caps;
        public HIDD_ATTRIBUTES Attributes;

        public nint[] InputReportBuffer;
        public HID_DATA[] InputData;
        public int InputDataLength;

        public nint[] OutputReportBuffer;
        public HID_DATA[] OutputData;
        public int OutputDataLength;

        public nint[] FeatureReportBuffer;
        public HID_DATA[] FeatureData;
        public int FeatureDataLength;
    }
    //==========================================================================================================================================================================================================
    // END_DATA      END_DATA      END_DATA      END_DATA      END_DATA      END_DATA      END_DATA      END_DATA      END_DATA      END_DATA      END_DATA      END_DATA      END_DATA      END_DATA
    //==========================================================================================================================================================================================================
}
#pragma warning restore SYSLIB1054 // Use 'LibraryImportAttribute' instead of 'DllImportAttribute' to generate P/Invoke marshalling code at compile time
#pragma warning restore CA1401 // P/Invokes should not be visible