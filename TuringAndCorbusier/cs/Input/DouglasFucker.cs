using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rhino;
using Rhino.Geometry;

namespace TuringAndCorbusier
{
    class DouglasFucker
    {
        public DouglasFucker() { }


        //내각 찾기
        public List<double> GetBoundaryInnerAngle(ref Curve boundary, out List<bool> finalIsConvex)
        {
            if (boundary.ClosedCurveOrientation(Plane.WorldXY) == CurveOrientation.CounterClockwise)
                boundary.Reverse();

            List<Curve> boundarySegmentList = new List<Curve>();
            boundarySegmentList = boundary.DuplicateSegments().ToList();
            List<double> radianList = new List<double>();
            List<bool> testing = new List<bool>();
            for (int i = 0; i < boundarySegmentList.Count; i++)
            {
                Curve c1 = boundarySegmentList[i];
                Curve c2 = boundarySegmentList[(i + 1) % boundarySegmentList.Count];
                Vector3d v1 = c1.TangentAtEnd;
                Vector3d v2 = c2.TangentAtStart;
                double radian = Vector3d.VectorAngle(v2, v1);
                Vector3d test = Vector3d.CrossProduct(v1, v2);
                if (test.Z < 0)
                {
                    testing.Add(false);
                }
                else
                {
                    testing.Add(true);
                }
                radianList.Add(180 - RhinoMath.ToDegrees(radian));
            }
            List<double> finalRadianList = new List<double>();
            for (int i = 0; i < radianList.Count; i++)
            {
                finalRadianList.Add(radianList[(i + radianList.Count - 1) % radianList.Count]);
            }

            finalIsConvex = new List<bool>();
            for (int i = 0; i < testing.Count; i++)
            {
                finalIsConvex.Add(testing[(i + testing.Count - 1) % testing.Count]);
            }

            return finalRadianList;
        }
        //대지 경계선 단순화
        public Curve Simplification(Curve shape, int epsilon)
        {
            if (shape.ClosedCurveOrientation(Vector3d.ZAxis) == CurveOrientation.CounterClockwise)
            {
                shape.Reverse();
            }
            Curve[] shapeSegments = shape.DuplicateSegments();
            List<Line> shapeLineSegs = LineConversion(shapeSegments);

            List<Point3d> finalPoints = new List<Point3d>();
            for (int i = 0; i < shapeLineSegs.Count; i++)
            {

                 finalPoints.Add(shapeLineSegs[i].From);

            }
            finalPoints.Add(shapeLineSegs[shapeLineSegs.Count - 1].To);

            List<Point3d> simPoints = Douglas(finalPoints, epsilon);
            Curve finalResult = new Polyline(simPoints).ToNurbsCurve();

            return finalResult;
        }//Simplification method

        //커브 라인으로 변환 (옛날에 만든코드)
        public List<Line> LineConversion(Curve[] segments)
        {
            List<Line> shapeLineSeg = new List<Line>();
            for (int i = 0; i < segments.Length; i++)
            {
                Line line = new Line(segments[i].PointAtStart, segments[i].PointAtEnd);
                shapeLineSeg.Add(line);
            }
            return shapeLineSeg;
        }//line converson method

        //포인트 단순화
        public List<Point3d> Douglas(List<Point3d> validPoints, double epsilon)
        {
            double maxDistance = 0;
            int index = 0;
            int end = validPoints.Count;
            //Douglas fucker 알고리즘
            for (int i = 0; i < end; i++)
            {
                Point3d p1 = new Line(validPoints[0], validPoints[end - 1]).ClosestPoint(validPoints[i], false);
                double distance = new Line(p1, validPoints[i]).Length;

                if (distance > maxDistance)
                {
                    index = i;
                    maxDistance = distance;
                }
            }
            List<Point3d> finalResult = new List<Point3d>();

            if (maxDistance > epsilon)
            {
                List<Point3d> result1 = new List<Point3d>();
                List<Point3d> vp1 = new List<Point3d>();
                for (int i = 0; i < index + 1; i++)
                {
                    vp1.Add(validPoints[i]);
                }
                List<Point3d> result2 = new List<Point3d>();
                List<Point3d> vp2 = new List<Point3d>();
                for (int i = index + 1; i < end; i++)
                {
                    vp2.Add(validPoints[i]);
                }

                result1 = Douglas(vp1, epsilon);
                result2 = Douglas(vp2, epsilon);

                finalResult.AddRange(result1);
                finalResult.AddRange(result2);
            }
            else
            {
                finalResult.Add(validPoints[0]);
                finalResult.Add(validPoints[end - 1]);
            }
            return finalResult;
        }//Douglas method
    }
}
