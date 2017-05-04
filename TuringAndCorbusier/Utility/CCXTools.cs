using System;
using System.Collections.Generic;
using Rhino.Geometry;
using System.Linq;
using Rhino.Display;
using System.IO;

namespace TuringAndCorbusier.Utility
{
    class CCXTools
    {
        public static Polyline RegionIntersect(Polyline polyline1, Polyline polyline2)
        {
            Polyline resultPolyine = new Polyline();

            List<double> tempParamA = new List<double>(); //Polyline1 위의 교차점
            List<double> tempParamB = new List<double>(); //Polyline1 위의 교차점
            Curve polyCurve1 = polyline1.ToNurbsCurve();
            Curve polyCurve2 = polyline2.ToNurbsCurve();

            List<Curve> tempLocalResult = new List<Curve>();

            //mutual intersection
            var tempIntersection = Rhino.Geometry.Intersect.Intersection.CurveCurve(polyCurve1, polyCurve2, 0, 0);

            if (tempIntersection.Count == 0) //없으면 null..
                return resultPolyine;

            foreach (var i in tempIntersection)
            {
                tempParamA.Add(i.ParameterA);
                tempParamB.Add(i.ParameterB);
            }


            List<Curve> tempSplittedA = polyCurve1.Split(tempParamA).ToList();
            List<Curve> tempSplittedB = polyCurve2.Split(tempParamB).ToList();

            //case of Polyline1
            foreach (Curve i in tempSplittedA)
            {
                List<Curve> testCrvSet = i.DuplicateSegments().ToList();

                if (testCrvSet.Count == 0)
                    testCrvSet.Add(i);

                foreach (Curve j in testCrvSet)
                {
                    Point3d testPt = (j.PointAtStart + j.PointAtEnd) / 2;
                    int decider = (int)polyCurve2.Contains(testPt);

                    if (decider != 2)
                        tempLocalResult.Add(j);
                }
            }

            //case of Polyline2
            foreach (Curve i in tempSplittedB)
            {
                List<Curve> testCrvSet = i.DuplicateSegments().ToList();

                if (testCrvSet.Count == 0)
                    testCrvSet.Add(i);

                foreach (Curve j in testCrvSet)
                {
                    Point3d testPt = (j.PointAtStart + j.PointAtEnd) / 2;
                    int decider = (int)polyCurve1.Contains(testPt);

                    if (decider != 2)
                        tempLocalResult.Add(j);
                }
            }
            List<Curve> resultList = Curve.JoinCurves(tempLocalResult,0,true).ToList();
            resultList.Sort((a,b) => a.GetArea().CompareTo(b.GetArea()));

            if (resultList.Count != 0)
                resultPolyine = CurveTools.ToPolyline(resultList[0]);

            return resultPolyine;
        }

        public static List<Curve> RegionIntersect(List<Curve> curveSet1, List<Curve> curveSet2)
        {
            List<Curve> IntersectCrvs = new List<Curve>();
            foreach (Curve i in curveSet1)
            {
                foreach (Curve j in curveSet2)
                {
                    List<double> tempParamA = new List<double>();
                    List<double> tempParamB = new List<double>();
                    List<Curve> tempLocalResult = new List<Curve>();

                    var tempIntersection = Rhino.Geometry.Intersect.Intersection.CurveCurve(i, j, 0, 0);

                    if (tempIntersection.Count == 0) // 없으면 다음커브로..
                        continue;

                    foreach (var k in tempIntersection)
                    {
                        tempParamA.Add(k.ParameterA);
                        tempParamB.Add(k.ParameterB);
                    }

                    List<Curve> tempSplittedA = i.Split(tempParamA).ToList();
                    List<Curve> tempSplittedB = j.Split(tempParamB).ToList();

                    //case of Curve1
                    foreach (Curve k in tempSplittedA)
                    {
                        List<Curve> testCrvSet = k.DuplicateSegments().ToList();

                        if (testCrvSet.Count == 0)
                            testCrvSet.Add(k);

                        foreach (Curve l in testCrvSet)
                        {
                            Point3d testPt = new Point3d((l.PointAtEnd + l.PointAtStart) / 2);
                            int decider = (int)j.Contains(testPt);

                            if (decider != 2)
                                tempLocalResult.Add(l);
                        }
                    }

                    //case of Curve2
                    foreach (Curve k in tempSplittedB)
                    {
                        List<Curve> testCrvSet = k.DuplicateSegments().ToList();

                        if (testCrvSet.Count == 0)
                            testCrvSet.Add(k);

                        foreach (Curve l in testCrvSet)
                        {
                            Point3d testPt = new Point3d((l.PointAtEnd + l.PointAtStart) / 2);
                            int decider = (int)i.Contains(testPt);

                            if (decider != 2)
                                tempLocalResult.Add(l);
                        }
                    }
                    IntersectCrvs.AddRange(Curve.JoinCurves(tempLocalResult).ToList());
                }
            }
            return IntersectCrvs;
        }

        public static Point3d GetCrossPt(Line line1, Line line2)
        {
            //dSide: directionVector 쪽, oSide: origin 쪽
            Point3d origin1 = line1.PointAt(0);
            Vector3d direction1 = line1.UnitTangent;

            Point3d origin2 = line2.PointAt(0);
            Vector3d direction2 = line2.UnitTangent;

            //ABC is coefficient of linear Equation, Ax+By=C 
            double A1 = direction1.Y;
            double B1 = -direction1.X;
            double C1 = A1 * origin1.X + B1 * origin1.Y;

            double A2 = direction2.Y;
            double B2 = -direction2.X;
            double C2 = A2 * origin2.X + B2 * origin2.Y;

            //det=0: isParallel, 평행한 경우
            double detTolerance = 0.005;
            double det = A1 * B2 - B1 * A2;

            if (Math.Abs(det) < detTolerance)
                return Point3d.Unset;

            double crossX = (B2 * C1 - B1 * C2) / det;
            double crossY = (A1 * C2 - A2 * C1) / det;

            return new Point3d(crossX, crossY, origin1.Z);
        }

        /// <summary>
        /// Curve.ClosestPoint와 같은 역할을 합니다. 평행일 경우 Point3d.Unset 을 반환합니다.
        /// </summary>
        public static Point3d GetCrossPt3D(Line line1, Line line2)
        {
            //Set origins and directional vectors.
            Point3d origin1 = line1.PointAt(0);
            Vector3d direction1 = line1.UnitTangent;
            Point3d origin2 = line2.PointAt(0);
            Vector3d direction2 = line2.UnitTangent;


            //Set matrices
            //Line equations are expressed by parameter t,s
            //l1: O1 + D1*t, l2: O2 + D2*s
            //Ax = B, x = [t ,s]T
            Matrix A = new Matrix(3, 2);
            A[0, 0] = direction1.X; A[0, 1] = -direction2.X;
            A[1, 0] = direction1.Y; A[1, 1] = -direction2.Y;
            A[2, 0] = direction1.Z; A[2, 1] = -direction2.Z;

            Matrix B = new Matrix(3, 1);
            B[0, 0] = origin2.X - origin1.X;
            B[1, 0] = origin2.Y - origin1.Y;
            B[2, 0] = origin2.Z - origin1.Z;


            //Get pseudo inverse.
            Matrix At = A.Duplicate();
            At.Transpose();
            Matrix AtAInverse = At * A;

            bool hasInverse = AtAInverse.Invert(0);
            if (!hasInverse)
                return Point3d.Unset;

            Matrix AiPseudo = AtAInverse * At;


            //Solve equation.
            Matrix x = AiPseudo * B;

            Point3d crossPt = origin1 + direction1 * x[0, 0]; //반환 값
            Point3d crossPtOnLine2 = origin2 + direction2 * x[1, 0]; //다른 커브 위의 점

            return crossPt;
        }

        public static bool IsIntersect(Line line1, Line line2, bool checkCoincidence)
        {
            //seive1: xRange
            double xHi1 = Math.Max(line1.FromX, line1.ToX);
            double xLo1 = Math.Min(line1.FromX, line1.ToX);
            double xHi2 = Math.Max(line2.FromX, line2.ToX);
            double xLo2 = Math.Min(line2.FromX, line2.ToX);

            if (xHi1 > xHi2)
            {
                if (xLo1 > xHi2)
                    return false;
            }

            if (xHi2 > xHi1)
            {
                if (xLo2 > xHi1)
                    return false;
            }

            //seive2: yRange
            double yHi1 = Math.Max(line1.FromY, line1.ToY);
            double yLo1 = Math.Min(line1.FromY, line1.ToY);
            double yHi2 = Math.Max(line2.FromY, line2.ToY);
            double yLo2 = Math.Min(line2.FromY, line2.ToY);

            if (yHi1 > yHi2)
            {
                if (yLo1 > yHi2)
                    return false;
            }

            if (yHi2 > yHi1)
            {
                if (yLo2 > yHi1)
                    return false;
            }

            //intersect check
            Point3d crossPt = GetCrossPt(line1, line2);
            if (crossPt == Point3d.Unset && checkCoincidence) //colinear check
            {
                if (PCXTools.IsPtOnLine(line2.From, line1, 0.5))
                    return true;

                if (PCXTools.IsPtOnLine(line2.To, line1, 0.5))
                    return true;

                return false;
            }


            if (!checkCoincidence)  //end point check
            {
                if (crossPt.DistanceTo(line1.From) < 0.5)
                    return false;

                if (crossPt.DistanceTo(line1.To) < 0.5)
                    return false;

                if (crossPt.DistanceTo(line2.From) < 0.5)
                    return false;

                if (crossPt.DistanceTo(line2.To) < 0.5)
                    return false;
            }

            if (!PCXTools.IsPtOnLine(crossPt, line1, 0.5))
                return false;

            if (!PCXTools.IsPtOnLine(crossPt, line2, 0.5))
                return false;

            return true;
        }

        public static bool IsIntersectCrv(Polyline checker, Polyline boundary, bool checkCoincidence)
        {
            Line[] seg1 = checker.GetSegments();
            Line[] seg2 = boundary.GetSegments();

            Point3d[] xOrder = checker.OrderByDescending(n => n.X).ToArray();
            Point3d[] yOrder = checker.OrderByDescending(n => n.Y).ToArray();

            double xHi1 = xOrder.First().X;
            double xLo1 = xOrder.Last().X;
            double yHi1 = yOrder.First().Y;
            double yLo1 = yOrder.Last().Y;

            foreach (Line i in seg2)
            {
                //seive
                double xHi2 = Math.Max(i.FromX, i.ToX);
                double xLo2 = Math.Min(i.FromX, i.ToX);

                if (xHi1 > xHi2)
                {
                    if (xLo1 > xHi2)
                        continue;
                }

                if (xHi2 > xHi1)
                {
                    if (xLo2 > xHi1)
                        continue;
                }

                double yHi2 = Math.Max(i.FromY, i.ToY);
                double yLo2 = Math.Min(i.FromY, i.ToY);

                if (yHi1 > yHi2)
                {
                    if (yLo1 > yHi2)
                        continue;
                }

                if (yHi2 > yHi1)
                {
                    if (yLo2 > yHi1)
                        continue;
                }

                //check
                foreach (Line j in seg1)
                {
                    if (IsIntersect(j, i, checkCoincidence))
                        return true;
                }
            }

            return false;
        }
    }
}
