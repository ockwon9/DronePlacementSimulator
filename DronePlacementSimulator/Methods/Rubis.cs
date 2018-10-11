using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Windows.Shapes;

namespace DronePlacementSimulator
{
    public static class RUBIS
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

        public static List<Station> Calculate(Grid eventGrid, List<OHCAEvent> eventList, ref Simulator simulator, int stations, int drones)
        {
            // Step 1. Find an initial station placement that covers the whole of Seoul
            List<Station> stationList = new List<Station>();
            stationList.Add(new Station(4.5, 15.0, 1));
            stationList.Add(new Station(8.5, 7.7, 1));
            stationList.Add(new Station(14.5, 5.0, 1));
            stationList.Add(new Station(12.0, 16.3, 1));
            stationList.Add(new Station(16.5, 13.0, 1));
            stationList.Add(new Station(17.0, 22.6, 1));
            stationList.Add(new Station(21.0, 7.5, 1));
            stationList.Add(new Station(22.0, 19.0, 1));
            stationList.Add(new Station(24.7, 27.0, 1));
            stationList.Add(new Station(25.0, 13.0, 1));
            stationList.Add(new Station(27.0, 4.5, 1));
            stationList.Add(new Station(27.0, 20.5, 1));
            stationList.Add(new Station(30.5, 8.5, 1));
            stationList.Add(new Station(33.0, 13.5, 1));

            // Add additional stations in crowded cells
            int remainingStations = stations - stationList.Count;
            for (int i = 0; i < remainingStations; i++)
            {
                double kiloX = eventGrid.cells[i].kiloX;
                double kiloY = eventGrid.cells[i].kiloY;
                stationList.Add(new Station(kiloX, kiloY, 1));
            }

            int remainingDrones = drones - stationList.Count;
            for (int i = 0; i < remainingDrones; i++)
            {
                double kiloX = eventGrid.cells[i].kiloX;
                double kiloY = eventGrid.cells[i].kiloY;
                
            }

            // Check whether the initial placement covers the whole of Seoul and count how many events each station contains
            if (!IsAllCovered(eventGrid, ref stationList))
            {
                return null;
            }

            // Step 2. Assign remaining drones to crowded stations
            /*
            stationList.Sort((a, b) => a.eventCount < b.eventCount ? 1 : -1);
            int remainingDrones = (int)(remainingBudget / Utils.DRONE_PRICE);
            for (int i = 0; i < remainingDrones; i++)
            {
                stationList[i].droneList.Add(new Drone(stationList[i].stationID));
            }
            */
            
            // Step 3. Search the best station placement
            double currentTemp = 100.0;
            double epsilonTemp = 0.01;
            double alpha = 0.99;
            int iteration = 0;

            List<Station> nextStationList;
            List<Station> currentStationList = stationList;
            List<Station> solutionList = new List<Station>();

            double currentSurvivalRate = GetOverallSurvivalRate(eventList, stationList);
            double bestSurvivalRate = 0.0;

            // Step 4. Simulated Annealing
            while (currentTemp > epsilonTemp)
            {
                iteration++;

                // Find random, but feasible station placement
                nextStationList = MoveOneStepToBestDirection(eventGrid, eventList, currentStationList, remainingBudget);
                double nextSurvivalRate = GetOverallSurvivalRate(eventList, currentStationList);
                double delta = nextSurvivalRate - currentSurvivalRate;

                if (delta > 0)
                {
                    // If better, choose it
                    currentSurvivalRate = nextSurvivalRate;
                    currentStationList = nextStationList;
                }
                else
                {
                    // Even if worst, choose it randomly according to the current temperature
                    double probility = new Random().NextDouble();
                    if (probility < Math.Exp(-delta / currentTemp))
                    {
                        nextStationList = FindRandomStationPlacement(eventList, currentStationList, remainingBudget);
                        currentSurvivalRate = nextSurvivalRate;
                        currentStationList = nextStationList;
                    }
                }

                // Keep the best solution
                if (currentSurvivalRate > bestSurvivalRate)
                {
                    bestSurvivalRate = currentSurvivalRate;
                    solutionList = currentStationList;
                }

                // Cool-down
                // TODO: When do we have to heat up?
                currentTemp *= alpha;

                if (iteration % 10 == 0)
                {
                    Console.WriteLine("Iteration [" + iteration + "] CurrentTemperature: " + currentTemp + "℃, CurrentSurvivalRate = " + (currentSurvivalRate*100) + "%");
                }
            }

            return solutionList;
        }

        // Find the best station placement with only one-step movement
        private static List<Station> MoveOneStepToBestDirection(Grid eventGrid, List<OHCAEvent> eventList, List<Station> currentStationList)
        {
            List<Station> tempList = new List<Station>();
            tempList.AddRange(currentStationList);

            List<Station> solutionList = new List<Station>();
            double maxSurvivalRate = GetOverallSurvivalRate(eventList, currentStationList);

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
                    if(IsAllCovered(eventGrid, ref tempList))
                    {
                        double survivalRate = GetOverallSurvivalRate(eventList, tempList);
                        if (survivalRate > maxSurvivalRate)
                        {
                            maxSurvivalRate = survivalRate;
                            solutionList.Clear();
                            solutionList.AddRange(tempList);
                        }
                    }

                    // Go back to the status of current station list
                    s.SetLocation(tempKiloX, tempKiloY);
                }
            }

            return solutionList;
        }

        private static List<Station> FindRandomStationPlacement(Grid eventGrid, List<OHCAEvent> eventList, List<Station> currentStationList, int drones)
        {
            List<Station> tempList = new List<Station>();
            List<Station> feasibleList = new List<Station>();
            int iteration = Utils.ITERATION_COUNT;

            while (true)
            {
                tempList.Clear();
                tempList.AddRange(currentStationList);

                // Assign drones to randomly-selected stations
                for (int i = 0; i < drones; i++)
                {
                    int randomIndex = new Random().Next(0, currentStationList.Count - 1);
                    currentStationList[randomIndex].droneList.Add(new Drone(currentStationList[randomIndex].stationID));
                }

                // Move each station a random distance in a random direction
                foreach (Station s in tempList)
                {
                    int randomDirection = new Random().Next(0, 8);
                    int randomDistance = new Random().Next(0, 3);
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
                if (IsAllCovered(eventGrid, ref tempList))
                {
                    feasibleList.Clear();
                    feasibleList.AddRange(tempList);
                    break;
                }

                iteration--;
                if (iteration < 0)
                {
                    return currentStationList;
                }
            }

            return feasibleList;
        }

        // Check whether all events is reachable
        public static bool IsAllCovered(Grid eventGrid, ref List<Station> stationList)
        {
            foreach (Station s in stationList)
            {
                s.eventCount = 0;
            }

            foreach (Cell c in eventGrid.cells)
            {
                bool isCovered = false;
                foreach (Station s in stationList)
                {
                    if(Utils.GetDistance(e.kiloX, e.kiloY, s.kiloX, s.kiloY) <= Utils.GOLDEN_TIME)
                    {
                        isCovered = true;
                        s.eventCount++;
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
        private static void CalculateCoveredEvents(List<OHCAEvent> eventList, ref List<Station> stationList)
        {
            foreach (Station s in stationList)
            {
                s.eventCount = 0;
            }

            foreach (OHCAEvent e in eventList)
            {
                bool isCovered = false;
                foreach (Station s in stationList)
                {
                    if (Utils.GetDistance(e.kiloX, e.kiloY, s.kiloX, s.kiloY) <= Utils.GOLDEN_TIME)
                    {
                        isCovered = true;
                        s.eventCount++;
                    }
                }
                if (isCovered == false)
                {
                    return false;
                }
            }
            return true;
        }

        //TODO: How to calculate the survival rate for the given event list and station list
        private static double GetOverallSurvivalRate(List<OHCAEvent> eventList, List<Station> stationList)
        {
            return new Random().NextDouble();
        }

        //TODO: How to calculate the survival rate when the Station s dispatches a drone to the Event e
        public static double GetSurvivalRate(List<Station> stationList, ref Counter counter, Station s, OHCAEvent e)
        {
            return new Random().NextDouble();
        }

        public static double GetPotential(List<Station> stationList, ref Counter counter, Station s, OHCAEvent e)
        {
            return 0.0;
        }
    }
}
