using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DronePlacementSimulator
{
    class Drone
    {
        private static int ID = 1000;
        public int droneID;
        public int stationID;
        public DateTime lastDepartureTime;
        public DateTime readyTime;
        public float lastDestX;
        public float lastDestY;

        public Drone(int droneID, int stationID)
        {
            this.droneID = ID++;
            this.stationID = stationID;
        }
    }
}
