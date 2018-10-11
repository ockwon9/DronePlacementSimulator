using System.Collections.Generic;

namespace DronePlacementSimulator
{
    class Station
    {
        private static int ID = 100;

        public int stationID;
        public double kiloX, kiloY;
        public int pixelX, pixelY;
        public List<Drone> droneList;
        public int eventCount;

        public Station(double kiloX, double kiloY, int drones)
        {
            this.stationID = ID++;
            this.kiloX = kiloX;
            this.kiloY = kiloY;
            this.pixelX = Utils.TransformKiloXToPixel(kiloX);
            this.pixelY = Utils.TransformKiloYToPixel(kiloY);

            droneList = new List<Drone>();
            for (int i = 0; i < drones; i++)
            {
                droneList.Add(new Drone(stationID));
            }
        }

        public Station(Station copy)
        {
            this.stationID = copy.stationID;
            this.kiloX = copy.kiloX;
            this.kiloY = copy.kiloY;
            this.pixelX = copy.pixelX;
            this.pixelY = copy.pixelY;

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
            return (this.kiloX == other.kiloX && this.kiloY == other.kiloY);
        }

        public void SetLocation(double kiloX, double kiloY)
        {
            this.kiloX = kiloX;
            this.kiloY = kiloY;
            this.pixelX = Utils.TransformKiloXToPixel(kiloX);
            this.pixelY = Utils.TransformKiloYToPixel(kiloY);
        }
    }
}
