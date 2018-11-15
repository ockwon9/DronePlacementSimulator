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
        private int unreachableEvents;
        private int noDrones;

        private int secondChoices;
        private double survivalRateGain;
        private double survivalRateLoss;

        private List<OHCAEvent> simulatedEventList;
        private List<RubisCell> rCellList;
        private List<RubisStation> rStationList;

        public Simulator()
        {
            pathPlanner = new PathPlanner();
            simulatedEventList = new List<OHCAEvent>();
            if (File.Exists("simulation_events.csv") && simulatedEventList.Count == 0)
            {
                ReadSimulatedEvents();
            }
            else
            {
                MessageBox.Show("There is no simulated events file.", "Simulation", MessageBoxButtons.OK);
                return;
            }
        }

        public void Simulate(List<Station> stationList, Grid eventGrid)
        {
            expectedSurvivalRate = 0;
            unreachableEvents = 0;
            noDrones = 0;

            survivalRateGain = 0.0;
            survivalRateLoss = 0.0;

            // Instance Test
            foreach (Station s in stationList)
            {
                s.droneList.Clear();
                s.droneList.Add(new Drone(1));
                s.droneList.Add(new Drone(1));
            }

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
                    GetHighestSurvivalRateStation(stationList, ref current, e);
                e.assignedStationId = dispatchFrom;

                if (dispatchFrom == -1)
                {
                    noDrones++;
                }
                else if (dispatchFrom == -2)
                {
                    unreachableEvents++;
                }
                else
                {
                    double flightTime = pathPlanner.CalcuteFlightTime(e.kiloX, e.kiloY, stationList[dispatchFrom].kiloX, stationList[dispatchFrom].kiloY);
                    sum += CalculateSurvivalRate(flightTime);
                    current.Dispatch(dispatchFrom, e.occurrenceTime);
                }

                if (iteration++% 10000 == 0)
                {
                    Console.WriteLine("[" + iteration + "] time = " + e.occurrenceTime + ", unreachables = " + unreachableEvents + ", noDrones = " + noDrones);
                    Console.WriteLine("   secondChoices = " + secondChoices + ", loss = " + survivalRateLoss + ", gain = " + survivalRateGain + "\n");
                }
            }
            expectedSurvivalRate = sum / simulatedEventList.Count;
        }

        public List<OHCAEvent> GetSimulatedEvents()
        {
            return simulatedEventList;
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
                distance[i] = pathPlanner.CalcuteFlightTime(s.kiloX, s.kiloY, e.kiloX, e.kiloY);

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

            bool isReachable = false;
            
            for (;k < n; k++)
            {
                if (distance[k] <= Utils.GOLDEN_TIME)
                {
                    isReachable = true;
                    if (counter.whenReady[index[k]].Count < stationList[index[k]].droneList.Count)
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
        
        private int GetHighestSurvivalRateStation(List<Station> stationList, ref Counter counter, OHCAEvent e)
        {
            int resultIndex = -1;
            int nearestIndex = -1;
            double maxSurvivalRate = Double.NegativeInfinity;
            double maxOverallSurvivalRate = Double.NegativeInfinity;
            
            double potential = 0.0;
            double survivalRate = 0.0;
            double overallSurvivalRate = 0.0;

            counter.Flush(e.occurrenceTime);

            bool isReachable = false;
            foreach (RubisStation s in rStationList)
            {
                int index = rStationList.IndexOf(s);
                double distance = pathPlanner.CalcuteFlightTime(e.kiloX, e.kiloY, s.kiloX, s.kiloY);

                if (distance <= Utils.GOLDEN_TIME)
                {
                    isReachable = true;
                    if (counter.whenReady[index].Count < rStationList[index].droneList.Count)
                    {
                        potential = CalculatePotential(s, counter);
                        survivalRate = CalculateSurvivalRate(distance);
                        overallSurvivalRate = survivalRate - potential;

                        if (overallSurvivalRate > maxOverallSurvivalRate)
                        {
                            maxOverallSurvivalRate = overallSurvivalRate;
                            resultIndex = rStationList.IndexOf(s);
                        }

                        if (survivalRate > maxSurvivalRate)
                        {
                            maxSurvivalRate = survivalRate;
                            nearestIndex = rStationList.IndexOf(s);
                        }
                    }                    
                }
            }

            if (resultIndex != nearestIndex)
            {
                secondChoices++;
                double nearestDistance = pathPlanner.CalcuteFlightTime(e.kiloX, e.kiloY, rStationList[nearestIndex].kiloX, rStationList[nearestIndex].kiloY);
                double resultDistance = pathPlanner.CalcuteFlightTime(e.kiloX, e.kiloY, rStationList[resultIndex].kiloX, rStationList[resultIndex].kiloY);
                survivalRateLoss = survivalRateLoss + (CalculateSurvivalRate(resultDistance) - CalculateSurvivalRate(nearestDistance));

                // Look-ahead simulation
                Counter tempCounter = new Counter(counter);
                int eventIndex = simulatedEventList.IndexOf(e);
                tempCounter.Dispatch(nearestIndex, e.occurrenceTime);

                while (simulatedEventList[++eventIndex].occurrenceTime < e.occurrenceTime.AddHours(6))
                {
                    OHCAEvent next = simulatedEventList[eventIndex];
                    tempCounter.Flush(next.occurrenceTime);

                    int dispatchFrom = GetNearestStation(stationList, ref tempCounter, next);
                    if (dispatchFrom != -1 && dispatchFrom != -2)
                    {
                        double flightTime = pathPlanner.CalcuteFlightTime(next.kiloX, next.kiloY, stationList[dispatchFrom].kiloX, stationList[dispatchFrom].kiloY);
                        if (dispatchFrom == nearestIndex)
                        {
                            KeyValuePair<int, double> secondStation = GetSecondNearestStation(stationList, next);
                            if (nearestIndex != secondStation.Key)
                            {
                                survivalRateGain = survivalRateGain + (CalculateSurvivalRate(flightTime) - CalculateSurvivalRate(secondStation.Value));
                            }
                        }
                        tempCounter.Dispatch(dispatchFrom, next.occurrenceTime);
                    }
                }
            }

            if (resultIndex == -1)
            {
                noDrones++;
                return isReachable ? -1 : -2;
            }
            
            return resultIndex;
        }

        private KeyValuePair<int, double> GetSecondNearestStation(List<Station> stationList, OHCAEvent next)
        {
            List<KeyValuePair<int, double>> distanceList = new List<KeyValuePair<int, double>>();
            for(int i = 0; i < stationList.Count; i++)
            {
                Station s = stationList[i];
                double distance = pathPlanner.CalcuteFlightTime(next.kiloX, next.kiloY, s.kiloX, s.kiloY);
                distanceList.Add(new KeyValuePair<int, double>(i, distance));
            }
            distanceList.Sort((a, b) => a.Value >= b.Value ? 1 : -1);
            return distanceList[1];
        }

        private double CalculatePotential(RubisStation s, Counter counter)
        {
            double prev = GetOverallSurvivalRate(s, counter, false);
            double next = GetOverallSurvivalRate(s, counter, true);
            return (1 - ProbabilityMassFunction(0, Utils.DRONE_REST_TIME * 60 * s.pdfSum)) * (prev - next);
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
                        double prob = (1 - pSum) * ProbabilityMassFunction(ready[index] - 1, Utils.DRONE_REST_TIME * 60 * s.pdfSum);
                        pSum += prob;
                        cell.survivalRate = cell.survivalRate + (prob * CalculateSurvivalRate(pair.distance));
                    }
                }
                overallSum += cell.survivalRate;
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
            return (distance < Utils.GOLDEN_TIME) ? (0.7 - (0.2 * distance / Utils.GOLDEN_TIME)) : 0;
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

        public double GetSurvivalRateLoss()
        {
            return survivalRateLoss;
        }
        
        public double GetSurvivalRateGain()
        {
            return survivalRateGain;
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
                string[] dateComponents = values[2].Split('-', ' ', ':');
                int year = int.Parse(dateComponents[0]);
                int month = int.Parse(dateComponents[1]);
                int day = int.Parse(dateComponents[2]);
                int hour = int.Parse(dateComponents[4]);
                if (dateComponents[3].Equals("PM"))
                {
                    hour += 12;
                }
                if (hour % 12 == 0)
                {
                    hour -= 12;
                }
                int minute = int.Parse(dateComponents[5]);
                int second = int.Parse(dateComponents[6]);
                DateTime occurenceTime = new DateTime(year, month, day, hour, minute, second);
                simulatedEventList.Add(new OHCAEvent(kiloX, kiloY, occurenceTime));
                line = reader.ReadLine();
            }
            reader.Close();
            simulatedEventList.Sort((a, b) => a.occurrenceTime <= b.occurrenceTime ? -1 : 1);
            
            StreamWriter file = new StreamWriter("events.csv");
            for (int i = 0; i < simulatedEventList.Count; i++)
            {
                OHCAEvent e = simulatedEventList[i];
                file.Write(e.kiloX + "," + e.kiloY + "," + e.occurrenceTime + "\n");
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
