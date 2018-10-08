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
        private Grid eventGrid;
        private Del policy;

        private double expectedSurvivalRate;
        private int missCount;

        public Test(List<Station> stationList, Grid eventGrid, Del policy)
        {
            this.stationList = new List<Station>(stationList);
            this.eventGrid = new Grid(eventGrid);
            this.policy = policy;

            expectedSurvivalRate = 0;
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
            DateTime currentTime = new DateTime(2018, 1, 1);
            double sum = 0;
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

                current.Flush(currentTime);
                int dispatchFrom = policy(stationList, ref current, e);
                e.assignedStationId = dispatchFrom;
                if (dispatchFrom == -1)
                {
                    missCount++;
                }
                else
                {
                    double distance = Utils.GetDistance(e.kiloX, e.kiloY, stationList[dispatchFrom].kiloX, stationList[dispatchFrom].kiloY);
                    if (distance > Utils.GOLDEN_TIME)
                    {
                        missCount++;
                    }
                    else
                    {
                        current.Dispatch(dispatchFrom, e.occurrenceTime);
                        sum += CalcauteSurvivalRate(distance);
                    }
                }
            }
            expectedSurvivalRate = sum / (Utils.SIMULATION_EVENTS - missCount);
        }

        private double CalcauteSurvivalRate(double distance)
        {            
            return 0.7 - (0.1 * distance);
        }

        double nextEventTime(double arrivalRate)
        {
            unchecked
            {
                double rand = new Random().NextDouble() / 1.0000000000000000001;
                return -Math.Log(1.0 - rand) / arrivalRate;
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
