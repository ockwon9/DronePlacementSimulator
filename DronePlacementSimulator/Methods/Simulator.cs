using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Windows.Shapes;
using Excel = Microsoft.Office.Interop.Excel;

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
            double sum = 0;
            Random rand = new Random();

            StreamReader reader = new StreamReader(File.OpenRead("\\simulationEventList.csv"));
            List<OHCAEvent> eventList = new List<OHCAEvent>();
            for (int k = 0; k < Utils.SIMULATION_EVENTS / Utils.EVENTS_IN_ONE_READ; k++)
            {
                for (int r = 0; r < Utils.EVENTS_IN_ONE_READ; r++)
                {
                    string line = reader.ReadLine();
                    string[] values = line.Split(',');
                    double kiloX = double.Parse(values[0]);
                    double kiloY = double.Parse(values[1]);
                    DateTime occurenceTime = DateTime.Parse(values[2]);
                    eventList.Add(new OHCAEvent(kiloX, kiloY, occurenceTime));
                }

                for (int i = 0; i < eventList.Count; i++)
                {
                    OHCAEvent e = eventList[i];

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
                eventList.Clear();
            }

            expectedSurvivalRate = sum / Utils.SIMULATION_EVENTS;
        }
        private static void ReleaseExcelObject(object obj)
        {
            try
            {
                if (obj != null)
                {
                    Marshal.ReleaseComObject(obj);
                    obj = null;
                }
            }
            catch (Exception ex)
            {
                obj = null;
                throw ex;
            }
            finally
            {
                GC.Collect();
            }
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
