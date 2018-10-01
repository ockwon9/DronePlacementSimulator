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
    class Grid
    {
        public int numCells;
        public List<double[]> cells;
        public double unit;
        public double[] pdf;
        public IdwInterpolator idw;
        
        public List<int[]> intCoords;
        
        public Grid (double minLon, double minLat, double maxLon, double maxLat, double unit, ref List<List<double[]>> polyCoordList)
        {
            this.numCells = 0;
            this.cells = new List<double[]>();
            this.unit = unit;
            this.idw = new IdwInterpolator(2);
            int numLon = (int) Math.Ceiling((maxLon - minLon) / unit);
            int numLat = (int)Math.Ceiling((maxLat - minLat) / unit);

            intCoords = new List<int[]>();
            for (int i = 0; i < numLat; i++)
            {
                for (int j = 0; j < numLon; j++)
                {
                    double lon = minLon + j * unit;
                    double lat = minLat + i * unit;

                    if (intersects(lon, lat, ref polyCoordList))
                    {
                        numCells++;
                        double[] coord = new double[2];
                        coord[0] = lon;
                        coord[1] = lat;
                        cells.Add(coord);
                        int[] intCoord = new int[2];
                        intCoord[0] = i;
                        intCoord[1] = j;
                        intCoords.Add(intCoord);
                    }
                }
            }

            this.pdf = new double[numCells];
        }

        public bool intersects(double lon, double lat, ref List<List<double[]>> polyCoordList)
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
            int[][] count = new int[3690][];
            for (int i = 0; i < 3690; i++)
            {
                count[i] = new int[3036];
                for (int j = 0; j < 3036; j++)
                {
                    count[i][j] = 0;
                }
            }

            foreach (OHCAEvent e in eventList)
            {
                count[(int)Math.Round(100 * e.kiloX)][(int)Math.Round(100 * e.kiloY)]++;
            }

            for (int i = 0; i < 3690; i++)
            {
                for (int j = 0; j < 3036; j++)
                {
                    eventLocations.Add(new CSharpIDW.Point((double)count[i][j], i / 100.0, j / 100.0));
                }
            }
            
            const int power = 3;
            const int dimension = 2;
            const int numberOfNeighbors = 3;
            var interpolator = new IdwInterpolator(dimension, power, numberOfNeighbors);
            interpolator.AddPointRange(eventLocations);

            for (int i = 0; i < numCells; i++)
            {
                this.pdf[i] = interpolator.Interpolate(this.cells[i]).Value;
            }
        }

        public double getMaxDemand()
        {
            double mD = 0;
            for (int i = 0; i < numCells; i++)
            {
                if (pdf[i] > mD)
                {
                    mD = pdf[i];
                }
            }

            return mD;
        }
    }
}
