using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Windows.Shapes;

namespace DronePlacementSimulator
{
    class Cell
    {
        public double kiloX;
        public double kiloY;
        public int intX;
        public int intY;
        public int eventCount;

        public Cell()
        {
        }

        public Cell(double kiloX, double kiloY, int j, int i)
        {
            this.kiloX = kiloX;
            this.kiloY = kiloY;
            this.intX = j;
            this.intY = i;
            this.eventCount = 0;
        }

        public void addEvent()
        {
            this.eventCount++;
        }
    }

    class Grid
    {
        public List<Cell> cells;
        private int lambda_width;
        private int lambda_height;
        public double[][] lambda;

        public Grid (ref List<List<double[]>> polyCoordList)
        {
            this.cells = new List<Cell>();
            int numLon = (int) Math.Ceiling(Utils.SEOUL_WIDTH / Utils.UNIT);
            int numLat = (int) Math.Ceiling(Utils.SEOUL_HEIGHT / Utils.UNIT);
            
            for (int i = 0; i < numLat; i++)
            {
                double kiloY = (i + 0.5) * Utils.UNIT;
                for (int j = 0; j < numLon; j++)
                {
                    double kiloX = (j + 0.5) * Utils.UNIT;
                    if (IsInside(kiloX, kiloY, ref polyCoordList))
                    {   
                        cells.Add(new Cell(kiloX, kiloY, j, i));
                    }
                }
            }

            this.lambda_width = (int )Math.Ceiling(Utils.SEOUL_WIDTH / Utils.LAMBDA_PRECISION);
            this.lambda_height = (int) Math.Ceiling(Utils.SEOUL_HEIGHT / Utils.LAMBDA_PRECISION);
            this.lambda = new double[lambda_height][];
            for (int i = 0; i < lambda_height; i++)
            {
                this.lambda[i] = new double[lambda_width];
                for (int j = 0; j < lambda_width; j++)
                {
                    this.lambda[i][j] = 0;
                }
            }
        }

        public Grid(Grid temp)
        {
            this.cells = new List<Cell>(temp.cells);
        }

        public bool IsInside(double kiloX, double kiloY, ref List<List<double[]>> polyCoordList)
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
                    onLine |= (p1[1] == kiloY && (p1[0] - kiloX) * (p2[0] - kiloX) <= 0);
                }
                else if ((p1[1] - kiloY) * (p2[1] - kiloY) <= 0)
                {
                    temp = ((p2[1] - kiloY) * p1[0] + (kiloY - p1[1]) * p2[0]) / (p2[1] - p1[1]);
                    onLine |= (temp == kiloX);
                    leftCount += (temp < kiloX) ? 1 : 0;
                }

                for (int i = 1; i < n; i++)
                {
                    p1 = p2;
                    p2 = pList[i];

                    if (p1[1] == p2[1])
                    {
                        onLine |= (p1[1] == kiloY && (p1[0] - kiloX) * (p2[0] - kiloX) <= 0);
                    }
                    else if ((p1[1] - kiloY) * (p2[1] - kiloY) <= 0)
                    {
                        temp = ((p2[1] - kiloY) * p1[0] + (kiloY - p1[1]) * p2[0]) / (p2[1] - p1[1]);
                        onLine |= (temp == kiloX);
                        leftCount += (temp < kiloX) ? 1 : 0;
                    }
                }

                if (onLine || leftCount % 2 != 0)
                {
                    return true;
                }
            }

            return false;
        }

        public void Interpolate(ref List<OHCAEvent> eventList)
        {
            for (int i = 0; i < eventList.Count; i++)
            {
                OHCAEvent e = eventList[i];
                int intX = (int) Math.Round(e.kiloX / Utils.LAMBDA_PRECISION - 0.5);
                int intY = (int) Math.Round(e.kiloY / Utils.LAMBDA_PRECISION - 0.5);

                for (int j = 0; j < this.lambda_height; j++)
                {
                    for (int k = 0; k < this.lambda_width; k++)
                    {
                        double x = (k - intX) * Utils.LAMBDA_PRECISION;
                        double y = (j - intY) * Utils.LAMBDA_PRECISION;
                        double temp = 1 + x * x + y * y;
                        this.lambda[j][k] += 1 / temp / temp;
                    }
                }
            }

            for (int j =  0; j < this.lambda_height; j++)
            {
                for (int k = 0; k < this.lambda_width; k++)
                {
                    this.lambda[j][k] /= (Math.PI * Utils.MINUTES_IN_4_YEARS);
                    this.lambda[j][k] = this.lambda[j][k] * Utils.LAMBDA_PRECISION * Utils.LAMBDA_PRECISION;
                }
            }
        }

        public double GetMaxDemand()
        {
            double maxDemand = Double.NegativeInfinity;
            for (int i = 0; i < this.lambda.Length; i++)
            {
                for (int j = 0; j < this.lambda[i].Length; j++)
                {
                    if (maxDemand < lambda[i][j])
                    {
                        maxDemand = lambda[i][j];
                    }
                }
            }

            return maxDemand;
        }
    }
}
