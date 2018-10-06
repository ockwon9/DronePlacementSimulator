using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DronePlacementSimulator
{
    public static class Policy
    {
        public static int NearestStation(List<Station> stationList, ref Counter counter, OHCAEvent ohca)
        {
            int n = stationList.Count;
            int[] index = new int[n];
            double[] distance = new double[n];

            for (int i = 0; i < n; i++)
            {
                Station s = stationList[i];
                index[i] = i;
                distance[i] = Utils.GetDistance(s.kiloX, s.kiloY, ohca.kiloX, ohca.kiloY);

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

        //TODO: How to refer the Counter object?
        public static int HighestSurvalRateStation(List<Station> stationList, ref Counter counter, OHCAEvent e)
        {
            //counter.flush(ohca.occurrenceTime);
            int resultIndex = -1;
            double maxSurvivalRate = Double.PositiveInfinity;
            foreach (Station s in stationList)
            {
                double survivalRate = Rubis.GetSurvivalRate(stationList, s, e);
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
