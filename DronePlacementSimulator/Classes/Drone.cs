﻿namespace DronePlacementSimulator
{
    public class Drone
    {
        private static int ID = 1000;

        public int          droneID;
        public int          stationID;

        public Drone(int stationID)
        {
            this.droneID = ID++;
            this.stationID = stationID;
        }
    }
}
