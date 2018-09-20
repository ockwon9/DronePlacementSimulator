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
        
        public Grid (double minLon, double minLat, double maxLon, double maxLat, double unit, ref List<OHCAEvent> eventList, ref List<Polygon> polygonList)
        {
            this.numCells = 0;
            this.cells = new List<double[]>();
            this.unit = unit;
            this.idw = new IdwInterpolator(2);
            int numLon = (int) Math.Ceiling((maxLon - minLon) / unit);
            int numLat = (int)Math.Ceiling((maxLat - minLat) / unit);

            for (int i = 0; i < numLat; i++)
            {
                for (int j = 0; j < numLon; j++)
                {
                    double lon = minLon + j * unit;
                    double lat = minLat + i * unit;

                    if (intersects(lon, lat, ref polygonList))
                    {
                        numCells++;
                        double[] coord = new double[2];
                        coord[0] = lon;
                        coord[1] = lat;
                        cells.Add(coord);
                    }
                }
            }

            this.pdf = new double[numCells];

            IdwInterpolate(ref eventList);
        }

        public bool intersects(double lon, double lat, ref List<Polygon> polygonList)
        {
            bool intersectsTop = false, intersectsBottom = false, intersectsLeft = false, intersectsRight = false;

            foreach (Polygon p in polygonList)
            {
                System.Windows.Media.PointCollection pc = p.Points;
                int n = pc.Count;
                System.Windows.Point p1 = pc[n - 1];
                System.Windows.Point p2 = pc[0];
                double x1 = p1.X, y1 = p1.Y;
                double x2 = p2.X, y2 = p2.Y;
                double temp;

                if (y1 != y2)
                {
                    if (!intersectsTop)
                    {
                        temp = ((y2 - lat) * x1 + (lat - y1) * x2) / (y2 - y1);
                        intersectsTop |= (lon <= temp) && (temp <= lon + unit);
                    }

                    if (!intersectsBottom)
                    {
                        temp = ((y2 - lat - unit) * x1 + (lat + unit - y1) * x2) / (y2 - y1);
                        intersectsBottom |= (lon <= temp) && (temp <= lon + unit);
                    }
                }

                if (x1 != x2)
                {
                    if (!intersectsLeft)
                    {
                        temp = ((x2 - lon) * y1 + (lon - x1) * y2) / (x2 - x1);
                        intersectsLeft |= (lat <= temp) && (temp <= lat + unit);
                    }

                    if (!intersectsRight)
                    {
                        temp = ((x2 - lon - unit) * y1 + (lon + unit - x1) * y2) / (x2 - x1);
                        intersectsRight |= (lat <= temp) && (temp <= lat + unit);
                    }
                }

                for (int i = 1; i < n; i++)
                {
                    p2 = pc[i];
                    x1 = x2;
                    y1 = y2;
                    x2 = p2.X;
                    y2 = p2.Y;

                    if (y1 != y2)
                    {
                        if (!intersectsTop)
                        {
                            temp = ((y2 - lat) * x1 + (lat - y1) * x2) / (y2 - y1);
                            intersectsTop |= (lon <= temp) && (temp <= lon + unit);
                        }

                        if (!intersectsBottom)
                        {
                            temp = ((y2 - lat - unit) * x1 + (lat + unit - y1) * x2) / (y2 - y1);
                            intersectsBottom |= (lon <= temp) && (temp <= lon + unit);
                        }
                    }

                    if (x1 != x2)
                    {
                        if (!intersectsLeft)
                        {
                            temp = ((x2 - lon) * y1 + (lon - x1) * y2) / (x2 - x1);
                            intersectsLeft |= (lat <= temp) && (temp <= lat + unit);
                        }

                        if (!intersectsRight)
                        {
                            temp = ((x2 - lon - unit) * y1 + (lon + unit - x1) * y2) / (x2 - x1);
                            intersectsRight |= (lat <= temp) && (temp <= lat + unit);
                        }
                    }
                }
            }

            return intersectsTop || intersectsBottom || intersectsLeft || intersectsRight;
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
