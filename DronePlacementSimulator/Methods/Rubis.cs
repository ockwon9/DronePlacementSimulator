using System;
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
            Right,
            LeftBottom,
            Bottom,
            RightBottom
        }

        public static List<Station> doCalculate(List<OHCAEvent> eventList, List<List<double[]>> polyCoordList)
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
            if (!isAllCovered(eventList, ref stationList))
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
            List<Station> tempList;
            double currentSurvivalRate = getSurvivalRate(eventList, stationList);
            List<Station> solutionList = new List<Station>();

            while (currentTemp > epsilonTemp)
            {
                iteration++;

                tempList = moveToPromisingStation(gridEvent, eventList, stationList);
                double nextSurvivalRate = getSurvivalRate(eventList, tempList);
                double delta = nextSurvivalRate - currentSurvivalRate;

                if (delta > 0)
                {
                    currentSurvivalRate = nextSurvivalRate;
                    solutionList.Clear();
                    solutionList = tempList;
                }
                else
                {
                    double probility = new Random().NextDouble();
                    if (probility < Math.Exp(-delta / currentTemp))
                    {
                        currentSurvivalRate = nextSurvivalRate;
                        solutionList.Clear();
                        solutionList = tempList;
                    }
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
        private static List<Station> moveToPromisingStation(Grid gridEvent, List<OHCAEvent> eventList, List<Station> stationList)
        {
            List<Station> tempList = new List<Station>();
            List<Station> solutionList = new List<Station>();
            double maxSurvivalRate = 0.0;

            foreach (Station s in tempList)
            {
                foreach (Direction d in Enum.GetValues(typeof(Direction)))
                {
                    tempList.Clear();
                    tempList.AddRange(stationList);

                    switch (d)
                    {
                        case Direction.LeftTop:
                            s.setLocation(s.kiloX - Utils.UNIT, s.kiloY + Utils.UNIT);
                            break;
                        case Direction.Top:
                            s.setLocation(s.kiloX, s.kiloY + Utils.UNIT);
                            break;
                        case Direction.RightTop:
                            s.setLocation(s.kiloX + Utils.UNIT, s.kiloY + Utils.UNIT);
                            break;
                        case Direction.Left:
                            s.setLocation(s.kiloX - Utils.UNIT, s.kiloY);
                            break;
                        case Direction.Right:
                            s.setLocation(s.kiloX + Utils.UNIT, s.kiloY);
                            break;
                        case Direction.LeftBottom:
                            s.setLocation(s.kiloX - Utils.UNIT, s.kiloY - Utils.UNIT);
                            break;
                        case Direction.Bottom:
                            s.setLocation(s.kiloX, s.kiloY - Utils.UNIT);
                            break;
                        case Direction.RightBottom:
                            s.setLocation(s.kiloX + Utils.UNIT, s.kiloY - Utils.UNIT);
                            break;
                    }

                    if(isAllCovered(eventList, ref tempList))
                    {
                        double survivalRate = getSurvivalRate(eventList, tempList);
                        if (survivalRate > maxSurvivalRate)
                        {
                            maxSurvivalRate = survivalRate;
                            solutionList.Clear();
                            solutionList.AddRange(tempList);
                        }
                    }
                }
            }

            return (CheckMatch(stationList, solutionList)) ? null : solutionList;
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
        public static bool isAllCovered(List<OHCAEvent> eventList, ref List<Station> stationList)
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
                    if(Utils.getDistance(e.kiloX, e.kiloY, s.kiloX, s.kiloY) <= Utils.GOLDEN_TIME)
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
        private static double getSurvivalRate(List<OHCAEvent> eventList, List<Station> stationList)
        {
            return 1.0f;
        }

        //TODO: How to calculate the survival rate when the Station s dispatches a drone to the Event e
        public static double getSurvivalRate(List<Station> stationList, Station s, OHCAEvent e)
        {
            return 1.0f;
        }
    }
}
