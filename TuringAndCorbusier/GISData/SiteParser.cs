using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rhino.Geometry;
using GISData.DataStruct;
using Rhino;
using TuringAndCorbusier.Utility;
namespace GISData.Extract
{
    public class SiteParser
    {
        public static List<Pilji> ImprovedFind(Curve curve, List<Pilji> temppiljis)
        {
            //add segments param
            Curve c = curve;
            if (c.ClosedCurveOrientation(Plane.WorldXY) == CurveOrientation.CounterClockwise)
                c.Reverse();

            List<double> cparams = new List<double>();
            cparams.AddRange(c.DuplicateSegments().Select(n => n.Domain.Min));

            //get objects in 100m
            List<Pilji> objectsin100m = new List<Pilji>();

            Point3d testpoint = c.Centroid();


            DateTime start = DateTime.Now;
            List<Pilji> objects = new List<Pilji>();

            foreach (var p in temppiljis)
            {
                //통과하는 필지 - outbound 내부에 c의 중점을 가지고 있지 않아야함 , subtractors 내부에 c의 중점을 가지고 있으면 괜찮음.
                //중점 포함함?
                if (p.Outbound[0].Contains(c.Centroid()) == PointContainment.Inside)
                {//ㅇㅇ
                    bool subContainsP = false;
                    foreach (var polygon in p.Shape.Polygons)
                    {
                        foreach (var subs in polygon.Subtractors)
                        {
                            if (subs.ToNurbsCurve().Contains(c.Centroid()) == PointContainment.Inside)
                            {
                                objects.Add(p);
                                subContainsP = true;
                                break;
                            }
                        }

                        if (subContainsP)
                            break;
                    }
                }
                else
                    objects.Add(p);
            }
            //.Select(n => n.Geometry as Curve).Where(n => n.UserStringCount > 1)
            //.Where(n => c.Contains(n.Centroid()) != PointContainment.Inside)
            //.ToArray();
            
            DateTime end = DateTime.Now;

            //RhinoApp.WriteLine("오브젝트 긁어오기 시간 : " + (end.Second - start.Second) + "초" + (end.Millisecond - start.Millisecond) + "ms");


            start = DateTime.Now;
            Point3d[] closestpoints = new Point3d[objects.Count];
            for (int i = 0; i < objects.Count; i++)
            {
                double d = 0;
                var x = objects[i].Outbound[0];
                x.ClosestPoint(testpoint, out d);
                closestpoints[i] = x.PointAt(d);

                if (closestpoints[i].DistanceTo(testpoint) < 100000)
                    objectsin100m.Add(objects[i]);
            }

            end = DateTime.Now;

            //RhinoApp.WriteLine("100m 긁어오기 시간 : " + (end.Second - start.Second) + "초" + (end.Millisecond - start.Millisecond) + "ms");

            start = DateTime.Now;

            //intersection check & add params

            foreach (var obj in objectsin100m)
            {
                var objc = obj.Outbound[0];
                var intersection = Rhino.Geometry.Intersect.Intersection.CurveCurve(c, objc, 0, 0.1);
                foreach (var i in intersection)
                {
                    if (i.IsPoint)
                    {
                        cparams.Add(i.ParameterA);
                    }
                    else if (i.IsOverlap)
                    {
                        cparams.Add(i.OverlapA.Min);
                        cparams.Add(i.OverlapA.Max);
                    }
                }
            }

            end = DateTime.Now;

            //RhinoApp.WriteLine("오버랩체크 시간 : " + (end.Second - start.Second) + "초" + (end.Millisecond - start.Millisecond) + "ms");



            //clean value

            cparams = cparams.Distinct().ToList();

            //cparams = cparams.Where(n => n % 1 == 0 || ( Math.Abs(n % 1) > 0.05 && Math.Abs(n%1) < 0.95 )).ToList() ;


            //split with clean values

            List<Curve> result = c.Split(cparams).ToList();

            return objectsin100m;


        }
        public static double[] GetRoadWidths(Curve boundary,List<Pilji> objects)
        {
            //List<Pilji> objects = (TuringAndCorbusierPlugIn.InstanceClass.turing.GISSlot.Content as ServerUI).Piljis;
            List<Curve> notdo = ImprovedFind(boundary, objects).Select(n=>n.Outbound[0]).ToList();
            List<Curve> found = boundary.DuplicateSegments().ToList();
            double[] roadwidth = new double[found.Count];

            for (int i = 0; i < found.Count; i++)
            {
                //RhinoDoc.ActiveDoc.Objects.Add(found[i]);

                if (found[i].GetLength() < 10)
                {
                    roadwidth[i] = 0;
                    continue;
                }

                double mid = found[i].Domain.Mid;
                var v = found[i].TangentAt(mid);




                v.Rotate(-Math.PI / 2, Vector3d.ZAxis);
                Curve cross = new LineCurve(found[i].PointAtNormalizedLength(0.5) + v * -100, found[i].PointAtNormalizedLength(0.5) + v * 100000);
                //RhinoDoc.ActiveDoc.Objects.Add(cross);
                List<double> distances = new List<double>();
                foreach (var obj in notdo)
                {
                    var intersect = Rhino.Geometry.Intersect.Intersection.CurveCurve(cross, obj, 0, 0);
                    if (intersect.Count >= 2)
                    {
                        var mindistance = intersect.Min(n => n.PointA.DistanceTo(cross.PointAtStart));
                        distances.Add(mindistance);
                    }

                }
                //RhinoDoc.ActiveDoc.Objects.Add(cross);
                distances = distances.OrderBy(n => n).ToList();
                if (distances.Count == 0)
                {
                    roadwidth[i] = 0;
                }
                else if (distances[0] < 500)
                {
                    roadwidth[i] = 0;
                }
                else
                {
                    double temp = Math.Round(distances[0] - 100);
                    double tempper1000 = temp / 1000;
                    double roundtemp = Math.Round(tempper1000);
                    double tempmult1000 = roundtemp * 1000;

                    roadwidth[i] = tempmult1000;
                }




            }

            for (int i = 0; i < roadwidth.Length; i++)
            {
                var cp = found[i].PointAtNormalizedLength(0.5);
                //RhinoDoc.ActiveDoc.Objects.AddTextDot(roadwidth[i].ToString(), cp);
            }

            return roadwidth.Reverse().ToArray();
        }
    }
}
