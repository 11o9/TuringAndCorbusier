using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rhino.Geometry;
using TuringAndCorbusier.Utility;
namespace GISData.DataStruct
{

    public class District
    {

        protected MultiPolygon shape;
        protected string code;
        protected string name;

        public List<Curve> Outbound { get { return GetOutBound(); } }
        public string Name { get { return name; } }
        public string Code { get { return code; } }
        public Curve[] Region { get { return GetRegion(); } }

        public MultiPolygon Shape { get { return shape; } }

        private Curve[] GetRegion()
        {
            return shape.Shape.Select(n => n.ToNurbsCurve()).ToArray();
        }

        private List<Curve> GetOutBound()
        {
            return shape.OutBounds.Select(n => n.ToNurbsCurve() as Curve).ToList();
        }

        public void Scale(double k)
        {
            Shape.Scale(k);
        }
    }

    public class Pilji : District
    {
        string jimok;
        string usage;
        public Pilji(MultiPolygon m, string code, string name)
        {
            shape = m;
            this.code = code;
            this.name = name;
        }

        public Pilji(Pilji pilji)
        {
            code = pilji.Code;
            jimok = pilji.jimok;
            usage = pilji.usage;
            shape = new MultiPolygon(pilji.shape);
        }
        public string Jimok { get { return jimok; } set { jimok = value; } }
        public string Usage { get { return usage; } set { usage = value; } }
    }

    public class PiljiGroup
    {
        List<int> piljiindex = new List<int>();
        Polyline boundary = null;

        public PiljiGroup(Polyline bound)
        {
            boundary = bound;
        }

        public Polyline Boundary { get { return boundary; } }
        public List<int> Piljiindex { get { return piljiindex; } }
    }

    public class Building : District
    {
        string address = "";
        DateTime bday;
        double archarea = 0;
        double totalarea = 0;
        int group = -1;
        public Building(string bday, MultiPolygon p, double archarea, double totalarea, int group)
        {
            this.archarea = archarea;
            this.totalarea = totalarea;
            if (bday.Length == 8)
                this.bday = new DateTime(int.Parse(bday.Substring(0, 4)), int.Parse(bday.Substring(4, 2)), int.Parse(bday.Substring(6, 2)));
            else
                this.bday = DateTime.MinValue;
            shape = p;
            this.group = group;
        }

        public double GetAge()
        {
            if (bday == DateTime.MinValue)
            {
                return 40 * 365;
            }
            else
            {
                TimeSpan span = DateTime.Now - bday;

                return span.TotalDays;

            }
        }

        public string Address { get { return address; } set { address = value; } }
        public double ArchArea { get { return archarea; } }
        public double TotalArea { get { return totalarea; } }
        public double Years { get { return Math.Round(GetAge() / 365); } }
        public string BuildDate { get { return bday.Year + "-" + bday.Month + "-" + bday.Day; } }
        public int Group { get { return group; } }
        public string Code { get { return code; } set { code = value; } }
        public DateTime Bday { get { return bday; } }
    }

    ////show ds
    class GroupByRoad
    {
        List<Pilji> piljis = new List<Pilji>();
        List<Building> buildings = new List<Building>();
        MultiPolygon groupMP;
        public List<Pilji> Piljis { get { return piljis; } set { piljis = value; } }
        public List<Building> Buildings { get { return buildings; } set { buildings = value; } }
        public MultiPolygon GroupMP { get { return groupMP; } }

        public GroupByRoad Duplicate()
        {
            GroupByRoad newone = new GroupByRoad();
            newone.Buildings = new List<Building>(buildings);
            newone.piljis = new List<Pilji>(piljis);

            return newone;
        }
        public double SumArea()
        {
            double area = 0;
            foreach (var p in piljis)
            {
                foreach (var b in p.Outbound)
                {
                    area += b.GetArea();
                }
            }

            return area;
        }

        public double UsageCheck()
        {
            double availablearea = 0;
            double wholearea = 0;
            for (int i = 0; i < piljis.Count; i++)
            {
                wholearea += piljis[i].Outbound.Sum(n => n.GetArea());
                if (piljis[i].Usage == "13" || piljis[i].Usage == "14")
                    availablearea += piljis[i].Outbound.Sum(n => n.GetArea());
            }

            return availablearea / wholearea * 100;
        }

        public double BuildingAgeCheck(double agecut)
        {
            double olderthanyear = 0;
            double whole = buildings.Count;
            for (int i = 0; i < buildings.Count; i++)
            {
                if (buildings[i].GetAge() >= agecut * 365)
                    olderthanyear++;
            }

            return olderthanyear / whole * 100;
        }

        public void SetMP()
        {
            groupMP = PolygonTools.Merge(Piljis.Select(n => n.Shape).ToList());
        }
    }

    public class NameCode
    {
        public string name { get; set; }
        public string code { get; set; }
        public NameCode(string name, string code)
        {
            this.name = name;
            this.code = code;
        }
    }

    public class MultiPolygon
    {
        List<Polygon> polygons = new List<Polygon>();
        public MultiPolygon(IEnumerable<Polygon> polygons)
        {
            if (polygons.Count() == 1)
                this.polygons = polygons.ToList();
            else
            {
                this.polygons = PolygonTools.Arrange(polygons);
            }
        }

        public MultiPolygon(MultiPolygon origin)
        {
            this.polygons = new List<Polygon>(origin.polygons);
        }

        public List<Polyline> Shape { get { return GetShape(); } }
        public List<Polyline> OutBounds { get { return polygons.Select(n => n.OutBound).ToList(); } }
        public List<Polygon> Polygons { get { return polygons; } }
        List<Polyline> GetShape()
        {
            List<Polyline> result = new List<Polyline>();
            polygons.ForEach(n => result.AddRange(n.Shape));
            return result;
        }

        public void Scale(double k)
        {
            Polygons.ForEach(n => n.Scale(k));
        }
    }

    public class Polygon
    {
        Polyline outbound;
        List<Polyline> subtractors = new List<Polyline>();

        public Polygon(Polyline outbound, List<Polyline> subtractors)
        {
            this.outbound = outbound;
            this.subtractors = subtractors;
        }

        public Polygon()
        {
            this.outbound = null;
            this.subtractors = new List<Polyline>();
        }

        public List<Polyline> Shape { get { return GetShape(); } }
        public Polyline OutBound { get { return outbound; } set { outbound = value; } }
        public List<Polyline> Subtractors { get { return subtractors; } }
        List<Polyline> GetShape()
        {
            List<Polyline> result = new List<Polyline>();
            var diff = Curve.CreateBooleanDifference(outbound.ToNurbsCurve(), subtractors.Select(n => n.ToNurbsCurve()));
            for (int i = 0; i < diff.Length; i++)
            {
                Polyline temp;
                diff[i].TryGetPolyline(out temp);
                result.Add(temp);
            }

            if (result.Count == 0)
                result.Add(outbound);
            return result;
        }

        public void Scale(double k)
        {
            outbound = new Polyline(outbound.Select(n => n * k));
            for (int i = 0; i < subtractors.Count; i++)
                subtractors[i] = new Polyline(subtractors[i].Select(n => n * k));
        }

    }


    public static class PolygonTools
    {

        //multipolygon 에서, 다른 polygon (A) 에 포함되는 polygon (B) 를 찾고 , B를 A의 subtractor에 포함시킨다. 
        public static List<Polygon> Arrange(IEnumerable<Polygon> polys)
        {
            List<Polygon> output = new List<Polygon>();
            List<Polygon> orderbyarea = polys.OrderByDescending(n => n.OutBound.GetArea()).ToList();

            for (int i = 1; i < orderbyarea.Count; i++)
            {
                if (Curve.PlanarClosedCurveRelationship(orderbyarea[0].OutBound.ToNurbsCurve(), orderbyarea[i].OutBound.ToNurbsCurve(), Plane.WorldXY, 0)
                    == RegionContainment.BInsideA)
                {
                    orderbyarea[0].Subtractors.Add(orderbyarea[i].OutBound);
                    orderbyarea[i] = null;
                }

            }

            output = orderbyarea.Where(n => n != null).ToList();
            return output;
        }

        //multipolygon 두개 이상을 합친 새 multipolygon 생성
        public static MultiPolygon Merge(List<MultiPolygon> mpolys)
        {
            List<Polygon> flattenpoly = new List<Polygon>();
            List<Polyline> flattensubs = new List<Polyline>();
            List<Polyline> newsubs = new List<Polyline>();
            mpolys.ForEach(n => flattenpoly.AddRange(new List<Polygon>(n.Polygons)));

            //외곽선을 구함.
            List<Curve> toMerge = flattenpoly.Select(n => n.OutBound.ToNurbsCurve() as Curve).ToList();
            var outbounds = Curve.CreateBooleanUnion(toMerge);


            //RhinoApp.WriteLine("{0} 개의 멀티 폴리곤 병합 시도, flattenpoly count = {1} outbounds count = {2}", mpolys.Count, flattenpoly.Count, outbounds.Length);

            //List<Curve> outbounds = new List<Curve>();
            //int count = 0;
            //foreach (var f in flattenpoly)
            //{
            //    if (outbounds.Count == 0)
            //    {
            //        outbounds.Add(f.OutBound.ToNurbsCurve());
            //    }
            //    else
            //    {
            //        outbounds.Add(f.OutBound.ToNurbsCurve());
            //        try
            //        {
            //            var bb = Curve.CreateBooleanUnion(outbounds);
            //            var bu = new List<Curve>(bb);
            //            outbounds.Clear();
            //            outbounds.AddRange(bu);
            //        }
            //        catch (Exception e)
            //        {
            //            RhinoApp.WriteLine(e.Message);
            //        }

            //    }

            //    count++;

            //}

            //내부선을 구함.
            //1. 각 subtractor 마다,
            //2. 모든 outbound 들 중 자기에게 속해있는 outbound 를 찾아서
            //3. diff 해줌.
            //4. 결과를 새 subtractor에 저장
            flattenpoly.ForEach(n => flattensubs.AddRange(n.Subtractors));
            foreach (var sub in flattensubs)
            {
                Curve subc = sub.ToNurbsCurve();
                List<Curve> outbs = flattenpoly.Select(n => n.OutBound.ToNurbsCurve() as Curve).ToList();
                List<Curve> subsubset = new List<Curve>();
                foreach (var b in outbs)
                {
                    if (Curve.PlanarClosedCurveRelationship(subc, b, Plane.WorldXY, 0) == RegionContainment.AInsideB)
                    {
                        continue;
                    }

                    if (subc.Contains(b.Centroid()) == PointContainment.Inside)
                    {
                        subsubset.Add(b);
                    }

                }

                if (subsubset.Count == 0)
                {
                    Polyline tt;
                    subc.TryGetPolyline(out tt);
                    newsubs.Add(tt);
                }

                else
                {
                    var tempsubresult = Curve.CreateBooleanDifference(subc, subsubset);

                    foreach (var t in tempsubresult)
                    {
                        Polyline tt;
                        t.TryGetPolyline(out tt);
                        if (tt != null)
                            newsubs.Add(tt);
                    }
                }
            }

            List<Polygon> resultpolygons = new List<Polygon>();
            //각 outbound를 가지고 새 polygon을 만들고 포함하는 sub들을 찾아 가져감.
            for (int i = 0; i < outbounds.Length; i++)
            {
                var tempincludesub = newsubs.Where(n => outbounds[i].Contains(n.CenterPoint()) == PointContainment.Inside).ToList();
                Polyline tempoutb;
                outbounds[i].TryGetPolyline(out tempoutb);
                Polygon newpolygon = new Polygon(tempoutb, tempincludesub);
                resultpolygons.Add(newpolygon);
            }

            return new MultiPolygon(resultpolygons);

            //var subtractors = flatten.Select(n=>n.Subtractors.Select(m=>Curve.CreateBooleanDifference(m.ToNurbsCurve(),)))
        }

        public static Polygon Merge(List<Polygon> polys)
        {
            //pilji들은 어쨌든 접해있을수밖에 없으니(전처리때문에)



            var outbounds = Curve.CreateBooleanUnion(polys.Select(n => n.OutBound.ToNurbsCurve()));
            List<Polyline> subtractors = new List<Polyline>();

            //가능한지는모르겠지만
            if (outbounds.Length == 0)
            {
                return null;
            }

            //하나로 다 합쳐짐
            else if (outbounds.Length == 1)
            {
                Polyline temp;
                outbounds[0].TryGetPolyline(out temp);
                polys.ForEach(n => subtractors.AddRange(n.Subtractors));
                return new Polygon(temp, subtractors);
            }
            //둘 이상 덩어리
            else
            {

                var orderbyarea = outbounds.OrderByDescending(n => n.GetArea()).ToList();
                Polyline temp;
                orderbyarea[0].TryGetPolyline(out temp);
                polys.ForEach(n => subtractors.AddRange(n.Subtractors));
                for (int j = 0; j < subtractors.Count; j++)
                {
                    for (int i = 1; i < orderbyarea.Count; i++)
                    {
                        if (Curve.PlanarCurveCollision(subtractors[j].ToNurbsCurve(), orderbyarea[i].ToNurbsCurve(), Plane.WorldXY, 0.1))
                        {
                            Polyline tempsub;
                            var diff = Curve.CreateBooleanDifference(subtractors[j].ToNurbsCurve(), orderbyarea[i].ToNurbsCurve());
                            if (diff.Length != 1)
                            {
                                Rhino.RhinoApp.WriteLine("diff문제{0}", diff.Length);
                                continue;
                            }

                            diff[0].TryGetPolyline(out tempsub);
                            subtractors[j] = tempsub;
                        }
                    }
                }

                return new Polygon(temp, subtractors);
            }
        }

        public static Polygon Merge(Polygon p1, Polygon p2)
        {
            var outbig = p1.OutBound.GetArea() > p2.OutBound.GetArea() ? p1.OutBound : p2.OutBound;
            var outsmall = p1.OutBound.GetArea() > p2.OutBound.GetArea() ? p2.OutBound : p1.OutBound;
            var inbig = p1.OutBound.GetArea() > p2.OutBound.GetArea() ? p1.Subtractors : p2.Subtractors;
            var insmall = p1.OutBound.GetArea() > p2.OutBound.GetArea() ? p2.Subtractors : p1.Subtractors;

            Polyline tempoutb;
            List<Polyline> inside = new List<Polyline>();

            //바깥커브에접함
            if (Curve.PlanarCurveCollision(outbig.ToNurbsCurve(), outsmall.ToNurbsCurve(), Plane.WorldXY, 0.1))
            {
                var union = Curve.CreateBooleanUnion(new Curve[] { outbig.ToNurbsCurve(), outsmall.ToNurbsCurve() });
                if (union.Length != 1)
                {
                    Rhino.RhinoApp.WriteLine("union문제{0}", union.Length);
                    return p1;
                }

                Polyline temp;
                union[0].TryGetPolyline(out temp);

                inside.AddRange(p1.Subtractors);
                inside.AddRange(p2.Subtractors);

                return new Polygon(temp, inside);
            }

            //내부커브에접함
            else
            {
                for (int i = 0; i < inbig.Count; i++)
                {
                    if (Curve.PlanarCurveCollision(inbig[i].ToNurbsCurve(), outsmall.ToNurbsCurve(), Plane.WorldXY, 0.1))
                    {
                        Polyline temp;
                        var diff = Curve.CreateBooleanDifference(inbig[i].ToNurbsCurve(), outsmall.ToNurbsCurve());
                        if (diff.Length != 1)
                        {
                            Rhino.RhinoApp.WriteLine("diff문제{0}", diff.Length);
                            return p1;
                        }

                        diff[0].TryGetPolyline(out temp);
                        inbig[i] = temp;
                        break;
                    }
                }

                inside.AddRange(insmall);
                inside.AddRange(inbig);

                return new Polygon(outbig, inside);
            }



        }
    }
}
