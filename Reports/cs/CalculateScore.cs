using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rhino.Geometry;
using System.Collections;

namespace TuringAndCorbusier
{
    class CalculateScore
    {
        public static int SatisfyTargetArea(ApartmentGeneratorOutput agOut)
        {
            List<double> targetData = new List<double>();
            List<double> realData = new List<double>();

            for (int i = 0; i < agOut.HouseholdProperties.Count(); i++)
            {
                for (int j = 0; j < agOut.HouseholdProperties[i].Count(); j++)
                {
                    for (int k = 0; k < agOut.HouseholdProperties[i][j].Count(); k++)
                    {
                        targetData.Add(agOut.HouseholdProperties[i][j][k].GetArea());
                        realData.Add(agOut.Target.TargetArea[ agOut.HouseholdProperties[i][j][k].HouseholdSizeType]);
                    }
                }
            }

            //make list of ((real/target)-1)
            List<double> resultRatio = new List<double>();
            for (int i = 0; i < targetData.Count; i++)
            {
                resultRatio.Add((realData[i] / targetData[i]) - 1);
            }

            //standard deviation
            double variance = 0;
            for (int i = 0; i < resultRatio.Count; i++)
            {
                variance += resultRatio[i] * resultRatio[i] / resultRatio.Count;
            }

            double std = Math.Sqrt(variance);

            //뭔가 나온 결과값들 보니까 그냥 100 곱한 다음에 100점에서 빼면 될 것 같이 생김

            int result = (int)(100 - std * 100);

            return result;
        }

        public static int FacingSouth(ApartmentGeneratorOutput agOut)
        {
            //create house outlines and windows with normal facing outwards
            List<List<List<Curve>>> houseOutline = new List<List<List<Curve>>>();
            List<List<List<List<Line>>>> windowLinesOld = agOut.getLightingWindow();
            List<List<List<List<Line>>>> windowLines = new List<List<List<List<Line>>>>();

            for (int i = 0; i < agOut.HouseholdProperties.Count; i++)
            {
                List<List<Curve>> houseOutline_i = new List<List<Curve>>();
                List<List<List<Line>>> windowLines_i = new List<List<List<Line>>>();

                for (int j = 0; j < agOut.HouseholdProperties[i].Count; j++)
                {
                    List<Curve> houseOutline_j = new List<Curve>();
                    List<List<Line>> windowLines_j = new List<List<Line>>();

                    for (int k = 0; k < agOut.HouseholdProperties[i][j].Count(); k++)
                    {
                        //create house outline curve
                        List<Point3d> outlinePoints = new List<Point3d>();
                        Point3d pt = new Point3d(agOut.HouseholdProperties[i][j][k].Origin);
                        Vector3d x = new Vector3d(agOut.HouseholdProperties[i][j][k].XDirection);
                        Vector3d y = new Vector3d(agOut.HouseholdProperties[i][j][k].YDirection);
                        double xa = agOut.HouseholdProperties[i][j][k].XLengthA;
                        double xb = agOut.HouseholdProperties[i][j][k].XLengthB;
                        double ya = agOut.HouseholdProperties[i][j][k].YLengthA;
                        double yb = agOut.HouseholdProperties[i][j][k].YLengthB;

                        outlinePoints.Add(pt);
                        pt.Transform(Transform.Translation(Vector3d.Multiply(y, yb)));
                        outlinePoints.Add(pt);
                        pt.Transform(Transform.Translation(Vector3d.Multiply(x, xa - xb)));
                        outlinePoints.Add(pt);
                        pt.Transform(Transform.Translation(Vector3d.Multiply(y, -ya)));
                        outlinePoints.Add(pt);
                        pt.Transform(Transform.Translation(Vector3d.Multiply(x, -xa)));
                        outlinePoints.Add(pt);
                        pt.Transform(Transform.Translation(Vector3d.Multiply(y, ya - yb)));
                        outlinePoints.Add(pt);

                        Point3d.CullDuplicates(outlinePoints, 0);

                        pt.Transform(Transform.Translation(Vector3d.Multiply(x, xb)));
                        outlinePoints.Add(pt);

                        Polyline outlinePolyline = new Polyline(outlinePoints);
                        Curve outlineCurve = outlinePolyline.ToNurbsCurve();
                        houseOutline_j.Add(outlineCurve);

                        //create windows with normal facing outwards : window normal is defined as window line tangent rotated Pi/2
                        List<Line> windowLines_k = new List<Line>();

                        for (int l = 0; l < windowLinesOld[i][j][k].Count; l++)
                        {
                            Line winLine = windowLinesOld[i][j][k][l];
                            Vector3d normal = winLine.UnitTangent;
                            normal.Rotate(Math.PI / 2, Vector3d.ZAxis);
                            Point3d midpt = winLine.PointAt(0);
                            midpt.Transform(Transform.Translation(Vector3d.Multiply(winLine.UnitTangent, winLine.Length / 2)));
                            midpt.Transform(Transform.Translation(Vector3d.Multiply(normal, 10)));
                            if (outlineCurve.Contains(midpt) == Rhino.Geometry.PointContainment.Inside)
                            {
                                winLine.Flip();
                                windowLines_k.Add(winLine);
                            }
                            else if (outlineCurve.Contains(midpt) == Rhino.Geometry.PointContainment.Outside)
                            {
                                windowLines_k.Add(winLine);
                            }
                        }
                        windowLines_j.Add(windowLines_k);
                    }

                    houseOutline_i.Add(houseOutline_j);
                    windowLines_i.Add(windowLines_j);
                }
                houseOutline.Add(houseOutline_i);
                windowLines.Add(windowLines_i);
            }

            //calculate southward wall and window ratio
            List<List<double>> southwardWindowRatio = new List<List<double>>();
            List<double> ratiosForScore = new List<double>();
            for (int i = 0; i < windowLines.Count; i++)
            {
                List<double> southwardWindowRatioTemp = new List<double>();
                for (int j = 0; j < windowLines[i].Count; j++)
                {
                    double southwardWindowRatioTempTemp = 0;
                    for (int k = 0; k < windowLines[i][j].Count; k++)
                    {
                        for (int l = 0; l < windowLines[i][j][k].Count(); l++)
                        {
                            southwardWindowRatioTempTemp += southwardWindow(houseOutline[i][j][k], windowLines[i][j][k][l]);
                        }
                    }
                    ratiosForScore.Add(southwardWindowRatioTempTemp);
                    southwardWindowRatioTemp.Add(southwardWindowRatioTempTemp);
                }
                southwardWindowRatio.Add(southwardWindowRatioTemp);
            }
            //average
            double avrg = 0;
            for (int i = 0; i < ratiosForScore.Count; i++)
            {
                avrg += ratiosForScore[i] / ratiosForScore.Count;
            }

            return (int)(avrg * 100);
        }

        private static double southwardWindow(Curve outline, Line window)
        {
            double alpha = 10;
            Polyline outlinePoly;
            outline.TryGetPolyline(out outlinePoly);
            var bBox = outlinePoly.BoundingBox;
            Point3d minP = bBox.Min;
            Point3d maxP = bBox.Max;
            Vector3d south = Vector3d.Multiply(Vector3d.YAxis, -1);
            Vector3d windowNormal = window.UnitTangent;
            windowNormal.Rotate(Math.PI / 2, Vector3d.ZAxis);
            double validRatio = 0;

            //check if window is facing south
            if (Vector3d.Multiply(windowNormal, south) > 0)
            {

                Curve windowCurve = window.ToNurbsCurve();
                List<Point3d> pts = new List<Point3d>();
                List<Point3d> alivePts = new List<Point3d>();
                List<Point3d> deadPts = new List<Point3d>();
                pts.Add(windowCurve.PointAtEnd);
                pts.Add(windowCurve.PointAtStart);
                double projectedLength = maxP.X - minP.X;
                Curve validWindow;

                for (int i = 0; i < 2; i++)
                {
                    Line downLine = new Line(pts[i], south, maxP.Y - minP.Y);
                    downLine.Transform(Transform.Translation(Vector3d.Multiply(south, alpha)));
                    int xNum = Rhino.Geometry.Intersect.Intersection.CurveCurve(downLine.ToNurbsCurve(), outline, 0, 0).Count;
                    if (xNum == 0)
                    {
                        alivePts.Add(pts[i]);
                    }
                    else
                    {
                        deadPts.Add(pts[i]);
                    }
                }
                if (alivePts.Count == 2)
                {
                    validWindow = windowCurve;
                }
                else if (alivePts.Count == 0)
                {
                    Point3d pt1 = Point3d.Origin;
                    Point3d pt2 = Point3d.Origin;
                    Line zeroL = new Line(pt1, pt2);
                    Curve zeroLL = zeroL.ToNurbsCurve();
                    validWindow = zeroLL;
                }
                else
                {
                    List<Point3d> outlinePoints = new List<Point3d>(outlinePoly);
                    List<Point3d> windowIntersectionPoints = new List<Point3d>();
                    double windowHighY = Math.Max(pts[0].Y, pts[1].Y);
                    for (int i = 0; i < outlinePoints.Count; i++)
                    {
                        if (outlinePoints[i].Y < windowHighY && outlinePoints[i].X > Math.Min(alivePts[0].X, deadPts[0].X) && outlinePoints[i].X < Math.Max(alivePts[0].X, deadPts[0].X))
                        {
                            Point3d upperPoint = new Point3d(outlinePoints[i].X, windowHighY, 0);
                            Point3d lowerPoint = new Point3d(outlinePoints[i].X, minP.Y - alpha, 0);
                            Line checkIntersectionLine = new Line(upperPoint, lowerPoint);
                            windowIntersectionPoints.Add(Rhino.Geometry.Intersect.Intersection.CurveCurve(checkIntersectionLine.ToNurbsCurve(), windowCurve, 0, 0)[0].PointA);
                        }
                    }
                    List<double> dists = new List<double>();
                    for (int i = 0; i < windowIntersectionPoints.Count; i++)
                    {
                        dists.Add(alivePts[0].DistanceTo(windowIntersectionPoints[i]));
                    }
                    List<double> distsTemp = new List<double>(dists);
                    distsTemp.Sort();
                    Point3d cutPoint = windowIntersectionPoints[dists.IndexOf(distsTemp[0])];
                    Line cutWindow = new Line(alivePts[0], cutPoint);
                    validWindow = cutWindow.ToNurbsCurve();
                }

                Point3d sP = validWindow.PointAtStart;
                Point3d eP = validWindow.PointAtEnd;
                double validLength = Math.Abs(sP.X - eP.X);
                validRatio = validLength / (maxP.X - minP.X);
            }
            return validRatio;
        }
    }
}