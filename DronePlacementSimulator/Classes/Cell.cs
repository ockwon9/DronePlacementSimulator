namespace DronePlacementSimulator
{
    class Cell
    {
        public double lat, lon;
        public int row, col;
        public int eventCount;

        public Cell()
        {
            this.lat = 0.0;
            this.lon = 0.0;
            this.row = 0;
            this.col = 0;
            this.eventCount = 0;
        }

        public Cell(double lat, double lon, int i, int j)
        {
            this.lat = lat;
            this.lon = lon;
            this.row = i;
            this.col = j;
            this.eventCount = 0;
        }

        public Cell(int row, int col)
        {
            this.lat = Utils.ConvertRowToLat(row);
            this.lon = Utils.ConvertColToLon(col);
            this.row = row;
            this.col = col;
            this.eventCount = 0;
        }

        public void addEvent()
        {
            this.eventCount++;
        }
    }
}
