using System.Collections.Generic;
using System.Device.Location;

namespace DronePlacementSimulator
{
    class Station
    {
        private static int ID = 100;

        public int stationID;
        public GeoCoordinate geo;
        public int pixelX, pixelY;
        public List<Drone> droneList;
        public int eventCount;

        public Station()
        {
            this.geo = new GeoCoordinate(0, 0);
            this.pixelX = 0;
            this.pixelY = 0;
            this.eventCount = 0;

            droneList = new List<Drone>();
        }

        public Station(GeoCoordinate geo, int drones)
        {
            this.stationID = ID++;
            this.geo = geo;
            this.pixelX = Utils.TransformLonToPixel(geo.Longitude);
            this.pixelY = Utils.TransformLatToPixel(geo.Latitude);
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
            this.geo = copy.geo;
            this.pixelX = copy.pixelX;
            this.pixelY = copy.pixelY;
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
            return (this.geo == other.geo);
        }

        public void SetLocation(GeoCoordinate geo)
        {
            this.geo = geo;
            this.pixelX = Utils.TransformLonToPixel(geo.Longitude);
            this.pixelY = Utils.TransformLatToPixel(geo.Latitude);
        }
    }
}
