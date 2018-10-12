using System;
using System.Collections.Generic;

namespace DronePlacementSimulator
{
    delegate int Del(List<Station> stationList, ref Counter counter, OHCAEvent ohca);

    class Simulator
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
            
            int eventCount = 0;
            double sum = 0;
            Random rand = new Random();
            while (eventCount < Utils.SIMULATION_EVENTS)
            {
                currentTime.AddMinutes(1.0);
                double randVal = rand.NextDouble();
                for (int i = 0; i < eventGrid.lambda.Length; i++)
                {
                    for (int j = 0; j < eventGrid.lambda[i].Length; j++)
                    {
                        if (randVal < eventGrid.lambda[i][j])
                        {
                            OHCAEvent e = new OHCAEvent((j + 0.5) * Utils.LAMBDA_PRECISION, (i + 0.5) * Utils.LAMBDA_PRECISION, currentTime);

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
                                    sum += CalculateSurvivalRate(flightTime);
                                }
                            }

                            eventCount++;
                            if (eventCount % 100000 == 0)
                            {
                                Console.WriteLine("Simulation progress: {0:F1}%", (double)eventCount / (double)Utils.SIMULATION_EVENTS * 100);
                            }
                        }
                    }
                }
            }

            expectedSurvivalRate = sum / Utils.SIMULATION_EVENTS;
        }

        public void SetPolicy(Del policy)
        {
            this.policy = policy;
        }

        private double CalculateSurvivalRate(double distance)
        {            
            return 0.7 - (0.1 * distance);
        }

        public double GetExpectedSurvivalRate()
        {
            return expectedSurvivalRate;
        }

        public int GetMissCount()
        {
            return missCount;
        }

        public ref PathPlanner GetPathPlanner()
        {
            return ref pathPlanner;
        }
    }
}
