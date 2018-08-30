using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DronePlacementSimulator
{
    class Rubis
    {
        Grid grid;
        Station[] stationArray;
        int[] numDronesAtStation;
        OHCAEvent[] eventArray;
        Counter counter;

        public Rubis(double minLat, double minLon, double maxLat, double maxLon, int numLat, int numLon, ref List<OHCAEvent> eventList, ref List<Station> stationList)
        {
            this.grid = new Grid(minLat, minLon, maxLat, maxLon, numLat, numLon, ref eventList);
            this.stationArray = stationList.ToArray();
            this.eventArray = eventList.ToArray();
            int n = stationArray.Length;
            this.numDronesAtStation = new int[n];
            for (int i = 0; i < n; i++)
            {
                numDronesAtStation[i] = 0;
            }

            this.counter = new Counter(stationArray.Length, ref numDronesAtStation);
        }

        public int RubisPolicy(ref List<Station> stationList, OHCAEvent ohca)
        {
            counter.flush(ohca.occurrenceTime);
            int index = 0;
            int highest = -1;
            double max = Double.PositiveInfinity;
            foreach (var station in stationList)
            {
                double temp = SurvivalRate(station, ohca) + PotentialNegativePart(station, ohca);
                if (temp > max)
                {
                    max = temp;
                    highest = index;
                }
                index++;
            }

            return highest;
        }

        public double SurvivalRate(Station s, OHCAEvent ohca)
        {
            return 1.0f;
        }

        public double PotentialNegativePart(Station s, OHCAEvent ohca)
        {
            return 0.0f;
        }
    }
}
