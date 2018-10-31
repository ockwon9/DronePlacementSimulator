using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

namespace DronePlacementSimulator
{
    delegate int Del(List<Station> stationList, ref Counter counter, OHCAEvent ohca);

    class Simulator
    {
        private Del policy;
        private PathPlanner pathPlanner;
        
        private double expectedSurvivalRate;
        private int missCount;

        private List<OHCAEvent> simulatedEventList;

        public Simulator()
        {
            expectedSurvivalRate = 0;
            missCount = 0;
            pathPlanner = new PathPlanner();
            simulatedEventList = new List<OHCAEvent>();
        }

        public void Simulate(List<Station> stationList, Grid eventGrid)
        {
            if (File.Exists("simulation_events.csv"))
            {
                ReadSimulatedEvents();
            }
            else
            {
                MessageBox.Show("There is no simulated events file.", "Simulation", MessageBoxButtons.OK);
                return;
            }

            missCount = 0;
            int n = stationList.Count;
            int[] initialCount = new int[n];
            for (int i = 0; i < n; i++)
            {
                initialCount[i] = stationList[i].droneList.Count;
            }

            Counter current = new Counter(ref initialCount);
            double sum = 0;

            foreach (OHCAEvent e in simulatedEventList)
            {
                current.Flush(e.occurrenceTime);
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
            expectedSurvivalRate = sum / simulatedEventList.Count;
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

        private void ReadSimulatedEvents()
        {
            StreamReader reader = new StreamReader("simulation_events.csv");
            string line = reader.ReadLine();
            while (line != null)
            {
                string[] values = line.Split(',');
                double kiloX = double.Parse(values[0]);
                double kiloY = double.Parse(values[1]);
                string[] dateComponents = values[2].Split('-', ' ', ':');
                int year = int.Parse(dateComponents[0]);
                int month = int.Parse(dateComponents[1]);
                int day = int.Parse(dateComponents[2]);
                int hour = int.Parse(dateComponents[4]);
                if (dateComponents[3].Equals("PM"))
                {
                    hour += 12;
                }
                if (hour % 12 == 0)
                {
                    hour -= 12;
                }
                int minute = int.Parse(dateComponents[5]);
                int second = int.Parse(dateComponents[6]);
                DateTime occurenceTime = new DateTime(year, month, day, hour, minute, second);
                simulatedEventList.Add(new OHCAEvent(kiloX, kiloY, occurenceTime));
                line = reader.ReadLine();
            }
            reader.Close();
        }

        public int GetSimulatedEventsCount()
        {
            return simulatedEventList.Count;
        }
    }
}
