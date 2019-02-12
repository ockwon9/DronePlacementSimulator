using System.Collections.Generic;

namespace DronePlacementSimulator
{
    class Station
    {
        private static int ID = 100;

        public int stationID;
        public double lat, lon;
        public int pixelRow, pixelCol;
        public List<Drone> droneList;
        public int eventCount;

        public Station()
        {
            this.lat = 0.0;
            this.lon = 0.0;
            this.pixelRow = 0;
            this.pixelCol = 0;
            this.eventCount = 0;

            droneList = new List<Drone>();
        }

        public Station(double lat, double lon, int drones)
        {
            this.stationID = ID++;
            this.lat = lat;
            this.lon = lon;
            this.pixelRow = Utils.TransformLatToPixel(lat);
            this.pixelCol = Utils.TransformLonToPixel(lon);
            this.eventCount = 0;

            droneList = new List<Drone>();
            for (int i = 0; i < drones; i++)
            {
                droneList.Add(new Drone(stationID));
            }
        }

        public Station(Station copy)
        {
            this.stationID = copy.stationID;
            this.lat = copy.lat;
            this.lon = copy.lon;
            this.pixelRow = copy.pixelRow;
            this.pixelCol = copy.pixelCol;
            this.eventCount = 0;

            droneList = new List<Drone>();
            for (int i = 0; i < copy.droneList.Count; i++)
            {
                droneList.Add(new Drone(copy.stationID));
            }
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
            return (this.lat == other.lat && this.lon == other.lon);
        }

        public void SetLocation(double lat, double lon)
        {
            this.lat = lat;
            this.lon = lon;
            this.pixelRow = Utils.TransformLatToPixel(lat);
            this.pixelCol = Utils.TransformLonToPixel(lon);
        }
    }
}
