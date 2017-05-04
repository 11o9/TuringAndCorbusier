using System;
using System.Collections.Generic;
using Rhino.Geometry;
using System.Linq;
using Rhino.Display;
using System.IO;

namespace TuringAndCorbusier.Utility
{
    class PCXTools
    {
        /// <summary>
        /// 주어진 시작점과 방향을 따라 경계에 닿는 최단선분을 반환합니다.
        /// </summary>
        public static Line PCXByEquation(Point3d basePt, Polyline boundary, Vector3d direction)
        {
            double onCurveTolerance = 0.5;
            List<Point3d> crossPtCandidate = GetAllCrossPoints(basePt, boundary, direction, onCurveTolerance);

            if (crossPtCandidate.Count ==0)
                return new Line(basePt,basePt);

            crossPtCandidate.Sort((a, b) => (basePt.DistanceTo(a).CompareTo(basePt.DistanceTo(b))));
            return new Line(basePt, crossPtCandidate[0]);
        }


        /// <summary>
        /// 주어진 시작점과 방향을 따라 경계에 닿는 최단선분을 반환합니다. 같은 점일 경우를 제외 합니다. 결과가 자기 자신 뿐일 때 같은 점을 반환합니다.
        /// </summary>
        public static Line PCXStrict(Point3d basePt, Polyline boundary, Vector3d direction)
        {
            double onCurveTolerance = 0.5;
            double samePtTolerance = 0.005;

            List<Point3d> allCrossPts = GetAllCrossPoints(basePt, boundary, direction, onCurveTolerance);
            allCrossPts.Sort((a, b) => (basePt.DistanceTo(a).CompareTo(basePt.DistanceTo(b))));

            List<Point3d> notItselfPts = new List<Point3d>();
            foreach(Point3d i in allCrossPts)
            {
                if (i.DistanceTo(basePt) > samePtTolerance)
                    notItselfPts.Add(i);
            }

            if (notItselfPts.Count == 0)
                return new Line(basePt,basePt);

            return new Line(basePt, notItselfPts[0]);
        }

        /// <summary>
        /// 반시계 방향 기준으로 경계 안에 그릴 수 있는 최장의 선을 반환합니다. 기준점이 경계를 벗어나는 경우는 PCXStrict와 동일합니다.
        /// </summary>
        public static Line PCXLongest(Point3d basePt, Polyline boundary, Vector3d direction)
        {
            double onCurveTolerance = 0.5;
            double samePtTolerance = 0.005;
            double parallelTolerance = 0.005;
            Point3d longestEnd = basePt;


            //Get all crossing points.
            List<Point3d> allCrossPts = GetAllCrossPoints(basePt, boundary, direction, onCurveTolerance);
            allCrossPts.Sort((a, b) => (basePt.DistanceTo(a).CompareTo(basePt.DistanceTo(b))));

            //Set compare points.
            List<Point3d> boundaryPt = new List<Point3d>(boundary);
            if (boundary.IsClosed)
                boundaryPt.RemoveAt(boundaryPt.Count - 1);


            //Find end point of longest line.
            foreach (Point3d i in allCrossPts)
            {
                longestEnd = i;

                //seive1: Remove same as basePt.
                if (i.DistanceTo(basePt) < samePtTolerance)
                    continue;


                //seive2: End if not vertex.
                int vertexIndex = boundaryPt.FindIndex(n => n.DistanceTo(i) < samePtTolerance);
                bool isVertex = vertexIndex != -1;

                if (!isVertex)
                    break;


                //seive3: End if not concave(& anti-parallel).
                Vector3d toPre = boundaryPt[(boundaryPt.Count + vertexIndex - 1) % boundaryPt.Count] - boundaryPt[vertexIndex];
                Vector3d toPost = boundaryPt[(boundaryPt.Count + vertexIndex + 1) % boundaryPt.Count] - boundaryPt[vertexIndex];
                Convexity cnv = VectorTools.CheckConvexity(toPre, toPost, parallelTolerance);

                if (cnv == Convexity.Convex || cnv == Convexity.Parallel)
                    break;


                //seive4: Continue if not between.
                if (!VectorTools.IsBetweenVector(toPre, toPost, -direction))
                    continue;


                //seive5: End if pre or post is not parallel to direction.
                bool isPreDirParallel = Math.Abs(toPre * direction / (toPre.Length * direction.Length)) > 1 - parallelTolerance;
                bool isPostDirParallel = Math.Abs(toPost * direction / (toPost.Length * direction.Length)) > 1 - parallelTolerance;

                if (isPreDirParallel || isPostDirParallel)
                    continue;


                //seive6: Continue if passable.
                Vector3d perpToDir = direction;
                perpToDir.Rotate(Math.PI / 2, Vector3d.ZAxis);

                double preDirDot = toPre * perpToDir;
                double postDirDot = toPost * perpToDir;

                if (preDirDot * postDirDot > 0)
                    continue;

                break;
            }

            return new Line(basePt, longestEnd);
        }

        public static List<Point3d> GetAllCrossPoints(Point3d basePt, Polyline boundary, Vector3d direction, double onTolerance)
        {
            List<Line> boundarySeg = boundary.GetSegments().ToList();
            List<Point3d> crossPts = new List<Point3d>();

            //testline setting
            double coverAllLength = new BoundingBox(boundary).Diagonal.Length*2;
            Line testLine = new Line(basePt, basePt + direction / direction.Length* coverAllLength);
            double xHi1 = Math.Max(testLine.FromX, testLine.ToX);
            double xLo1 = Math.Min(testLine.FromX, testLine.ToX);
            double yHi1 = Math.Max(testLine.FromY, testLine.ToY);
            double yLo1 = Math.Min(testLine.FromY, testLine.ToY);

            foreach (Line i in boundarySeg)
            {
                //seive1: xRange
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

                //seive2: yRange
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

                Point3d tempCrossPt = CCXTools.GetCrossPt(testLine, i);

                if (IsPtOnLine(tempCrossPt, testLine, onTolerance)&& IsPtOnLine(tempCrossPt, i, onTolerance))
                    crossPts.Add(tempCrossPt);
            }

            return crossPts;
        }

        public static bool IsPtOnLine(Point3d testPt, Line testLine, double tolerance)
        {
            if (testPt == Point3d.Unset)
                return false;

            List<Point3d> linePtList = new List<Point3d>();
            linePtList.Add(testLine.PointAt(0));
            linePtList.Add(testLine.PointAt(1));


            linePtList.Sort((a, b) => (a.X.CompareTo(b.X)));
            double minX = linePtList.First().X;
            double maxX = linePtList.Last().X;

            linePtList.Sort((a, b) => (a.Y.CompareTo(b.Y)));
            double minY = linePtList.First().Y;
            double maxY = linePtList.Last().Y;

            linePtList.Sort((a, b) => (a.Z.CompareTo(b.Z)));
            double minZ = linePtList.First().Z;
            double maxZ = linePtList.Last().Z;



            //isOnOriginTest
            bool isSatisfyingX = (testPt.X - (minX - tolerance)) * ((maxX + tolerance) - testPt.X) >= 0;
            bool isSatisfyingY = (testPt.Y - (minY - tolerance)) * ((maxY + tolerance) - testPt.Y) >= 0;
            bool isSatisfyingZ = (testPt.Z - (minZ - tolerance)) * ((maxZ + tolerance) - testPt.Z) >= 0;

            if (isSatisfyingX && isSatisfyingY && isSatisfyingZ)
                return true;

            return false;
        }

        public static bool IsPtOnLine2(Point3d testPt, Line testLine, double tolerance)
        {
            Point3d test = testLine.ClosestPoint(testPt, true);
            bool isOn = test.DistanceTo(testPt) < tolerance;

            if (isOn)
                return true;

            return false;
        }

        /// <summary>
        /// ray-casting algorithm
        /// </summary>
        /// <param name="tolerance">coincidence tolerance</param>
        public static PointContainment IsPtInside(Point3d testPt, Polyline bound, double tolerance)
        {
            Line[] segments = bound.GetSegments();
            int segmentCount = segments.Count();

            int intersections = 0;

            foreach(Line i in segments)
            {
                Interval yDomain = new Interval(i.FromY, i.ToY);
                yDomain.MakeIncreasing();

                if (yDomain.T0- tolerance <= testPt.Y && testPt.Y <= yDomain.T1+ tolerance)
                {
                    double lineSlope = (i.ToY - i.FromY) / (i.ToX - i.FromX);

                    if (lineSlope == 0)
                        return PointContainment.Coincident;

                    double intersectionX = i.ToX + (testPt.Y - i.ToY) / lineSlope;

                    if (testPt.X > intersectionX)
                        intersections++;

                    double testPtToInter = testPt.X - intersectionX;
                    if (Math.Abs(testPtToInter) < tolerance)
                        return PointContainment.Coincident;
                }
            }

            if (intersections % 2 == 0)
                return PointContainment.Outside;

            return PointContainment.Inside;
        }
    }
}
