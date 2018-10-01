using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DronePlacementSimulator
{
    delegate int Del(ref List<Station> stationList, ref Counter counter, OHCAEvent ohca);

    class Test
    {
        private double expectedSurvivalRate;
        private Del policy;
        
        public Test(ref List<Station> stationList, ref List<OHCAEvent> eventList, Del policy)
        {
            this.policy = policy;
            this.expectedSurvivalRate = ComputeSurvivalRate(ref stationList, ref eventList, policy);
        }

        public double getExpectedSurvivalRate()
        {
            return expectedSurvivalRate;
        }

        public double ComputeSurvivalRate(ref List<Station> stationList, ref List<OHCAEvent> eventList, Del policy)
        {
            int n = stationList.Count;
            int[] initialCount = new int[n];
            for (int i = 0; i < n; i++)
            {
                initialCount[i] = stationList[i].droneList.Count;
            }
            Counter current = new Counter(n, ref initialCount);
            int numEvents = eventList.Count;
            List<OHCAEvent> sortedEventList = eventList.OrderBy(o => o.occurrenceTime).ToList();
            double sum = 0;
            
            foreach (var ohca in sortedEventList)
            {
                int dispatchFrom = policy(ref stationList, ref current, ohca);
                if (dispatchFrom >= 0)
                {
                    Console.WriteLine("s.kiloX = " + stationList[dispatchFrom].kiloX + ", s.kiloY = " + stationList[dispatchFrom].kiloY + ", e.kiloX = " + ohca.kiloX + ", e.kiloY = " + ohca.kiloY);
                    Console.WriteLine("survival rate = " + SurvivalRate(stationList[dispatchFrom], ohca));
                    current.Dispatch(dispatchFrom, ohca.occurrenceTime);
                    sum += SurvivalRate(stationList[dispatchFrom], ohca);
                }
            }
            
            return sum / numEvents;
        }

        public double SurvivalRate(Station s, OHCAEvent e)
        {
            /* SurvivalRate is 0 when the time to arrival is greater than GOLDEN_TIME */
            double d = Utils.getDistance(s.kiloX, s.kiloY, e.kiloX, e.kiloY);
            return 0.7f - 0.1f * (d > Utils.GOLDEN_TIME ? 7 : d);
        }
    }
}
