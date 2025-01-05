using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace msfs_bouled.MSFS {
    enum ESimDataDefinition {
        StructMSFS,
    }

    enum ESimDataRequest {
        RequestPlaneStatus,
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    public struct PlaneStatus {
        public double flapsPositionPct;
        public double gearPositionPct;
        public bool isInteriorLightOn;
        public override string ToString() {
            return $"flaps {flapsPositionPct} gear {gearPositionPct} isInteriorLightOn {isInteriorLightOn}";
        }
    };
}
