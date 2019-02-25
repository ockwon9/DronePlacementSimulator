using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using Nito.AsyncEx;
using System.Threading.Tasks;

namespace DronePlacementSimulator
{
    class Simulator
    {
        private PathPlanner pathPlanner;
        
        private double expectedSurvivalRate;
        private int unreachableEvents;
        private int noDrones;

        private List<OHCAEvent> simulatedEventList;
        private List<RubisCell> rCellList;
        private List<RubisStation> rStationList;

        public Simulator()
        {
            pathPlanner = new PathPlanner();
            simulatedEventList = new List<OHCAEvent>();
            if (File.Exists("simulationEvents.csv"))
            {
                ReadSimulatedEvents();
            }
            else
            {
                MessageBox.Show("There is no simulated events file.", "Simulation", MessageBoxButtons.OK);
                return;
            }
        }

        public List<DispatchFailure> Simulate(List<Station> stationList, Grid eventGrid)
        {
            List<DispatchFailure> failedEventList = new List<DispatchFailure>();

            expectedSurvivalRate = 0;
            unreachableEvents = 0;
            noDrones = 0;

            int n = stationList.Count;
            int[] initialCount = new int[n];
            for (int i = 0; i < n; i++)
            {
                initialCount[i] = stationList[i].droneList.Count;
            }

            Counter current = new Counter(ref initialCount);
            double sum = 0;

            SetRubisMethod(eventGrid, stationList);

            int iteration = 0;
            foreach (OHCAEvent e in simulatedEventList)
            {
                current.Flush(e.occurrenceTime);
                int dispatchFrom = GetNearestStation(rStationList, ref current, e);
                e.assignedStationId = dispatchFrom;

                if (dispatchFrom == -1)
                {
                    noDrones++;
                    failedEventList.Add(new DispatchFailure(e.lat, e.lon));
                }
                else if (dispatchFrom == -2)
                {
                    unreachableEvents++;
                    failedEventList.Add(new DispatchFailure(e.lat, e.lon));
                }
                else
                {
                    double flightTime = pathPlanner.CalculateFlightTime(rStationList[dispatchFrom].lat, rStationList[dispatchFrom].lon, e.lat, e.lon);
                    sum += CalculateSurvivalRate(flightTime);
                    current.Dispatch(dispatchFrom, e.occurrenceTime);
                }

                if (iteration++% 10000 == 0)
                {
                    Console.WriteLine("[" + iteration + "] time = " + e.occurrenceTime + ", unreachables = " + unreachableEvents + ", noDrones = " + noDrones);
                }
            }
            expectedSurvivalRate = sum / simulatedEventList.Count;
            return failedEventList;
        }

        public List<OHCAEvent> GetSimulatedEvents()
        {
            return simulatedEventList;
        }
        
        private int GetNearestStation(List<RubisStation> rStationList, ref Counter counter, OHCAEvent e)
        {
            int n = rStationList.Count;
            int[] index = new int[n];
            double[] distance = new double[n];

            for (int i = 0; i < n; i++)
            {
                RubisStation s = rStationList[i];
                index[i] = i;
                distance[i] = pathPlanner.CalculateFlightTime(s.lat, s.lon, e.lat, e.lon);

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

            bool isReachable = false;
            
            for (;k < n; k++)
            {
                if (distance[k] <= Utils.GOLDEN_TIME)
                {
                    isReachable = true;
                    if (counter.whenReady[index[k]].Count < rStationList[index[k]].droneList.Count)
                    {
                        break;
                    }
                }
            }

            if (k == n)
            {
                return isReachable ? -1 : -2;
            }

            return index[k];
        }
        
        private double GetOverallSurvivalRate(RubisStation targetStation, Counter counter, bool dispatch)
        {
            RubisStation tempStation = new RubisStation(targetStation);

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

            // Calculates the survival rate for each cell
            double overallSum = AsyncContext.Run(() => ComputeSurvivalRate(tempStation.cellList, ready));
            double survivalRate = overallSum / tempStation.cellList.Count;
            return survivalRate;
        }

        private class WorkObject
        {
            public RubisCell[] cellList;
            public int index;
            public int[] ready;

            public WorkObject(RubisCell[] cellList, int index, int[] ready)
            {
                this.cellList = cellList.Clone() as RubisCell[];
                this.index = index;
                this.ready = ready.Clone() as int[];
            }
        }

        private double ComputeSurvivalRateDoWork(WorkObject workObject)
        {
            double sum = 0.0;
            foreach (RubisCell cell in workObject.cellList)
            {
                double pSum = 0.0;
                foreach (StationDistancePair pair in cell.stations)
                {
                    RubisStation s = pair.station;
                    int index = rStationList.IndexOf(s);
                    if (workObject.ready[index] > 0)
                    {
                        double prob = (1 - pSum) * ProbabilityMassFunction(workObject.ready[index] - 1, Utils.DRONE_REST_TIME * 60 * s.pdfSum);
                        pSum += prob;
                        cell.survivalRate = cell.survivalRate + (prob * CalculateSurvivalRate(pair.distance));
                    }
                }
                sum += cell.survivalRate;
            }
            return sum;
        }

        private async Task<double> ComputeSurvivalRateAsync(WorkObject workObject)
        {
            return await Task.Run(() => ComputeSurvivalRateDoWork(workObject));
        }

        private async Task<double> ComputeSurvivalRate(List<RubisCell> cellList, int[] ready)
        {
            int coreCount = 12;
            List<Task<double>> tasks = new List<Task<double>>();
            int dividedLoad = cellList.Count / coreCount;
            int remainder = cellList.Count % coreCount;

            int pos = 0;
            for (int i = 0; i < coreCount; i++)
            {
                int actualLoad = dividedLoad + (i < remainder ? 1 : 0);
                RubisCell[] workLoad = new RubisCell[actualLoad];
                Array.Copy(cellList.ToArray(), pos, workLoad, 0, actualLoad);
                WorkObject workObject = new WorkObject(workLoad, i, ready);
                pos += actualLoad;
                tasks.Add(ComputeSurvivalRateAsync(workObject));
            }

            await Task.WhenAll(tasks);

            double sum = 0.0;
            for (int i = 0; i < coreCount; i++)
            {
                sum += tasks[i].Result;
            }

            return sum;
        }

        public void SetRubisMethod(Grid eventGrid, List<Station> stationList)
        {
            rCellList = new List<RubisCell>();
            foreach (Pair c in eventGrid.seoulCells)
            {
                rCellList.Add(new RubisCell(new Cell(c.row, c.col), eventGrid.lambda[c.row, c.col]));
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
                    double time = pathPlanner.CalculateFlightTime(s.lat, s.lon, cell.lat, cell.lon);
                    if (time <= Utils.GOLDEN_TIME)
                    {
                        cell.stations.Add(new StationDistancePair(s, time));
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

        private double CalculateSurvivalRate(double time)
        {            
            return (time <= Utils.GOLDEN_TIME) ? (0.7 - (Utils.SURVIVAL_RATE_SLOPE * time / Utils.GOLDEN_TIME)) : 0.0;
        }

        public double GetExpectedSurvivalRate()
        {
            return expectedSurvivalRate;
        }

        public int GetUnreachableEvents()
        {
            return unreachableEvents;
        }

        public int GetNoDrones()
        {
            return noDrones;
        }

        public ref PathPlanner GetPathPlanner()
        {
            return ref pathPlanner;
        }

        private void ReadSimulatedEvents()
        {
            StreamReader reader = new StreamReader("simulationEvents.csv");
            string line = reader.ReadLine();
            while (line != null)
            {
                string[] values = line.Split(',');
                double lat = double.Parse(values[0]);
                double lon = double.Parse(values[1]);
                DateTime occurenceTime = DateTime.Parse(values[2]);
                simulatedEventList.Add(new OHCAEvent(lat, lon, occurenceTime));
                line = reader.ReadLine();
            }
            reader.Close();
            
            StreamWriter file = new StreamWriter("events.csv");
            for (int i = 0; i < simulatedEventList.Count; i++)
            {
                OHCAEvent e = simulatedEventList[i];
                file.Write(e.lat + "," + e.lon + "," + e.occurrenceTime + "\n");
            }
            file.Close();
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
