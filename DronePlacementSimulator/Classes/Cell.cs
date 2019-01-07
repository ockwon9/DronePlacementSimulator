using System.Device.Location;

namespace DronePlacementSimulator
{
    class Cell
    {
        public GeoCoordinate geo;
        public int intX;
        public int intY;
        public int eventCount;

        public Cell()
        {
            this.geo = new GeoCoordinate(0, 0);
            this.intX = 0;
            this.intY = 0;
            this.eventCount = 0;
        }

        public Cell(GeoCoordinate geo, int j, int i)
        {
            this.geo = geo;
            this.intX = j;
            this.intY = i;
            this.eventCount = 0;
        }

        public void addEvent()
        {
            this.eventCount++;
        }
    }
}
