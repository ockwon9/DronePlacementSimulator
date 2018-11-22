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

        public double CalcuteFlightTime(double srcX, double srcY, double dstX, double dstY)
        {
            if (srcX < 0 || srcY < 0 || dstX < 0 || dstY < 0)
            {
                return Double.PositiveInfinity;
            }

            double distance = GetDistance(srcX, srcY, dstX, dstY);

            int srcCol = ConvertKiloToCol(srcX);
            int srcRow = ConvertKiloToRow(srcY);
            double srcHeight = land_elevation[srcRow, srcCol];

            int dstCol = ConvertKiloToCol(dstX);
            int dstRow = ConvertKiloToRow(dstY);
            double dstHeight = land_elevation[dstRow, dstCol];

            double maxHeightOnRoute = getMaxHeight(srcX, srcY, dstX, dstY);
            double takeOffHeight = maxHeightOnRoute - srcHeight + Utils.BASE_FLIGHT_HEIGHT;
            double landdingHeight = maxHeightOnRoute - dstHeight + Utils.BASE_FLIGHT_HEIGHT;
            
            return (takeOffHeight / Utils.DRONE_TAKE_OFF_VELOCITY / 60) + (distance / Utils.DRONE_VELOCITY) + (landdingHeight / Utils.DRONE_LANDING_VELOCITY / 60);
        }

        private double getMaxHeight(double srcX, double srcY, double dstX, double dstY)
        {
            if (srcX > dstX)
            {
                Swap<double>(ref srcX, ref dstX);
                Swap<double>(ref srcY, ref dstY);
            }

            int srcCol = ConvertKiloToCol(srcX);
            int srcRow = ConvertKiloToRow(srcY);

            int dstCol = ConvertKiloToCol(dstX);
            int dstRow = ConvertKiloToRow(dstY);

            int diffCol = dstCol - srcCol;
            int diffRow = dstRow - srcRow;
            int rowIncrement = (diffRow < 0) ? -1 : 1;

            double maxHeight = 0.0;

            if (diffCol == 0) // Vertical movement
            {
                for (int i = 0; i <= diffRow; i = i + 100)
                {
                    double height = getHeight(srcRow + (i * rowIncrement), srcCol);
                    if (height > maxHeight)
                    {
                        maxHeight = height;
                    }
                }
            }
            else
            {
                for (int i = 0; i <= diffCol; i = i + 100)
                {
                    int midCol = srcCol + i; // The mid index between srcCol and dstCol
                    int midRow = srcRow + (int)(diffRow * i / diffCol); // The mid index between srcRow and dstRow

                    double height = getHeight(midRow, midCol);
                    if (height > maxHeight)
                    {
                        maxHeight = height;
                    }
                }
            }

            return maxHeight;
        }

        private double getHeight(int row, int col)
        {
            return land_elevation[row, col] + building_height[row, col];
        }

        private int ConvertKiloToCol(double kiloX)
        {
            return (int)(Utils.COL_NUM * kiloX / Utils.SEOUL_WIDTH);
        }

        private int ConvertKiloToRow(double kiloY)
        {
            return (int)(Utils.ROW_NUM * (Utils.SEOUL_HEIGHT - kiloY) / Utils.SEOUL_HEIGHT);
        }
        
        private double GetDistance(double x1, double y1, double x2, double y2)
        {
            return Math.Sqrt((x1 - x2) * (x1 - x2) + (y1 - y2) * (y1 - y2));
        }

        private void ReadHeight()
        {
            StreamReader sr1 = new StreamReader("land_elevation.txt");
            StreamReader sr2 = new StreamReader("building_height.txt");
            for (int i = 0; i < Utils.ROW_NUM; i++)
            {
                for (int j = 0; j < Utils.COL_NUM; j++)
                {
                    land_elevation[i, j] = Double.Parse(sr1.ReadLine().Split('\t')[2]);
                    building_height[i, j] = Double.Parse(sr2.ReadLine().Split('\t')[2]);
                }
            }
            sr1.Close();
            sr2.Close();
        }

        private void WriteHeight()
        {
            double[,] le = new double[20000, 20000];
            double[,] bh = new double[20000, 20000];

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
                            bh[i, j] = Double.Parse(data[4]);
                            le[i, j] = Double.Parse(data[5]);

                            int m = i / Utils.ROW_NUM;
                            int n = j / Utils.COL_NUM;
                            if (le[i, j] > land_elevation[m, n])
                            {
                                land_elevation[m, n] = le[i, j];
                            }
                            if (bh[i, j] > building_height[m, n])
                            {
                                building_height[m, n] = bh[i, j];
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
