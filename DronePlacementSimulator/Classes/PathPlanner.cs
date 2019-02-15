using System;
using System.IO;

namespace DronePlacementSimulator
{
    class PathPlanner
    {
        private double[,] land_elevation;
        private double[,] building_height;

        public PathPlanner()
        {
            land_elevation = new double[Utils.ROW_NUM, Utils.COL_NUM];
            building_height = new double[Utils.ROW_NUM, Utils.COL_NUM];

            for (int i = 0; i < Utils.ROW_NUM; i++)
            {
                for (int j = 0; j < Utils.COL_NUM; j++)
                {
                    land_elevation[i, j] = 0.0;
                    building_height[i, j] = 0.0;
                }
            }

            if (File.Exists("land_elevation.txt") && File.Exists("building_height.txt"))
            {
                ReadHeight();
            }
            else
            {
                WriteHeight();
            }
        }

        public double CalculateFlightTime(double srcLat, double srcLon, double dstLat, double dstLon)
        {
            double distance = Utils.GetDistance(srcLat, srcLon, dstLat, dstLon);

            int srcRow = Utils.ConvertLatToRow(srcLat);
            int srcCol = Utils.ConvertLonToCol(srcLon);
            double srcHeight = land_elevation[srcRow, srcCol];

            int dstRow = Utils.ConvertLatToRow(dstLat);
            int dstCol = Utils.ConvertLonToCol(dstLon);
            double dstHeight = land_elevation[dstRow, dstCol];

            double maxHeightOnRoute = getMaxHeight(srcLat, srcLon, dstLat, dstLon);
            double takeOffHeight = maxHeightOnRoute - srcHeight + Utils.BASE_FLIGHT_HEIGHT;
            double landdingHeight = maxHeightOnRoute - dstHeight + Utils.BASE_FLIGHT_HEIGHT;
            
            return (takeOffHeight / Utils.DRONE_TAKE_OFF_VELOCITY) + (distance / Utils.DRONE_VELOCITY) + (landdingHeight / Utils.DRONE_LANDING_VELOCITY);
        }

        private double Intersect(int row1, int col1, int row2, int col2, int row)
        {
            return col1 + 0.5 + (col2 - col1) * (row - row1 - 0.5) / (row2 - row1);
        }

        private double getMaxHeight(double srcLat, double srcLon, double dstLat, double dstLon)
        {
            int srcRow = Utils.ConvertLatToRow(srcLat);
            int srcCol = Utils.ConvertLonToCol(srcLon);

            int dstRow = Utils.ConvertLatToRow(dstLat);
            int dstCol = Utils.ConvertLonToCol(dstLon);

            if (srcRow > dstRow || srcRow == dstRow && srcCol > dstCol)
            {
                Swap<int>(ref srcRow, ref dstRow);
                Swap<int>(ref srcCol, ref dstCol);
            }

            int diffRow = dstRow - srcRow;
            int diffCol = dstCol - srcCol;

            double maxHeight = 0.0;

            if (srcRow == dstRow)
            {
                for (int col = srcCol; col <= dstCol; col++)
                {
                    double height = getHeight(srcRow, col);
                    if (height > maxHeight)
                    {
                        maxHeight = height;
                    }
                }
            }
            else
            {
                int minCol = (srcCol < dstCol) ? srcCol : dstCol;
                int maxCol = (srcCol > dstCol) ? srcCol : dstCol;
                for (int row = srcRow; row <= dstRow; row++)
                {
                    int min = (int)Math.Floor(Intersect(srcRow, srcCol, dstRow, dstCol, row));
                    if (min < minCol)
                    {
                        min = minCol;
                    }

                    int max = (int)Math.Ceiling(Intersect(srcRow, srcCol, dstRow, dstCol, row + 1));
                    if (max > maxCol)
                    {
                        max = maxCol;
                    }

                    for (int col = min; col <= max; col++)
                    {
                        double height = getHeight(row, col);
                        if (height > maxHeight)
                        {
                            maxHeight = height;
                        }
                    }
                }
            }

            return maxHeight;
        }

        private double getHeight(int row, int col)
        {
            return land_elevation[row, col] + building_height[row, col];
        }

        private void ReadHeight()
        {
            StreamReader sr1 = new StreamReader("land_elevation.txt");
            StreamReader sr2 = new StreamReader("building_height.txt");
            for (int i = 0; i < Utils.ROW_NUM; i++)
            {
                for (int j = 0; j < Utils.COL_NUM; j++)
                {
                    land_elevation[i, j] = Double.Parse(sr1.ReadLine().Split('\t')[2]) / 1000;
                    building_height[i, j] = Double.Parse(sr2.ReadLine().Split('\t')[2]) / 1000;
                }
            }
            sr1.Close();
            sr2.Close();
        }

        private void WriteHeight()
        {
            double le, bh;

            StreamReader objReader = new StreamReader("seoul.txt");
            string line = "";
            line = objReader.ReadLine();
            line = objReader.ReadLine();

            for (int i = 0; i < 20000; i++)
            {
                for (int j = 0; j < 20000; j++)
                {
                    try
                    {
                        line = objReader.ReadLine();
                        if (line != null)
                        {
                            string[] data = line.Split(' ');
                            bh = Double.Parse(data[4]);
                            le = Double.Parse(data[5]);

                            int m = (int)Math.Round((19999.5 - i) * Utils.ROW_NUM / 20000 - 0.5);
                            int n = (int)Math.Round((0.5 + j) * Utils.COL_NUM / 20000 - 0.5);
                            if (le > land_elevation[m, n])
                            {
                                land_elevation[m, n] = le;
                            }
                            if (bh > building_height[m, n])
                            {
                                building_height[m, n] = bh;
                            }
                        }
                    }
                    catch (Exception)
                    {
                        return;
                    }
                }
            }
            objReader.Close();

            StreamWriter file1 = new StreamWriter("land_elevation.txt");
            StreamWriter file2 = new StreamWriter("building_height.txt");
            for (int i = 0; i < Utils.ROW_NUM; i++)
            {
                for (int j = 0; j < Utils.COL_NUM; j++)
                {
                    file1.WriteLine(String.Format("{0}\t{1}\t{2}", i, j, land_elevation[i, j]));
                    file2.WriteLine(String.Format("{0}\t{1}\t{2}", i, j, building_height[i, j]));
                }
            }
            file1.Close();
            file2.Close();
        }

        private static void Swap<T>(ref T lhs, ref T rhs)
        {
            T temp = lhs;
            lhs = rhs;
            rhs = temp;
        }
    }
}
