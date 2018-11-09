using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

namespace DronePlacementSimulator
{
    enum Policy
    {
        NearestStationFirst,
        HighestSurvivalRateStationFirst
    }

    class Simulator
    {
        private Policy policy;
        private PathPlanner pathPlanner;
        
        private double expectedSurvivalRate;
        private int missCount;

        private List<OHCAEvent> simulatedEventList;
        private List<RubisCell> rCellList;
        private List<RubisStation> rStationList;

        public Simulator()
        {
            expectedSurvivalRate = 0;
            missCount = 0;
            pathPlanner = new PathPlanner();
            simulatedEventList = new List<OHCAEvent>();
        }

        public void Simulate(List<Station> stationList, Grid eventGrid)
        {
            // Instance Test
            foreach(Station s in stationList)
            {
                s.droneList.Clear();
                s.droneList.Add(new Drone(1));
                s.droneList.Add(new Drone(1));
                s.droneList.Add(new Drone(1));
                s.droneList.Add(new Drone(1));
            }

            if (File.Exists("simulation_events.csv") && simulatedEventList.Count == 0)
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

            if(policy == Policy.HighestSurvivalRateStationFirst)
            {
                SetRubisMethod(eventGrid, stationList);
            }

            int iteration = 0;
            foreach (OHCAEvent e in simulatedEventList)
            {
                current.Flush(e.occurrenceTime);
                int dispatchFrom = policy == Policy.NearestStationFirst ? 
                    GetNearestStation(stationList, ref current, e) : 
                    GetHighestSurvivalRateStation(ref current, e);
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

                if (iteration++% 100000 == 0)
                {
                    Console.WriteLine("[" + iteration + "] time = " + e.occurrenceTime);
                }
            }
            expectedSurvivalRate = sum / simulatedEventList.Count;
        }
        
        private int GetNearestStation(List<Station> stationList, ref Counter counter, OHCAEvent e)
        {
            int n = stationList.Count;
            int[] index = new int[n];
            double[] distance = new double[n];

            for (int i = 0; i < n; i++)
            {
                Station s = stationList[i];
                index[i] = i;
                distance[i] = Utils.GetDistance(s.kiloX, s.kiloY, e.kiloX, e.kiloY);

                for (int j = i; j > 0; j--)
                {
                    if (distance[j] < distance[j - 1])
                    {
                        int temp = index[j];
                        index[j] = index[j - 1];
                        index[j - 1] = temp;
                        double tem = distance[j];
                        distance[j] = distance[j - 1];
                        distance[j - 1] = tem;
                    }
                }
            }

            int k = 0;
            counter.Flush(e.occurrenceTime);
            while (k < n && counter.whenReady[index[k]].Count == stationList[index[k]].droneList.Count)
            {
                k++;
            }

            if (k == n)
            {
                return -1;
            }

            return index[k];
        }
        
        private int GetHighestSurvivalRateStation(ref Counter counter, OHCAEvent e)
        {
            int resultIndex = -1;
            double maxSurvivalRate = Double.NegativeInfinity;
            double survivalRate = 0.0;

            counter.Flush(e.occurrenceTime);
            foreach (RubisStation s in rStationList)
            {
                double distance = pathPlanner.CalcuteFlightTime(e.kiloX, e.kiloY, s.kiloX, s.kiloY);
                if (distance <= Utils.GOLDEN_TIME)
                {
                    double potential = CalculatePotential(s, counter);
                    survivalRate = CalculateSurvivalRate(distance) - potential;
                    if (survivalRate > maxSurvivalRate)
                    {
                        maxSurvivalRate = survivalRate;
                        resultIndex = rStationList.IndexOf(s);
                    }
                }
            }
            
            return resultIndex;
        }

        private double CalculatePotential(RubisStation s, Counter counter)
        {
            double prev = GetOverallSurvivalRate(s, counter, false);
            double next = GetOverallSurvivalRate(s, counter, true);
            return prev - next;
        }

        private double GetOverallSurvivalRate(RubisStation targetStation, Counter counter, bool dispatch)
        {
            RubisStation tempStation = new RubisStation(targetStation);
            double overallSum = 0.0;

            int[] ready = new int[rStationList.Count];
            for (int i = 0; i < rStationList.Count; i++)
            {
                ready[i] = rStationList[i].droneList.Count - counter.whenReady[i].Count;
            }

            if (dispatch == true)
            {
                int index = rStationList.IndexOf(tempStation);
                ready[index]--;
            }

            foreach (RubisCell cell in tempStation.cellList)
            {
                double pSum = 0.0;
                foreach (StationDistancePair pair in cell.stations)
                {
                    RubisStation s = pair.station;
                    int index = rStationList.IndexOf(s);
                    if(ready[index] > 0)
                    {
                        double prob = (1 - pSum) * ProbabilityMassFunction(ready[index] - 1, s.pdfSum);
                        pSum += prob;
                        cell.survivalRate = cell.survivalRate + (prob * CalculateSurvivalRate(pair.distance));
                    }
                }
                overallSum += cell.survivalRate;
                //Console.WriteLine("overallSum = " + overallSum);
            }

            return overallSum / tempStation.cellList.Count;
        }

        public void SetRubisMethod(Grid eventGrid, List<Station> stationList)
        {
            rCellList = new List<RubisCell>();
            foreach (Cell c in eventGrid.cells)
            {
                rCellList.Add(new RubisCell(c, eventGrid.lambda[c.intX][c.intY]));
            }

            // Finds reachable stations for each cell
            rStationList = new List<RubisStation>();
            foreach (Station s in stationList)
            {
                rStationList.Add(new RubisStation(s));
            }

            foreach (RubisCell cell in rCellList)
            {
                foreach (RubisStation s in rStationList)
                {
                    double distance = pathPlanner.CalcuteFlightTime(cell.kiloX, cell.kiloY, s.kiloX, s.kiloY);
                    if (distance <= Utils.GOLDEN_TIME)
                    {
                        cell.stations.Add(new StationDistancePair(s, distance));
                        s.cellList.Add(cell);
                    }
                }
            }

            // Sorts stationList ordered by distance
            foreach (RubisCell cell in rCellList)
            {
                cell.stations.Sort((a, b) => a.distance >= b.distance ? 1 : -1);
            }

            // Calculates the average probabilty of including cells for each station
            foreach (RubisStation s in rStationList)
            {
                foreach (RubisCell cell in s.cellList)
                {
                    s.pdfSum += cell.pdf;
                }
            }
        }

        public void SetPolicy(Policy policy)
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
                DateTime date = DateTime.Parse(values[2]);
                simulatedEventList.Add(new OHCAEvent(kiloX, kiloY, date));
                line = reader.ReadLine();
            }
            reader.Close();
        }

        public int GetSimulatedEventsCount()
        {
            return simulatedEventList.Count;
        }

        private double ProbabilityMassFunction(int k, double lambda)
        {
            double e = Math.Pow(Math.E, -lambda);
            int i = 0;
            double sum = 0.0;
            while (i <= k)
            {
                double n = Math.Pow(lambda, i) / Factorial(i);
                sum += n;
                i++;
            }
            double cdf = e * sum;
            return cdf;
        }

        private int Factorial(int k)
        {
            int count = k;
            int factorial = 1;
            while (count >= 1)
            {
                factorial = factorial * count;
                count--;
            }
            return factorial;
        }

        private void CloneList(List<RubisCell> srcList, List<RubisCell> dstList)
        {
            dstList.Clear();
            srcList.ForEach((item) =>
            {
                dstList.Add(new RubisCell(item));
            });
        }
    }
}
