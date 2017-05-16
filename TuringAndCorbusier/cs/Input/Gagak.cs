using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rhino;
using Rhino.Geometry;

namespace TuringAndCorbusier
{
    public class Gagak
    {
        public List<double> newRoadWidth { get; set; }
        public Curve finalLand { get; set; }
        public double finalArea { get; set; }

        public void RefineEdge(Curve land,List<double> roadWidth)
        {
            //대지 중심
            Point3d landCentroid = AreaMassProperties.Compute(land).Centroid;
            Curve[] segments = land.DuplicateSegments();
            List<Line> lineSegments = LineConversion(segments);

            Point3d[] segmentspoints = segments.Select(n => n.PointAtStart).ToArray();
            Point3d[] linepoints = lineSegments.Select(n => n.From).ToArray();

            //건축선 후퇴(막다른길 제외)
            List<double> expendDistance = new List<double>();
            List<Vector3d> retreatDirection = new List<Vector3d>();
            bool isExpendable = true;
            RetreatBuildingLine rbl = new RetreatBuildingLine();
            //후퇴할때 필요한 후퇴 길이와 후퇴방향 구하기
            //rbl.RetreatBuildingLineUtil(isExpendable,roadWidth,landCentroid,lineSegments,out expendDistance,out retreatDirection);
            //후퇴 방향으로 건축선 후퇴하기
            //List<Line> buildingLines = rbl.RetrieveRoad(lineSegments,retreatDirection);
            //후퇴가 있었다면 새로 후퇴된 line segment로 바꾸기
            //List<Line> retreatedBldLines = rbl.NewBuildingLineConversion(buildingLines);

            List<Line> retreatedBldLines = lineSegments;


            //이유는 모르겠지만.. rbl 1씩 밀어봄
            //retreatedBldLines.Insert(0, retreatedBldLines.Last());
            //retreatedBldLines.RemoveAt(retreatedBldLines.Count - 1);
 
            //내각 구하기
            List<double> innerAngles = InnerAngles(lineSegments);
            List<double> parameter = new List<double>();
            for (int i = 0; i < retreatedBldLines.Count; i++)
            {
                double param = 0;
                double road1 = roadWidth[i];
                double road2 = roadWidth[(i + 1) % roadWidth.Count];
                double min, max;
                MinValue(road1, road2, out min, out max);
                //90도 미만
                if (innerAngles[i] < 90)
                {
                    //8미터 미만 도로
                    if (min < 8000 && max < 8000)
                    {
                        //4미터 이상 6미터 미만이면 3미터
                        if (min >= 4000 && min < 6000)
                        {
                            param = 3000;
                        }
                        //6미터 이상 8미터 미만이면 4미터
                        else if (min >= 6000 && min < 8000)
                        {
                            param = 4000;
                        }
                    }
                    //90도 미만 8미터 이상이면 
                    else
                    {
                        param = 0;
                    }
                }
                //90도 이상 120도 미만
                if (innerAngles[i] >= 90 && innerAngles[i] < 120)
                {
                    //8미터 미만 도로
                    if (min < 8000 && max < 8000)
                    {
                        if (min >= 4000 && min < 6000)
                        {
                            param = 2000;
                        }
                        else if (min >= 6000 && min < 8000)
                        {
                            param = 3000;
                        }
                    }
                    else
                    {
                        param = 0;
                    }
                }
                //120도 이상 8미터 이상 도로
                if (innerAngles[i] >= 120 || max>=8000)
                {
                    param = 0;
                }
                parameter.Add(param);
            }

            List<Point3d> validPoints = CutCorner(parameter, retreatedBldLines, roadWidth);

            this.finalLand = new Polyline(validPoints).ToNurbsCurve();

            var finaltest = finalLand.DuplicateSegments().Select(n => n.PointAtStart).ToArray();
            this.finalArea = AreaMassProperties.Compute(this.finalLand).Area;

        }


        //코너 자르기
        public List<Point3d> CutCorner(List<double> param, List<Line> lineSegments, List<double> roadWidth)
        {
            //새로 산정된 대지를 그리기 위한 유효 포인트
            List<Point3d> validPoints = new List<Point3d>();
            //새로 생긴 segment와 기존에 존재하는 도로 폭 리스트
            this.newRoadWidth = new List<double>();
            for (int i = 0; i < lineSegments.Count; i++)
            {
                //가각정리가 필요 없는 곳에는 기존에 존재하는 도록폭 그대로 추가
                newRoadWidth.Add(roadWidth[i]);
                Line l1 = lineSegments[i];
                Line l2 = lineSegments[(i + 1) % lineSegments.Count];
                double a, b;
                Vector3d v1, v2;
                //대지가 꺾인 부분마다 벡터 방향 다시 조정
                Rhino.Geometry.Intersect.Intersection.LineLine(l1, l2, out a, out b, 0, false);
                if (a > 0 && b <= 0)
                {
                    v1 = lineSegments[i].UnitTangent * -1;
                    v2 = lineSegments[(i + 1) % lineSegments.Count].UnitTangent;
                }
                else if (b > 0 && a <= 0)
                {
                    v1 = lineSegments[i].UnitTangent;
                    v2 = lineSegments[(i + 1) % lineSegments.Count].UnitTangent * -1;
                }
                else if (b > 0 && a > 0)
                {
                    v1 = lineSegments[i].UnitTangent * -1;
                    v2 = lineSegments[(i + 1) % lineSegments.Count].UnitTangent * -1;
                }
                else
                {
                    v1 = lineSegments[i].UnitTangent;
                    v2 = lineSegments[(i + 1) % lineSegments.Count].UnitTangent;
                }
                Line line1 = new Line(l1.To, v1, param[i]);
                Line line2 = new Line(l2.From, v2, param[i]);
                //새로 생긴 segment에 도로폭은 양쪽으로 더 큰 도로폭으로 
                double min, max;
                MinValue(roadWidth[i], roadWidth[(i + 1) % roadWidth.Count], out min, out max);
                //가각정리가 된곳에 도로폭 추가
                if (param[i] != 0)
                {
                    //int insertIndex = newRoadWidth.Count > 2 ? newRoadWidth.Count - 2 : 0;
                    //newRoadWidth.Insert(newRoadWidth.Count-2, max);
                    newRoadWidth.Add(max);
                }
                validPoints.Add(new Point3d(line1.To));
                validPoints.Add(new Point3d(line2.To));
            }
            validPoints.Add(validPoints[0]);
            return validPoints;
        }

        //curve배열 line 리스트로 변환
        public List<Line> LineConversion(Curve[] segments)
        {
            List<Line> lineSegments = new List<Line>();
            for (int i = 0; i < segments.Length; i++)
            {
                Line line = new Line(segments[i].PointAtStart, segments[i].PointAtEnd);
                lineSegments.Add(line);
            }
            return lineSegments;
        }

        //내각 구하기
        public List<double> InnerAngles(List<Line> lineSegments)
        {
            List<double> innerAngles = new List<double>();
            for (int i = 0; i < lineSegments.Count; i++)
            {
                Line l1 = lineSegments[i];
                Line l2 = lineSegments[(i + 1) % lineSegments.Count];
                double a, b;
                Vector3d v1, v2;
                Rhino.Geometry.Intersect.Intersection.LineLine(l1, l2, out a, out b, 0, false);
                if (a > 0 && b <= 0)
                {
                    v1 = lineSegments[i].UnitTangent * -1;
                    v2 = lineSegments[(i + 1) % lineSegments.Count].UnitTangent;
                }
                else if (b > 0 && a <= 0)
                {
                    v1 = lineSegments[i].UnitTangent;
                    v2 = lineSegments[(i + 1) % lineSegments.Count].UnitTangent * -1;
                }
                else if (b > 0 && a > 0)
                {
                    v1 = lineSegments[i].UnitTangent * -1;
                    v2 = lineSegments[(i + 1) % lineSegments.Count].UnitTangent * -1;
                }
                else
                {
                    v1 = lineSegments[i].UnitTangent;
                    v2 = lineSegments[(i + 1) % lineSegments.Count].UnitTangent;
                }
                double innerAngle = RhinoMath.ToDegrees(Vector3d.VectorAngle(v2, v1,Plane.WorldXY));
                innerAngles.Add(innerAngle);
            }
            return innerAngles;
        }

        //도로폭 중에서 좁은 도로와 넓은 도로 구분하기
        public void MinValue(double a, double b, out double min, out double max)
        {
            List<double> c = new List<double>();
            c.Add(a);
            c.Add(b);
            c.Sort();
            min = c[0];
            max = c[c.Count - 1];
        }
    }// gagak class
}//prototype namespace
