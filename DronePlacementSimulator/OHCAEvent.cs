using System;

namespace DronePlacementSimulator
{
    class OHCAEvent
    {
        private static int ID = 10000;

        public int          eventID;

        public DateTime     occurrenceTime;
        public DateTime     arrivalTIme;

        public float        latitude, longitude;
        public int          x, y;

        public OHCAEvent()
        {
            this.eventID = ID++;
        }
    }
}
