using System;
using System.Collections.Generic;

namespace DronePlacementSimulator
{
    delegate int Del(List<Station> stationList, ref Counter counter, OHCAEvent ohca);

    public class Simulator
    {
        private Del policy;
        private PathPlanner pathPlanner;
        
        private double expectedSurvivalRate;
        private int missCount;

        public Simulator()
        {
            expectedSurvivalRate = 0;
            missCount = 0;

            // TODO: It is too heavy workload. Anyway, we load all data on the memory (about 6.4 GB).
            pathPlanner = new PathPlanner();
        }

        public void Simulate(List<Station> stationList, Grid eventGrid)
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
                int selectedIndex = eventGrid.SelectCell();
                double kiloX = eventGrid.cells[selectedIndex][0];
                double kiloY = eventGrid.cells[selectedIndex][1];

                double nextMin = nextEventTime(Utils.ARRIVAL_RATE);
                currentTime = currentTime.AddMinutes(nextMin);
                DateTime occurrenceTime = currentTime;

                OHCAEvent e = new OHCAEvent(kiloX + 0.5 * Utils.UNIT, kiloY + 0.5 * Utils.UNIT, occurrenceTime);

                current.Flush(currentTime);
                int dispatchFrom = policy(stationList, ref current, e);
                e.assignedStationId = dispatchFrom;

                if (dispatchFrom == -1)
                {
                    missCount++;
                }
                else
                {
                    double flightTime = pathPlanner.CalcuteFlightTime(e.kiloX, e.kiloY, stationList[dispatchFrom].kiloX, stationList[dispatchFrom].kiloY);
                    if (flightTime > Utils.GOLDEN_TIME)
                    {
                        missCount++;
                    }
                    else
                    {
                        current.Dispatch(dispatchFrom, e.occurrenceTime);
                        sum += CalcauteSurvivalRate(flightTime);
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

        public void SetPolicy(Del policy)
        {
            this.policy = policy;
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
