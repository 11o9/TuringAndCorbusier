using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rhino.Geometry;
using Rhino.Collections;

namespace TuringAndCorbusier.cs
{
    class RegulationChecker
    {
        //Constructor, 생성자

        public RegulationChecker(ApartmentGeneratorOutput agOut)
        {
            int storiesHigh = (int)Math.Max(agOut.ParameterSet.Parameters[0], agOut.ParameterSet.Parameters[1]);
            int storiesLow = (int)Math.Min(agOut.ParameterSet.Parameters[0], agOut.ParameterSet.Parameters[1]);
            this.FromSurroundings = fromSurroundingsRegulation(agOut);
            this.FromNorthHigh = fromNorthRegulation(agOut, storiesHigh);
            this.FromNorthLow = fromNorthRegulation(agOut, storiesLow);
            this.BuildingDistancesLL = buildingDistanceRegulation(agOut, "LL");
            this.BuildingDistancesLW = buildingDistanceRegulation(agOut, "LW");
            this.BuildingDistancesWW = buildingDistanceRegulation(agOut, "WW");
        }

        //Property, 속성
        public Curve FromSurroundings { get; private set; }
        public Curve FromNorthHigh { get; private set; }
        public Curve FromNorthLow { get; private set; }
        public List<Curve> BuildingDistancesLL { get; private set; }
        public List<Curve> BuildingDistancesLW { get; private set; }
        public List<Curve> BuildingDistancesWW { get; private set; }

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

        private List<Curve> buildingDistanceRegulation(ApartmentGeneratorOutput agOut, string str)//LL : lighting lighting, LW : lighting wall, WW : wall wall
        {
            Regulation regulation = new Regulation(agOut.ParameterSet.Stories);
            double offsetDistance;
            if (str == "LL")
                offsetDistance = regulation.DistanceLL;
            else if (str == "LW")
                offsetDistance = regulation.DistanceLW;
            else if (str == "WW")
                offsetDistance = regulation.DistanceWW;
            else
                return new List<Curve>();

            List<List<Curve>> outlines = new List<List<Curve>>(agOut.buildingOutline);
            List<Curve> output = new List<Curve>();
            foreach (List<Curve> lc in outlines)
            {
                foreach (Curve c in lc)
                {
                    output.Add(c.Offset(Plane.WorldXY, offsetDistance, 0, CurveOffsetCornerStyle.Sharp)[0]);
                }
            }
            return output;
        }

    }
}
