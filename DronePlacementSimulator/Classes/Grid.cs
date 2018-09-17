using System.Collections.Generic;
using CSharpIDW;

namespace DronePlacementSimulator
{
    class Grid
    {
        public double minLat, minLon;
        public double unitLat, unitLon;
        public int numLat, numLon;
        public double[,] pdf;
        public IdwInterpolator idw;
        
        public Grid (double minLat, double minLon, double maxLat, double maxLon, int numLat, int numLon, ref List<OHCAEvent> eventList)
        {
            this.numLat = numLat;
            this.numLon = numLon;
            this.unitLat = (maxLat - minLat) / numLat;
            this.unitLon = (maxLon - minLon) / numLon;
            this.minLat = minLat + this.unitLat / 2;
            this.minLon = minLon + this.unitLon / 2;
            this.pdf = new double[numLat, numLon];
            this.idw = new IdwInterpolator(2);

            IdwInterpolate(ref eventList);
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

            double[] tempCoord = new double[2];
            tempCoord[0] = this.minLat;
            for (int i = 0; i < this.numLat; i++, tempCoord[0] += this.unitLat)
            {
                tempCoord[1] = this.minLon;

                for (int j = 0; j < this.numLon; j++, tempCoord[1] += this.unitLon)
                {
                    this.pdf[i, j] = idw.Interpolate(tempCoord).Value;
                }
            }

            return;
        }

        public double getMaxDemand()
        {
            double mD = 0;
            for (int i = 0; i < numLat; i++)
            {
                for (int j = 0; j < numLon; j++)
                {
                    if (pdf[i, j] > mD)
                    {
                        mD = pdf[i, j];
                    }
                }
            }

            return mD;
        }

        public double getLat(int i)
        {
            return this.minLat + i * this.unitLat;
        }

        public double getLon(int i)
        {
            return this.minLon + i * this.unitLon;
        }

        public double getLatMin(int i)
        {
            return this.minLat + (i - 0.5) * this.unitLat;
        }

        public double getLatMax(int i)
        {
            return this.minLat + (i + 0.5) * this.unitLat;
        }

        public double getLonMin(int i)
        {
            return this.minLon + (i - 0.5) * this.unitLat;
        }

        public double getLonMax(int i)
        {
            return this.minLon + (i + 0.5) * this.unitLat;
        }
    }
}
