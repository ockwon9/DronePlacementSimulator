using System.Collections.Generic;

namespace DronePlacementSimulator
{
    class StationDistancePair
    {
        public RubisStation station;
        public double distance;

        public StationDistancePair(RubisStation station, double distance)
        {
            this.station = station;
            this.distance = distance;
        }
    }

    class RubisCell : Cell
    {
        public double pdf;
        public double survivalRate;
        public List<StationDistancePair> stations;

        public RubisCell(Cell cell, double pdf)
        {
            this.lat = cell.lat;
            this.lon = cell.lon;
            this.row = cell.row;
            this.col = cell.col;
            this.pdf = pdf;
            survivalRate = 0.0;
            stations = new List<StationDistancePair>();
        }

        public RubisCell(RubisCell cell)
        {
            this.lat = cell.lat;
            this.lon = cell.lon;
            this.row = cell.row;
            this.col = cell.col;
            this.pdf = cell.pdf;
            survivalRate = cell.survivalRate;
            stations = new List<StationDistancePair>();
            foreach (StationDistancePair pair in cell.stations)
            {
                this.stations.Add(new StationDistancePair(pair.station, pair.distance));
            }
        }
    }
}
