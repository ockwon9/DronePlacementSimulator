using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DronePlacementSimulator
{
    class Station
    {
        private static int ID = 100;

        public int stationID;
        public float latitude;
        public float longitude;
        public List<Drone> droneList;

        public Station(float latitude, float longitude)
        {
            this.stationID = ID++;
            this.latitude = latitude;
            this.longitude = longitude;

            droneList = new List<Drone>();
        }
    }
}
