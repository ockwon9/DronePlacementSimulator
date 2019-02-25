using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DronePlacementSimulator
{
    class DispatchFailure
    {
        public double lat, lon;

        public DispatchFailure(double lat, double lon)
        {
            this.lat = lat;
            this.lon = lon;
        }
    }
}
