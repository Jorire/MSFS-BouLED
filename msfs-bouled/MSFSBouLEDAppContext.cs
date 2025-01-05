using System.Windows.Forms;
using msfs_bouled.USB;

namespace msfs_bouled
{
    internal enum EAppStatus
    {
        OFF,
        WARMUP,
        ON
    }

    /// <summary>
    /// App in systray 
    /// </summary>
    internal class MSFSBouLEDAppContext : ApplicationContext 
    {
        private readonly System.ComponentModel.IContainer mComponents;
        private readonly NotifyIcon mNotifyIcon;
        private readonly ContextMenuStrip mContextMenu;
        private readonly ToolStripMenuItem mExitApplication;

        public MSFSBouLEDAppContext()
        {
            mComponents = new System.ComponentModel.Container();

            // Systray icon
            mNotifyIcon = new NotifyIcon(this.mComponents) {
                Icon = Assets.bouLED_off,
                Text = "MSFS - BouLED",
                Visible = true
            };

            // Basic context menu to exit app
            mContextMenu = new ContextMenuStrip();
            mExitApplication = new ToolStripMenuItem();
            mNotifyIcon.ContextMenuStrip = mContextMenu;
            
            // Notification on systray icon click
            mNotifyIcon.Click += new EventHandler(notifyIcon_Click);
            mExitApplication.Text = "Exit";
            mExitApplication.Click += MExitApplication_Click;
            mContextMenu.Items.Add(mExitApplication);

            // Initialize background worker
            SyncLEDService svc = SyncLEDService.GetInstance();
            svc.StatusChanged += App_StatusChanged;
            svc.Initialize();
            this.SetNotifyIcon(svc);
        }

        /// <summary>
        /// Summary app status (sim connected vs controller detected vs ????)
        /// </summary>
        /// <param name="sender">SyncLEDService emitting the event</param>
        /// <param name="e">Nothing</param>
        private void App_StatusChanged(object? sender, EventArgs e)
        {
            if (sender != null && sender.GetType() == typeof(SyncLEDService))
            {
                SetNotifyIcon((SyncLEDService)sender);
            }
        }

        private void SetNotifyIcon(SyncLEDService svc) {
            switch (getAppStatus(svc)) {
                case EAppStatus.OFF:
                    mNotifyIcon.Icon = Assets.bouLED_off;
                    break;
                case EAppStatus.WARMUP:
                    mNotifyIcon.Icon = Assets.bouLED_warn;
                    break;
                case EAppStatus.ON:
                    mNotifyIcon.Icon = Assets.bouLED_on;
                    break;
            }
        }
        
        /// <summary>
        /// Show a notification to display app status
        /// Sim connection and controller status are displayed to the user
        /// </summary>
        void notifyIcon_Click(object? sender, EventArgs e)
        {
            Type t = e.GetType();
            if (t.Equals(typeof(MouseEventArgs))) {
                MouseEventArgs mouse = (MouseEventArgs)e;
                if (mouse.Button == MouseButtons.Right) {
                    return;
                }
            }

            SyncLEDService svc = SyncLEDService.GetInstance();          
            mNotifyIcon.BalloonTipText = "";            
            if (svc.IsControllerConnected) {
                foreach (HIDDevice device in svc.USBService.Controllers) {
                    mNotifyIcon.BalloonTipText += $"{device.Manufacturer} - {device.Product}\n";
                }
            }
            else { 
                mNotifyIcon.BalloonTipText += "Controller(s) not detected\n";
            }

            if (svc.IsSimconnectConnected){
                mNotifyIcon.BalloonTipText += "MSFS connected";
            }
            else{
                mNotifyIcon.BalloonTipText += "MSFS not connected";
            }

            switch (getAppStatus(svc))
            {
                case EAppStatus.OFF:
                    mNotifyIcon.BalloonTipIcon = ToolTipIcon.Error;
                    break;
                case EAppStatus.WARMUP:
                    mNotifyIcon.BalloonTipIcon = ToolTipIcon.Warning;
                    break;
                case EAppStatus.ON:
                    mNotifyIcon.BalloonTipIcon = ToolTipIcon.Info;
                    break;
            }
            mNotifyIcon.ShowBalloonTip(20000);
        }

        /// <summary>
        /// Get app status according sim connection and controller status
        /// </summary>
        private EAppStatus getAppStatus(SyncLEDService svc)
        {
            if (svc.IsControllerConnected && svc.IsSimconnectConnected)
            {
                return EAppStatus.ON;
            }
            else
            {
                if (!svc.IsControllerConnected && !svc.IsSimconnectConnected)
                {
                    return EAppStatus.OFF;
                }
                else
                {
                    return EAppStatus.WARMUP;
                }
            }
        }

        /// <summary>
        /// Exit handler 
        /// Stop the background worker
        /// </summary>
        private void MExitApplication_Click(object? sender, EventArgs e)
        {
            SyncLEDService.GetInstance().Shutdown();
            base.ExitThreadCore();
        }
    }
}
