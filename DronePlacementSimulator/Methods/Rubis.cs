using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows.Forms;
using System.Windows.Shapes;

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

        private Grid eventGrid;
        private Simulator simulator;
        private int stations;
        private int drones;

        private List<RubisStation> stationList;
        private List<RubisCell> cellList;

        public RUBIS(Grid eventGrid, List<Station> stationList, Simulator simulator, int stations, int drones)
        {
            this.eventGrid = eventGrid;
            this.simulator = simulator;
            this.stations = stations;
            this.drones = drones;

            this.stationList = new List<RubisStation>();
            foreach (RubisStation s in stationList)
            {
                this.stationList.Add(new RubisStation(s));
            }

            cellList = new List<RubisCell>();
            foreach (Cell c in eventGrid.cells)
            {
                cellList.Add(new RubisCell(c, eventGrid.lambda[c.intX][c.intY]));
            }
        }

        public void Calculate()
        {
            List<RubisStation> prevStationList = new List<RubisStation>();
            List<RubisStation> nextStationList;
            CloneList(stationList, prevStationList);

            // Step 1. Find an initial station placement that covers the whole of Seoul
            prevStationList.Add(new RubisStation(4.5, 15.0, 1));
            prevStationList.Add(new RubisStation(8.5, 7.7, 1));
            prevStationList.Add(new RubisStation(14.5, 5.0, 1));
            prevStationList.Add(new RubisStation(12.0, 16.3, 1));
            prevStationList.Add(new RubisStation(16.5, 13.0, 1));
            prevStationList.Add(new RubisStation(17.0, 22.6, 1));
            prevStationList.Add(new RubisStation(21.0, 7.5, 1));
            prevStationList.Add(new RubisStation(22.0, 19.0, 1));
            prevStationList.Add(new RubisStation(24.7, 27.0, 1));
            prevStationList.Add(new RubisStation(25.0, 13.0, 1));
            prevStationList.Add(new RubisStation(27.0, 4.5, 1));
            prevStationList.Add(new RubisStation(27.0, 20.5, 1));
            prevStationList.Add(new RubisStation(30.5, 8.5, 1));
            prevStationList.Add(new RubisStation(33.0, 13.5, 1));

            // Step 2. Add remaining stations in crowded cells
            int remainingStations = stations - prevStationList.Count;
            for (int i = 0; i < remainingStations; i++)
            {
                double kiloX = eventGrid.cells[i].kiloX;
                double kiloY = eventGrid.cells[i].kiloY;
                prevStationList.Add(new RubisStation(kiloX, kiloY, 1));
            }

            // Step 3. Add remaining drones in busy stations
            CalculateCoveredCells(prevStationList);
            int remainingDrones = drones - prevStationList.Count;
            for (int i = 0; i < remainingDrones; i++)
            {
                prevStationList[i].droneList.Add(new Drone(prevStationList[i].stationID));
            }

            // Step 4. Simulated Annealing
            double currentTemp = 100.0;
            double epsilonTemp = 0.01;
            double alpha = 0.999;
            int iteration = 0;

            double prevSurvivalRate = GetOverallSurvivalRate(prevStationList);
            double bestSurvivalRate = 0.0;

            while (currentTemp > epsilonTemp)
            {
                iteration++;

                // Near search using local optimization
                nextStationList = MoveOneStepToBestDirection(prevStationList, prevSurvivalRate);
                double nextSurvivalRate = GetOverallSurvivalRate(nextStationList);
                double delta = nextSurvivalRate - prevSurvivalRate;

                if (delta > 0)
                {
                    // If better, choose it
                    CloneList(nextStationList, prevStationList);
                    prevSurvivalRate = nextSurvivalRate;
                }
                else
                {
                    // Even if worst, choose it randomly according to the current temperature
                    double probility = new Random().NextDouble();
                    if (probility < Math.Exp(-delta / currentTemp))
                    {
                        // Far search using random placement
                        nextStationList = FindRandomStationPlacement(prevStationList, remainingDrones);
                        CloneList(nextStationList, prevStationList);
                        prevSurvivalRate = GetOverallSurvivalRate(prevStationList);
                    }
                }

                // Keep the best solution
                if (prevSurvivalRate > bestSurvivalRate)
                {
                    CloneList(prevStationList, stationList);
                    bestSurvivalRate = prevSurvivalRate;
                }

                // Cool-down
                // TODO: When do we have to heat up?
                currentTemp *= alpha;
                Console.WriteLine("[" + iteration + "] Temp.: " + currentTemp + "℃, Best = " + (bestSurvivalRate * 100) + "%, Current = " + (prevSurvivalRate * 100) + "%");
            }
        }

        // Find the best station placement with only one-step movement
        private List<RubisStation> MoveOneStepToBestDirection(List<RubisStation> currentStationList, double currentSurvivalRate)
        {
            List<RubisStation> tempList = new List<RubisStation>();
            CloneList(currentStationList, tempList);

            List<RubisStation> solutionList = new List<RubisStation>();
            CloneList(currentStationList, solutionList);

            double maxSurvivalRate = currentSurvivalRate;

            // Find the best one-step movement for all directions of all stations
            double kiloX = 0.0;
            double kiloY = 0.0;

            foreach (RubisStation s in tempList)
            {
                double tempKiloX = s.kiloX;
                double tempKiloY = s.kiloY;

                foreach (Direction direction in Enum.GetValues(typeof(Direction)))
                {
                    switch (direction)
                    {
                        case Direction.LeftTop:
                            kiloX = s.kiloX - Utils.LAMBDA_PRECISION * 5;
                            kiloY = s.kiloY + Utils.LAMBDA_PRECISION * 5;
                            break;
                        case Direction.Top:
                            kiloX = s.kiloX;
                            kiloY = s.kiloY + Utils.LAMBDA_PRECISION * 5;
                            break;
                        case Direction.RightTop:
                            kiloX = s.kiloX = Utils.LAMBDA_PRECISION * 5;
                            kiloY = s.kiloY + Utils.LAMBDA_PRECISION * 5;
                            break;
                        case Direction.Left:
                            kiloX = s.kiloX - Utils.LAMBDA_PRECISION * 5;
                            kiloY = s.kiloY;
                            break;
                        case Direction.Right:
                            kiloX = s.kiloX + Utils.LAMBDA_PRECISION * 5;
                            kiloY = s.kiloY;
                            break;
                        case Direction.LeftBottom:
                            kiloX = s.kiloX - Utils.LAMBDA_PRECISION * 5;
                            kiloY = s.kiloY - Utils.LAMBDA_PRECISION * 5;
                            break;
                        case Direction.Bottom:
                            kiloX = s.kiloX;
                            kiloY = s.kiloY - Utils.LAMBDA_PRECISION * 5;
                            break;
                        case Direction.RightBottom:
                            kiloX = s.kiloX + Utils.LAMBDA_PRECISION * 5;
                            kiloY = s.kiloY - Utils.LAMBDA_PRECISION * 5;
                            break;
                    }

                    if (kiloX > 0.1 && kiloX < Utils.SEOUL_WIDTH - 0.1 && kiloY > 0.1 && kiloY < Utils.SEOUL_HEIGHT - 0.1)
                    {
                        s.SetLocation(kiloX, kiloY);
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
                    s.SetLocation(tempKiloX, tempKiloY);
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

            int iteration = 0;
            while (true)
            {
                CloneList(currentStationList, tempList);
                double kiloX = 0.0;
                double kiloY = 0.0;

                // Move each station a random distance in a random direction
                foreach (Station s in tempList)
                {
                    int randomDirection = new Random().Next(0, 8);
                    int randomDistance = new Random().Next(0, 5);
                    switch ((Direction)randomDirection)
                    {
                        case Direction.LeftTop:
                            kiloX = s.kiloX - Utils.LAMBDA_PRECISION * randomDistance;
                            kiloY = s.kiloY + Utils.LAMBDA_PRECISION * randomDistance;
                            break;
                        case Direction.Top:
                            kiloX = s.kiloX;
                            kiloY = s.kiloY + Utils.LAMBDA_PRECISION * randomDistance;
                            break;
                        case Direction.RightTop:
                            kiloX = s.kiloX + Utils.LAMBDA_PRECISION * randomDistance;
                            kiloY = s.kiloY + Utils.LAMBDA_PRECISION * randomDistance;
                            break;
                        case Direction.Left:
                            kiloX = s.kiloX - Utils.LAMBDA_PRECISION * randomDistance;
                            kiloY = s.kiloY;
                            break;
                        case Direction.Center:
                            kiloX = s.kiloX;
                            kiloY = s.kiloY;
                            break;
                        case Direction.Right:
                            kiloX = s.kiloX + Utils.LAMBDA_PRECISION * randomDistance;
                            kiloY = s.kiloY;
                            break;
                        case Direction.LeftBottom:
                            kiloX = s.kiloX - Utils.LAMBDA_PRECISION * randomDistance;
                            kiloY = s.kiloY - Utils.LAMBDA_PRECISION * randomDistance;
                            break;
                        case Direction.Bottom:
                            kiloX = s.kiloX;
                            kiloY = s.kiloY - Utils.LAMBDA_PRECISION * randomDistance;
                            break;
                        case Direction.RightBottom:
                            kiloX = s.kiloX + Utils.LAMBDA_PRECISION * randomDistance;
                            kiloY = s.kiloY - Utils.LAMBDA_PRECISION * randomDistance;
                            break;
                    }

                    if (kiloX > 0.1 && kiloX < Utils.SEOUL_WIDTH - 0.1 && kiloY > 0.1 && kiloY < Utils.SEOUL_HEIGHT - 0.1)
                    {
                        s.SetLocation(kiloX, kiloY);
                    }
                }

                if(IsAllCovered(tempList))
                {
                    CloneList(tempList, feasibleList);
                    break;
                }

                if (iteration++ > 1000)
                {
                    return feasibleList;
                }
            }

            // Assign drones to randomly-selected stations
            // CalculateCoveredEvents(eventList, ref feasibleList, ref simulator);
            foreach (RubisStation s in feasibleList)
            {
                s.droneList.Clear();
                s.droneList.Add(new Drone(s.stationID));
            }
            for (int i = 0; i < remainingDrones; i++)
            {
                Station s = feasibleList[new Random().Next(0, feasibleList.Count - 1)];
                s.droneList.Add(new Drone(s.stationID));
            }

            return feasibleList;
        }

        // Check whether all events is reachable
        public bool IsAllCovered(List<RubisStation> stationList)
        {
            foreach (RubisCell cell in cellList)
            {
                bool isCovered = false;
                foreach (RubisStation s in stationList)
                {
                    double distance = simulator.GetPathPlanner().CalcuteFlightTime(cell.kiloX, cell.kiloY, s.kiloX, s.kiloY);
                    if (distance <= Utils.GOLDEN_TIME)
                    {
                        isCovered = true;
                        break;
                    }
                }
                if(isCovered == false)
                {
                    return false;
                }
            }
            return true;
        }

        // Check whether all events is reachable
        private void CalculateCoveredCells(List<RubisStation> stationList)
        {
            foreach (RubisStation s in stationList)
            {
                s.eventCount = 0;
            }

            foreach (RubisCell e in cellList)
            {
                foreach (RubisStation s in stationList)
                {
                    if (simulator.GetPathPlanner().CalcuteFlightTime(e.kiloX, e.kiloY, s.kiloX, s.kiloY) <= Utils.GOLDEN_TIME)
                    {
                        s.eventCount++;
                    }
                }
            }

            stationList.Sort((a, b) => a.eventCount <= b.eventCount ? 1 : -1);
        }

        private double GetOverallSurvivalRate(List<RubisStation> stationList)
        {
            List<RubisCell> tempCellList = new List<RubisCell>();
            CloneList(cellList, tempCellList);

            // Finds reachable stations for each cell
            foreach (RubisCell cell in tempCellList)
            {
                foreach (RubisStation s in stationList)
                {
                    if (s.kiloY < 0.1)
                    {
                        Console.WriteLine(s.ToString());
                    }
                    double distance = simulator.GetPathPlanner().CalcuteFlightTime(cell.kiloX, cell.kiloY, s.kiloX, s.kiloY);
                    if (distance <= Utils.GOLDEN_TIME)
                    {
                        cell.stations.Add(new StationDistancePair(s, distance));
                        s.cellList.Add(cell);
                    }
                }
            }

            // Sorts stationList ordered by distance
            foreach (RubisCell cell in tempCellList)
            {
                cell.stations.Sort((a, b) => a.distance >= b.distance ? 1 : -1);
            }

            // Calculates the average probabilty of including cells for each station
            foreach (RubisStation s in stationList)
            {
                foreach(RubisCell cell in s.cellList)
                {
                    s.pdfSum += cell.pdf;
                }
            }

            // Calculates the survival rate for each cell
            double overallSum = 0.0;
            foreach (RubisCell cell in tempCellList)
            {
                double pSum = 0.0;
                foreach (StationDistancePair pair in cell.stations)
                {
                    RubisStation s = pair.station;
                    double prob = (1 - pSum) * ProbabilityMassFunction(s.droneList.Count - 1, s.pdfSum);
                    pSum += prob;
                    cell.survivalRate += prob * CalculateSurvivalRate(pair.distance);
                }
                overallSum += cell.survivalRate;
            }

            double survivalRate = overallSum / tempCellList.Count;
            return survivalRate;
        }

        private double ProbabilityMassFunction(int k, double lambda)
        {
            int kFactorial = Factorial(k);
            double numerator = Math.Pow(Math.E, -(double)lambda) * Math.Pow((double)lambda, (double)k);
            return numerator / kFactorial;
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

        private double CalculateSurvivalRate(double time)
        {
            return 0.7 - (0.1 * time);
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
