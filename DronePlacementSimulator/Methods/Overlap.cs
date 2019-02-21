using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Device;
using System.Device.Location;

// Author: Emanuel Jöbstl <emi@eex-dev.net>
// Date  : 18.06.2011
// Link  : http://www.eex-dev.net/index.php?id=100
// 
// Sample of the use of computer integration for the calculation of intersection areas. 

namespace DronePlacementSimulator
{
    class Overlap
    {
        /// <summary>
        /// Executes some test cases
        /// </summary>
        /// <param name="args"></param>
        
        /// <summary>
        /// Executes a test case and outputs the results on the console window
        /// </summary>
        /// <param name="x">The rectangles X coordinate</param>
        /// <param name="y">The rectangles Y coordinate</param>
        /// <param name="width">The rectangles width</param>
        /// <param name="height">The rectangles height</param>
        /// <param name="circleX">The X coordinate of the circles center</param>
        /// <param name="circleY">The Y coordinate of the circles center</param>
        /// <param name="r">The radius of the circle</param>
        public double Area(double lat, double lon, double latDiff, double lonDiff, double circleLat, double circleLon, double r)
        {
            GeoCoordinate start = new GeoCoordinate(lat, lon);
            double height = start.GetDistanceTo(new GeoCoordinate(lat + latDiff, lon)) / 1000;
            double width = start.GetDistanceTo(new GeoCoordinate(lat, lon + lonDiff)) / 1000;

            GeoCoordinate circle = new GeoCoordinate(circleLat, circleLon);

            double circleHeight = circle.GetDistanceTo(new GeoCoordinate(lat, circleLon)) / 1000;
            if (circleLat < lat)
            {
                circleHeight = -circleHeight;
            }

            double circleWidth = circle.GetDistanceTo(new GeoCoordinate(circleLat, lon)) / 1000;
            if (circleLon < lon)
            {
                circleWidth = -circleWidth;
            }

            double a = IntersectionArea(height, width, circleHeight, circleWidth, r);
            return a;
        }

        /// <summary>
        /// The resolution to use for approximation.
        /// </summary>
        const double Resolution = 0.00001;

        /// <summary>
        /// Calculates the intersection area between a rectangle and a circle
        /// </summary>
        /// <param name="rect">The rectangle</param>
        /// <param name="m">The center of the circle</param>
        /// <param name="r">The radius</param>
        /// <returns>The intersection area of the two shapes</returns>
        
        bool inCircle(double x, double y, double r)
        {
            return x * x + y * y < r * r;
        }

        double IntersectionArea(double height, double width, double circleHeight, double circleWidth, double r)
        {
            double a = 0; //Area

            //Check whether the rectangle lies completely outside of the circle. 
            //Note: It is easier to check if a rectangle is outside another rectangle or
            //circle than to check whether it is inside.
            if ((circleWidth < 0 && circleHeight < 0) && !inCircle(circleHeight, circleWidth, r) ||
                (circleWidth < 0 && circleHeight > height) && !inCircle(circleHeight - height, circleWidth, r) ||
                (circleWidth > width && circleHeight < 0) && !inCircle(circleHeight, circleWidth - width, r) ||
                (circleWidth > width && circleHeight > height) && !inCircle(circleHeight - height, circleWidth - width, r))
            {
                return 0; //Terminate fast
            }

            //A variable storing the nearest horizontal edge of the rectangle. 
            double nearestRectangleEdge = 0;

            //Determine what is nearer to the circle center - the rectangle top edge or the rectangle bottom edge
            if (Math.Abs(circleHeight) > Math.Abs(circleHeight - height))
            {
                nearestRectangleEdge = height;
            }

            //The bounds of our integration
            double leftBound = 0;
            double rightBound = 0;

            if (circleHeight >= 0 && circleHeight <= height)
            {
                //Take care if the circle's center lies within the rectangle. 
                leftBound = Math.Max(-r + circleWidth, 0);
                rightBound = Math.Min(r + circleWidth, width);
            }
            else if (r >= Math.Abs(nearestRectangleEdge - circleHeight))
            {
                //If the circle's center lies outside of the rectangle, we can choose optimal bounds.
                leftBound = Math.Max(-Math.Sqrt(r * r - Math.Abs(Math.Pow(nearestRectangleEdge - circleHeight, 2))) + circleWidth, 0);
                rightBound = Math.Min(Math.Sqrt(r * r - Math.Abs(Math.Pow(nearestRectangleEdge - circleHeight, 2))) + circleWidth, width);
            }

            double upperBound;
            double lowerBound;

            //Loop trough the intersection area and sum up the area
            for (double i = leftBound + Resolution; i <= rightBound; i += Resolution)
            {
                upperBound = Math.Min(height, UpperCircleFunction(circleWidth, circleHeight, r, i - Resolution / 2));
                lowerBound = Math.Max(0, LowerCircleFunction(circleWidth, circleHeight, r, i - Resolution / 2));

                a += (upperBound - lowerBound) * Resolution;
            }
            
            return (double)Math.Round((decimal)a, 4);
        }

        /// <summary>
        /// Function which defines the upper circle curve
        /// </summary>
        static double UpperCircleFunction(double circleX, double circleY, double r, double x)
        {
            if (circleX - r > x || circleX + r < x)
            {
                throw new InvalidOperationException("The requested point lies outside of the circle");
            }
            return circleY + Math.Sqrt((r * r) - Math.Pow((x - circleX), 2));
         }

        /// <summary>
        /// Function which defines the lower circle curve
        /// </summary>
        static double LowerCircleFunction(double circleX, double circleY, double r, double x)
        {
            if (circleX - r > x || circleX + r < x)
            {
                throw new InvalidOperationException("The requested point lies outside of the circle");
            }
            return circleY - Math.Sqrt((r * r) - Math.Pow((x - circleX), 2));
        }
    }
}
