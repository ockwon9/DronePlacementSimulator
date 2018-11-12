using System;

namespace DronePlacementSimulator
{
    public class OHCAEvent 
    {
        private static int ID = 10000;
        
        public int eventID;
        public double kiloX, kiloY;
        public int pixelX, pixelY;
        public DateTime occurrenceTime;
        public int assignedStationId;
        
        public OHCAEvent(double kiloX, double kiloY, DateTime occurrenceTime)
        {
            this.kiloX = kiloX;
            this.kiloY = kiloY;
            this.pixelX = Utils.TransformKiloXToPixel(kiloX);
            this.pixelY = Utils.TransformKiloYToPixel(kiloY);
            this.occurrenceTime = occurrenceTime;
            this.eventID = ID++;
        }

        public OHCAEvent(OHCAEvent e)
        {
            this.kiloX = e.kiloX;
            this.kiloY = e.kiloY;
            this.pixelX = e.pixelX;
            this.pixelY = e.pixelY;
            this.occurrenceTime = e.occurrenceTime;
            this.eventID = e.eventID;
        }

        public void SetLocation(double kiloX, double kiloY)
        {
            this.kiloX = kiloX;
            this.kiloY = kiloY;
            this.pixelX = Utils.TransformKiloXToPixel(kiloX);
            this.pixelY = Utils.TransformKiloYToPixel(kiloY);
        }
    }
}
