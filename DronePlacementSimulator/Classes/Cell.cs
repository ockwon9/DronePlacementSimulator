namespace DronePlacementSimulator
{
    class Cell
    {
        public double kiloX;
        public double kiloY;
        public int intX;
        public int intY;
        public int eventCount;

        public Cell()
        {
            this.kiloX = 0.0;
            this.kiloY = 0.0;
            this.intX = 0;
            this.intY = 0;
            this.eventCount = 0;
        }

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
