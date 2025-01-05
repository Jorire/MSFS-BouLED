using System.Configuration;
using msfs_bouled.MSFS;
using msfs_bouled.USB.API;

namespace msfs_bouled.USB.GameController.Thrustmaster.Warthog
{
    public class Throttle(string devicePAth, IntPtr handle, HIDD_ATTRIBUTES attributes, HIDP_CAPS capabilities) : HIDDevice(devicePAth, handle, attributes, capabilities) {
        const int BLINK_DELAY_MS = 300;
        const int HID_REPORTID = 1;

        private HashSet<ELED> blinkingLED = new();
        private CancellationTokenSource? cancelBlinking = null;
        private Task? taskBlinkingLED = null;

        private static readonly object _lock = new();

        public enum ELEDIntensity : short {
            OFF = 0x00,
            EXTRA_LOW = 0x01,
            LOW = 0x02,
            MED = 0x03,
            HIGH = 0x04,
            EXTRA_HIGH = 0x05,
        } 

        [Flags]
        public enum ELED : byte {
            /// <summary>
            /// Top round light
            /// /// </summary>
            LED1 = 0x04,

            /// <summary>
            /// Round light 2
            /// </summary>
            LED2 = 0x02,

            /// <summary>
            /// Round light 3
            /// </summary>
            LED3 = 0x10,

            /// <summary>
            /// Round light 4
            /// </summary>
            LED4 = 0x01,

            /// <summary>
            /// Bottom round light
            /// </summary>
            LED5 = 0x40,

            /// <summary>
            /// Global Backlight
            /// </summary>
            Backlight = 0x08,
        }
        public ELEDIntensity LEDIntensiy { get; set; } 

        public ELED LEDs { get; set; } = 0x0;

        public void UpdateLEDState(ELED light, bool enable) {
            LEDs = enable ? (LEDs | light) : (LEDs & ~light);
        }
        public void UpdateLEDState(ELED[] lights, bool enable) {
            foreach (ELED light in lights) {
                LEDs = enable ? (LEDs | light) : (LEDs & ~light);
            }
        }

        public bool GetLEDState(ELED light) {
            return (LEDs & light) != 0;
        }

        public void UpdateLEDOnDevice() {
            byte[] data = new byte[Capabilities.OutputReportByteLength];
            data[0] = 6;
            data[1] = (byte)LEDs;
            data[2] = (byte)LEDIntensiy;
            WriteData(HID_REPORTID, data);
        }

        /// <summary>
        /// Vendor id for Trustmaster Hotas Warthog Throtle device
        /// </summary>
        public static new int VendorId { get { return 0x044f; } }

        /// <summary>
        /// The product id for Trustmaster Hotas Warthog Throtle device
        /// </summary>
        public static new int ProductId { get { return 0x0404; } }

        /// <summary>
        /// Reset Device state 
        /// </summary>
        public override void ResetState() {
            this.LEDIntensiy = ELEDIntensity.EXTRA_LOW;
            UpdateLEDState([ELED.LED1, ELED.LED2, ELED.LED3, ELED.LED4, ELED.LED5, ELED.Backlight], false);
            UpdateLEDOnDevice();
        }

        private void ToggleLED(ELED[] lights) {
            if (lights.Length > 0) {
                bool bEnabled = GetLEDState(lights[0]);
                UpdateLEDState(lights, !bEnabled);
            }
        }

        private void BlinkLED() {
            if(cancelBlinking != null) {
                return;
            }
            lock (_lock) {
                cancelBlinking = new CancellationTokenSource();
                Task.Run(async () => {
                    while (!cancelBlinking.Token.IsCancellationRequested) {
                        ToggleLED(this.blinkingLED.ToArray());
                        UpdateLEDOnDevice();
                        await Task.Delay(BLINK_DELAY_MS, cancelBlinking.Token);
                    }
                }, cancelBlinking.Token)
                    .ContinueWith(t => {
                        lock (_lock) {
                            try {
                                cancelBlinking.Dispose();
                            }
                            catch { }
                            cancelBlinking = null;
                        }
                    });
            }
        }

        /// <summary>
        /// Update device to reflect Sim State
        /// </summary>
        public override void UpdateState(PlaneStatus planeStatus) {
            UpdateFlapsState(planeStatus.flapsPositionPct);
            UpdateBackLight(planeStatus.isInteriorLightOn);
            UpdateLandingGearIndicatory(planeStatus.gearPositionPct);

            this.LEDIntensiy = this.GetLEDIntensityFromSettings();

            // Apply change
            UpdateLEDOnDevice();
            if (blinkingLED.Count > 0) {
                BlinkLED();
            }
        }

        private void UpdateLandingGearIndicatory(double gearPositionPct) {
            switch (gearPositionPct) {
                case 0:
                    this.UpdateLEDState([ELED.LED5], false);
                    this.blinkingLED.Remove(ELED.LED5);
                    break;
                case 100:
                    this.UpdateLEDState([ELED.LED5], true);
                    this.blinkingLED.Remove(ELED.LED5);
                    break;
                default:
                    this.blinkingLED.Add(ELED.LED5);
                    break;
            }
        }

        private void UpdateBackLight(bool isInteriorLightsOn) {
            this.UpdateLEDState([ELED.Backlight], isInteriorLightsOn);
        }

        private void UpdateFlapsState(double flapsPct) {
            int flapIndex = Convert.ToInt32(Math.Floor(flapsPct / 25));
            switch (flapIndex) {
                case 0:
                    this.UpdateLEDState([ELED.LED1, ELED.LED2, ELED.LED3, ELED.LED4], false);
                    break;
                case 1:
                    this.UpdateLEDState([ELED.LED2, ELED.LED3, ELED.LED4], false);
                    this.UpdateLEDState([ELED.LED1], true);
                    break;
                case 2:
                    this.UpdateLEDState([ELED.LED3, ELED.LED4], false);
                    this.UpdateLEDState([ELED.LED1, ELED.LED2], true);
                    break;
                case 3:
                    this.UpdateLEDState([ELED.LED4], false);
                    this.UpdateLEDState([ELED.LED1, ELED.LED2, ELED.LED3], true);
                    break;
                case 4:
                    this.UpdateLEDState([ELED.LED1, ELED.LED2, ELED.LED3, ELED.LED4], true);
                    break;
            }
        }
    }
}
