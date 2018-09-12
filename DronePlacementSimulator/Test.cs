using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DronePlacementSimulator
{
    delegate int Del(ref List<Station> stationList, OHCAEvent ohca);
    
    class Test
    {
        public double expectedSurvivalRate;
        public Del policy;
        
        public Test(ref List<Station> stationList, ref List<OHCAEvent> eventList, Del policy)
        {
            this.policy = policy;
            this.expectedSurvivalRate = ComputeSurvivalRate(ref stationList, ref eventList, policy);
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
                int dispatchFrom = policy(ref stationList, ohca);
                if (dispatchFrom >= 0)
                {
                    sum += SurvivalRate(stationList[dispatchFrom], ohca);
                }
            }
            
            return sum / numEvents;
        }

        public double SurvivalRate(Station s, OHCAEvent e)
        {
            return 0.7f - 0.01f * Distance(s.latitude, s.longitude, e.latitude, e.longitude);
        }

        public double Distance(double x1, double y1, double x2, double y2)
        {
            return Math.Sqrt((x1 - x2) * (x1 - x2) + (y1 - y2) * (y1 - y2));
        }
    }
}
