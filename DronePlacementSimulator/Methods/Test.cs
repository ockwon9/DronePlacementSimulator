using System;
using System.Collections.Generic;

namespace DronePlacementSimulator
{
    delegate int Del(List<Station> stationList, ref Counter counter, OHCAEvent ohca);

    class Test
    {
        private Del policy;

        private double expectedSurvivalRate;
        private int missCount;
        private PathPlanner pathPlanner;

        public Test()
        {
            expectedSurvivalRate = 0;
            missCount = 0;

            // TODO: Heavy workload
            pathPlanner = new PathPlanner();
        }

        public void SetPolicy(Del policy)
        {
            this.policy = policy;
        }

        public void Simulate(List<Station> stationList, Grid eventGrid)
        {
            Console.WriteLine("???");
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
                Console.WriteLine("???");
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
                        }
                    }
                }
            }

            expectedSurvivalRate = sum / Utils.SIMULATION_EVENTS;
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
    }
}
