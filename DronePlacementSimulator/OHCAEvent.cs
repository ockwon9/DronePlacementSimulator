using System;

namespace DronePlacementSimulator
{
    public class OHCAEvent
    {
        private static int ID = 10000;
        
        public int          eventID;
        public double       latitude, longitude;
        public int          x, y;
        //public DateTime     occurrenceTime;
        //public DateTime     arrivalTIme;

        public OHCAEvent()
        {
            this.eventID = ID++;
        }
    }
}
