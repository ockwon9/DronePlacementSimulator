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
    static class RUBIS
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

        public static void Calculate(Grid eventGrid, List<OHCAEvent> eventList, ref List<Station> stationList, ref Simulator simulator, int stations, int drones)
        {
            List<Station> prevStationList = new List<Station>();
            List<Station> nextStationList;
            CloneList(stationList, prevStationList);

            // Step 1. Find an initial station placement that covers the whole of Seoul
            prevStationList.Add(new Station(4.5, 15.0, 1));
            prevStationList.Add(new Station(8.5, 7.7, 1));
            prevStationList.Add(new Station(14.5, 5.0, 1));
            prevStationList.Add(new Station(12.0, 16.3, 1));
            prevStationList.Add(new Station(16.5, 13.0, 1));
            prevStationList.Add(new Station(17.0, 22.6, 1));
            prevStationList.Add(new Station(21.0, 7.5, 1));
            prevStationList.Add(new Station(22.0, 19.0, 1));
            prevStationList.Add(new Station(24.7, 27.0, 1));
            prevStationList.Add(new Station(25.0, 13.0, 1));
            prevStationList.Add(new Station(27.0, 4.5, 1));
            prevStationList.Add(new Station(27.0, 20.5, 1));
            prevStationList.Add(new Station(30.5, 8.5, 1));
            prevStationList.Add(new Station(33.0, 13.5, 1));

            // Step 2. Add remaining stations in crowded cells
            int remainingStations = stations - prevStationList.Count;
            for (int i = 0; i < remainingStations; i++)
            {
                double kiloX = eventGrid.cells[i].kiloX;
                double kiloY = eventGrid.cells[i].kiloY;
                prevStationList.Add(new Station(kiloX, kiloY, 1));
            }

            // Step 3. Check whether the initial placement covers the whole of Seoul and count how many events each station contains
            /*if (!IsAllCovered(eventGrid, prevStationList, ref simulator))
            {
                return;
            }*/

            // Step 4. Add remaining drones in busy stations
            CalculateCoveredEvents(eventList, ref prevStationList, ref simulator);
            int remainingDrones = drones - prevStationList.Count;
            for (int i = 0; i < remainingDrones; i++)
            {
                prevStationList[i].droneList.Add(new Drone(prevStationList[i].stationID));
            }

            // Step 2. Simulated Annealing
            double currentTemp = 100.0;
            double epsilonTemp = 0.01;
            double alpha = 0.99;
            int iteration = 0;

            double currentSurvivalRate = GetOverallSurvivalRate(eventList, prevStationList, ref simulator);
            double bestSurvivalRate = 0.0;

            while (currentTemp > epsilonTemp)
            {
                iteration++;

                // Near search using local optimization
                nextStationList = MoveOneStepToBestDirection(eventGrid, eventList, prevStationList, ref simulator);
                double nextSurvivalRate = GetOverallSurvivalRate(eventList, nextStationList, ref simulator);
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
                        nextStationList = FindRandomStationPlacement(eventGrid, eventList, prevStationList, ref simulator, remainingDrones);
                        CloneList(nextStationList, prevStationList);
                        currentSurvivalRate = GetOverallSurvivalRate(eventList, prevStationList, ref simulator);
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

                if (iteration % 1 == 0)
                {
                    Console.WriteLine("Iteration [" + iteration + "] CurrentTemperature: " + currentTemp + "℃, BestSurvivalRate = " + (bestSurvivalRate * 100) + "%");
                }
            }
        }

        // Find the best station placement with only one-step movement
        private static List<Station> MoveOneStepToBestDirection(Grid eventGrid, List<OHCAEvent> eventList, List<Station> currentStationList, ref Simulator simulator)
        {
            List<Station> tempList = new List<Station>();
            CloneList(currentStationList, tempList);

            List<Station> solutionList = new List<Station>();
            double maxSurvivalRate = GetOverallSurvivalRate(eventList, currentStationList, ref simulator);

            // Find the best one-step movement for all directions of all stations
            foreach (Station s in tempList)
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

                    // Check the all-coverage constraint
                    //if(IsAllCovered(eventGrid, tempList, ref simulator))
                    {
                        double survivalRate = GetOverallSurvivalRate(eventList, tempList, ref simulator);
                        if (survivalRate > maxSurvivalRate)
                        {
                            maxSurvivalRate = survivalRate;
                            CloneList(tempList, solutionList);
                        }
                    }

                    // Go back to the status of current station list
                    s.SetLocation(tempKiloX, tempKiloY);
                }
            }

            double sr = GetOverallSurvivalRate(eventList, solutionList, ref simulator);
            return solutionList;
        }

        private static List<Station> FindRandomStationPlacement(Grid eventGrid, List<OHCAEvent> eventList, List<Station> currentStationList, ref Simulator simulator, int remainingDrones)
        {
            List<Station> tempList = new List<Station>();
            List<Station> feasibleList = new List<Station>();
            int iteration = 1000;

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

                // Check the all-coverage constraint
                /*if (IsAllCovered(eventGrid, tempList, ref simulator))
                {
                    CloneList(tempList, feasibleList);
                    break;
                }*/

                CloneList(tempList, feasibleList);
                break;

                /*
                iteration--;
                if (iteration < 0)
                {
                    return tempList;
                }
                */
            }

            // Assign drones to randomly-selected stations
            CalculateCoveredEvents(eventList, ref feasibleList, ref simulator);
            foreach (Station s in feasibleList)
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
        public static bool IsAllCovered(Grid eventGrid, List<Station> stationList, ref Simulator simulator)
        {
            foreach (Cell c in eventGrid.cells)
            {
                bool isCovered = false;
                foreach (Station s in stationList)
                {
                    if(simulator.GetPathPlanner().CalcuteFlightTime(c.kiloX, c.kiloY, s.kiloX, s.kiloY) <= Utils.GOLDEN_TIME)
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
        private static void CalculateCoveredEvents(List<OHCAEvent> eventList, ref List<Station> stationList, ref Simulator simulator)
        {
            foreach (Station s in stationList)
            {
                s.eventCount = 0;
            }

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
            stationList.Sort((a, b) => a.eventCount <= b.eventCount ? 1 : -1);
        }

        //TODO: How to calculate the survival rate for the given event list and station list
        private static double GetOverallSurvivalRate(List<OHCAEvent> eventList, List<Station> stationList, ref Simulator simulator)
        {
            double sum = 0.0;
            foreach (OHCAEvent e in eventList)
            {
                double minTime = Double.MaxValue;
                foreach (Station s in stationList)
                {
                    double time = simulator.GetPathPlanner().CalcuteFlightTime(e.kiloX, e.kiloY, s.kiloX, s.kiloY);
                    if (time < minTime)
                    {
                        minTime = time;
                    }
                }
                sum += (minTime > Utils.GOLDEN_TIME) ? 0 : CalculateSurvivalRate(minTime);
            }

            return sum / eventList.Count;
        }

        //TODO: How to calculate the survival rate when the Station s dispatches a drone to the Event e
        public static double GetSurvivalRate(List<Station> stationList, ref Counter counter, Station s, OHCAEvent e)
        {
            return new Random().NextDouble();
        }

        private static double CalculateSurvivalRate(double time)
        {
            return 0.7 - (0.1 * time);
        }

        public static double GetPotential(List<Station> stationList, ref Counter counter, Station s, OHCAEvent e)
        {
            return 0.0;
        }

        private static void CloneList(List<Station> srcList, List<Station> dstList)
        {
            dstList.Clear();
            srcList.ForEach((item) =>
            {
                dstList.Add(new Station(item));
            });
        }
    }
}
