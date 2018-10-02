using System.Collections.Generic;

namespace DronePlacementSimulator
{
    public class Station
    {
        private static int ID = 100;

        public int stationID;
        public double kiloX, kiloY;
        public int pixelX, pixelY;
        public List<Drone> droneList;
        public int eventCount;

        public Station(double kiloX, double kiloY)
        {
            this.stationID = ID++;
            this.kiloX = kiloX;
            this.kiloY = kiloY;
            this.pixelX = Utils.transformKiloXToPixel(kiloX);
            this.pixelY = Utils.transformKiloYToPixel(kiloY);
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
            return (this.kiloX == other.kiloX && this.kiloY == other.kiloY);
        }

        public void setLocation(double kiloX, double kiloY)
        {
            this.kiloX = kiloX;
            this.kiloY = kiloY;
            this.pixelX = Utils.transformKiloXToPixel(kiloX);
            this.pixelY = Utils.transformKiloYToPixel(kiloY);
        }
    }
}
