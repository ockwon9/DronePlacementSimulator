namespace DronePlacementSimulator
{
    class DispatchFailure
    {
        public double lat, lon;
        public Utils.Failure failure;

        public DispatchFailure(double lat, double lon, Utils.Failure failure)
        {
            this.lat = lat;
            this.lon = lon;
            this.failure = failure;
        }
    }
}
