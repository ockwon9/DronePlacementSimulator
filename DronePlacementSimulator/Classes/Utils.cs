using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DronePlacementSimulator
{
    public class Utils
    {
        public static double MIN_LONGITUDE = 126.7645806f;
        public static double MAX_LONGITUDE = 127.1831312f;
        public static double MIN_LATITUDE = 37.42834757f;
        public static double MAX_LATITUDE = 37.70130154f;
        public static double SEOUL_WIDTH = 36.89;
        public static double SEOUL_HEIGHT = 30.35;

        public static double UNIT = 3.0;
        public static double GOLDEN_TIME = 6.36;

        public static int SCREEN_WIDTH;
        public static int SCREEN_HEIGHT;

        static Utils()
        {
            SCREEN_HEIGHT = Screen.PrimaryScreen.Bounds.Height;
            SCREEN_WIDTH = (int)(SCREEN_HEIGHT * SEOUL_WIDTH / SEOUL_HEIGHT);
        }

        public static int transformKiloXToPixel(double kiloX)
        {           
            double ratio = kiloX / SEOUL_WIDTH;
            return (int)(SCREEN_WIDTH * ratio);
        }

        public static int transformKiloYToPixel(double kiloY)
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

        public static double getDistance(double x1, double y1, double x2, double y2)
        {
            return Math.Sqrt((x1 - x2) * (x1 - x2) + (y1 - y2) * (y1 - y2));
        }
    }
}
