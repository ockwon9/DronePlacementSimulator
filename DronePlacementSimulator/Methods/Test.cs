using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DronePlacementSimulator
{
    delegate int Del(List<Station> stationList, ref Counter counter, OHCAEvent ohca);

    class Test
    {
        private List<Station> stationList;
        private List<OHCAEvent> eventList;
        private Grid eventGrid;
        private Del policy;

        private double expectedSurvivalRate;
        private int missCount;

        public Test(List<Station> stationList, Grid eventGrid, Del policy)
        {
            this.stationList = new List<Station>(stationList);
            this.eventGrid = new Grid(eventGrid);
            this.policy = policy;

            expectedSurvivalRate = 0.0f;
            missCount = 0;
        }

        public void Simulate()
        {
            int n = stationList.Count;
            int[] initialCount = new int[n];
            for (int i = 0; i < n; i++)
            {
                initialCount[i] = stationList[i].droneList.Count;
            }

            Counter current = new Counter(ref initialCount);
            double sum = 0.0f;

            DateTime currentTime = new DateTime(2018, 1, 1);
            eventList = new List<OHCAEvent>();

            for (int i = 0; i<Utils.SIMULATION_EVENTS; i++)
            {
                OHCAEvent e = new OHCAEvent();
                double nextMin = nextEventTime(Utils.ARRIVAL_RATE);
                currentTime = currentTime.AddMinutes(nextMin);
                e.occurrenceTime = currentTime;
                int selectedIndex = eventGrid.SelectCell();

                double kiloX = eventGrid.cells[selectedIndex][0];
                double kiloY = eventGrid.cells[selectedIndex][1];

                e.SetLocation(kiloX + 0.5 * Utils.UNIT, kiloY + 0.5 * Utils.UNIT);
                eventList.Add(e);

                int dispatchFrom = policy(stationList, ref current, e);
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
                    sum += CalcauteSurvivalRate(stationList[dispatchFrom], e);
                }
            }

            DateTime max = eventList[Utils.SIMULATION_EVENTS - 1].occurrenceTime;
            DateTime min = eventList[0].occurrenceTime;
            double rate = Utils.SIMULATION_EVENTS / (max - min).TotalMinutes;
            Console.WriteLine("Arrival rate = " + rate);

            expectedSurvivalRate = sum / Utils.SIMULATION_EVENTS;
        }

        private double CalcauteSurvivalRate(Station s, OHCAEvent e)
        {            
            double d = Utils.GetDistance(s.kiloX, s.kiloY, e.kiloX, e.kiloY);
            if (d <= Utils.GOLDEN_TIME)
            {
                return 0.7f - (0.1f * d);
            }
            return 0.0f;
        }

        double nextEventTime(double arrivalRate)
        {
            unchecked
            {
                double rand = new Random().NextDouble() / 1.0000000000000000001f;
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

        public List<OHCAEvent> getEventList()
        {
            return eventList;
        }
    }
}
