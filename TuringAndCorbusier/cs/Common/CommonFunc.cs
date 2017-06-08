using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rhino.Geometry;
using System.Windows.Forms;
using Microsoft.Win32;
using System.IO.Ports;
using Oracle.ManagedDataAccess.Client;
using Rhino.Collections;
using GISData.DataStruct;

using TuringAndCorbusier.Utility;

namespace TuringAndCorbusier
{

    class CommonFunc
    {
        

        public static List<Curve> NewJoin(IEnumerable<Curve> spl)
        {
            Queue<Curve> q = new Queue<Curve>(spl);
            Stack<Curve> s = new Stack<Curve>();
            List<Curve> f = new List<Curve>();
            while (q.Count > 0)
            {
                Curve temp = q.Dequeue();

                if (temp.IsClosable(0))
                {
                    temp.MakeClosed(0);
                    f.Add(temp);
                }

                else
                {
                    if (s.Count > 0)
                    {
                        Curve pop = s.Pop();
                        var joined = Curve.JoinCurves(new Curve[] { pop, temp });
                        if (joined[0].IsClosable(0))
                        {
                            joined[0].MakeClosed(0);
                            f.Add(joined[0]);
                        }
                        else
                        {
                            s.Push(pop);
                            s.Push(temp);
                        }
                    }
                    else
                    {
                        s.Push(temp);
                    }
                }
            }

            if (s.Count > 0)
            {
                var last = Curve.JoinCurves(s);
                f.AddRange(last);
            }
            return f;
        }
        public static List<Curve> JoinRegulation(Plot plot, double stories, Apartment output)
        {
            Regulation reg = new Regulation(stories);
            var result = JoinRegulations(reg.byLightingCurve(plot,output.ParameterSet.Parameters[3]), reg.fromNorthCurve(plot), reg.fromSurroundingsCurve(plot));
            return result.ToList();
        }
        public static class LawLineDrawer
        {
            public static List<Curve> Boundary(Plot plot, double stories, bool using1F)
            {
                List<Curve> result = new List<Curve>();

                double heightBase = Consts.PilotiHeight;

                if (using1F)
                    heightBase = 0;

                for (int i = 0; i <= stories; i++)
                {
                    double storyheight = heightBase + Consts.FloorHeight * i;
                    Curve crv = plot.Boundary.DuplicateCurve();   //angle radian
                    crv.Translate(Vector3d.ZAxis * storyheight);
                    result.Add(crv);
                }

                return result;
            }

            public static List<Curve> Lighting(Plot plot, double stories, Apartment output, bool using1f)
            {
                double height = output.Household.Count * Consts.FloorHeight;
                double posz = height + Consts.PilotiHeight;
                if (using1f)
                    posz = height;

                double k = TuringAndCorbusierPlugIn.InstanceClass.regSettings.DistanceLighting;
                k = output.Household.Count > TuringAndCorbusierPlugIn.InstanceClass.regSettings.EaseFloor ? 0.5 : k;  //0.5 = 기본, 변수화해야.
                double distance = height * k;

                List<Curve> result = new List<Curve>();

                List<List<Household>> baseFloorHouses = output.Household[output.Household.Count-2];

                for (int i = 0; i < baseFloorHouses.Count; i++)
                {
                    for (int j = 0; j < baseFloorHouses[i].Count; j++)
                    {
                        var outline = baseFloorHouses[i][j].GetOutline();
                        for (int l = 0; l < baseFloorHouses[i][j].LightingEdge.Count; l++)
                        {
                            Line tempLighting = baseFloorHouses[i][j].LightingEdge[l];

                            Curve ligtingBox = DrawLightingBox(tempLighting, outline, distance);
                            result.Add(ligtingBox);
                        }
                    }
                }

                Regulation reg = new Regulation(0);
                Curve roadcenter = reg.RoadCenterLines(plot);
                result.Add(roadcenter);

                return result;
            }


            public static List<Curve> North(Plot plot, double stories, bool using1f)
            {
                var boundary = plot.SimplifiedBoundary;
                var surr = plot.SimplifiedSurroundings;

                List<Curve> result = new List<Curve>();

                for (int i = 0; i <= stories; i++)
                {
                    Regulation reg = new Regulation(i,using1f);

                        var tempfloornorth = reg.fromNorthCurve(plot);
                        foreach (var n in tempfloornorth)
                            n.Transform(Transform.Translation(Vector3d.ZAxis * reg.height));
                        result.AddRange(tempfloornorth);
                }
                return result;
            }

            public static List<Curve> NearPlot(Plot plot, double stories, bool using1f)
            {
                var boundary = plot.SimplifiedBoundary;
                var surr = plot.SimplifiedSurroundings;

                List<Curve> result = new List<Curve>();

                for (int i = 0; i <= stories; i++)
                {
                    Regulation reg = new Regulation(i,using1f);
       
                        var tempfloorsurr = reg.fromSurroundingsCurve(plot);
                        foreach (var n in tempfloorsurr)
                            n.Transform(Transform.Translation(Vector3d.ZAxis * reg.height));
                        result.AddRange(tempfloorsurr);
   
                }
                return result;
            }

            public static List<Curve> ApartDistance(Apartment output, out List<string> log)
            {
                List<Curve> crvs = new List<Curve>();
                List<string> dim = new List<string>();

                switch (output.AGtype)
                {
                    case "PT-1":
                        crvs.AddRange(AG1AptDistance(output, ref dim));
                        break;

                    case "PT-3":
                        crvs.AddRange(AG3AptDistance(output, ref dim));
                        break;

                    case "PT-4":
                        crvs.AddRange(AG4AptDistance(output, ref dim));
                        break;

                    default:
                        break;
                }

                log = dim;

                return crvs;

            }
           

            //sub
        
            private static Curve DrawLightingBox(Line lightingEdge, Curve outline, double distance)
            {

                Line currentLighting = lightingEdge;
                Point3d windowMidPt = currentLighting.From + currentLighting.Direction / 2;

             
                Vector3d algin = currentLighting.UnitTangent;
                Vector3d perp = currentLighting.UnitTangent;
                perp.Rotate(Math.PI / 2, Vector3d.ZAxis);

                Point3d testPt = windowMidPt + perp * 10;


                if (outline.Contains(testPt) == PointContainment.Inside)
                    perp = -perp;

                Point3d p1 = currentLighting.From;
                Point3d p2 = p1 + algin * currentLighting.Length;
                Point3d p3 = p2 + perp * distance;
                Point3d p4 = p1 + perp * distance;

                Polyline lightingBox = new Polyline { p1, p2, p3, p4, p1 };
                Curve ligtingCurve = lightingBox.ToNurbsCurve();

                return ligtingCurve;
            }

            private static List<Curve> AG1AptDistance(Apartment output, ref List<string> dim)
            {
                List<Curve> result = new List<Curve>();
                foreach (var aptline in output.AptLines)
                {
                    
                    if (output.AptLines.IndexOf(aptline) == 0)
                        continue;
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
                    double pilotiHeight = output.ParameterSet.using1F ? 0 : Consts.PilotiHeight;
                    var d2 = output.Household.Last()[0][0].Origin.Z + Consts.FloorHeight - pilotiHeight;
                    //height(except pil)

                    var d3 = d2 * TuringAndCorbusierPlugIn.InstanceClass.regSettings.DistanceIndentation;
                    front2.Translate(v * d3);
                    back2.Translate(v * d3 * -1);

                    var centerline = new LineCurve(front.PointAtNormalizedLength(0.5), front2.PointAtNormalizedLength(0.5));
                    var anothercenterline = new LineCurve(back.PointAtNormalizedLength(0.5), back2.PointAtNormalizedLength(0.5));

                    centerline.Translate(Vector3d.ZAxis * d2);
                    anothercenterline.Translate(Vector3d.ZAxis * d2);

                    result.Add(centerline);
                    //result.Add(anothercenterline);

                    string log = string.Format("h = {0} m, h * {2} = {1} m ", Math.Round(d2) / 1000, Math.Round(d3) / 1000, TuringAndCorbusierPlugIn.InstanceClass.regSettings.DistanceIndentation);
                    dim.Add(log);
                }

                return result;
            }

            private static List<Curve> AG3AptDistance(Apartment output, ref List<string> dim)
            {
                /*중정형은 짧은 쪽이 아파트라인 인덱스 0번입니다*/

                List<Curve> result = new List<Curve>();

                //seives
                if (output.AptLines == null || output.AptLines.Count == 0) //seive: null
                    return new List<Curve>();

                Curve rect = output.AptLines[0];
                double width = output.ParameterSet.Parameters[2];
                var offset = rect.Offset(Plane.WorldXY, width / 2, 1, CurveOffsetCornerStyle.Sharp);

                if (offset == null || offset.Length == 0) //seive: cannot offset
                    return new List<Curve>();


                //initial setting
                Curve innerRect = offset[0];

                List<Core> baseFloorCore = output.Core[output.Core.Count - 3];
                int coreCount = baseFloorCore.Count;
                double baseHeight = baseFloorCore.First().Origin.Z;
                innerRect.Translate(Vector3d.ZAxis * (baseHeight - innerRect.PointAtStart.Z));

                Polyline innerRectPoly = CurveTools.ToPolyline(innerRect); 

                if (output.ParameterSet.fixedCoreType == CoreType.Folded)
                {
                    Core dimBaseCore = baseFloorCore.First();

                    Point3d dimOriginBase = dimBaseCore.Origin + 
                        dimBaseCore.XDirection * dimBaseCore.Width / 2 + dimBaseCore.YDirection * dimBaseCore.Depth/2;
                    Point3d dimOrigin1 = dimOriginBase + dimBaseCore.XDirection * dimBaseCore.Width / 2;
                    Point3d dimOrigin2 = dimOriginBase + dimBaseCore.YDirection * dimBaseCore.Depth / 2;

                    if (coreCount <= 2)
                    {
                       
                        //add short
                        Line dimLine1 = PCXTools.PCXByEquation(dimOrigin1, innerRectPoly, dimBaseCore.XDirection);
                        Line dimLine1sub = PCXTools.PCXByEquation(dimOrigin1, innerRectPoly, -dimBaseCore.XDirection);

                        if (dimLine1.Length > 0.5)
                        {
                            dim.Add(dimLine1.Length.ToString());
                            result.Add(dimLine1.ToNurbsCurve());
                        }

                        else
                        {
                            dim.Add(dimLine1sub.Length.ToString());
                            result.Add(dimLine1sub.ToNurbsCurve());
                        }

                        //add long
                        Line dimLine2 = PCXTools.PCXByEquation(dimOrigin2, innerRectPoly, dimBaseCore.YDirection);
                        Line dimLine2sub = PCXTools.PCXByEquation(dimOrigin2, innerRectPoly, -dimBaseCore.YDirection);

                        if (dimLine2.Length > 0.5)
                        {
                            dim.Add(dimLine2.Length.ToString());
                            result.Add(dimLine2.ToNurbsCurve());
                        }

                        else
                        {
                            dim.Add(dimLine2sub.Length.ToString());
                            result.Add(dimLine2sub.ToNurbsCurve());
                        }

                        return result;
                    }
                

                    else if (coreCount > 2)
                    {
                        //add LW
                        Line dimLine1 = PCXTools.PCXByEquation(dimOrigin1, innerRectPoly, dimBaseCore.XDirection);
                        Line dimLine1sub = PCXTools.PCXByEquation(dimOrigin1, innerRectPoly, -dimBaseCore.XDirection);

                        if (dimLine1.Length > 0.5)
                        {
                            dim.Add(dimLine1.Length.ToString());
                            result.Add(dimLine1.ToNurbsCurve());
                        }

                        else
                        {
                            dim.Add(dimLine1sub.Length.ToString());
                            result.Add(dimLine1sub.ToNurbsCurve());
                        }

                        //add LL
                        Core firstEscapeCore = baseFloorCore[1];
                        Point3d escOrigin1 = firstEscapeCore.Origin +
                            firstEscapeCore.XDirection * firstEscapeCore.Width / 2 + firstEscapeCore.YDirection * firstEscapeCore.Depth;

                        Core lastEscapeCore = baseFloorCore.Last();
                        Point3d escOrigin2 = lastEscapeCore.Origin +
                         lastEscapeCore.XDirection * lastEscapeCore.Width / 2 + lastEscapeCore.YDirection * lastEscapeCore.Depth;

                        Vector3d escapeToEscape = escOrigin2 - escOrigin1;
                        double interEscapeLength = escapeToEscape * firstEscapeCore.YDirection;
                        Line dimLine2 = new Line(escOrigin1, escOrigin1 + firstEscapeCore.YDirection * interEscapeLength);

                        dim.Add(dimLine2.Length.ToString());
                        result.Add(dimLine2.ToNurbsCurve());

                        //add WW
                        Vector3d dimBaseToLastEscape = escOrigin2 - dimBaseCore.Origin;
                        double dimBaseToLastMid = dimBaseToLastEscape * dimBaseCore.YDirection;
                        Point3d onRectOrigin = dimBaseCore.Origin + dimBaseCore.YDirection * dimBaseToLastMid / 2;

                        Line dimLine3 = PCXTools.PCXStrict(onRectOrigin, innerRectPoly, dimBaseCore.XDirection);
                        Line dimLine3sub = PCXTools.PCXStrict(onRectOrigin, innerRectPoly, -dimBaseCore.XDirection);
                        if (dimLine3.Length > 0.5)
                        {
                            dim.Add(dimLine3.Length.ToString());
                            result.Add(dimLine3.ToNurbsCurve());
                        }
                        else
                        {
                            dim.Add(dimLine3sub.Length.ToString());
                            result.Add(dimLine3sub.ToNurbsCurve());
                        }

                        return result;
                    }

                    else
              
                        return result;
                }

          
                else if (output.ParameterSet.fixedCoreType == CoreType.CourtShortEdge)
                {
                    Core dimBaseCore = baseFloorCore.First();
                    bool dimBaseLong = dimBaseCore.Width > CoreType.CourtShortEdge.GetWidth();

                    Point3d dimOriginBase = dimBaseCore.Origin +
                        dimBaseCore.XDirection * dimBaseCore.Width / 2 + dimBaseCore.YDirection * dimBaseCore.Depth/2;

                    Point3d dimOrigin1 = dimOriginBase + dimBaseCore.XDirection * dimBaseCore.Width / 2;
                    Point3d dimOrigin2 = dimOriginBase + dimBaseCore.YDirection * dimBaseCore.Depth / 2;

                    if (coreCount == 2)
                    {
                        //LW
                        if (!dimBaseLong)
                        {
                            Line dimLine1 = PCXTools.PCXByEquation(dimOrigin1, innerRectPoly, dimBaseCore.XDirection);

                            if (dimLine1.Length > 0.5)
                            {
                                dim.Add(dimLine1.Length.ToString());
                                result.Add(dimLine1.ToNurbsCurve());
                            }
                        }

                        //LL
                        Core anotherCore = baseFloorCore[1];

                        Point3d anotherDimOrigin2 = anotherCore.Origin 
                            + anotherCore.YDirection * anotherCore.Depth + anotherCore.XDirection * anotherCore.Width / 2;

                        Vector3d coreToCore = anotherDimOrigin2 - dimOrigin2;
                        double interCore = coreToCore * dimBaseCore.YDirection;

                        Line dimLine2 = new Line(dimOrigin2, dimOrigin2 + dimBaseCore.YDirection * interCore);
                        dim.Add(dimLine2.Length.ToString());
                        result.Add(dimLine2.ToNurbsCurve());

                        //WW
                        Point3d onRectOrigin = innerRectPoly.SegmentAt(1).PointAt(0.5);
                        Line dimLine3 = PCXTools.PCXStrict(onRectOrigin, innerRectPoly, -innerRectPoly.SegmentAt(0).UnitTangent);
                        Line dimLine3sub = PCXTools.PCXStrict(onRectOrigin, innerRectPoly, innerRectPoly.SegmentAt(0).UnitTangent);
                        if (dimLine3.Length >0.5)
                        {
                            dim.Add(dimLine3.Length.ToString());
                            result.Add(dimLine3.ToNurbsCurve());
                        }
                        else
                        {
                            dim.Add(dimLine3sub.Length.ToString());
                            result.Add(dimLine3sub.ToNurbsCurve());
                        }
                        return result;
                    }

                    else if (coreCount > 2)
                    {
                        //add LW
                        if (!dimBaseLong)
                        {
                            Line dimLine1 = PCXTools.PCXByEquation(dimOrigin1, innerRectPoly, dimBaseCore.XDirection);

                            if (dimLine1.Length > 0.5)
                            {
                                dim.Add(dimLine1.Length.ToString());
                                result.Add(dimLine1.ToNurbsCurve());
                            }
                        }

                        //add WW
                        Core firstEscapeCore = baseFloorCore[1];
                        Point3d escOrigin1 = firstEscapeCore.Origin +
                            firstEscapeCore.XDirection * firstEscapeCore.Width / 2 + firstEscapeCore.YDirection * firstEscapeCore.Depth;

                        Core lastEscapeCore = baseFloorCore.Last();
                        Point3d escOrigin2 = lastEscapeCore.Origin +
                         lastEscapeCore.XDirection * lastEscapeCore.Width / 2 + lastEscapeCore.YDirection * lastEscapeCore.Depth;

                        Vector3d escapeToEscape = escOrigin2 - escOrigin1;
                        double interEscapeLength = escapeToEscape * firstEscapeCore.YDirection;
                        Line dimLine2 = new Line(escOrigin1, escOrigin1 + firstEscapeCore.YDirection * interEscapeLength);

                        dim.Add(dimLine2.Length.ToString());
                        result.Add(dimLine2.ToNurbsCurve());

                        //add LL
                        Vector3d dimBaseToLastEscape = escOrigin2 - dimBaseCore.Origin;
                        double dimBaseToLastMid = dimBaseToLastEscape * dimBaseCore.YDirection;
                        Point3d onRectOrigin = dimBaseCore.Origin 
                            + dimBaseCore.YDirection * dimBaseToLastMid / 2 -dimBaseCore.XDirection*Consts.corridorWidth;

                        Line dimLine3 = PCXTools.PCXStrict(onRectOrigin, innerRectPoly, dimBaseCore.XDirection);

                        dim.Add(dimLine3.Length.ToString());
                        result.Add(dimLine3.ToNurbsCurve());

                        return result;
                    }

                    else
                        return result;
                }
          
                return new List<Curve>();
            }


            private static List<Curve> AG4AptDistance(Apartment output, ref List<string> dim)
            {
                List<Curve> result = new List<Curve>();
                if (output.AptLines.Count == 0)
                    return new List<Curve>();
                Curve[] segments = output.AptLines[0].DuplicateSegments();
                if (segments.Length != 3)
                    return new List<Curve>();

                Vector3d seg1v = segments[0].TangentAtStart;
                Vector3d seg2v = segments[2].TangentAtStart;

                double width = output.ParameterSet.Parameters[2];

                if (Vector3d.VectorAngle(seg1v, seg2v) < 0.1)
                    return new List<Curve>();

                else
                {
                    Line a = new Line(segments[0].PointAtStart, segments[2].PointAtStart);
                    Line b = new Line(segments[2].PointAtEnd, segments[0].PointAtEnd);

                    double length = segments[1].GetLength() - width;
                    double paramA, paramB;
                    Rhino.Geometry.Intersect.Intersection.LineLine(a, b, out paramA, out paramB);
                    Point3d cross = a.PointAt(paramA);
                    double onSegments0;
                    segments[0].ClosestPoint(cross, out onSegments0);
                    Point3d onSeg0 = segments[0].PointAt(onSegments0);
                    Vector3d dir = cross - onSeg0;
                    dir.Unitize();
                    onSeg0 += dir * width / 2;
                    onSeg0.Z = output.Household.Last().First().First().Origin.Z;

                    LineCurve line = new LineCurve(onSeg0, onSeg0 + dir * length);
                    result.Add(line);
                    dim.Add(Math.Round(length).ToString());
                }

                return result;

            }

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

        public static string GetApartmentType(Apartment agOutput)
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

        public static double GetBuildingCoverage_outSideAGOutput(List<List<List<Household>>> household, List<List<Core>> core, Plot plot)
        {
            double ExclusiveAreaSum = 0;

            foreach (List<List<Household>> i in household)
            {

                foreach (Household k in i[0])
                {
                    ExclusiveAreaSum += k.GetExclusiveArea();
                }

            }

            foreach (List<Core> i in core)
            {
                foreach (Core j in i)
                {
                    ExclusiveAreaSum += j.GetArea();
                }
            }

            return ExclusiveAreaSum / plot.GetArea() * 100;
        }

        //public static double GetGrossArea_outSideAGOutput(List<List<List<Household>>> household, List<List<Core>> core, Plot plot)
        // {
        //     double ExclusiveAreaSum = 0;

        //     foreach (List<List<Household>> i in household)
        //     {
        //         foreach (List<Household> j in i)
        //         {
        //             foreach (Household k in j)
        //             {


        //                 ExclusiveAreaSum += k.GetExclusiveArea();
        //                 ExclusiveAreaSum += k.GetWallArea();


        //             }
        //         }
        //     }

        //     foreach (List<Core> i in core)
        //     {
        //         foreach (Core j in i)
        //         {
        //             double dkdk = j.GetArea();
        //             ExclusiveAreaSum += j.GetArea() * (j.Stories + 1);

        //         }
        //     }

        //     return ExclusiveAreaSum / plot.GetArea() * 100;
        // }

        //public static double GetGrossArea_outSideAGOutput(List<List<List<Household>>> household, List<List<Core>> core, List<Household> publicFacilitires, List<Household> commercialFacilities , Plot plot)
        //{
        //    double ExclusiveAreaSum = 0;

        //    foreach(List<List<Household>> i in household)
        //    {
        //        foreach(List<Household> j in i)
        //        {
        //            foreach(Household k in j)
        //            {
        //                ExclusiveAreaSum += k.GetExclusiveArea();
        //                ExclusiveAreaSum += k.GetWallArea();
        //            }
        //        }
        //    }

        //    foreach(Household i in publicFacilitires)
        //    {
        //        ExclusiveAreaSum += i.GetExclusiveArea();
        //    }

        //    foreach(Household i in commercialFacilities)
        //    {
        //        ExclusiveAreaSum += i.GetExclusiveArea();
        //    }

        //    foreach(List<Core> i in core)
        //    {
        //        foreach(Core j in i)
        //        {
        //            ExclusiveAreaSum += j.GetArea() * (j.Stories + 1);
        //        }
        //    }

        //    return ExclusiveAreaSum / plot.GetArea() * 100;
        //}

        public static int toplevel(List<List<Core>> core)
        {
            int result = 0;
            foreach (var i in core)
            {
                foreach (var j in i)
                {
                    if (j.Stories > result)
                        result = int.Parse(Math.Truncate(j.Stories).ToString());

                }
            }

            return result;
        }

        private static int householdPropertyCounter(List<List<List<Household>>> ObjectToCount)
        {
            int output = 0;

            foreach (List<List<Household>> i in ObjectToCount)
            {
                foreach (List<Household> j in i)
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
            List<Household> hhps = new List<Household>();
            public Floor(List<Household> hhps)
            {
                this.hhps = hhps;
            }
        }

        public static void reduceFloorAreaRatio_Mixref (ref List<List<List<Household>>> household_Original, ref List<List<Core>> core_Original, ref double currentFloorAreaRatio, Plot plot, double legalFloorAreaRatio, bool overload)
        {
            //기존 L<L<L<hhp>>> = 동(층(호)))

            //계산시 복도형 - 동/층/호 순서로.
            List<Dong> dongs = new List<Dong>();

            for (int i = 0; i < household_Original.Count; i++)
            {
                //동 하나씩
                var dong = household_Original[i];
                var dongcore = core_Original[i];

                var firstfloor = dong[0];
                var hocount = firstfloor.Count;

                if (dong.Count == 0 || dongcore.Count == 0)
                    continue;

                var temp = new Dong(dong, dongcore);
                dongs.Add(temp);
                
            }
            double tempfar = grossArea(household_Original, core_Original) / plot.GetArea() * 100;

            //WriteLog(household_Original);

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
               
                tempfar = grossArea(household_Original, core_Original) / plot.GetArea() * 100;
                //WriteLog(household_Original);
            }

            
        }

        public static void WriteLog(List<List<List<Household>>> hhps)
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
            List<List<Household>> units = new List<List<Household>>();
            List<Core> cores = new List<Core>();
            List<Household> dedicates = new List<Household>();
            List<Core> dedicatescore = new List<Core>();


            public Dong(List<List<Household>> hhps, List<Core> cps)
            {
                units = hhps;
                cores = cps;
            }

            public List<Household> FirstFloor { get { return units[0]; } }
            public int HoCount { get { return FirstFloor.Count; } }
            public int[] FloorsCount { get { return units.Select(n => n.Count).ToArray(); } }
            public List<Household> Dedicates { get { dedicates = GetDedicates(); return dedicates; } }
            public bool isCorridorType { get { return isCorridor(); } }
            public List<Household> LastFloor { get { return units.Last(); } }
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
            List<Household> GetDedicates()
            {

                List<Household> resultdedicates = new List<Household>();
                if (isCorridor())
                {   //3층 이하라면 안깎음
                    if (units.Count < 3 || cores.Count == 1)
                    {
                        return new List<Household>();
                    }
                    //3층 이상이면 깎음
                    else
                    {
                        //[첫 코어] [마지막 코어] 사이의 유닛 제외, 외곽유닛부터 ,
                        //사이의유닛구하기
                        List<Household> outside = new List<Household>();

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
                        return new List<Household>();
                    }//unitcount > 3
                   
                }//ifcorridor?
                else
                {
                    //min / max
                    List<List<Household>> byrow = new List<List<Household>>();
                    for (int i = 0; i < FirstFloor.Count; i++)
                    {
                        //제 1 열 
                        var rowtempX = Math.Round(FirstFloor[i].Origin.X);
                        var rowtempY = Math.Round(FirstFloor[i].Origin.Y);
                        List<Household> temprow = new List<Household>();
                        List<Household> wholehhps = new List<Household>();
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
                        return new List<Household>();

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
                return new List<Household>();
            }
            //List<Core> GetDedicores()
            //{
            //    shit!
            //}
        }
        public static void reduceFloorAreaRatio_Vertical(ref List<List<List<Household>>> household_Original, ref List<List<Core>> core_Original, ref double currentFloorAreaRatio, Plot plot, double legalFloorAreaRatio, bool overload)
        {
            //목표 줄일 면적

            //double areaToReduce = (currentFloorAreaRatio - legalFloorAreaRatio) / 100;
            //areaToReduce *= plot.GetArea();
            //double tempAreaToReduce = areaToReduce;
            //int floor = 0;
            //List<Household> namungo = new List<Household>();
            //List<Household> toRemove = new List<Household>();
            //while (tempAreaToReduce > plot.GetArea() / 100)
            //{
            //    floor++;
            //    if (household_Original.Max(n => n.Count) < floor)
            //        break;
            //    // 층 모든 유닛 모음
            //    List<Household> topfloorunits = new List<Household>();



            //    for (int i = 0; i < household_Original.Count; i++)
            //    {
            //        //해당 층 유닛 수 / 코어 수 = offsets...일단 좌우부터 ㄱ
            //        if (household_Original[i].Count - floor < 0)
            //            continue;
            //        var tempfloorunits = household_Original[i][household_Original[i].Count - floor];
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


            //    List<Household> notthese = new List<Household>();

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
            //    household_Original.ForEach(n => n.ForEach(m => m.Remove(p)));
            //}

            ////정렬



            ////동 별로
            ////for (int i = 0; i < household_Original.Count; i++)
            ////{
            ////    //현재저장된층
            ////    List<Household> tempfloorhhps = new List<Household>();

            ////    //동의 꼭대기층부터 순차적으로
            ////    for (int j = household_Original[i].Count - 1; j > 1; j--)
            ////    {
            ////        //처음이면
            ////        if (tempfloorhhps == null)
            ////        {
            ////            tempfloorhhps = new List<Household>(household_Original[i][j]);
            ////            household_Original[i][j].Clear();
            ////            continue;
            ////        }
            ////        //다 때려박음
            ////        //층 clear

            ////        //아니면
            ////        else
            ////        {
            ////            List<Household> nomore = new List<Household>();
            ////            //현재층 hhps마다
            ////            foreach (var temphhps in tempfloorhhps)
            ////            {
            ////                //아래층에 무엇인가 있는지 체크
            ////                foreach (var nexthhps in household_Original[i][j])
            ////                {
            ////                    if (temphhps.Origin.X == nexthhps.Origin.X && temphhps.Origin.Y == nexthhps.Origin.Y)
            ////                    {
            ////                        //아래층이 있는녀석 -> 현재에서 제거, 해당층리스트에 넣어줌
            ////                        household_Original[i][j + 1].Add(temphhps);
            ////                        nomore.Add(temphhps);

            ////                        break;
            ////                    }
            ////                }
            ////            }
            ////            nomore.ForEach(n => tempfloorhhps.Remove(n));
            ////        }

            ////        //아래층없는애들 하나씩 움직임
            ////        tempfloorhhps = tempfloorhhps.Select(n => new Household(n, Consts.FloorHeight)).ToList();
            ////        tempfloorhhps.AddRange(new List<Household>(household_Original[i][j]));

            ////    }
            ////}


            //for (int i = 0; i < household_Original.Count; i++)
            //{
            //    //동에 소속된 hhps
            //    var tempBuildinghhps = household_Original[i];

            //    //동에 소속된 core
            //    var tempBuildingcps = core_Original[i];

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
            List<List<List<Household>>> hhpsl = new List<List<List<Household>>>();
            double land = plot.GetArea();
            for (int i = 0; i < household_Original.Count; i++)
            {
                //건물별
                //층 수 * 열 수 리스트
                //층 수
                int floors = household_Original[i].Count;
                //열 수
                int rows = household_Original[i][floors - 1].Count;
                //열 수 * 층 수 크기의 배열 선언
                Household[,] hhps = new Household[rows, floors];
                for (int j = 0; j < floors; j++)
                {
                    for (int k = 0; k < rows; k++)
                    {
                        //열,층 칸에 층,열 넣음
                        household_Original[i][j][k].indexer = new int[] { k, j };
                        hhps[k, j] = household_Original[i][j][k];
                    }
                }
                List<List<Household>> tempfloor = new List<List<Household>>();
                for (int j = 0; j < rows; j++)
                {
                    List<Household> temprow = new List<Household>();
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
                if (hhpsl[buildingnum].Count / 2 != core_Original[buildingnum].Count && !ranonece)
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

                    //core_Original[buildingnum][rownum / 2].Stories = 0;


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
                    foreach (var z in household_Original[buildingnum])
                    {
                        if (z.Contains(hhp1))
                        {
                            z.Remove(hhp1);
                            break;
                        }
                    }
                    hhpsl[buildingnum][rownum].RemoveAt(hhpsl[buildingnum][rownum].Count - 1);
                    //var mindistance = core_Original[buildingnum].Select(n => n.Origin.DistanceTo(hhp1.Origin)).Min();
                    //var closesetcore = core_Original[buildingnum].Where(n => n.Origin.DistanceTo(hhp1.Origin) == mindistance).ToList();
                    //closesetcore[0].Stories--;

                    //tempratio -= closesetcore[0].GetArea() / land * 100;

                    if (hhpsl[buildingnum][rownum2].Count > 0)
                    {
                        var hhp2 = hhpsl[buildingnum][rownum2][hhpsl[buildingnum][rownum2].Count - 1];
                        foreach (var z in household_Original[buildingnum])
                        {
                            if (z.Contains(hhp2))
                            {
                                z.Remove(hhp2);
                                break;
                            }
                        }
                        hhpsl[buildingnum][rownum2].RemoveAt(hhpsl[buildingnum][rownum2].Count - 1);
                    }


                    for (int i = 0; i < household_Original.Count; i++)
                    {
                        //동에 소속된 hhps
                        var tempBuildinghhps = household_Original[i];

                        //동에 소속된 core
                        var tempBuildingcps = core_Original[i];

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
                                    var testpoint = cps.Origin + cps.XDirection * cps.Width / 2 + cps.YDirection * cps.Depth / 2 + Vector3d.ZAxis * hhps.EntrancePoint.Z;
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
                        tempratio = grossArea(household_Original, core_Original) * 100 / plot.GetArea();
                    }


                }

                

                if (tempratio <= legalFloorAreaRatio * 1.01)
                {
                    return;
                }
            }



        }

        public static double grossArea(List<List<List<Household>>> Household,List<List<Core>> Core)
        {
            double output = 0;

            for (int i = 0; i < Household.Count; i++)
            {
                for (int j = 0; j < Household[i].Count; j++)
                {
                    for (int k = 0; k < Household[i][j].Count; k++)
                    {

                        output += Household[i][j][k].GetExclusiveArea();

                        output += Household[i][j][k].GetWallArea();

                    }
                }

                foreach (Core j in Core[i])
                {
                    output = output + j.GetArea() * (j.Stories + 1);
                }

            }

            return output;
        }

        public static void reduceFloorAreaRatio_Vertical(ref List<List<List<Household>>> household_Original, ref List<List<Core>> core_Original, ref double currentFloorAreaRatio, Plot plot, double legalFloorAreaRatio)
        {
            double thisFloorAreaRatio = currentFloorAreaRatio;
            List<List<List<Household>>> household = new List<List<List<Household>>>(household_Original);
            List<List<Core>> core = new List<List<Core>>(core_Original);

            //////// 용적률이 너무 크면 제거

            List<List<int>> coreRanks = new List<List<int>>();

            //for (int i = 0; i < core_Original.Count(); i++)
            //{
            //    List<Core> otherCore = new List<Core>();

            //    for (int j = 0; j < core_Original.Count(); j++)
            //    {
            //        if (i != j)
            //            otherCore.AddRange(core_Original[j]);
            //    }

            //    coreRanks.Add(RankCoreDIstance(core_Original[i], otherCore));
            //}

            if (thisFloorAreaRatio > legalFloorAreaRatio)
            {
                coreRanks.Clear();


                for (int i = 0; i < core.Count(); i++)
                {
                    List<Core> otherCore = new List<Core>();

                    for (int j = 0; j < core.Count(); j++)
                    {
                        if (i != j)
                            otherCore.AddRange(core[j]);
                    }

                    coreRanks.Add(RankCoreDIstance(core[i], otherCore));
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
                        List<List<Household>> tempHousehold = new List<List<Household>>(household[tempApartmentIndex]);

                        double tempExpectedAreaRemove = 0;
                        List<int> tempRemoveHouseholdPropertyStoriesIndex = new List<int>();
                        List<int> tempRemoveHouseholdPropertyIndex = new List<int>();

                        List<Point3d> tempCoreOrigin = (from coreProperty in core[tempApartmentIndex]
                                                                  select coreProperty.Origin).ToList();

                        List<List<int>> tempClosestCorePropertyIndex = new List<List<int>>();

                        for (int i = 0; i < tempHousehold.Count(); i++)
                        {
                            tempClosestCorePropertyIndex.Add(new List<int>());

                            for (int j = 0; j < tempHousehold[i].Count(); j++)
                            {
                                Point3d tempHouseholdOrigin = tempHousehold[i][j].Origin;

                                RhinoList<double> tempDistance = new RhinoList<double>();
                                RhinoList<int> tempCorePropertyIndex = new RhinoList<int>();

                                for (int k = 0; k < tempCoreOrigin.Count(); k++)
                                {
                                    tempCorePropertyIndex.Add(k);
                                    tempDistance.Add(Math.Pow(Math.Pow(tempCoreOrigin[k].X - tempHouseholdOrigin.X, 2) + Math.Pow(tempCoreOrigin[k].Y - tempHouseholdOrigin.Y, 2), 0.5) + tempHouseholdOrigin.Z);
                                }

                                tempCorePropertyIndex.Sort(tempDistance.ToArray());
                                tempClosestCorePropertyIndex[i].Add(tempCorePropertyIndex[0]);
                            }
                        }

                        RhinoList<int> tempSelectedHouseholdIndex = new RhinoList<int>();
                        RhinoList<int> tempSelectedHouseholdFloor = new RhinoList<int>();

                        for (int i = 0; i < tempHousehold.Count(); i++)
                        {
                            for (int j = 0; j < tempHousehold[i].Count(); j++)
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
                        //    if (tempCoreIndex == core[tempApartmentIndex].Count)
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
                                    tempExpectedAreaRemove += household[tempApartmentIndex][i][selectedHouseholdIndexByFloor[i][j]].GetExclusiveArea();
                                    tempExpectedAreaRemove += household[tempApartmentIndex][i][selectedHouseholdIndexByFloor[i][j]].GetWallArea();

                                    tempRemoveHouseholdIndex.Add(selectedHouseholdIndexByFloor[i][j]);
                                    tempRemoveHosueholdFloor.Add(i);

                                    if (selectedHouseholdIndexByFloor[i][j] == minHouseholdIndexByFloor[i])
                                    {
                                        coreRemoveCount++;

                                        tempExpectedAreaRemove += core[tempApartmentIndex][tempCoreIndex].GetArea();
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
                                household[tempApartmentIndex][tempRemoveHosueholdFloor[i]].RemoveAt(tempRemoveHouseholdIndex[i]);
                            }

                            thisFloorAreaRatio -= tempExpectedAreaRemove / plot.GetArea() * 100;

                            if (coreRemoveCount != 0)
                            {
                                core[tempApartmentIndex][tempCoreIndex].Stories -= coreRemoveCount;
                                //thisFloorAreaRatio -= (core[tempApartmentIndex][tempCoreIndex].GetArea() * (coreRemoveCount)) / plot.GetArea() * 100;
                            }

                            if (tempRemoveHosueholdFloor.Count() == tempSelectedHouseholdFloor.Count())
                            {
                                thisFloorAreaRatio -= (core[tempApartmentIndex][tempCoreIndex].GetArea() * (core[tempApartmentIndex][tempCoreIndex].Stories + 1)) / plot.GetArea() * 100;

                                core[tempApartmentIndex].RemoveAt(tempCoreIndex);
                            }

                        }
                    }

                    tempApartmentIndex = (tempApartmentIndex + 1) % household.Count();

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

        public static List<int> RankCoreDIstance(List<Core> currentCores, List<Core> otherCores)
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
        public static void reduceFloorAreaRatio(ref List<List<List<Household>>> household_Original, ref List<List<Core>> core_Original, ref double currentFloorAreaRatio, Plot plot, double legalFloorAreaRatio)
        {
            double thisFloorAreaRatio = currentFloorAreaRatio;
            List<List<List<Household>>> household = new List<List<List<Household>>>(household_Original);
            List<List<Core>> core = new List<List<Core>>(core_Original);

            //////// 용적률이 너무 크면 제거

            if (thisFloorAreaRatio > legalFloorAreaRatio)
            {
                int tempApartmentIndex = 0;
                int tempStoriesFromTopIndex = 1;
                bool isSomeThingRemoved = false;

                while (thisFloorAreaRatio > legalFloorAreaRatio)
                {
                    int tempStoryIndex = household[tempApartmentIndex].Count() - tempStoriesFromTopIndex;

                    double tempExpectedAreaRemove = 0;
                    List<int> tempRemoveHouseholdPropertyIndex = new List<int>();
                    List<int> tempRemoveCorePropertyIndex = new List<int>();

                    if (tempStoryIndex < 0)
                    {
                        tempApartmentIndex = (tempApartmentIndex + 1) % household.Count();
                        continue;
                    }

                    int nextCorePropertyIndex = 0;

                    for (int i = 0; i < household[tempApartmentIndex][tempStoryIndex].Count(); i++)
                    {
                        if (thisFloorAreaRatio - tempExpectedAreaRemove / plot.GetArea() * 100 > legalFloorAreaRatio)
                        {
                            tempExpectedAreaRemove += household[tempApartmentIndex][tempStoryIndex][i].GetExclusiveArea();
                            tempExpectedAreaRemove += household[tempApartmentIndex][tempStoryIndex][i].GetWallArea();
                            tempRemoveHouseholdPropertyIndex.Add(i);

                            if (Math.Ceiling((double)household[tempApartmentIndex][tempStoryIndex].Count / (double)core[tempApartmentIndex].Count * (double)(nextCorePropertyIndex + 1))-1 == i)
                            {
                                tempExpectedAreaRemove += core[tempApartmentIndex][nextCorePropertyIndex].GetArea();
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
                            household[tempApartmentIndex][tempStoryIndex].RemoveAt(tempRemoveHouseholdPropertyIndex[i]);
                        }

                        for (int i = 0; i < tempRemoveCorePropertyIndex.Count(); i++)
                        {
                            Core tempCore = new Core(core[tempApartmentIndex][tempRemoveCorePropertyIndex[i]]);
                            tempCore.Stories = tempCore.Stories - 1;

                            core[tempApartmentIndex][tempRemoveCorePropertyIndex[i]] = tempCore;

                            if (core[tempApartmentIndex][tempRemoveCorePropertyIndex[i]].Stories == 0) ////////////////////////////////////////////////////////////
                            {
                                core[tempApartmentIndex].RemoveAt(tempRemoveCorePropertyIndex[i]);
                                tempExpectedAreaRemove += core[tempApartmentIndex][nextCorePropertyIndex].GetArea();
                            }
                        }

                        thisFloorAreaRatio -= tempExpectedAreaRemove / plot.GetArea() * 100;
                        isSomeThingRemoved = true;
                    }

                    tempApartmentIndex = (tempApartmentIndex + 1) % household.Count();

                    if (tempApartmentIndex == 0)
                    {
                        if (isSomeThingRemoved == false)
                            break;

                        tempStoriesFromTopIndex += 1;
                        isSomeThingRemoved = false;
                    }

                }

            }

            household_Original = household;
            core_Original = core;
            currentFloorAreaRatio = thisFloorAreaRatio;
        }
        public static void sortHHPOnEarth_OnlyCreateCommercial(ref RhinoList<Household> householdOnEarth, List<Curve> PlotArr, List<int> roadWidth)
        {
            List<Point3d> hhpOnEarthOrigin = (from hhp in householdOnEarth
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

            householdOnEarth.Sort(distanceFromClosestPlot.ToArray());
            tempRoadWidth.Sort(distanceFromClosestPlot.ToArray());

            householdOnEarth.Sort(tempRoadWidth.ToArray());

        }

        public static List<Household> createCommercialFacility(List<List<List<Household>>> household, List<Curve> apartmentBaseCurves, Plot plot, double buildingCoverageReamin, double grossAreaRatioRemain)
        {
            double grossAreaRemain = plot.GetArea() * grossAreaRatioRemain / 100;
            double buildingAreaRemain = plot.GetArea() * buildingCoverageReamin / 100;

            List<Household> output = new List<Household>();

            RhinoList<Household> hhpOnEarth = new RhinoList<Household>();

            for (int i = 0; i < household.Count; i++)
            {
                if (household[i].Count != 0)
                {
                    hhpOnEarth.AddRange(household[i][0]);
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
                        Household tempHousehold = new Household(hhpOnEarth[i]);

                        tempHousehold.Origin = new Point3d(tempHousehold.Origin.X, tempHousehold.Origin.Y, 0);

                        double LastHouseholdProperteis = tempHousehold.GetArea();

                        if (tempHousehold.YLengthB != 0)
                        {
                            tempHousehold.XLengthA = tempHousehold.XLengthA - tempHousehold.XLengthB;
                            tempHousehold.XLengthB = 0;
                        }

                        output.Add(tempHousehold);
                        grossAreaRemain -= tempHousehold.GetArea();
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




       



        /// <summary>
        /// //////////dmdfskdljflskdjfdgsdasfdasfdaolfjdlfjkdlsfdajsfkdl
        /// </summary>
        /// <param name="idColumnName"></param>
        /// <param name="idColumnCode"></param>
        /// <param name="path"></param>
        /// 


        /// <summary>
        /// 최초 등록
        /// </summary>
        /// <param name="idColumnName"></param>
        /// <param name="idColumnCode"></param>
        /// <param name="path"></param>


      


   
  

      

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
            //if (Curve.JoinCurves(usableCrvs).Length != 0)
            //    combinationCurve = Curve.JoinCurves(usableCrvs)[0];
            //else
            //{
            //    combinationCurve = firstCurve;
            //}
            var result = NewJoin(usableCrvs);
            if (result.Count == 0)
                combinationCurve = firstCurve;
            else
                combinationCurve = result[0];

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
                if (i > 19)
                    i = 19;
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


