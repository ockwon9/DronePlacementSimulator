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
            land_elevation = new double[20000, 20000];
            building_height = new double[20000, 20000];

            StreamReader objReader = new StreamReader("seoul.txt");
            string line = "";
            line = objReader.ReadLine();
            line = objReader.ReadLine();

            for (int i = 0; i < 20000; i++)
            {
                for (int j = 0; j < 20000; j++)
                {
                    building_height[i, j] = 10.0;
                    land_elevation[i, j] = 10.0;
                    /*
                    try
                    {
                        line = objReader.ReadLine();
                        if (line != null)
                        {
                            string[] data = line.Split(' ');
                            building_height[i, j] = Double.Parse(data[4]);
                            land_elevation[i, j] = Double.Parse(data[5]);
                        }
                    }
                    catch (Exception)
                    {
                        return;
                    }
                    */
                }
            }
            objReader.Close();
        }

        public double CalcuteFlightTime(double srcX, double srcY, double dstX, double dstY)
        {
            double distance = GetDistance(srcX, srcY, dstX, dstY);
            return distance;
            /*
            int srcCol = ConvertKiloToCol(srcX);
            int srcRow = ConvertKiloToRow(srcY);
            double srcHeight = land_elevation[srcRow, srcCol];

            int dstCol = ConvertKiloToCol(dstX);
            int dstRow = ConvertKiloToRow(dstY);
            double dstHeight = land_elevation[dstRow, dstCol];

            double maxHeightOnRoute = getMaxHeight(srcX, srcY, dstX, dstY);
            double takeOffHeight = maxHeightOnRoute - srcHeight + Utils.FLIGHT_HEIGHT;
            double landdingHeight = maxHeightOnRoute - dstHeight + Utils.FLIGHT_HEIGHT;
            
            return (takeOffHeight / Utils.DRONE_TAKE_OFF_VELOCITY / 60) + distance + (landdingHeight / Utils.DRONE_LANDING_VELOCITY / 60);
            */
        }

        private double getMaxHeight(double srcX, double srcY, double dstX, double dstY)
        {
            if(srcX > dstX)
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

                    if (midRow > 20000)
                        Console.WriteLine("!!!");

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
            return (int)(20000 * kiloX / Utils.SEOUL_WIDTH);
        }

        private int ConvertKiloToRow(double kiloY)
        {
            return (int)(20000 * (Utils.SEOUL_HEIGHT - kiloY) / Utils.SEOUL_HEIGHT);
        }
        
        private double GetDistance(double x1, double y1, double x2, double y2)
        {
            return Math.Sqrt((x1 - x2) * (x1 - x2) + (y1 - y2) * (y1 - y2));
        }

        private static void Swap<T>(ref T lhs, ref T rhs)
        {
            T temp = lhs;
            lhs = rhs;
            rhs = temp;
        }
    }
}
