using Nito.AsyncEx;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Device.Location;

namespace DronePlacementSimulator
{
    class RUBIS
    {
        enum Direction
        {
            LeftTop,
            Top,
            RightTop,
            Left,
            Center,
            Right,
            LeftBottom,
            Bottom,
            RightBottom
        }

        private static int[] factorial = { 1, 1, 2, 6, 24, 120, 720, 5040, 40320, 362880};
        private Grid eventGrid;
        private Simulator simulator;
        private int stations;
        private int drones;

        private List<RubisStation> stationList;
        private List<RubisCell> cellList;

        public RUBIS(Grid eventGrid, Simulator simulator, ref List<List<GeoCoordinate>> polyCoordList)
        {
            this.eventGrid = eventGrid;
            this.simulator = simulator;            

            this.stationList = new List<RubisStation>();
            this.cellList = new List<RubisCell>();
            
            foreach (Pair c in eventGrid.seoulCells)
            {
                cellList.Add(new RubisCell(new Cell(c.row, c.col), eventGrid.lambda[c.row, c.col]));
            }
        }

        public List<RubisStation> Calculate(List<OHCAEvent> eventList, int budget)
        {
            List<RubisStation> prevStationList = new List<RubisStation>();
            List<RubisStation> nextStationList;

            
            double epsilonTemp = 0.1;
            double alpha = 0.995;
            
            double bestSurvivalRate = 0.0;

            int tempBudget;
            int maxStations = (int)(budget / (Utils.STATION_PRICE + Utils.DRONE_PRICE));
            //for (int stations = 1; stations <= maxStations; stations++)
            for (int stations = 18; stations <= 18; stations++)
            {
                tempBudget = budget;

                // Step 1. Finds initial stations with a drone using K-Means
                tempBudget = tempBudget - (stations * (Utils.STATION_PRICE + Utils.DRONE_PRICE));
                KMeansResults<OHCAEvent> kMeansStations = KMeans.Cluster<OHCAEvent>(eventList.ToArray(), stations, Utils.KMEANS_ITERATION_COUNT);
                prevStationList.Clear();
                foreach (double[] d in kMeansStations.Means)
                {
                    prevStationList.Add(new RubisStation(d[0], d[1], 1));
                }

                // Step 2. Assigns remaining drones to busy stations
                int remainingDrones = (int)(tempBudget / Utils.DRONE_PRICE);
                while (remainingDrones > 0)
                {
                    int mostBusyStationIndex = getIndexOfMostBusyStation(prevStationList);
                    prevStationList[mostBusyStationIndex].droneList.Add(new Drone(prevStationList[mostBusyStationIndex].stationID));
                    remainingDrones--;
                }

                // Step 4. Simulated Annealing
                double currentTemp = 100.0;
                int iteration = 0;
                double prevSurvivalRate = GetOverallSurvivalRate(prevStationList);
                double delta = 0.0;
                double nextSurvivalRate = 0.0;

                while (currentTemp > epsilonTemp)
                {
                    iteration++;

                    // Near search using local optimization
                    nextStationList = MoveOneStepToBestDirection(prevStationList, prevSurvivalRate);
                    nextSurvivalRate = GetOverallSurvivalRate(nextStationList);
                    delta = nextSurvivalRate - prevSurvivalRate;

                    if (delta > 0)
                    {
                        CloneList(nextStationList, prevStationList);
                        prevSurvivalRate = nextSurvivalRate;

                        // Heat-up
                        //currentTemp += currentTemp * 0.01;
                    }
                    else
                    {
                        // Even if worst, choose it randomly according to the current temperature
                        double probility = new Random().NextDouble();
                        if (probility < Math.Exp(-delta / currentTemp))
                        {
                            // Far search using random placement
                            nextStationList = FindRandomStationPlacement(prevStationList, 0);
                            CloneList(nextStationList, prevStationList);
                            prevSurvivalRate = GetOverallSurvivalRate(prevStationList);

                            //if (prevSurvivalRate < bestSurvivalRate * 0.99)
                            double r = new Random().NextDouble();
                            if (r < 0.1)
                            {
                                /*
                                kMeansStations = KMeans.Cluster<OHCAEvent>(eventList.ToArray(), stations, new Random().Next(50, 100));
                                foreach (double[] d in kMeansStations.Means)
                                {
                                    prevStationList.Add(new RubisStation(d[0], d[1], 2));
                                }*/

                                Random rand = new Random();

                                bool okay = false;
                                while (!okay)
                                {
                                    int pos = rand.Next(0, 990000);
                                    kMeansStations = KMeans.Cluster<OHCAEvent>(simulator.GetSimulatedEvents().GetRange(pos, 10000).ToArray(), stations, Utils.KMEANS_ITERATION_COUNT);

                                    okay = true;
                                    foreach (double[] d in kMeansStations.Means)
                                    {
                                        if (d[0] == 0.0 || d[1] == 0.0)
                                        {
                                            okay = false;
                                            break;
                                        }
                                    }
                                }

                                prevStationList.Clear();
                                foreach (double[] d in kMeansStations.Means)
                                {
                                    prevStationList.Add(new RubisStation(d[0], d[1], 1));
                                }

                                remainingDrones = (int)(tempBudget / Utils.DRONE_PRICE);
                                while (remainingDrones > 0)
                                {
                                    int mostBusyStationIndex = getIndexOfMostBusyStation(prevStationList);
                                    prevStationList[mostBusyStationIndex].droneList.Add(new Drone(prevStationList[mostBusyStationIndex].stationID));
                                    remainingDrones--;
                                }

                                prevSurvivalRate = GetOverallSurvivalRate(prevStationList);
                            }
                        }
                    }

                    // Keep the best solution
                    if (prevSurvivalRate > bestSurvivalRate)
                    {
                        CloneList(prevStationList, stationList);
                        bestSurvivalRate = prevSurvivalRate;
                    }

                    // Cool-down
                    currentTemp *= alpha;
                    Console.WriteLine("[" + iteration + "] Stations = " + stations +", Temp.: " + String.Format("{0:0.000000000000}", currentTemp) + "℃    " +
                        "Best = " + String.Format("{0:0.000000}", (bestSurvivalRate * 100)) + "%    " +
                        "Current = " + String.Format("{0:0.000000}", (prevSurvivalRate * 100)) + "%");
                }
            }

            return stationList;
        }

        private int getIndexOfMostBusyStation(List<RubisStation> prevStationList)
        {
            int index = 1;
            double maxPdf = Double.MinValue;

            List<RubisCell> tempCellList = new List<RubisCell>();
            CloneList(cellList, tempCellList);
            InitRubisStation(ref prevStationList, ref tempCellList);

            foreach(RubisStation s in prevStationList)
            {
                int drones = s.droneList.Count;
                if (s.pdfSum / drones > maxPdf)
                {
                    maxPdf = s.pdfSum / drones;
                    index = prevStationList.IndexOf(s);
                }
            }

            return index;
        }

        private void InitRubisStation(ref List<RubisStation> stationList, ref List<RubisCell> cellList)
        {
            foreach (RubisStation s in stationList)
            {
                s.cellList.Clear();
                s.pdfSum = 0.0;
            }

            foreach (RubisCell cell in cellList)
            {
                cell.stations.Clear();
                cell.survivalRate = 0.0;
            }

            foreach (RubisCell cell in cellList)
            {
                foreach (RubisStation s in stationList)
                {
                    double time = simulator.GetPathPlanner().CalculateFlightTime(cell.lat, cell.lon, s.lat, s.lon);
                    if (time <= Utils.GOLDEN_TIME)
                    {
                        cell.stations.Add(new StationDistancePair(s, time));
                        s.cellList.Add(cell);
                    }
                }
            }

            // Sorts stationList ordered by distance
            foreach (RubisCell cell in cellList)
            {
                cell.stations.Sort((a, b) => a.distance >= b.distance ? 1 : -1);
            }

            // Calculates the average probabilty of including cells for each station
            foreach (RubisStation s in stationList)
            {
                foreach (RubisCell cell in s.cellList)
                {
                    s.pdfSum += cell.pdf;
                }
            }
        }

        private List<RubisStation> MoveOneStepToBestDirection(List<RubisStation> currentStationList, double currentSurvivalRate)
        {
            List<RubisStation> tempList = new List<RubisStation>();
            CloneList(currentStationList, tempList);

            List<RubisStation> solutionList = new List<RubisStation>();
            CloneList(currentStationList, solutionList);

            double maxSurvivalRate = currentSurvivalRate;

            // Find the best one-step movement for all directions of all stations
            double lat = 0.0;
            double lon = 0.0;

            foreach (RubisStation s in tempList)
            {
                double tempLat = s.lat;
                double tempLon = s.lon;
                int step = 5;

                foreach (Direction direction in Enum.GetValues(typeof(Direction)))
                {
                    switch (direction)
                    {
                        case Direction.LeftTop:
                            lat = s.lat + Utils.LAT_UNIT * step;
                            lon = s.lon - Utils.LON_UNIT * step;
                            break;
                        case Direction.Top:
                            lat = s.lat + Utils.LAT_UNIT * step;
                            lon = s.lon;
                            break;
                        case Direction.RightTop:
                            lat = s.lat + Utils.LAT_UNIT * step;
                            lon = s.lon + Utils.LON_UNIT * step;
                            break;
                        case Direction.Left:
                            lat = s.lat;
                            lon = s.lon - Utils.LON_UNIT * step;
                            break;
                        case Direction.Right:
                            lat = s.lat;
                            lon = s.lon + Utils.LON_UNIT * step;
                            break;
                        case Direction.LeftBottom:
                            lat = s.lat - Utils.LAT_UNIT * step;
                            lon = s.lon - Utils.LON_UNIT * step;
                            break;
                        case Direction.Bottom:
                            lat = s.lat - Utils.LAT_UNIT * step;
                            lon = s.lon;
                            break;
                        case Direction.RightBottom:
                            lat = s.lat - Utils.LAT_UNIT * step;
                            lon = s.lon + Utils.LON_UNIT * step;
                            break;
                    }

                    if (lat >= 0.1 && lat <= Utils.SEOUL_WIDTH - 0.1 && lon >= 0.1 && lon <= Utils.SEOUL_HEIGHT - 0.1)
                    {
                        s.SetLocation(lat, lon);
                    }
                    else
                    {
                        continue;
                    }

                    double survivalRate = GetOverallSurvivalRate(tempList);
                    if (survivalRate > maxSurvivalRate)
                    {
                        maxSurvivalRate = survivalRate;
                        CloneList(tempList, solutionList);
                    }

                    // Go back to the status of current station list
                    s.SetLocation(tempLat, tempLon);
                }
            }

            double sr = GetOverallSurvivalRate(solutionList);
            return solutionList;
        }

        private List<RubisStation> FindRandomStationPlacement(List<RubisStation> currentStationList, int remainingDrones)
        {
            List<RubisStation> tempList = new List<RubisStation>();
            List<RubisStation> feasibleList = new List<RubisStation>();
            CloneList(currentStationList, feasibleList);
            
            while (true)
            {
                CloneList(currentStationList, tempList);
                double lat = 0.0;
                double lon = 0.0;

                // Move each station a random distance in a random direction
                foreach (Station s in tempList)
                {
                    int randomDirection = new Random().Next(0, 8);
                    int randomDistance = new Random().Next(0, 5);
                    switch ((Direction)randomDirection)
                    {
                        case Direction.LeftTop:
                            lat = s.lat + Utils.LAT_UNIT * randomDistance;
                            lon = s.lon - Utils.LON_UNIT * randomDistance;
                            break;
                        case Direction.Top:
                            lat = s.lat + Utils.LAT_UNIT * randomDistance;
                            lon = s.lon;
                            break;
                        case Direction.RightTop:
                            lat = s.lat + Utils.LAT_UNIT * randomDistance;
                            lon = s.lon + Utils.LON_UNIT * randomDistance;
                            break;
                        case Direction.Left:
                            lat = s.lat;
                            lon = s.lon - Utils.LON_UNIT * randomDistance;
                            break;
                        case Direction.Right:
                            lat = s.lat;
                            lon = s.lon + Utils.LON_UNIT * randomDistance;
                            break;
                        case Direction.LeftBottom:
                            lat = s.lat - Utils.LAT_UNIT * randomDistance;
                            lon = s.lon - Utils.LON_UNIT * randomDistance;
                            break;
                        case Direction.Bottom:
                            lat = s.lat - Utils.LAT_UNIT * randomDistance;
                            lon = s.lon;
                            break;
                        case Direction.RightBottom:
                            lat = s.lat - Utils.LAT_UNIT * randomDistance;
                            lon = s.lon + Utils.LON_UNIT * randomDistance;
                            break;
                    }

                    if (lat >= 0.1 && lat <= Utils.SEOUL_WIDTH - 0.1 && lon >= 0.1 && lon <= Utils.SEOUL_HEIGHT - 0.1)
                    {
                        s.SetLocation(lat, lon);
                    }
                }

                CloneList(tempList, feasibleList);
                break;
            }
            
            return feasibleList;
        }
       
        public bool IsAllCovered(List<RubisStation> stationList)
        {
            foreach (RubisCell cell in cellList)
            {
                bool isCovered = false;
                foreach (RubisStation s in stationList)
                {
                    double distance = simulator.GetPathPlanner().CalculateFlightTime(cell.lat, cell.lon, s.lat, s.lon);
                    if (distance <= Utils.GOLDEN_TIME)
                    {
                        isCovered = true;
                        break;
                    }
                }
                if (isCovered == false)
                {
                    return false;
                }
            }
            return true;
        }

        public double GetOverallSurvivalRate(List<RubisStation> stationList)
        {
            List<RubisCell> tempCellList = new List<RubisCell>();
            CloneList(cellList, tempCellList);
            InitRubisStation(ref stationList, ref tempCellList);

            // Calculates the survival rate for each cell
            double overallSum = AsyncContext.Run(() => ComputeSurvivalRate(tempCellList));
            double survivalRate = overallSum / tempCellList.Count;
            return survivalRate;
        }

        private class WorkObject
        {
            public RubisCell[] cellList;
            public int index;

            public WorkObject(RubisCell[] cellList, int index)
            {
                this.cellList = cellList.Clone() as RubisCell[];
                this.index = index;
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
                    double prob = (1 - pSum) * ProbabilityMassFunction(s.droneList.Count - 1, s.pdfSum);
                    pSum = pSum + prob;
                    cell.survivalRate = cell.survivalRate + (prob * CalculateSurvivalRate(pair.distance));
                }
                sum += cell.survivalRate;
            }
            return sum;
        }

        private async Task<double> ComputeSurvivalRateAsync(WorkObject workObject)
        {
            return await Task.Run(() => ComputeSurvivalRateDoWork(workObject));
        }

        private async Task<double> ComputeSurvivalRate(List<RubisCell> cellList)
        {
            int coreCount = 12;
            List<Task<double>> tasks = new List<Task<double>>();
            int dividedLoad = cellList.Count / coreCount;
            int remainder = cellList.Count % coreCount;
            RubisCell[] cellArray = cellList.ToArray();

            int pos = 0;
            for (int i = 0; i < coreCount; i++)
            {
                int actualLoad = dividedLoad + (i < remainder ? 1 : 0);
                RubisCell[] workLoad = new RubisCell[actualLoad];
                Array.Copy(cellArray, pos, workLoad, 0, actualLoad);
                WorkObject workObject = new WorkObject(workLoad, i);
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

        private double ProbabilityMassFunction(int k, double lambda)
        {
            double e = Math.Pow(Math.E, -lambda);
            int i = 0;
            double sum = 0.0;
            while (i <= k && i < 10)
            {
                double n = Math.Pow(lambda, i) / factorial[i];
                sum += n;
                i++;
            }
            double cdf = e * sum;

            return cdf;
        }

        private double CalculateSurvivalRate(double time)
        {
            return (time <= Utils.GOLDEN_TIME) ? (0.7 - (Utils.SURVIVAL_RATE_SLOPE * time / Utils.GOLDEN_TIME)) : 0.0;
        }

        private void CloneList(List<RubisStation> srcList, List<RubisStation> dstList)
        {
            dstList.Clear();
            srcList.ForEach((item) =>
            {
                dstList.Add(new RubisStation(item));
            });
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
