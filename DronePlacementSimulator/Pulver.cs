using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gurobi;

namespace DronePlacementSimulator
{
    class Pulver
    {
        Grid grid;
        int[] numDronesAtStation;
        Counter counter;

        public Pulver(double minLat, double minLon, double maxLat, double maxLon, int numLat, int numLon, ref List<OHCAEvent> eventList, ref List<Station> stationList)
        {
            this.grid = new Grid(minLat, minLon, maxLat, maxLon, numLat, numLon, ref eventList);
            int n = stationList.Count;
            this.numDronesAtStation = new int[n];
            for (int i = 0; i < n; i++)
            {
                numDronesAtStation[i] = 0;
            }

            this.counter = new Counter(stationList.Count, ref numDronesAtStation);
        }
    }
}
