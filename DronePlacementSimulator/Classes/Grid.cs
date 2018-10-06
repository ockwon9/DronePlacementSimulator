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
                    if (IsInside(lon + 0.5 * unit, lat + 0.5 * unit, ref polyCoordList))
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
            this.cells = new List<double[]>(temp.cells);
            this.unit = temp.unit;
            this.idw = new IdwInterpolator(2);
            this.intCoords = new List<int[]>(temp.intCoords);
            this.pdf = new double[cells.Count];
            Array.Copy(temp.pdf, this.pdf, this.cells.Count);
        }

        public bool IsInside(double lon, double lat, ref List<List<double[]>> polyCoordList)
        {
            foreach (List<double[]> pList in polyCoordList)
            {
                int n = pList.Count;
                double[] p1 = pList[n - 1];
                double[] p2 = pList[0];
                double temp;
                int leftCount = 0;
                bool onLine = false;

                if (p1[1] == p2[1])
                {
                    onLine |= (p1[1] == lat && (p1[0] - lon) * (p2[0] - lon) <= 0);
                }
                else if ((p1[1] - lat) * (p2[1] - lat) <= 0)
                {
                    temp = ((p2[1] - lat) * p1[0] + (lat - p1[1]) * p2[0]) / (p2[1] - p1[1]);
                    onLine |= (temp == lon);
                    leftCount += (temp < lon) ? 1 : 0;
                }

                for (int i = 1; i < n; i++)
                {
                    p1 = p2;
                    p2 = pList[i];

                    if (p1[1] == p2[1])
                    {
                        onLine |= (p1[1] == lat && (p1[0] - lon) * (p2[0] - lon) <= 0);
                    }
                    else if ((p1[1] - lat) * (p2[1] - lat) <= 0)
                    {
                        temp = ((p2[1] - lat) * p1[0] + (lat - p1[1]) * p2[0]) / (p2[1] - p1[1]);
                        onLine |= (temp == lon);
                        leftCount += (temp < lon) ? 1 : 0;
                    }
                }

                if (onLine || leftCount % 2 != 0)
                {
                    return true;
                }
            }

            return false;
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
