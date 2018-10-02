﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Windows.Shapes;
using CSharpIDW;

namespace DronePlacementSimulator
{
    public static class Rubis
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

        public static List<Station> Calculate(List<OHCAEvent> eventList, List<List<double[]>> polyCoordList)
        {
            Grid gridEvent = new Grid(0.0, 0.0, Utils.SEOUL_WIDTH, Utils.SEOUL_HEIGHT, Utils.UNIT, ref polyCoordList);
            //gridEvent.IdwInterpolate(ref eventList);

            // Step 1. Find an initial station placement that covers the whole of Seoul
            List<Station> stationList = new List<Station>();
            Grid gridStation = new Grid(0.0, 0.0, Utils.SEOUL_WIDTH, Utils.SEOUL_HEIGHT, 7, ref polyCoordList);
            foreach (double[] cell in gridStation.cells)
            {
                double kiloX = cell[0] + 0.5 * 9;
                double kiloY = cell[1] + 0.5 * 9;
                Station s = new Station(kiloX, kiloY);
                Drone drone = new Drone(s.stationID);
                s.droneList.Add(drone);
                stationList.Add(s);
            }

            // Remove two useless stations
            stationList.Remove(stationList[23]);
            stationList.Remove(stationList[21]);

            // Add additional stations
            // 13번째 station부터는 제일 많은 이벤트가 포함되는 셀의 위치를 찾아서 포함시키자
            // Station s = new Station(0, 0);
            // s.droneList.Add(new Drone(s.stationID));
            // remainingBudget = remainingBudget - Utils.STATION_PRICE - Utils.DRONE_PRICE;

            // Check whether the initial placement covers the whole of Seoul and count how many events each station contains
            if (!IsAllCovered(eventList, ref stationList))
            {
                return null;
            }

            // Calculate the initial budget
            // All stations should have at least one drone 
            int remainingBudget = Utils.BUDGET;
            remainingBudget = remainingBudget - (stationList.Count * Utils.STATION_PRICE) - (stationList.Count * Utils.DRONE_PRICE);

            // Step 2. Assign remaining drones to crowded stations
            stationList.Sort((a, b) => a.eventCount < b.eventCount ? 1 : -1);
            int remainingDrones = (int)(remainingBudget / Utils.DRONE_PRICE);
            for (int i = 0; i < remainingDrones; i++)
            {
                stationList[i].droneList.Add(new Drone(stationList[0].stationID));
            }

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

                // Find random, but feasible placement
                nextStationList = FindRandomStationPlacement(eventList, currentStationList);
                double nextSurvivalRate = GetOverallSurvivalRate(eventList, currentStationList);
                double delta = nextSurvivalRate - currentSurvivalRate;

                if (delta > 0)
                {
                    currentSurvivalRate = nextSurvivalRate;
                    currentStationList = nextStationList;
                }
                else
                {
                    double probility = new Random().NextDouble();
                    if (probility < Math.Exp(-delta / currentTemp))
                    {
                        currentSurvivalRate = nextSurvivalRate;
                        currentStationList = nextStationList;
                    }
                }

                if (currentSurvivalRate > bestSurvivalRate)
                {
                    bestSurvivalRate = currentSurvivalRate;
                    solutionList = currentStationList;
                }

                currentTemp *= alpha;
                if (iteration % 10 == 0)
                {
                    Console.WriteLine("Iteration [" + iteration + "] CurrentTemperature: " + currentTemp + "℃, CurrentSurvivalRate = " + (currentSurvivalRate*100) + "%");
                }
            }

            return solutionList;
        }

        // Find the best station placement with only one-step movement
        private static List<Station> MoveOneStepToBestDirection(Grid gridEvent, List<OHCAEvent> eventList, List<Station> currentStationList)
        {
            List<Station> tempList = new List<Station>();
            tempList.AddRange(currentStationList);

            List<Station> solutionList = new List<Station>();
            double maxSurvivalRate = GetOverallSurvivalRate(eventList, currentStationList);

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

                    if(IsAllCovered(eventList, ref tempList))
                    {
                        double survivalRate = GetOverallSurvivalRate(eventList, tempList);
                        if (survivalRate > maxSurvivalRate)
                        {
                            maxSurvivalRate = survivalRate;
                            solutionList.Clear();
                            solutionList.AddRange(tempList);
                        }
                    }

                    s.SetLocation(tempKiloX, tempKiloY);
                }
            }

            return solutionList;
        }

        private static List<Station> FindRandomStationPlacement(List<OHCAEvent> eventList, List<Station> currentStationList)
        {
            List<Station> tempList = new List<Station>();
            List<Station> feasibleList = new List<Station>();
            int iteration = 100;
            while (true)
            {
                tempList.Clear();
                tempList.AddRange(currentStationList);
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

                if (IsAllCovered(eventList, ref tempList))
                {
                    feasibleList.Clear();
                    feasibleList.AddRange(tempList);
                    break;
                }

                iteration--;
                if (iteration < 0)
                {
                    break;
                }
            }

            return feasibleList;
        }

        // Compare two lists 
        private static bool CheckMatch(List<Station> l1, List<Station> l2)
        {
            if (l1 == null && l2 == null)
            {
                return true;
            }
            else if (l1 == null || l2 == null)
            {
                return false;
            }

            if (l1.Count != l2.Count)
            {
                return false;
            }

            for (int i = 0; i < l1.Count; i++)
            {
                if (l1[i].kiloX != l2[i].kiloX || l1[i].kiloY != l2[i].kiloY)
                    return false;
            }
            return true;
        }

        // Check whether all events is reachable
        public static bool IsAllCovered(List<OHCAEvent> eventList, ref List<Station> stationList)
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

        //TODO: How to calculate the survival rate for the given event list
        private static double GetOverallSurvivalRate(List<OHCAEvent> eventList, List<Station> stationList)
        {
            return new Random().NextDouble();
        }

        //TODO: How to calculate the survival rate when the Station s dispatches a drone to the Event e
        public static double GetSurvivalRate(List<Station> stationList, Station s, OHCAEvent e)
        {
            return new Random().NextDouble();
        }
    }
}
