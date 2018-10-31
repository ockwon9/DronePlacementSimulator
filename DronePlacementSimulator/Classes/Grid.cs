using System;
using System.Collections.Generic;

namespace DronePlacementSimulator
{
    class Pair
    {
        public int xIndex;
        public int yIndex;
        public double kiloX;
        public double kiloY;

        public Pair(int xIndex, int yIndex, double kiloX, double kiloY)
        {
            this.xIndex = xIndex;
            this.yIndex = yIndex;
            this.kiloX = kiloX;
            this.kiloY = kiloY;
        }
    }

    class Grid
    {
        public List<Cell> cells;
        public int lambda_width;
        public int lambda_height;
        public double[][] lambda;
        public bool[][] inSeoulBool;
        public List<Pair> inSeoul;
        public double[][] pooledLambda;

        public Grid (ref List<List<double[]>> polyCoordList)
        {
            this.cells = new List<Cell>();
            int numLon = (int) Math.Ceiling(Utils.SEOUL_WIDTH / Utils.UNIT);
            int numLat = (int) Math.Ceiling(Utils.SEOUL_HEIGHT / Utils.UNIT);
            
            for (int i = 2; i < numLat; i++)
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

            this.lambda_width = (int) Math.Ceiling(Utils.SEOUL_WIDTH / Utils.LAMBDA_PRECISION);
            this.lambda_height = (int) Math.Ceiling(Utils.SEOUL_HEIGHT / Utils.LAMBDA_PRECISION);
            this.lambda = new double[lambda_height][];
            this.inSeoul = new List<Pair>();
            this.inSeoulBool = new bool[lambda_height][];
            this.pooledLambda = new double[(lambda_height + 2)/ 5][];
            for (int i = 0; i < lambda_height; i++)
            {
                this.lambda[i] = new double[lambda_width];
                this.inSeoulBool[i] = new bool[lambda_width];
                for (int j = 0; j < lambda_width; j++)
                {
                    this.lambda[i][j] = 0;
                    inSeoulBool[i][j] = (IsInside((j + 0.5) * Utils.LAMBDA_PRECISION, (i + 0.5) * Utils.LAMBDA_PRECISION, ref polyCoordList));
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
                int intX = (int)Math.Round(e.kiloX / Utils.LAMBDA_PRECISION - 0.5);
                int intY = (int)Math.Round(e.kiloY / Utils.LAMBDA_PRECISION - 0.5);

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

            for (int j = 0; j < this.lambda_height; j++)
            {
                for (int k = 0; k < this.lambda_width; k++)
                {
                    this.lambda[j][k] /= (Math.PI * Utils.MINUTES_IN_4_YEARS);
                    this.lambda[j][k] = this.lambda[j][k] * Utils.LAMBDA_PRECISION * Utils.LAMBDA_PRECISION;
                }
            }
        }

        public void Pool(ref List<List<double[]>> polyCoordList)
        {
            for (int i = 2; i < lambda_height; i += 5)
            {
                pooledLambda[i / 5] = new double[(lambda_width + 2) / 5];
                double kiloY = (i + 0.5) * Utils.LAMBDA_PRECISION;
                for (int j = 2; j < lambda_width; j += 5)
                {
                    double kiloX = (j + 0.5) * Utils.LAMBDA_PRECISION;
                    if (IsInside(kiloX, kiloY, ref polyCoordList))
                    {
                        inSeoul.Add(new Pair(j / 5, i / 5, (j - 2) * Utils.LAMBDA_PRECISION, (i - 2) * Utils.LAMBDA_PRECISION));
                    }

                    int remX = (lambda_height > i + 2) ? 2 : lambda_height - 1 - i;
                    int remY = (lambda_width > j + 2) ? 2 : lambda_width - 1 - j;
                    pooledLambda[i / 5][j / 5] = 0;
                    for (int k = -2; k <= remX; k++)
                    {
                        for (int l = -2; l <= remY; l++)
                        {
                            pooledLambda[i / 5][j / 5] += lambda[i + k][j + l];
                        }
                    }
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

