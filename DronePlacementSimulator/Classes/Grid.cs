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

        public bool[][] intersect;
        public List<int[]> intCoords;
        
        public Grid (double minLon, double minLat, double maxLon, double maxLat, double unit, ref List<OHCAEvent> eventList, ref List<List<double[]>> polyCoordList)
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

            IdwInterpolate(ref eventList);
        }

        public bool intersects(double lon, double lat, ref List<List<double[]>> polyCoordList)
        {
            Console.WriteLine("lon = " + lon + ", lat = " + lat);
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
            Dictionary<double[], int> eventDict = new Dictionary<double[], int>();

            foreach (var ohca in eventList)
            {
                double[] ohcaGps = new double[2] { ohca.longitude, ohca.latitude };
                if (eventDict.ContainsKey(ohcaGps))
                {
                    eventDict[ohcaGps]++;
                }
                else
                {
                    eventDict.Add(ohcaGps, 1);
                }
            }

            foreach (KeyValuePair<double[], int> entry in eventDict)
            {
                idw.AddPoint((double) entry.Value, entry.Key);
            }

            int i = 0;
            foreach (double[] temp in cells)
            {
                this.pdf[i] = idw.Interpolate(temp).Value;
                i++;
            }

            return;
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
