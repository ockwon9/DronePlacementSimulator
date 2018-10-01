using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Windows.Shapes;
using CSharpIDW;

namespace DronePlacementSimulator
{
    public static class Rubis
    {
        public static void doCalculate(List<OHCAEvent> eventList, List<List<double[]>> polyCoordList, ref List<Station> stationList)
        {
            //Grid gridEvent = new Grid(0.0, 0.0, Utils.SEOUL_WIDTH, Utils.SEOUL_HEIGHT, Utils.UNIT, ref polyCoordList);
            //gridEvent.IdwInterpolate(ref eventList);

            // Find intial placement of stations
            Grid gridStation = new Grid(0.0, 0.0, Utils.SEOUL_WIDTH, Utils.SEOUL_HEIGHT, 9, ref polyCoordList);
            foreach (double[] cell in gridStation.cells)
            {
                double kiloX = cell[0] + 0.5 * 9;
                double kiloY = cell[1] + 0.5 * 9;
                Station s = new Station(kiloX, kiloY);
                s.pixelX = Utils.transformKiloXToPixel(kiloX);
                s.pixelY = Utils.transformKiloYToPixel(kiloY);
                for (int i = 0; i < 1; i++)
                {
                    Drone drone = new Drone(s.stationID);
                    s.droneList.Add(drone);
                }
                stationList.Add(s);
            }

            int n = stationList.Count;
            int[] numDronesAtStation = new int[n];
            for (int i = 0; i < n; i++)
            {
                numDronesAtStation[i] = 0;
            }

            Counter counter = new Counter(stationList.Count, ref numDronesAtStation);
        }

        public static double SurvivalRate(Station s, OHCAEvent ohca)
        {
            return 1.0f;
        }

        public static double PotentialNegativePart(Station s, OHCAEvent ohca)
        {
            return 0.0f;
        }
    }
}
