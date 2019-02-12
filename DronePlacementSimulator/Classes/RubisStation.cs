using System.Collections.Generic;

namespace DronePlacementSimulator
{
    class RubisStation : Station
    {
        public List<RubisCell> cellList;
        public double pdfSum;

        public RubisStation(double lat, double lon, int drones) : base (lat, lon, drones)
        {
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

            cellList = new List<RubisCell>();
            pdfSum = 0.0;
        }

        public RubisStation(Station s)
        {
            this.stationID = s.stationID;
            this.lat = s.lat;
            this.lon = s.lon;
            this.pixelRow = s.pixelRow;
            this.pixelCol = s.pixelCol;
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
            this.lat = s.lat;
            this.lon = s.lon;
            this.pixelRow = s.pixelRow;
            this.pixelCol = s.pixelCol;
            this.eventCount = s.eventCount;

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
