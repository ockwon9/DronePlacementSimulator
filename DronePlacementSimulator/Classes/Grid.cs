using System.Collections.Generic;
using CSharpIDW;
using System;

namespace DronePlacementSimulator
{
    class Grid
    {
        public int numCells;
        public List<double[]> cells;
        public double unit;
        public double[] pdf;
        public IdwInterpolator idw;
        
        public Grid (double minLat, double minLon, double maxLat, double maxLon, int unit, ref List<OHCAEvent> eventList, ref System.Windows.Media.PointCollection pc)
        {
            this.numCells = 0;
            this.cells = new List<double[]>();
            this.unit = unit;
            this.idw = new IdwInterpolator(2);
            int numLat = (int) Math.Ceiling((maxLat - minLat) / unit);
            int numLon = (int)Math.Ceiling((maxLon - minLon) / unit);

            for (int i = 0; i < numLat; i++)
            {
                for (int j = 0; j < numLon; j++)
                {
                    double lat = minLat + i * unit;
                    double lon = minLon + j * unit;

                    if (intersects(lat, lon, pc))
                    {
                        numCells++;
                        double[] coord = new double[2];
                        coord[0] = lat;
                        coord[1] = lon;
                        cells.Add(coord);
                    }
                }
            }

            this.pdf = new double[numCells];

            IdwInterpolate(ref eventList);
        }

        public Boolean intersects(double lat, double lon, System.Windows.Media.PointCollection pc)
        {
            return true;
        }

        public void IdwInterpolate(ref List<OHCAEvent> eventList)
        {
            Dictionary<double[], int> eventDict = new Dictionary<double[], int>();

            foreach (var ohca in eventList)
            {
                double[] ohcaGps = new double[2] { ohca.latitude, ohca.longitude };
                if (eventDict.ContainsKey(ohcaGps))
                {
                    eventDict[ohcaGps]++;
                }
                else
                {
                    eventDict.Add(ohcaGps, 1);
                }
            }

            foreach (KeyValuePair<double[], int> entry in eventDict)
            {
                idw.AddPoint((double) entry.Value, entry.Key);
            }

            int i = 0;
            foreach (double[] temp in cells)
            {
                this.pdf[i] = idw.Interpolate(temp).Value;
                i++;
            }

            return;
        }

        public double getMaxDemand()
        {
            double mD = 0;
            for (int i = 0; i < numCells; i++)
            {
                if (pdf[i] > mD)
                {
                    mD = pdf[i];
                }
            }

            return mD;
        }
    }
}
