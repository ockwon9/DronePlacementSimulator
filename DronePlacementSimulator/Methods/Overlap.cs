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
            return IntersectionArea(new RectangleF((float)x, (float)y, (float)width, (float)height), new PointF((float)circleX, (float)circleY), (float)r);
        }

        /// <summary>
        /// The resolution to use for approximation.
        /// </summary>
        const decimal Resolution = 0.01M;

        /// <summary>
        /// Calculates the intersection area between a rectangle and a circle
        /// </summary>
        /// <param name="rect">The rectangle</param>
        /// <param name="m">The center of the circle</param>
        /// <param name="r">The radius</param>
        /// <returns>The intersection area of the two shapes</returns>
        double IntersectionArea(RectangleF rect, PointF m, float r)
        {
            decimal a = 0; //Area

            //Check whether the rectangle lies completely outside of the circle. 
            //Note: It is easier to check if a rectangle is outside another rectangle or
            //circle than to check whether it is inside.
            if ((rect.Bottom < m.Y && rect.Right < m.X)
                 && (GetDistance(new PointF(rect.Bottom, rect.Right), m) > r) ||
               (rect.Top > m.Y && rect.Right < m.X)
                 && (GetDistance(new PointF(rect.Top, rect.Right), m) > r) ||
               (rect.Bottom < m.Y && rect.Left > m.X)
                 && (GetDistance(new PointF(rect.Bottom, rect.Left), m) > r) ||
              (rect.Top > m.Y && rect.Left > m.X)
                 && (GetDistance(new PointF(rect.Top, rect.Left), m) > r))
            {
                return 0; //Terminate fast
            }

            //A variable storing the nearest horizontal edge of the rectangle. 
            double nearestRectangleEdge = 0;

            //Determine what is nearer to the circle center - the rectangle top edge or the rectangle bottom edge
            if (Math.Abs(m.Y - rect.Top) > Math.Abs(m.Y - rect.Bottom))
            {
                nearestRectangleEdge = rect.Bottom;
            }
            else
            {
                nearestRectangleEdge = rect.Top; 
            }

            //The bounds of our integration
            decimal leftBound = 0;
            decimal rightBound = 0;

            if (m.Y >= rect.Top && m.Y <= rect.Bottom)
            {
                //Take care if the circle's center lies within the rectangle. 
                leftBound = RoundToDecimal(Math.Max(-r + m.X, rect.Left));
                rightBound = RoundToDecimal(Math.Min(r + m.X, rect.Right));
            }
            else if (r >= Math.Abs(nearestRectangleEdge - m.Y))
            {
                //If the circle's center lies outside of the rectangle, we can choose optimal bounds.
                leftBound = RoundToDecimal(Math.Max(-Math.Sqrt(r * r - Math.Abs(Math.Pow(nearestRectangleEdge - m.Y, 2))) + m.X, rect.Left));
                rightBound = RoundToDecimal(Math.Min(Math.Sqrt(r * r - Math.Abs(Math.Pow(nearestRectangleEdge - m.Y, 2))) + m.X, rect.Right));
            }

            double upperBound;
            double lowerBound;

            //Loop trough the intersection area and sum up the area
            for (decimal i = leftBound + Resolution; i <= rightBound; i += Resolution)
            {
                upperBound = Math.Max(UpperRectangleFunction(rect, i - Resolution / 2), UpperCircleFunction(m, r, i - Resolution / 2));
                lowerBound = Math.Min(LowerRectangleFunction(rect, i - Resolution / 2), LowerCircleFunction(m, r, i - Resolution / 2));

                a += ((decimal)lowerBound - (decimal)upperBound) * Resolution;
            }

            return (float)a;
        }

        /// <summary>
        /// Gets the distance between two points using trigonometry
        /// </summary>
        static double GetDistance(PointF p1, PointF p2)
        {
            return Math.Sqrt(Math.Pow(p1.X - p2.X, 2) + Math.Pow(p1.Y - p2.Y, 2));
        }

        /// <summary>
        /// Discards all information behind the 3rd decimal place 
        /// and converts the double to a decimal to work around precision 
        /// errors when comparing double or float values. 
        /// </summary>
        static decimal RoundToDecimal(double d)
        {
            return Math.Round((decimal)d * 1000) / 1000;
        }

        /// <summary>
        /// Function which defines the upper rectangle curve
        /// </summary>
        static double UpperRectangleFunction(RectangleF rect, decimal x)
        {
            if (RoundToDecimal(rect.Left) > x || RoundToDecimal(rect.Right) < x)
            {
                throw new InvalidOperationException("The requested point lies outside of the rectangle");
            }
            return rect.Top;
        }

        /// <summary>
        /// Function which defines the lower rectangle curve
        /// </summary>
        static double LowerRectangleFunction(RectangleF rect, decimal x)
        {
            if (RoundToDecimal(rect.Left) > x || RoundToDecimal(rect.Right) < x)
            {
                throw new InvalidOperationException("The requested point lies outside of the rectangle");
            }
            return rect.Bottom;
        }

        /// <summary>
        /// Function which defines the upper circle curve
        /// </summary>
        static double UpperCircleFunction(PointF m, float r, decimal x)
        {
            if (RoundToDecimal(m.X - r) > x || RoundToDecimal(m.X + r) < x)
            {
                throw new InvalidOperationException("The requested point lies outside of the circle");
            }
            return m.Y - Math.Sqrt((r * r) - Math.Pow(((double)x - m.X), 2));
         }

        /// <summary>
        /// Function which defines the lower circle curve
        /// </summary>
        static double LowerCircleFunction(PointF m, float r, decimal x)
        {
            if (RoundToDecimal(m.X - r) > x || RoundToDecimal(m.X + r) < x)
            {
                throw new InvalidOperationException("The requested point lies outside of the circle");
            }
            return m.Y + Math.Sqrt((r * r) - Math.Pow(((double)x - m.X), 2));
        }
    }
}
