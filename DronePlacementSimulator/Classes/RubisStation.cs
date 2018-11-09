using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DronePlacementSimulator
{
    class RubisStation : Station
    {
        public List<RubisCell> cellList;
        public double pdfSum;

        public RubisStation(double kiloX, double kiloY, int drones) : base (kiloX, kiloY, drones)
        {
            this.kiloX = kiloX;
            this.kiloY = kiloY;
            this.pixelX = Utils.TransformKiloXToPixel(kiloX);
            this.pixelY = Utils.TransformKiloYToPixel(kiloY);
            this.eventCount = 0;

            droneList = new List<Drone>();
            for (int i = 0; i < drones; i++)
            {
                droneList.Add(new Drone(stationID));
            }

            cellList = new List<RubisCell>();
            pdfSum = 0.0;
        }

        public RubisStation(Station s)
        {
            this.stationID = s.stationID;
            this.kiloX = s.kiloX;
            this.kiloY = s.kiloY;
            this.pixelX = s.pixelX;
            this.pixelY = s.pixelY;
            this.eventCount = s.eventCount; ;

            droneList = new List<Drone>();
            for (int i = 0; i < s.droneList.Count; i++)
            {
                this.droneList.Add(new Drone(s.stationID));
            }

            cellList = new List<RubisCell>();
            pdfSum = 0.0;
        }

        public RubisStation(RubisStation s)
        {
            this.stationID = s.stationID;
            this.kiloX = s.kiloX;
            this.kiloY = s.kiloY;
            this.pixelX = s.pixelX;
            this.pixelY = s.pixelY;
            this.eventCount = s.eventCount; ;

            droneList = new List<Drone>();
            for (int i = 0; i < s.droneList.Count; i++)
            {
                this.droneList.Add(new Drone(s.stationID));
            }

            cellList = new List<RubisCell>();
            foreach(RubisCell c in s.cellList)
            {
                this.cellList.Add(new RubisCell(c));
            }
            pdfSum = s.pdfSum;
        }
    }
}
