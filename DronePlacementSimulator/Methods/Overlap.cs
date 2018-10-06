using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

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
        public double Area(double x, double y, double width, double height, double circleX, double circleY, double r)
        {
            // Console.WriteLine("x = " + x + ", y = " + y + ", width = " + width + ", height = " + height + ", cX = " + circleX + ", cY = " + circleY + ", r = " + r);
            double a = IntersectionArea(x, y, width, height, circleX, circleY, r);
            // Console.WriteLine("a = " + a);
            return a;
        }

        /// <summary>
        /// The resolution to use for approximation.
        /// </summary>
        const double Resolution = 0.0001;

        /// <summary>
        /// Calculates the intersection area between a rectangle and a circle
        /// </summary>
        /// <param name="rect">The rectangle</param>
        /// <param name="m">The center of the circle</param>
        /// <param name="r">The radius</param>
        /// <returns>The intersection area of the two shapes</returns>
        double IntersectionArea(double x, double y, double width, double height, double circleX, double circleY, double r)
        {
            double a = 0; //Area

            //Check whether the rectangle lies completely outside of the circle. 
            //Note: It is easier to check if a rectangle is outside another rectangle or
            //circle than to check whether it is inside.
            if ((x > circleX && y > circleY) && (Utils.GetDistance(x, y, circleX, circleY) > r) ||
                (x > circleX && y + height < circleY)  && (Utils.GetDistance(x, y + height, circleX, circleY) > r) ||
                (x + width < circleX && y > circleY) && (Utils.GetDistance(x + width, y, circleX, circleY) > r) ||
                (x + width < circleX && y + height < circleY) && (Utils.GetDistance(x + width, y + height, circleX, circleY) > r))
            {
                return 0; //Terminate fast
            }

            //A variable storing the nearest horizontal edge of the rectangle. 
            double nearestRectangleEdge = 0;

            //Determine what is nearer to the circle center - the rectangle top edge or the rectangle bottom edge
            if (Math.Abs(circleY - y) > Math.Abs(circleY - y - height))
            {
                nearestRectangleEdge = y + height;
            }
            else
            {
                nearestRectangleEdge = y; 
            }

            //The bounds of our integration
            double leftBound = 0;
            double rightBound = 0;

            if (circleY >= y && circleY <= y + height)
            {
                //Take care if the circle's center lies within the rectangle. 
                leftBound = Math.Max(-r + circleX, x);
                rightBound = Math.Min(r + circleX, x + width);
            }
            else if (r >= Math.Abs(nearestRectangleEdge - circleY))
            {
                //If the circle's center lies outside of the rectangle, we can choose optimal bounds.
                leftBound = Math.Max(-Math.Sqrt(r * r - Math.Abs(Math.Pow(nearestRectangleEdge - circleY, 2))) + circleX, x);
                rightBound = Math.Min(Math.Sqrt(r * r - Math.Abs(Math.Pow(nearestRectangleEdge - circleY, 2))) + circleX, x + width);
            }

            double upperBound;
            double lowerBound;

            //Loop trough the intersection area and sum up the area
            for (double i = leftBound + Resolution; i <= rightBound; i += Resolution)
            {
                upperBound = Math.Min(y + height, UpperCircleFunction(circleX, circleY, r, i - Resolution / 2));
                lowerBound = Math.Max(y, LowerCircleFunction(circleX, circleY, r, i - Resolution / 2));

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
