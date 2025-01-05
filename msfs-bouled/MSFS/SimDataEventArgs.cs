using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace msfs_bouled.MSFS {
    public class SimDataEventArgs : EventArgs {
        public PlaneStatus PlaneStatus { get; set; }

        public SimDataEventArgs(PlaneStatus status) => PlaneStatus = status;
    }
}
