using System;

namespace DronePlacementSimulator
{
    public class OHCAEvent 
    {
        private static int ID = 10000;
        
        public int eventID;
        public double lat, lon;
        public int pixelRow, pixelCol;
        public DateTime occurrenceTime;
        public int assignedStationId;
        
        public OHCAEvent(double lat, double lon, DateTime occurrenceTime)
        {
            this.lat = lat;
            this.lon = lon;
            this.pixelRow = Utils.TransformLatToPixel(lat);
            this.pixelCol = Utils.TransformLonToPixel(lon);
            this.occurrenceTime = occurrenceTime;
            this.eventID = ID++;
        }

        public OHCAEvent(OHCAEvent e)
        {
            this.lat = e.lat;
            this.lon = e.lon;
            this.pixelRow = e.pixelRow;
            this.pixelCol = e.pixelCol;
            this.occurrenceTime = e.occurrenceTime;
            this.eventID = e.eventID;
        }

        public void SetLocation(double lat, double lon)
        {
            this.lat = lat;
            this.lon = lon;
            this.pixelRow = Utils.TransformLatToPixel(lat);
            this.pixelCol = Utils.TransformLonToPixel(lon);
        }
    }
}
