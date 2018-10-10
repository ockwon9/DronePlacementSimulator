using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DronePlacementSimulator
{
    public static class Utils
    {
        public static double MIN_LONGITUDE = 126.7645806;
        public static double MAX_LONGITUDE = 127.1831312;
        public static double MIN_LATITUDE = 37.42834757;
        public static double MAX_LATITUDE = 37.70130154;
        public static double SEOUL_WIDTH = 36.89;
        public static double SEOUL_HEIGHT = 30.35;
        public static int MINUTES_IN_4_YEARS = 2103840;

        public static double UNIT = 3.0;
        public static double GOLDEN_TIME = 4.949;

        public static int SCREEN_WIDTH;
        public static int SCREEN_HEIGHT;

        public static int BUDGET = 600000;
        public static int STATION_PRICE = 10000;
        public static int DRONE_PRICE = 10000;

        public static int ITERATION_COUNT = 100;
        public static int NUMBER_OF_NEIGHBORS = 1000;

        public static double ARRIVAL_RATE = 0.0079858844405054936;
        public static int SIMULATION_EVENTS = 10000000;

        static Utils()
        {
            SCREEN_HEIGHT = Screen.PrimaryScreen.Bounds.Height;
            SCREEN_WIDTH = (int)(SCREEN_HEIGHT * SEOUL_WIDTH / SEOUL_HEIGHT);
        }

        public static int TransformKiloXToPixel(double kiloX)
        {           
            double ratio = kiloX / SEOUL_WIDTH;
            return (int)(SCREEN_WIDTH * ratio);
        }

        public static int TransformKiloYToPixel(double kiloY)
        {
            double ratio = kiloY / SEOUL_HEIGHT;
            return SCREEN_HEIGHT - (int)(SCREEN_HEIGHT * ratio);
        }

        public static double LonToKilos(double longitude)
        {
            return (longitude - MIN_LONGITUDE) / 0.4185506 * SEOUL_WIDTH;
        }

        public static double LatToKilos(double latitude)
        {
            return (latitude - MIN_LATITUDE) / 0.27295397 * SEOUL_HEIGHT;
        }

        public static double GetDistance(double x1, double y1, double x2, double y2)
        {
            return Math.Sqrt((x1 - x2) * (x1 - x2) + (y1 - y2) * (y1 - y2));
        }
    }
}
