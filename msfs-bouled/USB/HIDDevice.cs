using static msfs_bouled.USB.API.Kernel32;
using System.Reflection;
using msfs_bouled.USB.API;
using msfs_bouled.MSFS;
using static msfs_bouled.USB.GameController.Thrustmaster.Warthog.Throttle;
using System.Configuration;

namespace msfs_bouled.USB {
    /// <summary>
    /// Basic device identification
    /// </summary>
    internal struct DeviceIdentification {
        public int? vendorId;
        public int? productId;
        public Type? type;
    }

    public abstract class HIDDevice(String devicePath, IntPtr handle, HIDD_ATTRIBUTES attributes, HIDP_CAPS capabilities) {
        // List of device type manageable by the app
        internal static List<DeviceIdentification> supportedDevicesTypes;

        /// <summary>
        /// Static initializer for list of devices types supported by the app
        /// </summary>
        static HIDDevice() {
            supportedDevicesTypes = [];
            var currentAssembly = Assembly.GetAssembly(typeof(HIDDevice));
            if (currentAssembly == null) {
                return;
            }

            // Find all classes inheriting from HIDDevice
            IEnumerable<Type> types = from type in currentAssembly.GetTypes()
                                      where type.IsSubclassOf(typeof(HIDDevice))
                                      select type;
            foreach (Type type in types) {
                // Get default device VendorId / ProductID 
                PropertyInfo? vendorIdProp = type.GetProperty("VendorId");
                PropertyInfo? productIdProp = type.GetProperty("ProductId");
                if (vendorIdProp != null && productIdProp != null) {
                    DeviceIdentification intf = new DeviceIdentification {
                        vendorId = (int?)vendorIdProp.GetValue(null),
                        productId = (int?)productIdProp.GetValue(null),
                        type = type
                    };

                    supportedDevicesTypes.Add(intf);
                }
            }
        }

        /// <summary>
        /// Vendor id for usb device (must be overrided by subclasses)
        /// </summary>
        public static int VendorId { get { return 0x0; } }

        /// <summary>
        /// The product id for usb device (must be overrided by subclasses)
        /// </summary>
        public static int ProductId { get { return 0x0; } }


        /// <summary>
        /// Device Capabilities (# buttons, i/o buffer definitions, ...)
        /// </summary>
        public HIDP_CAPS Capabilities { get; } = capabilities;

        /// <summary>
        /// Device definition (vendorId, productID, serial number)
        /// </summary>
        public HIDD_ATTRIBUTES Attributes { get; } = attributes;
        

        /// <summary>
        /// Win32 device path
        /// </summary>
        public IntPtr Handle { get; } = handle;

        /// <summary>
        /// Win32 device path
        /// </summary>
        public String DevicePath { get; } = devicePath;

        /// <summary>
        /// Device manufacturer
        /// </summary>
        public string? Manufacturer { get; set; }

        /// <summary>
        /// Device commercial name
        /// </summary>
        public string? Product { get; set; }

        /// <summary>
        /// Device serial number
        /// </summary>
        public int SerialNumber { get; set; }

        /// <summary>
        /// Read data USB device has sent to PC 
        /// </summary>
        /// <returns>byte values of last data send by the device</returns>
        protected List<byte> ReadData() {
            if(this.Capabilities.InputReportByteLength > 0) {
                Byte[] ReportBuffer = new Byte[this.Capabilities.InputReportByteLength];

                if (ReportBuffer.Length > 0) {
                    uint NumberOfBytesRead = 0U;
                    Kernel32.ReadFile(handle, ReportBuffer, this.Capabilities.InputReportByteLength, ref NumberOfBytesRead, IntPtr.Zero);
                    return new List<Byte>(ReportBuffer);
                }
            }
            return [];
        }
        /// <summary>
        /// Sent data to the USB device 
        /// </summary>
        protected void WriteData(byte HIDReportID, Byte[] data) {
            if(data.Length != this.Capabilities.OutputReportByteLength) {
                throw new ArgumentException($"Data sent must have {this.Capabilities.OutputReportByteLength} bytes (actually {data.Length} bytes long).");
            }
            var ReportBuffer = new Byte[this.Capabilities.OutputReportByteLength];
            ReportBuffer[0] = HIDReportID;
            Array.Copy(data, 0, ReportBuffer, 1, data.Length -1);
            var varA = 0U;
            Kernel32.WriteFile(handle, ReportBuffer, this.Capabilities.OutputReportByteLength, ref varA, IntPtr.Zero);
        }

        /// <summary>
        /// Reset Device state 
        /// </summary>
        public abstract void ResetState();
        /// <summary>
        /// Update device to reflect Sim State
        /// </summary>
        public abstract void UpdateState(PlaneStatus planeStatus);

        protected ELEDIntensity GetLEDIntensityFromSettings() {
            ELEDIntensity? settings = null;
            try {
                String? settingIntensity = ConfigurationManager.AppSettings["LightIntensity"];
                if (settingIntensity != null) {
                    return (ELEDIntensity)Enum.Parse(typeof(ELEDIntensity), settingIntensity);
                }
            }
            catch {
            }
            return ELEDIntensity.EXTRA_LOW;
        }

    }
}
