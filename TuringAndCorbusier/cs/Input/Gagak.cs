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
        public Gagak() { }

        public int order { get; set; }
        public Curve segment { get; set; }
        public int roadWidth { get; set; }
        public double innerAngle { get; set; }
        public double roadLength { get; set; }
        public bool isConvex { get; set; }
        public Guid previousBoundary { get; set; }


        public void DrawSimplifiedBoundary()
        {
            Curve test = TuringAndCorbusierPlugIn.InstanceClass.plot.Boundary.DuplicateCurve();
            
            RhinoApp.WriteLine("DrawSimplification clicked");
            var guid = LoadManager.getInstance().DrawObjectWithSpecificLayer(test, LoadManager.NamedLayer.Guide);
            RhinoDoc.ActiveDoc.Objects.Select(guid);
            RhinoDoc.ActiveDoc.Objects.Delete(this.previousBoundary,true);
            RhinoDoc.ActiveDoc.Views.Redraw();
        }

        public void SortSimplifiedRoadWidth(Curve simplifiedBoundary,Curve originalBoundary,List<Gagak> gagakList)
        {
            var roadWidth = TuringAndCorbusierPlugIn.InstanceClass.plot.Surroundings.ToList();
            List<Curve> simplifiedBoundarySegments = simplifiedBoundary.DuplicateSegments().ToList();
            List<Curve> originalBoundarySegments = originalBoundary.DuplicateSegments().ToList();

            List<int> simplifiedIndex = new List<int>();
            List<int> newlyAddIndex = new List<int>();
            try
            {
                for (int i = 0; i < originalBoundarySegments.Count; i++)
                {
                    Point3d originalStartPoint = originalBoundarySegments[i].PointAtStart;
                    Point3d originalEndPoint = originalBoundarySegments[i].PointAtEnd;
                    Point3d simplifiedStartPoint = simplifiedBoundarySegments[i].PointAtStart;
                    Point3d simplifiedEndPoint = simplifiedBoundarySegments[i].PointAtEnd;
                    if(originalStartPoint!=simplifiedStartPoint)
                }

            }
            catch (Exception e)
            {
                RhinoApp.WriteLine(e.ToString());
            }

        }

        public void SortRoadWidth(List<int> cuttingLength, List<Gagak> gagakList)
        {
            var roadWidth = TuringAndCorbusierPlugIn.InstanceClass.plot.Surroundings.ToList();
            Dictionary<int, int> additionalRoadWidth = new Dictionary<int, int>();
            List<int> finalRoadWidth = new List<int>();


                for (int i = 0; i < gagakList.Count; i++)
                {
                    var gagak1 = gagakList[i].roadWidth;
                    var gagak2 = gagakList[(i + gagakList.Count - 1) % gagakList.Count].roadWidth;

                    if (cuttingLength[i] != 0)
                    {
                        var widerRoad = gagak1 >= gagak2 ? gagak1 : gagak2;
                        additionalRoadWidth.Add(i, widerRoad);
                    }
                }
            try
            {
                for (int i = 0; i < roadWidth.Count; i++)
                {
                    if (additionalRoadWidth.ContainsKey(i))
                    {
                        finalRoadWidth.Add(additionalRoadWidth[i]);
                    }
                    finalRoadWidth.Add(roadWidth[i]);
                }

            }
            catch (Exception e) {
                RhinoApp.WriteLine(e.ToString());
            }
            List<int> sortedRoadWidth = new List<int>();
            if (additionalRoadWidth.ContainsKey(0))
            {
                for (int i = 0; i < finalRoadWidth.Count; i++)
                {
                    sortedRoadWidth.Add(finalRoadWidth[(i + 1) % finalRoadWidth.Count]);
                }

            }
            else
            {
                sortedRoadWidth = finalRoadWidth;
            }
            TuringAndCorbusierPlugIn.InstanceClass.plot.Surroundings = sortedRoadWidth.ToArray();
                TuringAndCorbusierPlugIn.InstanceClass.plot.UpdateSimplifiedSurroundings();

        }

        //도로 모퉁이 건축선
        public List<Point3d> CutBoundary(List<int> cuttingLength, Curve originalBoundary)
        {
            List<Point3d> validPoints = new List<Point3d>();
            List<Curve> boundary = originalBoundary.DuplicateSegments().ToList();
            for (int i = 0; i < boundary.Count; i++)
            {
                Line line1 = new Line(boundary[i].PointAtStart, boundary[i].PointAtEnd);
                Line line2 = new Line(boundary[(i + boundary.Count - 1) % boundary.Count].PointAtEnd, boundary[(i + boundary.Count - 1) % boundary.Count].PointAtStart);

                Vector3d v1, v2;
                v1 = line1.UnitTangent;
                v2 = line2.UnitTangent;

                Line finalLine1 = new Line(line1.From, v1, cuttingLength[i]);
                Line finalLine2 = new Line(line2.From, v2, cuttingLength[i]);

                if (finalLine1.To != finalLine2.To)
                {
                    validPoints.Add(finalLine1.To);
                    validPoints.Add(finalLine2.To);
                }
                else
                {
                    validPoints.Add(finalLine1.To);
                }
            }
            return validPoints;
        }

        //포인트 재정렬
        public List<Point3d> ReOrderPointList(Curve boundary, List<Point3d> validPoints)
        {
            List<double> parameter = new List<double>();
            List<Curve> segments = boundary.DuplicateSegments().ToList();
            Point3d originPoint = segments[0].PointAtStart;

            for (int i = 0; i < validPoints.Count; i++)
            {
                double param;
                boundary.ClosestPoint(validPoints[i], out param);
                parameter.Add(param);
            }
            parameter.Sort();
            //for (int i = 0; i < parameter.Count; i++)
            //{

            //    RhinoApp.WriteLine(parameter[i].ToString());

            //}
            List<Point3d> finalPoints = new List<Point3d>();
            for (int i = 0; i < parameter.Count; i++)
            {
                for (int j = 0; j < validPoints.Count; j++)
                {
                    double param;
                    boundary.ClosestPoint(validPoints[j], out param);
                    if (parameter[i] == param)
                    {
                        finalPoints.Add(validPoints[j]);
                    }
                }
            }
            finalPoints.Add(finalPoints[0]);
            for(int i = 0; i < finalPoints.Count; i++)
            {
                RhinoApp.WriteLine("finalPoints : "+i.ToString()+" : "+finalPoints[i].ToString());
            }
            return finalPoints;
        }

        //도로모퉁이 거리 구하기
        public List<int> GetGagak(List<Gagak> parcel)
        {
            var roadWidth = TuringAndCorbusierPlugIn.InstanceClass.plot.Surroundings.ToList();
            List<int> cuttingLength = new List<int>();
            for (int i = 0; i < parcel.Count; i++)
            {
                var segment1 = parcel[i];
                var segment2 = parcel[(i + parcel.Count - 1) % parcel.Count];

                //conditon1
                int min = GetMin(segment1.roadWidth, segment2.roadWidth);
                if (min <= 4999)
                {
                    if (segment1.innerAngle >= 90)
                    {
                       cuttingLength.Add(2000);
                       // cuttingLength.Add(1111);
                    }
                    else if (segment1.innerAngle < 90)
                    {
                       cuttingLength.Add(3000);
                       // cuttingLength.Add(2222);
                    }
                }
                else if (min > 4999 && min < 8000)
                {
                    if (segment1.innerAngle >= 90)
                    {
                        cuttingLength.Add(3000);
                       // cuttingLength.Add(3333);
                    }
                    else if (segment1.innerAngle < 90)
                    {
                       cuttingLength.Add(4000);
                       // cuttingLength.Add(4444);
                    }
                }

                if (min >= 8000)
                {
                   cuttingLength.Add(0);
                  //  cuttingLength.Add(5555);
                }

            }

            for (int i = 0; i < parcel.Count; i++)
            {
                if (parcel[i].roadWidth >= 8000)
                {
                    cuttingLength[i] = 0;
                    cuttingLength[(i + 1) % cuttingLength.Count] = 0;
                }
                if (parcel[i].roadWidth == 0)
                {
                    cuttingLength[i] = 0;

                    cuttingLength[(i + 1) % cuttingLength.Count] = 0;
    
                }
                if (parcel[i].isConvex != true)
                {
                    cuttingLength[i] = 0;
       
                }
                if (parcel[i].roadLength < 7000)
                {
                    cuttingLength[i] = 0;

                    cuttingLength[(i + 1) % cuttingLength.Count] = 0;

                    //cuttingLength[(i + cuttingLength.Count - 1) % cuttingLength.Count] = 0;

                }
                if (parcel[i].innerAngle >= 120)
                {
                    cuttingLength[i] = 0;
                }
            }
            return cuttingLength;
        }

        //2개의 도로중 더 짧은 도로 찾기
        public int GetMin(int roadWidth1, int roadWidth2)
        {
            if (roadWidth1 >= 8000 || roadWidth2 >= 8000)
            {
                return roadWidth1 >= roadWidth2 ? roadWidth1 : roadWidth2;
            }
            if (roadWidth1 > roadWidth2)
            {
                return roadWidth2;
            }
            if (roadWidth1 < roadWidth2)
            {
                return roadWidth1;
            }
            else
            {
                return roadWidth1;
            }
        }

    }// gagak class
}//prototype namespace
