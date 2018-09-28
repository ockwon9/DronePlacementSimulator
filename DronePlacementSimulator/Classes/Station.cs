﻿using System.Collections.Generic;

namespace DronePlacementSimulator
{
    class Station
    {
        private static int ID = 100;

        public int          stationID;
        public double       latitude, longitude;
        public int          x, y;
        public List<Drone>  droneList;

        public Station(double longitude, double latitude)
        {
            this.stationID = ID++;
            this.longitude = longitude;
            this.latitude = latitude;
            droneList = new List<Drone>();
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            Station objAsStation = obj as Station;
            if (objAsStation == null)
            {
                return false;
            }
            else
            {
                return Equals(objAsStation);
            }   
        }

        public override int GetHashCode()
        {
            return stationID;
        }

        public bool Equals(Station other)
        {
            if (other == null)
            {
                return false;
            }
            return (this.latitude == other.latitude && this.longitude == other.longitude);
        }
    }
}