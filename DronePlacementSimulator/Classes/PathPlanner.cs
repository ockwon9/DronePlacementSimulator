using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MySql.Data;
using MySql.Data.MySqlClient;

namespace DronePlacementSimulator
{
    class PathPlanner
    {
        private double DIST_LAT = Utils.MAX_LATITUDE - Utils.MIN_LATITUDE;
        private double DIST_LNG = Utils.MAX_LONGITUDE - Utils.MIN_LONGITUDE;
        private MySqlConnection conn;

        public PathPlanner()
        {
            string connStr = "server=rubis.snu.ac.kr;user=ockwon;database=ockwon_drone;port=3306;password=koc383838";
            conn = new MySqlConnection(connStr);
            try
            {                
                conn.Open();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            conn.Close();
        }

        public double CalcuteFlightTime(double srcX, double srcY, double dstX, double dstY)
        {
            double distance = GetDistance(srcX, srcY, dstX, dstY);

            return distance;
            /*
            int srcCol = ConvertKiloToCol(srcX);
            int srcRow = ConvertKiloToRow(srcY);
            double srcHeight = getLandElevation(srcCol, srcRow);

            int dstCol = ConvertKiloToCol(dstX);
            int dstRow = ConvertKiloToRow(dstY);
            double dstHeight = getLandElevation(dstCol, dstRow);

            double maxHeightOnRoute = getMaxHeight(srcX, srcY, dstX, dstY);
            double maxHeight = Math.Max(srcHeight, Math.Max(dstHeight, maxHeightOnRoute));
                
            double takeOffHeight = (srcHeight > maxHeight) ? Utils.FLIGHT_HEIGHT : maxHeight - srcHeight + Utils.FLIGHT_HEIGHT;
            double landdingHeight = (dstHeight > maxHeight) ? Utils.FLIGHT_HEIGHT : maxHeight - dstHeight + Utils.FLIGHT_HEIGHT;
        
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

            double maxHeight = 0.0;
            for (int i = 0; i <= diffCol; i++)
            {
                int midCol = srcCol + i; // The mid index between srcCol and dstCol
                int midRow = srcRow + (int)(diffRow * i / diffCol); // The mid index between srcRow and dstRow
                double height = getHeight(midCol, midRow);
                if (height > maxHeight)
                {
                    maxHeight = height;
                }
            }

            return maxHeight;
        }

        private int ConvertKiloToCol(double kiloX)
        {
            return (int)(20000 * kiloX / Utils.SEOUL_WIDTH);
        }

        private int ConvertKiloToRow(double kiloY)
        {
            return (int)(20000 * (Utils.SEOUL_HEIGHT - kiloY) / Utils.SEOUL_HEIGHT);
        }
        
        private double getHeight(int col, int row)
        {
            double land_elevation = 0.0;
            double building_height = 0.0;
            string sql = "SELECT land_elevation, building_height FROM db_seoul WHERE row='" + row + "' and col='" + col + "'";

            try
            {
                MySqlCommand cmd = new MySqlCommand(sql, conn);
                MySqlDataReader rdr = cmd.ExecuteReader();
                land_elevation = rdr.GetDouble(0);
                building_height = rdr.GetDouble(1);
                rdr.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            return land_elevation + building_height;
        }

        private double getLandElevation(int col, int row)
        {
            double land_elevation = 0.0;
            string sql = "SELECT land_elevation FROM db_seoul WHERE row='" + row + "' and col='" + col + "'";

            try
            {
                MySqlCommand cmd = new MySqlCommand(sql, conn);
                MySqlDataReader rdr = cmd.ExecuteReader();
                land_elevation = rdr.GetDouble(0);
                rdr.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            return land_elevation;
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
