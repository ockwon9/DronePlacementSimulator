using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DronePlacementSimulator
{
    static class Policy
    {
        public static int NearestStation(List<Station> stationList, ref Counter counter, OHCAEvent e, ref PathPlanner pathPlanner)
        {
            int n = stationList.Count;
            int[] index = new int[n];
            double[] distance = new double[n];

            for (int i = 0; i < n; i++)
            {
                Station s = stationList[i];
                index[i] = i;
                distance[i] = Utils.GetDistance(s.kiloX, s.kiloY, e.kiloX, e.kiloY);

                for (int j = i; j > 0; j--)
                {
                    if (distance[j] < distance[j - 1])
                    {
                        int temp = index[j];
                        index[j] = index[j - 1];
                        index[j - 1] = temp;
                        double tem = distance[j];
                        distance[j] = distance[j - 1];
                        distance[j - 1] = tem;
                    }
                }
            }

            int k = 0;
            counter.Flush(e.occurrenceTime);
            while (k < n && counter.whenReady[index[k]].Count == stationList[index[k]].droneList.Count)
            {
                k++;
            }

            if (k == n)
            {
                return -1;
            }

            return index[k];
        }

        public static int HighestSurvivalRateStation(List<Station> stationList, ref Counter counter, OHCAEvent e, ref PathPlanner pathPlanner)
        {
            int resultIndex = -1;
            double maxSurvivalRate = Double.NegativeInfinity;;
            foreach (Station s in stationList)
            {
                double survivalRate = RUBIS.GetSurvivalRate(stationList, ref counter, s, e, ref pathPlanner) - RUBIS.GetPotential(stationList, ref counter, s, e, ref pathPlanner);
                if (survivalRate > maxSurvivalRate)
                {
                    maxSurvivalRate = survivalRate;
                    resultIndex = stationList.IndexOf(s);
                }
            }

            return resultIndex;
        }
    }
}
