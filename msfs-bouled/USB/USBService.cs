using System.Management;
using System.Reflection;
using System.Runtime.InteropServices;
using msfs_bouled.USB.API;
using static msfs_bouled.USB.API.Kernel32;

namespace msfs_bouled.USB {
    internal class USBService {
        /// <summary>
        /// Controllers manageable
        /// </summary>
        private List<HIDDevice> _controllers = [];
        public List<HIDDevice> Controllers {
            get {
                lock (_lock) {
                    return _controllers;
                }
            }
            private set {
                lock (_lock) {
                    _controllers = value;
                }
            }
        }

        private static readonly object _lock = new();

        /// <summary>
        /// Emit when something changed (sim cx or controllers list)
        /// </summary>
        public event EventHandler? ControllersChanged;

        // Use to detect USB device connection/disconnection 
        private ManagementEventWatcher? usbWatcher;

        public void Initialize() {
            // Watch USB event            
            this.usbWatcher = new(new WqlEventQuery("SELECT * FROM Win32_DeviceChangeEvent WHERE EventType = 2 OR EventType = 3"));
            this.usbWatcher.EventArrived += this.UsbWatcher_EventArrived;
            this.usbWatcher.Start();
            this._controllers = [];
            UpdateManageableDevices();
        }

        /// <summary>
        /// Shutdown background worker
        /// </summary>
        public void Shutdown() {
            lock (_lock) {
                foreach (HIDDevice device in Controllers) {
                    device.ResetState();
                }
                // Remove USB device event handler
                this.usbWatcher?.Stop();
                try {
                    this.usbWatcher?.Dispose();
                }catch { }
                this.usbWatcher = null;
            }
        }

        /// <summary>
        /// USB device connected or disconnected
        /// </summary>
        private void UsbWatcher_EventArrived(object sender, EventArrivedEventArgs e) {
            UpdateManageableDevices();
            ControllersChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Count available devices
        /// </summary>
        /// <returns>number of USB device connected</returns>
        private Int32 GetDevicesCount() {
            var hidGuid = new Guid();
            var deviceInfoData = new SP_DEVICE_INTERFACE_DATA();

            HID.HidD_GetHidGuid(ref hidGuid);

            //
            // Open a handle to the plug and play dev node.
            //
            SetupDiDestroyDeviceInfoList(Kernel32.hardwareDeviceInfo);
            hardwareDeviceInfo = SetupDiGetClassDevs(ref hidGuid, IntPtr.Zero, IntPtr.Zero, DIGCF_PRESENT | DIGCF_DEVICEINTERFACE);
            deviceInfoData.cbSize = Marshal.SizeOf(typeof(SP_DEVICE_INTERFACE_DATA));

            var Index = 0;
            // Count devices until none
            while (SetupDiEnumDeviceInterfaces(hardwareDeviceInfo, IntPtr.Zero, ref hidGuid, Index, ref deviceInfoData)) {
                Index++;
            }

            return (Index);
        }

        /// <summary>
        /// Search and find connected USB devices manageable by the app
        /// </summary>
        /// <returns>list of HID device available and supported by the app</returns>
        private void UpdateManageableDevices() {
            List<HIDDevice> devicesSupported = [];

            var hidGuid = new Guid();
            var deviceInfoData = new SP_DEVICE_INTERFACE_DATA();
            var functionClassDeviceData = new SP_DEVICE_INTERFACE_DETAIL_DATA();

            HID.HidD_GetHidGuid(ref hidGuid);
            // Clear data
            SetupDiDestroyDeviceInfoList(hardwareDeviceInfo);

            // Open a handle to the plug and play device node.
            hardwareDeviceInfo = SetupDiGetClassDevs(ref hidGuid, IntPtr.Zero, IntPtr.Zero, DIGCF_PRESENT | DIGCF_DEVICEINTERFACE);
            deviceInfoData.cbSize = Marshal.SizeOf(typeof(SP_DEVICE_INTERFACE_DATA));

            Int32 numberOfDevices = GetDevicesCount();
            for (var iHIDD = 0; iHIDD < numberOfDevices; iHIDD++) {
                // Enumerate device interfaces in the device node
                SetupDiEnumDeviceInterfaces(hardwareDeviceInfo, IntPtr.Zero, ref hidGuid, iHIDD, ref deviceInfoData);

                // Allocate a function class device data structure to receive the informations
                var RequiredLength = 0;
                SetupDiGetDeviceInterfaceDetail(hardwareDeviceInfo, ref deviceInfoData, IntPtr.Zero, 0, ref RequiredLength, IntPtr.Zero);

                if (IntPtr.Size == 8) {
                    functionClassDeviceData.cbSize = 8;
                } else if (IntPtr.Size == 4) {
                    functionClassDeviceData.cbSize = 5;
                }

                // Get device informations
                SetupDiGetDeviceInterfaceDetail(hardwareDeviceInfo, ref deviceInfoData, ref functionClassDeviceData, RequiredLength, ref RequiredLength, IntPtr.Zero);

                HIDDevice? existing = this.Controllers.FirstOrDefault(d => d.DevicePath == functionClassDeviceData.DevicePath, null);
                if (existing == null) {
                    // Get HID Device details if supported by the app
                    HIDDevice? deviceFound = GetHIDDeviceInformations(functionClassDeviceData.DevicePath);
                    if (deviceFound != null) {
                        devicesSupported.Add(deviceFound);
                    }
                } else {
                    devicesSupported.Add(existing);
                }
            }

            //Reset new devices
            foreach (HIDDevice newDevice in devicesSupported.Where(newDevice => !Controllers.Any(oldDevice => newDevice.DevicePath == oldDevice.DevicePath))) {
                newDevice.ResetState();
            }
            //Clean up old devices
            foreach (HIDDevice disconnectedDevice in Controllers.Where(oldDevice => !devicesSupported.Any(newDevice => oldDevice.DevicePath == newDevice.DevicePath))) {
                CloseHandle(disconnectedDevice.Handle);
            }
            //Replace new list of devices
            this.Controllers = devicesSupported;
        }

        /// <summary>
        /// Get device information 
        /// </summary>
        /// <param name="devicePath">Win32 device path</param>
        /// <returns>HIDDevice with identification informations</returns>
        private HIDDevice? GetHIDDeviceInformations(String devicePath) {
            // Given the HardwareDeviceInfo, representing a handle to the plug and
            // play information, and deviceInfoData, representing a specific hid device,
            // open that device and fill in all the relivant information in the given
            // HID_DEVICE structure.
            IntPtr handle = CreateFile(devicePath, FileAccess.ReadWrite, FileShare.ReadWrite, 0, FileMode.Open, FileOptions.None, IntPtr.Zero);
            IntPtr preparsedData = new();
            HID.HidD_FreePreparsedData(ref preparsedData);
            preparsedData = IntPtr.Zero;


            HIDD_ATTRIBUTES attributes = new();
            HID.HidD_GetAttributes(handle, ref attributes);

            DeviceIdentification deviceType = (from type in HIDDevice.supportedDevicesTypes
                                               where type.productId == attributes.ProductID && type.vendorId == attributes.VendorID
                                               select type).SingleOrDefault();

            if (deviceType.type != null) {
                ConstructorInfo? ctor = deviceType.type.GetConstructor([typeof(string), typeof(IntPtr), typeof(HIDD_ATTRIBUTES), typeof(HIDP_CAPS)]);
                if (ctor != null) {
                    HIDP_CAPS capabilities = new();

                    HID.HidD_GetPreparsedData(handle, ref preparsedData);
                    HID.HidP_GetCaps(preparsedData, ref capabilities);

                    HIDDevice instance = (HIDDevice)ctor.Invoke([devicePath, handle, attributes, capabilities]);
                    var Buffer = Marshal.AllocHGlobal(126);
                    {
                        HID.HidD_GetManufacturerString(handle, Buffer, 126);
                        instance.Manufacturer = Marshal.PtrToStringAuto(Buffer);

                        HID.HidD_GetProductString(handle, Buffer, 126);
                        instance.Product = Marshal.PtrToStringAuto(Buffer);

                        HID.HidD_GetSerialNumberString(handle, Buffer, 126);
                        instance.SerialNumber = Marshal.PtrToStructure<Int32>(Buffer);
                    }
                    Marshal.FreeHGlobal(Buffer);
                    HID.HidD_FreePreparsedData(ref preparsedData);
                    return instance;
                }
            }
            return null;
        }
    }
}
