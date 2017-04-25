using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rhino.Geometry;
using Rhino.Collections;

namespace TuringAndCorbusier
{
    public class RegulationChecker
    {
        //Constructor, 생성자

        public RegulationChecker(ApartmentGeneratorOutput agOut)
        {
            //boundary regulation
            int storiesHigh = (int)Math.Max(agOut.ParameterSet.Parameters[0], agOut.ParameterSet.Parameters[1]);
            int storiesLow = (int)Math.Min(agOut.ParameterSet.Parameters[0], agOut.ParameterSet.Parameters[1]);

            this.FromSurroundings = fromSurroundingsRegulation(agOut);
            this.FromNorthHigh = fromNorthRegulation(agOut, storiesHigh);
            this.FromNorthLow = fromNorthRegulation(agOut, storiesLow);
            this.ByLightingHigh = byLightingRegulation(agOut, storiesHigh);

            //building distance regulation
            this.BuildingDistRegulation = buildingDistanceRegulation(agOut);

        }

        //Property, 속성
        public Curve FromSurroundings { get; private set; }
        public Curve FromNorthHigh { get; private set; }
        public Curve FromNorthLow { get; private set; }
        public Curve ByLightingHigh { get; private set; }
        public Curve ByLightingLow { get; private set; }
        public List<FloorPlan.Dimension> BuildingDistRegulation { get; private set; }

        //method, 메소드
        private Curve fromSurroundingsRegulation(ApartmentGeneratorOutput agOut)
        {
            //initial settings
            Polyline outline;
            agOut.Plot.Boundary.TryGetPolyline(out outline);
            List<Line> lines = outline.GetSegments().ToList();
            Regulation regulation = new Regulation(agOut.ParameterSet.Stories);

            //offset distance
            List<double> offsetDists = new List<double>();
            for (int i = 0; i < agOut.Plot.Surroundings.Length; i++)
            {
                if (agOut.Plot.Surroundings[i] == 0)
                    offsetDists.Add(regulation.DistanceFromPlot);
                else
                    offsetDists.Add(regulation.DistanceFromRoad);
            }

            //offset lines and add the first to last(for intersect lineline operation)
            List<Line> linesTemp = new List<Line>();
            for (int i = 0; i < lines.Count; i++)
            {
                Vector3d vec = lines[i].UnitTangent;
                vec.Rotate(-Math.PI / 2, Vector3d.ZAxis);//inside direction
                Line lineTemp = lines[i];
                lineTemp.Transform(Transform.Translation(vec * offsetDists[i]));
                linesTemp.Add(lineTemp);
            }
            linesTemp.Add(linesTemp[0]);
            lines = new List<Line>(linesTemp);

            //intersection points
            List<Point3d> ptsNew = new List<Point3d>();
            for (int i = 0; i < lines.Count - 1; i++)
            {
                double a, b;
                bool check = Rhino.Geometry.Intersect.Intersection.LineLine(lines[i], lines[i + 1], out a, out b);
                if (check)
                    ptsNew.Add(lines[i].PointAt(a));
                else
                    ptsNew.Add(lines[i].PointAt(1));
            }
            ptsNew.Add(ptsNew[0]);

            return new Polyline(ptsNew).ToNurbsCurve();
        }

        private Curve fromNorthRegulation(ApartmentGeneratorOutput agOut, int stories)
        {
            Plot plot = agOut.Plot;
            Regulation regulation = new Regulation(stories);
            Polyline temp;
            plot.Boundary.TryGetPolyline(out temp);
            Curve[] plotArr = temp.GetSegments().Select(n => n.ToNurbsCurve()).ToArray();

            //법규적용(일조에 의한 높이제한)

            double[] distanceFromNorth = new double[plotArr.Length];
            for (int i = 0; i < plotArr.Length; i++)
            {
                Vector3d tempVector = new Vector3d(plotArr[i].PointAt(plotArr[i].Domain.T1) - plotArr[i].PointAt(plotArr[i].Domain.T0));
                tempVector = tempVector / tempVector.Length;

                double moveDistance = 0;

                if (tempVector.X > 0 && plot.SimplifiedSurroundings[i] >= 0)
                {
                    double tempSineFactor = System.Math.Abs(tempVector.X);

                    double tempDistanceByRoad = plot.SimplifiedSurroundings[i] * 1000 / 2 * tempVector.Length / System.Math.Abs(tempVector.X);
                    if (tempDistanceByRoad < regulation.DistanceFromNorth)
                        moveDistance = regulation.DistanceFromNorth - tempDistanceByRoad;
                }

                distanceFromNorth[i] = moveDistance;
            }

            List<Point3d> distanceFromNorthPts = new List<Point3d>();

            for (int i = 0; i < plotArr.Length; i++)
            {
                int h = (i - 1 + plotArr.Length) % plotArr.Length;
                int j = (i + 1 + plotArr.Length) % plotArr.Length;

                double distH = distanceFromNorth[h];
                double distI = distanceFromNorth[i];
                double distJ = distanceFromNorth[j];

                if (distI == 0)
                {
                    distanceFromNorthPts.Add(plotArr[i].PointAt(plotArr[i].Domain.T0));
                    distanceFromNorthPts.Add(plotArr[i].PointAt(plotArr[i].Domain.T1));
                }
                else
                {
                    Curve tempCurve = new LineCurve(plotArr[i].PointAt(plotArr[i].Domain.T0), plotArr[i].PointAt(plotArr[i].Domain.T1));
                    tempCurve.Transform(Transform.Translation(new Vector3d(0, -distI, 0)));

                    var tempIntersect1 = Rhino.Geometry.Intersect.Intersection.CurveCurve(tempCurve, plotArr[h], 0, 0);
                    var tempIntersect2 = Rhino.Geometry.Intersect.Intersection.CurveCurve(tempCurve, plotArr[j], 0, 0);

                    if (tempIntersect1.Count > 0)
                        distanceFromNorthPts.Add(tempIntersect1[0].PointA);
                    else if (tempIntersect1.Count <= 0)
                        distanceFromNorthPts.Add(tempCurve.PointAt(tempCurve.Domain.T0));

                    if (tempIntersect2.Count > 0)
                        distanceFromNorthPts.Add(tempIntersect2[0].PointA);
                    else if (tempIntersect2.Count <= 0)
                        distanceFromNorthPts.Add(tempCurve.PointAt(tempCurve.Domain.T1));
                }
            }

            distanceFromNorthPts.Add(distanceFromNorthPts[0]);

            Curve fromNorthCurve = (new Polyline(distanceFromNorthPts)).ToNurbsCurve();
            return fromNorthCurve;
        }

        private Curve byLightingRegulation(ApartmentGeneratorOutput agOut, int stories)
        {
            double angleRadian = agOut.ParameterSet.Parameters[3];
            Curve lighting1 = byLightingCurve(agOut, stories, angleRadian);
            Curve lighting2 = byLightingCurve(agOut, stories, angleRadian + Math.PI / 2);
            if (agOut.AGtype == "PT-1")
                return lighting1;
            else
                return CommonFunc.joinRegulations(lighting1, lighting2);
        }

        private Curve byLightingCurve(ApartmentGeneratorOutput agOut, int stories, double angleRadian)
        {
            //법규적용 인접대지경계선(채광창)


            Plot plot = agOut.Plot;
            Regulation regulation = new Regulation(stories);
            Polyline temp;
            plot.Boundary.TryGetPolyline(out temp);
            Curve[] plotArr = temp.GetSegments().Select(n => n.ToNurbsCurve()).ToArray();

            //extend plotArr
            Curve[] plotArrExtended = new Curve[plotArr.Length];
            Array.Copy(plotArr, plotArrExtended, plotArr.Length);

            for (int i = 0; i < plotArrExtended.Length; i++)
            {
                Curve tempCurve = plotArrExtended[i].Extend(CurveEnd.Both, 20000, CurveExtensionStyle.Line);

                plotArrExtended[i] = new LineCurve(tempCurve.PointAt(tempCurve.Domain.T0 - Math.Abs(tempCurve.Domain.T1 - tempCurve.Domain.T0)), tempCurve.PointAt(tempCurve.Domain.T1 + Math.Abs(tempCurve.Domain.T1 - tempCurve.Domain.T0)));
            }
            //

            double[] distanceByLighting = new double[plotArr.Length];
            for (int i = 0; i < plotArr.Length; i++)
            {
                Vector3d tempVector = new Vector3d(plotArr[i].PointAt(plotArr[i].Domain.T1) - plotArr[i].PointAt(plotArr[i].Domain.T0));
                Vector3d baseVector = new Vector3d(Math.Cos(angleRadian), Math.Sin(angleRadian), 0);
                //검증할 필요 있음

                double tempAngle = Math.Acos((tempVector.X * baseVector.X + tempVector.Y * baseVector.Y) / tempVector.Length / baseVector.Length);
                double convertedAngle = Math.PI - (Math.Abs(tempAngle - Math.PI));
                ///검증할 필요 있음
                double offsetDistance = ((plot.PlotType == PlotType.상업지역) ? 0 : regulation.DistanceByLighting) * Math.Cos(convertedAngle) - 0.5 * plot.SimplifiedSurroundings[i];

                if (offsetDistance > 0)
                {
                    distanceByLighting[i] = offsetDistance;
                }
                else
                {
                    distanceByLighting[i] = 0;
                }
            }

            List<Point3d> ptsByLighting = new List<Point3d>();
            for (int i = 0; i < plotArr.Length; i++)
            {
                int j = (i - 1 + plotArr.Length) % plotArr.Length;

                Vector3d iVec = plotArrExtended[i].TangentAtStart;
                Vector3d jVec = plotArrExtended[j].TangentAtStart;

                Vector3d iVert = new Vector3d(iVec);
                iVert.Rotate(-Math.PI / 2, Vector3d.ZAxis);
                iVert.Unitize();
                Vector3d jVert = new Vector3d(jVec);
                jVert.Rotate(-Math.PI / 2, Vector3d.ZAxis);
                jVert.Unitize();

                Point3d pt = plotArr[i].PointAtStart;
                Point3d iP = new Point3d(pt + iVert * distanceByLighting[i]);
                Point3d jP = new Point3d(pt + jVert * distanceByLighting[j]);

                double a, b;
                Line iLine = new Line(iP, iVec);
                Line jLine = new Line(jP, jVec);
                Rhino.Geometry.Intersect.Intersection.LineLine(iLine, jLine, out a, out b);
                ptsByLighting.Add(iLine.PointAt(a));

            }

            ptsByLighting.Add(ptsByLighting[0]);
            Curve byLightingCurve = (new Polyline(ptsByLighting)).ToNurbsCurve();

            return byLightingCurve;
        }

        private List<FloorPlan.Dimension> buildingDistanceRegulation(ApartmentGeneratorOutput agOut)
        {
            double aptWidth = agOut.ParameterSet.Parameters[2];
            //create core outlines
            List<List<Curve>> coreOutline = new List<List<Curve>>();
            for (int i = 0; i < agOut.CoreProperties.Count; i++)
            {
                List<Curve> coreOutlineTemp = new List<Curve>();
                for (int j = 0; j < agOut.CoreProperties[i].Count; j++)
                {
                    List<Point3d> outlinePoints = new List<Point3d>();

                    Point3d pt = new Point3d(agOut.CoreProperties[i][j].Origin);
                    Vector3d x = new Vector3d(agOut.CoreProperties[i][j].XDirection);
                    Vector3d y = new Vector3d(agOut.CoreProperties[i][j].YDirection);
                    double width = agOut.CoreProperties[i][j].CoreType.GetWidth();
                    double depth = agOut.CoreProperties[i][j].CoreType.GetDepth();

                    outlinePoints.Add(pt);
                    pt.Transform(Transform.Translation(Vector3d.Multiply(x, width)));
                    outlinePoints.Add(pt);
                    pt.Transform(Transform.Translation(Vector3d.Multiply(y, depth)));
                    outlinePoints.Add(pt);
                    pt.Transform(Transform.Translation(Vector3d.Multiply(x, -width)));
                    outlinePoints.Add(pt);
                    pt.Transform(Transform.Translation(Vector3d.Multiply(y, -depth)));
                    outlinePoints.Add(pt);

                    Polyline outlinePolyline = new Polyline(outlinePoints);
                    Curve outlineCurve = outlinePolyline.ToNurbsCurve();
                    coreOutlineTemp.Add(outlineCurve);
                }
                coreOutline.Add(coreOutlineTemp);
            }

            //create building outlines
            List<List<Curve>> buildingOutline = agOut.buildingOutline;

            //create aptlines
            List<Curve> aptlines = agOut.AptLines;

            if (agOut.AGtype == "PT-1")
            {
                //Lighting to Lighting
                List<Curve> ll = new List<Curve>();
                for (int i = 0; i < aptlines.Count - 1; i++)
                {
                    Point3d sP = aptlines[i].PointAtStart;
                    Vector3d ver = aptlines[i].TangentAtStart;
                    ver.Rotate(Math.PI / 2, Vector3d.ZAxis);
                    //sP.Transform(Transform.Translation(ver * aptWidth / 2));

                    List<Point3d> distP = new List<Point3d>();
                    List<double> dists = new List<double>();
                    for (int j = 0; j < aptlines.Count; j++)
                    {
                        double param;
                        aptlines[j].ClosestPoint(sP, out param);
                        Point3d testP = aptlines[j].PointAt(param);
                        if (sP.DistanceTo(testP) > aptWidth && Math.Abs(Vector3d.Multiply(ver, new Vector3d(testP - sP))) > 0.99 * sP.DistanceTo(testP))
                        {
                            distP.Add(testP);
                            dists.Add(sP.DistanceTo(testP));
                        }
                    }
                    if (dists.Count > 0)
                    {
                        Point3d closestP = distP[dists.IndexOf(dists.Min())];
                        Vector3d moveVec = new Vector3d(closestP - sP);
                        moveVec.Unitize();
                        sP.Transform(Transform.Translation(moveVec * aptWidth / 2));
                        closestP.Transform(Transform.Translation(-moveVec * aptWidth / 2));
                        ll.Add(new Line(sP, closestP).ToNurbsCurve());
                    }

                }

                //Wall to Lighting
                List<Curve> lw = new List<Curve>();
                for (int i = 0; i < agOut.CoreProperties.Count; i++)
                {
                    for (int j = 0; j < agOut.CoreProperties[i].Count; j++)
                    {
                        List<Point3d> outlinePoints = new List<Point3d>();

                        Point3d pt = new Point3d(agOut.CoreProperties[i][j].Origin);
                        Vector3d x = new Vector3d(agOut.CoreProperties[i][j].XDirection);
                        Vector3d y = new Vector3d(agOut.CoreProperties[i][j].YDirection);
                        double width = agOut.CoreProperties[i][j].CoreType.GetWidth();
                        double depth = agOut.CoreProperties[i][j].CoreType.GetDepth();

                        pt.Transform(Transform.Translation(Vector3d.Multiply(x, width / 2)));
                        pt.Transform(Transform.Translation(Vector3d.Multiply(y, depth)));

                        List<Point3d> distP = new List<Point3d>();
                        List<double> dists = new List<double>();
                        for (int k = 0; k < buildingOutline.Count; k++)
                        {
                            double param;
                            buildingOutline[k][0].ClosestPoint(pt, out param);
                            Point3d testP = buildingOutline[k][0].PointAt(param);
                            if (pt.DistanceTo(testP) > depth + 1 && Vector3d.Multiply(y, new Vector3d(testP - pt)) > 0.99 * pt.DistanceTo(testP))
                            {
                                distP.Add(testP);
                                dists.Add(pt.DistanceTo(testP));
                            }
                        }
                        if (dists.Count > 0)
                        {
                            Point3d closestP = distP[dists.IndexOf(dists.Min())];
                            lw.Add(new Line(pt, closestP).ToNurbsCurve());
                        }

                    }
                }

                //Wall to Wall
                List<Curve> ww = new List<Curve>();
                for (int i = 0; i < agOut.CoreProperties.Count; i++)
                {
                    if (agOut.CoreProperties[i].Count > 1)
                    {
                        List<Point3d> outlinePoints = new List<Point3d>();

                        Point3d pt = new Point3d(agOut.CoreProperties[i][0].Origin);
                        Vector3d x = new Vector3d(agOut.CoreProperties[i][0].XDirection);
                        Vector3d y = new Vector3d(agOut.CoreProperties[i][0].YDirection);
                        double width = agOut.CoreProperties[i][0].CoreType.GetWidth();
                        double depth = agOut.CoreProperties[i][0].CoreType.GetDepth();

                        pt.Transform(Transform.Translation(Vector3d.Multiply(x, width)));
                        pt.Transform(Transform.Translation(Vector3d.Multiply(y, depth / 2)));

                        double param;
                        coreOutline[i][1].ClosestPoint(pt, out param);
                        Point3d testP = coreOutline[i][1].PointAt(param);
                        ww.Add(new Line(pt, testP).ToNurbsCurve());
                    }
                }

                //output - test
                List<FloorPlan.Dimension> output = new List<FloorPlan.Dimension>();

                List<double> llDist = ll.Select(n => n.GetLength()).ToList();
                List<double> lwDist = lw.Select(n => n.GetLength()).ToList();
                List<double> wwDist = ww.Select(n => n.GetLength()).ToList();

                Curve llMin;
                Curve lwMin;
                Curve wwMin;

                if (llDist.Count != 0)
                {
                    llMin = ll[llDist.IndexOf(llDist.Min())];
                    output.Add(dimMaker(llMin, "ll"));
                }

                if (llDist.Count != 0)
                {
                    lwMin = lw[lwDist.IndexOf(lwDist.Min())];
                    output.Add(dimMaker(lwMin, "lw"));
                }

                if (wwDist.Count != 0)
                {
                    wwMin = ww[wwDist.IndexOf(wwDist.Min())];
                    output.Add(dimMaker(wwMin, "ww"));
                }
                return output;
            }
            else if (agOut.AGtype == "PT-3")
            {
                List<FloorPlan.Dimension> output = new List<FloorPlan.Dimension>();

                Curve[] segs = buildingOutline[0][0].DuplicateSegments();
                Curve side = segs[0].DuplicateCurve();
                Vector3d moveVec = side.TangentAtStart;
                moveVec.Rotate(-Math.PI / 2, Vector3d.ZAxis);
                double moveLen = segs[1].GetLength() / 2;
                side.Translate(moveVec * moveLen);

                output.Add(dimMaker(side, "ll"));

                double width = agOut.CoreProperties[0][0].CoreType.GetWidth();
                double depth = agOut.CoreProperties[0][0].CoreType.GetDepth();

                Curve guide = segs[0].DuplicateCurve();
                string newGuideStr = "lw";
                Point3d sP = guide.PointAtStart;
                Point3d eP = guide.PointAtEnd;
                Vector3d vec = new Vector3d(eP - sP);
                vec.Unitize();
                sP.Transform(Transform.Translation(vec * width));
                if (agOut.CoreProperties[0].Count == 4)
                {
                    eP.Transform(Transform.Translation(-vec * width));
                    newGuideStr = "ww";
                }

                Curve newGuide = new Line(sP, eP).ToNurbsCurve();
                newGuide.Translate(moveVec * depth / 2);

                output.Add(dimMaker(newGuide, newGuideStr));

                return output;
            }
            else
            {
                List<FloorPlan.Dimension> output = new List<FloorPlan.Dimension>();

                Vector3d vec1 = new Vector3d(aptlines[0].PointAtEnd - aptlines[0].PointAtStart);
                Vector3d vec2 = new Vector3d(aptlines[2].PointAtEnd - aptlines[2].PointAtStart);
                if (Vector3d.Multiply(vec1, vec2) < 0)
                {
                    //Lighting to Lighting
                    List<double> aptlineL = new List<double>();
                    aptlineL.Add(aptlines[0].GetLength());
                    aptlineL.Add(aptlines[2].GetLength());
                    Curve shorter = aptlines[aptlineL.IndexOf(aptlineL.Min()) * 2].DuplicateCurve();
                    Curve longer = aptlines[aptlineL.IndexOf(aptlineL.Max()) * 2].DuplicateCurve();
                    Point3d sP = shorter.PointAtLength(shorter.GetLength() / 2);
                    double param;
                    longer.ClosestPoint(sP, out param);
                    Point3d eP = longer.PointAt(param);
                    Vector3d sPvec = new Vector3d(eP - sP);
                    sPvec.Unitize();
                    sP.Transform(Transform.Translation(sPvec * aptWidth / 2));
                    eP.Transform(Transform.Translation(-sPvec * aptWidth / 2));

                    Curve guide = new Line(sP, eP).ToNurbsCurve();

                    output.Add(dimMaker(guide, "ll"));
                }

                return output;
            }
        }

        private FloorPlan.Dimension dimMaker(Curve c, string str)
        {
            List<Point3d> cP = new List<Point3d>();
            cP.Add(c.PointAtStart);
            cP.Add(c.PointAtEnd);
            Vector3d cVec = c.TangentAtStart;
            cVec.Rotate(Math.PI / 2, Vector3d.ZAxis);
            Point3d cSide = new Point3d(c.PointAtStart);
            cSide.Transform(Transform.Translation(cVec));
            FloorPlan.Dimension cDim = new FloorPlan.Dimension(cP, cSide, 2000, str);

            return cDim;
        }


    }
}
