using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rhino;
using Rhino.Geometry;

namespace TuringAndCorbusier
{
    class RetreatBuildingLine
    {
        public void RetreatBuildingLineUtil(bool isExpendable, List<double> roadWidth, Point3d landCentr, List<Line> BLine, out List<double> expendDistance, out List<Vector3d> retreatDir)
        {
            expendDistance = new List<double>();
            retreatDir = new List<Vector3d>();
            List<Point3d> closestPntList = new List<Point3d>();
            List<Vector3d> vecList = new List<Vector3d>();
            for (int i = 0; i < roadWidth.Count; i++)
            {
                if (roadWidth[i] < 4000 && roadWidth[i] >= 2000)
                {
                    if (isExpendable == true)
                    {
                        double roadExpension = (4000 - roadWidth[i]) / 2000;
                        expendDistance.Add(roadExpension);
                    }
                    else
                    {
                        double roadExpension = (4000 - roadWidth[i]);
                        expendDistance.Add(roadExpension);
                    }
                }
                else
                {
                    expendDistance.Add(0);
                }
                Point3d cp = BLine[i].ClosestPoint(landCentr, false);
                Vector3d v = new Vector3d(landCentr - cp);
                vecList.Add(v);
                closestPntList.Add(cp);
            }
            for (int i = 0; i < closestPntList.Count; i++)
            {
                Line l = new Line(closestPntList[i], vecList[i], expendDistance[i]);
                Vector3d v = l.Direction;
                retreatDir.Add(v);
            }
        }

        public List<Line> RetrieveRoad(List<Line> buildingLines, List<Vector3d> retreatDir)
        {
            for (int i = 0; i < buildingLines.Count; i++)
            {
                Line temp = new Line(buildingLines[i].From, buildingLines[i].To);
                temp.Transform(Transform.Translation(retreatDir[i]));
                buildingLines[i] = temp;
            }
            return buildingLines;
        }

        //public List<Line> NewBuildingLineConversion(List<Line> buildingLine)
        //{
        //    List<Point3d> newBldLinePoints = new List<Point3d>();
        //    for (int i = 0; i < buildingLine.Count; i++)
        //    {
        //        double a, b;
        //        Line l1 = buildingLine[i];
        //        Line l2 = buildingLine[(i + 1) % buildingLine.Count];
        //        Rhino.Geometry.Intersect.Intersection.LineLine(l1, l2, out a, out b, 0, false);
        //        newBldLinePoints.Add(l1.PointAt(a));
        //    }
        //    newBldLinePoints.Add(newBldLinePoints[0]);

        //    Curve retreatedBldLine = new Polyline(newBldLinePoints).ToNurbsCurve();
        //    Curve[] segments = retreatedBldLine.DuplicateSegments();
        //    Gagak gagak = new Gagak();
        //    List<Line> newBldLines = gagak.LineConversion(segments);
        //    return newBldLines;
        //}
    }
}
