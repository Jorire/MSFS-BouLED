using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using msfs_bouled.MSFS;
using msfs_bouled.USB;
using msfs_bouled.USB.GameController;
using msfs_bouled.USB.GameController.Thrustmaster.Warthog;

namespace msfs_bouled {
    /// <summary>
    /// Main background worker (singleton)
    /// Handle sim cx and controller identification
    /// </summary>
    internal class SyncLEDService {
        // Periodic retry to connect to sim (in ms.)
        const int SIM_REFRESH_RATE_MS = 5000;

        #region Singleton
        private static SyncLEDService? _instance;

        private SyncLEDService() {
            _instance = null;
        }
        private static readonly object _lock = new();

        public static SyncLEDService GetInstance() {
            if (_instance == null) {
                lock (_lock) {
                    _instance ??= new SyncLEDService();
                }
            }
            return _instance;
        }
        #endregion

        /// <summary>
        /// ASimConnect
        /// </summary>
        public SimConnectService SimConnectService { get; } = new SimConnectService();
        /// <summary>
        /// Usb Interface
        /// </summary>
        public USBService USBService { get;  } = new USBService();

        /// <summary>
        /// Emit when something changed (sim cx or controllers list)
        /// </summary>
        public event EventHandler? StatusChanged;

        // Cancel token for peridioc sim cx refresh
        private CancellationTokenSource? cancelKeepAliveSim = null;
        private Task? keepAliveSimTask = null;

        /// <summary>
        /// One or more Controller detected ?
        /// </summary>
        public bool IsControllerConnected { get { return USBService.Controllers.Count > 0; } }
        /// <summary>
        /// MSFS connected ?
        /// </summary>
        public bool IsSimconnectConnected { get { return SimConnectService.IsSimConnected == EConnectionStatus.Connected; } } 

        #region lifecycle
        /// <summary>
        /// Start background worker
        /// </summary>
        public void Initialize() {
            // Monitor Controllers
            this.USBService.Initialize();
            // Connect to USB events
            this.USBService.ControllersChanged += USBService_ControllersChanged;

            // Connect to Sim if possible (or retry)
            this.KeepAliveSimCx();
            // Connect to Sim events
            this.SimConnectService.SimConnected += SimConnectService_SimConnected;
            this.SimConnectService.SimDisconnected += SimConnectService_SimDisconnected;
            this.SimConnectService.SimUpdate += SimConnectService_SimUpdate;
        }

        private void USBService_ControllersChanged(object? sender, EventArgs e) {
            StatusChanged?.Invoke(this, EventArgs.Empty);
        }

        private void SimConnectService_SimUpdate(object? sender, SimDataEventArgs e) {
            foreach(HIDDevice device in USBService.Controllers) {
                device.UpdateState(e.PlaneStatus);
            }
        }

        private void SimConnectService_SimDisconnected(object? sender, EventArgs e) {
            KeepAliveSimCx();
            StatusChanged?.Invoke(this, EventArgs.Empty);
        }

        private void SimConnectService_SimConnected(object? sender, EventArgs e) {
            CancelKeepAliveTask();
            StatusChanged?.Invoke(this, EventArgs.Empty);
        }

        private void CancelKeepAliveTask() {
            lock (_lock) {
                this.cancelKeepAliveSim?.Cancel();
                if (this.keepAliveSimTask?.Status < TaskStatus.RanToCompletion) {
                    //Wait for completion
                    this.keepAliveSimTask.Wait(SIM_REFRESH_RATE_MS);
                }
                try {
                    this.keepAliveSimTask?.Dispose();
                }
                catch { }
                this.cancelKeepAliveSim?.Dispose();
                this.cancelKeepAliveSim = null;
                this.keepAliveSimTask = null;
            }
        }

        /// <summary>
        /// Shutdown background worker
        /// </summary>
        public void Shutdown() {
            lock (_lock) {
                // Disconnect from Sim
                this.SimConnectService.DisconnectFromSim();

                // Reset controllers
                this.USBService.Shutdown();

                // Stop refresh MSFS cx
                this.CancelKeepAliveTask();
            }
        }
        #endregion

        #region Keep Alive Controller & Sim Connection
        /// <summary>
        /// Try to connect or retry after a pause
        /// </summary>
        private void KeepAliveSimCx() {
            lock (_lock) {
                this.cancelKeepAliveSim = new CancellationTokenSource();
                CancellationToken token = this.cancelKeepAliveSim.Token;
                this.keepAliveSimTask = Task.Run(async () => {
                    while (!token.IsCancellationRequested) {
                        lock (_lock) {
                            if (!this.IsSimconnectConnected) {
                                this.SimConnectService.ConnectToSim();
                            }
                        }
                        await Task.Delay(SIM_REFRESH_RATE_MS);
                    }
                }, token);
            }
        }
        #endregion
    }
}
