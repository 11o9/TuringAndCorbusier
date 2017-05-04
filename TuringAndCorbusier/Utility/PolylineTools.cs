using System;
using System.Collections.Generic;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;
using System.Linq;


namespace TuringAndCorbusier.Utility
{
    class PolylineTools
    {
        public static double GetArea(Polyline input)
        {
            if (!input.IsClosed)
                return 0;

            List<Point3d> y = new List<Point3d>(input);
            double area = 0;

            for (int i = 0; i < y.Count - 1; i++)
            {
                area += y[i].X * y[i + 1].Y;
                area -= y[i].Y * y[i + 1].X;
            }

            area /= 2;
            return (area < 0 ? -area : area);
        }

        /// <summary>
        /// 닫힌 폴리라인 방향을 반시계 방향으로 만들어줍니다. (꼭지점의 index 순서 기준)
        /// </summary>
        public static Polyline AlignPolyline(Polyline polyline)
        {
            Polyline output = new Polyline(polyline);
            if (output.ToNurbsCurve().ClosedCurveOrientation(Vector3d.ZAxis) == CurveOrientation.CounterClockwise)
            {
                output.Reverse();
            }
            return output;
        }

        public static List<Point3d> GetVertex(Polyline polyline)
        {
            List<Point3d> tempVertex = new List<Point3d>(polyline);
            tempVertex.RemoveAt(tempVertex.Count - 1);

            return tempVertex;
        }

        public static void AlignCCW(Polyline polyline)
        {
            if (polyline.ToNurbsCurve().ClosedCurveOrientation(Vector3d.ZAxis) == CurveOrientation.CounterClockwise)
                polyline.Reverse();

            return;
        }

        public static void AlignCW(Polyline polyline)
        {
            if (polyline.ToNurbsCurve().ClosedCurveOrientation(Vector3d.ZAxis) != CurveOrientation.CounterClockwise)
                polyline.Reverse();

            return;
        }

        /// <summary>
        /// 폴리라인의 각 세그먼트들에 대해 평행한 또는 안쪽으로 수직인 벡터 리스트를 구해줍니다.(반시계방향 기준)
        /// </summary>
        public class SegmentVector
        {
            public static List<Vector3d> GetAlign(Polyline polyline, bool unitize)
            {
                Polyline ccwAlign = AlignPolyline(polyline);
                List<Point3d> tempVertex = new List<Point3d>(ccwAlign);
                tempVertex.RemoveAt(tempVertex.Count() - 1);
                List<Vector3d> alignVector = new List<Vector3d>();
                int numVertex = tempVertex.Count;
                for (int i = 0; i < numVertex; i++)
                {
                    Point3d tempStart = tempVertex[i];
                    Point3d tempEnd = tempVertex[(numVertex + i + 1) % numVertex];
                    Vector3d tempVector = new Vector3d(tempEnd - tempStart); // Align Vector with length

                    if (unitize)
                        tempVector = tempVector / tempVector.Length;

                    alignVector.Add(tempVector);
                }
                return alignVector;
            }

            public static List<Vector3d> GetPerpendicular(Polyline polyline, bool unitize)
            {
                Polyline ccwAlign = AlignPolyline(polyline);
                List<Point3d> tempVertex = new List<Point3d>(ccwAlign);
                tempVertex.RemoveAt(tempVertex.Count() - 1);
                List<Vector3d> perpVector = new List<Vector3d>();
                int numVertex = tempVertex.Count;
                for (int i = 0; i < numVertex; i++)
                {
                    Point3d tempStart = tempVertex[i];
                    Point3d tempEnd = tempVertex[(numVertex + i + 1) % numVertex];
                    Vector3d tempVector = new Vector3d(tempEnd - tempStart); // Align Vector with length
                    tempVector.Transform(Transform.Rotation(Math.PI / 2, tempStart));

                    if (unitize)
                        tempVector = tempVector / tempVector.Length;

                    perpVector.Add(tempVector);
                }
                return perpVector;
            }
        }


        /// <summary>
        /// 폴리라인의 각 변마다 거리를 지정해 Offset 커브를 만들어줍니다.
        /// </summary>
        public static List<Polyline> ImprovedOffset(Polyline bound, List<double> offsetDist)
        {
            Polyline ccwBound = AlignPolyline(bound);
            List<Point3d> trimmedOffsetPt = new List<Point3d>();

            //set vectors
            List<Vector3d> alignVector = SegmentVector.GetAlign(ccwBound, true);
            List<Vector3d> perpVector = SegmentVector.GetPerpendicular(ccwBound, true);
            List<Point3d> boundVertex = GetVertex(ccwBound);

            int numSegment = alignVector.Count;
            int numVertex = boundVertex.Count;


            //offset and trim segments
            for (int i = 0; i < numSegment; i++)
            {
                double a = offsetDist[i];
                double b = offsetDist[(i + 1) % offsetDist.Count];
                double dotProduct = Vector3d.Multiply(alignVector[i], alignVector[(i + 1) % numSegment]);
                Vector3d crossProduct = Vector3d.CrossProduct(alignVector[i], alignVector[(i + 1) % numSegment]);
                double cos = Math.Abs(dotProduct / (alignVector[i].Length * alignVector[(i + 1) % numSegment].Length));
                double sin = Math.Sqrt(1 - Math.Pow(cos, 2));

                double decider1 = Vector3d.Multiply(Plane.WorldXY.ZAxis, crossProduct);
                double decider2 = Vector3d.Multiply(-alignVector[i], alignVector[(i + 1) % numSegment]);

                Point3d tempPt = new Point3d();

                if (decider1 > 0.005) // concave
                {
                    if (decider2 < 0) // blunt
                        tempPt = boundVertex[(i + 1) % numVertex] + perpVector[i] * a + alignVector[i] * ((a * cos - b) / sin);
                    else // acute (right angle included)
                        tempPt = boundVertex[(i + 1) % numVertex] + perpVector[i] * a + alignVector[i] * ((-a * cos - b) / sin);
                }

                else if (decider1 < -0.005) // convex
                {
                    if (decider2 < 0) //blunt
                        tempPt = boundVertex[(i + 1) % numVertex] + perpVector[i] * a + alignVector[i] * ((-a * cos + b) / sin);
                    else // acute (right angle included)
                        tempPt = boundVertex[(i + 1) % numVertex] + perpVector[i] * a + alignVector[i] * ((a * cos + b) / sin);
                }

                else //straight & near straight
                    tempPt = boundVertex[(i + 1) % numVertex] + perpVector[i] * Math.Max(a, b);

                trimmedOffsetPt.Add(tempPt);
            }

            trimmedOffsetPt.Add(trimmedOffsetPt[0]);
            Polyline offBound = new Polyline(trimmedOffsetPt);

            //remove loop
            List<Polyline> loopOut = RemoveLoop(offBound, true);

            if (loopOut.Count > 0)
                return loopOut;

            return new List<Polyline>();
        }

        public static Polyline ChangeCoordinate(Polyline basePoly, Plane fromPln, Plane toPln)
        {
            Polyline changedPoly = new Polyline(basePoly);
            changedPoly.Transform(Transform.ChangeBasis(fromPln, toPln));

            return changedPoly;
        }

        public static bool IsOverlap(Polyline poly1, Polyline poly2)
        {
            Curve polyCrv1 = poly1.ToNurbsCurve();
            Curve polyCrv2 = poly2.ToNurbsCurve();

            var polyIntersection = Rhino.Geometry.Intersect.Intersection.CurveCurve(polyCrv1, polyCrv2, 0, 0);
            foreach (var i in polyIntersection)
            {
                if (i.IsOverlap)
                    return true;
            }

            return false;
        }

        public class FeaturedCenter
        {
            /// <summary>
            /// 주어진 폴리라인 내부에 있는 중심점을 찾아줍니다.
            /// </summary>
            /// <param name="resolution"> 폴리라인 분할 셀 크기 제한</param>
            /// <returns></returns>
            public static Point3d FindFeaturedCenter(Polyline poly, double resolution)
            {
                Point3d output = new Point3d();

                //get boundingBox
                BoundingBox polyBounding = new BoundingBox(poly);
                Point3d minPt = polyBounding.Min;
                Point3d maxPt = polyBounding.Max;

                //set cellSize
                double width = maxPt.X - minPt.X;
                double height = maxPt.Y - minPt.Y;
                double cellSize = Math.Min(width, height) / 2.0;

                //seive1
                if (cellSize == 0)
                    return minPt;

                //
                Queue<Cell> centerCandidate = new Queue<Cell>();

                //cover poly with initial cells
                for (double x = minPt.X; x < maxPt.X; x += cellSize)
                {
                    for (double y = minPt.Y; y < maxPt.Y; y += cellSize)
                        centerCandidate.Enqueue(new Cell(new Point3d(x + cellSize / 2.0, y + cellSize / 2.0, minPt.Z), cellSize, poly));
                }

                //get initial best when poly is rect
                Cell bestCell = new Cell(polyBounding.Center, 0, poly);

                while (centerCandidate.Count != 0)
                {
                    Cell currentCell = centerCandidate.Dequeue();

                    if (currentCell.DistToPoly > bestCell.DistToPoly)
                        bestCell = currentCell;

                    if (currentCell.CellSize <= resolution)
                        continue;

                    cellSize /= 2.0;
                    Point3d newPt1 = new Point3d(currentCell.X - cellSize, currentCell.Y - cellSize, currentCell.Z);
                    Point3d newPt2 = new Point3d(currentCell.X + cellSize, currentCell.Y - cellSize, currentCell.Z);
                    Point3d newPt3 = new Point3d(currentCell.X - cellSize, currentCell.Y + cellSize, currentCell.Z);
                    Point3d newPt4 = new Point3d(currentCell.X + cellSize, currentCell.Y + cellSize, currentCell.Z);

                    centerCandidate.Enqueue(new Cell(newPt1, cellSize, poly));
                    centerCandidate.Enqueue(new Cell(newPt2, cellSize, poly));
                    centerCandidate.Enqueue(new Cell(newPt3, cellSize, poly));
                    centerCandidate.Enqueue(new Cell(newPt4, cellSize, poly));
                }

                output = bestCell.CenterPt;
                return output;
            }

            class Cell
            {
                public Point3d CenterPt { get; set; }
                public double X { get; set; }
                public double Y { get; set; }
                public double Z { get; set; }
                public double CellSize { get; set; }
                public double DistToPoly { get; set; }

                public Cell(Point3d basePt, double cellSize, Polyline poly)
                {
                    this.CenterPt = basePt;
                    this.X = basePt.X;
                    this.Y = basePt.Y;
                    this.CellSize = cellSize;
                    this.DistToPoly = GetDistToPoly(basePt, poly);
                }
            }

            //get distance to polyline, negative if point is outside
            private static double GetDistToPoly(Point3d basePt, Polyline poly)
            {
                Point3d closestPt = poly.ClosestPoint(basePt);
                bool isInside = poly.ToNurbsCurve().Contains(basePt) == PointContainment.Inside;

                return (isInside ? 1 : -1) * basePt.DistanceTo(closestPt);
            }

        }

        public class Divider
        {
            public static List<Polyline> ByPolyLine(Polyline divided, Polyline divider)
            {
                double deciderStartParam = divided.ClosestParameter(divider.First);
                double deciderEndParam = divided.ClosestParameter(divider.Last);

                if (deciderEndParam < deciderStartParam)
                {
                    divider.Reverse();
                    double tempParamForExchange = deciderStartParam;
                    deciderStartParam = deciderEndParam;
                    deciderEndParam = tempParamForExchange;

                }

                List<Polyline> dividedPoly = StigmatizeInnocents(divided, divider, deciderStartParam, deciderEndParam);
                return dividedPoly;
            }

            public static List<Polyline> ByLine(Polyline divided, Line divider)
            {
                Polyline dividerPoly = new Polyline { divider.From, divider.To };
                return ByPolyLine(divided, dividerPoly);
            }

            private static List<Polyline> StigmatizeInnocents(Polyline divided, Polyline divider, double startParam, double endParam)
            {
                List<Point3d> sinisterPtLow = new List<Point3d>();
                List<Point3d> sinisterPtHigh = new List<Point3d>();
                List<Point3d> dexterPt = new List<Point3d>();

                int vertexCount = divided.Count;

                for (int i = 0; i < vertexCount - 1; i++)
                {
                    if (i == startParam || i == endParam)
                        continue;

                    if (i > startParam && i < endParam)
                        dexterPt.Add(divided[i]);

                    else
                    {
                        if (i < startParam)
                            sinisterPtLow.Add(divided[i]);
                        else
                            sinisterPtHigh.Add(divided[i]);
                    }
                }

                List<Polyline> dividedPoly = new List<Polyline>();
                dividedPoly.Add(MakeDexterPoly(divider, dexterPt));
                dividedPoly.Add(MakeSinisterPoly(divider, sinisterPtLow, sinisterPtHigh));

                return dividedPoly;
            }

            private static Polyline MakeDexterPoly(Polyline divider, List<Point3d> dexterPt)
            {
                List<Point3d> dexterPolyVertices = new List<Point3d>();
                List<Point3d> dividerPt = new List<Point3d>(divider);

                dexterPolyVertices.AddRange(dexterPt);
                dividerPt.Reverse();
                dexterPolyVertices.AddRange(dividerPt);

                dexterPolyVertices.Add(dexterPolyVertices.First());

                return new Polyline(dexterPolyVertices);
            }

            private static Polyline MakeSinisterPoly(Polyline divider, List<Point3d> sinisterPtLow, List<Point3d> sinisterPtHigh)
            {
                List<Point3d> sinisterPolyVertices = new List<Point3d>();
                List<Point3d> dividerPt = new List<Point3d>(divider);

                sinisterPolyVertices.AddRange(sinisterPtLow);
                sinisterPolyVertices.AddRange(dividerPt);
                sinisterPolyVertices.AddRange(sinisterPtHigh);

                sinisterPolyVertices.Add(sinisterPolyVertices.First());

                return new Polyline(sinisterPolyVertices);
            }
        }


        public static void RemoveOnStraightPt(Polyline poly, double straightTolerance)
        {
            List<Point3d> removed = new List<Point3d>();

            bool isClosed = poly.IsClosed;

            if (isClosed)
                poly.RemoveAt(poly.Count - 1);

            int pointCount = poly.Count;

            for (int i = 0; i < pointCount; i++)
            {
                if (i == 0 && !isClosed)
                {
                    removed.Add(poly[i]);
                    continue;
                }

                Vector3d preVector = poly[(pointCount + i - 1) % pointCount] - poly[i];
                Vector3d postVector = poly[(pointCount + i + 1) % pointCount] - poly[i];

                if (preVector.Length < 0.005)
                    continue;

                if (i == pointCount - 1 && !isClosed)
                {
                    removed.Add(poly[i]);
                    break;
                }

                var tempConvexity = VectorTools.CheckConvexity(preVector, postVector, straightTolerance);
                if ((tempConvexity == Convexity.Convex || tempConvexity == Convexity.Concave)||postVector.Length < 0.005)
                    removed.Add(poly[i]);
            }

            poly.Clear();
            poly.AddRange(removed);
            if (isClosed)
                poly.Add(poly.First);
        }

        public static List<Polyline> RemoveLoop(Polyline looped, bool isCCW)
        {
            Curve loopedCrv = looped.ToNurbsCurve();

            //Search selfX params.
            List<double> selfXParams = new List<double>();

            var selfIntersect = Intersection.CurveSelf(loopedCrv, 0);
            foreach(IntersectionEvent e in selfIntersect)
            {
                selfXParams.Add(e.ParameterA);
                selfXParams.Add(e.ParameterB);
            }

            //Set trimming intervals.
            List<double> trimmingParams = new List<double>();
            for (int i = 0; i < looped.Count; i++)
                trimmingParams.Add(i);
            trimmingParams.AddRange(selfXParams);
            trimmingParams.Sort();

            List<Interval> trimmingInterval = new List<Interval>();
            int paramCount = trimmingParams.Count;
            for (int i = 0; i < paramCount-1; i++)
            {
                trimmingInterval.Add(
                    new Interval(trimmingParams[i], trimmingParams[(paramCount + i + 1) % paramCount]));
            }

            //Get segments.
            List<LineCurve> trimmedSegs = new List<LineCurve>();
            foreach (Interval i in trimmingInterval)
            {
                trimmedSegs.Add(
                    new LineCurve(looped.PointAt(i.T0), looped.PointAt(i.T1)));
            }

            //Divide segments into in/out group.
            List<LineCurve> innerSegs = new List<LineCurve>();
            List<LineCurve> outerSegs = new List<LineCurve>();
            foreach (LineCurve i in trimmedSegs)
            {
                Vector3d toInnerCCW = i.TangentAtStart;
                toInnerCCW.Rotate(Math.PI / 2, Vector3d.ZAxis);
                Point3d inOutTestPoint = (i.PointAtStart+i.PointAtEnd)/2 + toInnerCCW;

                PointContainment inOutResult = loopedCrv.Contains(inOutTestPoint);
                if (inOutResult == PointContainment.Inside)
                    innerSegs.Add(i);
                else
                    outerSegs.Add(i);
            }

            //Decided output by curve orientation.
            List<LineCurve> outputSegs;
            if (isCCW)
                outputSegs = innerSegs;
            else
                outputSegs = outerSegs;

            if (outputSegs.Count > 0)
            {
                Curve[] outputCrv = Curve.JoinCurves(outputSegs);
                return outputCrv.Select(n => CurveTools.ToPolyline(n)).ToList();
            }

            return new List<Polyline>();
        }
    }
}
