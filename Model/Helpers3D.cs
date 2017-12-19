using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Media3D;

namespace Model
{
    /// <summary>
    /// A collection of useful methods implementing certain common Cartesian functions.
    /// </summary>
    public static class Helpers3D
    {

        /// <summary>
        /// Returns the distance between two described 3D points.
        /// </summary>
        public static double GetDistance(Point3D pointA, Point3D pointB)
        {
            return GetDistance(pointA.X, pointA.Y, pointA.Z, pointB.X, pointB.Y, pointB.Z);
        }

        /// <summary>
        /// Returns the distance between two described 3D points.
        /// </summary>
        /// <param name="Xa">The x value of point A.</param>
        /// <param name="Ya">The y value of point A.</param>
        /// <param name="Za">The z value of point A.</param>
        /// <param name="Xb">The x value of point B.</param>
        /// <param name="Yb">The y value of Point B.</param>
        /// <param name="Zb">The z value of Point B.</param>
        /// <returns></returns>
        public static double GetDistance(double Xa, double Ya, double Za, double Xb, double Yb, double Zb)
        {
            return Math.Sqrt(Math.Pow((Xb - Xa), 2) + Math.Pow((Yb - Ya), 2) + Math.Pow((Zb - Za), 2));
        }

        /// <summary>
        /// Returns the distance between two described 2D points.
        /// </summary>
        public static double GetDistance(Point pointA, Point pointB)
        {
            return GetDistance(pointA.X, pointA.Y, pointB.X, pointB.Y);
        }
        /// <summary>
        /// Returns the distance between two described 2D points.
        /// </summary>
        public static double GetDistance(double Xa, double Ya, double Xb, double Yb)
        {
            return Math.Sqrt(Math.Pow((Xb - Xa), 2) + Math.Pow((Yb - Ya), 2));
        }


        /// <summary>
        /// Returns the normal vector perpendicular to the plane described by three 3D points.  For 
        /// clarity, if you imagine looking down on a counterclockwise-decribed triangle with PointA at 
        /// the bottom, PointB in the upper-right, and PointC in the upper-left, the then normal vector 
        /// will stab you in the eye.  (Wear safety goggles when using this method.)
        /// </summary>         
        public static Vector3D GetNormal(Point3D pointA, Point3D pointB, Point3D pointC)
        {
            return GetNormal(pointB - pointA, pointC - pointA);
        }

        /// <summary>
        /// Returns the normal vector perpendicular to the plane described by two vectors.  For 
        /// clarity, if you imagine looking down on a V-shaped structure with vectorA pointing up-right, 
        /// and vectorB pointing up-left, then the normal vector will stab you in the eye.  (Wear safety 
        /// goggles when using this method.)
        /// </summary>    
        public static Vector3D GetNormal(Vector3D vectorA, Vector3D vectorB)
        {
            Vector3D result =  Vector3D.CrossProduct(vectorA, vectorB);
            result.Normalize();
            return result;
        }


        
    }
}
