using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Device.Location;

namespace DronePlacementSimulator
{
    public static class Utils
    {
        public static int SCREEN_WIDTH;
        public static int SCREEN_HEIGHT;

        public static double MIN_LONGITUDE = 126.7645806;
        public static double MAX_LONGITUDE = 127.1831312;
        public static double MIN_LATITUDE = 37.42834757;
        public static double MAX_LATITUDE = 37.70130154;
        public static double RANGE_LONGTITUDE = MAX_LONGITUDE - MIN_LONGITUDE;
        public static double RANGE_LATITUDE = MAX_LATITUDE - MIN_LATITUDE;
        
        public static int ROW_NUM = 305;
        public static int COL_NUM = 370;

        public static double UNIT = 0.5;
        public static double LAMBDA_PRECISION = 0.1;
        public static double GOLDEN_TIME = 5;
        public static double SURVIVAL_RATE_SLOPE = 0.2;

        public static int KMEANS_ITERATION_COUNT = 100;

        public static int SIMULATION_EVENTS = 1000000;
        public static double ARRIVAL_RATE = 0.0079858844405054936;
        public static int MINUTES_IN_4_YEARS = 2103840;

        public static double DRONE_VELOCITY = 1.5;
        public static double BASE_FLIGHT_HEIGHT = 10.0; // meter
        public static double DRONE_TAKE_OFF_VELOCITY = 2.0; // m/s
        public static double DRONE_LANDING_VELOCITY = 1.0; // m/s
        public static int DRONE_REST_TIME = 6;

        public static int STATION_PRICE = 8000;
        public static int DRONE_PRICE = 1000;

        static Utils()
        {
            SCREEN_HEIGHT = Screen.PrimaryScreen.Bounds.Height;
            SCREEN_WIDTH = (int)(SCREEN_HEIGHT * RANGE_LONGTITUDE / RANGE_LATITUDE);
        }

        public static int TransformLonToPixel(double lon)
        {           
            double ratio = (lon - MIN_LONGITUDE) / RANGE_LONGTITUDE;
            return (int)(SCREEN_WIDTH * ratio);
        }

        public static int TransformLatToPixel(double lat)
        {
            double ratio = (lat - MIN_LATITUDE) / RANGE_LATITUDE;
            return SCREEN_HEIGHT - (int)(SCREEN_HEIGHT * ratio);
        }
    }
}
