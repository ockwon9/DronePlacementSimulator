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
            foreach (Station s in stationList)
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
            CalculateCoveredEvents(prevStationList);
            int remainingDrones = drones - prevStationList.Count;
            for (int i = 0; i < remainingDrones; i++)
            {
                prevStationList[i].droneList.Add(new Drone(prevStationList[i].stationID));
            }

            // Step 4. Simulated Annealing
            double currentTemp = 100.0;
            double epsilonTemp = 0.01;
            double alpha = 0.99;
            int iteration = 0;

            double currentSurvivalRate = GetOverallSurvivalRate(prevStationList);
            double bestSurvivalRate = 0.0;

            while (currentTemp > epsilonTemp)
            {
                iteration++;

                // Near search using local optimization
                nextStationList = MoveOneStepToBestDirection(prevStationList);
                double nextSurvivalRate = GetOverallSurvivalRate(nextStationList);
                double delta = nextSurvivalRate - currentSurvivalRate;

                if (delta > 0)
                {
                    // If better, choose it
                    CloneList(nextStationList, prevStationList);
                    currentSurvivalRate = nextSurvivalRate;
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
                        currentSurvivalRate = GetOverallSurvivalRate(prevStationList);
                    }
                }

                // Keep the best solution
                if (currentSurvivalRate > bestSurvivalRate)
                {
                    CloneList(prevStationList, stationList);
                    bestSurvivalRate = currentSurvivalRate;
                }

                // Cool-down
                // TODO: When do we have to heat up?
                currentTemp *= alpha;
                Console.WriteLine("[" + iteration + "] CurrentTemperature: " + currentTemp + "℃, BestSurvivalRate = " + (bestSurvivalRate * 100) + "%");
            }
        }

        // Find the best station placement with only one-step movement
        private List<RubisStation> MoveOneStepToBestDirection(List<RubisStation> currentStationList)
        {
            List<RubisStation> tempList = new List<RubisStation>();
            CloneList(currentStationList, tempList);

            List<RubisStation> solutionList = new List<RubisStation>();
            double maxSurvivalRate = GetOverallSurvivalRate(currentStationList);

            // Find the best one-step movement for all directions of all stations
            foreach (RubisStation s in tempList)
            {
                double tempKiloX = s.kiloX;
                double tempKiloY = s.kiloY;

                foreach (Direction d in Enum.GetValues(typeof(Direction)))
                {
                    switch (d)
                    {
                        case Direction.LeftTop:
                            s.SetLocation(s.kiloX - Utils.UNIT, s.kiloY + Utils.UNIT);
                            break;
                        case Direction.Top:
                            s.SetLocation(s.kiloX, s.kiloY + Utils.UNIT);
                            break;
                        case Direction.RightTop:
                            s.SetLocation(s.kiloX + Utils.UNIT, s.kiloY + Utils.UNIT);
                            break;
                        case Direction.Left:
                            s.SetLocation(s.kiloX - Utils.UNIT, s.kiloY);
                            break;
                        case Direction.Right:
                            s.SetLocation(s.kiloX + Utils.UNIT, s.kiloY);
                            break;
                        case Direction.LeftBottom:
                            s.SetLocation(s.kiloX - Utils.UNIT, s.kiloY - Utils.UNIT);
                            break;
                        case Direction.Bottom:
                            s.SetLocation(s.kiloX, s.kiloY - Utils.UNIT);
                            break;
                        case Direction.RightBottom:
                            s.SetLocation(s.kiloX + Utils.UNIT, s.kiloY - Utils.UNIT);
                            break;
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

            while (true)
            {
                CloneList(currentStationList, tempList);

                // Move each station a random distance in a random direction
                foreach (Station s in tempList)
                {
                    int randomDirection = new Random().Next(0, 8);
                    int randomDistance = new Random().Next(0, 4);

                    switch ((Direction)randomDirection)
                    {
                        case Direction.LeftTop:
                            s.SetLocation(s.kiloX - Utils.UNIT * randomDistance, s.kiloY + Utils.UNIT * randomDistance);
                            break;
                        case Direction.Top:
                            s.SetLocation(s.kiloX, s.kiloY + Utils.UNIT * randomDistance);
                            break;
                        case Direction.RightTop:
                            s.SetLocation(s.kiloX + Utils.UNIT * randomDistance, s.kiloY + Utils.UNIT * randomDistance);
                            break;
                        case Direction.Left:
                            s.SetLocation(s.kiloX - Utils.UNIT * randomDistance, s.kiloY);
                            break;
                        case Direction.Center:
                            s.SetLocation(s.kiloX, s.kiloY);
                            break;
                        case Direction.Right:
                            s.SetLocation(s.kiloX + Utils.UNIT * randomDistance, s.kiloY);
                            break;
                        case Direction.LeftBottom:
                            s.SetLocation(s.kiloX - Utils.UNIT * randomDistance, s.kiloY - Utils.UNIT * randomDistance);
                            break;
                        case Direction.Bottom:
                            s.SetLocation(s.kiloX, s.kiloY - Utils.UNIT * randomDistance);
                            break;
                        case Direction.RightBottom:
                            s.SetLocation(s.kiloX + Utils.UNIT * randomDistance, s.kiloY - Utils.UNIT * randomDistance);
                            break;
                    }
                }

                CloneList(tempList, feasibleList);
                break;
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
        /*
        public static bool IsAllCovered(Grid eventGrid, List<Station> stationList, ref Simulator simulator)
        {
            foreach (Cell c in eventGrid.cells)
            {
                bool isCovered = false;
                foreach (Station s in stationList)
                {
                    if (simulator.GetPathPlanner().CalcuteFlightTime(c.kiloX, c.kiloY, s.kiloX, s.kiloY) <= Utils.GOLDEN_TIME)
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
        */

        // Check whether all events is reachable
        private void CalculateCoveredEvents(List<RubisStation> stationList)
        {
            foreach (Station s in stationList)
            {
                s.eventCount = 0;
            }

            /*
            foreach (OHCAEvent e in eventList)
            {
                foreach (Station s in stationList)
                {
                    if (simulator.GetPathPlanner().CalcuteFlightTime(e.kiloX, e.kiloY, s.kiloX, s.kiloY) <= Utils.GOLDEN_TIME)
                    {
                        s.eventCount++;
                    }
                }
            }
            */

            stationList.Sort((a, b) => a.eventCount <= b.eventCount ? 1 : -1);
        }

        private double GetOverallSurvivalRate(List<RubisStation> stationList)
        {
            double overallSurvivalRateSum = 0.0;

            // Finds reachable stations for each cell
            foreach (RubisCell cell in cellList)
            {
                foreach (RubisStation s in stationList)
                {
                    double distance = simulator.GetPathPlanner().CalcuteFlightTime(cell.kiloX, cell.kiloY, s.kiloX, s.kiloY);
                    if (distance <= Utils.GOLDEN_TIME)
                    {
                        cell.stations.Add(new StationDistancePair(s, distance));
                        s.cellList.Add(cell);
                    }
                }
            }

            // Sorts stationList ordered by distance
            foreach (RubisCell cell in cellList)
            {
                cell.stations.Sort((a, b) => a.distance <= b.distance ? 1 : -1);
            }

            // Calculates the average probabilty of including cells for each station
            foreach (RubisStation s in stationList)
            {
                double sum = 0.0;
                foreach(RubisCell cell in s.cellList)
                {
                    sum += cell.pdf;
                }
                s.averagePDF = sum / s.cellList.Count * 60;
            }

            // Calculates the survival rate for each cell
            foreach (RubisCell cell in cellList)
            {
                double pSum = 0.0;
                foreach (StationDistancePair pair in cell.stations)
                {
                    RubisStation s = pair.station;
                    pSum = (1 - pSum) * ProbabilityMassFunction(s.droneList.Count - 1, s.averagePDF);
                    cell.survivalRate += pSum * pair.distance;
                }
                overallSurvivalRateSum += cell.survivalRate;
            }

            return overallSurvivalRateSum / cellList.Count;
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
    }
}
