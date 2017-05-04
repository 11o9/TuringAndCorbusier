using System;
using System.Collections.Generic;
using Rhino.Geometry;
using System.Linq;
using Rhino.Display;
using System.IO;
using Rhino;


namespace TuringAndCorbusier.Utility
{
    public static class CurveExtended
    {
        public static Point3d Centroid(this Curve curve)
        {
            if (!curve.IsClosed)
                return Point3d.Unset;

            return AreaMassProperties.Compute(curve).Centroid;

        }

        public static double GetArea(this Curve curve)
        {
            if (!curve.IsClosed)
                return 0;

            double area = AreaMassProperties.Compute(curve).Area;
            return area;
        }

        public static Curve ReduceSegments(this Curve curve, double tolerance)
        {
            if (!curve.IsClosed)
                return curve;

            Polyline crvPoly;
            curve.TryGetPolyline(out crvPoly);

            crvPoly.ReduceSegments(tolerance);
            return crvPoly.ToNurbsCurve();            
        }

        public static bool IsOverlap(this Curve curve, Curve otherCurve)
        {
            var tempIntersection = Rhino.Geometry.Intersect.Intersection.CurveCurve(curve, curve, 0, 0);
            if (tempIntersection.Count == 0)
                return false;

            foreach (var i in tempIntersection)
            {
                if (i.IsOverlap)
                    return true;
            }

            return false;
        }

        public static bool IsOverlap(this Curve curve, List<Curve> otherCurves)
        {
            foreach (Curve i in otherCurves)
            {
                var tempIntersection = Rhino.Geometry.Intersect.Intersection.CurveCurve(curve, i, 0, 0);
                if (tempIntersection.Count == 0)
                    return false;

                foreach (var j in tempIntersection)
                {
                    if (j.IsOverlap)
                        return true;
                }
            }
            return false;
        }


        public static Vector3d PV(this Curve c)
        {
            var tempv = c.TangentAtStart;
            tempv.Rotate(-Math.PI / 2, Vector3d.ZAxis);
            return tempv;
        }

    }

}
