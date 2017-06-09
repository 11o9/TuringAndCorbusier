using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rhino;
using Rhino.Geometry;


namespace TuringAndCorbusier
{
    class DrawSection
    {
        Apartment _agout = null;
        bool drawnorthline = true;
        public DrawSection(Apartment agout)
        {
            _agout = agout;
            if (agout.Plot.PlotType == PlotType.상업지역)
                drawnorthline = false;
        }

        public SectionBase Draw()
        {

            if (_agout == null)
                return null;

            SectionBase result = null;
            if (_agout.AGtype == "PT-1")
            {
                result = Section1(_agout);
                return result;
            }
            else if (_agout.AGtype == "PT-3")
            {
                result = Section3(_agout);
                return result;
            }

            else if (_agout.AGtype == "PT-4")
            {
                result = Section4(_agout);
                return result;
            }

            else
            {
                return result;
            }

        }

        public Section_Pattern1 Section1(Apartment agout)
        {
            Section_Pattern1 section = new Section_Pattern1(agout);
            section.DrawSection(drawnorthline);
            return section;
        }
        public Section_Pattern3 Section3(Apartment agout)
        {
            Section_Pattern3 section = new Section_Pattern3(agout);
            section.DrawSection(drawnorthline);
            return section;
        }
        public Section_Pattern4 Section4(Apartment agout)
        {
            Section_Pattern4 section = new Section_Pattern4(agout);
            section.DrawSection(drawnorthline);
            return section;
        }

    }

    public abstract class SectionBase
    {
        //dimension
        public abstract List<FloorPlan.Dimension2> Dimensions { get; set; }
        //sectioncurve
        public abstract List<Curve> SectionLines { get; set; }
        //outlinecurve
        public abstract List<Curve> CoreOutLines { get; set; }
        //regulation
        public abstract List<Curve> RegsOutput { get; set; }
        //text
        public abstract List<Rhino.Display.Text3d> texts { get; set; }
        //hatch
        public abstract List<Hatch> Hatchs { get; set; }
        //unitinfo
        public abstract List<UnitInfo> Unitinfo { get; set; }
        //boundingBox
        public abstract BoundingBox boundingBox { get; set; }

        public abstract List<Guid> DrawSection(bool northline);
    }


    public class UnitInfo
    {
        public List<Curve> curves = new List<Curve>();
        public List<Rhino.Display.Text3d> text = new List<Rhino.Display.Text3d>();
        Point3d position = Point3d.Unset;


        public UnitInfo(Point3d origin, int floor, double househeight)
        {
            double width = 2400;
            double height = 800;
            List<Point3d> corner = new List<Point3d>();
            corner.Add(origin + Vector3d.XAxis * width / 2 + Vector3d.YAxis * height / 2);
            corner.Add(origin - Vector3d.XAxis * width / 2 + Vector3d.YAxis * height / 2);
            corner.Add(origin - Vector3d.XAxis * width / 2 - Vector3d.YAxis * height / 2);
            corner.Add(origin + Vector3d.XAxis * width / 2 - Vector3d.YAxis * height / 2);
            corner.Add(origin + Vector3d.XAxis * width / 2 + Vector3d.YAxis * height / 2);

            Polyline rect = new Polyline(corner);
            Line horizontal = new Line(origin + Vector3d.XAxis * -width / 2, Vector3d.XAxis * width);
            Line vertical = new Line(origin, Vector3d.YAxis * -height / 2);

            Point3d titlepos = origin + Vector3d.YAxis * height / 4;
            Point3d housenumpos = origin - Vector3d.YAxis * height / 4 - Vector3d.XAxis * width / 4;
            Point3d heightnumpos = origin - Vector3d.YAxis * height / 4 + Vector3d.XAxis * width / 4;

            curves.Add(rect.ToNurbsCurve());
            curves.Add(horizontal.ToNurbsCurve());
            curves.Add(vertical.ToNurbsCurve());

            text.Add(MatchText("주거시설", titlepos));
            text.Add(MatchText( floor.ToString() + "01", housenumpos));
            text.Add(MatchText("CH : "+ (househeight-300).ToString(), heightnumpos));
        }

        public Rhino.Display.Text3d MatchText(string tag, Point3d pos)
        {
            Plane tempplane = new Plane(pos, Vector3d.XAxis, Vector3d.YAxis);
            return new Rhino.Display.Text3d(tag, tempplane, 0);
        }
            
        public List<Guid> Print()
        {
            List<Guid> result = new List<Guid>();
            result.AddRange(LoadManager.getInstance().DrawObjectsWithSpecificLayer(curves, LoadManager.NamedLayer.Guide));
            result.AddRange(LoadManager.getInstance().DrawObjectWithSpecificLayer(text, LoadManager.NamedLayer.Guide,TextJustification.MiddleCenter));
            return result;

        }
    }
    class Section_Pattern3 : SectionBase
    {
        List<Point3d> HousePoints = new List<Point3d>();
        // % 2 . 0 = front 1 = back
        List<Point3d> CorewidthPoints = new List<Point3d>();
        // % 2 . 0 = front 1 = back
        List<Point3d> HousewidthPoints = new List<Point3d>();
        // % 2 . 0 = coreback , 1 = housefront
        List<Point3d> BuildingwidthPoints = new List<Point3d>();
        // % 2 . 0 = househeightbottom , 1 = househeighttop
        List<Point3d> AptwidthPoints = new List<Point3d>();
        // % 2 . 0 = aptwidth , 1 = aptwidth
        List<Point3d> HouseheightPoints = new List<Point3d>();

        List<Point3d> RegPoints = new List<Point3d>();
        List<string> Regname = new List<string>();


        //print these

        //dimension
        public override List<FloorPlan.Dimension2> Dimensions { get; set; }
        //sectioncurve
        public override List<Curve> SectionLines { get; set; }
        //outlinecurve
        public override List<Curve> CoreOutLines { get; set; }
        //regulation
        public override List<Curve> RegsOutput { get; set; }
        //text
        public override List<Rhino.Display.Text3d> texts { get; set; }
        //hatch
        public override List<Hatch> Hatchs { get; set; }
        //unitinfo
        public override List<UnitInfo> Unitinfo { get; set; }
        //boundingBox
        public override BoundingBox boundingBox { get; set; }

        List<Point3d> toboundingbox = new List<Point3d>();



        List<Curve> AptLines = null;
        List<double> x = null; //House Left
        List<double> y = null; //Core Left
        List<double> z = null; //House Right
        List<double> u = null; //Core Right
        List<int> strs = new List<int>();
        Curve Plot = null;
        List<int> surr = null;
        Point3d SectionBasePoint = Point3d.Unset;


        List<Polyline> Regs = new List<Polyline>();
        List<Curve> Guides = new List<Curve>();

        List<Guid> drawnGuids = new List<Guid>();

        //minx maxx miny maxy
        //

        bool is4core = false;

        public Section_Pattern3(Apartment agOut)
        {
            this.Dimensions = new List<FloorPlan.Dimension2>();
            this.SectionLines = new List<Curve>();
            this.CoreOutLines = new List<Curve>();
            this.RegsOutput = new List<Curve>();
            this.texts = new List<Rhino.Display.Text3d>();
            this.Hatchs = new List<Hatch>();
            this.Unitinfo = new List<UnitInfo>();
            this.boundingBox = new BoundingBox();

            this.AptLines = agOut.AptLines[0].DuplicateSegments().ToList();

            ///    0, 2 번 아파트라인 추출해서 자릅니다.

            //coreback = coreright
            List<double> temp = new List<double>();
            List<double> temp2 = new List<double>();
            List<double> widthtemp = new List<double>();
            for (int i = 0; i < agOut.Core.Count; i++)
            {
                Point3d coreOri = agOut.Core[i][0].Origin;
                double param;
                AptLines[i].ClosestPoint(coreOri, out param);
                Point3d aptClosest = AptLines[i].PointAt(param);
                temp.Add(Vector3d.Multiply(agOut.Core[i][0].YDirection, new Vector3d(coreOri - aptClosest)));
                if (agOut.Core[i].Count > 2)
                {
                    is4core = true;
                }
                widthtemp.Add(agOut.ParameterSet.Parameters[2] / 2);
            }

            this.x = widthtemp.Select(n => -n).ToList();
            this.z = widthtemp;
            this.u = temp;
            //corefront = coreleft

            for (int i = 0; i < agOut.Core.Count; i++)
            {
                temp2.AddRange(u.Select(n => n + agOut.Core[i][0].Depth).ToList());
                strs.Add(Convert.ToInt32(Math.Round(agOut.Core[i][0].Stories + 1)));
            }
            this.y = temp2;


            Plot = agOut.Plot.Boundary;
            surr = agOut.Plot.Surroundings.ToList();

            SectionBasePoint = Point3d.Origin;

        }

        public override List<Guid> DrawSection(bool northline)
        {

            double slavHeight = 300;
            double houseHeight = 2700;
            double pilHeight = 3300;
            double wallThick = 300;

            List<Curve> offsetcurves = new List<Curve>();
            Curve Axis = null;
            List<Point3d> dirPoint = new List<Point3d>();
            List<Point3d> cPs = new List<Point3d>();
            List<Vector3d> dirVector = new List<Vector3d>();

            

            //아파트 라인 1의 수직인 방향 점과 방향 벡터 구함
     
            cPs.Add(AptLines[1].PointAtNormalizedLength(0.5));
            Curve tempC = AptLines[1].DuplicateCurve();

            tempC.Rotate(RhinoMath.ToRadians(-90), Vector3d.ZAxis, AptLines[1].PointAtNormalizedLength(0.5));
            dirVector.Add(new Vector3d(tempC.PointAtEnd - tempC.PointAtStart));
            dirPoint.Add(tempC.PointAtEnd);


            ///아파트 라인 1의 중심에서 수직인 선을 만들고 축선으로 쓴다.

            for (int i = 0; i < cPs.Count; i++)
            {
                Line templine = new Line(cPs[i], dirVector[i] * 100);
                Curve tempCurve = templine.ToNurbsCurve();
                tempCurve.Translate(new Vector3d(-dirVector[i]) * 50);
                Axis = tempCurve;
            }
            //만약 축선이 안구해지면 리턴
            if (Axis == null)
                return null;


            //북향 대지 경계 찾기.
            List<int> NCindex = new List<int>();

            for (int i = 0; i < Plot.DuplicateSegments().Length; i++)
            {
                if (Plot.DuplicateSegments()[i].TangentAt(0.5).X > 0)
                    NCindex.Add(i);

            }

            ///ag3 only// 센터점 구함
            ///
            List<Point3d> toCenter = new List<Point3d>();
            for (int i = 0; i < AptLines.Count; i++)
            {
                var intersection = Rhino.Geometry.Intersect.Intersection.CurveCurve(Axis, AptLines[i], 0, 0);
                if (intersection.Count > 0)
                    toCenter.Add(intersection[0].PointA);
            }
            if (toCenter.Count != 2)
                return null;

            Line centerline = new Line(toCenter[0], toCenter[1]);
            Point3d CenterPoint = centerline.PointAt(0.5);

            

            //구한 축선을 대지에 맞게 자르고 아파트라인, 코어라인을 그어준다.
            //만약 접한 경계가 도로라면 경계와 접한 도로 너비의 값을 축선에 보정

            List<double> splitparam = new List<double>();
            var plotseg = Plot.DuplicateSegments();
            double[] realRoad = new double[2];
            double[] roadwidth = new double[2];
            bool[] isN = new bool[2];
            double[] sangle = new double[2];
            double tangle = 0;
            int arrayindex = 1;

            for (int i = 0; i < plotseg.Length; i++)
            {

                var intersection2 = Rhino.Geometry.Intersect.Intersection.CurveCurve(Axis, plotseg[i], 0, 0);
                int dir = 1;
                double tempd = surr[i];
                foreach (var intersect in intersection2)
                {


                    if (intersect.PointA.DistanceTo(Axis.PointAtStart) < intersect.PointA.DistanceTo(Axis.PointAtEnd))
                    {
                        dir = -1;
                        arrayindex = 0;
                    }
                    else
                    {
                        dir = 1;
                        arrayindex = 1;
                    }

                    if (NCindex.Contains(i))
                    {
                        isN[arrayindex] = true;
                    }
                    else
                        isN[arrayindex] = false;



                    tangle = Vector3d.VectorAngle(Axis.TangentAtEnd, plotseg[i].TangentAtEnd);
                    realRoad[arrayindex] = tempd * dir / Math.Sin(tangle);
                    roadwidth[arrayindex] = tempd;
                    double temprad = Vector3d.VectorAngle(plotseg[i].TangentAtEnd, Vector3d.YAxis);
                    double temprad2 = Vector3d.VectorAngle(Axis.TangentAtEnd, plotseg[i].TangentAtEnd);

                    double tempsinval = Math.Abs(Math.Sin(temprad)) / Math.Abs(Math.Sin(temprad2));
                    //sangle[arrayindex] = Math.Abs(Math.Cos(temprad));
                    sangle[arrayindex] = tempsinval;
                    splitparam.Add(intersect.ParameterA);

                }

            }



            Axis = Axis.Split(splitparam)[1];

            Interval newdomain = new Interval(0, 1);
            Axis.Domain = newdomain;
            AptLines.RemoveRange(2, 2);
            AptLines.RemoveAt(0);

            offsetcurves.AddRange(Offsets(AptLines, dirPoint, x));
            offsetcurves.AddRange(Offsets(AptLines, dirPoint, z));
            offsetcurves.AddRange(Offsets(AptLines, dirPoint, y));
            offsetcurves.AddRange(Offsets(AptLines, dirPoint, u));

            //단면 대지선을 지정한 단면 포인트로부터 만들어주고 축선과 offset라인의 교점의 값을 sectionbase에 저장.



            List<double> sectionbase = new List<double>();

            foreach (Curve c in offsetcurves)
            {
                var intersection3 = Rhino.Geometry.Intersect.Intersection.CurveCurve(Axis, c, 0, 0);
                foreach (var intersect in intersection3)
                {
                    sectionbase.Add(intersect.ParameterA);
                }

            }


            //단면 대지선을 그려주고 sectionbase값에 맞춰 잘라줌.


            double sectiondir = 1;


            if (Axis.TangentAtEnd.X == 0 && Axis.PointAtStart.Y > Axis.PointAtEnd.Y)
                sectiondir = 1;

            else if (Axis.TangentAtEnd.X == 0 && Axis.PointAtStart.Y < Axis.PointAtEnd.Y)
                sectiondir = -1;

            else
                sectiondir = Axis.TangentAtEnd.X / Math.Abs(Axis.TangentAtEnd.X);

            Line SectionBaseline = new Line(SectionBasePoint, Vector3d.XAxis * sectiondir, new Vector3d(Axis.PointAtEnd - Axis.PointAtStart).Length);
            

            //RhinoApp.WriteLine(Axis.TangentAtEnd.ToString());
            //RhinoApp.WriteLine(sectiondir.ToString());

            Curve SectionBasecurve = SectionBaseline.ToNurbsCurve();

            if (sectiondir == -1)
                SectionBasecurve.Translate(Vector3d.XAxis * SectionBaseline.Length);

            var spltSB = SectionBasecurve.Split(sectionbase);

            double param;
            Axis.ClosestPoint(CenterPoint, out param);

            Point3d sbcenter = SectionBasecurve.PointAt(param);

            //단면 대지선의 양 사이드에 먼저 추출해둔 도로선만큼 확장 해줌.


            Line left = new Line(SectionBasecurve.PointAtStart, Vector3d.XAxis * sectiondir * realRoad[0]);
            Line right = new Line(SectionBasecurve.PointAtEnd, Vector3d.XAxis * sectiondir * realRoad[1]);



            Line left2 = new Line(left.To, Vector3d.XAxis * sectiondir * -5000);
            Line right2 = new Line(right.To, Vector3d.XAxis * sectiondir * 5000);

            toboundingbox.Add(left2.To + Vector3d.YAxis * -5000);
            toboundingbox.Add(right2.To + Vector3d.YAxis * -5000);
            // boundingbox 만들기
            toboundingbox.Add(Point3d.Origin + Vector3d.YAxis * (strs[0] * 2700 + 15000));

            boundingBox = new BoundingBox(toboundingbox);


            //hatch 만들기

            double hatchheight = -5000;

            List<Point3d> groundoutrect = new List<Point3d>();
            groundoutrect.Add(left2.To);
            groundoutrect.Add(left2.To + Vector3d.YAxis * hatchheight);
            groundoutrect.Add(right2.To + Vector3d.YAxis * hatchheight);
            groundoutrect.Add(right2.To);
            groundoutrect.Add(left2.To);
            Curve groundhatchbox = new PolylineCurve(groundoutrect) as Curve;

            groundhatchbox.MakeClosed(1000);

            var ground = Hatch.Create(groundhatchbox, 1, RhinoMath.ToRadians(45), 10000);
            for (int i = 0; i < ground.Length; i++)
                //RhinoDoc.ActiveDoc.Objects.AddHatch(ground[i]);
                if (ground.Length > 0)
                    RhinoDoc.ActiveDoc.Views.Redraw();





            //대지경계 / 인접대지경계 선 출력

            List<Line> lines = new List<Line>();

            Line tempbline = Line.Unset;

            Point3d[] nearplot = new Point3d[2];
            tempbline = new Line(SectionBasecurve.PointAtStart, Vector3d.YAxis * 5000);
            lines.Add(tempbline);
            dotted(tempbline, 500);
            nearplot[0] = tempbline.To;
            if (left.Length != 0)
            {
                Regname.Add("대지경계");
                RegPoints.Add(tempbline.PointAt(0.5));
                tempbline = new Line(left.To, Vector3d.YAxis * 5000);
                lines.Add(tempbline);

                dotted(tempbline, 500);

                nearplot[0] = tempbline.To;
            }


            Regname.Add("인접대지경계");
            RegPoints.Add(tempbline.PointAt(0.5));

            tempbline = new Line(SectionBasecurve.PointAtEnd, Vector3d.YAxis * 5000);
            lines.Add(tempbline);
            dotted(tempbline, 500);
            nearplot[1] = tempbline.To;
            if (right.Length != 0)
            {
                Regname.Add("대지경계");
                RegPoints.Add(tempbline.PointAt(0.5));
                tempbline = new Line(right.To, Vector3d.YAxis * 5000);
                lines.Add(tempbline);
                dotted(tempbline, 500);
                nearplot[1] = tempbline.To;
            }

            Regname.Add("인접대지경계");
            RegPoints.Add(tempbline.PointAt(0.5));

            //법규선 시작점 설정
            Point3d fromstart = SectionBasecurve.PointAtStart + Vector3d.XAxis * sectiondir * realRoad[0] / 2;
            Point3d fromend = SectionBasecurve.PointAtEnd + Vector3d.XAxis * sectiondir * realRoad[1] / 2;

            //법규선 생성


            List<Polyline> reg = new List<Polyline>();
            List<Point3d> toPoly = new List<Point3d>();
            Point3d tempp = Point3d.Unset;
            if (northline)
            {
                if (isN[0])
                {
                    toPoly.Add(fromstart);
                    tempp = fromstart + Vector3d.XAxis * sangle[0] * 1500 * sectiondir;
                    toPoly.Add(tempp);
                    tempp = tempp + Vector3d.YAxis * 9000;
                    toPoly.Add(tempp);
                    tempp = tempp + Vector3d.XAxis * sangle[0] * 3000 * sectiondir;
                    toPoly.Add(tempp);
                    tempp = tempp + new Vector3d(sangle[0] * sectiondir, 2, 0) * 9600;
                    toPoly.Add(tempp);

                    //이름붙이기
                    tempp = tempp + new Vector3d(sangle[0] * sectiondir, 2, 0) * -4800;
                    RegPoints.Add(tempp);
                    Regname.Add("정북방향 일조사선");

                    reg.Add(new Polyline(toPoly));
                }

                toPoly.Clear();
                if (nearplot[0] == null)
                    tempp = fromstart + Vector3d.YAxis * 3300;
                else
                {
                    tempp = new Point3d(nearplot[0].X, 3300, 0);
                }

                toPoly.Add(tempp);
                tempp = tempp + new Vector3d(1 * sectiondir, 4, 0) * 9600;
                toPoly.Add(tempp);

                reg.Add(new Polyline(toPoly));

                tempp = tempp + new Vector3d(1 * sectiondir, 4, 0) * -4800;
                RegPoints.Add(tempp);
                Regname.Add("채광방향 높이제한");


                toPoly.Clear();

                if (isN[1])
                {
                    toPoly.Add(fromend);
                    tempp = fromend + Vector3d.XAxis * -sangle[1] * 1500 * sectiondir;
                    toPoly.Add(tempp);
                    tempp = tempp + Vector3d.YAxis * 9000;
                    toPoly.Add(tempp);
                    tempp = tempp + Vector3d.XAxis * -sangle[1] * 3000 * sectiondir;
                    toPoly.Add(tempp);
                    tempp = tempp + new Vector3d(-sangle[1] * sectiondir, 2, 0) * 9600;
                    toPoly.Add(tempp);

                    tempp = tempp + new Vector3d(-sangle[0] * sectiondir, 2, 0) * -4800;
                    RegPoints.Add(tempp);
                    Regname.Add("정북방향 일조사선");

                    reg.Add(new Polyline(toPoly));
                }

                toPoly.Clear();



                if (nearplot[1] == null)
                    tempp = fromend + Vector3d.YAxis * 3300;
                else
                    tempp = new Point3d(nearplot[1].X, 3300, 0);
                toPoly.Add(tempp);
                tempp = tempp + new Vector3d(-1 * sectiondir, 4, 0) * 9600;
                toPoly.Add(tempp);

                reg.Add(new Polyline(toPoly));


                toPoly.Clear();


                tempp = tempp + new Vector3d(-1 * sectiondir, 4, 0) * -4800;
                RegPoints.Add(tempp);
                Regname.Add("채광방향 높이제한");

            }


            //자른 선분들의 끝점을 아파트선, 코어선 시작 리스트에 저장
            List<Point3d> CoreStart = new List<Point3d>();
            List<Point3d> HouseStart = new List<Point3d>();


            for (int i = 0; i < spltSB.Length-1; i++)
            {
                //앞에서 두개 집
                if (i % 4 < 2)
                {
                    HouseStart.Add(spltSB[i].PointAtEnd);
                    HousewidthPoints.Add(spltSB[i].PointAtEnd);
                    // 건물 총 너비
                    if (i == 0)
                    {
                        BuildingwidthPoints.Add(spltSB[i].PointAtEnd);
                        AptwidthPoints.Add(spltSB[i].PointAtEnd);
                    }
                    if (i == 1)
                    {
                        CoreStart.Add(spltSB[i].PointAtEnd);
                        CorewidthPoints.Add(spltSB[i].PointAtEnd);
                    }
                }

                //뒤에 두개 코어
                else
                {
                    CoreStart.Add(spltSB[i].PointAtEnd);
                    CorewidthPoints.Add(spltSB[i].PointAtEnd);
                    AptwidthPoints.Add(spltSB[i].PointAtEnd);
   
                }
            }

            //아파트와 코어의 외곽선을 그려준다.

            List<Curve> HouseOutline = new List<Curve>();
            List<Curve> CoreOutline = new List<Curve>();

            double AptHeight = 0;

            for (int j = 0; j < strs[0]; j++)
            {
                if (j != 0)
                    AptHeight += houseHeight;
            }

            double CoreHeight = AptHeight + houseHeight + pilHeight;

            Line AptSide1 = new Line(HouseStart[0] + Vector3d.YAxis * (pilHeight - 300), Vector3d.YAxis * (AptHeight + 300));
            Line AptSide2 = new Line(HouseStart[1] + Vector3d.YAxis * (pilHeight - 300), Vector3d.YAxis * (AptHeight + 300));
            Line AptUp = new Line(AptSide1.ToNurbsCurve().PointAtEnd, AptSide2.ToNurbsCurve().PointAtEnd);
            Line AptDown = new Line(AptSide1.ToNurbsCurve().PointAtStart, AptSide2.ToNurbsCurve().PointAtStart);

            HouseOutline.Add(AptSide1.ToNurbsCurve());
            HouseOutline.Add(AptSide2.ToNurbsCurve());
            HouseOutline.Add(AptUp.ToNurbsCurve());
            HouseOutline.Add(AptDown.ToNurbsCurve());



            Line CoreSide1 = new Line(CoreStart[0], Vector3d.YAxis * CoreHeight);
            Line CoreSide2 = new Line(CoreStart[1], Vector3d.YAxis * CoreHeight);
            Line CoreUp = new Line(CoreSide1.ToNurbsCurve().PointAtEnd, CoreSide2.ToNurbsCurve().PointAtEnd);
            Line CoreDown = new Line(CoreSide1.ToNurbsCurve().PointAtStart, CoreSide2.ToNurbsCurve().PointAtStart);

            CoreOutline.Add(CoreSide1.ToNurbsCurve());
            CoreOutline.Add(CoreSide2.ToNurbsCurve());
            CoreOutline.Add(CoreUp.ToNurbsCurve());
            CoreOutline.Add(CoreDown.ToNurbsCurve());




            //아웃라인에서 겹치는 선 제거.

            List<Curve> inHouse = new List<Curve>();
            inHouse.AddRange(HouseOutline);
            CoreOutline = Curve.JoinCurves(CoreOutline).ToList();
            HouseOutline = Curve.JoinCurves(HouseOutline).ToList();

            for (int i = 0; i < HouseOutline.Count; i++)
            {
                splitparam.Clear();

                var intersection4 = Rhino.Geometry.Intersect.Intersection.CurveCurve(CoreOutline[i], HouseOutline[i], 0, 0);
                foreach (var intersect in intersection4)
                {
                    splitparam.Add(intersect.ParameterA);
                }

                var spltd = CoreOutline[i].Split(splitparam);

                List<Curve> tempc = new List<Curve>();
                foreach (var sc in spltd)
                {
                    sc.Domain = newdomain;
                    if (HouseOutline[i].Contains(sc.PointAt(0.5)) != PointContainment.Inside)
                        tempc.Add(sc);
                }

                CoreOutline[i] = Curve.JoinCurves(tempc)[0];

            }


            //내부 선 만들어줌
            List<Curve> inside = new List<Curve>();


            List<Curve> unit = new List<Curve>();
            Curve tempunit = null;

            Curve temp;

            temp = inHouse[3].DuplicateCurve();
            temp.SetStartPoint(temp.PointAtStart + Vector3d.XAxis * sectiondir * wallThick);
            temp.SetEndPoint(temp.PointAtEnd + Vector3d.XAxis * sectiondir * -wallThick);

            temp.Translate(Vector3d.YAxis * slavHeight);
            unit.Add(temp);
            temp = temp.DuplicateCurve();
            temp.Translate(Vector3d.YAxis * (houseHeight - slavHeight));
            unit.Add(temp);

            Line temp1;
            temp1 = new Line(unit[0].PointAtStart, unit[1].PointAtStart);
            unit.Add(temp1.ToNurbsCurve());
            temp1 = new Line(unit[0].PointAtEnd, unit[1].PointAtEnd);
            unit.Add(temp1.ToNurbsCurve());


            List<Curve> toHatch = new List<Curve>();

            toHatch.Add(HouseOutline[0]);
            List<Curve> path = new List<Curve>();
            for (int j = 0; j < strs[0] - 1; j++)
            {

                tempunit = Curve.JoinCurves(unit)[0];
                tempunit.Translate(Vector3d.YAxis * houseHeight * j);
               

               
   
                inside.Add(tempunit);
                toHatch.Add(tempunit);
                RhinoDoc.ActiveDoc.Objects.AddCurve(tempunit);


                List<Point3d> topoly = new List<Point3d>();
                Point3d temppp = new Point3d(tempunit.PointAtStart) + Vector3d.XAxis * sectiondir * wallThick;
                topoly.Add(temppp);
                temppp = temppp + Vector3d.XAxis * sectiondir * 1300;
                topoly.Add(temppp);
                temppp = temppp + Vector3d.YAxis * 1000;
                topoly.Add(temppp);
                temppp = temppp + Vector3d.XAxis * sectiondir * wallThick;
                topoly.Add(temppp);
                temppp = temppp + Vector3d.YAxis * -(1000 + slavHeight);
                topoly.Add(temppp);
                temppp = temppp + Vector3d.XAxis * sectiondir * -(1300 + wallThick);
                topoly.Add(temppp);
                topoly.Add(topoly[0]);
                Polyline tempoly = new Polyline(topoly);
                path.Add(tempoly.ToNurbsCurve());
                //var thatch = Hatch.Create(tempoly.ToNurbsCurve(), 0, 0, 1000);
                //Hatchs.AddRange(thatch);


                // 세대 중심
                Polyline tempunitpl;
                tempunit.TryGetPolyline(out tempunitpl);
                HousePoints.Add(tempunitpl.CenterPoint());

                // 세대 높이선(디멘션 용 점2개)
                HouseheightPoints.Add(tempunitpl.CenterPoint() + (Vector3d.XAxis * (temp.GetLength() / 4) + Vector3d.YAxis * (houseHeight / 2 - slavHeight / 2)));
                HouseheightPoints.Add(tempunitpl.CenterPoint() + (Vector3d.XAxis * (temp.GetLength() / 4) - Vector3d.YAxis * (houseHeight / 2 - slavHeight / 2)));

                //Point3d unitheightbottom = tempunitpl.CenterPoint() + Vector3d.XAxis * 
            }


            mirror(ref toHatch, sbcenter);


                var hatch = Hatch.Create(toHatch, 0, 0, 1000);

                Hatchs.AddRange(hatch);

            mirror(ref path, sbcenter);
            foreach (var c in path)
            {
                var thatch = Hatch.Create(c, 0, 0, 1000);
                Hatchs.AddRange(thatch);
            }


            //List<FloorPlan.Dimension2> dimensions = new List<FloorPlan.Dimension2>();
            FloorPlan.Dimension2 tempdim;
            Line bgoutlinebase;
            Line bgoutline2base;
            double dimensionoffset = pilHeight + houseHeight * strs[0] + 300;
            Vector3d dimensionvec = Vector3d.YAxis * dimensionoffset;


            if (this.is4core)
            {
                mirror(ref CoreOutline, sbcenter);

                tempdim = new FloorPlan.Dimension2(CoreSide2.From + dimensionvec, CoreSide2.From + dimensionvec + Vector3d.XAxis * (sbcenter.X - CoreSide2.FromX) * 2, 500);
                Dimensions.Add(tempdim);

                bgoutlinebase = new Line(CoreSide2.From, Vector3d.XAxis * (sbcenter.X - CoreSide2.FromX) * 2);
                bgoutline2base = bgoutlinebase;
            }

            else
            {
                tempdim = new FloorPlan.Dimension2(CoreSide2.From + dimensionvec, CoreSide2.From + dimensionvec + Vector3d.XAxis * ((sbcenter.X - CoreSide2.FromX) * 2 + (CoreSide2.FromX - CoreSide1.FromX)), 500);
                Dimensions.Add(tempdim);
                bgoutlinebase = new Line(CoreSide2.From, Vector3d.XAxis * ((sbcenter.X - CoreSide2.FromX) * 2 + CoreSide2.FromX - CoreSide1.FromX));
                bgoutline2base = new Line(CoreSide2.From, Vector3d.XAxis * ((sbcenter.X - CoreSide2.FromX) * 2 + CoreSide2.FromX - CoreSide1.FromX));
            }

            tempdim = new FloorPlan.Dimension2(AptSide2.From + dimensionvec - Vector3d.YAxis * 3000, AptSide2.From + dimensionvec + Vector3d.XAxis * (sbcenter.X - AptSide2.FromX) * 2 - Vector3d.YAxis * 3000, 1250);
            Dimensions.Add(tempdim);

            //pil



            bgoutlinebase.Transform(Transform.Translation(Vector3d.YAxis * 3000));
            bgoutline2base.Transform(Transform.Translation(Vector3d.YAxis * 4300));
            Curve tempbg;
            Curve tempbg2;
            tempbg = bgoutlinebase.ToNurbsCurve().DuplicateCurve();
            tempbg2 = bgoutline2base.ToNurbsCurve().DuplicateCurve();
            CoreOutLines.Add(tempbg.DuplicateCurve());
            CoreOutLines.Add(tempbg2.DuplicateCurve());
            for (int i = 0; i < strs[0] - 1; i++)
            {

                tempbg.Transform(Transform.Translation(Vector3d.YAxis * 2700));
                if (i == strs[0] - 2)
                {
                    CoreOutLines.Add(tempbg.DuplicateCurve());
                    tempbg.Transform(Transform.Translation(Vector3d.YAxis * 300));
                    CoreOutLines.Add(tempbg.DuplicateCurve());
                    break;
                }

                tempbg2.Transform(Transform.Translation(Vector3d.YAxis * 2700));
                CoreOutLines.Add(tempbg.DuplicateCurve());
                CoreOutLines.Add(tempbg2.DuplicateCurve());
            }
            


            List<Polyline> Regs = new List<Polyline>();
            List<Curve> Guides = new List<Curve>();

            CoreOutLines.AddRange(CoreOutline);
            SectionLines.AddRange(HouseOutline);
            SectionLines.Add(SectionBasecurve);
            SectionLines.Add(right.ToNurbsCurve());
            SectionLines.Add(left.ToNurbsCurve());
            SectionLines.Add(right2.ToNurbsCurve());
            SectionLines.Add(left2.ToNurbsCurve());
            Hatchs.AddRange(ground);

            Regs.AddRange(reg);

            for (int i = 0; i < RegPoints.Count; i++)
            {
                int textdir = 1;
                if (RegPoints[i].X < HousewidthPoints[0].X)
                {
                    textdir = -1;
                }

                if ((RegPoints[i].X == SectionBasecurve.PointAtStart.X || RegPoints[i].X == SectionBasecurve.PointAtEnd.X))
                {
                    textdir *= -1;
                }

                Point3d plnorigin = RegPoints[i] + Vector3d.XAxis * textdir * 2000;

                Line textline = new Line(RegPoints[i], Vector3d.XAxis * textdir * 1000);


                Plane textpln = new Plane(plnorigin, Vector3d.XAxis, Vector3d.YAxis);

                Rhino.Display.Text3d text = new Rhino.Display.Text3d(Regname[i], textpln, 2000);


                RegsOutput.Add(textline.ToNurbsCurve());
                texts.Add(text);

                //drawnGuids.Add(LoadManager.getInstance().DrawObjectWithSpecificLayer(textline.ToNurbsCurve(), LoadManager.NamedLayer.Guide));
                //drawnGuids.Add(LoadManager.getInstance().DrawObjectWithSpecificLayer(text, LoadManager.NamedLayer.Guide));
            }

            for (int i = 0; i < AptLines.Count; i++)
            {
                Point3d heightstart;
                FloorPlan.Dimension2 heightdimension;
                
                if (sectiondir > 0)
                {
                    heightstart = HousewidthPoints[i * 2] - Vector3d.XAxis * 1000;
                    heightdimension = new FloorPlan.Dimension2(heightstart + Vector3d.YAxis * (pilHeight + (strs[i] - 1) * houseHeight), heightstart, 500);

                }
                else
                {
                    heightstart = HousewidthPoints[i * 2] + Vector3d.XAxis * 1000;
                    heightdimension = new FloorPlan.Dimension2(heightstart, heightstart + Vector3d.YAxis * (pilHeight + (strs[i] - 1) * houseHeight), 500);
                }

                Dimensions.Add(heightdimension);

                //drawnGuids.AddRange(heightdimension.Print());

            }
            //foreach (var p in HouseheightPoints)
            //{
            //    TextDot test = new TextDot("높이점", p);
            //    drawnGuids.Add(RhinoDoc.ActiveDoc.Objects.AddTextDot(test));
            //}

            for (int i = 0; i < HouseheightPoints.Count; i += 2)
            {
                //Point3d[] temp = { HouseheightPoints[i], HouseheightPoints[i + 1] };
                //Point3d side = HouseheightPoints[i] + Vector3d.YAxis * 1200 + Vector3d.XAxis * -50;
                //FloorPlan.Dimension heightdimension = new FloorPlan.Dimension(temp.ToList(), side, 2400);
                //LoadManager.getInstance().DrawObjectWithSpecificLayer(heightdimension.DimensionLine, LoadManager.NamedLayer.Guide);
                //LoadManager.getInstance().DrawObjectWithSpecificLayer(heightdimension.ExtensionLine, LoadManager.NamedLayer.Guide);
                //LoadManager.getInstance().DrawObjectWithSpecificLayer(heightdimension.NumberText, LoadManager.NamedLayer.Guide);
                FloorPlan.Dimension2 dim2 = new FloorPlan.Dimension2(HouseheightPoints[i], HouseheightPoints[i + 1]);


                Dimensions.Add(dim2);
                //drawnGuids.AddRange(dim2.Print());



            }
            int stackedhouse = 0;





            mirror(ref HousewidthPoints, sbcenter);

            if(is4core)
                mirror(ref CorewidthPoints, sbcenter);

            mirror(ref AptwidthPoints, sbcenter);
            mirror(ref BuildingwidthPoints, sbcenter);
            mirror(ref HousePoints, sbcenter);
           
           

            FloorPlan.Dimension2 buildingwidth = new FloorPlan.Dimension2(BuildingwidthPoints[1] + dimensionvec, BuildingwidthPoints[0] + dimensionvec, 2000);
            Dimensions.Add(buildingwidth);
            //drawnGuids.AddRange(buildingwidth.Print());
            for (int i = 0; i < 2; i++)
            {
                

                FloorPlan.Dimension2 housewidth = new FloorPlan.Dimension2(HousewidthPoints[i * 2 + 1] + dimensionvec, HousewidthPoints[i * 2] + dimensionvec, 500);
                FloorPlan.Dimension2 corewidth;
                if (CorewidthPoints.Count >= i * 2+1)
                {
                    corewidth = new FloorPlan.Dimension2(CorewidthPoints[i * 2 + 1] + dimensionvec, CorewidthPoints[i * 2] + dimensionvec, 500);
                    Dimensions.Add(corewidth);
                    //drawnGuids.AddRange(corewidth.Print());
                }
                FloorPlan.Dimension2 aptwidth = new FloorPlan.Dimension2(AptwidthPoints[i*2 +1] + dimensionvec, AptwidthPoints[i * 2 + 1] + dimensionvec, 1500);


                Dimensions.Add(aptwidth);
                Dimensions.Add(housewidth);
                //drawnGuids.AddRange(aptwidth.Print());
                //drawnGuids.AddRange(housewidth.Print());
                
                

                for (int j = 2; j < strs[0]+1; j++)
                {
                    int ho = j;
                    if (i == 1)
                        ho = strs[0] + 2 - j;
                    UnitInfo tempunitinfo = new UnitInfo(HousePoints[stackedhouse], ho, houseHeight);

                    Unitinfo.Add(tempunitinfo);
                    //drawnGuids.AddRange(tempunitinfo.Print());
                    stackedhouse++;
                }

                if (i + 1 < AptLines.Count)
                {
                    FloorPlan.Dimension2 aptdimension = new FloorPlan.Dimension2(HousewidthPoints[i * 2 + 2] + dimensionvec / 2, HousewidthPoints[i * 2 + 1] + dimensionvec / 2, 0);

                    Dimensions.Add(aptdimension);
                    //drawnGuids.AddRange(aptdimension.Print());
                }

            }


            if (realRoad[0] != 0)
            {
                Plane roadplane = new Plane(left.PointAt(0.5) + Vector3d.YAxis * 2500, Vector3d.XAxis, Vector3d.YAxis);
                Rhino.Display.Text3d road = new Rhino.Display.Text3d(roadwidth[0].ToString() + "m 도로", roadplane, 4500);

                texts.Add(road);
                //drawnGuids.Add(LoadManager.getInstance().DrawObjectWithSpecificLayer(road, LoadManager.NamedLayer.Guide));
            }

            if (realRoad[1] != 0)
            {
                Plane roadplane = new Plane(right.PointAt(0.5) + Vector3d.YAxis * 2500, Vector3d.XAxis, Vector3d.YAxis);
                Rhino.Display.Text3d road = new Rhino.Display.Text3d(roadwidth[1].ToString() + "m 도로", roadplane, 4500);

                texts.Add(road);
                drawnGuids.Add(LoadManager.getInstance().DrawObjectWithSpecificLayer(road, LoadManager.NamedLayer.Guide));
            }



            //foreach (var p in HousewidthPoints)
            //{

            //    //TextDot test = new TextDot("세대너비", p);
            //    //drawnGuids.Add(RhinoDoc.ActiveDoc.Objects.AddTextDot(test));
            //}

            //foreach (var p in CorewidthPoints)
            //{
            //    //TextDot test = new TextDot("코어너비", p);
            //    //drawnGuids.Add(RhinoDoc.ActiveDoc.Objects.AddTextDot(test));
            //}

            //foreach (var p in BuildingwidthPoints)
            //{
            //    //TextDot test = new TextDot("건물너비", p);
            //    //drawnGuids.Add(RhinoDoc.ActiveDoc.Objects.AddTextDot(test));
            //}








            foreach (var c in CoreOutLines)
            {
                drawnGuids.Add(RhinoDoc.ActiveDoc.Objects.AddCurve(c));
            }

            foreach (var c in SectionLines)
            {
                drawnGuids.Add(RhinoDoc.ActiveDoc.Objects.AddCurve(c));
            }

            foreach (var c in Hatchs)
            {
                drawnGuids.Add(RhinoDoc.ActiveDoc.Objects.AddHatch(c));
            }

            foreach (var c in Regs)
            {
                dotted(c.ToNurbsCurve(), 500);
            }

            //foreach (var c in lines)
            //{
            //    drawnGuids.Add(RhinoDoc.ActiveDoc.Objects.AddLine(c));
            //}
            foreach (var c in Dimensions)
            {
                drawnGuids.AddRange(c.Print());
            }
            foreach (var c in Unitinfo)
            {
                drawnGuids.AddRange(c.Print());
            }
            drawnGuids.AddRange(LoadManager.getInstance().DrawObjectWithSpecificLayer(texts, LoadManager.NamedLayer.Guide));


            drawnGuids.AddRange(LoadManager.getInstance().DrawObjectsWithSpecificLayer(this.Guides, LoadManager.NamedLayer.Guide));

            drawnGuids.Add(LoadManager.getInstance().DrawObjectWithSpecificLayer(Axis, LoadManager.NamedLayer.Guide));

            return drawnGuids;
            //A = CoreOutline;
            //B = HouseOutline;
            //C = inside;
            //D = Axis;
            //E = reg;
            //F = right;
            //AA = left;
            //AB = SectionBasecurve;
            //AC = lines;
            //AD = Axis.PointAtStart;
            //AE = SectionBasecurve.PointAtStart;
            //AF = isN;
            //BA = realRoad;
            //BB = NCindex;
            //BC = groundhatchbox;


        }

        private void mirror(ref List<Point3d> points , Point3d center)
        {
            Plane cp = new Plane(center, Vector3d.XAxis);
            int index = points.Count;
            for (int i = index-1; i >= 0; i--)
            {
                Point3d temp = new Point3d(points[i]);
                
                temp.Transform(Transform.Mirror(cp));

                points.Add(temp);
            }
        }

        private void mirror(ref List<Curve> curves, Point3d center)
        {
            Plane cp = new Plane(center, Vector3d.XAxis);
            int index = curves.Count;
            for (int i = index-1; i >= 0; i--)
            {
                Curve temp = curves[i].DuplicateCurve();

                temp.Transform(Transform.Mirror(cp));

                curves.Add(temp);
            }
        }



        private void dotted(Curve line, double t)
        {
            double length = line.GetLength();
            double t2 = t / 4;


            var shatter = line.DivideByLength(t2, true);
            var spltd = line.Split(shatter);
            for (int f = 0; f < spltd.Length; f++)
            {
                if (f % (t / t2) * 2 + 1 == t / t2 + 1)
                {
                    spltd[f] = null;
                }
            }

            var merged = Curve.JoinCurves(spltd);
            this.RegsOutput.AddRange(merged);

        }
        private void dotted(Line line, double t)
        {
            double length = line.Length;
            double t2 = t / 4;
            double x = length / t2;


            var shatter = line.ToNurbsCurve().DivideByLength(x, true);
            var spltd = line.ToNurbsCurve().Split(shatter);
            for (int f = 0; f < spltd.Length; f++)
            {
                if (f % (t / t2) * 2 + 1 == t / t2 + 1)
                {
                    spltd[f] = null;
                }
            }

            var merged = Curve.JoinCurves(spltd);
            this.RegsOutput.AddRange(merged);

        }
        private List<Curve> Offsets(List<Curve> aptlines, List<Point3d> dir, List<double> param)
        {
            List<Curve> result = new List<Curve>();
            for (int i = 0; i < aptlines.Count; i++)
            {
                var temp = aptlines[i].Offset(dir[i], Vector3d.ZAxis, param[i], 0, CurveOffsetCornerStyle.None);
                result.Add(temp[0]);
            }

            return result;
        }

    }

 

    

    class Section_Pattern4 : SectionBase
    {
        List<Point3d> HousePoints = new List<Point3d>();
        // % 2 . 0 = front 1 = back
        List<Point3d> CorewidthPoints = new List<Point3d>();
        // % 2 . 0 = front 1 = back
        List<Point3d> HousewidthPoints = new List<Point3d>();
        // % 2 . 0 = coreback , 1 = housefront
        List<Point3d> BuildingwidthPoints = new List<Point3d>();
        // % 2 . 0 = househeightbottom , 1 = househeighttop
        List<Point3d> AptwidthPoints = new List<Point3d>();
        // % 2 . 0 = aptwidth , 1 = aptwidth
        List<Point3d> HouseheightPoints = new List<Point3d>();

        List<Point3d> RegPoints = new List<Point3d>();
        List<string> Regname = new List<string>();


        //print these

        //dimension
        public override List<FloorPlan.Dimension2> Dimensions { get; set; }
        //sectioncurve
        public override List<Curve> SectionLines { get; set; }
        //outlinecurve
        public override List<Curve> CoreOutLines { get; set; }
        //regulation
        public override List<Curve> RegsOutput { get; set; }
        //text
        public override List<Rhino.Display.Text3d> texts { get; set; }
        //hatch
        public override List<Hatch> Hatchs { get; set; }
        //unitinfo
        public override List<UnitInfo> Unitinfo { get; set; }
        //boundingBox
        public override BoundingBox boundingBox { get; set; }

        List<Point3d> toboundingbox = new List<Point3d>();



        List<Curve> AptLines = null;
        List<double> x = null; //House Left
        List<double> y = null; //Core Left
        List<double> z = null; //House Right
        List<double> u = null; //Core Right
        List<int> strs = new List<int>();
        Curve Plot = null;
        List<int> surr = null;
        Point3d SectionBasePoint = Point3d.Unset;


        List<Polyline> Regs = new List<Polyline>();
        List<Curve> Guides = new List<Curve>();

        List<Guid> drawnGuids = new List<Guid>();

        //minx maxx miny maxy
        //
        List<Point3d> CenterlineCore = new List<Point3d>();

        public Section_Pattern4(Apartment agOut)
        {
            this.Dimensions = new List<FloorPlan.Dimension2>();
            this.SectionLines = new List<Curve>();
            this.CoreOutLines = new List<Curve>();
            this.RegsOutput = new List<Curve>();
            this.texts = new List<Rhino.Display.Text3d>();
            this.Hatchs = new List<Hatch>();
            this.Unitinfo = new List<UnitInfo>();
            this.boundingBox = new BoundingBox();

            this.AptLines = agOut.AptLines[0].DuplicateSegments().ToList();

            ///    0, 2 번 아파트라인 추출해서 자릅니다.

            //coreback = coreright
            List<double> temp = new List<double>();
            List<double> temp2 = new List<double>();
            List<double> widthtemp = new List<double>();
            for (int i = 0; i < agOut.Core.Count; i++)
            {
                Point3d coreOri = agOut.Core[i][0].Origin;
                double param;
                AptLines[i].ClosestPoint(coreOri, out param);
                Point3d aptClosest = AptLines[i].PointAt(param);
                temp.Add(Vector3d.Multiply(agOut.Core[i][0].YDirection, new Vector3d(coreOri - aptClosest)));
  
                widthtemp.Add(agOut.ParameterSet.Parameters[2] / 2);
            }

            List<Point3d> centercores = new List<Point3d>();
            for (int i = 0; i < agOut.Core[0].Count; i++)
            {
                Point3d coreOri = agOut.Core[0][i].Origin;
                double param;
                AptLines[1].ClosestPoint(coreOri, out param);
                Point3d aptClosest = AptLines[1].PointAt(param);
                //corewidth = 2500

                if (coreOri.DistanceTo(aptClosest) <= widthtemp[0] * 1.1)
                {
                    centercores.Add(aptClosest);
                } 
               
            }



            this.x = widthtemp.Select(n => -n).ToList();
            this.z = widthtemp;
            this.CenterlineCore = centercores;

            //this.u = temp;
            //corefront = coreleft

            for (int i = 0; i < agOut.Core[0].Count; i++)
            {
               // temp2.AddRange(u.Select(n => n + agOut.Core[i][0].CoreType.GetDepth()).ToList());
                strs.Add(Convert.ToInt32(Math.Round(agOut.Core[0][i].Stories + 1)));
            }
           //this.y = temp2;


            Plot = agOut.Plot.Boundary;
            surr = agOut.Plot.Surroundings.ToList();

            SectionBasePoint = Point3d.Origin;

        }

        public override List<Guid> DrawSection(bool northline)
        {

            double slavHeight = 300;
            double houseHeight = 2700;
            double pilHeight = 3300;
            double wallThick = 300;
            //mainsection
            List<Curve> offsetcurves = new List<Curve>();
            //bg
            List<Curve> offsetcurves2 = new List<Curve>();

            Curve Axis = null;
            List<Point3d> dirPoint = new List<Point3d>();
            List<Point3d> cPs = new List<Point3d>();
            List<Vector3d> dirVector = new List<Vector3d>();



            //아파트 라인 0의 수직인 방향 점과 방향 벡터 구함

            cPs.Add(AptLines[2].PointAtNormalizedLength(0.5));
            Curve tempC = AptLines[2].DuplicateCurve();

            tempC.Rotate(RhinoMath.ToRadians(-90), Vector3d.ZAxis, AptLines[0].PointAtNormalizedLength(0.5));
            dirVector.Add(new Vector3d(tempC.PointAtEnd - tempC.PointAtStart));
            dirPoint.Add(tempC.PointAtEnd);


            ///아파트 라인 1의 중심에서 수직인 선을 만들고 축선으로 쓴다.

            for (int i = 0; i < cPs.Count; i++)
            {
                Line templine = new Line(cPs[i], dirVector[i] * 100);
                Curve tempCurve = templine.ToNurbsCurve();
                tempCurve.Translate(new Vector3d(-dirVector[i]) * 50);
                Axis = tempCurve;
            }
            //만약 축선이 안구해지면 리턴
            if (Axis == null)
                return null;


            //북향 대지 경계 찾기.
            List<int> NCindex = new List<int>();

            for (int i = 0; i < Plot.DuplicateSegments().Length; i++)
            {
                if (Plot.DuplicateSegments()[i].TangentAt(0.5).X > 0)
                    NCindex.Add(i);

            }

            ///ag3 only// 센터점 구함
            ///
            List<Point3d> toCenter = new List<Point3d>();
            for (int i = 0; i < AptLines.Count; i++)
            {
                var intersection = Rhino.Geometry.Intersect.Intersection.CurveCurve(Axis, AptLines[i], 0, 0);
                if (intersection.Count > 0)
                    toCenter.Add(intersection[0].PointA);
            }
            if (toCenter.Count != 2)
                return null;

            Line centerline = new Line(toCenter[0], toCenter[1]);
            Point3d CenterPoint = centerline.PointAt(0.5);



            //구한 축선을 대지에 맞게 자르고 아파트라인, 코어라인을 그어준다.
            //만약 접한 경계가 도로라면 경계와 접한 도로 너비의 값을 축선에 보정

            List<double> splitparam = new List<double>();
            var plotseg = Plot.DuplicateSegments();
            double[] realRoad = new double[2];
            double[] roadwidth = new double[2];
            bool[] isN = new bool[2];
            double[] sangle = new double[2];
            double tangle = 0;
            int arrayindex = 1;

            for (int i = 0; i < plotseg.Length; i++)
            {

                var intersection2 = Rhino.Geometry.Intersect.Intersection.CurveCurve(Axis, plotseg[i], 0, 0);
                int dir = 1;
                double tempd = surr[i];
                foreach (var intersect in intersection2)
                {


                    if (intersect.PointA.DistanceTo(Axis.PointAtStart) < intersect.PointA.DistanceTo(Axis.PointAtEnd))
                    {
                        dir = -1;
                        arrayindex = 0;
                    }
                    else
                    {
                        dir = 1;
                        arrayindex = 1;
                    }

                    if (NCindex.Contains(i))
                    {
                        isN[arrayindex] = true;
                    }
                    else
                        isN[arrayindex] = false;



                    tangle = Vector3d.VectorAngle(Axis.TangentAtEnd, plotseg[i].TangentAtEnd);
                    realRoad[arrayindex] = tempd * dir / Math.Sin(tangle);
                    roadwidth[arrayindex] = tempd;
                    double temprad = Vector3d.VectorAngle(plotseg[i].TangentAtEnd, Vector3d.YAxis);
                    double temprad2 = Vector3d.VectorAngle(Axis.TangentAtEnd, plotseg[i].TangentAtEnd);

                    double tempsinval = Math.Abs(Math.Sin(temprad)) / Math.Abs(Math.Sin(temprad2));
                    //sangle[arrayindex] = Math.Abs(Math.Cos(temprad));
                    sangle[arrayindex] = tempsinval;
                    splitparam.Add(intersect.ParameterA);

                }

            }

           

            Axis = Axis.Split(splitparam)[1];




            Interval newdomain = new Interval(0, 1);
            Axis.Domain = newdomain;
            AptLines.RemoveRange(1, 2);

            List<double> prms = new List<double>();
            foreach (var p in CenterlineCore)
            {
                double prm;
                Axis.ClosestPoint(p, out prm);
                prms.Add(prm);
            }

          


            //AptLines.RemoveAt(2);
            //AptLines.RemoveAt(1);

            offsetcurves.AddRange(Offsets(AptLines, dirPoint, x));
            offsetcurves.AddRange(Offsets(AptLines, dirPoint, z));
            //offsetcurves.AddRange(Offsets(AptLines, dirPoint, y));
            //offsetcurves.AddRange(Offsets(AptLines, dirPoint, u));





            //단면 대지선을 지정한 단면 포인트로부터 만들어주고 축선과 offset라인의 교점의 값을 sectionbase에 저장.



            List<double> sectionbase = new List<double>();

            foreach (Curve c in offsetcurves)
            {
                var intersection3 = Rhino.Geometry.Intersect.Intersection.CurveCurve(Axis, c, 0, 0);
                foreach (var intersect in intersection3)
                {
                    sectionbase.Add(intersect.ParameterA);
                }

            }



            //단면 대지선을 그려주고 sectionbase값에 맞춰 잘라줌.


            double sectiondir = 1;


            if (Axis.TangentAtEnd.X == 0 && Axis.PointAtStart.Y > Axis.PointAtEnd.Y)
                sectiondir = 1;

            else if (Axis.TangentAtEnd.X == 0 && Axis.PointAtStart.Y < Axis.PointAtEnd.Y)
                sectiondir = -1;

            else
                sectiondir = Axis.TangentAtEnd.X / Math.Abs(Axis.TangentAtEnd.X);

            Line SectionBaseline = new Line(SectionBasePoint, Vector3d.XAxis * sectiondir, new Vector3d(Axis.PointAtEnd - Axis.PointAtStart).Length);


            //RhinoApp.WriteLine(Axis.TangentAtEnd.ToString());
            //RhinoApp.WriteLine(sectiondir.ToString());

            Curve SectionBasecurve = SectionBaseline.ToNurbsCurve();

            if (sectiondir == -1)
                SectionBasecurve.Translate(Vector3d.XAxis * SectionBaseline.Length);


            //코어그려두기

            

            var spltSB = SectionBasecurve.Split(sectionbase);

            double param;
            Axis.ClosestPoint(CenterPoint, out param);

            Point3d sbcenter = SectionBasecurve.PointAt(param);

            //단면 대지선의 양 사이드에 먼저 추출해둔 도로선만큼 확장 해줌.


            Line left = new Line(SectionBasecurve.PointAtStart, Vector3d.XAxis * sectiondir * realRoad[0]);
            Line right = new Line(SectionBasecurve.PointAtEnd, Vector3d.XAxis * sectiondir * realRoad[1]);



            Line left2 = new Line(left.To, Vector3d.XAxis * sectiondir * -5000);
            Line right2 = new Line(right.To, Vector3d.XAxis * sectiondir * 5000);

            toboundingbox.Add(left2.To + Vector3d.YAxis * -5000);
            toboundingbox.Add(right2.To + Vector3d.YAxis * -5000);
            // boundingbox 만들기
            toboundingbox.Add(Point3d.Origin + Vector3d.YAxis * (strs[0] * 2700 + 15000));

            boundingBox = new BoundingBox(toboundingbox);

            //hatch 만들기

            double hatchheight = -5000;

            List<Point3d> groundoutrect = new List<Point3d>();
            groundoutrect.Add(left2.To);
            groundoutrect.Add(left2.To + Vector3d.YAxis * hatchheight);
            groundoutrect.Add(right2.To + Vector3d.YAxis * hatchheight);
            groundoutrect.Add(right2.To);
            groundoutrect.Add(left2.To);
            Curve groundhatchbox = new PolylineCurve(groundoutrect) as Curve;

            groundhatchbox.MakeClosed(1000);

            var ground = Hatch.Create(groundhatchbox, 1, RhinoMath.ToRadians(45), 10000);
            for (int i = 0; i < ground.Length; i++)
                //RhinoDoc.ActiveDoc.Objects.AddHatch(ground[i]);
                if (ground.Length > 0)
                    RhinoDoc.ActiveDoc.Views.Redraw();





            //대지경계 / 인접대지경계 선 출력

            List<Line> lines = new List<Line>();

            Line tempbline = Line.Unset;

            Point3d[] nearplot = new Point3d[2];
            tempbline = new Line(SectionBasecurve.PointAtStart, Vector3d.YAxis * 5000);
            lines.Add(tempbline);
            dotted(tempbline, 500);
            nearplot[0] = tempbline.To;
            if (left.Length != 0)
            {
                Regname.Add("대지경계");
                RegPoints.Add(tempbline.PointAt(0.5));
                tempbline = new Line(left.To, Vector3d.YAxis * 5000);
                lines.Add(tempbline);

                dotted(tempbline, 500);

                nearplot[0] = tempbline.To;
            }


            Regname.Add("인접대지경계");
            RegPoints.Add(tempbline.PointAt(0.5));

            tempbline = new Line(SectionBasecurve.PointAtEnd, Vector3d.YAxis * 5000);
            lines.Add(tempbline);
            dotted(tempbline, 500);
            nearplot[1] = tempbline.To;
            if (right.Length != 0)
            {
                Regname.Add("대지경계");
                RegPoints.Add(tempbline.PointAt(0.5));
                tempbline = new Line(right.To, Vector3d.YAxis * 5000);
                lines.Add(tempbline);
                dotted(tempbline, 500);
                nearplot[1] = tempbline.To;
            }

            Regname.Add("인접대지경계");
            RegPoints.Add(tempbline.PointAt(0.5));

            //법규선 시작점 설정
            Point3d fromstart = SectionBasecurve.PointAtStart + Vector3d.XAxis * sectiondir * realRoad[0] / 2;
            Point3d fromend = SectionBasecurve.PointAtEnd + Vector3d.XAxis * sectiondir * realRoad[1] / 2;

            //법규선 생성


            List<Polyline> reg = new List<Polyline>();
            List<Point3d> toPoly = new List<Point3d>();
            Point3d tempp = Point3d.Unset;

            if (northline)
            {
                if (isN[0])
                {
                    toPoly.Add(fromstart);
                    tempp = fromstart + Vector3d.XAxis * sangle[0] * 1500 * sectiondir;
                    toPoly.Add(tempp);
                    tempp = tempp + Vector3d.YAxis * 9000;
                    toPoly.Add(tempp);
                    tempp = tempp + Vector3d.XAxis * sangle[0] * 3000 * sectiondir;
                    toPoly.Add(tempp);
                    tempp = tempp + new Vector3d(sangle[0] * sectiondir, 2, 0) * 9600;
                    toPoly.Add(tempp);

                    //이름붙이기
                    tempp = tempp + new Vector3d(sangle[0] * sectiondir, 2, 0) * -4800;
                    RegPoints.Add(tempp);
                    Regname.Add("정북방향 일조사선");

                    reg.Add(new Polyline(toPoly));
                }

                toPoly.Clear();
                if (nearplot[0] == null)
                    tempp = fromstart + Vector3d.YAxis * 3300;
                else
                {
                    tempp = new Point3d(nearplot[0].X, 3300, 0);
                }

                toPoly.Add(tempp);
                tempp = tempp + new Vector3d(1 * sectiondir, 4, 0) * 9600;
                toPoly.Add(tempp);

                reg.Add(new Polyline(toPoly));

                tempp = tempp + new Vector3d(1 * sectiondir, 4, 0) * -4800;
                RegPoints.Add(tempp);
                Regname.Add("채광방향 높이제한");


                toPoly.Clear();

                if (isN[1])
                {
                    toPoly.Add(fromend);
                    tempp = fromend + Vector3d.XAxis * -sangle[1] * 1500 * sectiondir;
                    toPoly.Add(tempp);
                    tempp = tempp + Vector3d.YAxis * 9000;
                    toPoly.Add(tempp);
                    tempp = tempp + Vector3d.XAxis * -sangle[1] * 3000 * sectiondir;
                    toPoly.Add(tempp);
                    tempp = tempp + new Vector3d(-sangle[1] * sectiondir, 2, 0) * 9600;
                    toPoly.Add(tempp);

                    tempp = tempp + new Vector3d(-sangle[0] * sectiondir, 2, 0) * -4800;
                    RegPoints.Add(tempp);
                    Regname.Add("정북방향 일조사선");

                    reg.Add(new Polyline(toPoly));
                }

                toPoly.Clear();



                if (nearplot[1] == null)
                    tempp = fromend + Vector3d.YAxis * 3300;
                else
                    tempp = new Point3d(nearplot[1].X, 3300, 0);
                toPoly.Add(tempp);
                tempp = tempp + new Vector3d(-1 * sectiondir, 4, 0) * 9600;
                toPoly.Add(tempp);

                reg.Add(new Polyline(toPoly));


                toPoly.Clear();


                tempp = tempp + new Vector3d(-1 * sectiondir, 4, 0) * -4800;
                RegPoints.Add(tempp);
                Regname.Add("채광방향 높이제한");

            }


            //자른 선분들의 끝점을 아파트선, 코어선 시작 리스트에 저장
            List<Point3d> CoreStart = new List<Point3d>();
            List<Point3d> HouseStart = new List<Point3d>();


            for (int i = 0; i < spltSB.Length - 1; i++)
            {
                //짝수 앞
                if (i % 2 == 0)
                {
                    CoreStart.Add(spltSB[i].PointAtEnd);
                    HouseStart.Add(spltSB[i].PointAtEnd);
                    HousewidthPoints.Add(spltSB[i].PointAtEnd);
                    CorewidthPoints.Add(spltSB[i].PointAtEnd);
                    // 건물 총 너비
                    if (i == 0)
                    {
                        BuildingwidthPoints.Add(spltSB[i].PointAtEnd);
                        AptwidthPoints.Add(spltSB[i].PointAtEnd);
                    }

                }

                //홀수 뒤
                else
                {
                    CoreStart.Add(spltSB[i].PointAtEnd);
                    HouseStart.Add(spltSB[i].PointAtEnd);
                    HousewidthPoints.Add(spltSB[i].PointAtEnd);
                    CorewidthPoints.Add(spltSB[i].PointAtEnd);
                    if (i == spltSB.Length - 1)
                    {

                    }
                }
            }

            //아파트와 코어의 외곽선을 그려준다.

            List<Curve> HouseOutline = new List<Curve>();
            List<Curve> CoreOutline = new List<Curve>();

            double AptHeight = 0;

            for (int j = 0; j < strs[0]; j++)
            {
                if (j != 0)
                    AptHeight += houseHeight;
            }

            double CoreHeight = AptHeight + houseHeight + pilHeight;

            Line AptSide1 = new Line(HouseStart[0] + Vector3d.YAxis * (pilHeight - 300), Vector3d.YAxis * (AptHeight + 300));
            Line AptSide2 = new Line(HouseStart[1] + Vector3d.YAxis * (pilHeight - 300), Vector3d.YAxis * (AptHeight + 300));
            Line AptUp = new Line(AptSide1.ToNurbsCurve().PointAtEnd, AptSide2.ToNurbsCurve().PointAtEnd);
            Line AptDown = new Line(AptSide1.ToNurbsCurve().PointAtStart, AptSide2.ToNurbsCurve().PointAtStart);

            HouseOutline.Add(AptSide1.ToNurbsCurve());
            HouseOutline.Add(AptSide2.ToNurbsCurve());
            HouseOutline.Add(AptUp.ToNurbsCurve());
            HouseOutline.Add(AptDown.ToNurbsCurve());



            Line CoreSide1 = new Line(CoreStart[0], Vector3d.YAxis * CoreHeight);
            Line CoreSide2 = new Line(CoreStart[1], Vector3d.YAxis * CoreHeight);
            Line CoreUp = new Line(CoreSide1.ToNurbsCurve().PointAtEnd, CoreSide2.ToNurbsCurve().PointAtEnd);
            Line CoreDown = new Line(CoreSide1.ToNurbsCurve().PointAtStart, CoreSide2.ToNurbsCurve().PointAtStart);

            CoreOutline.Add(CoreSide1.ToNurbsCurve());
            CoreOutline.Add(CoreSide2.ToNurbsCurve());
            CoreOutline.Add(CoreUp.ToNurbsCurve());
            CoreOutline.Add(CoreDown.ToNurbsCurve());




            //아웃라인에서 겹치는 선 제거.

            List<Curve> inHouse = new List<Curve>();
            inHouse.AddRange(HouseOutline);
            CoreOutline = Curve.JoinCurves(CoreOutline).ToList();
            HouseOutline = Curve.JoinCurves(HouseOutline).ToList();

            for (int i = 0; i < HouseOutline.Count; i++)
            {
                splitparam.Clear();

                var intersection4 = Rhino.Geometry.Intersect.Intersection.CurveCurve(CoreOutline[i], HouseOutline[i], 0, 0);
                foreach (var intersect in intersection4)
                {
                    splitparam.Add(intersect.ParameterA);
                }

                var spltd = CoreOutline[i].Split(splitparam);

                List<Curve> tempc = new List<Curve>();
                foreach (var sc in spltd)
                {
                    sc.Domain = newdomain;
                    if (HouseOutline[i].Contains(sc.PointAt(0.5)) != PointContainment.Inside)
                        tempc.Add(sc);
                }

                CoreOutline[i] = Curve.JoinCurves(tempc)[0];

            }

            foreach (var d in prms)
            {
                List<Point3d> topoly = new List<Point3d>();

                Point3d origin = SectionBasecurve.PointAt(d) ;
                topoly.Add(origin);
                topoly.Add(new Point3d(origin + Vector3d.XAxis * sectiondir * 2500));
                topoly.Add(new Point3d(origin + Vector3d.XAxis * sectiondir * 2500 + Vector3d.YAxis * (strs[0] * houseHeight + pilHeight)));
                topoly.Add(new Point3d(origin + Vector3d.YAxis * (strs[0] * houseHeight + pilHeight)));
                topoly.Add(origin);
                PolylineCurve coreoutline = new PolylineCurve(topoly);
                CoreOutLines.Add(coreoutline);

                

            }


            List<Curve> outputs = new List<Curve>();
            Vector3d pv = sbcenter - HouseStart[1];
            Curve houseoutline0 = new LineCurve(HouseStart[1], new Point3d(HouseStart[1] + 2 * pv));
            houseoutline0.Translate(Vector3d.YAxis * 3000);


            List<double> oprm = new List<double>();
            for (int i = 0; i < CoreOutLines.Count; i++)
            {
                var intersec = Rhino.Geometry.Intersect.Intersection.CurveCurve(houseoutline0, CoreOutLines[i], 0, 0);
                
                if(intersec.Count != 0)
                {
                    foreach (var intsct in intersec)
                    {
                        oprm.Add(intsct.ParameterA);
                    }
                }
            }

            var osplt = houseoutline0.Split(oprm);
            for (int i = 0; i < osplt.Length; i++)
            {
                if (i % 2 == 0)
                {
                    outputs.Add(osplt[i]);
                    Curve tempc = osplt[i].DuplicateCurve();
                    tempc.Translate(Vector3d.YAxis *( (strs[0] - 1) * houseHeight + 300));
                    outputs.Add(tempc);


                }
                    
            }





            


            mirror(ref CoreOutline, sbcenter);

            CoreOutline.AddRange(outputs);

            //내부 선 만들어줌
            List<Curve> inside = new List<Curve>();


            List<Curve> unit = new List<Curve>();
            Curve tempunit = null;

            Curve temp;

            temp = inHouse[3].DuplicateCurve();
            temp.SetStartPoint(temp.PointAtStart + Vector3d.XAxis * sectiondir * wallThick);
            temp.SetEndPoint(temp.PointAtEnd + Vector3d.XAxis * sectiondir * -wallThick);

            temp.Translate(Vector3d.YAxis * slavHeight);
            unit.Add(temp);
            temp = temp.DuplicateCurve();
            temp.Translate(Vector3d.YAxis * (houseHeight - slavHeight));
            unit.Add(temp);

            Line temp1;
            temp1 = new Line(unit[0].PointAtStart, unit[1].PointAtStart);
            unit.Add(temp1.ToNurbsCurve());
            temp1 = new Line(unit[0].PointAtEnd, unit[1].PointAtEnd);
            unit.Add(temp1.ToNurbsCurve());


            List<Curve> toHatch = new List<Curve>();

            toHatch.Add(HouseOutline[0]);
            List<Curve> path = new List<Curve>();
            for (int j = 0; j < strs[0] - 1; j++)
            {

                tempunit = Curve.JoinCurves(unit)[0];
                tempunit.Translate(Vector3d.YAxis * houseHeight * j);




                inside.Add(tempunit);
                toHatch.Add(tempunit);
                RhinoDoc.ActiveDoc.Objects.AddCurve(tempunit);

                //복도
                //List<Point3d> topoly = new List<Point3d>();
                //Point3d temppp = new Point3d(tempunit.PointAtStart) + Vector3d.XAxis * sectiondir * wallThick;
                //topoly.Add(temppp);
                //temppp = temppp + Vector3d.XAxis * sectiondir * 1300;
                //topoly.Add(temppp);
                //temppp = temppp + Vector3d.YAxis * 1000;
                //topoly.Add(temppp);
                //temppp = temppp + Vector3d.XAxis * sectiondir * wallThick;
                //topoly.Add(temppp);
                //temppp = temppp + Vector3d.YAxis * -(1000 + slavHeight);
                //topoly.Add(temppp);
                //temppp = temppp + Vector3d.XAxis * sectiondir * -(1300 + wallThick);
                //topoly.Add(temppp);
                //topoly.Add(topoly[0]);
                //Polyline tempoly = new Polyline(topoly);
                //path.Add(tempoly.ToNurbsCurve());
                //var thatch = Hatch.Create(tempoly.ToNurbsCurve(), 0, 0, 1000);
                //Hatchs.AddRange(thatch);


                // 세대 중심
                Polyline tempunitpl;
                tempunit.TryGetPolyline(out tempunitpl);
                HousePoints.Add(tempunitpl.CenterPoint());

                // 세대 높이선(디멘션 용 점2개)
                HouseheightPoints.Add(tempunitpl.CenterPoint() + (Vector3d.XAxis * (temp.GetLength() / 4) + Vector3d.YAxis * (houseHeight / 2 - slavHeight / 2)));
                HouseheightPoints.Add(tempunitpl.CenterPoint() + (Vector3d.XAxis * (temp.GetLength() / 4) - Vector3d.YAxis * (houseHeight / 2 - slavHeight / 2)));

                //Point3d unitheightbottom = tempunitpl.CenterPoint() + Vector3d.XAxis * 
            }


            mirror(ref toHatch, sbcenter);
            

            var hatch = Hatch.Create(toHatch, 0, 0, 1000);

            Hatchs.AddRange(hatch);

            //mirror(ref path, sbcenter);
            //foreach (var c in path)
            //{
            //    var thatch = Hatch.Create(c, 0, 0, 1000);
            //    Hatchs.AddRange(thatch);
            //}


            //List<FloorPlan.Dimension2> dimensions = new List<FloorPlan.Dimension2>();
            FloorPlan.Dimension2 tempdim;
            Line bgoutlinebase;
            Line bgoutline2base;
            double dimensionoffset = pilHeight + houseHeight * strs[0] + 300;
            Vector3d dimensionvec = Vector3d.YAxis * dimensionoffset;


            //if (this.is4core)
            //{
            //    mirror(ref CoreOutline, sbcenter);

            //    tempdim = new FloorPlan.Dimension2(CoreSide2.From + dimensionvec, CoreSide2.From + dimensionvec + Vector3d.XAxis * (sbcenter.X - CoreSide2.FromX) * 2, 500);
            //    dimensions.Add(tempdim);

            //    bgoutlinebase = new Line(CoreSide2.From, Vector3d.XAxis * (sbcenter.X - CoreSide2.FromX) * 2);
            //    bgoutline2base = bgoutlinebase;
            //}

            //else
            //{
            //    tempdim = new FloorPlan.Dimension2(CoreSide2.From + dimensionvec, CoreSide2.From + dimensionvec + Vector3d.XAxis * ((sbcenter.X - CoreSide2.FromX) * 2 + (CoreSide2.FromX - CoreSide1.FromX)), 500);
            //    dimensions.Add(tempdim);
            //    bgoutlinebase = new Line(CoreSide2.From, Vector3d.XAxis * ((sbcenter.X - CoreSide2.FromX) * 2 + CoreSide2.FromX - CoreSide1.FromX));
            //    bgoutline2base = new Line(CoreSide2.From, Vector3d.XAxis * ((sbcenter.X - CoreSide2.FromX) * 2 + CoreSide2.FromX - CoreSide1.FromX + 1600));
            //}

            tempdim = new FloorPlan.Dimension2(AptSide2.From + dimensionvec - Vector3d.YAxis * 3000, AptSide2.From + dimensionvec + Vector3d.XAxis * (sbcenter.X - AptSide2.FromX) * 2 - Vector3d.YAxis * 3000, 500);
            Dimensions.Add(tempdim);

            //pil
            //bgoutlinebase.Transform(Transform.Translation(Vector3d.YAxis * 3000));
            //bgoutline2base.Transform(Transform.Translation(Vector3d.YAxis * 4300));

            Curve tempbg;
            Curve tempbg2;
            //tempbg = bgoutlinebase.ToNurbsCurve().DuplicateCurve();
            //tempbg2 = bgoutline2base.ToNurbsCurve().DuplicateCurve();
            //CoreOutLines.Add(tempbg.DuplicateCurve());
            //CoreOutLines.Add(tempbg2.DuplicateCurve());
            //for (int i = 0; i < strs[0] - 1; i++)
            //{

            //    tempbg.Transform(Transform.Translation(Vector3d.YAxis * 2700));
            //    if (i == strs[0] - 2)
            //    {
            //        CoreOutLines.Add(tempbg.DuplicateCurve());
            //        tempbg.Transform(Transform.Translation(Vector3d.YAxis * 300));
            //        CoreOutLines.Add(tempbg.DuplicateCurve());
            //        break;
            //    }

            //    tempbg2.Transform(Transform.Translation(Vector3d.YAxis * 2700));
            //    CoreOutLines.Add(tempbg.DuplicateCurve());
            //    CoreOutLines.Add(tempbg2.DuplicateCurve());
            //}




            List<Polyline> Regs = new List<Polyline>();
            List<Curve> Guides = new List<Curve>();

            CoreOutLines.AddRange(CoreOutline);
            SectionLines.AddRange(HouseOutline);
            SectionLines.Add(SectionBasecurve);
            SectionLines.Add(right.ToNurbsCurve());
            SectionLines.Add(left.ToNurbsCurve());
            SectionLines.Add(right2.ToNurbsCurve());
            SectionLines.Add(left2.ToNurbsCurve());
            Hatchs.AddRange(ground);

            Regs.AddRange(reg);

            for (int i = 0; i < RegPoints.Count; i++)
            {
                int textdir = 1;
                if (RegPoints[i].X < HousewidthPoints[0].X)
                {
                    textdir = -1;
                }

                if ((RegPoints[i].X == SectionBasecurve.PointAtStart.X || RegPoints[i].X == SectionBasecurve.PointAtEnd.X))
                {
                    textdir *= -1;
                }

                Point3d plnorigin = RegPoints[i] + Vector3d.XAxis * textdir * 2000;

                Line textline = new Line(RegPoints[i], Vector3d.XAxis * textdir * 1000);


                Plane textpln = new Plane(plnorigin, Vector3d.XAxis, Vector3d.YAxis);

                Rhino.Display.Text3d text = new Rhino.Display.Text3d(Regname[i], textpln, 2000);


                RegsOutput.Add(textline.ToNurbsCurve());
                texts.Add(text);

                //drawnGuids.Add(LoadManager.getInstance().DrawObjectWithSpecificLayer(textline.ToNurbsCurve(), LoadManager.NamedLayer.Guide));
                //drawnGuids.Add(LoadManager.getInstance().DrawObjectWithSpecificLayer(text, LoadManager.NamedLayer.Guide));
            }

            for (int i = 0; i < AptLines.Count; i++)
            {
                Point3d heightstart;
                FloorPlan.Dimension2 heightdimension;

                if (sectiondir > 0)
                {
                    heightstart = HousewidthPoints[i * 2] - Vector3d.XAxis * 1000;
                    heightdimension = new FloorPlan.Dimension2(heightstart + Vector3d.YAxis * (pilHeight + (strs[i] - 1) * houseHeight), heightstart, 500);

                }
                else
                {
                    heightstart = HousewidthPoints[i * 2] + Vector3d.XAxis * 1000;
                    heightdimension = new FloorPlan.Dimension2(heightstart, heightstart + Vector3d.YAxis * (pilHeight + (strs[i] - 1) * houseHeight), 500);
                }

                Dimensions.Add(heightdimension);

                //drawnGuids.AddRange(heightdimension.Print());

            }
            //foreach (var p in HouseheightPoints)
            //{
            //    TextDot test = new TextDot("높이점", p);
            //    drawnGuids.Add(RhinoDoc.ActiveDoc.Objects.AddTextDot(test));
            //}

            for (int i = 0; i < HouseheightPoints.Count; i += 2)
            {
                //Point3d[] temp = { HouseheightPoints[i], HouseheightPoints[i + 1] };
                //Point3d side = HouseheightPoints[i] + Vector3d.YAxis * 1200 + Vector3d.XAxis * -50;
                //FloorPlan.Dimension heightdimension = new FloorPlan.Dimension(temp.ToList(), side, 2400);
                //LoadManager.getInstance().DrawObjectWithSpecificLayer(heightdimension.DimensionLine, LoadManager.NamedLayer.Guide);
                //LoadManager.getInstance().DrawObjectWithSpecificLayer(heightdimension.ExtensionLine, LoadManager.NamedLayer.Guide);
                //LoadManager.getInstance().DrawObjectWithSpecificLayer(heightdimension.NumberText, LoadManager.NamedLayer.Guide);
                FloorPlan.Dimension2 dim2 = new FloorPlan.Dimension2(HouseheightPoints[i], HouseheightPoints[i + 1]);


                Dimensions.Add(dim2);
                //drawnGuids.AddRange(dim2.Print());



            }
            int stackedhouse = 0;





            mirror(ref HousewidthPoints, sbcenter);

            
            mirror(ref CorewidthPoints, sbcenter);

            mirror(ref AptwidthPoints, sbcenter);
            mirror(ref BuildingwidthPoints, sbcenter);
            mirror(ref HousePoints, sbcenter);



            FloorPlan.Dimension2 buildingwidth = new FloorPlan.Dimension2(BuildingwidthPoints[1] + dimensionvec, BuildingwidthPoints[0] + dimensionvec, 1000);
            Dimensions.Add(buildingwidth);
            //drawnGuids.AddRange(buildingwidth.Print());
            for (int i = 0; i < 2; i++)
            {


                FloorPlan.Dimension2 housewidth = new FloorPlan.Dimension2(HousewidthPoints[i * 2 + 1] + dimensionvec, HousewidthPoints[i * 2] + dimensionvec, 500);
                FloorPlan.Dimension2 corewidth;
                if (CorewidthPoints.Count >= i * 2 + 1)
                {
                    corewidth = new FloorPlan.Dimension2(CorewidthPoints[i * 2 + 1] + dimensionvec, CorewidthPoints[i * 2] + dimensionvec, 500);
                    Dimensions.Add(corewidth);
                    //drawnGuids.AddRange(corewidth.Print());
                }
                //FloorPlan.Dimension2 aptwidth = new FloorPlan.Dimension2(AptwidthPoints[i * 2 + 1] + dimensionvec, AptwidthPoints[i * 2 + 1] + dimensionvec, 1500);


                //Dimensions.Add(aptwidth);


                Dimensions.Add(housewidth);
                //drawnGuids.AddRange(aptwidth.Print());
                //drawnGuids.AddRange(housewidth.Print());



                for (int j = 2; j < strs[0] + 1; j++)
                {
                    int ho = j;
                    if (i == 1)
                        ho = strs[0] + 2 - j;
                    UnitInfo tempunitinfo = new UnitInfo(HousePoints[stackedhouse], ho, houseHeight);

                    Unitinfo.Add(tempunitinfo);
                    //drawnGuids.AddRange(tempunitinfo.Print());
                    stackedhouse++;
                }

                if (i + 1 < AptLines.Count)
                {
                    //FloorPlan.Dimension2 aptdimension = new FloorPlan.Dimension2(HousewidthPoints[i * 2 + 2] + dimensionvec / 2, HousewidthPoints[i * 2 + 1] + dimensionvec / 2, 0);

                    //Dimensions.Add(aptdimension);
                    //drawnGuids.AddRange(aptdimension.Print());
                }

            }


            if (realRoad[0] != 0)
            {
                Plane roadplane = new Plane(left.PointAt(0.5) + Vector3d.YAxis * 2500, Vector3d.XAxis, Vector3d.YAxis);
                Rhino.Display.Text3d road = new Rhino.Display.Text3d(roadwidth[0].ToString() + "m 도로", roadplane, 4500);

                texts.Add(road);
                //drawnGuids.Add(LoadManager.getInstance().DrawObjectWithSpecificLayer(road, LoadManager.NamedLayer.Guide));
            }

            if (realRoad[1] != 0)
            {
                Plane roadplane = new Plane(right.PointAt(0.5) + Vector3d.YAxis * 2500, Vector3d.XAxis, Vector3d.YAxis);
                Rhino.Display.Text3d road = new Rhino.Display.Text3d(roadwidth[1].ToString() + "m 도로", roadplane, 4500);

                texts.Add(road);
                drawnGuids.Add(LoadManager.getInstance().DrawObjectWithSpecificLayer(road, LoadManager.NamedLayer.Guide));
            }



            //foreach (var p in HousewidthPoints)
            //{

            //    //TextDot test = new TextDot("세대너비", p);
            //    //drawnGuids.Add(RhinoDoc.ActiveDoc.Objects.AddTextDot(test));
            //}

            //foreach (var p in CorewidthPoints)
            //{
            //    //TextDot test = new TextDot("코어너비", p);
            //    //drawnGuids.Add(RhinoDoc.ActiveDoc.Objects.AddTextDot(test));
            //}

            //foreach (var p in BuildingwidthPoints)
            //{
            //    //TextDot test = new TextDot("건물너비", p);
            //    //drawnGuids.Add(RhinoDoc.ActiveDoc.Objects.AddTextDot(test));
            //}








            foreach (var c in CoreOutLines)
            {
                drawnGuids.Add(RhinoDoc.ActiveDoc.Objects.AddCurve(c));
            }

            foreach (var c in SectionLines)
            {
                drawnGuids.Add(RhinoDoc.ActiveDoc.Objects.AddCurve(c));
            }

            foreach (var c in Hatchs)
            {
                drawnGuids.Add(RhinoDoc.ActiveDoc.Objects.AddHatch(c));
            }

            foreach (var c in Regs)
            {
                dotted(c.ToNurbsCurve(), 500);
            }

            //foreach (var c in lines)
            //{
            //    drawnGuids.Add(RhinoDoc.ActiveDoc.Objects.AddLine(c));
            //}
            foreach (var c in Dimensions)
            {
                drawnGuids.AddRange(c.Print());
            }
            foreach (var c in Unitinfo)
            {
                drawnGuids.AddRange(c.Print());
            }
            drawnGuids.AddRange(LoadManager.getInstance().DrawObjectWithSpecificLayer(texts, LoadManager.NamedLayer.Guide));


            drawnGuids.AddRange(LoadManager.getInstance().DrawObjectsWithSpecificLayer(this.Guides, LoadManager.NamedLayer.Guide));

            drawnGuids.Add(LoadManager.getInstance().DrawObjectWithSpecificLayer(Axis, LoadManager.NamedLayer.Guide));

            return drawnGuids;
            //A = CoreOutline;
            //B = HouseOutline;
            //C = inside;
            //D = Axis;
            //E = reg;
            //F = right;
            //AA = left;
            //AB = SectionBasecurve;
            //AC = lines;
            //AD = Axis.PointAtStart;
            //AE = SectionBasecurve.PointAtStart;
            //AF = isN;
            //BA = realRoad;
            //BB = NCindex;
            //BC = groundhatchbox;


        }

        private void mirror(ref List<Point3d> points, Point3d center)
        {
            Plane cp = new Plane(center, Vector3d.XAxis);
            int index = points.Count;
            for (int i = index - 1; i >= 0; i--)
            {
                Point3d temp = new Point3d(points[i]);

                temp.Transform(Transform.Mirror(cp));

                points.Add(temp);
            }
        }

        private void mirror(ref List<Curve> curves, Point3d center)
        {
            Plane cp = new Plane(center, Vector3d.XAxis);
            int index = curves.Count;
            for (int i = index - 1; i >= 0; i--)
            {
                Curve temp = curves[i].DuplicateCurve();

                temp.Transform(Transform.Mirror(cp));

                curves.Add(temp);
            }
        }



        private void dotted(Curve line, double t)
        {
            double length = line.GetLength();
            double t2 = t / 4;


            var shatter = line.DivideByLength(t2, true);
            var spltd = line.Split(shatter);
            for (int f = 0; f < spltd.Length; f++)
            {
                if (f % (t / t2) * 2 + 1 == t / t2 + 1)
                {
                    spltd[f] = null;
                }
            }

            var merged = Curve.JoinCurves(spltd);
            this.RegsOutput.AddRange(merged);

        }
        private void dotted(Line line, double t)
        {
            double length = line.Length;
            double t2 = t / 4;
            double x = length / t2;


            var shatter = line.ToNurbsCurve().DivideByLength(x, true);
            var spltd = line.ToNurbsCurve().Split(shatter);
            for (int f = 0; f < spltd.Length; f++)
            {
                if (f % (t / t2) * 2 + 1 == t / t2 + 1)
                {
                    spltd[f] = null;
                }
            }

            var merged = Curve.JoinCurves(spltd);
            this.RegsOutput.AddRange(merged);

        }
        private List<Curve> Offsets(List<Curve> aptlines, List<Point3d> dir, List<double> param)
        {
            List<Curve> result = new List<Curve>();
            for (int i = 0; i < aptlines.Count; i++)
            {
                var temp = aptlines[i].Offset(dir[i], Vector3d.ZAxis, param[i], 0, CurveOffsetCornerStyle.None);
                result.Add(temp[0]);
            }

            return result;
        }

    }

    class Section_Pattern1 : SectionBase
    {   // % 2 . 0 = 중점 1 = 중점과 사이드의 중점
        List<Point3d> HousePoints = new List<Point3d>();
        // % 2 . 0 = front 1 = back
        List<Point3d> CorewidthPoints = new List<Point3d>();
        // % 2 . 0 = front 1 = back
        List<Point3d> HousewidthPoints = new List<Point3d>();
        // % 2 . 0 = coreback , 1 = housefront
        List<Point3d> BuildingwidthPoints = new List<Point3d>();
        // % 2 . 0 = househeightbottom , 1 = househeighttop
        List<Point3d> HouseheightPoints = new List<Point3d>();

        List<Point3d> RegPoints = new List<Point3d>();
        List<string> Regname = new List<string>();

        //print these

        //dimension
        public override  List<FloorPlan.Dimension2> Dimensions { get; set; }
        //sectioncurve
        public override List<Curve> SectionLines { get; set; }
        //outlinecurve
        public override List<Curve> CoreOutLines { get; set; }
        //regulation
        public override List<Curve> RegsOutput { get; set; }
        //text
        public override List<Rhino.Display.Text3d> texts { get; set; }
        //hatch
        public override List<Hatch> Hatchs { get; set; }
        //unitinfo
        public override List<UnitInfo> Unitinfo { get; set; }
        //boundingBox
        public override BoundingBox boundingBox { get; set; }

        List<Point3d> toboundingbox = new List<Point3d>();


        List<Curve> AptLines = null;
        List<double> x = null; //House Left
        List<double> y = null; //Core Left
        List<double> z = null; //House Right
        List<double> u = null; //Core Right
        List<int> strs = new List<int>();
        Curve Plot = null;
        List<int> surr = null;
        Point3d SectionBasePoint = Point3d.Unset;
        


        List<Curve> Guides = new List<Curve>();

        List<Guid> drawnGuids = new List<Guid>();

        public Section_Pattern1(Apartment agOut)
        {
            this.Dimensions = new List<FloorPlan.Dimension2>();
            this.SectionLines = new List<Curve>();
            this.CoreOutLines = new List<Curve>();
            this.RegsOutput = new List<Curve>();
            this.texts = new List<Rhino.Display.Text3d>();
            this.Hatchs = new List<Hatch>();
            this.Unitinfo = new List<UnitInfo>();
            this.boundingBox = new BoundingBox();



            List<int> passindex = new List<int>();
            

            for (int i = 0; i < agOut.AptLines.Count - 1; i++)
            {
                if (agOut.AptLines[i].PointAtEnd.DistanceTo(agOut.AptLines[i + 1].PointAtStart) <= 1000)
                    passindex.Add(i);
            }

            //coreback = coreright
            List<double> temp = new List<double>();
            List<double> temp2 = new List<double>();
            List<double> widthtemp = new List<double>();
            for (int i = 0; i < agOut.AptLines.Count; i++)
            {
                if (passindex.Contains(i))
                    continue;
                for (int j = 0; j < agOut.Core[0].Count; j++)
                {
                    Point3d coreOri = agOut.Core[0][j].Origin;
                    double param;
                    agOut.AptLines[i].ClosestPoint(coreOri, out param);
                    Point3d aptClosest = agOut.AptLines[i].PointAt(param);

                    if (coreOri.DistanceTo(aptClosest) < agOut.ParameterSet.Parameters[2])
                    {
                        temp.Add(Vector3d.Multiply(agOut.Core[0][j].YDirection, new Vector3d(coreOri - aptClosest)));
                        widthtemp.Add(agOut.ParameterSet.Parameters[2] / 2);
                        temp2.Add(temp.Last() + agOut.Core[0][j].Depth);
                        strs.Add(agOut.Household.Count);
                    }
                }
            }

            this.x = widthtemp.Select(n => -n).ToList();
            this.z = widthtemp;
            this.u = temp;
            //corefront = coreleft
      
            //for (int i = 0; i < agOut.AptLines.Count; i++)
            //{
            //    if (passindex.Contains(i))
            //        continue;

            //    temp2.AddRange(u.Select(n => n + agOut.Core[i][0].CoreType.GetDepth()).ToList());
            //    strs.Add(Convert.ToInt32(Math.Round(agOut.Core[i][0].Stories+1)));
            //}
            this.y = temp2;


            Plot = agOut.Plot.Boundary;
            surr = agOut.Plot.Surroundings.ToList();

            SectionBasePoint = Point3d.Origin;

            this.AptLines = Curve.JoinCurves(agOut.AptLines).ToList();


        }


        public override List<Guid> DrawSection(bool northline)
        {
            double slavHeight = 300;
            double houseHeight = 2700;
            double pilHeight = 3300;
            double wallThick = 300;

            List<Curve> offsetcurves = new List<Curve>();
            Curve Axis = null;
            List<Point3d> dirPoint = new List<Point3d>();
            List<Point3d> cPs = new List<Point3d>();
            List<Vector3d> dirVector = new List<Vector3d>();

            ///각 아파트 라인의 수직인 방향 점과 방향 벡터 구함
            foreach (Curve c in AptLines)
            {

                cPs.Add(c.PointAtNormalizedLength(0.5));
                Curve tempCurve = c.DuplicateCurve();

                tempCurve.Rotate(RhinoMath.ToRadians(90), Vector3d.ZAxis, c.PointAtNormalizedLength(0.5));
                dirVector.Add(new Vector3d(tempCurve.PointAtEnd - tempCurve.PointAtStart));
                dirPoint.Add(tempCurve.PointAtEnd);

            }

            ///각 아파트 라인의 중심에서 수직인 선을 만들고, 모든 아파트를 통과하면 축선으로 쓴다.

            for (int i = 0; i < cPs.Count; i++)
            {
                Line templine = new Line(cPs[i], dirVector[i] * 100);
                Curve tempCurve = templine.ToNurbsCurve();
                tempCurve.Translate(new Vector3d(-dirVector[i]) * 50);
                int intersections = 0;
                for (int j = 0; j < AptLines.Count; j++)
                {


                    var intersection = Rhino.Geometry.Intersect.Intersection.CurveCurve(tempCurve, AptLines[j], 0, 0);

                    if (intersection.Count > 0)
                        intersections++;
                }

                if (intersections == AptLines.Count)
                {

                    Axis = tempCurve;
                    break;
                }
            }
            //만약 축선이 안구해지면 리턴
            if (Axis == null)
            {
                //RhinoApp.WriteLine("여기구나");
                return null;
            }
               


            //북향 대지 경계 찾기.
            List<int> NCindex = new List<int>();

            for (int i = 0; i < Plot.DuplicateSegments().Length; i++)
            {
                if (Plot.DuplicateSegments()[i].TangentAt(0.5).X > 0)
                    NCindex.Add(i);

            }




            //구한 축선을 대지에 맞게 자르고 아파트라인, 코어라인을 그어준다.
            //만약 접한 경계가 도로라면 경계와 접한 도로 너비의 값을 축선에 보정

            List<double> splitparam = new List<double>();
            var plotseg = Plot.DuplicateSegments();
            double[] realRoad = new double[2];
            double[] roadwidth = new double[2];
            bool[] isN = new bool[2];
            double[] sangle = new double[2];
            double tangle = 0;
            int arrayindex = 1;

            for (int i = 0; i < plotseg.Length; i++)
            {

                var intersection2 = Rhino.Geometry.Intersect.Intersection.CurveCurve(Axis, plotseg[i], 0, 0);
                int dir = 1;
                double temp = surr[i];
                foreach (var intersect in intersection2)
                {


                    if (intersect.PointA.DistanceTo(Axis.PointAtStart) < intersect.PointA.DistanceTo(Axis.PointAtEnd))
                    {
                        dir = -1;
                        arrayindex = 0;
                    }
                    else
                    {
                        dir = 1;
                        arrayindex = 1;
                    }

                    if (NCindex.Contains(i))
                    {
                        isN[arrayindex] = true;
                    }
                    else
                        isN[arrayindex] = false;



                    tangle = Vector3d.VectorAngle(Axis.TangentAtEnd, plotseg[i].TangentAtEnd);
                    realRoad[arrayindex] = temp * dir / Math.Sin(tangle);
                    roadwidth[arrayindex] = temp;
                    double temprad = Vector3d.VectorAngle(plotseg[i].TangentAtEnd, Vector3d.YAxis);
                    double temprad2 = Vector3d.VectorAngle(Axis.TangentAtEnd, plotseg[i].TangentAtEnd);

                    double tempsinval = Math.Abs(Math.Sin(temprad)) / Math.Abs(Math.Sin(temprad2));
                    //sangle[arrayindex] = Math.Abs(Math.Cos(temprad));
                    sangle[arrayindex] = tempsinval;
                    splitparam.Add(intersect.ParameterA);



                    //RhinoApp.WriteLine("TangentAtEnd = " + Axis.TangentAtEnd.ToString());
                    //RhinoApp.WriteLine("tempRoad = " + (temp * dir * 1000).ToString());
                    //RhinoApp.WriteLine("realRoad = " + (temp * dir * 1000 / Math.Sin(tangle)).ToString());
                    //RhinoApp.WriteLine("angle = " + (tangle).ToString());


                }

            }



            Axis = Axis.Split(splitparam)[1];

            Interval newdomain = new Interval(0, 1);
            Axis.Domain = newdomain;

            offsetcurves.AddRange(Offsets(AptLines, dirPoint, x));
            offsetcurves.AddRange(Offsets(AptLines, dirPoint, y));
            offsetcurves.AddRange(Offsets(AptLines, dirPoint, z));
            offsetcurves.AddRange(Offsets(AptLines, dirPoint, u));




            //단면 대지선을 지정한 단면 포인트로부터 만들어주고 축선과 offset라인의 교점의 값을 sectionbase에 저장.



            List<double> sectionbase = new List<double>();
           // List<Point3d> debug = new List<Point3d>();
            foreach (Curve c in offsetcurves)
            {
                Line Axisline = new Line(Axis.PointAtStart, Axis.PointAtEnd);
                Line templ = new Line(c.PointAtStart, c.PointAtEnd);
                double paramA;
                double paramB;
                var intersection3 = Rhino.Geometry.Intersect.Intersection.LineLine(Axisline, templ,out paramA,out paramB);
            
                sectionbase.Add(paramA);
            }


            //단면 대지선을 그려주고 sectionbase값에 맞춰 잘라줌.


            double sectiondir = 1;


            if (Axis.TangentAtEnd.X == 0 && Axis.PointAtStart.Y > Axis.PointAtEnd.Y)
                sectiondir = 1;


            else if (Axis.TangentAtEnd.X == 0 && Axis.PointAtStart.Y < Axis.PointAtEnd.Y)
                sectiondir = -1;

            else
                sectiondir = Axis.TangentAtEnd.X / Math.Abs(Axis.TangentAtEnd.X);

            Line SectionBaseline = new Line(SectionBasePoint, Vector3d.XAxis * sectiondir, new Vector3d(Axis.PointAtEnd - Axis.PointAtStart).Length);


            //RhinoApp.WriteLine(Axis.TangentAtEnd.ToString());
            //RhinoApp.WriteLine(sectiondir.ToString());


            Curve SectionBasecurve = SectionBaseline.ToNurbsCurve();

            if (sectiondir == -1)
                SectionBasecurve.Translate(Vector3d.XAxis * SectionBaseline.Length);




            var spltSB = SectionBasecurve.Split(sectionbase);

            List<Point3d> CoreStart = new List<Point3d>();
            List<Point3d> HouseStart = new List<Point3d>();

            //단면 대지선의 양 사이드에 먼저 추출해둔 도로선만큼 확장 해줌.


            Line left = new Line(SectionBasecurve.PointAtStart, Vector3d.XAxis * sectiondir * realRoad[0]);
            Line right = new Line(SectionBasecurve.PointAtEnd, Vector3d.XAxis * sectiondir * realRoad[1]);



            Line left2 = new Line(left.To, Vector3d.XAxis * sectiondir * -5000);
            Line right2 = new Line(right.To, Vector3d.XAxis * sectiondir * 5000);


            toboundingbox.Add(left2.To + Vector3d.YAxis * -5000);
            toboundingbox.Add(right2.To + Vector3d.YAxis * -5000);
            //toboundingbox[0] = left2.ToX;
            //toboundingbox[1] = right2.ToX;
            //if (sectiondir < 0)
            //{
            //    toboundingbox[0] = right2.ToX;
            //    toboundingbox[1] = left2.ToX;
            //}

            //


            //hatch 만들기

            double hatchheight = -5000;
            //toboundingbox[2] = hatchheight;
            List<Point3d> groundoutrect = new List<Point3d>();
            groundoutrect.Add(left2.To);
            groundoutrect.Add(left2.To + Vector3d.YAxis * hatchheight);
            groundoutrect.Add(right2.To + Vector3d.YAxis * hatchheight);
            groundoutrect.Add(right2.To);
            groundoutrect.Add(left2.To);
            Curve groundhatchbox = new PolylineCurve(groundoutrect) as Curve;

            groundhatchbox.MakeClosed(1000);

            var ground = Hatch.Create(groundhatchbox, 2, RhinoMath.ToRadians(45), 10000);
            //for (int i = 0; i < ground.Length; i++)
            //    //RhinoDoc.ActiveDoc.Objects.AddHatch(ground[i]);
            //    if (ground.Length > 0)
            //        RhinoDoc.ActiveDoc.Views.Redraw();





            //대지경계 / 인접대지경계 선 출력

            List<Line> lines = new List<Line>();

            Line tempbline = Line.Unset;

            Point3d[] nearplot = new Point3d[2];
            tempbline = new Line(SectionBasecurve.PointAtStart, Vector3d.YAxis * 5000);
            lines.Add(tempbline);
            dotted(tempbline, 500);
            nearplot[0] = tempbline.To;
            if (left.Length != 0)
            {
                Regname.Add("대지경계");
                RegPoints.Add(tempbline.PointAt(0.5));
                tempbline = new Line(left.To, Vector3d.YAxis * 5000);
                lines.Add(tempbline);


                dotted(tempbline, 500);


                nearplot[0] = tempbline.To;
            }


            Regname.Add("인접대지경계");
            RegPoints.Add(tempbline.PointAt(0.5));

            tempbline = new Line(SectionBasecurve.PointAtEnd, Vector3d.YAxis * 5000);
            lines.Add(tempbline);
            dotted(tempbline, 500);
            nearplot[1] = tempbline.To;
            if (right.Length != 0)
            {
                Regname.Add("대지경계");
                RegPoints.Add(tempbline.PointAt(0.5));
                tempbline = new Line(right.To, Vector3d.YAxis * 5000);
                lines.Add(tempbline);
                dotted(tempbline, 500);
                nearplot[1] = tempbline.To;
            }

            Regname.Add("인접대지경계");
            RegPoints.Add(tempbline.PointAt(0.5));

            //법규선 시작점 설정
            Point3d fromstart = SectionBasecurve.PointAtStart + Vector3d.XAxis * sectiondir * realRoad[0] / 2;
            Point3d fromend = SectionBasecurve.PointAtEnd + Vector3d.XAxis * sectiondir * realRoad[1] / 2;
            //
            //    if(sectiondir == -1)
            //    {
            //      Point3d temp = leftstart;
            //      leftstart = rightstart;
            //      rightstart = temp;
            //    }


            //    if(sectiondir == -1)
            //    {
            //      bool temp = isN[0];
            //      isN[0] = isN[1];
            //      isN[1] = temp;
            //    }


            //법규선 생성


            List<Polyline> reg = new List<Polyline>();
            List<Point3d> toPoly = new List<Point3d>();
            Point3d tempp = Point3d.Unset;
            if (northline)
            {
                if (isN[0])
                {
                    toPoly.Add(fromstart);
                    tempp = fromstart + Vector3d.XAxis * sangle[0] * 1500 * sectiondir;
                    toPoly.Add(tempp);
                    tempp = tempp + Vector3d.YAxis * 9000;
                    toPoly.Add(tempp);
                    tempp = tempp + Vector3d.XAxis * sangle[0] * 3000 * sectiondir;
                    toPoly.Add(tempp);
                    tempp = tempp + new Vector3d(sangle[0] * sectiondir, 2, 0) * 9600;
                    toPoly.Add(tempp);

                    //이름붙이기
                    tempp = tempp + new Vector3d(sangle[0] * sectiondir, 2, 0) * -4800;
                    RegPoints.Add(tempp);
                    Regname.Add("정북방향 일조사선");

                    reg.Add(new Polyline(toPoly));
                }

                toPoly.Clear();
                if (nearplot[0] == null)
                    tempp = fromstart + Vector3d.YAxis * 3300;
                else
                {
                    tempp = new Point3d(nearplot[0].X, 3300, 0);
                }

                toPoly.Add(tempp);
                tempp = tempp + new Vector3d(1 * sectiondir, 4, 0) * 9600;
                toPoly.Add(tempp);

                reg.Add(new Polyline(toPoly));

                tempp = tempp + new Vector3d(1 * sectiondir, 4, 0) * -4800;
                RegPoints.Add(tempp);
                Regname.Add("채광방향 높이제한");


                toPoly.Clear();

                if (isN[1])
                {
                    toPoly.Add(fromend);
                    tempp = fromend + Vector3d.XAxis * -sangle[1] * 1500 * sectiondir;
                    toPoly.Add(tempp);
                    tempp = tempp + Vector3d.YAxis * 9000;
                    toPoly.Add(tempp);
                    tempp = tempp + Vector3d.XAxis * -sangle[1] * 3000 * sectiondir;
                    toPoly.Add(tempp);
                    tempp = tempp + new Vector3d(-sangle[1] * sectiondir, 2, 0) * 9600;
                    toPoly.Add(tempp);

                    tempp = tempp + new Vector3d(-sangle[0] * sectiondir, 2, 0) * -4800;
                    RegPoints.Add(tempp);
                    Regname.Add("정북방향 일조사선");

                    reg.Add(new Polyline(toPoly));
                }

                toPoly.Clear();



                if (nearplot[1] == null)
                    tempp = fromend + Vector3d.YAxis * 3300;
                else
                    tempp = new Point3d(nearplot[1].X, 3300, 0);
                toPoly.Add(tempp);
                tempp = tempp + new Vector3d(-1 * sectiondir, 4, 0) * 9600;
                toPoly.Add(tempp);

                reg.Add(new Polyline(toPoly));


                toPoly.Clear();


                tempp = tempp + new Vector3d(-1 * sectiondir, 4, 0) * -4800;
                RegPoints.Add(tempp);
                Regname.Add("채광방향 높이제한");

            }


            //자른 선분들의 끝점을 아파트선, 코어선 시작 리스트에 저장

            for (int i = 0; i < spltSB.Length - 1; i++)
            {
                //두개마다 집
                if (i % 2 == 0)
                {
                    HouseStart.Add(spltSB[i].PointAtEnd);
                    //네개마다 집 시작, 건물 총 너비 시작
                    if (i % 4 == 0)
                    {
                        HousewidthPoints.Add(spltSB[i].PointAtEnd);
                        BuildingwidthPoints.Add(spltSB[i].PointAtEnd);
                    }

                    else if (i % 4 == 2)
                    {
                        HousewidthPoints.Add(spltSB[i].PointAtEnd);
                    }

                }

                //두개마다 코어
                else
                {
                    CoreStart.Add(spltSB[i].PointAtEnd);

                    if (i % 4 == 1)
                    {
                        CorewidthPoints.Add(spltSB[i].PointAtEnd);
                    }
                    else if (i % 4 == 3)
                    {
                        CorewidthPoints.Add(spltSB[i].PointAtEnd);
                        BuildingwidthPoints.Add(spltSB[i].PointAtEnd);
                    }
                }
            }

            //아파트와 코어의 외곽선을 그려준다.

            List<Curve> HouseOutline = new List<Curve>();
            List<Curve> CoreOutline = new List<Curve>();
            for (int i = 0; i < AptLines.Count; i++)
            {
                double AptHeight = 0;

                for (int j = 0; j < strs[i]; j++)
                {
                    if (j != 0)
                        AptHeight += houseHeight;
                }

                double CoreHeight = AptHeight + houseHeight + pilHeight;

                Line AptSide1 = new Line(HouseStart[i * 2] + Vector3d.YAxis * (pilHeight - 300), Vector3d.YAxis * (AptHeight + 300));
                Line AptSide2 = new Line(HouseStart[i * 2 + 1] + Vector3d.YAxis * (pilHeight - 300), Vector3d.YAxis * (AptHeight + 300));
                Line AptUp = new Line(AptSide1.ToNurbsCurve().PointAtEnd, AptSide2.ToNurbsCurve().PointAtEnd);
                Line AptDown = new Line(AptSide1.ToNurbsCurve().PointAtStart, AptSide2.ToNurbsCurve().PointAtStart);

                HouseOutline.Add(AptSide1.ToNurbsCurve());
                HouseOutline.Add(AptSide2.ToNurbsCurve());
                HouseOutline.Add(AptUp.ToNurbsCurve());
                HouseOutline.Add(AptDown.ToNurbsCurve());



                Line CoreSide1 = new Line(CoreStart[i * 2], Vector3d.YAxis * CoreHeight);
                Line CoreSide2 = new Line(CoreStart[i * 2 + 1], Vector3d.YAxis * CoreHeight);
                Line CoreUp = new Line(CoreSide1.ToNurbsCurve().PointAtEnd, CoreSide2.ToNurbsCurve().PointAtEnd);
                Line CoreDown = new Line(CoreSide1.ToNurbsCurve().PointAtStart, CoreSide2.ToNurbsCurve().PointAtStart);

                CoreOutline.Add(CoreSide1.ToNurbsCurve());
                CoreOutline.Add(CoreSide2.ToNurbsCurve());
                CoreOutline.Add(CoreUp.ToNurbsCurve());
                CoreOutline.Add(CoreDown.ToNurbsCurve());


            }


            //아웃라인에서 겹치는 선 제거.

            List<Curve> inHouse = new List<Curve>();
            inHouse.AddRange(HouseOutline);
            CoreOutline = Curve.JoinCurves(CoreOutline).ToList();
            HouseOutline = Curve.JoinCurves(HouseOutline).ToList();

            for (int i = 0; i < HouseOutline.Count; i++)
            {
                splitparam.Clear();

                var intersection4 = Rhino.Geometry.Intersect.Intersection.CurveCurve(CoreOutline[i], HouseOutline[i], 0, 0);
                foreach (var intersect in intersection4)
                {
                    splitparam.Add(intersect.ParameterA);
                }

                var spltd = CoreOutline[i].Split(splitparam);

                List<Curve> temp = new List<Curve>();
                foreach (var sc in spltd)
                {
                    sc.Domain = newdomain;
                    if (HouseOutline[i].Contains(sc.PointAt(0.5)) != PointContainment.Inside)
                        temp.Add(sc);
                }

                CoreOutline[i] = Curve.JoinCurves(temp)[0];

            }


            //내부 선 만들어줌
            List<Curve> inside = new List<Curve>();

            for (int i = 0; i < AptLines.Count; i++)
            {

                List<Curve> unit = new List<Curve>();
                Curve tempunit = null;

                Curve temp;

                temp = inHouse[i * 4 + 3].DuplicateCurve();
                temp.SetStartPoint(temp.PointAtStart + Vector3d.XAxis * sectiondir * wallThick);
                temp.SetEndPoint(temp.PointAtEnd + Vector3d.XAxis * sectiondir * -wallThick);

                temp.Translate(Vector3d.YAxis * slavHeight);
                unit.Add(temp);
                temp = temp.DuplicateCurve();
                temp.Translate(Vector3d.YAxis * (houseHeight - slavHeight));
                unit.Add(temp);

                Line temp1;
                temp1 = new Line(unit[0].PointAtStart, unit[1].PointAtStart);
                unit.Add(temp1.ToNurbsCurve());
                temp1 = new Line(unit[0].PointAtEnd, unit[1].PointAtEnd);
                unit.Add(temp1.ToNurbsCurve());


                List<Curve> toHatch = new List<Curve>();

                toHatch.Add(HouseOutline[i]);

                for (int j = 0; j < strs[i] - 1; j++)
                {


                    tempunit = Curve.JoinCurves(unit)[0];
                    tempunit.Translate(Vector3d.YAxis * houseHeight * j);
                    inside.Add(tempunit);
                    toHatch.Add(tempunit);

                    // 세대 중심
                    Polyline tempunitpl;
                    tempunit.TryGetPolyline(out tempunitpl);
                    HousePoints.Add(tempunitpl.CenterPoint());

                    // 세대 높이선(디멘션 용 점2개)
                    HouseheightPoints.Add(tempunitpl.CenterPoint() + (Vector3d.XAxis * (temp.GetLength() / 4) + Vector3d.YAxis * (houseHeight / 2 - slavHeight / 2)));
                    HouseheightPoints.Add(tempunitpl.CenterPoint() + (Vector3d.XAxis * (temp.GetLength() / 4) - Vector3d.YAxis * (houseHeight / 2 - slavHeight / 2)));

                    //Point3d unitheightbottom = tempunitpl.CenterPoint() + Vector3d.XAxis * 
                }


                var hatch = Hatch.Create(toHatch, 0, 0, 1000);

                Hatchs.AddRange(hatch);




            }








            List<Polyline> Regs = new List<Polyline>();
            List<Curve> Guides = new List<Curve>();
            
            CoreOutLines.AddRange(CoreOutline);
            SectionLines.AddRange(HouseOutline);
            SectionLines.Add(SectionBasecurve);
            SectionLines.Add(right.ToNurbsCurve());
            SectionLines.Add(left.ToNurbsCurve());
            SectionLines.Add(right2.ToNurbsCurve());
            SectionLines.Add(left2.ToNurbsCurve());
            Hatchs.AddRange(ground);

            if(northline)
            Regs.AddRange(reg);


            

            for (int i = 0; i < RegPoints.Count; i++)
            {
                int textdir = 1;
                if (RegPoints[i].X < HousewidthPoints[0].X)
                {
                    textdir = -1;
                }

                if ((RegPoints[i].X == SectionBasecurve.PointAtStart.X || RegPoints[i].X == SectionBasecurve.PointAtEnd.X))
                {
                    textdir *= -1;
                }

                Point3d plnorigin = RegPoints[i] + Vector3d.XAxis * textdir * 2000;

                Line textline = new Line(RegPoints[i], Vector3d.XAxis * textdir * 1000);


                Plane textpln = new Plane(plnorigin, Vector3d.XAxis, Vector3d.YAxis);

                Rhino.Display.Text3d text = new Rhino.Display.Text3d(Regname[i], textpln, 2000);


                this.RegsOutput.Add(textline.ToNurbsCurve());
                this.texts.Add(text);

                ///fordebug
                //drawnGuids.Add(LoadManager.getInstance().DrawObjectWithSpecificLayer(textline.ToNurbsCurve(), LoadManager.NamedLayer.Guide));
                //drawnGuids.Add(LoadManager.getInstance().DrawObjectWithSpecificLayer(text, LoadManager.NamedLayer.Guide));
            }

            for (int i = 0; i < AptLines.Count; i++)
            {
                Point3d heightstart;
                FloorPlan.Dimension2 heightdimension;
                if (sectiondir > 0)
                {
                    heightstart = HousewidthPoints[i * 2] - Vector3d.XAxis * 1000;
                    heightdimension = new FloorPlan.Dimension2(heightstart + Vector3d.YAxis * (pilHeight + (strs[i] - 1) * houseHeight), heightstart, 500);

                }
                else
                {
                    heightstart = HousewidthPoints[i * 2] + Vector3d.XAxis * 1000;
                    heightdimension = new FloorPlan.Dimension2(heightstart, heightstart + Vector3d.YAxis * (pilHeight + (strs[i] - 1) * houseHeight), 500);
                }


                Dimensions.Add(heightdimension);

                ///fordebug
                //drawnGuids.AddRange(heightdimension.Print());

            }


            for (int i = 0; i < HouseheightPoints.Count; i += 2)
            {

                FloorPlan.Dimension2 dim2 = new FloorPlan.Dimension2(HouseheightPoints[i], HouseheightPoints[i + 1]);

                Dimensions.Add(dim2);


                ///fordebug
                //drawnGuids.AddRange(dim2.Print());

            }
            int stackedhouse = 0;
            for (int i = 0; i < AptLines.Count; i++)
            {
                double dimensionoffset = pilHeight + houseHeight * strs[i] + 300;
                Vector3d dimensionvec = Vector3d.YAxis * dimensionoffset;
                FloorPlan.Dimension2 housewidth = new FloorPlan.Dimension2(HousewidthPoints[i * 2 + 1] + dimensionvec, HousewidthPoints[i * 2] + dimensionvec, 500);
                FloorPlan.Dimension2 corewidth = new FloorPlan.Dimension2(CorewidthPoints[i * 2 + 1] + dimensionvec, CorewidthPoints[i * 2] + dimensionvec, 1000);
                FloorPlan.Dimension2 buildingwidth = new FloorPlan.Dimension2(BuildingwidthPoints[i * 2 + 1] + dimensionvec, BuildingwidthPoints[i * 2] + dimensionvec, 1500);


                Dimensions.Add(housewidth);
                Dimensions.Add(corewidth);
                Dimensions.Add(buildingwidth);


                ///fordebug
                //drawnGuids.AddRange(housewidth.Print());
                //drawnGuids.AddRange(corewidth.Print());
                //drawnGuids.AddRange(buildingwidth.Print());

                for (int j = 0; j < strs[i] - 1; j++)
                {

                    UnitInfo tempunitinfo = new UnitInfo(HousePoints[stackedhouse], j + 2, houseHeight);

                    Unitinfo.Add(tempunitinfo);


                    ///fordebug
                    //drawnGuids.AddRange(tempunitinfo.Print());
                    stackedhouse++;
                }

                if (i + 1 < AptLines.Count)
                {
                    FloorPlan.Dimension2 aptdimension = new FloorPlan.Dimension2(HousewidthPoints[i * 2 + 2] + dimensionvec / 2, HousewidthPoints[i * 2 + 1] + dimensionvec / 2, 0);

                    Dimensions.Add(aptdimension);

                    ///fordebug
                    //drawnGuids.AddRange(aptdimension.Print());
                }

            }


            if (realRoad[0] != 0)
            {
                Plane roadplane = new Plane(left.PointAt(0.5) + Vector3d.YAxis * 2500, Vector3d.XAxis, Vector3d.YAxis);
                Rhino.Display.Text3d road = new Rhino.Display.Text3d(roadwidth[0].ToString() + "m 도로",roadplane,4500);

                texts.Add(road);
                ///fordebug
                //drawnGuids.Add(LoadManager.getInstance().DrawObjectWithSpecificLayer(road, LoadManager.NamedLayer.Guide));
            }

            if (realRoad[1] != 0)
            {
                Plane roadplane = new Plane(right.PointAt(0.5) + Vector3d.YAxis * 2500, Vector3d.XAxis, Vector3d.YAxis);
                Rhino.Display.Text3d road = new Rhino.Display.Text3d(roadwidth[1].ToString() + "m 도로", roadplane, 4500);

                texts.Add(road);
                ///fordebug
                //drawnGuids.Add(LoadManager.getInstance().DrawObjectWithSpecificLayer(road, LoadManager.NamedLayer.Guide));
            }



            //foreach (var p in HousewidthPoints)
            //{

            //    //TextDot test = new TextDot("세대너비", p);
            //    //drawnGuids.Add(RhinoDoc.ActiveDoc.Objects.AddTextDot(test));
            //}

            //foreach (var p in CorewidthPoints)
            //{
            //    //TextDot test = new TextDot("코어너비", p);
            //    //drawnGuids.Add(RhinoDoc.ActiveDoc.Objects.AddTextDot(test));
            //}

            //foreach (var p in BuildingwidthPoints)
            //{
            //    //TextDot test = new TextDot("건물너비", p);
            //    //drawnGuids.Add(RhinoDoc.ActiveDoc.Objects.AddTextDot(test));
            //}

            foreach (var c in Regs)
            {

                dotted(c.ToNurbsCurve(), 500);

            }





            ///fordebug

            foreach (var c in CoreOutLines)
            {
                drawnGuids.Add(RhinoDoc.ActiveDoc.Objects.AddCurve(c));
            }

            foreach (var c in SectionLines)
            {
                drawnGuids.Add(RhinoDoc.ActiveDoc.Objects.AddCurve(c));
            }

            foreach (var c in Hatchs)
            {
                drawnGuids.Add(RhinoDoc.ActiveDoc.Objects.AddHatch(c));
            }
            foreach (var c in Dimensions)
            {
                drawnGuids.AddRange(c.Print());
            }
            foreach (var c in Unitinfo)
            {
                drawnGuids.AddRange(c.Print());
            }
            drawnGuids.AddRange(LoadManager.getInstance().DrawObjectWithSpecificLayer(texts, LoadManager.NamedLayer.Guide));

            drawnGuids.AddRange(LoadManager.getInstance().DrawObjectsWithSpecificLayer(this.RegsOutput, LoadManager.NamedLayer.Guide));

            drawnGuids.Add(LoadManager.getInstance().DrawObjectWithSpecificLayer(Axis, LoadManager.NamedLayer.Guide));

            // boundingBox = new BoundingBox(toboundingbox[0], toboundingbox[2], 0, toboundingbox[3], toboundingbox[1], 0).ToBrep().DuplicateEdgeCurves()[0];


            toboundingbox.Add(Point3d.Origin + Vector3d.YAxis * (strs[0] * 2700 + 15000));

            boundingBox = new BoundingBox(toboundingbox);

            return drawnGuids;
            //A = CoreOutline;
            //B = HouseOutline;
            //C = inside;
            //D = Axis;
            //E = reg;
            //F = right;
            //AA = left;
            //AB = SectionBasecurve;
            //AC = lines;
            //AD = Axis.PointAtStart;
            //AE = SectionBasecurve.PointAtStart;
            //AF = isN;
            //BA = realRoad;
            //BB = NCindex;
            //BC = groundhatchbox;


        }

        private void dotted(Curve line, double t)
        {
            double length = line.GetLength();
            double t2 = t / 4;
           

            var shatter = line.DivideByLength(t2, true);
            var spltd = line.Split(shatter);
            for (int f = 0; f < spltd.Length; f++)
            {
                if (f % (t / t2) * 2 + 1 == t / t2 + 1)
                {
                    spltd[f] = null;
                }
            }

            var merged = Curve.JoinCurves(spltd);
            this.RegsOutput.AddRange(merged);

        }
        private void dotted(Line line, double t)
        {
            double length = line.Length;
            double t2 = t / 4;
            double x = length / t2;


            var shatter = line.ToNurbsCurve().DivideByLength(x, true);
            var spltd = line.ToNurbsCurve().Split(shatter);
            for (int f = 0; f < spltd.Length; f++)
            {
                if (f % (t/t2)*2+1 == t/t2+1)
                {
                    spltd[f] = null;
                }
            }

            var merged = Curve.JoinCurves(spltd);

            List<Curve> mergedCopy = (from i in merged
                                     where i != null
                                     select i).ToList();

            this.RegsOutput.AddRange(mergedCopy);

        }
        private List<Curve> Offsets(List<Curve> aptlines, List<Point3d> dir, List<double> param)
        {
            List<Curve> result = new List<Curve>();
           
            for (int i = 0; i < aptlines.Count; i++)
            {
                Curve extended = aptlines[i].DuplicateCurve().Extend(CurveEnd.Both, 50000, CurveExtensionStyle.Line);
                var temp = extended.Offset(dir[i], Vector3d.ZAxis, param[i], 0, CurveOffsetCornerStyle.None);
                result.Add(temp[0]);
            }

            return result;
        }
    }
}
