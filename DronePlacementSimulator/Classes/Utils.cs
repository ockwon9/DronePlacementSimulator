using System;
using System.Windows.Forms;
using System.Device.Location;

namespace DronePlacementSimulator
{
    public static class Utils
    {
        public static int SCREEN_WIDTH;
        public static int SCREEN_HEIGHT;

        public static double MIN_LATITUDE = 37.42834757;
        public static double MAX_LATITUDE = 37.70130154;
        public static double MIN_LONGITUDE = 126.7645806;
        public static double MAX_LONGITUDE = 127.1831312;
        public static double RANGE_LATITUDE = MAX_LATITUDE - MIN_LATITUDE;
        public static double RANGE_LONGITUDE = MAX_LONGITUDE - MIN_LONGITUDE;
        public static double SEOUL_WIDTH = 36.89;
        public static double SEOUL_HEIGHT = 30.35;

        public static double UNIT = 0.1;
        public static int ROW_NUM = (int)Math.Ceiling(SEOUL_HEIGHT / UNIT);
        public static int COL_NUM = (int)Math.Ceiling(SEOUL_WIDTH / UNIT);

        public static double LAT_UNIT = RANGE_LATITUDE / ROW_NUM;
        public static double LON_UNIT = RANGE_LONGITUDE / COL_NUM;
        
        public static double GOLDEN_TIME = 5;
        public static double SURVIVAL_RATE_SLOPE = 0.2;

        public static int KMEANS_ITERATION_COUNT = 100;

        public static int SIMULATION_EVENTS = 10000; //00;
        public static double ARRIVAL_RATE = 0.0079858844405054936;
        public static int MINUTES_IN_4_YEARS = 2103840;

        public static double DRONE_VELOCITY = 1;
        public static double BASE_FLIGHT_HEIGHT = 0.01;
        public static double DRONE_TAKE_OFF_VELOCITY = 0.24;
        public static double DRONE_LANDING_VELOCITY = 0.12;
        public static int DRONE_REST_TIME = 6;

        public static int STATION_PRICE = 8000;
        public static int DRONE_PRICE = 1000;

        public enum Failure { NO_DRONES, UNREACHABLE };

        static Utils()
        {
            SCREEN_HEIGHT = Screen.PrimaryScreen.Bounds.Height;
            SCREEN_WIDTH = (int)(SCREEN_HEIGHT * SEOUL_WIDTH / SEOUL_HEIGHT);
        }

        public static int TransformLatToPixel(double lat)
        {
            double ratio = (lat - MIN_LATITUDE) / RANGE_LATITUDE;
            return SCREEN_HEIGHT - (int)(SCREEN_HEIGHT * ratio);
        }

        public static int TransformLonToPixel(double lon)
        {           
            double ratio = (lon - MIN_LONGITUDE) / RANGE_LONGITUDE;
            return (int)(SCREEN_WIDTH * ratio);
        }

        public static int ConvertLatToRow(double lat)
        {
            return (int)Math.Round((lat - MIN_LATITUDE) / LAT_UNIT - 0.5);
        }

        public static int ConvertLonToCol(double lon)
        {
            return (int)Math.Round((lon - MIN_LONGITUDE) / LON_UNIT - 0.5);
        }

        public static double ConvertRowToLat(int row)
        {
            return MIN_LATITUDE + (row + 0.5) * LAT_UNIT;
        }

        public static double ConvertColToLon(int col)
        {
            return MIN_LONGITUDE + (col + 0.5) * LON_UNIT;
        }

        public static double ConvertRowToLatFloor(int row)
        {
            return MIN_LATITUDE + row * LAT_UNIT;
        }

        public static double ConvertColToLonFloor(int col)
        {
            return MIN_LONGITUDE + col * LON_UNIT;
        }

        public static double GetDistance(double lat1, double lon1, double lat2, double lon2)
        {
            return (new GeoCoordinate(lat1, lon1).GetDistanceTo(new GeoCoordinate(lat2, lon2)) / 1000);
        }
    }
}
