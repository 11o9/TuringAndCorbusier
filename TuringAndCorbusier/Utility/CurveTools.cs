using System;
using System.Collections.Generic;
using Rhino.Geometry;
using System.Linq;
using Rhino.Display;
using System.IO;

namespace TuringAndCorbusier.Utility
{
    class CurveTools
    {
        public static double GetArea(Curve input)
        {
            Polyline plotPolyline;
            input.TryGetPolyline(out plotPolyline);

            List<Point3d> y = new List<Point3d>(plotPolyline);
            double area = 0;

            for (int i = 0; i < y.Count - 1; i++)
            {
                area += y[i].X * y[i + 1].Y;
                area -= y[i].Y * y[i + 1].X;
            }

            area /= 2;
            return (area < 0 ? -area : area);
        }

        public static Polyline ToPolyline(Curve curve)
        {
            Polyline output = new Polyline();
            curve.TryGetPolyline(out output);
            return output;
        }

        public static void SetCurveAlignCCW(Curve curve)
        {
            if (curve.ToNurbsCurve().ClosedCurveOrientation(Vector3d.ZAxis) == CurveOrientation.CounterClockwise)
                curve.Reverse();

            return;
        }


        public static Curve GetAlignCCW(Curve curve)
        {
            Curve crvCopy = curve.DuplicateCurve();

            if (curve.ClosedCurveOrientation(Vector3d.ZAxis) == CurveOrientation.CounterClockwise)
                crvCopy.Reverse();

            return crvCopy;
        }

        public static Curve ChangeCoordinate(Curve baseCrv, Plane fromPln, Plane toPln)
        {
            Curve changedCrv = baseCrv.DuplicateCurve();
            changedCrv.Transform(Transform.ChangeBasis(fromPln, toPln));

            return changedCrv;
        }

        public static bool IsOverlap(Curve curve1, Curve curve2, double tolerance)
        {
            var polyIntersection = Rhino.Geometry.Intersect.Intersection.CurveCurve(curve1, curve2, 0, tolerance);
            foreach (var i in polyIntersection)
            {
                if (i.IsOverlap)
                    return true;
            }

            return false;
        }

        public static bool IsOverlap(Curve curve, List<Curve> otherCurves, double tolerance)
        {
            foreach (Curve i in otherCurves)
            {
                var tempIntersection = Rhino.Geometry.Intersect.Intersection.CurveCurve(curve, i, 0, tolerance);
                if (tempIntersection.Count == 0)
                    continue;

                foreach (var j in tempIntersection)
                {
                    if (j.IsOverlap)
                        return true;
                }
            }
            return false;
        }
    }
}
