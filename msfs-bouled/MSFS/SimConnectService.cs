using Microsoft.FlightSimulator.SimConnect;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace msfs_bouled.MSFS {
    public enum EConnectionStatus {
        Connected,
        Disconnected,
        Pending
    }

    public class SimConnectService {
        /// <summary>
        ///  Timeout waiting for a event (in ms.)
        /// </summary>
        const int MSG_RCV_WAIT_TIME_SIM_MS = 5000;
        /// <summary>
        /// Connection timeout (in ms.)
        /// </summary>
        const int SIM_CONNECT_TIMEOUT_MS = 30000;

        /// <summary>
        /// SimConnect object
        /// </summary>
        public SimConnect? SimConnect {
            get; set;
        }

        #region Simconnect Message pump 
        /// <summary>
        /// MSFS Wait Event Handle
        /// </summary>
        readonly EventWaitHandle simEventReady = new EventWaitHandle(false, EventResetMode.AutoReset);
        /// <summary>
        /// Cancelation source for wait event handler
        /// </summary>
        private CancellationTokenSource? cancelSimWaitEvent = new();
        /// <summary>
        /// Wait event task
        /// </summary>
        private Task? simEventWaitTask = null;
        #endregion

        #region
        /// <summary>
        /// Emit when is connected
        /// </summary>
        public event EventHandler? SimConnected;
        /// <summary>
        /// Emit when sim is disconnected
        /// </summary>
        public event EventHandler? SimDisconnected;
        /// <summary>
        /// Emit when sim send data
        /// </summary>
        public event EventHandler<SimDataEventArgs>? SimUpdate;

        #endregion

        /// <summary>
        /// Lock for concurrent access
        /// </summary>
        private static readonly object _lock = new();

        private EConnectionStatus _isSimConnected = EConnectionStatus.Disconnected;
        public EConnectionStatus IsSimConnected {
            get {
                lock (_lock) {
                    return _isSimConnected;
                }
            }
            private set {
                lock (_lock) {
                    _isSimConnected = value;
                }
            }
        }

        /// <summary>
        /// Shutdown simconnect CX
        /// </summary>
        public void DisconnectFromSim() {
            lock (_lock) {
                // Free simConnect cx
                if (this.SimConnect != null) {
                    this.cancelSimWaitEvent?.Cancel();
                    if (this.simEventWaitTask?.Status < TaskStatus.RanToCompletion) {
                        //Wait for completion
                        this.simEventWaitTask.Wait(MSG_RCV_WAIT_TIME_SIM_MS);
                    }

                    try {
                        this.simEventWaitTask?.Dispose();
                        this.SimConnect.Dispose();
                        this.cancelSimWaitEvent?.Dispose();
                    }
                    catch { }
                    this.simEventWaitTask = null;
                    this.SimConnect = null;
                    this.cancelSimWaitEvent = null;

                    //Send cx callback
                    this.SimDisconnected?.Invoke(this, EventArgs.Empty);
                }
                this.IsSimConnected = EConnectionStatus.Disconnected;
            }
        }

        /// <summary>
        /// Connect to MSFS
        /// </summary>
        public void ConnectToSim() {
            try {
                if (IsSimConnected != EConnectionStatus.Disconnected) {
                    return;
                }
                lock (_lock) {
                    if (IsSimConnected != EConnectionStatus.Disconnected) {
                        return;
                    }
                    this.IsSimConnected = EConnectionStatus.Pending;

                    this.SimConnect = new SimConnect(System.Reflection.Assembly.GetExecutingAssembly().GetName().Name, IntPtr.Zero, 0, this.simEventReady, 0);

                    this.SimConnect.OnRecvOpen += SimConnect_OnRecvOpen;

                    this.cancelSimWaitEvent = new();
                    this.simEventWaitTask = Task.Run(ReceiveMessages, cancelSimWaitEvent!.Token);
                }

                // Make sure we actually connect
                System.Threading.Tasks.Task.Delay(SIM_CONNECT_TIMEOUT_MS).ContinueWith((_) => { if (this.IsSimConnected != EConnectionStatus.Connected) { DisconnectFromSim(); } });
            }
            catch (COMException ex) {
                DisconnectFromSim();
            }
        }

        private void SimConnect_OnRecvSimobjectData(SimConnect sender, SIMCONNECT_RECV_SIMOBJECT_DATA data) {
            if (data.dwData != null && data.dwData.Length > 0 &&
                data.dwData[0] != null &&
                data.dwData[0].GetType().IsAssignableTo(typeof(PlaneStatus))) {
                this.SimUpdate?.Invoke(this, new SimDataEventArgs((PlaneStatus)data.dwData[0]));
            }
        }

        private void SimConnect_OnRecvQuit(SimConnect sender, SIMCONNECT_RECV data) {
            DisconnectFromSim();
        }

        private void SimConnect_OnRecvOpen(SimConnect sender, SIMCONNECT_RECV_OPEN data) {
            this.IsSimConnected = EConnectionStatus.Connected;
            //Send cx callback
            this.SimConnected?.Invoke(this, EventArgs.Empty);

            // Set Handlers
            if (this.SimConnect != null) {
                this.SimConnect.OnRecvQuit += SimConnect_OnRecvQuit;
                this.SimConnect.OnRecvSimobjectData += SimConnect_OnRecvSimobjectData;

                this.SimConnect.RegisterDataDefineStruct<PlaneStatus>(ESimDataDefinition.StructMSFS);
                this.SimConnect.AddToDataDefinition(ESimDataDefinition.StructMSFS, "FLAPS HANDLE PERCENT", "percent", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
                this.SimConnect.AddToDataDefinition(ESimDataDefinition.StructMSFS, "GEAR POSITION:0", "percent", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
                this.SimConnect.AddToDataDefinition(ESimDataDefinition.StructMSFS, "IS ANY INTERIOR LIGHT ON", "bool", SIMCONNECT_DATATYPE.INT32, 0, SimConnect.SIMCONNECT_UNUSED);

                this.SimConnect.RequestDataOnSimObject(ESimDataRequest.RequestPlaneStatus, ESimDataDefinition.StructMSFS, SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_PERIOD.SECOND, SIMCONNECT_DATA_REQUEST_FLAG.CHANGED, 0, 0, 0);
            }
        }

        private void ReceiveMessages() {
            int sig;
            var waitHandles = new WaitHandle[] { this.simEventReady, cancelSimWaitEvent!.Token.WaitHandle };
            try {
                while (!cancelSimWaitEvent!.Token.IsCancellationRequested) {
                    sig = WaitHandle.WaitAny(waitHandles, SIM_CONNECT_TIMEOUT_MS);
                    if (sig == 0 && IsSimConnected != EConnectionStatus.Disconnected) {
                        // Event received + sim connected 
                        // => pump message (sync call -> blocking !)
                        this.SimConnect?.ReceiveMessage();
                    }
                }
            }
            catch (ObjectDisposedException) { }
            catch (Exception ex) {
                Task.Run(DisconnectFromSim);
            }
        }
    }
}