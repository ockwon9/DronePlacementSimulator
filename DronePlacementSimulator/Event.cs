using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DronePlacementSimulator
{
    class Event
    {
        public DateTime occurrenceTime;
        public DateTime arrivalTIme;
        public float longitude;
        public float latitude;

        public Event(DateTime occurrenceTime, DateTime arrivalTIme, float latitude, float longitude)
        {
            this.occurrenceTime = occurrenceTime;
            this.arrivalTIme = arrivalTIme;
            this.latitude = latitude;
            this.longitude = longitude;
        }

        public Event(float latitude, float longitude)
        {
            this.latitude = latitude;
            this.longitude = longitude;
        }
    }
}
