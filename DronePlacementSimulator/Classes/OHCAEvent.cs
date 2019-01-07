using System;
using System.Device.Location;

namespace DronePlacementSimulator
{
    public class OHCAEvent 
    {
        private static int ID = 10000;
        
        public int eventID;
        public GeoCoordinate geo;
        public int pixelX, pixelY;
        public DateTime occurrenceTime;
        public int assignedStationId;
        
        public OHCAEvent(GeoCoordinate geo, DateTime occurrenceTime)
        {
            this.geo = geo;
            this.pixelX = Utils.TransformLonToPixel(geo.Longitude);
            this.pixelY = Utils.TransformLatToPixel(geo.Latitude);
            this.occurrenceTime = occurrenceTime;
            this.eventID = ID++;
        }

        public OHCAEvent(OHCAEvent e)
        {
            this.geo = e.geo;
            this.pixelX = e.pixelX;
            this.pixelY = e.pixelY;
            this.occurrenceTime = e.occurrenceTime;
            this.eventID = e.eventID;
        }

        public void SetLocation(GeoCoordinate geo)
        {
            this.geo = geo;
            this.pixelX = Utils.TransformLonToPixel(geo.Longitude);
            this.pixelY = Utils.TransformLatToPixel(geo.Latitude);
        }
    }
}
