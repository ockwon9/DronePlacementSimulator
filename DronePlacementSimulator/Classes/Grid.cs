using System;
using System.Collections.Generic;
using System.Device.Location;
using System.IO;

namespace DronePlacementSimulator
{
    class Pair
    {
        public int row, col;

        public Pair(int row, int col)
        {
            this.row = row;
            this.col = col;
        }
    }

    class Grid
    {
        public double[,] lambda;
        public bool[,] inSeoul;
        public List<Pair> seoulCells;

        public Grid (ref List<List<GeoCoordinate>> polyCoordList)
        {
            this.lambda = new double[Utils.ROW_NUM, Utils.COL_NUM];
            this.inSeoul = new bool[Utils.ROW_NUM, Utils.COL_NUM];
            this.seoulCells = new List<Pair>();
            for (int i = 0; i < Utils.ROW_NUM; i++)
            {
                for (int j = 0; j < Utils.COL_NUM; j++)
                {
                    this.lambda[i, j] = 0;
                    inSeoul[i, j] = (IsInside(Utils.ConvertRowToLat(i), Utils.ConvertColToLon(j), ref polyCoordList));
                    if (inSeoul[i, j])
                    {
                        seoulCells.Add(new Pair(i, j));
                    }
                }
            }

            if (File.Exists("inSeoul.csv"))
            {
                StreamReader file = new StreamReader("inSeoul.csv");
                String line = file.ReadLine();
                int row = 0;
                while (line != null)
                {
                    string[] cells = line.Split(',');
                    for (int col = 0; col < Utils.COL_NUM; col++)
                    {
                        inSeoul[row, col] = Boolean.Parse(cells[col]);
                    }
                    row++;
                    line = file.ReadLine();
                }
                file.Close();
            }
            else
            {
                StreamWriter file = new StreamWriter("inSeoul.csv");
                for (int i = 0; i < Utils.ROW_NUM; i++)
                {
                    for (int j = 0; j < Utils.COL_NUM; j++)
                    {
                        file.Write(inSeoul[i, j]);
                        file.Write(",");
                    }
                    file.Write("\n");
                }
                file.Close();
            }
        }

        public Grid(Grid temp)
        {
            this.lambda = temp.lambda as double[,];
            this.inSeoul = temp.inSeoul as bool[,];
        }

        public bool IsInside(double lat, double lon, ref List<List<GeoCoordinate>> polyCoordList)
        {
            foreach (List<GeoCoordinate> pList in polyCoordList)
            {
                int n = pList.Count;
                GeoCoordinate p1 = pList[n - 1];
                GeoCoordinate p2 = pList[0];
                double temp;
                int leftCount = 0;
                bool onLine = false;

                if (p1.Latitude == p2.Latitude)
                {
                    onLine |= (p1.Latitude == lat && (p1.Longitude - lon) * (p2.Longitude - lon) <= 0);
                }
                else if ((p1.Latitude - lat) * (p2.Latitude - lat) <= 0)
                {
                    temp = ((p2.Latitude - lat) * p1.Longitude + (lat - p1.Latitude) * p2.Longitude) / (p2.Latitude - p1.Latitude);
                    onLine |= (temp == lon);
                    leftCount += (temp < lon) ? 1 : 0;
                }

                for (int i = 1; i < n; i++)
                {
                    p1 = p2;
                    p2 = pList[i];

                    if (p1.Latitude == p2.Latitude)
                    {
                        onLine |= (p1.Latitude == lat && (p1.Longitude - lon) * (p2.Longitude - lon) <= 0);
                    }
                    else if ((p1.Latitude - lat) * (p2.Latitude - lat) <= 0)
                    {
                        temp = ((p2.Latitude - lat) * p1.Longitude + (lat - p1.Latitude) * p2.Longitude) / (p2.Latitude - p1.Latitude);
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

        public void Interpolate(ref List<OHCAEvent> eventList)
        {
            int[,] eventCount = new int[Utils.ROW_NUM, Utils.COL_NUM];

            foreach (OHCAEvent e in eventList)
            {
                int row = Utils.ConvertLatToRow(e.lat);
                int col = Utils.ConvertLonToCol(e.lon);
                eventCount[row, col]++;
            }

            // TODO : Poisson Kriging
            double[,] count = new double[Utils.ROW_NUM, Utils.COL_NUM];
            for (int i = 0; i < Utils.ROW_NUM; i++)
            {
                for (int j = 0; j < Utils.COL_NUM; j++)
                {
                    lambda[i, j] = eventCount[i, j];
                    count[i, j] = 1.0;
                }
            }

            double side = 0.5;
            double diag = 0.3;

            for (int i = 1; i < Utils.ROW_NUM; i++)
            {
                for (int j = 0; j < Utils.COL_NUM; j++)
                {
                    count[i, j] += side;
                    lambda[i, j] += eventCount[i - 1, j] * side;
                }

                for (int j = 1; j < Utils.COL_NUM; j++)
                {
                    count[i, j] += diag;
                    lambda[i, j] += eventCount[i - 1, j - 1] * diag;
                }

                for (int j = 0; j < Utils.COL_NUM - 1; j++)
                {
                    count[i, j] += diag;
                    lambda[i, j] += eventCount[i - 1, j + 1] * diag;
                }
            }

            for (int i = 0; i < Utils.ROW_NUM - 1; i++)
            {
                for (int j = 0; j < Utils.COL_NUM; j++)
                {
                    count[i, j] += side;
                    lambda[i, j] += eventCount[i + 1, j] * side;
                }

                for (int j = 1; j < Utils.COL_NUM; j++)
                {
                    count[i, j] += diag;
                    lambda[i, j] += eventCount[i + 1, j - 1] * diag;
                }

                for (int j = 0; j < Utils.COL_NUM - 1; j++)
                {
                    count[i, j] += diag;
                    lambda[i, j] += eventCount[i + 1, j + 1] * diag;
                }
            }

            for (int i = 0; i < Utils.ROW_NUM; i++)
            {
                for (int j = 1; j < Utils.COL_NUM; j++)
                {
                    count[i, j] += side;
                    lambda[i, j] += eventCount[i, j - 1] * side;
                }

                for (int j = 0; j < Utils.COL_NUM - 1; j++)
                {
                    count[i, j] += side;
                    lambda[i, j] += eventCount[i, j + 1] * side;
                }
            }

            for (int i = 0; i < Utils.ROW_NUM; i++)
            {
                for (int j = 0; j < Utils.COL_NUM; j++)
                {
                    lambda[i, j] /= count[i, j];
                    lambda[i, j] /= 100000;
                }
            }
            // End of Temporary Solution
        }

        public double GetMaxDemand()
        {
            double maxDemand = Double.NegativeInfinity;
            for (int i = 0; i < Utils.ROW_NUM; i++)
            {
                for (int j = 0; j < Utils.COL_NUM; j++)
                {
                    if (maxDemand < lambda[i, j])
                    {
                        maxDemand = lambda[i, j];
                    }
                }
            }

            return maxDemand;
        }
    }
}

