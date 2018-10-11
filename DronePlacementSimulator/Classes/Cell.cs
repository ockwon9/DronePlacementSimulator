namespace DronePlacementSimulator
{
    class Cell
    {
        public double kiloX;
        public double kiloY;
        public int intX;
        public int intY;
        public int eventCount;

        public Cell(double kiloX, double kiloY, int j, int i)
        {
            this.kiloX = kiloX;
            this.kiloY = kiloY;
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
