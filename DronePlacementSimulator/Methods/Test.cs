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
        private int missCount;

        public Test(ref List<Station> stationList, Grid eventGrid, Del policy)
        {
            this.policy = policy;
            missCount = 0;
            expectedSurvivalRate = Simulate(ref stationList, eventGrid, policy);
        }

        private double Simulate(ref List<Station> stationList, Grid eventGrid, Del policy)
        {
            int n = stationList.Count;
            int[] initialCount = new int[n];
            for (int i = 0; i < n; i++)
            {
                initialCount[i] = stationList[i].droneList.Count;
            }

            Counter current = new Counter(ref initialCount);
            double sum = 0;

            DateTime currentTime = new DateTime(2018, 1, 1);
            for(int i = 0; i<Utils.SIMULATION_EVENTS; i++)
            {
                OHCAEvent e = new OHCAEvent();
                double min = nextEventTime(Utils.ARRIVAL_RATE);
                e.occurrenceTime = currentTime.AddMinutes(min);
                int selectedIndex = eventGrid.SelectCell();
                e.kiloX = eventGrid.cells[selectedIndex][0];
                e.kiloY = eventGrid.cells[selectedIndex][1];

                int dispatchFrom = policy(ref stationList, ref current, e);
                if (dispatchFrom == -1)
                {
                    missCount++;
                }
                else
                {
                    double t = Utils.GetDistance(e.kiloX, e.kiloY, stationList[dispatchFrom].kiloX, stationList[dispatchFrom].kiloY);
                    if (t > 5.0f)
                    {
                        missCount++;
                    }

                    current.Dispatch(dispatchFrom, e.occurrenceTime);
                    sum += SurvivalRate(stationList[dispatchFrom], e);
                }
            }
            
            return sum / Utils.SIMULATION_EVENTS;
        }

        private double SurvivalRate(Station s, OHCAEvent e)
        {            
            double d = Utils.GetDistance(s.kiloX, s.kiloY, e.kiloX, e.kiloY);

            if (d > Utils.GOLDEN_TIME)
            {
                return 0;
            }
            else
            {
                return 0.7f - (0.1f * d);
            }
        }

        double nextEventTime(double arrivalRate)
        {
            unchecked
            {
                double rand = ((double)new Random().Next(100) / 101.0f);
                return -Math.Log(1.0f - rand) / arrivalRate;
            }
        }

        public double GetExpectedSurvivalRate()
        {
            return expectedSurvivalRate;
        }

        public int GetMissCount()
        {
            return missCount;
        }
    }
}
