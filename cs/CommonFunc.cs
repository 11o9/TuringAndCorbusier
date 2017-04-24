﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rhino.Geometry;
using System.Windows.Forms;
using Microsoft.Win32;
using System.IO.Ports;
using Oracle.ManagedDataAccess.Client;
using Rhino.Collections;



namespace TuringAndCorbusier
{

    class CommonFunc
    {
        public static List<Curve> ApartDistance(ApartmentGeneratorOutput output, out string log)
        {
            List<Curve> crvs = new List<Curve>();
            string dim = "";
            foreach (var aptline in output.AptLines)
            {
                Curve front = aptline.DuplicateCurve();
                Curve back = front.DuplicateCurve();
                var v = aptline.TangentAtStart;
                v.Rotate(-Math.PI / 2, Vector3d.ZAxis);
                //aptwidth/2
                var d = output.ParameterSet.Parameters[2] / 2;
                front.Translate(v * d);
                back.Translate(v * d * -1);

                Curve front2 = front.DuplicateCurve();
                Curve back2 = back.DuplicateCurve();

                //height(total)
                var d2 = output.HouseholdProperties.Last()[0][0].Origin.Z + Consts.FloorHeight;
                //height(except pil)
                var d3 = d2 * 0.8;
                front2.Translate(v * d3);
                back2.Translate(v * d3 * -1);

                var centerline = new LineCurve(front.PointAtNormalizedLength(0.5), front2.PointAtNormalizedLength(0.5));
                var anothercenterline = new LineCurve(back.PointAtNormalizedLength(0.5), back2.PointAtNormalizedLength(0.5));

                centerline.Translate(Vector3d.ZAxis * d2);
                anothercenterline.Translate(Vector3d.ZAxis * d2);

                crvs.Add(centerline);
                crvs.Add(anothercenterline);

                dim = string.Format("h = {0} m, h * 0.8 = {1} m ", Math.Round(d2) / 1000, Math.Round(d3) / 1000);

            }

            log = dim;
            //foreach (var top in output.HouseholdProperties.Last())
            //{
            //    foreach (var tophhp in top)
            //    {
            //        foreach (var lighting in tophhp.LightingEdge)
            //        {

            //            var l = new Line(lighting.From, lighting.To);
            //            var v = lighting.UnitTangent;
            //            var h = lighting.FromZ + Consts.FloorHeight;
            //            var every2 = (tophhp.indexer[1] % 2 == 0)? 1:-1;
            //            v.Rotate(Math.PI / 2, Vector3d.ZAxis);
            //            l.Transform(Transform.Translation(v * h * 0.8 * every2));
            //            var c = new PolylineCurve(new Point3d[] { lighting.From, lighting.To, l.To, l.From, lighting.From });
            //            c.Translate(Vector3d.ZAxis * Consts.FloorHeight*2);
            //            crvs.Add(c);
            //        }
            //    }
            //}





            return crvs;
           
        }
        public static List<Curve> JoinRegulation(Plot plot, double stories, ApartmentGeneratorOutput output)
        {
            Regulation reg = new Regulation(stories);
            var result = JoinRegulations(reg.byLightingCurve(plot,output.ParameterSet.Parameters[3]), reg.fromNorthCurve(plot), reg.fromSurroundingsCurve(plot));
            return result.ToList();
        }
        public static List<Curve> LawLines(Plot plot, double stories)
        {
            //if (plot.PlotType == PlotType.상업지역)
            //    return new List<Curve>();

            List<Curve> result = new List<Curve>();
            for (int i = 0; i <= stories; i++)
            {
                double storyheight = Consts.PilotiHeight + Consts.FloorHeight * i;
                Curve crv = plot.Boundary.DuplicateCurve();   //angle radian
                crv.Translate(Vector3d.ZAxis * storyheight);
                result.Add(crv);
            }

            return result;
        }
        //fortest
        public static List<Curve> LawbyLighting(Plot plot, double stories)
        {
            double angle = 0;
            List<Curve> result = new List<Curve>();
            for (int i = 0; i <= stories; i++)
            {
                Regulation reg2 = new Regulation(stories, i);
                Curve[] crv = reg2.byLightingCurve(plot, angle);  //angle radian
                foreach (var c in crv)
                    c.Translate(Vector3d.ZAxis * reg2.height);
                result.AddRange(crv);
            }

            
            return result;
        }

        public static List<Curve> LawLines(Plot plot, double stories, ApartmentGeneratorOutput output)
        {
            //#region AreaPrint

            //if (plot.PlotType == PlotType.상업지역)
            //    return new List<Curve>();

            //List<Curve> result = new List<Curve>();
            //for (int i = 0; i <= stories; i++)
            //{
            //    Regulation reg2 = new Regulation(stories, i);
            //    Curve[] crv = reg2.byLightingCurve(plot, output.ParameterSet.Parameters[3]);  //angle radian
            //    foreach (var c in crv)
            //        c.Translate(Vector3d.ZAxis * reg2.height);
            //    result.AddRange(crv);
            //}
            #region regacy                                              
            //Regulation tempreg = new Regulation(stories);
            //double eyelevel = 1500;
            //foreach (var hhp2 in output.HouseholdProperties)
            //{
            //    foreach (var hhp1 in hhp2)
            //    {
            //        foreach (var hhp in hhp1)
            //        {
            //            var outline = hhp.GetOutline();
            //            for (int i = 0; i < hhp.LightingEdge.Count; i++)
            //            {
            //                var start = hhp.LightingEdge[i].PointAt(0.5);
            //                var dir1 = hhp.LightingEdge[i].UnitTangent;
            //                dir1.Rotate(Math.PI / 2, Vector3d.ZAxis);

            //                var testpoint = start + dir1;
            //                if (outline.Contains(testpoint) == PointContainment.Inside)
            //                    dir1.Reverse();

            //                var dir2 = -Vector3d.ZAxis;
            //                var length2 = hhp.Origin.Z + eyelevel;
            //                var length1 = length2 * tempreg.Lightingk;


            //                LineCurve newline = new LineCurve(new Line(start + Vector3d.ZAxis * eyelevel, (dir1 * length1 + dir2 * length2)));
            //                result.Add(newline);
            //            }
            //        }
            //    }
            //}
            //#endregion

            //return result;

            #endregion

            #region PrintLine?
            double rotateAngle = Math.PI / 2;

            List<Curve> result = new List<Curve>();

            List<Line> lightingEdgeFront = new List<Line>();
            List<Line> lightingEdgeBack = new List<Line>();

            for (int i = 0; i < output.HouseholdProperties.Last().Count; i++)
            {
                for (int j = 0; j < output.HouseholdProperties.Last()[i].Count; j++)
                {
                    var front = output.HouseholdProperties.Last()[i][j].LightingEdge[0];
                    lightingEdgeFront.Add(front);

                    var back = output.HouseholdProperties.Last()[i][j].LightingEdge[1];
                    lightingEdgeBack.Add(back);
                }
            }



            if (lightingEdgeFront.Count == 0 || lightingEdgeBack.Count == 0)
                return new List<Curve>();

            double height = output.HouseholdProperties.Count * Consts.FloorHeight;
            double posz = height + Consts.PilotiHeight;
            double distance = height * (plot.isSpecialCase ? 0.25 : 0.5);

            Vector3d frontv = lightingEdgeFront[0].Direction;
            frontv.Rotate(rotateAngle, Vector3d.ZAxis);
            frontv.Unitize();

            foreach (var edge in lightingEdgeFront)
            {
                Line edge2 = new Line(edge.From + frontv * distance, edge.To + frontv * distance);
                result.Add(new PolylineCurve(new Point3d[] { edge.From, edge.To, edge2.To, edge2.From, edge.From }));
            }

            foreach (var edge in lightingEdgeBack)
            {
                Line edge2 = new Line(edge.From + -frontv * distance, edge.To + -frontv * distance);
                result.Add(new PolylineCurve(new Point3d[] { edge.From, edge.To, edge2.To, edge2.From, edge.From }));
            }
            #endregion

            Regulation reg = new Regulation(0);
            Curve roadcenter = reg.RoadCenterLines(plot);
            result.Add(roadcenter);

            return result;

        }
        public static List<Curve> LawLines(Plot plot,double stories,bool north)
        {
            var boundary = plot.SimplifiedBoundary;
            var surr = plot.SimplifiedSurroundings;

            List<Curve> result = new List<Curve>();

            for (int i = 0; i <= stories; i++)
            {
                Regulation reg = new Regulation(stories, i);
                if(north)
                {
                    //if (reg.height <= 9000)
                    //    reg = new Regulation(stories,0);
                    
                    var tempfloornorth = reg.fromNorthCurve(plot);
                    foreach(var n in tempfloornorth)
                        n.Transform(Transform.Translation(Vector3d.ZAxis * reg.height));
                    result.AddRange(tempfloornorth);
                }
                else
                {
                    var tempfloorsurr = reg.fromSurroundingsCurve(plot);
                    foreach (var n in tempfloorsurr)
                        n.Transform(Transform.Translation(Vector3d.ZAxis * reg.height));
                    result.AddRange(tempfloorsurr);
                }
               
            }

            return result;
        }

        class myFunc
        {
            public static List<double> internalAngle(Polyline pLine)
            {
                Curve pCrv = pLine.ToNurbsCurve();
                Line[] segments = pLine.GetSegments();
                Curve[] segmentCrvs = new Curve[segments.Length];
                Circle[] circle = new Circle[segments.Length];
                Curve[] circleCrvs = new Curve[segments.Length];
                List<double> output = new List<double>();

                for (int i = 0; i < segments.Length; i++)
                {
                    segmentCrvs[i] = segments[i].ToNurbsCurve();
                    circle[i] = new Circle(segments[i].PointAt(0), 1);
                    circleCrvs[i] = circle[i].ToNurbsCurve();
                }

                Brep boundary = Brep.CreatePlanarBreps(segmentCrvs)[0];

                for (int i = 0; i < circleCrvs.Length; i++)
                {
                    var intersect = Rhino.Geometry.Intersect.Intersection.CurveCurve(circleCrvs[i], pCrv, 0.1, 0.1);
                    List<double> paramsB = new List<double>();

                    Curve[] pieces = circleCrvs[i].Split(boundary, 0);

                    foreach (Curve j in pieces)
                    {
                        double centerParam = j.Domain.Mid;
                        String containStr = pCrv.Contains(j.PointAt(centerParam)).ToString();
                        double length = j.GetLength();

                        if (containStr == "Inside")
                            output.Add(length / 2 / Math.PI * 360);
                    }
                }
                return output;
            }
        }

        public static string GetApartmentType(ApartmentGeneratorOutput agOutput)
        {
            if (agOutput.AGtype == "PT-1")
                return "판상형";
            else if (agOutput.AGtype == "PT-3")
                return "중정형";
            else if (agOutput.AGtype == "PT-4")
                return "ㄷ자형";
            else
                return "null";
        }

        public static string GetApartmentType(string PatternName)
        {
            if (PatternName == "PT-1")
                return "판상형";
            else if (PatternName == "PT-3")
                return "중정형";
            else if (PatternName == "PT-4")
                return "ㄷ자형";
            else
                return "null";
        }

        public static Curve scaleCurve(Curve input, double ScaleFactor)
        {
            Curve inputCopy = input.DuplicateCurve();

            inputCopy.Transform(Transform.Scale(KDG.getInstance().center, ScaleFactor));

            return inputCopy;
        }


        /// <summary>
        /// 원점기준 스케일, 0530
        /// </summary>
        /// <param name="input"></param>
        /// <param name="ScaleFactor"></param>
        /// <returns></returns>

        public static List<Curve> scaleCurves(List<Curve> input, double ScaleFactor)
        {
            foreach (Curve c in input)
            {
                c.Transform(Transform.Scale(KDG.getInstance().center, ScaleFactor));
            }

            return input;
        }


        public static double getArea(Curve input)
        {
            Polyline plotPolyline;
            input.TryGetPolyline(out plotPolyline);

            List<Point3d> y = new List<Point3d>(plotPolyline);
            double area = 0;

            for (int i = 0; i < y.Count - 1; i++)
            {
                area += y[i].X * y[i + 1].Y;
                area -= y[i].Y * y[i + 1].X;
            }

            area /= 2;

            return (area < 0 ? -area : area);
        }

        public static double GetBuildingCoverage_outSideAGOutput(List<List<List<HouseholdProperties>>> householdProperties, List<List<CoreProperties>> coreProperties, Plot plot)
        {
            double ExclusiveAreaSum = 0;

            foreach (List<List<HouseholdProperties>> i in householdProperties)
            {

                foreach (HouseholdProperties k in i[0])
                {
                    ExclusiveAreaSum += k.GetExclusiveArea();
                }

            }

            foreach (List<CoreProperties> i in coreProperties)
            {
                foreach (CoreProperties j in i)
                {
                    ExclusiveAreaSum += j.GetArea();
                }
            }

            return ExclusiveAreaSum / plot.GetArea() * 100;
        }

        //public static double GetGrossArea_outSideAGOutput(List<List<List<HouseholdProperties>>> householdProperties, List<List<CoreProperties>> coreProperties, Plot plot)
        // {
        //     double ExclusiveAreaSum = 0;

        //     foreach (List<List<HouseholdProperties>> i in householdProperties)
        //     {
        //         foreach (List<HouseholdProperties> j in i)
        //         {
        //             foreach (HouseholdProperties k in j)
        //             {


        //                 ExclusiveAreaSum += k.GetExclusiveArea();
        //                 ExclusiveAreaSum += k.GetWallArea();


        //             }
        //         }
        //     }

        //     foreach (List<CoreProperties> i in coreProperties)
        //     {
        //         foreach (CoreProperties j in i)
        //         {
        //             double dkdk = j.GetArea();
        //             ExclusiveAreaSum += j.GetArea() * (j.Stories + 1);

        //         }
        //     }

        //     return ExclusiveAreaSum / plot.GetArea() * 100;
        // }

        //public static double GetGrossArea_outSideAGOutput(List<List<List<HouseholdProperties>>> householdProperties, List<List<CoreProperties>> coreProperties, List<HouseholdProperties> publicFacilitires, List<HouseholdProperties> commercialFacilities , Plot plot)
        //{
        //    double ExclusiveAreaSum = 0;

        //    foreach(List<List<HouseholdProperties>> i in householdProperties)
        //    {
        //        foreach(List<HouseholdProperties> j in i)
        //        {
        //            foreach(HouseholdProperties k in j)
        //            {
        //                ExclusiveAreaSum += k.GetExclusiveArea();
        //                ExclusiveAreaSum += k.GetWallArea();
        //            }
        //        }
        //    }

        //    foreach(HouseholdProperties i in publicFacilitires)
        //    {
        //        ExclusiveAreaSum += i.GetExclusiveArea();
        //    }

        //    foreach(HouseholdProperties i in commercialFacilities)
        //    {
        //        ExclusiveAreaSum += i.GetExclusiveArea();
        //    }

        //    foreach(List<CoreProperties> i in coreProperties)
        //    {
        //        foreach(CoreProperties j in i)
        //        {
        //            ExclusiveAreaSum += j.GetArea() * (j.Stories + 1);
        //        }
        //    }

        //    return ExclusiveAreaSum / plot.GetArea() * 100;
        //}

        public static int toplevel(List<List<CoreProperties>> coreProperties)
        {
            int result = 0;
            foreach (var i in coreProperties)
            {
                foreach (var j in i)
                {
                    if (j.Stories > result)
                        result = int.Parse(Math.Truncate(j.Stories).ToString());

                }
            }

            return result;
        }


        private static int householdPropertyCounter(List<List<List<HouseholdProperties>>> ObjectToCount)
        {
            int output = 0;

            foreach (List<List<HouseholdProperties>> i in ObjectToCount)
            {
                foreach (List<HouseholdProperties> j in i)
                {
                    output += j.Count();
                }
            }

            return output;
        }

        class Building
        {
            List<CoreLine> corelines = new List<CoreLine>();
        }
        class CoreLine
        {
            List<Floor> floors = new List<Floor>();
        }
        class Floor
        {
            List<HouseholdProperties> hhps = new List<HouseholdProperties>();
            public Floor(List<HouseholdProperties> hhps)
            {
                this.hhps = hhps;
            }
        }

        public static void reduceFloorAreaRatio_Mixref (ref List<List<List<HouseholdProperties>>> householdProperties_Original, ref List<List<CoreProperties>> coreProperties_Original, ref double currentFloorAreaRatio, Plot plot, double legalFloorAreaRatio, bool overload)
        {
            //기존 L<L<L<hhp>>> = 동(층(호)))

            //계산시 복도형 - 동/층/호 순서로.
            List<Dong> dongs = new List<Dong>();

            for (int i = 0; i < householdProperties_Original.Count; i++)
            {
                //동 하나씩
                var dong = householdProperties_Original[i];
                var dongcore = coreProperties_Original[i];

                var firstfloor = dong[0];
                var hocount = firstfloor.Count;

                if (dong.Count == 0 || dongcore.Count == 0)
                    continue;

                var temp = new Dong(dong, dongcore);
                dongs.Add(temp);
                
            }
            double tempfar = grossArea(householdProperties_Original, coreProperties_Original) / plot.GetArea() * 100;

            //WriteLog(householdProperties_Original);

            while ( tempfar > legalFloorAreaRatio)
            {
                
                int minindex = 0;
                double minval = double.MaxValue;
                double target = tempfar - legalFloorAreaRatio;
                for (int i = 0; i < dongs.Count; i++)
                {
                    if (dongs[i].isCorridorType)
                    {
                        if (dongs[i].Dedicates.Count != 0)
                        {
                            minindex = i;
                            break;
                        }
                    }
                    var tempval = dongs[i].Dedicates.Sum(n => n.GetArea()) / plot.GetArea() * 100;
                    var targetdist = Math.Abs(target - tempval);
                    if (targetdist < minval && tempval != 0)
                    {
                        minval = targetdist;
                        minindex = i;
                    }
                    
                }
                if (dongs[minindex].Dedicates.Count == 0)
                    break;

                dongs[minindex].RemoveDedicates();
               
                tempfar = grossArea(householdProperties_Original, coreProperties_Original) / plot.GetArea() * 100;
                //WriteLog(householdProperties_Original);
            }

            
        }

        public static void WriteLog(List<List<List<HouseholdProperties>>> hhps)
        {
            Rhino.RhinoApp.WriteLine("현재 hhp 개요");
            Rhino.RhinoApp.WriteLine();
            for (int i = 0; i < hhps.Count; i++)
            {
                Rhino.RhinoApp.WriteLine("{0} 동 " , i+1);



                for (int j = 0; j < hhps[i].Count; j++)
                {
                    Rhino.RhinoApp.Write("{0} 층 ", j + 1);

                    for (int k = 0; k < hhps[i][0].Count; k++)
                    {
                        bool empty = true;
                        for (int l = 0; l < hhps[i][j].Count; l++)
                        {
                            if (Math.Round(hhps[i][0][k].EntrancePoint.X) == Math.Round(hhps[i][j][l].EntrancePoint.X) &&
                                Math.Round(hhps[i][0][k].EntrancePoint.Y) == Math.Round(hhps[i][j][l].EntrancePoint.Y))
                            {
                                empty = false;
                                Rhino.RhinoApp.Write("  [{0} 호]", k + 1);
                                break;
                            }

                        }
                        if(empty)
                            Rhino.RhinoApp.Write("  [빈칸]");

                    }

                    Rhino.RhinoApp.WriteLine();
                }
                Rhino.RhinoApp.WriteLine();
            }
        }

        class Dong
        {
            List<List<HouseholdProperties>> units = new List<List<HouseholdProperties>>();
            List<CoreProperties> cores = new List<CoreProperties>();
            List<HouseholdProperties> dedicates = new List<HouseholdProperties>();
            List<CoreProperties> dedicatescore = new List<CoreProperties>();


            public Dong(List<List<HouseholdProperties>> hhps, List<CoreProperties> cps)
            {
                units = hhps;
                cores = cps;
            }

            public List<HouseholdProperties> FirstFloor { get { return units[0]; } }
            public int HoCount { get { return FirstFloor.Count; } }
            public int[] FloorsCount { get { return units.Select(n => n.Count).ToArray(); } }
            public List<HouseholdProperties> Dedicates { get { dedicates = GetDedicates(); return dedicates; } }
            public bool isCorridorType { get { return isCorridor(); } }
            public List<HouseholdProperties> LastFloor { get { return units.Last(); } }
            bool isCorridor()
            {
                if (cores.Count == 0)
                    return false;
                if (HoCount / cores.Count == 2 && FirstFloor[0].XLengthB != 0)
                    return false;
                else
                    return true;
            }
            public void RemoveDedicates()
            {
                for (int d = 0; d < dedicates.Count; d++)
                {
                    var dedicatet = dedicates[d];
                    for (int i = 0; i < units.Count; i++)
                    {
                        for (int j = 0; j < units[i].Count; j++)
                        {
                            if (dedicatet == units[i][j])
                            {
                                units[i].Remove(units[i][j]);
                                break;
                            }
                        }
                    }
                }
                for (int i = 0; i < dedicatescore.Count; i++)
                {
                    if (i % 2 == 0)
                    {
                        if (dedicatescore[i] == null)
                            continue;

                        dedicatescore[i].Stories--;
                        if (dedicatescore[i].Stories == 0)
                            cores.Remove(dedicatescore[i]);
                    }
                }
               
            }
            List<HouseholdProperties> GetDedicates()
            {

                List<HouseholdProperties> resultdedicates = new List<HouseholdProperties>();
                if (isCorridor())
                {   //3층 이하라면 안깎음
                    if (units.Count < 3 || cores.Count == 1)
                    {
                        return new List<HouseholdProperties>();
                    }
                    //3층 이상이면 깎음
                    else
                    {
                        //[첫 코어] [마지막 코어] 사이의 유닛 제외, 외곽유닛부터 ,
                        //사이의유닛구하기
                        List<HouseholdProperties> outside = new List<HouseholdProperties>();

                        int firstmeet = -1;
                        int lastmeet = -1;
                        bool checkedinlast = false;
                        for (int i = 0; i < FirstFloor.Count; i++)
                        {
                            //i번째 유닛
                            var firstflooroutlilne = FirstFloor[i].GetOutline();
                            var v = new Vector3d(0, 0, -1) * firstflooroutlilne.PointAtStart.Z;
                            firstflooroutlilne.Transform(Transform.Translation(v));

                            //첫 코어와 만나기 전
                            if (firstmeet == -1)
                            {
                                Point3d coreorigin = new Point3d(cores.First().Origin.X, cores.First().Origin.Y, 0);

                                var firstcoreoutline = new PolylineCurve(new Point3d[] {
                                    coreorigin,
                                    coreorigin + cores.First().XDirection,
                                    coreorigin + cores.First().XDirection + cores.First().YDirection,
                                    coreorigin + cores.First().YDirection,
                                    coreorigin
                                });

                                var ccxresult = Curve.PlanarCurveCollision(firstflooroutlilne, firstcoreoutline, Plane.WorldXY, 0);
                                if (ccxresult)
                                    firstmeet = i;
                            }

                            //첫 코어 만난 후
                            else
                            {
                                Point3d coreorigin = new Point3d(cores.Last().Origin.X, cores.Last().Origin.Y, 0);

                                var lastcoreoutline = new PolylineCurve(new Point3d[] {
                                    coreorigin,
                                    coreorigin + cores.Last().XDirection,
                                    coreorigin + cores.Last().XDirection + cores.Last().YDirection,
                                    coreorigin + cores.Last().YDirection,
                                    coreorigin
                                });

                                var ccxresult = Curve.PlanarCurveCollision(firstflooroutlilne, lastcoreoutline, Plane.WorldXY, 0);

                                //마지막 코어 만나기 전
                                if (ccxresult && !checkedinlast)
                                {
                                    lastmeet = i;
                                    checkedinlast = true;
                                }
                                //마지막 코어 만난 후
                                else if (!ccxresult && checkedinlast)
                                {
                                    lastmeet = i;
                                    break;
                                }
                            }
                        }

                        //층별로
                        for (int i = 0; i < units.Count; i++)
                        {
                            //맨윗층부터
                            int floor = units.Count - 1 - i;
                            //1층평면의 , 해당 인덱스의 unit  origin x,y 가 같은 유닛이 있으면 
                            for (int j = 0; j < FirstFloor.Count; j++)
                            {
                                if (j < firstmeet || j > lastmeet+1)
                                {
                                    var tempunitorigin = FirstFloor[j].Origin;

                                    for (int k = 0; k < units[floor].Count; k++)
                                    {
                                        if (units[floor][k].Origin.X == tempunitorigin.X && units[floor][k].Origin.Y == tempunitorigin.Y)
                                        {
                                            resultdedicates.Add(units[floor][k]);
                                            return resultdedicates;
                                        }
                                    }
                                }//inrange
                            }//firstfloorunitsloop
                        }//floorloop
                        return new List<HouseholdProperties>();
                    }//unitcount > 3
                   
                }//ifcorridor?
                else
                {
                    //min / max
                    List<List<HouseholdProperties>> byrow = new List<List<HouseholdProperties>>();
                    for (int i = 0; i < FirstFloor.Count; i++)
                    {
                        //제 1 열 
                        var rowtempX = Math.Round(FirstFloor[i].Origin.X);
                        var rowtempY = Math.Round(FirstFloor[i].Origin.Y);
                        List<HouseholdProperties> temprow = new List<HouseholdProperties>();
                        List<HouseholdProperties> wholehhps = new List<HouseholdProperties>();
                        units.ForEach(n => wholehhps.AddRange(n));

                        foreach (var hhp in wholehhps)
                        {
                            if (Math.Round(hhp.Origin.X) == rowtempX && Math.Round(hhp.Origin.Y) == rowtempY)
                            {
                                temprow.Add(hhp);
                            }
                        }
                        //for (int j = 0; j < units.Count; j++)
                        //{
                        //    //for (int k = 0; k < units[j].Count; k++)
                        //    //{
                        //    //    var tempunit = units[j][k];
                        //    //    if (tempunit.Origin.X == rowtempX && tempunit.Origin.Y == rowtempY)
                        //    //        temprow.Add(tempunit);
                        //    //}
                        //    if (units[j].Count == FirstFloor.Count)
                        //    {
                        //        temprow.Add(units[j][i]);
                        //    }
                        //    else
                        //    {
                        //        for (int k = 0; k < units[j].Count; k++)
                        //        {
                        //            var tempunit = units[j][k];
                        //            if (tempunit.Origin.X == rowtempX && tempunit.Origin.Y == rowtempY)
                        //            {
                        //                temprow.Add(tempunit);
                        //                break;
                        //            }
                        //        }
                        //    }

                        //}

                        byrow.Add(temprow.OrderBy(n=>n.EntrancePoint.Z).ToList());
                    }

                    //우선순위  //옆칸과 다름   3 < floor < max , max , 3 

                    if(byrow.Count == 0)
                        return new List<HouseholdProperties>();

                    for (int i = 0; i < byrow.Count / 2; i++)
                    {
                        if (byrow[i * 2].Count != byrow[i * 2 + 1].Count)
                        {
                            var largeside = byrow[i * 2].Count > byrow[i * 2 + 1].Count ? byrow[i * 2] : byrow[i * 2 + 1];
                            resultdedicates.Add(largeside.OrderByDescending(n => n.EntrancePoint.Z).First());

                            dedicatescore.Add(cores[i]);
                            break;
                        }
                    }

                    if (resultdedicates.Count >= 1)
                    {
                        return resultdedicates;
                    }
                    dedicatescore.Clear();

                    int max = byrow.Max(n => n.Count);

                    for(int i = 0; i < byrow.Count / 2 ;i++)
                    {
                        if (byrow[i].Count > 3 && byrow[i].Count < max)
                        {
                            resultdedicates.Add(byrow[i * 2].OrderByDescending(n => n.EntrancePoint.Z).First());
                            resultdedicates.Add(byrow[i * 2 + 1].OrderByDescending(n => n.EntrancePoint.Z).First());
                            if(cores.Count > i)
                            dedicatescore.Add(cores[i]);
                            break;
                        }
                    }

                    if (resultdedicates.Count >= 1)
                    {
                        return resultdedicates;
                    }
                    dedicatescore.Clear();



                    for (int i = 0; i < byrow.Count/2; i++)
                    {
                        if (byrow[i].Count == max)
                        {
                            resultdedicates.Add(byrow[i * 2].OrderByDescending(n => n.EntrancePoint.Z).First());
                            resultdedicates.Add(byrow[i * 2 + 1].OrderByDescending(n => n.EntrancePoint.Z).First());
                            if (cores.Count > i)
                                dedicatescore.Add(cores[i]);
                            break;
                        }
                    }

                    if (resultdedicates.Count >= 1)
                        return resultdedicates;
                    dedicatescore.Clear();


                    for (int i = 0; i < byrow.Count / 2; i++)
                    {
                        if (byrow[i].Count <= 3)
                        {
                            resultdedicates.Add(byrow[i * 2].OrderByDescending(n => n.EntrancePoint.Z).First());
                            resultdedicates.Add(byrow[i * 2 + 1].OrderByDescending(n => n.EntrancePoint.Z).First());
                            if (cores.Count > i)
                                dedicatescore.Add(cores[i]);
                            break;
                        }
                    }

                    if (resultdedicates.Count >= 1)
                        return resultdedicates;
                    dedicatescore.Clear();


                 
                }
                return new List<HouseholdProperties>();
            }
            //List<CoreProperties> GetDedicores()
            //{
            //    shit!
            //}
        }
        public static void reduceFloorAreaRatio_Vertical(ref List<List<List<HouseholdProperties>>> householdProperties_Original, ref List<List<CoreProperties>> coreProperties_Original, ref double currentFloorAreaRatio, Plot plot, double legalFloorAreaRatio, bool overload)
        {
            //목표 줄일 면적

            //double areaToReduce = (currentFloorAreaRatio - legalFloorAreaRatio) / 100;
            //areaToReduce *= plot.GetArea();
            //double tempAreaToReduce = areaToReduce;
            //int floor = 0;
            //List<HouseholdProperties> namungo = new List<HouseholdProperties>();
            //List<HouseholdProperties> toRemove = new List<HouseholdProperties>();
            //while (tempAreaToReduce > plot.GetArea() / 100)
            //{
            //    floor++;
            //    if (householdProperties_Original.Max(n => n.Count) < floor)
            //        break;
            //    // 층 모든 유닛 모음
            //    List<HouseholdProperties> topfloorunits = new List<HouseholdProperties>();



            //    for (int i = 0; i < householdProperties_Original.Count; i++)
            //    {
            //        //해당 층 유닛 수 / 코어 수 = offsets...일단 좌우부터 ㄱ
            //        if (householdProperties_Original[i].Count - floor < 0)
            //            continue;
            //        var tempfloorunits = householdProperties_Original[i][householdProperties_Original[i].Count - floor];
            //        topfloorunits = tempfloorunits;

            //        //for (int j = 0; j < tempfloorunits.Count; j++)
            //        //{
            //        //    //if (j % 2 == 0)
            //        //    //{
            //        //    //    topfloorunits.Add(tempfloorunits.First());
            //        //    //    tempfloorunits.RemoveAt(0);
            //        //    //}
            //        //    //else
            //        //    //{
            //        //    //    topfloorunits.Add(tempfloorunits.Last());
            //        //    //    tempfloorunits.RemoveAt(tempfloorunits.Count - 1);
            //        //    //}
            //        //}

            //    }

            //    //줄일면적과 비교해서, +- 오차가 가장 작게 세팅

            //    //작은것부터 때려박기

            //    topfloorunits = topfloorunits.OrderBy(n => n.ExclusiveArea).ToList();


            //    List<HouseholdProperties> notthese = new List<HouseholdProperties>();

            //    foreach (var tfu in topfloorunits)
            //    {
            //        foreach(var nmg in namungo)
            //        {
            //            if (tfu.EntrancePoint.X == nmg.EntrancePoint.X && tfu.EntrancePoint.Y == nmg.EntrancePoint.Y)
            //            {
            //                notthese.Add(tfu);
            //                break;
            //            }
            //        }
            //    }

            //    notthese.ForEach(n => topfloorunits.Remove(n));


            //    namungo = namungo.OrderBy(n => n.ExclusiveArea).ToList();

            //    topfloorunits.InsertRange(0, namungo);

            //    double areaSum = 0;

            //    for (int i = 0; i < topfloorunits.Count; i++)
            //    {
            //        //오차 1% 미만 -> 끝
            //        if (Math.Abs(tempAreaToReduce) < plot.GetArea() / 200)
            //            break;

            //        //들어갈 충분한 공간 or 1%미만의 오차 ->넣음
            //        if (tempAreaToReduce > topfloorunits[i].ExclusiveArea || Math.Abs(tempAreaToReduce - topfloorunits[i].ExclusiveArea) < plot.GetArea() / 200)
            //        {
            //            toRemove.Add(topfloorunits[i]);
            //            areaSum = toRemove.Sum(n => n.ExclusiveArea);
            //            tempAreaToReduce = areaToReduce - areaSum;
            //            continue;
            //        }

            //        //공간이 없다.
            //        else
            //        {
            //            //이미 들어가있는 유닛 탐색
            //            for (int j = 0; j < toRemove.Count; j++)
            //            {
            //                // 내면적 - j면적 < tempareaToReduce 면 교체.
            //                if (tempAreaToReduce > topfloorunits[i].ExclusiveArea - toRemove[j].ExclusiveArea
            //                    && topfloorunits[i].ExclusiveArea - toRemove[j].ExclusiveArea > 0)
            //                {

            //                    //if (topfloorunits[i].EntrancePoint.Z != toRemove[j].EntrancePoint.Z)
            //                    //    continue;
            //                    namungo.Add(toRemove[j]);
            //                    toRemove.RemoveAt(j);
            //                    toRemove.Add(topfloorunits[i]);
            //                    areaSum = toRemove.Sum(n => n.ExclusiveArea);
            //                    tempAreaToReduce = areaToReduce - areaSum;
            //                    break;

            //                }
            //            }
            //            namungo.Add(topfloorunits[i]);
            //        }

            //    }

            //   tempAreaToReduce  = areaToReduce - toRemove.Sum(n => n.ExclusiveArea);
            //   toRemove = toRemove.OrderBy(n => n.EntrancePoint.Z).ToList();
            //}


            ////제거
            //foreach (var p in toRemove)
            //{
            //    householdProperties_Original.ForEach(n => n.ForEach(m => m.Remove(p)));
            //}

            ////정렬



            ////동 별로
            ////for (int i = 0; i < householdProperties_Original.Count; i++)
            ////{
            ////    //현재저장된층
            ////    List<HouseholdProperties> tempfloorhhps = new List<HouseholdProperties>();

            ////    //동의 꼭대기층부터 순차적으로
            ////    for (int j = householdProperties_Original[i].Count - 1; j > 1; j--)
            ////    {
            ////        //처음이면
            ////        if (tempfloorhhps == null)
            ////        {
            ////            tempfloorhhps = new List<HouseholdProperties>(householdProperties_Original[i][j]);
            ////            householdProperties_Original[i][j].Clear();
            ////            continue;
            ////        }
            ////        //다 때려박음
            ////        //층 clear

            ////        //아니면
            ////        else
            ////        {
            ////            List<HouseholdProperties> nomore = new List<HouseholdProperties>();
            ////            //현재층 hhps마다
            ////            foreach (var temphhps in tempfloorhhps)
            ////            {
            ////                //아래층에 무엇인가 있는지 체크
            ////                foreach (var nexthhps in householdProperties_Original[i][j])
            ////                {
            ////                    if (temphhps.Origin.X == nexthhps.Origin.X && temphhps.Origin.Y == nexthhps.Origin.Y)
            ////                    {
            ////                        //아래층이 있는녀석 -> 현재에서 제거, 해당층리스트에 넣어줌
            ////                        householdProperties_Original[i][j + 1].Add(temphhps);
            ////                        nomore.Add(temphhps);

            ////                        break;
            ////                    }
            ////                }
            ////            }
            ////            nomore.ForEach(n => tempfloorhhps.Remove(n));
            ////        }

            ////        //아래층없는애들 하나씩 움직임
            ////        tempfloorhhps = tempfloorhhps.Select(n => new HouseholdProperties(n, Consts.FloorHeight)).ToList();
            ////        tempfloorhhps.AddRange(new List<HouseholdProperties>(householdProperties_Original[i][j]));

            ////    }
            ////}


            //for (int i = 0; i < householdProperties_Original.Count; i++)
            //{
            //    //동에 소속된 hhps
            //    var tempBuildinghhps = householdProperties_Original[i];

            //    //동에 소속된 core
            //    var tempBuildingcps = coreProperties_Original[i];

            //    //각 동 별로 가까운 hhps (1~n) 가 없다면 core 삭제 ( 0 ~ n+1)
            //    int cpscount = tempBuildingcps.Count;
            //    int hhpscount = tempBuildinghhps.Count;


            //    //각 층의 각 세대에 대해, 가장 가까운 coreproperty와 평면거리 list 구성
            //    //지목이 되지 않은 coreproperty는 층 --
            //    //현재 층
            //    for (int j = tempBuildinghhps.Count-1; j >= 0; j--)
            //    {
            //        //현재 층의 hhps
            //        var tempfloorhhps = tempBuildinghhps[j];
            //        //현재 층의 hhps가 선택한, 가까운 코어 들의 index
            //        List<int> closestcoreindex = new List<int>();

            //        //각 hhps마다
            //        foreach (var hhps in tempfloorhhps)
            //        {
            //            //가장 가까운 코어를 찾아 index 저장
            //            double mindistance = double.MaxValue;
            //            int mindistanceindex = -1;
            //            Point3d minpoint = Point3d.Unset;
            //            foreach (var cps in tempBuildingcps)
            //            {
            //                var testpoint = cps.Origin + cps.XDirection * cps.CoreType.GetWidth() / 2 + cps.YDirection * cps.CoreType.GetDepth()/2 + Vector3d.ZAxis * hhps.EntrancePoint.Z;
            //                var distance = testpoint.DistanceTo(hhps.EntrancePoint);
            //                if (distance < mindistance)
            //                {
            //                    mindistance = distance;
            //                    mindistanceindex = tempBuildingcps.IndexOf(cps);
            //                    minpoint = testpoint;
            //                }
            //            }
            //            //Rhino.RhinoDoc.ActiveDoc.Objects.Add(new LineCurve(hhps.EntrancePoint, minpoint));
            //            closestcoreindex.Add(mindistanceindex);
            //        }

            //        //각 코어 마다
            //        for (int k = 0; k < tempBuildingcps.Count; k++)
            //        {
            //            //closestcoreindex에 index k 가 포함되어있지 않다면
            //            if (!closestcoreindex.Contains(k))
            //            {
            //                //층 감소
            //                tempBuildingcps[k].Stories--;
            //            }
            //            else
            //            { 

            //            }

            //        }
            //    }

            //}

            //라인별로 재정렬
            List<List<List<HouseholdProperties>>> hhpsl = new List<List<List<HouseholdProperties>>>();
            double land = plot.GetArea();
            for (int i = 0; i < householdProperties_Original.Count; i++)
            {
                //건물별
                //층 수 * 열 수 리스트
                //층 수
                int floors = householdProperties_Original[i].Count;
                //열 수
                int rows = householdProperties_Original[i][floors - 1].Count;
                //열 수 * 층 수 크기의 배열 선언
                HouseholdProperties[,] hhps = new HouseholdProperties[rows, floors];
                for (int j = 0; j < floors; j++)
                {
                    for (int k = 0; k < rows; k++)
                    {
                        //열,층 칸에 층,열 넣음
                        householdProperties_Original[i][j][k].indexer = new int[] { k, j };
                        hhps[k, j] = householdProperties_Original[i][j][k];
                    }
                }
                List<List<HouseholdProperties>> tempfloor = new List<List<HouseholdProperties>>();
                for (int j = 0; j < rows; j++)
                {
                    List<HouseholdProperties> temprow = new List<HouseholdProperties>();
                    for (int k = 0; k < floors; k++)
                    {
                        temprow.Add(hhps[j, k]);
                    }
                    tempfloor.Add(temprow);
                }
                hhpsl.Add(tempfloor);
            }
            double tempratio = currentFloorAreaRatio;

            int buildingnum = 0;
            int rownum = 0;
            int rownum2 = 0;
            int rownumalpha = 0;
            bool rowfirst = true;
            bool buildinglast = false;
            bool ranonece = false;

            
            while (tempratio > legalFloorAreaRatio * 1.01)
            {
                // 복도형이 아니고, 첫번째가 아니면
                if (hhpsl[buildingnum].Count / 2 != coreProperties_Original[buildingnum].Count && !ranonece)
                {
                    //다음건물
                    buildingnum++;
                    //현재건물인덱스가 마지막이면
                    if (buildingnum >= hhpsl.Count - 1)
                    {
                        //처음으로
                        buildingnum = 0;
                        ranonece = true;
                    }
                    continue;
                }

                //현재 [건물][열]에 지울 칸이 없고
                if (hhpsl[buildingnum][rownum].Count == 0)
                {

                    //coreProperties_Original[buildingnum][rownum / 2].Stories = 0;


                    //[첫열이 아니고 마지막 건물] 일때
                    if (!rowfirst && buildinglast)
                    {
                        rownumalpha++;
                        rowfirst = true;
                        buildinglast = false;
                    }
                    else if (rowfirst)
                    {
                        rownum = hhpsl[buildingnum].Count - (1 + rownumalpha);
                        rowfirst = false;
                    }
                    else
                    {
                        rownum = 0 + rownumalpha;
                        buildingnum++;


                        if (buildingnum == hhpsl.Count)
                        {
                            buildinglast = false;
                            buildingnum = 0;
                        }
                        else if (buildingnum == hhpsl.Count - 1)
                        {
                            buildinglast = true;
                        }
                        rowfirst = true;

                    }
                }
                //지울칸이충분하다
                //두칸을 지운다
                else
                {
                    if (rowfirst)
                    {
                        rownum2 = rownum + 1;
                    }
                    else
                    {
                        rownum2 = rownum - 1;
                    }

                    var hhp1 = hhpsl[buildingnum][rownum][hhpsl[buildingnum][rownum].Count - 1];
                    foreach (var z in householdProperties_Original[buildingnum])
                    {
                        if (z.Contains(hhp1))
                        {
                            z.Remove(hhp1);
                            break;
                        }
                    }
                    hhpsl[buildingnum][rownum].RemoveAt(hhpsl[buildingnum][rownum].Count - 1);
                    //var mindistance = coreProperties_Original[buildingnum].Select(n => n.Origin.DistanceTo(hhp1.Origin)).Min();
                    //var closesetcore = coreProperties_Original[buildingnum].Where(n => n.Origin.DistanceTo(hhp1.Origin) == mindistance).ToList();
                    //closesetcore[0].Stories--;

                    //tempratio -= closesetcore[0].GetArea() / land * 100;

                    if (hhpsl[buildingnum][rownum2].Count > 0)
                    {
                        var hhp2 = hhpsl[buildingnum][rownum2][hhpsl[buildingnum][rownum2].Count - 1];
                        foreach (var z in householdProperties_Original[buildingnum])
                        {
                            if (z.Contains(hhp2))
                            {
                                z.Remove(hhp2);
                                break;
                            }
                        }
                        hhpsl[buildingnum][rownum2].RemoveAt(hhpsl[buildingnum][rownum2].Count - 1);
                    }


                    for (int i = 0; i < householdProperties_Original.Count; i++)
                    {
                        //동에 소속된 hhps
                        var tempBuildinghhps = householdProperties_Original[i];

                        //동에 소속된 core
                        var tempBuildingcps = coreProperties_Original[i];

                        //각 동 별로 가까운 hhps (1~n) 가 없다면 core 삭제 ( 0 ~ n+1)
                        int cpscount = tempBuildingcps.Count;
                        int hhpscount = tempBuildinghhps.Count;


                        //각 층의 각 세대에 대해, 가장 가까운 coreproperty와 평면거리 list 구성
                        //지목이 되지 않은 coreproperty는 층 --
                        //현재 층
                        for (int j = tempBuildinghhps.Count - 1; j >= 0; j--)
                        {
                            //현재 층의 hhps
                            var tempfloorhhps = tempBuildinghhps[j];
                            //현재 층의 hhps가 선택한, 가까운 코어 들의 index
                            List<int> closestcoreindex = new List<int>();

                            //각 hhps마다
                            foreach (var hhps in tempfloorhhps)
                            {
                                //가장 가까운 코어를 찾아 index 저장
                                double mindistance = double.MaxValue;
                                int mindistanceindex = -1;
                                Point3d minpoint = Point3d.Unset;
                                foreach (var cps in tempBuildingcps)
                                {
                                    var testpoint = cps.Origin + cps.XDirection * cps.CoreType.GetWidth() / 2 + cps.YDirection * cps.CoreType.GetDepth() / 2 + Vector3d.ZAxis * hhps.EntrancePoint.Z;
                                    var distance = testpoint.DistanceTo(hhps.EntrancePoint);
                                    if (distance < mindistance)
                                    {
                                        mindistance = distance;
                                        mindistanceindex = tempBuildingcps.IndexOf(cps);
                                        minpoint = testpoint;
                                    }
                                }
                                Rhino.RhinoDoc.ActiveDoc.Objects.Add(new LineCurve(hhps.EntrancePoint, minpoint));
                                closestcoreindex.Add(mindistanceindex);
                            }

                            //각 코어 마다
                            for (int k = 0; k < tempBuildingcps.Count; k++)
                            {
                                //closestcoreindex에 index k 가 포함되어있지 않다면
                                if (!closestcoreindex.Contains(k))
                                {
                                    //층 감소
                                    tempBuildingcps[k].Stories--;


                                    if (tempBuildingcps[k].Stories <= 1)
                                    {
                                        tempBuildingcps.RemoveAt(k);
                                    }
                                }
                            }
                        }
                        tempratio = grossArea(householdProperties_Original, coreProperties_Original) * 100 / plot.GetArea();
                    }


                }

                

                if (tempratio <= legalFloorAreaRatio * 1.01)
                {
                    return;
                }
            }



        }

        public static double grossArea(List<List<List<HouseholdProperties>>> HouseholdProperties,List<List<CoreProperties>> CoreProperties)
        {
            double output = 0;

            for (int i = 0; i < HouseholdProperties.Count; i++)
            {
                for (int j = 0; j < HouseholdProperties[i].Count; j++)
                {
                    for (int k = 0; k < HouseholdProperties[i][j].Count; k++)
                    {

                        output += HouseholdProperties[i][j][k].GetExclusiveArea();

                        output += HouseholdProperties[i][j][k].GetWallArea();

                    }
                }

                foreach (CoreProperties j in CoreProperties[i])
                {
                    output = output + j.GetArea() * (j.Stories + 1);
                }

            }

            return output;
        }

        public static void reduceFloorAreaRatio_Vertical(ref List<List<List<HouseholdProperties>>> householdProperties_Original, ref List<List<CoreProperties>> coreProperties_Original, ref double currentFloorAreaRatio, Plot plot, double legalFloorAreaRatio)
        {
            double thisFloorAreaRatio = currentFloorAreaRatio;
            List<List<List<HouseholdProperties>>> householdProperties = new List<List<List<HouseholdProperties>>>(householdProperties_Original);
            List<List<CoreProperties>> coreProperties = new List<List<CoreProperties>>(coreProperties_Original);

            //////// 용적률이 너무 크면 제거

            List<List<int>> coreRanks = new List<List<int>>();

            //for (int i = 0; i < coreProperties_Original.Count(); i++)
            //{
            //    List<CoreProperties> otherCoreProperties = new List<CoreProperties>();

            //    for (int j = 0; j < coreProperties_Original.Count(); j++)
            //    {
            //        if (i != j)
            //            otherCoreProperties.AddRange(coreProperties_Original[j]);
            //    }

            //    coreRanks.Add(RankCoreDIstance(coreProperties_Original[i], otherCoreProperties));
            //}

            if (thisFloorAreaRatio > legalFloorAreaRatio)
            {
                coreRanks.Clear();


                for (int i = 0; i < coreProperties.Count(); i++)
                {
                    List<CoreProperties> otherCoreProperties = new List<CoreProperties>();

                    for (int j = 0; j < coreProperties.Count(); j++)
                    {
                        if (i != j)
                            otherCoreProperties.AddRange(coreProperties[j]);
                    }

                    coreRanks.Add(RankCoreDIstance(coreProperties[i], otherCoreProperties));
                }


                int tempApartmentIndex = 0;
                int tempCoreFromLastIndex = 0;
                bool isSomeThingRemoved = false;
                int coreRemoved = 0;
                while (thisFloorAreaRatio > legalFloorAreaRatio)
                {
                    int tempCoreIndex = int.MaxValue;
                    int tempTempCoreFromLastIndexSum = 0;

                    if (coreRanks[tempApartmentIndex].Count() != 0)
                        tempCoreIndex = coreRanks[tempApartmentIndex].IndexOf((tempCoreFromLastIndex + (coreRanks[tempApartmentIndex].Max() + 1 + tempTempCoreFromLastIndexSum)) % (coreRanks[tempApartmentIndex].Max() + 1));
                    else
                        tempCoreIndex = -1;


                    if (tempCoreIndex != -1)
                    {
                        List<List<HouseholdProperties>> tempHouseholdProperties = new List<List<HouseholdProperties>>(householdProperties[tempApartmentIndex]);

                        double tempExpectedAreaRemove = 0;
                        List<int> tempRemoveHouseholdPropertyStoriesIndex = new List<int>();
                        List<int> tempRemoveHouseholdPropertyIndex = new List<int>();

                        List<Point3d> tempCorePropertiesOrigin = (from coreProperty in coreProperties[tempApartmentIndex]
                                                                  select coreProperty.Origin).ToList();

                        List<List<int>> tempClosestCorePropertyIndex = new List<List<int>>();

                        for (int i = 0; i < tempHouseholdProperties.Count(); i++)
                        {
                            tempClosestCorePropertyIndex.Add(new List<int>());

                            for (int j = 0; j < tempHouseholdProperties[i].Count(); j++)
                            {
                                Point3d tempHouseholdOrigin = tempHouseholdProperties[i][j].Origin;

                                RhinoList<double> tempDistance = new RhinoList<double>();
                                RhinoList<int> tempCorePropertyIndex = new RhinoList<int>();

                                for (int k = 0; k < tempCorePropertiesOrigin.Count(); k++)
                                {
                                    tempCorePropertyIndex.Add(k);
                                    tempDistance.Add(Math.Pow(Math.Pow(tempCorePropertiesOrigin[k].X - tempHouseholdOrigin.X, 2) + Math.Pow(tempCorePropertiesOrigin[k].Y - tempHouseholdOrigin.Y, 2), 0.5) + tempHouseholdOrigin.Z);
                                }

                                tempCorePropertyIndex.Sort(tempDistance.ToArray());
                                tempClosestCorePropertyIndex[i].Add(tempCorePropertyIndex[0]);
                            }
                        }

                        RhinoList<int> tempSelectedHouseholdIndex = new RhinoList<int>();
                        RhinoList<int> tempSelectedHouseholdFloor = new RhinoList<int>();

                        for (int i = 0; i < tempHouseholdProperties.Count(); i++)
                        {
                            for (int j = 0; j < tempHouseholdProperties[i].Count(); j++)
                            {
                                if (tempClosestCorePropertyIndex[i][j] == tempCoreIndex)
                                {
                                    tempSelectedHouseholdIndex.Add(j);
                                    tempSelectedHouseholdFloor.Add(i);
                                }
                            }
                        }

                        tempSelectedHouseholdFloor.Sort(tempSelectedHouseholdIndex.ToArray());
                        tempSelectedHouseholdIndex.Sort(tempSelectedHouseholdIndex.ToArray());
                        tempSelectedHouseholdIndex.Sort(tempSelectedHouseholdFloor.ToArray());
                        tempSelectedHouseholdFloor.Sort(tempSelectedHouseholdFloor.ToArray());

                        tempSelectedHouseholdIndex.Reverse();
                        tempSelectedHouseholdFloor.Reverse();

                        RhinoList<int> tempRemoveHouseholdIndex = new RhinoList<int>();
                        RhinoList<int> tempRemoveHosueholdFloor = new RhinoList<int>();
                        int coreRemoveCount = 0;

                        //if (tempSelectedHouseholdFloor.Count == 0)
                        //{
                        //    if (tempCoreIndex == coreProperties[tempApartmentIndex].Count)
                        //        tempApartmentIndex++;
                            
                        //    continue;
                        //}
                        List<List<int>> selectedHouseholdIndexByFloor = new List<List<int>>(tempSelectedHouseholdFloor.Max());

                        for (int i = 0; i <= tempSelectedHouseholdFloor.Max(); i++)
                        {
                            selectedHouseholdIndexByFloor.Add(new List<int>());
                        }

                        for (int i = 0; i < tempSelectedHouseholdIndex.Count(); i++)
                        {
                            selectedHouseholdIndexByFloor[tempSelectedHouseholdFloor[i]].Add(tempSelectedHouseholdIndex[i]);
                        }

                        List<int> minHouseholdIndexByFloor = new List<int>();

                        for (int i = 0; i < selectedHouseholdIndexByFloor.Count(); i++)
                        {
                            minHouseholdIndexByFloor.Add(selectedHouseholdIndexByFloor[i].Min());
                        }


                        for (int i = selectedHouseholdIndexByFloor.Count - 1; i >= 0; i--)
                        {
                            selectedHouseholdIndexByFloor[i].Sort();
                            selectedHouseholdIndexByFloor[i].Reverse();

                            for (int j = 0; j < selectedHouseholdIndexByFloor[i].Count(); j++)
                            {
                                if (thisFloorAreaRatio - tempExpectedAreaRemove / plot.GetArea() * 100 > legalFloorAreaRatio)
                                {
                                    tempExpectedAreaRemove += householdProperties[tempApartmentIndex][i][selectedHouseholdIndexByFloor[i][j]].GetExclusiveArea();
                                    tempExpectedAreaRemove += householdProperties[tempApartmentIndex][i][selectedHouseholdIndexByFloor[i][j]].GetWallArea();

                                    tempRemoveHouseholdIndex.Add(selectedHouseholdIndexByFloor[i][j]);
                                    tempRemoveHosueholdFloor.Add(i);

                                    if (selectedHouseholdIndexByFloor[i][j] == minHouseholdIndexByFloor[i])
                                    {
                                        coreRemoveCount++;

                                        tempExpectedAreaRemove += coreProperties[tempApartmentIndex][tempCoreIndex].GetArea();
                                    }
                                }
                            }
                        }

                        if (tempRemoveHouseholdIndex.Count != 0)
                        {
                            isSomeThingRemoved = true;

                            tempRemoveHosueholdFloor.Sort(tempRemoveHouseholdIndex.ToArray());
                            tempRemoveHouseholdIndex.Sort(tempRemoveHouseholdIndex.ToArray());

                            tempRemoveHouseholdIndex.Reverse();
                            tempRemoveHosueholdFloor.Reverse();

                            for (int i = 0; i < tempRemoveHosueholdFloor.Count(); i++)
                            {
                                householdProperties[tempApartmentIndex][tempRemoveHosueholdFloor[i]].RemoveAt(tempRemoveHouseholdIndex[i]);
                            }

                            thisFloorAreaRatio -= tempExpectedAreaRemove / plot.GetArea() * 100;

                            if (coreRemoveCount != 0)
                            {
                                coreProperties[tempApartmentIndex][tempCoreIndex].Stories -= coreRemoveCount;
                                //thisFloorAreaRatio -= (coreProperties[tempApartmentIndex][tempCoreIndex].GetArea() * (coreRemoveCount)) / plot.GetArea() * 100;
                            }

                            if (tempRemoveHosueholdFloor.Count() == tempSelectedHouseholdFloor.Count())
                            {
                                thisFloorAreaRatio -= (coreProperties[tempApartmentIndex][tempCoreIndex].GetArea() * (coreProperties[tempApartmentIndex][tempCoreIndex].Stories + 1)) / plot.GetArea() * 100;

                                coreProperties[tempApartmentIndex].RemoveAt(tempCoreIndex);
                            }

                        }
                    }

                    tempApartmentIndex = (tempApartmentIndex + 1) % householdProperties.Count();

                    if (thisFloorAreaRatio < legalFloorAreaRatio)
                    {
                        break;
                    }

                    if (tempApartmentIndex == 0)
                    {
                        if (isSomeThingRemoved == false)
                        {
                            break;
                        }
                        else
                        {
                            isSomeThingRemoved = false;
                        }
                    }

                }

            }

            currentFloorAreaRatio = thisFloorAreaRatio;
        }
        private static bool VectorIntersection(Point3d A, Vector3d Avector, Point3d B, Vector3d Bvector, ref Vector3d ATransformVector, ref Vector3d BTransformVector)
        {
            //Returns false If Intersection can't be made

            if (Avector.IsParallelTo(Bvector) != 0)
                return false;

            ATransformVector = (((B.X - A.X) * Bvector.Y - (B.Y - A.Y) * Bvector.X)) / (Avector.X * Bvector.Y - Avector.Y * Bvector.X) * Avector;
            BTransformVector = ((A + ATransformVector).X - B.X) / Bvector.X * Bvector;

            return true;
        }

        public static List<int> RankCoreDIstance(List<CoreProperties> currentCores, List<CoreProperties> otherCores)
        {
            try
            {
                List<double> otherCoresBVectorDistance = new List<double>();

                for (int i = 0; i < otherCores.Count(); i++)
                    otherCoresBVectorDistance.Add(99999999);

                for (int i = 0; i < currentCores.Count(); i++)
                {
                    for (int j = 0; j < otherCores.Count(); j++)
                    {
                        Vector3d tempAtransfromVector = new Vector3d();
                        Vector3d tempBTransformVector = new Vector3d();

                        bool tempVectorIntersection = VectorIntersection(otherCores[j].Origin, otherCores[j].XDirection, currentCores[i].Origin, currentCores[i].YDirection, ref tempAtransfromVector, ref tempBTransformVector);

                        if (tempVectorIntersection && otherCoresBVectorDistance[j] > Math.Abs(tempBTransformVector.Length))
                            otherCoresBVectorDistance[j] = Math.Abs(tempBTransformVector.Length);
                    }
                }

                int usableOtherCoreIndex = otherCoresBVectorDistance.IndexOf(otherCoresBVectorDistance.Min());

                List<double> coreDistance = new List<double>();

                for (int i = 0; i < currentCores.Count(); i++)
                {
                    Vector3d tempAtransfromVector = new Vector3d();
                    Vector3d tempBTransformVector = new Vector3d();

                    bool tempVectorIntersection = VectorIntersection(otherCores[usableOtherCoreIndex].Origin, otherCores[usableOtherCoreIndex].XDirection, currentCores[i].Origin, currentCores[i].YDirection, ref tempAtransfromVector, ref tempBTransformVector);

                    coreDistance.Add(Math.Abs(tempAtransfromVector.Length));
                }

                List<double> CoreDistanceCopy = new List<double>(coreDistance);
                CoreDistanceCopy.Sort();

                int[] outputRank = new int[coreDistance.Count()];

                for (int i = 0; i < coreDistance.Count(); i++)
                {
                    outputRank[i] = CoreDistanceCopy.IndexOf(coreDistance[i]);
                    CoreDistanceCopy[outputRank[i]] = double.MinValue;
                }

                int outputRankMax = outputRank.Max();

                for (int i = 0; i < outputRank.Length; i++)
                {
                    outputRank[i] = outputRankMax - outputRank[i];
                }

                return outputRank.ToList();
            }
            catch (Exception)
            {
                List<int> exceptionOutput = new List<int>();

                for (int i = 0; i < currentCores.Count(); i++)
                    exceptionOutput.Add(i);


                return exceptionOutput;
            }
        }
        public static void reduceFloorAreaRatio(ref List<List<List<HouseholdProperties>>> householdProperties_Original, ref List<List<CoreProperties>> coreProperties_Original, ref double currentFloorAreaRatio, Plot plot, double legalFloorAreaRatio)
        {
            double thisFloorAreaRatio = currentFloorAreaRatio;
            List<List<List<HouseholdProperties>>> householdProperties = new List<List<List<HouseholdProperties>>>(householdProperties_Original);
            List<List<CoreProperties>> coreProperties = new List<List<CoreProperties>>(coreProperties_Original);

            //////// 용적률이 너무 크면 제거

            if (thisFloorAreaRatio > legalFloorAreaRatio)
            {
                int tempApartmentIndex = 0;
                int tempStoriesFromTopIndex = 1;
                bool isSomeThingRemoved = false;

                while (thisFloorAreaRatio > legalFloorAreaRatio)
                {
                    int tempStoryIndex = householdProperties[tempApartmentIndex].Count() - tempStoriesFromTopIndex;

                    double tempExpectedAreaRemove = 0;
                    List<int> tempRemoveHouseholdPropertyIndex = new List<int>();
                    List<int> tempRemoveCorePropertyIndex = new List<int>();

                    if (tempStoryIndex < 0)
                    {
                        tempApartmentIndex = (tempApartmentIndex + 1) % householdProperties.Count();
                        continue;
                    }

                    int nextCorePropertyIndex = 0;

                    for (int i = 0; i < householdProperties[tempApartmentIndex][tempStoryIndex].Count(); i++)
                    {
                        if (thisFloorAreaRatio - tempExpectedAreaRemove / plot.GetArea() * 100 > legalFloorAreaRatio)
                        {
                            tempExpectedAreaRemove += householdProperties[tempApartmentIndex][tempStoryIndex][i].GetExclusiveArea();
                            tempExpectedAreaRemove += householdProperties[tempApartmentIndex][tempStoryIndex][i].GetWallArea();
                            tempRemoveHouseholdPropertyIndex.Add(i);

                            if (Math.Ceiling((double)householdProperties[tempApartmentIndex][tempStoryIndex].Count / (double)coreProperties[tempApartmentIndex].Count * (double)(nextCorePropertyIndex + 1))-1 == i)
                            {
                                tempExpectedAreaRemove += coreProperties[tempApartmentIndex][nextCorePropertyIndex].GetArea();
                                tempRemoveCorePropertyIndex.Add(nextCorePropertyIndex);
                                nextCorePropertyIndex++;
                            }
                        }
                    }

                    if (tempRemoveHouseholdPropertyIndex.Count != 0)
                    {
                        tempRemoveHouseholdPropertyIndex.Sort();
                        tempRemoveHouseholdPropertyIndex.Reverse();
                        tempRemoveCorePropertyIndex.Sort();
                        tempRemoveCorePropertyIndex.Reverse();

                        for (int i = 0; i < tempRemoveHouseholdPropertyIndex.Count(); i++)
                        {
                            householdProperties[tempApartmentIndex][tempStoryIndex].RemoveAt(tempRemoveHouseholdPropertyIndex[i]);
                        }

                        for (int i = 0; i < tempRemoveCorePropertyIndex.Count(); i++)
                        {
                            CoreProperties tempCoreProperties = coreProperties[tempApartmentIndex][tempRemoveCorePropertyIndex[i]];

                            coreProperties[tempApartmentIndex][tempRemoveCorePropertyIndex[i]] = new CoreProperties(tempCoreProperties.Origin, tempCoreProperties.XDirection, tempCoreProperties.YDirection, tempCoreProperties.CoreType, tempCoreProperties.Stories - 1, tempCoreProperties.CoreInterpenetration);

                            if (coreProperties[tempApartmentIndex][tempRemoveCorePropertyIndex[i]].Stories == 0) ////////////////////////////////////////////////////////////
                            {
                                coreProperties[tempApartmentIndex].RemoveAt(tempRemoveCorePropertyIndex[i]);
                                tempExpectedAreaRemove += coreProperties[tempApartmentIndex][nextCorePropertyIndex].GetArea();
                            }
                        }

                        thisFloorAreaRatio -= tempExpectedAreaRemove / plot.GetArea() * 100;
                        isSomeThingRemoved = true;
                    }

                    tempApartmentIndex = (tempApartmentIndex + 1) % householdProperties.Count();

                    if (tempApartmentIndex == 0)
                    {
                        if (isSomeThingRemoved == false)
                            break;

                        tempStoriesFromTopIndex += 1;
                        isSomeThingRemoved = false;
                    }

                }

            }

            householdProperties_Original = householdProperties;
            coreProperties_Original = coreProperties;
            currentFloorAreaRatio = thisFloorAreaRatio;
        }
        public static void sortHHPOnEarth_OnlyCreateCommercial(ref RhinoList<HouseholdProperties> householdPropertiesOnEarth, List<Curve> PlotArr, List<int> roadWidth)
        {
            List<Point3d> hhpOnEarthOrigin = (from hhp in householdPropertiesOnEarth
                                              select hhp.Origin).ToList();

            RhinoList<int> tempRoadWidth = new RhinoList<int>();
            List<double> distanceFromClosestPlot = new List<double>();

            for (int i = 0; i < hhpOnEarthOrigin.Count(); i++)
            {
                List<double> distanceFromPlotArr = new List<double>();

                for (int j = 0; j < PlotArr.Count; j++)
                {
                    double tempOutParameter;
                    PlotArr[j].ClosestPoint(hhpOnEarthOrigin[i], out tempOutParameter);

                    distanceFromPlotArr.Add(PlotArr[j].PointAt(tempOutParameter).DistanceTo(hhpOnEarthOrigin[i]));
                }

                tempRoadWidth.Add(roadWidth[distanceFromPlotArr.IndexOf(distanceFromPlotArr.Min())]);
                distanceFromClosestPlot.Add(distanceFromPlotArr.Min());
            }

            householdPropertiesOnEarth.Sort(distanceFromClosestPlot.ToArray());
            tempRoadWidth.Sort(distanceFromClosestPlot.ToArray());

            householdPropertiesOnEarth.Sort(tempRoadWidth.ToArray());

        }

        public static List<HouseholdProperties> createCommercialFacility(List<List<List<HouseholdProperties>>> householdProperties, List<Curve> apartmentBaseCurves, Plot plot, double buildingCoverageReamin, double grossAreaRatioRemain)
        {
            double grossAreaRemain = plot.GetArea() * grossAreaRatioRemain / 100;
            double buildingAreaRemain = plot.GetArea() * buildingCoverageReamin / 100;

            List<HouseholdProperties> output = new List<HouseholdProperties>();

            RhinoList<HouseholdProperties> hhpOnEarth = new RhinoList<HouseholdProperties>();

            for (int i = 0; i < householdProperties.Count; i++)
            {
                if (householdProperties[i].Count != 0)
                {
                    hhpOnEarth.AddRange(householdProperties[i][0]);
                }
            }

            List<Point3d> hhpOnEarthOrigin = (from hhp in hhpOnEarth
                                              select hhp.Origin).ToList();

            List<Curve> boundaryArr = plot.Boundary.DuplicateSegments().ToList();
            List<int> ClosestBoundaryRoadWidth = new List<int>();

            for (int i = 0; i < hhpOnEarthOrigin.Count; i++)
            {
                List<double> distanceFromPlotArr = new List<double>();

                for (int j = 0; j < boundaryArr.Count(); j++)
                {
                    double tempOutParamter;
                    boundaryArr[j].ClosestPoint(hhpOnEarthOrigin[i], out tempOutParamter);

                    distanceFromPlotArr.Add(boundaryArr[j].PointAt(tempOutParamter).DistanceTo(hhpOnEarthOrigin[i]));
                }

                ClosestBoundaryRoadWidth.Add(plot.Surroundings[distanceFromPlotArr.IndexOf(distanceFromPlotArr.Min())]);
            }

            hhpOnEarth.Sort(ClosestBoundaryRoadWidth.ToArray());
            hhpOnEarth.Reverse();

            for (int i = 0; i < hhpOnEarth.Count(); i++)
            {
                if (grossAreaRemain >= 0 && hhpOnEarth[i] != null)
                {
                    if (grossAreaRemain - hhpOnEarth[i].GetArea() >= 0)
                    {
                        HouseholdProperties tempHouseholdProperties = new HouseholdProperties(hhpOnEarth[i]);

                        tempHouseholdProperties.Origin = new Point3d(tempHouseholdProperties.Origin.X, tempHouseholdProperties.Origin.Y, 0);

                        double LastHouseholdProperteis = tempHouseholdProperties.GetArea();

                        if (tempHouseholdProperties.YLengthB != 0)
                        {
                            tempHouseholdProperties.XLengthA = tempHouseholdProperties.XLengthA - tempHouseholdProperties.XLengthB;
                            tempHouseholdProperties.XLengthB = 0;
                        }

                        output.Add(tempHouseholdProperties);
                        grossAreaRemain -= tempHouseholdProperties.GetArea();
                    }
                    else
                    {
                        break;
                    }

                }
                else
                {
                    break;
                }
            }

            return output;
        }



        public static string GetPlotBoundaryVector(List<Point3d> plotBoundary)
        {
            string output = "";

            for(int i = 0; i < plotBoundary.Count(); i++)
            {
                if (i != 0)
                    output += "/";

                output += plotBoundary[i].X.ToString() + "," + plotBoundary[i].Y.ToString() + "," + plotBoundary[i].Z.ToString();
            }

            return output;
        }

        public static int GetLastPreDesignNo(List<string> idColumnName, List<string> idColumnCode)
        {
            const string connectionString = "Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=oracle3.ejudata.co.kr)(PORT=1521))(CONNECT_DATA=(SERVER=DEDICATED)(SID = oracle3)));User Id=whruser_sh;Password=ejudata;";

            List<int> PRE_DESIGN_NO_INDEX = new List<int>();

            string readSql = "select * FROM TD_DESIGN_DETAIL";

            if (idColumnName.Count() != 0)
                readSql += " WHERE";

            for (int i = 0; i < idColumnName.Count(); i++)
            {
                if (i != 0)
                    readSql += " AND";

                readSql += " " + idColumnName[i] + "=" + idColumnCode[i] + " ";
            }

            using (OracleConnection connection = new OracleConnection(connectionString))
            {
                try
                {
                    connection.Open();

                    OracleCommand readComm = new OracleCommand(readSql, connection);
                    OracleDataReader reader = readComm.ExecuteReader();

                    while(reader.Read())
                    {
                        PRE_DESIGN_NO_INDEX.Add(int.Parse(reader["REGI_PRE_DESIGN_NO"].ToString()));
                    }
                }
                catch (OracleException ex)
                {
                    MessageBox.Show(ex.ToString());
                }
            }

            PRE_DESIGN_NO_INDEX.Sort();

            if (PRE_DESIGN_NO_INDEX.Count() == 0)
                return -1;

            return PRE_DESIGN_NO_INDEX[PRE_DESIGN_NO_INDEX.Count() - 1];
        }

        public static string GetProjectNameFromServer(List<string> idColumnName, List<string> idColumnCode)
        {
            string output = "";

            string readSql = "select * FROM TN_REGI_MASTER";

            if (idColumnName.Count() != 0)
                readSql += " WHERE";

            for (int i = 0; i < idColumnName.Count(); i++)
            {
                if (i != 0)
                    readSql += " AND";

                readSql += " " + idColumnName[i] + "=" + idColumnCode[i] + " ";
            }

            using (OracleConnection connection = new Oracle.ManagedDataAccess.Client.OracleConnection(Consts.connectionString))
            {
                try
                {
                    connection.Open();

                    OracleCommand readComm = new OracleCommand(readSql, connection);
                    OracleDataReader reader = readComm.ExecuteReader();

                    while (reader.Read())
                    {
                        output = reader["REGI_BIZNS_NM"].ToString();
                    }
                }
                catch (OracleException ex)
                {
                    MessageBox.Show(ex.ToString());
                }
            }

            return output;
        }

        public static string GetPlotTypeFromServer(List<string> idColumnName, List<string> idColumnCode)
        {
            string output = "";

            string readSql = "select * FROM TN_PREV_ASSET";

            if (idColumnName.Count() != 0)
                readSql += " WHERE";

            for (int i = 0; i < idColumnName.Count(); i++)
            {
                if (i != 0)
                    readSql += " AND";

                readSql += " " + idColumnName[i] + "=" + idColumnCode[i] + " ";
            }

            using (OracleConnection connection = new OracleConnection(Consts.connectionString))
            {
                try
                {
                    connection.Open();

                    OracleCommand readComm = new OracleCommand(readSql, connection);
                    OracleDataReader reader = readComm.ExecuteReader();

                    while (reader.Read())
                    {
                        output = reader["USE_REGION_CD"].ToString();
                    }
                }
                catch (OracleException ex)
                {
                    MessageBox.Show(ex.ToString());
                }
            }

            return output;
        }

        public static double GetManualAreaFromServer(List<string> idColumnName, List<string> idColumnCode)
        {
            List<string> areaList = new List<string>();

            string readSql = "select * FROM TN_PREV_ASSET";

            if (idColumnName.Count() != 0)
                readSql += " WHERE";

            for (int i = 0; i < idColumnName.Count(); i++)
            {
                if (i != 0)
                    readSql += " AND";

                readSql += " " + idColumnName[i] + "=" + idColumnCode[i] + " ";
            }

            using (OracleConnection connection = new OracleConnection(Consts.connectionString))
            {
                try
                {
                    connection.Open();

                    OracleCommand readComm = new OracleCommand(readSql, connection);
                    OracleDataReader reader = readComm.ExecuteReader();

                    while(reader.Read())
                    {
                        areaList.Add(reader["LAND_AREA"].ToString());
                    }
                }
                catch (OracleException ex)
                {
                    MessageBox.Show(ex.ToString());
                }
            }

            double areaMassSum = 0;

            foreach(string i in areaList)
            {
                areaMassSum += double.Parse(i);
            }

            return areaMassSum;
        }

        public static string GetAddressFromServer(List<string> idColumnName, List<string> idColumnCode)
        {
            List<string> addressList = new List<string>();

            string readSql = "select * FROM TN_PREV_ASSET";

            if (idColumnName.Count() != 0)
                readSql += " WHERE";

            for (int i = 0; i < idColumnName.Count(); i++)
            {
                if (i != 0)
                    readSql += " AND";

                readSql += " " + idColumnName[i] + "=" + idColumnCode[i] + " ";
            }

            bool IsFirst = true;

            using (OracleConnection connection = new OracleConnection(Consts.connectionString))
            {
                try
                {
                    connection.Open();

                    OracleCommand readComm = new OracleCommand(readSql, connection);
                    OracleDataReader reader = readComm.ExecuteReader();

                    while (reader.Read() && IsFirst)
                    {

                        string tempLandCode = reader["LAND_CD"].ToString();
                        string tempReadSql = "select * FROM TN_LAW_LAND WHERE LAND_CD=" + tempLandCode;

                        using (OracleConnection tempConnection = new OracleConnection(Consts.connectionString))
                        {
                            tempConnection.Open();

                            OracleCommand tempCommand = new OracleCommand(tempReadSql, tempConnection);
                            OracleDataReader tempReader = tempCommand.ExecuteReader();

                            while (tempReader.Read())
                            {
                                addressList.Add(tempReader["LAND_SIDO_NM"].ToString());
                                addressList.Add(tempReader["LAND_SIGUNGU_NM"].ToString());
                                addressList.Add(tempReader["LAND_DONG_NM"].ToString());

                                if (int.Parse(tempReader["DEPTH_LV"].ToString()) == 4)
                                    addressList.Add(tempReader["LAND_RI_NM"].ToString());
                            }
                        }

                        addressList.Add(reader["BNBUN"].ToString() + "-" + reader["BUBUN"].ToString());

                        IsFirst = false;
                    }
                }
                catch (OracleException ex)
                {
                    MessageBox.Show(ex.ToString());
                }
            }

            string address = "";

            foreach (string i in addressList)
            {
                address += i;
                address += " ";
            }

            if (IsFirst == false)
            {
                address += " 일원";
            }

            return address;
        }
        public static bool checkDesignMasterPresence(List<string> idColumnName, List<string> idColumnCode)
        {
            string readSql = "select * FROM TD_DESIGN_MASTER";

            if (idColumnName.Count() != 0)
                readSql += " WHERE";

            for (int i = 0; i < idColumnName.Count(); i++)
            {
                if (i != 0)
                    readSql += " AND";

                readSql += " " + idColumnName[i] + "=" + idColumnCode[i] + " ";
            }

            using (OracleConnection connection = new OracleConnection(Consts.connectionString))
            {
                try
                {
                    connection.Open();

                    OracleCommand tempCommand = new OracleCommand(readSql, connection);
                    OracleDataReader tempReader = tempCommand.ExecuteReader();

                    bool output = tempReader.Read();

                    tempCommand.Dispose();

                    return output;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());

                    return false;
                }

            }
        }
        public static void AddTdDesignMaster(List<string> idColumnName, List<string> idColumnCode, string userID, int GROSS_AREA_RATIO_REG, 
            int BUILDING_COVERAGE_REG, int STOREIS_REG, string PLOT_BOUNDARY_VECTOR, string PLOT_SURROUNDINGS_VECTOR, double PLOTAREA_PLAN)
        {
            //20160516 확인 및 수정완료, 추후 건축선 후퇴 구현 후 plotarea_Excluded에 입력


            string sql = "INSERT INTO TD_DESIGN_MASTER(REGI_MST_NO,REGI_SUB_MST_NO,PROJECT_NAME,PLOT_ADDRESS,PLOT_TYPE_CD,PLOT_BOUNDARY_VECTOR,PLOT_SURROUNDINGS_VECTOR,PLOTAREA_MANUAL,PLOTAREA_EXCLUDED,PLOTAREA_PLAN,GROSS_AREA_RATIO_REG,BUILDING_COVERAGE_REG,STOREIS_REG,FRST_REGIST_DT,FRST_REGISTER_ID) VALUES(:p_REGI_MST_NO,:p_REGI_SUB_MST_NO,:p_PROJECT_NAME,:p_PLOT_ADDRESS,:p_PLOT_TYPE_CD,:p_PLOT_BOUNDARY_VECTOR,:p_PLOT_SURROUNDINGS_VECTOR,:p_PLOTAREA_MANUAL,:p_PLOTAREA_EXCLUDED,:p_PLOTAREA_PLAN,:p_GROSS_AREA_RATIO_REG,:p_BUILDING_COVERAGE_REG,:p_STOREIS_REG,SYSDATE,:p_FRST_REGISTER_ID)";

            using (OracleConnection connection = new OracleConnection(Consts.connectionString))
            {
                try
                {
                    connection.Open();

                    OracleCommand comm = new OracleCommand(sql, connection);

                    OracleParameter p_REGI_MST_NO = new OracleParameter();
                    OracleParameter p_REGI_SUB_MST_NO = new OracleParameter();
                    OracleParameter p_PROJECT_NAME = new OracleParameter();
                    OracleParameter p_PLOT_ADDRESS = new OracleParameter();
                    OracleParameter p_PLOT_TYPE_CD = new OracleParameter();
                    OracleParameter p_PLOT_BOUNDARY_VECTOR = new OracleParameter();
                    OracleParameter p_PLOT_SURROUNDINGS_VECTOR = new OracleParameter();
                    OracleParameter p_PLOTAREA_MANUAL = new OracleParameter();
                    OracleParameter p_PLOTAREA_EXCLUDED = new OracleParameter();
                    OracleParameter p_PLOTAREA_PLAN = new OracleParameter();
                    OracleParameter p_GROSS_AREA_RATIO_REG = new OracleParameter();
                    OracleParameter p_BUILDING_COVERAGE_REG = new OracleParameter();
                    OracleParameter p_STOREIS_REG = new OracleParameter();
                    OracleParameter p_FRST_REGIST_DT = new OracleParameter();
                    OracleParameter p_FRST_REGISTER_ID = new OracleParameter();

                    ///추가사항
                    ///
                  



                    p_REGI_MST_NO.DbType = System.Data.DbType.String;
                    p_REGI_MST_NO.Value = idColumnCode[0].ToString();
                    p_REGI_MST_NO.ParameterName = "p_REGI_MST_NO";

                    p_REGI_SUB_MST_NO.DbType = System.Data.DbType.String;
                    p_REGI_SUB_MST_NO.Value = idColumnCode[1].ToString();
                    p_REGI_SUB_MST_NO.ParameterName = "p_REGI_SUB_MST_NO";

                    p_PROJECT_NAME.DbType = System.Data.DbType.String; 
                    p_PROJECT_NAME.Value = GetProjectNameFromServer(idColumnName, idColumnCode);
                    p_PROJECT_NAME.ParameterName = "p_PROJECT_NAME";

                    p_PLOT_ADDRESS.DbType = System.Data.DbType.String;
                    p_PLOT_ADDRESS.Value = GetAddressFromServer(idColumnName, idColumnCode);
                    p_PLOT_ADDRESS.ParameterName = "p_PLOT_ADDRESS";

                    p_PLOT_TYPE_CD.DbType = System.Data.DbType.String;
                    p_PLOT_TYPE_CD.Value = GetPlotTypeFromServer(idColumnName, idColumnCode);
                    p_PLOT_TYPE_CD.ParameterName = "p_PLOT_TYPE_CD";

                    p_PLOT_BOUNDARY_VECTOR.DbType = System.Data.DbType.String;
                    p_PLOT_BOUNDARY_VECTOR.Value = PLOT_BOUNDARY_VECTOR;
                    p_PLOT_BOUNDARY_VECTOR.ParameterName = "p_PLOT_BOUNDARY_VECTOR";

                    p_PLOT_SURROUNDINGS_VECTOR.DbType = System.Data.DbType.String;
                    p_PLOT_SURROUNDINGS_VECTOR.Value = PLOT_SURROUNDINGS_VECTOR;
                    p_PLOT_SURROUNDINGS_VECTOR.ParameterName = "p_PLOT_SURROUNDINGS_VECTOR";

                    p_PLOTAREA_MANUAL.DbType = System.Data.DbType.Decimal;
                    p_PLOTAREA_MANUAL.Value = Math.Round(GetManualAreaFromServer(idColumnName, idColumnCode), 2).ToString();
                    p_PLOTAREA_MANUAL.ParameterName = "p_PLOTAREA_MANUAL";

                    p_PLOTAREA_EXCLUDED.DbType = System.Data.DbType.Decimal;          /////////////////////////////////////////////////////////////
                    p_PLOTAREA_EXCLUDED.Value = "0";
                    p_PLOTAREA_EXCLUDED.ParameterName = "p_PLOTAREA_EXCLUDED";

                    p_PLOTAREA_PLAN.DbType = System.Data.DbType.Decimal;
                    p_PLOTAREA_PLAN.Value = Math.Round(PLOTAREA_PLAN / 1000000, 2).ToString() ;
                    p_PLOTAREA_PLAN.ParameterName = "p_PLOTAREA_PLAN";

                    p_GROSS_AREA_RATIO_REG.DbType = System.Data.DbType.Decimal;
                    p_GROSS_AREA_RATIO_REG.Value = GROSS_AREA_RATIO_REG.ToString();
                    p_GROSS_AREA_RATIO_REG.ParameterName = "p_GROSS_AREA_RATIO_REG";

                    p_BUILDING_COVERAGE_REG.DbType = System.Data.DbType.Decimal;
                    p_BUILDING_COVERAGE_REG.Value = BUILDING_COVERAGE_REG.ToString();
                    p_BUILDING_COVERAGE_REG.ParameterName = "p_BUILDING_COVERAGE_REG";

                    p_STOREIS_REG.DbType = System.Data.DbType.Decimal;
                    p_STOREIS_REG.Value = STOREIS_REG.ToString();
                    p_STOREIS_REG.ParameterName = "p_STOREIS_REG";

                    p_FRST_REGISTER_ID.DbType = System.Data.DbType.String;
                    p_FRST_REGISTER_ID.Value = userID;
                    p_FRST_REGISTER_ID.ParameterName = "p_FRST_REGISTER_ID";

                    


                    comm.Parameters.Add(p_REGI_MST_NO);
                    comm.Parameters.Add(p_REGI_SUB_MST_NO);

                    comm.Parameters.Add(p_PROJECT_NAME);
                    comm.Parameters.Add(p_PLOT_ADDRESS);
                    
                    comm.Parameters.Add(p_PLOT_TYPE_CD);
                    comm.Parameters.Add(p_PLOT_BOUNDARY_VECTOR);
                    comm.Parameters.Add(p_PLOT_SURROUNDINGS_VECTOR);
                    comm.Parameters.Add(p_PLOTAREA_MANUAL);
                    comm.Parameters.Add(p_PLOTAREA_EXCLUDED);
                    comm.Parameters.Add(p_PLOTAREA_PLAN);
                    comm.Parameters.Add(p_GROSS_AREA_RATIO_REG);
                    comm.Parameters.Add(p_BUILDING_COVERAGE_REG);
                    comm.Parameters.Add(p_STOREIS_REG);
                    
                    comm.Parameters.Add(p_FRST_REGISTER_ID);

                    comm.ExecuteNonQuery();

                    comm.Dispose();
                }
                catch (OracleException ex)
                {
                    MessageBox.Show(ex.ToString());
                }
            }
        }
        /// <summary>
        /// //////////dmdfskdljflskdjfdgsdasfdasfdaolfjdlfjkdlsfdajsfkdl
        /// </summary>
        /// <param name="idColumnName"></param>
        /// <param name="idColumnCode"></param>
        /// <param name="path"></param>
        /// 
        private static Oracle.ManagedDataAccess.Types.OracleBlob GetBlobDataFromFile(string path, OracleConnection conn)
        {
            Oracle.ManagedDataAccess.Types.OracleBlob blob = new Oracle.ManagedDataAccess.Types.OracleBlob(conn);

            System.IO.FileInfo fi = new System.IO.FileInfo(path);
            string filename = fi.Name;
            int filesize = (int)fi.Length;
            byte[] file = new byte[fi.Length - 1];
            System.IO.FileStream fs = new System.IO.FileStream(path, System.IO.FileMode.Open, System.IO.FileAccess.Read);
            System.IO.BinaryReader br = new System.IO.BinaryReader(fs);
            int bytes;

            try
            {
                while ((bytes = br.Read(file, 0, file.Length)) > 0)
                {
                    blob.Write(file, 0, bytes);

                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally { br.Close(); fs.Close(); }

            return blob;
        }


        /// <summary>
        /// 최초 등록
        /// </summary>
        /// <param name="idColumnName"></param>
        /// <param name="idColumnCode"></param>
        /// <param name="path"></param>
        public static void AddTdDesignReport(List<string> idColumnName, List<string> idColumnCode,Dictionary<string,string> path , int temp_REGI_PRE_DESIGN_NO)
        {

            string REPORT_path = "";
            //string ELEVATION_path = "";
            string SECTION_path = "";
            //string DWG_PLANS_path = "";
            string GROUND_PLAN_path = "";
            string TYPICAL_PLAN_path = "";
            string BIRDEYE1_path = "";
            string BIRDEYE2_path = "";

            path.TryGetValue("REPORT", out REPORT_path);
            //path.TryGetValue("ELEVATION", out ELEVATION_path);
            path.TryGetValue("SECTION", out SECTION_path);
            //path.TryGetValue("DWG_PLANS", out DWG_PLANS_path);
            path.TryGetValue("GROUND_PLAN", out GROUND_PLAN_path);
            path.TryGetValue("TYPICAL_PLAN", out TYPICAL_PLAN_path);
            path.TryGetValue("BIRDEYE1", out BIRDEYE1_path);
            path.TryGetValue("BIRDEYE2", out BIRDEYE2_path);

            //MessageBox.Show("report path = " + REPORT_path); //+ Environment.NewLine + "ELEVATION path = " + ELEVATION_path + Environment.NewLine + "SECTION path = " + SECTION_path + Environment.NewLine
            //    + "DWG_PLANS path = " + DWG_PLANS_path + Environment.NewLine + "GROUND_PLAN path = " + GROUND_PLAN_path + Environment.NewLine + "TYPICAL_PLAN path = " + TYPICAL_PLAN_path + Environment.NewLine +
            //    "BIRDEYE1 path = " + BIRDEYE1_path + Environment.NewLine + "BIRDEYE2 path = " + BIRDEYE2_path );



            //string sql = "INSERT INTO TD_DESIGN_REPORT("+
            //    "REGI_MST_NO,REGI_SUB_MST_NO,REGI_PRE_DESIGN_NO,PDF_REPORT,PDF_REPORT_SIZE,IMG_BIRD_EYE_1,IMG_BIRD_EYE_1_SIZE,IMG_BIRD_EYE_2,IMG_BIRD_EYE_2_SIZE,IMG_GROUND_FLOOR_PLAN,IMG_GROUND_FLOOR_PLAN_SIZE,"+
            //    "IMG_TYPICAL_FLOOR_PLAN,IMG_TYPICAL_FLOOR_PLAN_SIZE,IMG_ELEVATION,IMG_ELEVATION_SIZE,IMG_SECTION,IMG_SECTION_SIZE,DWG_PLANS,DWG_PLANS_SIZE,FRST_REGIST_DT,FRST_REGISTER_ID)"
            //    + "VALUES("+
            //    ":p_REGI_MST_NO,:p_REGI_SUB_MST_NO,:p_REGI_PRE_DESIGN_NO,:p_PDF_REPORT,:p_PDF_REPORT_SIZE,:p_IMG_BIRD_EYE_1,:p_IMG_BIRD_EYE_1_SIZE,:p_IMG_BIRD_EYE_2,:p_IMG_BIRD_EYE_2_SIZE,:p_IMG_GROUND_FLOOR_PLAN,:p_IMG_GROUND_FLOOR_PLAN_SIZE,"+
            //    ":p_IMG_TYPICAL_FLOOR_PLAN,:p_IMG_TYPICAL_FLOOR_PLAN_SIZE,:p_IMG_ELEVATION,:p_IMG_ELEVATION_SIZE,:p_IMG_SECTION,:p_IMG_SECTION_SIZE,:p_DWG_PLANS,:p_DWG_PLANS_SIZE,SYSDATE,:p_FRST_REGISTER_ID)";



            string sql = "INSERT INTO TD_DESIGN_REPORT (REGI_MST_NO,REGI_SUB_MST_NO,REGI_PRE_DESIGN_NO,PDF_REPORT,PDF_REPORT_SIZE,IMG_BIRD_EYE_1,IMG_BIRD_EYE_1_SIZE,IMG_BIRD_EYE_2,IMG_BIRD_EYE_2_SIZE,IMG_GROUND_FLOOR_PLAN,IMG_GROUND_FLOOR_PLAN_SIZE,IMG_TYPICAL_FLOOR_PLAN,IMG_TYPICAL_FLOOR_PLAN_SIZE,IMG_SECTION,IMG_SECTION_SIZE,FRST_REGIST_DT,FRST_REGISTER_ID)"
                + "VALUES(:p_REGI_MST_NO,:p_REGI_SUB_MST_NO,:p_REGI_PRE_DESIGN_NO,:p_PDF_REPORT,:p_PDF_REPORT_SIZE,:p_IMG_BIRD_EYE_1,:p_IMG_BIRD_EYE_1_SIZE,:p_IMG_BIRD_EYE_2,:p_IMG_BIRD_EYE_2_SIZE,:p_IMG_GROUND_FLOOR_PLAN,:p_IMG_GROUND_FLOOR_PLAN_SIZE,:p_IMG_TYPICAL_FLOOR_PLAN,:p_IMG_TYPICAL_FLOOR_PLAN_SIZE,:p_IMG_SECTION,:p_IMG_SECTION_SIZE,SYSDATE,:p_FRST_REGISTER_ID)";


            using (OracleConnection connection = new OracleConnection(Consts.connectionString))
            {
                try
                {
                    connection.Open();


                    Oracle.ManagedDataAccess.Types.OracleBlob REPORT_blob = GetBlobDataFromFile(REPORT_path, connection);
                    //Oracle.ManagedDataAccess.Types.OracleBlob ELEVATION_blob = GetBlobDataFromFile(ELEVATION_path, connection);
                    Oracle.ManagedDataAccess.Types.OracleBlob SECTION_blob = GetBlobDataFromFile(SECTION_path, connection);
                    //Oracle.ManagedDataAccess.Types.OracleBlob DWG_PLANS_blob = GetBlobDataFromFile(DWG_PLANS_path, connection);
                    Oracle.ManagedDataAccess.Types.OracleBlob GROUND_PLAN = GetBlobDataFromFile(GROUND_PLAN_path, connection);
                    Oracle.ManagedDataAccess.Types.OracleBlob TYPICAL_PLAN = GetBlobDataFromFile(TYPICAL_PLAN_path, connection);
                    Oracle.ManagedDataAccess.Types.OracleBlob BIRDEYE1 = GetBlobDataFromFile(BIRDEYE1_path, connection);
                    Oracle.ManagedDataAccess.Types.OracleBlob BIRDEYE2 = GetBlobDataFromFile(BIRDEYE2_path, connection);


                    OracleCommand comm = new OracleCommand(sql, connection);

                    OracleParameter p_REGI_MST_NO = new OracleParameter();
                    OracleParameter p_REGI_SUB_MST_NO = new OracleParameter();
                    OracleParameter p_REGI_PRE_DESIGN_NO = new OracleParameter();
                    OracleParameter p_PDF_REPORT = new OracleParameter();
                    OracleParameter p_PDF_REPORT_SIZE = new OracleParameter();
                    OracleParameter p_IMG_BIRD_EYE_1 = new OracleParameter();
                    OracleParameter p_IMG_BIRD_EYE_1_SIZE = new OracleParameter();
                    OracleParameter p_IMG_BIRD_EYE_2 = new OracleParameter();
                    OracleParameter p_IMG_BIRD_EYE_2_SIZE = new OracleParameter();
                    OracleParameter p_IMG_GROUND_FLOOR_PLAN = new OracleParameter();
                    OracleParameter p_IMG_GROUND_FLOOR_PLAN_SIZE = new OracleParameter();
                    OracleParameter p_IMG_TYPICAL_FLOOR_PLAN = new OracleParameter();
                    OracleParameter p_IMG_TYPICAL_FLOOR_PLAN_SIZE = new OracleParameter();
                    //OracleParameter p_IMG_ELEVATION = new OracleParameter();
                    //OracleParameter p_IMG_ELEVATION_SIZE = new OracleParameter();
                    OracleParameter p_IMG_SECTION = new OracleParameter();
                    OracleParameter p_IMG_SECTION_SIZE = new OracleParameter();
                    //OracleParameter p_DWG_PLANS = new OracleParameter();
                    //OracleParameter p_DWG_PLANS_SIZE = new OracleParameter();
                    OracleParameter p_FRST_REGISTER_ID = new OracleParameter();


                    p_REGI_MST_NO.OracleDbType = OracleDbType.Char;
                    p_REGI_MST_NO.Value = idColumnCode[0];
                    p_REGI_MST_NO.ParameterName = "p_REGI_MST_NO";

                    p_REGI_SUB_MST_NO.OracleDbType = OracleDbType.Char;
                    p_REGI_SUB_MST_NO.Value = idColumnCode[1];
                    p_REGI_SUB_MST_NO.ParameterName = "p_REGI_SUB_MST_NO";

                    p_REGI_PRE_DESIGN_NO.OracleDbType = OracleDbType.Decimal;
                    p_REGI_PRE_DESIGN_NO.Value = temp_REGI_PRE_DESIGN_NO;
                    p_REGI_PRE_DESIGN_NO.ParameterName = "p_REGI_PRE_DESIGN_NO";

                    p_PDF_REPORT.OracleDbType = OracleDbType.Blob;
                    p_PDF_REPORT.Value = REPORT_blob;
                    p_PDF_REPORT.ParameterName = "p_PDF_REPORT";

                    p_PDF_REPORT_SIZE.OracleDbType = OracleDbType.Decimal;
                    p_PDF_REPORT_SIZE.Value = REPORT_blob.Length;
                    p_PDF_REPORT_SIZE.ParameterName = "p_PDF_REPORT_SIZE";

                    p_IMG_BIRD_EYE_1.OracleDbType = OracleDbType.Blob;
                    p_IMG_BIRD_EYE_1.Value = BIRDEYE1;
                    p_IMG_BIRD_EYE_1.ParameterName = "p_IMG_BIRD_EYE_1";

                    p_IMG_BIRD_EYE_1_SIZE.OracleDbType = OracleDbType.Decimal;
                    p_IMG_BIRD_EYE_1_SIZE.Value = BIRDEYE1.Length;
                    p_IMG_BIRD_EYE_1_SIZE.ParameterName = "p_IMG_BIRD_EYE_1_SIZE";

                    p_IMG_BIRD_EYE_2.OracleDbType = OracleDbType.Blob;
                    p_IMG_BIRD_EYE_2.Value = BIRDEYE2;
                    p_IMG_BIRD_EYE_2.ParameterName = "p_IMG_BIRD_EYE_2";

                    p_IMG_BIRD_EYE_2_SIZE.OracleDbType = OracleDbType.Decimal;
                    p_IMG_BIRD_EYE_2_SIZE.Value = BIRDEYE2.Length;
                    p_IMG_BIRD_EYE_2_SIZE.ParameterName = "p_IMG_BIRD_EYE_2_SIZE";

                    p_IMG_GROUND_FLOOR_PLAN.OracleDbType = OracleDbType.Blob;
                    p_IMG_GROUND_FLOOR_PLAN.Value = GROUND_PLAN;
                    p_IMG_GROUND_FLOOR_PLAN.ParameterName = "p_IMG_GROUND_FLOOR_PLAN";

                    p_IMG_GROUND_FLOOR_PLAN_SIZE.OracleDbType = OracleDbType.Decimal;
                    p_IMG_GROUND_FLOOR_PLAN_SIZE.Value = GROUND_PLAN.Length;
                    p_IMG_GROUND_FLOOR_PLAN_SIZE.ParameterName = "p_IMG_GROUND_FLOOR_PLAN_SIZE";

                    p_IMG_TYPICAL_FLOOR_PLAN.OracleDbType = OracleDbType.Blob;
                    p_IMG_TYPICAL_FLOOR_PLAN.Value = TYPICAL_PLAN;
                    p_IMG_TYPICAL_FLOOR_PLAN.ParameterName = "p_IMG_TYPICAL_FLOOR_PLAN";

                    p_IMG_TYPICAL_FLOOR_PLAN_SIZE.OracleDbType = OracleDbType.Decimal;
                    p_IMG_TYPICAL_FLOOR_PLAN_SIZE.Value = TYPICAL_PLAN.Length;
                    p_IMG_TYPICAL_FLOOR_PLAN_SIZE.ParameterName = "p_IMG_TYPICAL_FLOOR_PLAN_SIZE";

                    //p_IMG_ELEVATION.OracleDbType = OracleDbType.Blob;
                    //p_IMG_ELEVATION.Value = ELEVATION_blob;  ///?
                    //p_IMG_ELEVATION.ParameterName = "p_IMG_ELEVATION";

                    //p_IMG_ELEVATION_SIZE.OracleDbType = OracleDbType.Decimal;
                    //p_IMG_ELEVATION_SIZE.Value = ELEVATION_blob.Length;
                    //p_IMG_ELEVATION_SIZE.ParameterName = "p_IMG_ELEVATION_SIZE";

                    p_IMG_SECTION.OracleDbType = OracleDbType.Blob;
                    p_IMG_SECTION.Value = SECTION_blob;  ///?
                    p_IMG_SECTION.ParameterName = "p_IMG_SECTION";

                    p_IMG_SECTION_SIZE.OracleDbType = OracleDbType.Decimal;
                    p_IMG_SECTION_SIZE.Value = SECTION_blob.Length;
                    p_IMG_SECTION_SIZE.ParameterName = "p_IMG_SECTION_SIZE";

                    //p_DWG_PLANS.OracleDbType = OracleDbType.Blob;
                    //p_DWG_PLANS.Value = DWG_PLANS_blob;  ///?
                    //p_DWG_PLANS.ParameterName = "p_DWG_PLANS";

                    //p_DWG_PLANS_SIZE.OracleDbType = OracleDbType.Decimal;
                    //p_DWG_PLANS_SIZE.Value = DWG_PLANS_blob.Length;
                    //p_DWG_PLANS_SIZE.ParameterName = "p_DWG_PLANS_SIZE";



                    p_FRST_REGISTER_ID.OracleDbType = OracleDbType.Varchar2;
                    p_FRST_REGISTER_ID.Value = getStringFromRegistry("USERID");//?
                    p_FRST_REGISTER_ID.ParameterName = "p_FRST_REGISTER_ID";


                    //얘는 추후 수정 코드에.
                    //p_LAST_UPDUSR_ID.OracleDbType = OracleDbType.Varchar2;
                    //p_LAST_UPDUSR_ID.Value = REPORT_blob;  ///?
                    //p_LAST_UPDUSR_ID.ParameterName = "p_LAST_UPDUSR_ID";

                    comm.Parameters.Add(p_REGI_MST_NO);
                    comm.Parameters.Add(p_REGI_SUB_MST_NO);
                    comm.Parameters.Add(p_REGI_PRE_DESIGN_NO);
                    comm.Parameters.Add(p_PDF_REPORT);
                    comm.Parameters.Add(p_PDF_REPORT_SIZE);
                    comm.Parameters.Add(p_IMG_BIRD_EYE_1);
                    comm.Parameters.Add(p_IMG_BIRD_EYE_1_SIZE);
                    comm.Parameters.Add(p_IMG_BIRD_EYE_2);
                    comm.Parameters.Add(p_IMG_BIRD_EYE_2_SIZE);
                    comm.Parameters.Add(p_IMG_GROUND_FLOOR_PLAN);
                    comm.Parameters.Add(p_IMG_GROUND_FLOOR_PLAN_SIZE);
                    comm.Parameters.Add(p_IMG_TYPICAL_FLOOR_PLAN);
                    comm.Parameters.Add(p_IMG_TYPICAL_FLOOR_PLAN_SIZE);
                    //comm.Parameters.Add(p_IMG_ELEVATION);
                    //comm.Parameters.Add(p_IMG_ELEVATION_SIZE);
                    comm.Parameters.Add(p_IMG_SECTION);
                    comm.Parameters.Add(p_IMG_SECTION_SIZE);
                    //comm.Parameters.Add(p_DWG_PLANS);
                    //comm.Parameters.Add(p_DWG_PLANS_SIZE);
                    comm.Parameters.Add(p_FRST_REGISTER_ID);
                    

                    int cntResult = comm.ExecuteNonQuery();
                    if (cntResult > 0)
                        Rhino.RhinoApp.WriteLine("설계보고서 업로드 완료");
                    else
                    {
                        MessageBox.Show(cntResult.ToString());

                    }



                }
                catch (OracleException ex)
                { Rhino.RhinoApp.WriteLine("설계보고서 업로드 실패");
                    MessageBox.Show(ex.ToString());
                }
            }
        }

        public static int GetLastPlanTypeNo(List<string> idColumnName, List<string> idColumnCode, int REGI_PRE_DESIGN_NO)
        {
            List<string> idColumnNameCopy = new List<string>(idColumnName);
            List<string> idColumnCodeCopy = new List<string>(idColumnCode);

            idColumnCodeCopy.Add("REGI_PRE_DESIGN_NO");
            idColumnCodeCopy.Add(REGI_PRE_DESIGN_NO.ToString());

            const string connectionString = "Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=oracle3.ejudata.co.kr)(PORT=1521))(CONNECT_DATA=(SERVER=DEDICATED)(SID = oracle3)));User Id=whruser_sh;Password=ejudata;";

            List<int> PLAN_TYPE_NO_INDEX = new List<int>();

            string readSql = "select * FROM TD_DESIGN_AREA";

            if (idColumnName.Count() != 0)
                readSql += " WHERE";

            for (int i = 0; i < idColumnNameCopy.Count(); i++)
            {
                if (i != 0)
                    readSql += " AND";

                readSql += " " + idColumnNameCopy[i] + "=" + idColumnCodeCopy[i] + " ";
            }

            using (OracleConnection connection = new OracleConnection(connectionString))
            {
                try
                {
                    connection.Open();

                    OracleCommand readComm = new OracleCommand(readSql, connection);
                    OracleDataReader reader = readComm.ExecuteReader();

                    while (reader.Read())
                    {
                        PLAN_TYPE_NO_INDEX.Add(int.Parse(reader["REGI_PLAN_TYPE_NO"].ToString()));
                    }
                }
                catch (OracleException ex)
                {
                    MessageBox.Show(ex.ToString());
                }
            }

            PLAN_TYPE_NO_INDEX.Sort();

            if (PLAN_TYPE_NO_INDEX.Count() != 0)
                return PLAN_TYPE_NO_INDEX[PLAN_TYPE_NO_INDEX.Count() - 1];
            else
                return -1;
        }

        public static string getDirection(Vector3d yDirection)
        {
            Vector3d yDirectionCopy = new Vector3d(yDirection);
            yDirectionCopy.Reverse();

            string[] directionString = { "EE", "NE", "NN", "NW", "WW", "SW", "SS", "SE" };

            double angle = CommonFunc.vectorAngle(yDirectionCopy);

            return directionString[(int)(((angle + Math.PI / 8) - (angle + Math.PI / 8) % (Math.PI / 4)) / (Math.PI / 4))];
        }

        public static void AddTdDesignArea(List<string> idColumnName, List<string> idColumnCode, int REGI_PRE_DESIGN_NO, string userID, string typeString, double CORE_AREA,double WELFARE_AREA, double FACILITIES_AREA, double PARKINGLOT_AREA,double PLOT_SHARE_AREA, HouseholdStatistics houseHoldStatistic)
        {
            string sql = "INSERT INTO TD_DESIGN_AREA(REGI_MST_NO,REGI_SUB_MST_NO,REGI_PRE_DESIGN_NO,REGI_PLAN_TYPE_NO,TYPE_SIZE,TYPE_STRING,TYPE_DIRECTION,TYPE_COUNT,TYPE_EXCLUSIVE_AREA,TYPE_WALL_AREA,TYPE_CORE_AREA,TYPE_COMMON_USE_AREA,TYPE_SUPPLY_AREA,TYPE_WELFARE_AREA,TYPE_FACILITIES_AREA,TYPE_PARKINGLOT_AREA,TYPE_OTHER_COMMON_USE_AREA,TYPE_CONTRACT_AREA,TYPE_BALCONY_AREA,TYPE_USABLE_AREA,TYPE_PLOT_SHARE_AREA,UNIT_PLA_DRWN_VECTOR,FRST_REGIST_DT,FRST_REGISTER_ID) VALUES(:p_REGI_MST_NO,:p_REGI_SUB_MST_NO,:p_REGI_PRE_DESIGN_NO,:p_REGI_PLAN_TYPE_NO,:p_TYPE_SIZE,:p_TYPE_STRING,:p_TYPE_DIRECTION,:p_TYPE_COUNT,:p_TYPE_EXCLUSIVE_AREA,:p_TYPE_WALL_AREA,:p_TYPE_CORE_AREA,:p_TYPE_COMMON_USE_AREA,:p_TYPE_SUPPLY_AREA,:p_TYPE_WELFARE_AREA,:p_TYPE_FACILITIES_AREA,:p_TYPE_PARKINGLOT_AREA,:p_TYPE_OTHER_COMMON_USE_AREA,:p_TYPE_CONTRACT_AREA,:p_TYPE_BALCONY_AREA,:p_TYPE_USABLE_AREA,:p_TYPE_PLOT_SHARE_AREA,:p_UNIT_PLA_DRWN_VECTOR,SYSDATE,:p_FRST_REGISTER_ID)";

            using (OracleConnection connection = new OracleConnection(Consts.connectionString))
            {
                try
                {
                    connection.Open();

                    OracleCommand comm = new OracleCommand(sql, connection);

                    OracleParameter p_REGI_MST_NO = new OracleParameter();
                    OracleParameter p_REGI_SUB_MST_NO = new OracleParameter();
                    OracleParameter p_REGI_PRE_DESIGN_NO = new OracleParameter();
                    OracleParameter p_REGI_PLAN_TYPE_NO = new OracleParameter();
                    OracleParameter p_TYPE_SIZE = new OracleParameter();
                    OracleParameter p_TYPE_STRING = new OracleParameter();
                    OracleParameter p_TYPE_DIRECTION = new OracleParameter();
                    OracleParameter p_TYPE_COUNT = new OracleParameter();
                    OracleParameter p_TYPE_EXCLUSIVE_AREA = new OracleParameter();
                    OracleParameter p_TYPE_WALL_AREA = new OracleParameter();
                    OracleParameter p_TYPE_CORE_AREA = new OracleParameter();
                    OracleParameter p_TYPE_COMMON_USE_AREA = new OracleParameter();
                    OracleParameter p_TYPE_SUPPLY_AREA = new OracleParameter();
                    OracleParameter p_TYPE_WELFARE_AREA = new OracleParameter();
                    OracleParameter p_TYPE_FACILITIES_AREA = new OracleParameter();
                    OracleParameter p_TYPE_PARKINGLOT_AREA = new OracleParameter();
                    OracleParameter p_TYPE_OTHER_COMMON_USE_AREA = new OracleParameter();
                    OracleParameter p_TYPE_CONTRACT_AREA = new OracleParameter();
                    OracleParameter p_TYPE_BALCONY_AREA = new OracleParameter();
                    OracleParameter p_TYPE_USABLE_AREA = new OracleParameter();
                    OracleParameter p_TYPE_PLOT_SHARE_AREA = new OracleParameter();
                    OracleParameter p_UNIT_PLA_DRWN_VECTOR = new OracleParameter();
                    OracleParameter p_FRST_REGIST_DT = new OracleParameter();
                    OracleParameter p_FRST_REGISTER_ID = new OracleParameter();

                    double sup = Math.Round((CORE_AREA + houseHoldStatistic.GetWallArea() + houseHoldStatistic.GetExclusiveArea()) / 1000000, 2);

                    p_REGI_MST_NO.DbType = System.Data.DbType.String;
                    p_REGI_MST_NO.Value = idColumnCode[0].ToString();
                    p_REGI_MST_NO.ParameterName = "p_REGI_MST_NO";

                    p_REGI_SUB_MST_NO.DbType = System.Data.DbType.String;
                    p_REGI_SUB_MST_NO.Value = idColumnCode[1].ToString();
                    p_REGI_SUB_MST_NO.ParameterName = "p_REGI_SUB_MST_NO";

                    p_REGI_PRE_DESIGN_NO.DbType = System.Data.DbType.Decimal;
                    p_REGI_PRE_DESIGN_NO.Value = REGI_PRE_DESIGN_NO.ToString();
                    p_REGI_PRE_DESIGN_NO.ParameterName = "p_REGI_PRE_DESIGN_NO";

                    p_REGI_PLAN_TYPE_NO.DbType = System.Data.DbType.Decimal;
                    p_REGI_PLAN_TYPE_NO.Value = (GetLastPlanTypeNo(idColumnName, idColumnCode, REGI_PRE_DESIGN_NO) + 1).ToString();
                    p_REGI_PLAN_TYPE_NO.ParameterName = "p_REGI_PLAN_TYPE_NO";

                    p_TYPE_SIZE.DbType = System.Data.DbType.Decimal;
                    p_TYPE_SIZE.Value = Math.Round(sup * 0.3025, 0).ToString();
                    p_TYPE_SIZE.ParameterName = "p_TYPE_SIZE";

                    p_TYPE_STRING.DbType = System.Data.DbType.String;
                    p_TYPE_STRING.Value = typeString;
                    p_TYPE_STRING.ParameterName = "p_TYPE_STRING";

                    p_TYPE_DIRECTION.DbType = System.Data.DbType.String;
                    p_TYPE_DIRECTION.Value = getDirection(houseHoldStatistic.YDirection);
                    p_TYPE_DIRECTION.ParameterName = "p_TYPE_DIRECTION";

                    p_TYPE_COUNT.DbType = System.Data.DbType.Decimal;
                    p_TYPE_COUNT.Value = houseHoldStatistic.Count.ToString();
                    p_TYPE_COUNT.ParameterName = "p_TYPE_COUNT";

                    p_TYPE_EXCLUSIVE_AREA.DbType = System.Data.DbType.Decimal;
                    p_TYPE_EXCLUSIVE_AREA.Value = Math.Round(houseHoldStatistic.GetExclusiveArea() / 1000000 , 2).ToString();
                    p_TYPE_EXCLUSIVE_AREA.ParameterName = "p_TYPE_EXCLUSIVE_AREA";

                    p_TYPE_WALL_AREA.DbType = System.Data.DbType.Decimal;
                    p_TYPE_WALL_AREA.Value = Math.Round(houseHoldStatistic.GetWallArea() / 1000000, 2).ToString();
                    p_TYPE_WALL_AREA.ParameterName = "p_TYPE_WALL_AREA";

                    p_TYPE_CORE_AREA.DbType = System.Data.DbType.Decimal;
                    p_TYPE_CORE_AREA.Value = Math.Round(CORE_AREA / 1000000, 2).ToString();
                    p_TYPE_CORE_AREA.ParameterName = "p_TYPE_CORE_AREA";

                    p_TYPE_COMMON_USE_AREA.DbType = System.Data.DbType.Decimal;
                    p_TYPE_COMMON_USE_AREA.Value = Math.Round((CORE_AREA + houseHoldStatistic.GetWallArea()) / 1000000, 2).ToString();
                    p_TYPE_COMMON_USE_AREA.ParameterName = "p_TYPE_COMMON_USE_AREA";

                    p_TYPE_SUPPLY_AREA.DbType = System.Data.DbType.Decimal;
                    p_TYPE_SUPPLY_AREA.Value = sup.ToString();
                    p_TYPE_SUPPLY_AREA.ParameterName = "p_TYPE_SUPPLY_AREA";

                    p_TYPE_WELFARE_AREA.DbType = System.Data.DbType.Decimal;
                    p_TYPE_WELFARE_AREA.Value = WELFARE_AREA;
                    p_TYPE_WELFARE_AREA.ParameterName = "p_TYPE_WELFARE_AREA";

                    p_TYPE_FACILITIES_AREA.DbType = System.Data.DbType.Decimal;
                    p_TYPE_FACILITIES_AREA.Value = FACILITIES_AREA;
                    p_TYPE_FACILITIES_AREA.ParameterName = "p_TYPE_FACILITIES_AREA";

                    p_TYPE_PARKINGLOT_AREA.DbType = System.Data.DbType.Decimal;
                    p_TYPE_PARKINGLOT_AREA.Value = Math.Round(PARKINGLOT_AREA /1000000, 2).ToString();
                    p_TYPE_PARKINGLOT_AREA.ParameterName = "p_TYPE_PARKINGLOT_AREA";

                    p_TYPE_OTHER_COMMON_USE_AREA.DbType = System.Data.DbType.Decimal;
                    p_TYPE_OTHER_COMMON_USE_AREA.Value = Math.Round((WELFARE_AREA + FACILITIES_AREA + PARKINGLOT_AREA) / 1000000, 2).ToString();
                    p_TYPE_OTHER_COMMON_USE_AREA.ParameterName = "p_TYPE_OTHER_COMMON_USE_AREA";

                    p_TYPE_CONTRACT_AREA.DbType = System.Data.DbType.Decimal;
                    p_TYPE_CONTRACT_AREA.Value = Math.Round((houseHoldStatistic.GetExclusiveArea() + houseHoldStatistic.GetWallArea() + CORE_AREA + WELFARE_AREA + FACILITIES_AREA + PARKINGLOT_AREA) / 1000000, 2).ToString();
                    p_TYPE_CONTRACT_AREA.ParameterName = "p_TYPE_CONTRACT_AREA";

                    p_TYPE_BALCONY_AREA.DbType = System.Data.DbType.Decimal;
                    p_TYPE_BALCONY_AREA.Value = Math.Round(houseHoldStatistic.GetBalconyArea() / 1000000, 2).ToString();
                    p_TYPE_BALCONY_AREA.ParameterName = "p_TYPE_BALCONY_AREA";

                    p_TYPE_USABLE_AREA.DbType = System.Data.DbType.Decimal;
                    p_TYPE_USABLE_AREA.Value = Math.Round((houseHoldStatistic.GetExclusiveArea() + houseHoldStatistic.GetBalconyArea()) / 1000000, 2).ToString();
                    p_TYPE_USABLE_AREA.ParameterName = "p_TYPE_USABLE_AREA";

                    p_TYPE_PLOT_SHARE_AREA.DbType = System.Data.DbType.Decimal;
                    p_TYPE_PLOT_SHARE_AREA.Value = Math.Round(PLOT_SHARE_AREA / 1000000, 2).ToString();
                    p_TYPE_PLOT_SHARE_AREA.ParameterName = "p_TYPE_PLOT_SHARE_AREA";

                    p_UNIT_PLA_DRWN_VECTOR.DbType = System.Data.DbType.String; ////////////////////////////////////
                    p_UNIT_PLA_DRWN_VECTOR.Value = "";
                    p_UNIT_PLA_DRWN_VECTOR.ParameterName = "p_UNIT_PLA_DRWN_VECTOR";

                    p_FRST_REGISTER_ID.DbType = System.Data.DbType.String;
                    p_FRST_REGISTER_ID.Value = userID;
                    p_FRST_REGISTER_ID.ParameterName = "p_FRST_REGISTER_ID";

                    comm.Parameters.Add(p_REGI_MST_NO);
                    comm.Parameters.Add(p_REGI_SUB_MST_NO);
                    comm.Parameters.Add(p_REGI_PRE_DESIGN_NO);
                    comm.Parameters.Add(p_REGI_PLAN_TYPE_NO);
                    comm.Parameters.Add(p_TYPE_SIZE);
                    comm.Parameters.Add(p_TYPE_STRING);
                    comm.Parameters.Add(p_TYPE_DIRECTION);
                    comm.Parameters.Add(p_TYPE_COUNT);
                    comm.Parameters.Add(p_TYPE_EXCLUSIVE_AREA);
                    comm.Parameters.Add(p_TYPE_WALL_AREA);
                    comm.Parameters.Add(p_TYPE_CORE_AREA);
                    comm.Parameters.Add(p_TYPE_COMMON_USE_AREA);
                    comm.Parameters.Add(p_TYPE_SUPPLY_AREA);
                    comm.Parameters.Add(p_TYPE_WELFARE_AREA);
                    comm.Parameters.Add(p_TYPE_FACILITIES_AREA);
                    comm.Parameters.Add(p_TYPE_PARKINGLOT_AREA);
                    comm.Parameters.Add(p_TYPE_OTHER_COMMON_USE_AREA);
                    comm.Parameters.Add(p_TYPE_CONTRACT_AREA);
                    comm.Parameters.Add(p_TYPE_BALCONY_AREA);
                    comm.Parameters.Add(p_TYPE_USABLE_AREA);
                    comm.Parameters.Add(p_TYPE_PLOT_SHARE_AREA);
                    comm.Parameters.Add(p_UNIT_PLA_DRWN_VECTOR);
                    comm.Parameters.Add(p_FRST_REGISTER_ID);

                    comm.ExecuteNonQuery();
                    comm.Dispose();
                }
                catch (OracleException ex)
                {
                    MessageBox.Show(ex.ToString());
                }
            }
        }

        public static string GetBuildingTypeCode(int maxStories)
        {
            if (maxStories < 5)
                return "1300";
            else
                return "2100";
        }

        public static string GetBuildingScale(int maxStories, int underGroundStories)
        {
            if (underGroundStories > 0)
                return "지하 " + underGroundStories.ToString() + "층, " + "지상 " + maxStories.ToString() + "층";
            else
                return "지상 " + maxStories.ToString() + "층";
        }

        public static void AddDesignDetail(List<string> idColumnName, List<string> idColumnCode, string userID, ApartmentGeneratorOutput agOutput, out int REGI_PRE_DESIGN_NO)
        {
            //////20160516_수정완료, 지하주차장 데이터 입력할 필요가 있음

            string sql = "INSERT INTO TD_DESIGN_DETAIL(REGI_MST_NO,REGI_SUB_MST_NO,REGI_PRE_DESIGN_NO, DESIGN_FXD_AT ,BUILDING_TYPE_CD,BUILDING_SCALE,BUILDING_STRUCTURE,BUILDING_AREA,FLOOR_AREA_UG,FLOOR_AREA_G,FLOOR_AREA_WHOLE,STORIES_UNDERGROUND,STORIES_ON_EARTH,BALCONY_AREA,PARKING_ROOFTOP_AREA,FLOOR_AREA_CONSTRUCTION,BUILDING_COVERAGE,GROSS_AREA_RATIO,HOUSEHOLD_COUNT,PARKINGLOT_COUNT,PARKINGLOT_AREA,PARKINGLOT_COUNT_LEGAL,LANDSCAPE_AREA,LANDSCAPE_AREA_LEGAL,NEIGHBOR_STORE_AREA,PUBLIC_FACILITY_AREA,FRST_REGIST_DT,FRST_REGISTER_ID) VALUES(:p_REGI_MST_NO,:p_REGI_SUB_MST_NO,:p_REGI_PRE_DESIGN_NO,:p_DESIGN_FXD_AT,:p_BUILDING_TYPE_CD,:p_BUILDING_SCALE,:p_STRUCTURE,:p_BUILDING_AREA,:p_FLOOR_AREA_UG,:p_FLOOR_AREA_G,:p_FLOOR_AREA_WHOLE,:p_STORIES_UNDERGROUND,:p_STORIES_ON_EARTH,:p_BALCONY_AREA,:p_PARKING_ROOFTOP_AREA,:p_FLOOR_AREA_CONSTRUCTION,:p_BUILDING_COVERAGE,:p_GROSS_AREA_RATIO,:p_HOUSEHOLD_COUNT,:p_PARKINGLOT_COUNT,:p_PARKINGLOT_AREA,:p_PARKINGLOT_COUNT_LEGAL,:p_LANDSCAPE_AREA,:p_LANDSCAPE_AREA_LEGAL,:p_NEIGHBOR_STORE_AREA,:p_PUBLIC_FACILITY_AREA,SYSDATE ,:p_FRST_REGISTER_ID)";

            REGI_PRE_DESIGN_NO = GetLastPreDesignNo(idColumnName, idColumnCode) + 1;

            using (OracleConnection connection = new OracleConnection(Consts.connectionString))
            {
                try
                {
                    connection.Open();

                    OracleCommand comm = new OracleCommand(sql, connection);

                    OracleParameter p_REGI_MST_NO = new OracleParameter();
                    OracleParameter p_REGI_SUB_MST_NO = new OracleParameter();
                    OracleParameter p_REGI_PRE_DESIGN_NO = new OracleParameter();
                    OracleParameter p_DESIGN_FXD_AT = new OracleParameter();
                    OracleParameter p_BUILDING_TYPE_CD = new OracleParameter();
                    OracleParameter p_BUILDING_SCALE = new OracleParameter();
                    OracleParameter p_STRUCTURE = new OracleParameter();
                    OracleParameter p_BUILDING_AREA = new OracleParameter();
                    OracleParameter p_FLOOR_AREA_UG = new OracleParameter();
                    OracleParameter p_FLOOR_AREA_G = new OracleParameter();
                    OracleParameter p_FLOOR_AREA_WHOLE = new OracleParameter();
                    OracleParameter p_STORIES_UNDERGROUND = new OracleParameter();
                    OracleParameter p_STORIES_ON_EARTH = new OracleParameter();
                    OracleParameter p_BALCONY_AREA = new OracleParameter();
                    OracleParameter p_PARKING_ROOFTOP_AREA = new OracleParameter();
                    OracleParameter p_FLOOR_AREA_CONSTRUCTION = new OracleParameter();
                    OracleParameter p_BUILDING_COVERAGE = new OracleParameter();
                    OracleParameter p_GROSS_AREA_RATIO = new OracleParameter();
                    OracleParameter p_HOUSEHOLD_COUNT = new OracleParameter();
                    OracleParameter p_PARKINGLOT_COUNT = new OracleParameter();
                    OracleParameter p_PARKINGLOT_AREA = new OracleParameter();
                    OracleParameter p_PARKINGLOT_COUNT_LEGAL = new OracleParameter();
                    OracleParameter p_LANDSCAPE_AREA = new OracleParameter();
                    OracleParameter p_LANDSCAPE_AREA_LEGAL = new OracleParameter();
                    OracleParameter p_NEIGHBOR_STORE_AREA = new OracleParameter();
                    OracleParameter p_PUBLIC_FACILITY_AREA = new OracleParameter();
                    OracleParameter p_FRST_REGIST_DT = new OracleParameter();
                    OracleParameter p_FRST_REGISTER_ID = new OracleParameter();

                    p_REGI_MST_NO.DbType = System.Data.DbType.String;
                    p_REGI_MST_NO.Value = idColumnCode[0].ToString();
                    p_REGI_MST_NO.ParameterName = "p_REGI_MST_NO";

                    p_REGI_SUB_MST_NO.DbType = System.Data.DbType.String;
                    p_REGI_SUB_MST_NO.Value = idColumnCode[1].ToString();
                    p_REGI_SUB_MST_NO.ParameterName = "p_REGI_SUB_MST_NO";

                    p_REGI_PRE_DESIGN_NO.DbType = System.Data.DbType.Decimal;
                    p_REGI_PRE_DESIGN_NO.Value = (GetLastPreDesignNo(idColumnName, idColumnCode) + 1).ToString();
                    p_REGI_PRE_DESIGN_NO.ParameterName = "p_REGI_PRE_DESIGN_NO";

                    p_DESIGN_FXD_AT.DbType = System.Data.DbType.String;
                    p_DESIGN_FXD_AT.Value = "0";
                    p_DESIGN_FXD_AT.ParameterName = "p_DESIGN_FXD_AT";

                    p_BUILDING_TYPE_CD.DbType = System.Data.DbType.String;
                    p_BUILDING_TYPE_CD.Value = GetBuildingTypeCode(Math.Max((int)agOutput.ParameterSet.Parameters[0], (int)agOutput.ParameterSet.Parameters[1]));
                    p_BUILDING_TYPE_CD.ParameterName = "p_BUILDING_TYPE_CD";

                    p_BUILDING_SCALE.DbType = System.Data.DbType.String;
                    p_BUILDING_SCALE.Value = GetBuildingScale(Math.Max((int)agOutput.ParameterSet.Parameters[0], (int)agOutput.ParameterSet.Parameters[1]), agOutput.ParkingLotUnderGround.Floors);
                    p_BUILDING_SCALE.ParameterName = "p_BUILDING_SCALE";

                    p_STRUCTURE.DbType = System.Data.DbType.String;
                    p_STRUCTURE.Value = "철근콘크리트 구조";
                    p_STRUCTURE.ParameterName = "p_STRUCTURE";

                    p_BUILDING_AREA.DbType = System.Data.DbType.String;
                    p_BUILDING_AREA.Value = Math.Round(agOutput.GetBuildingArea() / 1000000, 2).ToString();
                    p_BUILDING_AREA.ParameterName = "p_BUILDING_AREA";

                    p_FLOOR_AREA_UG.DbType = System.Data.DbType.Decimal;
                    p_FLOOR_AREA_UG.Value = Math.Round(agOutput.ParkingLotUnderGround.ParkingArea / 1000000, 2).ToString();
                    p_FLOOR_AREA_UG.ParameterName = "p_FLOOR_AREA_UG";

                    p_FLOOR_AREA_G.DbType = System.Data.DbType.Decimal;
                    p_FLOOR_AREA_G.Value = Math.Round(agOutput.GetGrossArea() / 1000000, 2).ToString();
                    p_FLOOR_AREA_G.ParameterName = "p_FLOOR_AREA_G";

                    p_FLOOR_AREA_WHOLE.DbType = System.Data.DbType.Decimal;
                    p_FLOOR_AREA_WHOLE.Value = Math.Round(agOutput.GetGrossArea() / 1000000, 2).ToString();
                    p_FLOOR_AREA_WHOLE.ParameterName = "p_FLOOR_AREA_WHOLE";

                    p_STORIES_UNDERGROUND.DbType = System.Data.DbType.Decimal;
                    p_STORIES_UNDERGROUND.Value = agOutput.ParkingLotUnderGround.Floors.ToString();
                    p_STORIES_UNDERGROUND.ParameterName = "p_STORIES_UNDERGROUND";

                    p_STORIES_ON_EARTH.DbType = System.Data.DbType.Decimal;
                    p_STORIES_ON_EARTH.Value = (Math.Max((int)agOutput.ParameterSet.Parameters[0], (int)agOutput.ParameterSet.Parameters[1]) + 1).ToString();
                    p_STORIES_ON_EARTH.ParameterName = "p_STORIES_ON_EARTH";

                    p_BALCONY_AREA.DbType = System.Data.DbType.Decimal;
                    p_BALCONY_AREA.Value = Math.Round(agOutput.GetBalconyArea() / 1000000, 2).ToString();
                    p_BALCONY_AREA.ParameterName = "p_BALCONY_AREA";

                    p_PARKING_ROOFTOP_AREA.DbType = System.Data.DbType.Decimal;
                    p_PARKING_ROOFTOP_AREA.Value = Math.Round((agOutput.GetCoreAreaOnEarthSum() + agOutput.ParkingLotUnderGround.ParkingArea) / 1000000, 2).ToString();
                    p_PARKING_ROOFTOP_AREA.ParameterName = "p_PARKING_ROOFTOP_AREA";

                    p_FLOOR_AREA_CONSTRUCTION.DbType = System.Data.DbType.Decimal;
                    p_FLOOR_AREA_CONSTRUCTION.Value = Math.Round((agOutput.GetGrossArea() + agOutput.GetBalconyArea() + agOutput.GetCoreAreaOnEarthSum() + agOutput.ParkingLotUnderGround.ParkingArea) / 1000000, 2).ToString();
                    p_FLOOR_AREA_CONSTRUCTION.ParameterName = "p_FLOOR_AREA_CONSTRUCTION";

                    p_BUILDING_COVERAGE.DbType = System.Data.DbType.Decimal;
                    p_BUILDING_COVERAGE.Value = Math.Round(agOutput.GetBuildingCoverage(), 2).ToString();
                    p_BUILDING_COVERAGE.ParameterName = "p_BUILDING_COVERAGE";

                    p_GROSS_AREA_RATIO.DbType = System.Data.DbType.Decimal;
                    p_GROSS_AREA_RATIO.Value = Math.Round(agOutput.GetGrossAreaRatio(), 2).ToString();
                    p_GROSS_AREA_RATIO.ParameterName = "p_GROSS_AREA_RATIO";

                    p_HOUSEHOLD_COUNT.DbType = System.Data.DbType.Decimal;
                    p_HOUSEHOLD_COUNT.Value = agOutput.GetHouseholdCount().ToString();
                    p_HOUSEHOLD_COUNT.ParameterName = "p_HOUSEHOLD_COUNT";

                    p_PARKINGLOT_COUNT.DbType = System.Data.DbType.Decimal;
                    p_PARKINGLOT_COUNT.Value = (agOutput.ParkingLotOnEarth.GetCount() + agOutput.ParkingLotUnderGround.Count).ToString();
                    p_PARKINGLOT_COUNT.ParameterName = "p_PARKINGLOT_COUNT";

                    p_PARKINGLOT_AREA.DbType = System.Data.DbType.Decimal;
                    p_PARKINGLOT_AREA.Value = Math.Round(agOutput.ParkingLotUnderGround.ParkingArea / 1000000, 2).ToString();
                    p_PARKINGLOT_AREA.ParameterName = "p_PARKINGLOT_AREA";

                    p_PARKINGLOT_COUNT_LEGAL.DbType = System.Data.DbType.Decimal;
                    p_PARKINGLOT_COUNT_LEGAL.Value = (agOutput.GetLegalParkingLotOfCommercial() + agOutput.GetLegalParkingLotofHousing()).ToString();
                    p_PARKINGLOT_COUNT_LEGAL.ParameterName = "p_PARKINGLOT_COUNT_LEGAL";

                    p_LANDSCAPE_AREA.DbType = System.Data.DbType.Decimal;
                    p_LANDSCAPE_AREA.Value = Math.Round(agOutput.GetGreenArea() / 1000000, 2).ToString();
                    p_LANDSCAPE_AREA.ParameterName = "p_LANDSCAPE_AREA";

                    p_LANDSCAPE_AREA_LEGAL.DbType = System.Data.DbType.Decimal;
                    p_LANDSCAPE_AREA_LEGAL.Value = Math.Round(agOutput.CalculateLegalGreen() / 1000000, 2).ToString();
                    p_LANDSCAPE_AREA_LEGAL.ParameterName = "p_LANDSCAPE_AREA_LEGAL";

                    p_NEIGHBOR_STORE_AREA.DbType = System.Data.DbType.Decimal;
                    p_NEIGHBOR_STORE_AREA.Value = Math.Round(agOutput.GetCommercialArea() / 1000000, 2).ToString();
                    p_NEIGHBOR_STORE_AREA.ParameterName = "p_NEIGHBOR_STORE_AREA";

                    p_PUBLIC_FACILITY_AREA.DbType = System.Data.DbType.Decimal;
                    p_PUBLIC_FACILITY_AREA.Value = Math.Round(agOutput.GetPublicFacilityArea() / 1000000, 2).ToString();
                    p_PUBLIC_FACILITY_AREA.ParameterName = "p_PUBLIC_FACILITY_AREA";

                    p_FRST_REGISTER_ID.DbType = System.Data.DbType.String;
                    p_FRST_REGISTER_ID.Value = userID;
                    p_FRST_REGISTER_ID.ParameterName = "p_FRST_REGISTER_ID";

                    comm.Parameters.Add(p_REGI_MST_NO);
                    comm.Parameters.Add(p_REGI_SUB_MST_NO);
                    comm.Parameters.Add(p_REGI_PRE_DESIGN_NO);
                    comm.Parameters.Add(p_DESIGN_FXD_AT);
                    comm.Parameters.Add(p_BUILDING_TYPE_CD);
                    comm.Parameters.Add(p_BUILDING_SCALE);
                    comm.Parameters.Add(p_STRUCTURE);
                    comm.Parameters.Add(p_BUILDING_AREA);
                    comm.Parameters.Add(p_FLOOR_AREA_UG);
                    comm.Parameters.Add(p_FLOOR_AREA_G);
                    comm.Parameters.Add(p_FLOOR_AREA_WHOLE);
                    comm.Parameters.Add(p_STORIES_UNDERGROUND);
                    comm.Parameters.Add(p_STORIES_ON_EARTH);
                    comm.Parameters.Add(p_BALCONY_AREA);
                    comm.Parameters.Add(p_PARKING_ROOFTOP_AREA);
                    comm.Parameters.Add(p_FLOOR_AREA_CONSTRUCTION);
                    comm.Parameters.Add(p_BUILDING_COVERAGE);
                    comm.Parameters.Add(p_GROSS_AREA_RATIO);
                    comm.Parameters.Add(p_HOUSEHOLD_COUNT);
                    comm.Parameters.Add(p_PARKINGLOT_COUNT);
                    comm.Parameters.Add(p_PARKINGLOT_AREA);
                    comm.Parameters.Add(p_PARKINGLOT_COUNT_LEGAL);
                    comm.Parameters.Add(p_LANDSCAPE_AREA);
                    comm.Parameters.Add(p_LANDSCAPE_AREA_LEGAL);
                    comm.Parameters.Add(p_NEIGHBOR_STORE_AREA);
                    comm.Parameters.Add(p_PUBLIC_FACILITY_AREA);
                    comm.Parameters.Add(p_FRST_REGISTER_ID);

                    comm.ExecuteNonQuery();
                    comm.Dispose();
                }
                catch (OracleException ex)
                {
                    MessageBox.Show(ex.ToString());
                }
            }
        }
        public static string ListToCSV(List<int> inputList)
        {
            string output = "";

            for (int i = 0; i < inputList.Count(); i++)
            {
                if (i != 0)
                    output += ",";

                output += inputList[i].ToString();
            }

            return output;
        }

        public static string ListToCSV(List<double> inputList)
        {
            string output = "";

            for (int i = 0; i < inputList.Count(); i++)
            {
                if (i != 0)
                    output += ",";

                output += inputList[i].ToString();
            }

            return output;
        }

        public enum NonResiType { Commercial, PublicFacility };

        public static string GetNonresiUseCode(NonResiType type)
        {
            if (type == NonResiType.Commercial)
                return "1000";
            else if (type == NonResiType.PublicFacility)
                return "2000";
            else
                return "1000";

        }



        public static void AddDesignNonResidential(List<string> idColumnName, List<string> idColumnCode, string userID, int REGI_PRE_DESIGN_NO, string USE_CD, double NONRESI_EXCLUSIVE_AREA, double NONRESI_COMMON_USE_AREA, double NONRESI_PARKING_AREA, int NONRESI_LEGAL_PARKING, double NONRESI_PLOT_SHARE_AREA)
        {
            string sql = "INSERT INTO TD_DESIGN_NONRESIDENTIAL(REGI_MST_NO,REGI_SUB_MST_NO,REGI_PRE_DESIGN_NO,NONRESI_USE_CD,NONRESI_EXCLUSIVE_AREA,NONRESI_COMMON_USE_AREA,NONRESI_SUPPLY_AREA,NONRESI_PARKING_AREA,NONRESI_CONTRACT_AREA,NONRESI_LEAGAL_PARKING,NONRESI_PLOT_SHARE_AREA,FRST_REGIST_DT,FRST_REGISTER_ID) VALUES(:p_REGI_MST_NO,:p_REGI_SUB_MST_NO,:p_REGI_PRE_DESIGN_NO,:p_NONRESI_USE_CD,:p_NONRESI_EXCLUSIVE_AREA,:p_NONRESI_COMMON_USE_AREA,:p_NONRESI_SUPPLY_AREA,:p_NONRESI_PARKING_AREA,:p_NONRESI_CONTRACT_AREA,:p_NONRESI_LEAGAL_PARKING,:p_NONRESI_PLOT_SHARE_AREA,SYSDATE ,:p_FRST_REGISTER_ID)";

            using (OracleConnection connection = new OracleConnection(Consts.connectionString))
            {
                try
                {
                    connection.Open();

                    OracleCommand comm = new OracleCommand(sql, connection);

                    OracleParameter p_REGI_MST_NO = new OracleParameter();
                    OracleParameter p_REGI_SUB_MST_NO = new OracleParameter();
                    OracleParameter p_REGI_PRE_DESIGN_NO = new OracleParameter();
                    OracleParameter p_NONRESI_USE_CD = new OracleParameter();
                    OracleParameter p_NONRESI_EXCLUSIVE_AREA = new OracleParameter();
                    OracleParameter p_NONRESI_COMMON_USE_AREA = new OracleParameter();
                    OracleParameter p_NONRESI_SUPPLY_AREA = new OracleParameter();
                    OracleParameter p_NONRESI_PARKING_AREA = new OracleParameter();
                    OracleParameter p_NONRESI_CONTRACT_AREA = new OracleParameter();
                    OracleParameter p_NONRESI_LEGAL_PARKING = new OracleParameter();
                    OracleParameter p_NONRESI_PLOT_SHARE_AREA = new OracleParameter();
                    OracleParameter p_FRST_REGISTER_ID = new OracleParameter();

                    p_REGI_MST_NO.DbType = System.Data.DbType.String;
                    p_REGI_MST_NO.Value = idColumnCode[0].ToString();
                    p_REGI_MST_NO.ParameterName = "p_REGI_MST_NO";

                    p_REGI_SUB_MST_NO.DbType = System.Data.DbType.String;
                    p_REGI_SUB_MST_NO.Value = idColumnCode[1].ToString();
                    p_REGI_SUB_MST_NO.ParameterName = "p_REGI_SUB_MST_NO";

                    p_REGI_PRE_DESIGN_NO.DbType = System.Data.DbType.Decimal;
                    p_REGI_PRE_DESIGN_NO.Value = REGI_PRE_DESIGN_NO.ToString();
                    p_REGI_PRE_DESIGN_NO.ParameterName = "p_REGI_PRE_DESIGN_NO";

                    p_NONRESI_USE_CD.DbType = System.Data.DbType.String;
                    p_NONRESI_USE_CD.Value = USE_CD.ToString();
                    p_NONRESI_USE_CD.ParameterName = "p_NONRESI_USE_CD";

                    p_NONRESI_EXCLUSIVE_AREA.DbType = System.Data.DbType.Decimal;
                    p_NONRESI_EXCLUSIVE_AREA.Value = Math.Round(NONRESI_EXCLUSIVE_AREA / 1000000, 2).ToString();
                    p_NONRESI_EXCLUSIVE_AREA.ParameterName = "p_NONRESI_EXCLUSIVE_AREA";

                    p_NONRESI_COMMON_USE_AREA.DbType = System.Data.DbType.Decimal;
                    p_NONRESI_COMMON_USE_AREA.Value = Math.Round(NONRESI_COMMON_USE_AREA / 1000000, 2).ToString();
                    p_NONRESI_COMMON_USE_AREA.ParameterName = "p_NONRESI_COMMON_USE_AREA";

                    p_NONRESI_SUPPLY_AREA.DbType = System.Data.DbType.Decimal;
                    p_NONRESI_SUPPLY_AREA.Value = Math.Round((NONRESI_EXCLUSIVE_AREA + NONRESI_COMMON_USE_AREA) / 1000000, 2).ToString();
                    p_NONRESI_SUPPLY_AREA.ParameterName = "p_NONRESI_SUPPLY_AREA";

                    p_NONRESI_PARKING_AREA.DbType = System.Data.DbType.Decimal;
                    p_NONRESI_PARKING_AREA.Value = Math.Round(NONRESI_PARKING_AREA / 1000000, 2).ToString();
                    p_NONRESI_PARKING_AREA.ParameterName = "p_NONRESI_PARKING_AREA";

                    p_NONRESI_CONTRACT_AREA.DbType = System.Data.DbType.Decimal;
                    p_NONRESI_CONTRACT_AREA.Value = Math.Round((NONRESI_EXCLUSIVE_AREA + NONRESI_COMMON_USE_AREA + NONRESI_PARKING_AREA) / 1000000, 2).ToString();
                    p_NONRESI_CONTRACT_AREA.ParameterName = "p_NONRESI_CONTRACT_AREA";

                    p_NONRESI_LEGAL_PARKING.DbType = System.Data.DbType.Decimal;
                    p_NONRESI_LEGAL_PARKING.Value = NONRESI_LEGAL_PARKING.ToString();
                    p_NONRESI_LEGAL_PARKING.ParameterName = "p_NONRESI_LEAGAL_PARKING";

                    p_NONRESI_PLOT_SHARE_AREA.DbType = System.Data.DbType.Decimal;
                    p_NONRESI_PLOT_SHARE_AREA.Value = Math.Round(NONRESI_PLOT_SHARE_AREA / 1000000, 2).ToString();
                    p_NONRESI_PLOT_SHARE_AREA.ParameterName = "p_NONRESI_PLOT_SHARE_AREA";

                    p_FRST_REGISTER_ID.DbType = System.Data.DbType.String;
                    p_FRST_REGISTER_ID.Value = userID;
                    p_FRST_REGISTER_ID.ParameterName = "p_FRST_REGISTER_ID";

                    comm.Parameters.Add(p_REGI_MST_NO);
                    comm.Parameters.Add(p_REGI_SUB_MST_NO);
                    comm.Parameters.Add(p_REGI_PRE_DESIGN_NO);
                    comm.Parameters.Add(p_NONRESI_USE_CD);
                    comm.Parameters.Add(p_NONRESI_EXCLUSIVE_AREA);
                    comm.Parameters.Add(p_NONRESI_COMMON_USE_AREA);
                    comm.Parameters.Add(p_NONRESI_SUPPLY_AREA);
                    comm.Parameters.Add(p_NONRESI_PARKING_AREA);
                    comm.Parameters.Add(p_NONRESI_CONTRACT_AREA);
                    comm.Parameters.Add(p_NONRESI_LEGAL_PARKING);
                    comm.Parameters.Add(p_NONRESI_PLOT_SHARE_AREA);

                    comm.Parameters.Add(p_FRST_REGISTER_ID);

                    comm.ExecuteNonQuery();
                    comm.Dispose();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                }
            }
        }
        public static void AddDesignModel(List<string> idColumnName, List<string> idColumnCode, string userID, int REGI_PRE_DESIGN_NO, string DESIGN_PATTERN, string INPUT_PARAMETER, string CORE_TYPE, List<double> targetArea, List<double> targetTypeCount)
        {
            string sql = "INSERT INTO TD_DESIGN_MODEL(REGI_MST_NO,REGI_SUB_MST_NO,REGI_PRE_DESIGN_NO,DESIGN_PATTERN,INPUT_PARAM_VECTOR,CORE_TYPE,GOAL_PYUNG_TYPE_VECTOR,GOAL_RT_VECTOR,FRST_REGIST_DT,FRST_REGISTER_ID) VALUES(:p_REGI_MST_NO,:p_REGI_SUB_MST_NO,:p_REGI_PRE_DESIGN_NO,:p_DESIGN_PATTERN,:p_INPUT_PARAM_VECTOR,:p_CORE_TYPE,:p_GOAL_PYUNG_TYPE_VECTOR,:p_GOAL_RT_VECTOR,SYSDATE,:p_FRST_REGISTER_ID)";

            using (OracleConnection connection = new OracleConnection(Consts.connectionString))
            {
                try
                {
                    connection.Open();

                    OracleCommand comm = new OracleCommand(sql, connection);

                    OracleParameter p_REGI_MST_NO = new OracleParameter();
                    OracleParameter p_REGI_SUB_MST_NO = new OracleParameter();
                    OracleParameter p_REGI_PRE_DESIGN_NO = new OracleParameter();
                    OracleParameter p_DESIGN_PATTERN = new OracleParameter();
                    OracleParameter p_INPUT_PARAM_VECTOR = new OracleParameter();
                    OracleParameter p_CORE_TYPE = new OracleParameter();
                    OracleParameter p_GOAL_PYUNG_TYPE_VECTOR = new OracleParameter();
                    OracleParameter p_GOAL_RT_VECTOR = new OracleParameter();
                    OracleParameter p_FRST_REGIST_DT = new OracleParameter();
                    OracleParameter p_FRST_REGISTER_ID = new OracleParameter();

                    p_REGI_MST_NO.DbType = System.Data.DbType.String;
                    p_REGI_MST_NO.Value = idColumnCode[0].ToString();
                    p_REGI_MST_NO.ParameterName = "p_REGI_MST_NO";

                    p_REGI_SUB_MST_NO.DbType = System.Data.DbType.String;
                    p_REGI_SUB_MST_NO.Value = idColumnCode[1].ToString();
                    p_REGI_SUB_MST_NO.ParameterName = "p_REGI_SUB_MST_NO";

                    p_REGI_PRE_DESIGN_NO.DbType = System.Data.DbType.Decimal;
                    p_REGI_PRE_DESIGN_NO.Value = REGI_PRE_DESIGN_NO.ToString();
                    p_REGI_PRE_DESIGN_NO.ParameterName = "p_REGI_PRE_DESIGN_NO";

                    p_DESIGN_PATTERN.DbType = System.Data.DbType.String;
                    p_DESIGN_PATTERN.Value = DESIGN_PATTERN;
                    p_DESIGN_PATTERN.ParameterName = "p_DESIGN_PATTERN";

                    p_INPUT_PARAM_VECTOR.DbType = System.Data.DbType.String;
                    p_INPUT_PARAM_VECTOR.Value = INPUT_PARAMETER;
                    p_INPUT_PARAM_VECTOR.ParameterName = "p_INPUT_PARAM_VECTOR";

                    p_CORE_TYPE.DbType = System.Data.DbType.String;
                    p_CORE_TYPE.Value = CORE_TYPE;
                    p_CORE_TYPE.ParameterName = "p_CORE_TYPE";

                    p_GOAL_PYUNG_TYPE_VECTOR.DbType = System.Data.DbType.String;
                    p_GOAL_PYUNG_TYPE_VECTOR.Value = ListToCSV(targetArea);
                    p_GOAL_PYUNG_TYPE_VECTOR.ParameterName = "p_GOAL_PYUNG_TYPE_VECTOR";

                    p_GOAL_RT_VECTOR.DbType = System.Data.DbType.String;
                    p_GOAL_RT_VECTOR.Value = ListToCSV(targetTypeCount);
                    p_GOAL_RT_VECTOR.ParameterName = "p_GOAL_RT_VECTOR";

                    p_FRST_REGISTER_ID.DbType = System.Data.DbType.String;
                    p_FRST_REGISTER_ID.Value = userID;
                    p_FRST_REGISTER_ID.ParameterName = "p_FRST_REGISTER_ID";

                    comm.Parameters.Add(p_REGI_MST_NO);
                    comm.Parameters.Add(p_REGI_SUB_MST_NO);
                    comm.Parameters.Add(p_REGI_PRE_DESIGN_NO);
                    comm.Parameters.Add(p_DESIGN_PATTERN);
                    comm.Parameters.Add(p_INPUT_PARAM_VECTOR);
                    comm.Parameters.Add(p_CORE_TYPE);
                    comm.Parameters.Add(p_GOAL_PYUNG_TYPE_VECTOR);
                    comm.Parameters.Add(p_GOAL_RT_VECTOR);

                    comm.Parameters.Add(p_FRST_REGISTER_ID);

                    comm.ExecuteNonQuery();
                    comm.Dispose();
                }
                catch (OracleException ex)
                {
                    MessageBox.Show(ex.ToString());
                }
            }
        }

        public static void SaveDesignReportBlob(string sourceFilePath, string rowName, List<string> idColumnName, List<string> idColumnCode)
        {
            string sql = "UPDATAE TD_DESIGN_REPORT SET " + rowName + "@Source";

            if (idColumnName.Count() != 0)
                sql += " WHERE";

            for (int i = 0; i < idColumnName.Count(); i++)
            {
                if (i != 0)
                    sql += " AND";

                sql += " " + idColumnName[i] + "=" + idColumnCode[i] + " ";
            }

            using (OracleConnection connection = new OracleConnection(Consts.connectionString))
            {
                try
                {
                    connection.Open();

                    OracleCommand comm = new OracleCommand(sql, connection);

                    System.IO.FileStream fs = new System.IO.FileStream(sourceFilePath, System.IO.FileMode.Open, System.IO.FileAccess.Read);
                    Byte[] currentByte = new Byte[fs.Length];
                    fs.Read(currentByte, 0, currentByte.Length);
                    fs.Close();

                    OracleParameter currentParameter = new OracleParameter("@Source", OracleDbType.Blob, currentByte.Length, System.Data.ParameterDirection.Input, false, 0, 0, null, System.Data.DataRowVersion.Current, currentByte);

                    comm.Parameters.Add(currentParameter);

                    connection.Open();
                    comm.ExecuteNonQuery();
                    connection.Close();
                }
                catch(Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        public static void AddDesignReport(List<string> idColumnName, List<string> idColumnCode,int REGI_PRE_DESIGN_NO, List<string> FilePath, List<string> RowName)
        {
            List<string> idColumnNameCopy = new List<string>(idColumnName);
            List<string> idColumnCodeCopy = new List<string>(idColumnCode);

            idColumnNameCopy.Add("REGI_PRE_DESIGN_NO");
            idColumnCodeCopy.Add(REGI_PRE_DESIGN_NO.ToString());

            for(int i = 0; i < FilePath.Count(); i++)
            {
                SaveDesignReportBlob(FilePath[i], RowName[i], idColumnNameCopy, idColumnCodeCopy);
            }
        }

        public static string getStringFromRegistry(string name)
        {
            
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey("Software\\SH\\HOUSE\\"))
                {
                    if (key != null)
                    {
                        Object o = key.GetValue(name);

                        if (o != null)
                        {
                            return o as string;                                                               //do what you like with version
                        }
                    }

                }
            }
            catch (Exception)  //just for demonstration...it's always best to handle specific exceptions
            {
                return "";
            }

            return "";
        }

        public static string getAddressFromServer(List<string> idColumnName, List<string> idColumnCode)
        {
            List<string> landCodeSet = new List<string>();
            string address = "";

            const string connectionString = "Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=oracle3.ejudata.co.kr)(PORT=1521))(CONNECT_DATA=(SERVER=DEDICATED)(SID = oracle3)));User Id=whruser_sh;Password=ejudata;";

            string sql = "select * FROM TN_PREV_ASSET";

            if (idColumnName.Count() != 0)
                sql += " WHERE";

            for (int i = 0; i < idColumnName.Count(); i++)
            {
                if (i != 0)
                    sql += " AND";

                sql += " " + idColumnName[i] + "=" + idColumnCode[i] + " ";
            }

            using (OracleConnection connection = new OracleConnection(connectionString))
            {
                try
                {
                    connection.Open();

                    OracleCommand comm = new OracleCommand(sql, connection);
                    OracleDataReader rs = comm.ExecuteReader();

                    while (rs.Read())
                    {
                        landCodeSet.Add(rs["LAND_CD"].ToString());
                    }

                    string sqlToGetAddress = "select * FROM TN_LAW_LAND WHERE LAND_CD=" + landCodeSet[0];

                    OracleCommand commToGetAddress = new OracleCommand(sqlToGetAddress, connection);
                    OracleDataReader readerToGetAddress = commToGetAddress.ExecuteReader();

                    while (readerToGetAddress.Read())
                    {
                        address += readerToGetAddress["LAND_SIDO_NM"].ToString();
                        address += " " + readerToGetAddress["LAND_SIGUNGU_NM"].ToString();
                        address += " " + readerToGetAddress["LAND_DONG_NM"].ToString();

                        if (readerToGetAddress["LAND_RI_NM"] != null)
                            address += " " + readerToGetAddress["LAND_RI_NM"].ToString();
                    }

                    //address += " " + rs["BNBUN"].ToString() + "-" + rs["BUBUN"].ToString() + "번지";

                    rs.Close();
                    readerToGetAddress.Close();
                }
                catch (OracleException ex)
                {
                    MessageBox.Show(ex.ToString());
                }

                connection.Close();
            }

            return address;
        }

        public static List<string> getStringFromServer(string dataToGet, string tableName, List<string> idColumnName, List<string> idColumnCode)
        {
            List<string> output = new List<string>();

            const string connectionString = "Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=oracle3.ejudata.co.kr)(PORT=1521))(CONNECT_DATA=(SERVER=DEDICATED)(SID = oracle3)));User Id=whruser_sh;Password=ejudata;";

            string sql = "select * FROM " + tableName;

            if (idColumnName.Count() != 0)
                sql += " WHERE";

            for (int i = 0; i < idColumnName.Count(); i++)
            {
                if (i != 0)
                    sql += " AND";

                sql += " " + idColumnName[i] + "=" + idColumnCode[i] + " ";
            }

            using (OracleConnection connection = new OracleConnection(connectionString))
            {
                try
                {
                    connection.Open();

                    OracleCommand comm = new OracleCommand(sql, connection);
                    OracleDataReader rs = comm.ExecuteReader();

                    while (rs.Read())
                    {
                        output.Add(rs[dataToGet].ToString());
                    }

                    rs.Close();
                }
                catch (OracleException)
                {
                }
            }

            return output;
        }

        private static double vectorAngle(Vector3d vector)
        {
            double sin = vector.Y / vector.Length;
            double cos = vector.X / vector.Length;

            if (sin > 0)
                return Math.Acos(cos);
            else
                return Math.PI - Math.Acos(cos);
        }

        public static Curve adjustOrientation(Curve crv)
        {
            if ((int)crv.ClosedCurveOrientation(new Vector3d(0, 0, 1)) == -1)
            {
                crv.Reverse();
            }
            return crv;
        }

        public static Curve[] JoinRegulations(Curve[] first, Curve[] second, Curve[] third)
        {
            List<Curve> result1 = new List<Curve>();
            for (int i = 0; i < first.Length; i++)
            {
                for (int j = 0; j < second.Length; j++)
                {
                    var intersections = Curve.CreateBooleanIntersection(first[i], second[j]);
                    if(intersections.Length>0)
                        result1.AddRange(intersections);
                }
            }

            List<Curve> result2 = new List<Curve>();
            for (int i = 0; i < result1.Count; i++)
            {
                for (int j = 0; j < third.Length; j++)
                {
                    var intersections = Curve.CreateBooleanIntersection(result1[i], third[j]);
                    if (intersections.Length > 0)
                        result2.AddRange(intersections);
                }
            }

            if (result2.Count == 1)
            {
                return result2.ToArray() ;
            }

            return Curve.CreateBooleanUnion(result2);

        }

        public static Curve[] JoinRegulation(Curve[] first, Curve[] second)
        {
            List<Curve> result1 = new List<Curve>();
            for (int i = 0; i < first.Length; i++)
            {
                for (int j = 0; j < second.Length; j++)
                {
                    var intersections = Curve.CreateBooleanIntersection(first[i], second[j]);
                    if (intersections.Length > 0)
                        result1.AddRange(intersections);
                }
            }

            return Curve.CreateBooleanUnion(result1);
        }

        public static Curve joinRegulations(Curve firstCurve, Curve secondCurve)
        {
            var intersections = Rhino.Geometry.Intersect.Intersection.CurveCurve(firstCurve, secondCurve, 0, 0);

            List<double> firstShatterDomain = new List<double>();
            List<double> secondShatterDomain = new List<double>();

            foreach (Rhino.Geometry.Intersect.IntersectionEvent i in intersections)
            {
                firstShatterDomain.Add(i.ParameterA);
                secondShatterDomain.Add(i.ParameterB);
            }

            Curve[] shatteredFirst = firstCurve.Split(firstShatterDomain);
            Curve[] shatteredSecond = secondCurve.Split(secondShatterDomain);

            List<Curve> usableCrvs = new List<Curve>();

            foreach (Curve i in shatteredFirst)
            {
                if (secondCurve.Contains(i.PointAt(i.Domain.Mid)) != PointContainment.Outside)
                {
                    usableCrvs.Add(i);
                }
            }

            foreach (Curve i in shatteredSecond)
            {
                if (firstCurve.Contains(i.PointAt(i.Domain.Mid)) == PointContainment.Inside)
                {
                    usableCrvs.Add(i);
                }
            }

            Curve combinationCurve;
            if (Curve.JoinCurves(usableCrvs).Length != 0)
                combinationCurve = Curve.JoinCurves(usableCrvs)[0];
            else
            {
                combinationCurve = firstCurve;
            }

            return combinationCurve;
        }

        public class colorSetting
        {
            public static System.Drawing.Color GetColorFromValue(int i)
            {
                if (i == 21)
                {
                    return System.Drawing.Color.Gray;
                }

                int myInt1 = (i <= 10) ? 25 * i : 255;
                int myInt2 = (i >= 10) ? 25 * (20 - i) : 255;
                System.Drawing.Color tempColor = System.Drawing.Color.FromArgb(myInt2, myInt1, 0);

                return tempColor;
            }
        }

        private static double parametersetStoriesChange(double existingStory, double newStory)
        {
            if (existingStory < newStory)
                return existingStory;
            else
                return newStory;
        }

    }

}


