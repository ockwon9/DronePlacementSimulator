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
    class Grid
    {
        public List<double[]> cells;
        public double unit;
        public double[] pdf;
        public IdwInterpolator idw;
        
        public List<int[]> intCoords;
        public static Random rand = new Random();

        public Grid (double minLon, double minLat, double maxLon, double maxLat, double unit, ref List<List<double[]>> polyCoordList)
        {
            this.cells = new List<double[]>();
            this.unit = unit;
            this.idw = new IdwInterpolator(2);
            int numLon = (int)Math.Ceiling((maxLon - minLon) / unit);
            int numLat = (int)Math.Ceiling((maxLat - minLat) / unit);

            this.intCoords = new List<int[]>();
            for (int i = 0; i < numLat; i++)
            {
                double lat = minLat + i * unit;
                for (int j = 0; j < numLon; j++)
                {
                    double lon = minLon + j * unit;
                    if (Intersects(lon, lat, ref polyCoordList))
                    {
                        cells.Add(new double[] { lon, lat });
                        intCoords.Add(new int[] { i, j });
                    }
                }
            }

            this.pdf = new double[cells.Count];
        }

        public Grid(Grid temp)
        {
            this.numCells = temp.numCells;
            this.cells = new List<double[]>(temp.cells);
            this.unit = temp.unit;
            this.idw = new IdwInterpolator(2);
            this.intCoords = new List<int[]>(temp.intCoords);
            this.pdf = new double[numCells];
            Array.Copy(temp.pdf, this.pdf, this.numCells);
        }

        public bool Intersects(double lon, double lat, ref List<List<double[]>> polyCoordList)
        {
            bool ans = false, intersectsTop = false, intersectsBottom = false, intersectsLeft = false, intersectsRight = false;

            foreach (List<double[]> pList in polyCoordList)
            {
                int n = pList.Count;
                double[] p1 = pList[n - 1];
                double[] p2 = pList[0];
                double temp;

                if (p1[1] != p2[1])
                {
                    if (!intersectsBottom)
                    {
                        temp = ((p2[1] - lat) * p1[0] + (lat - p1[1]) * p2[0]) / (p2[1] - p1[1]);
                        intersectsBottom = ((temp - p1[0]) * (temp - p2[0]) < 0) && (lon <= temp) && (temp <= lon + unit);
                    }

                    if (!intersectsTop)
                    {
                        temp = ((p2[1] - lat - unit) * p1[0] + (lat + unit - p1[1]) * p2[0]) / (p2[1] - p1[1]);
                        intersectsTop = ((temp - p1[0]) * (temp - p2[0]) < 0) && (lon <= temp) && (temp <= lon + unit);
                    }
                }

                if (p1[0] != p2[0])
                {
                    if (!intersectsLeft)
                    {
                        temp = ((p2[0] - lon) * p1[1] + (lon - p1[0]) * p2[1]) / (p2[0] - p1[0]);
                        intersectsLeft = ((temp - p1[1]) * (temp - p2[1]) < 0) && (lat <= temp) && (temp <= lat + unit);
                    }

                    if (!intersectsRight)
                    {
                        temp = ((p2[0] - lon - unit) * p1[1] + (lon + unit - p1[0]) * p2[1]) / (p2[0] - p1[0]);
                        intersectsRight = ((temp - p1[1]) * (temp - p2[1]) < 0) && (lat <= temp) && (temp <= lat + unit);
                    }
                }

                ans = intersectsBottom | intersectsTop | intersectsLeft | intersectsRight;

                for (int i = 1; i < n; i++)
                {
                    if (ans)
                        break;

                    p1 = p2;
                    p2 = pList[i];

                    if (p1[1] != p2[1])
                    {
                        if (!intersectsBottom)
                        {
                            temp = ((p2[1] - lat) * p1[0] + (lat - p1[1]) * p2[0]) / (p2[1] - p1[1]);
                            intersectsBottom = ((temp - p1[0]) * (temp - p2[0]) < 0) && (lon <= temp) && (temp <= lon + unit);
                        }

                        if (!intersectsTop)
                        {
                            temp = ((p2[1] - lat - unit) * p1[0] + (lat + unit - p1[1]) * p2[0]) / (p2[1] - p1[1]);
                            intersectsTop = ((temp - p1[0]) * (temp - p2[0]) < 0) && (lon <= temp) && (temp <= lon + unit);
                        }
                    }

                    if (p1[0] != p2[0])
                    {
                        if (!intersectsLeft)
                        {
                            temp = ((p2[0] - lon) * p1[1] + (lon - p1[0]) * p2[1]) / (p2[0] - p1[0]);
                            intersectsLeft = ((temp - p1[1]) * (temp - p2[1]) < 0) && (lat <= temp) && (temp <= lat + unit);
                        }

                        if (!intersectsRight)
                        {
                            temp = ((p2[0] - lon - unit) * p1[1] + (lon + unit - p1[0]) * p2[1]) / (p2[0] - p1[0]);
                            intersectsRight = ((temp - p1[1]) * (temp - p2[1]) < 0) && (lat <= temp) && (temp <= lat + unit);
                        }
                    }
                    ans = intersectsBottom | intersectsTop | intersectsLeft | intersectsRight;
                }
            }

            return ans;
        }

        public void IdwInterpolate(ref List<OHCAEvent> eventList)
        {
            List<CSharpIDW.Point> eventLocations = new List<CSharpIDW.Point>();
            int[][] count = new int[370][];
            for (int i = 0; i < 370; i++)
            {
                count[i] = new int[305];
                for (int j = 0; j < 305; j++)
                {
                    count[i][j] = 0;
                }
            }

            foreach (OHCAEvent e in eventList)
            {
                count[(int)Math.Round(10 * e.kiloX + 0.5)][(int)Math.Round(10 * e.kiloY + 0.5)]++;
            }

            for (int i = 0; i < 370; i++)
            {
                for (int j = 0; j < 305; j++)
                {
                    eventLocations.Add(new CSharpIDW.Point((double)count[i][j], i / 10.0 + 0.05, j / 10.0 + 0.05));
                }
            }
            
            const int power = 1;
            const int dimension = 2;            
            var interpolator = new IdwInterpolator(dimension, power, Utils.NUMBER_OF_NEIGHBORS);
            interpolator.AddPointRange(eventLocations);

            for (int i = 0; i < cells.Count; i++)
            {
                this.pdf[i] = (Utils.UNIT * 10) * (Utils.UNIT * 10) * interpolator.Interpolate(this.cells[i]).Value;
            }
        }

        public double GetMaxDemand()
        {
            double mD = 0;
            for (int i = 0; i < cells.Count; i++)
            {
                if (pdf[i] > mD)
                {
                    mD = pdf[i];
                }
            }

            return mD;
        }

        public int SelectCell()
        {
            double poolSize = 0;
            for (int i = 0; i < pdf.Length; i++)
            {
                poolSize += pdf[i];
            }

            double randomNumber = rand.NextDouble() * poolSize;
            double accumulatedProbability = 0.0f;
            for (int i = 0; i < pdf.Length; i++)
            {
                accumulatedProbability += pdf[i];
                if (randomNumber <= accumulatedProbability)
                    return i;
            }
            return -1;
        }
    }
}
