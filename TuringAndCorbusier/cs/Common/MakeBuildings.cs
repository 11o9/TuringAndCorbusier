using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rhino.Geometry;
using System.Collections;
using TuringAndCorbusier.Utility;

namespace TuringAndCorbusier
{


    public class MakeBuildings
    {
        public static List<Brep> makeBuildings(Apartment agOut)
        {
            List<Brep> output = new List<Brep>();

            List<Household> hhps = new List<Household>();
            foreach (var hh in agOut.Household)
                foreach (var h in hh)
                    hhps.AddRange(h);

            List<Core> cps = new List<Core>();
            foreach (var h in agOut.Core)
                cps.AddRange(h);

            foreach (var hhp in hhps)
            {
                output.AddRange(DrawHouse(hhp));
            }
            foreach (var cp in cps)
            {
                output.Add(DrawCore(cp));
            }

            return output;
        }

        public static List<Brep> DrawHouse(Household hhp)
        {
            double height = Consts.FloorHeight;
            Curve outline = hhp.GetOutline();
            Polyline outPoly = CurveTools.ToPolyline(outline);            

            Brep x = Extrusion.Create(outline, height, true).ToBrep();

            double windowSide = 300;
            double windowLow = 300;
            double windowHeight = 2100;
            double windowDepth = 200;

            List<Brep> wins = new List<Brep>();

            for (int i = 0; i < 2; i++)
            {
                //Rhino.RhinoDoc.ActiveDoc.Objects.Add(hhp.LightingEdge[i].ToNurbsCurve());
                //var c = hhp.LightingEdge[i].ToNurbsCurve().Trim(CurveEnd.Both, 300);
                //var p1 = c.PointAtStart + Vector3d.ZAxis * 300;
                //var p2 = c.PointAtStart + Vector3d.ZAxis * 2400;
                //var p3 = c.PointAtEnd + Vector3d.ZAxis * 2400;
                //var p4 = c.PointAtEnd + Vector3d.ZAxis * 300;
                //Brep[] open = Brep.CreatePlanarBreps(new PolylineCurve(new Point3d[] { p1, p2, p3, p4, p1 }));
                Line tempLine = hhp.LightingEdge[i];

                Point3d midPoint = tempLine.PointAt(0.5);
                Vector3d windowVec = tempLine.UnitTangent;
                windowVec.Rotate(Math.PI / 2, Vector3d.ZAxis);
                midPoint.Transform(Transform.Translation(Vector3d.Multiply(windowVec, 10)));

                if (hhp.GetOutline().Contains(midPoint) == Rhino.Geometry.PointContainment.Inside)
                {
                    tempLine.Flip();
                }

                tempLine.Extend(-windowSide, -windowSide);

                Curve tempCurve = tempLine.ToNurbsCurve();
                tempCurve.Translate(Vector3d.Multiply(Vector3d.ZAxis, windowLow));
                Point3d pt1 = tempCurve.PointAtStart;
                Point3d pt2 = tempCurve.PointAtEnd;
                Point3d pt3 = tempCurve.PointAtEnd;
                pt3.Transform(Transform.Translation(Vector3d.Multiply(Vector3d.ZAxis, windowHeight)));
                Point3d pt4 = tempCurve.PointAtStart;
                pt4.Transform(Transform.Translation(Vector3d.Multiply(Vector3d.ZAxis, windowHeight)));
                Point3d pt5 = tempCurve.PointAtStart;
                List<Point3d> windowPoints = new List<Point3d>();
                windowPoints.Add(pt1);
                windowPoints.Add(pt2);
                windowPoints.Add(pt3);
                windowPoints.Add(pt4);
                windowPoints.Add(pt5);
                Polyline windowPolyline = new Polyline(windowPoints);
                Curve win = windowPolyline.ToNurbsCurve();


                Plane windowPlane;
                win.TryGetPlane(out windowPlane);
                Vector3d windowNormal = windowPlane.Normal;
                Curve windowCurve = win.Duplicate() as Curve;
                windowCurve.Translate(Vector3d.Multiply(windowNormal, windowDepth));
                Surface windowSurface = Surface.CreateExtrusion(windowCurve, Vector3d.Multiply(windowNormal, -windowDepth * 2));
                Brep windowBrep = windowSurface.ToBrep();
                Brep[] withHolesTemp = x.Split(windowBrep, 1);

                if (withHolesTemp.Length != 0)
                {
                    x = withHolesTemp[0];
                }

                Curve duplicatedWindowCurve = (win.Duplicate() as Curve);
                duplicatedWindowCurve.Transform(Transform.Translation(windowNormal * (windowDepth - 100)));

                Curve windowCurveBottom = duplicatedWindowCurve.DuplicateSegments()[0];
                Curve heightCurve = duplicatedWindowCurve.DuplicateSegments()[1];

                wins.AddRange(DrawWindowAll(windowCurveBottom, heightCurve.GetLength(), false));

                Curve[] tempLoftBase = { win, duplicatedWindowCurve };

                Curve pathCurve = new LineCurve(Point3d.Origin, new Point3d(windowNormal * (-windowDepth + 100)));
                wins.Add(Brep.JoinBreps(Brep.CreateFromLoft(tempLoftBase, Point3d.Unset, Point3d.Unset, LoftType.Normal, false), 0)[0]);



            }


            //1. 양쪽에서 300씩 , 위400 아래300 사각형.빵꾸. 안쪽으로 200
            //->창틀
            //2. 사방 30씩, 안쪽으로 100
            //->창문와꾸
            //3. 창 안쪽에서 20, 창문와꾸 30, 바깥쪽50 ,길이 1200 * 2 
            //4. ㅊ



            //Brep w1 = Brep.create  hhp.LightingEdge[0]
            wins.Add(x);
            return wins;

        }

            public static Brep DrawCore(Core hhp)
        {
            double height = hhp.Stories == 0 ? Consts.PilotiHeight : Consts.FloorHeight;
            Vector3d x = hhp.XDirection;
            Vector3d y = hhp.YDirection;
            double width = hhp.CoreType.GetWidth();
            double depth = hhp.CoreType.GetDepth();

            PolylineCurve plc = new PolylineCurve(new Point3d[] { hhp.Origin, hhp.Origin + x * width, hhp.Origin + x * width + y * depth, hhp.Origin + y * depth, hhp.Origin });

            if(plc.ClosedCurveOrientation(Plane.WorldXY) == CurveOrientation.CounterClockwise)
                plc.Reverse();
            return Extrusion.Create(plc, height, true).ToBrep();
            
        }

        public static List<List<Brep>> makeBuildings(Apartment agOut, bool drawComplexModeling)
        {
            //description//
            //first List<Brep> : each house outline wall
            //second List<Brep> : each core
            //third List<Brep> : whole window
            //fourth List<Brep> : each window detail
            List<List<Brep>> output = new List<List<Brep>>();

            //create core outlines
            List<List<Curve>> coreOutline = new List<List<Curve>>();
            for (int i = 0; i < agOut.Core.Count; i++)
            {
                List<Curve> coreOutlineTemp = new List<Curve>();
                for (int j = 0; j < agOut.Core[i].Count; j++)
                {
                    List<Point3d> outlinePoints = new List<Point3d>();

                    Point3d pt = new Point3d(agOut.Core[i][j].Origin);
                    Vector3d x = new Vector3d(agOut.Core[i][j].XDirection);
                    Vector3d y = new Vector3d(agOut.Core[i][j].YDirection);
                    double width = agOut.Core[i][j].CoreType.GetWidth();
                    double depth = agOut.Core[i][j].CoreType.GetDepth();

                    outlinePoints.Add(pt);
                    pt.Transform(Transform.Translation(Vector3d.Multiply(x, width)));
                    outlinePoints.Add(pt);
                    pt.Transform(Transform.Translation(Vector3d.Multiply(y, depth)));
                    outlinePoints.Add(pt);
                    pt.Transform(Transform.Translation(Vector3d.Multiply(x, -width)));
                    outlinePoints.Add(pt);
                    pt.Transform(Transform.Translation(Vector3d.Multiply(y, -depth)));
                    outlinePoints.Add(pt);

                    Polyline outlinePolyline = new Polyline(outlinePoints);
                    Curve outlineCurve = outlinePolyline.ToNurbsCurve();
                    coreOutlineTemp.Add(outlineCurve);
                }
                coreOutline.Add(coreOutlineTemp);
            }

            //create building outlines
            List<List<Curve>> buildingOutline = agOut.buildingOutline;

            //create house outlines
            List<List<List<Curve>>> houseOutline = new List<List<List<Curve>>>();

            for (int i = 0; i < agOut.Household.Count; i++)
            {
                List<List<Curve>> houseOutline_i = new List<List<Curve>>();
                for (int j = 0; j < agOut.Household[i].Count; j++)
                {
                    List<Curve> houseOutline_j = new List<Curve>();

                    for (int k = 0; k < agOut.Household[i][j].Count(); k++)
                    {
                        List<Point3d> outlinePoints = new List<Point3d>();
                        Point3d pt = new Point3d(agOut.Household[i][j][k].Origin);
                        Vector3d x = new Vector3d(agOut.Household[i][j][k].XDirection);
                        Vector3d y = new Vector3d(agOut.Household[i][j][k].YDirection);
                        double xa = agOut.Household[i][j][k].XLengthA;
                        double xb = agOut.Household[i][j][k].XLengthB;
                        double ya = agOut.Household[i][j][k].YLengthA;
                        double yb = agOut.Household[i][j][k].YLengthB;

                        outlinePoints.Add(pt);
                        pt.Transform(Transform.Translation(Vector3d.Multiply(y, yb)));
                        outlinePoints.Add(pt);
                        pt.Transform(Transform.Translation(Vector3d.Multiply(x, xa - xb)));
                        outlinePoints.Add(pt);
                        pt.Transform(Transform.Translation(Vector3d.Multiply(y, -ya)));
                        outlinePoints.Add(pt);
                        pt.Transform(Transform.Translation(Vector3d.Multiply(x, -xa)));
                        outlinePoints.Add(pt);
                        pt.Transform(Transform.Translation(Vector3d.Multiply(y, ya - yb)));
                        outlinePoints.Add(pt);

                        outlinePoints = Point3d.CullDuplicates(outlinePoints, 1).ToList();

                        pt.Transform(Transform.Translation(Vector3d.Multiply(x, xb)));
                        outlinePoints.Add(pt);

                        Polyline outlinePolyline = new Polyline(outlinePoints);
                        Curve outlineCurve = outlinePolyline.ToNurbsCurve();
                        houseOutline_j.Add(outlineCurve);
                    }

                    houseOutline_i.Add(houseOutline_j);
                }

                houseOutline.Add(houseOutline_i);
            }

            //create window lines
            List<List<List<List<Line>>>> windowLines = agOut.getLightingWindow();

            //constants
            double pilotiHeight = Consts.PilotiHeight;
            double storiesHeight = Consts.FloorHeight;
            int stories = (int)agOut.ParameterSet.Stories;

            double windowSide = 300;
            double windowLow = 300;
            double windowHeight = 2100;
            double windowDepth = 200;


            //draw

            //make each house brep
            List<Brep> houseBrep = new List<Brep>();
            for (int i = 0; i < houseOutline.Count; i++)
            {

                for (int j = 0; j < houseOutline[i].Count; j++)
                {

                    for (int k = 0; k < houseOutline[i][j].Count(); k++)
                    {

                        //make each house block
                        Surface tempSurface = Surface.CreateExtrusion(houseOutline[i][j][k], Vector3d.Multiply(Vector3d.ZAxis, storiesHeight));
                        Brep tempBrep = tempSurface.ToBrep().CapPlanarHoles(1);

                        //make windows
                        List<Curve> windows = new List<Curve>();
                        for (int l = 0; l < windowLines[i][j][k].Count; l++)
                        {
                            Line tempLine = windowLines[i][j][k][l];

                            Point3d midPoint = tempLine.PointAt(0.5);
                            Vector3d windowVec = tempLine.UnitTangent;
                            windowVec.Rotate(Math.PI / 2, Vector3d.ZAxis);
                            midPoint.Transform(Transform.Translation(Vector3d.Multiply(windowVec, 10)));

                            if (houseOutline[i][j][k].Contains(midPoint) == Rhino.Geometry.PointContainment.Inside)
                            {
                                tempLine.Flip();
                            }

                            tempLine.Extend(-windowSide, -windowSide);

                            Curve tempCurve = tempLine.ToNurbsCurve();
                            tempCurve.Translate(Vector3d.Multiply(Vector3d.ZAxis, windowLow));
                            Point3d pt1 = tempCurve.PointAtStart;
                            Point3d pt2 = tempCurve.PointAtEnd;
                            Point3d pt3 = tempCurve.PointAtEnd;
                            pt3.Transform(Transform.Translation(Vector3d.Multiply(Vector3d.ZAxis, windowHeight)));
                            Point3d pt4 = tempCurve.PointAtStart;
                            pt4.Transform(Transform.Translation(Vector3d.Multiply(Vector3d.ZAxis, windowHeight)));
                            Point3d pt5 = tempCurve.PointAtStart;
                            List<Point3d> windowPoints = new List<Point3d>();
                            windowPoints.Add(pt1);
                            windowPoints.Add(pt2);
                            windowPoints.Add(pt3);
                            windowPoints.Add(pt4);
                            windowPoints.Add(pt5);
                            Polyline windowPolyline = new Polyline(windowPoints);
                            windows.Add(windowPolyline.ToNurbsCurve());

                        }
                        //punch holes

                        Brep withHoles = tempBrep;

                        for (int l = 0; l < windows.Count; l++)
                        {
                            Plane windowPlane;
                            windows[l].TryGetPlane(out windowPlane);
                            Vector3d windowNormal = windowPlane.Normal;
                            Curve windowCurve = windows[l].Duplicate() as Curve;
                            windowCurve.Translate(Vector3d.Multiply(windowNormal, windowDepth));
                            Surface windowSurface = Surface.CreateExtrusion(windowCurve, Vector3d.Multiply(windowNormal, -windowDepth * 2));
                            Brep windowBrep = windowSurface.ToBrep();
                            Brep[] withHolesTemp = withHoles.Split(windowBrep, 1);

                            if (withHolesTemp.Length != 0)
                            {
                                withHoles = withHolesTemp[0];
                            }

                            Curve duplicatedWindowCurve = (windows[l].Duplicate() as Curve);
                            duplicatedWindowCurve.Transform(Transform.Translation(windowNormal * (windowDepth - 100)));

                            Curve windowCurveBottom = duplicatedWindowCurve.DuplicateSegments()[0];
                            Curve heightCurve = duplicatedWindowCurve.DuplicateSegments()[1];

                            houseBrep.AddRange(DrawWindowAll(windowCurveBottom, heightCurve.GetLength(), drawComplexModeling));

                            Curve[] tempLoftBase = { windows[l], duplicatedWindowCurve };

                            Curve pathCurve = new LineCurve(Point3d.Origin, new Point3d(windowNormal * (-windowDepth + 100)));
                            houseBrep.Add(Brep.JoinBreps(Brep.CreateFromLoft(tempLoftBase, Point3d.Unset, Point3d.Unset, LoftType.Normal, false), 0)[0]);
                        }

                        houseBrep.Add(withHoles);

                    }
                }
            }



            output.Add(houseBrep);

            //make each core brep
            List<List<Brep>> coreBrep = new List<List<Brep>>();
            for (int i = 0; i < coreOutline.Count; i++)
            {
                List<Brep> coreBrepTemp = new List<Brep>();
                for (int j = 0; j < coreOutline[i].Count; j++)
                {
                    Surface tempSurface = Surface.CreateExtrusion(coreOutline[i][j], Vector3d.Multiply(Vector3d.ZAxis, storiesHeight * (agOut.Core[i][j].Stories + 1) + pilotiHeight));

                    Brep tempBrep = tempSurface.ToBrep().CapPlanarHoles(1);
                    coreBrepTemp.Add(tempBrep);
                }
                coreBrep.Add(coreBrepTemp);
            }

            List<Brep> coreBrepOut = new List<Brep>();
            for (int i = 0; i < coreBrep.Count; i++)
            {
                for (int j = 0; j < coreBrep[i].Count; j++)
                {
                    coreBrepOut.Add(coreBrep[i][j]);
                }
            }
            output.Add(coreBrepOut);



            //make corridor

            if (agOut.AGtype == "PT-1")
            {

                List<Brep> corridorBrep = new List<Brep>();
                List<Curve> aptLines = agOut.AptLines;

                for (int i = 0; i < aptLines.Count; i++)
                {

                    try
                    {
                        if (agOut.Target.Area[agOut.Household[i][0][0].HouseholdSizeType] * 1000 * 1000 < Consts.AreaLimit)
                        {

                            for (int j = 0; j < agOut.Household[i].Count; j++)
                            {
                                double corridorLength = 0;
                                Point3d startPoint = agOut.Household[i][j][0].Origin + agOut.Household[i][j][0].XDirection * agOut.Household[i][j][0].XLengthA;
                                for (int k = 0; k < agOut.Household[i][j].Count; k++)
                                {
                                    corridorLength += agOut.Household[i][j][k].XLengthA;
                                }

                                Vector3d tangentVec = aptLines[i].TangentAtStart;
                                Vector3d verticalVec = new Vector3d(tangentVec);
                                verticalVec.Rotate(Math.PI / 2, Vector3d.ZAxis);
                                double width = agOut.ParameterSet.Parameters[2];

                                Point3d pt2 = new Point3d(startPoint.X,startPoint.Y,0) + verticalVec * Consts.corridorWidth;
                                //pt2.Transform(Transform.Translation(Vector3d.Multiply(verticalVec, width / 2)));
                                Point3d pt1 = new Point3d(pt2);
                                pt1.Transform(Transform.Translation(Vector3d.Multiply(verticalVec, -Consts.corridorWidth)));
                                Point3d pt4 = new Point3d(pt2);
                                pt4.Transform(Transform.Translation(Vector3d.Multiply(tangentVec, corridorLength)));
                                Point3d pt3 = new Point3d(pt4);
                                pt3.Transform(Transform.Translation(Vector3d.Multiply(verticalVec, -Consts.corridorWidth)));

                                List<Point3d> corridorWallPts = new List<Point3d>();
                                corridorWallPts.Add(pt1);
                                corridorWallPts.Add(pt2);
                                corridorWallPts.Add(pt4);
                                corridorWallPts.Add(pt3);
                                Curve corridorCurve = new Polyline(corridorWallPts).ToNurbsCurve();
                                Surface corridorWallSurface = Surface.CreateExtrusion(corridorCurve, Vector3d.Multiply(Vector3d.ZAxis, Consts.corridorWallHeight));
                                Brep corridorWallBrep = corridorWallSurface.ToBrep();
                                Line lineTemp = new Line(aptLines[i].PointAtStart, aptLines[i].PointAtEnd);
                                //lineTemp.Transform(Transform.Translation(verticalVec * width / 2));
                                Line corridorFloorline = new Line(pt2,pt4);

                                Brep corridorFloorBrep = Surface.CreateExtrusion(corridorFloorline.ToNurbsCurve(), -verticalVec * Consts.corridorWidth).ToBrep();

                                corridorWallBrep.Translate(Vector3d.Multiply(Vector3d.ZAxis, agOut.Household[i][j][0].Origin.Z));
                                corridorFloorBrep.Translate(Vector3d.Multiply(Vector3d.ZAxis, agOut.Household[i][j][0].Origin.Z));
                                corridorBrep.Add(corridorWallBrep);
                                corridorBrep.Add(corridorFloorBrep);

                            }
                        }

                    }

                    catch (Exception)
                    {
                        continue;
                    } 
                }
                output.Add(corridorBrep);
            }



            return output;
        }



        private static List<Brep> DrawWindowAll(Curve baseCurve, double height, bool drawComplexModeling)
        {
            Curve tempCurve = new LineCurve(baseCurve.PointAt(baseCurve.Domain.T0), baseCurve.PointAt(baseCurve.Domain.T1));
            Plane basePlane = new Plane(baseCurve.PointAt(baseCurve.Domain.T0), new Vector3d(baseCurve.PointAt(baseCurve.Domain.T1) - baseCurve.PointAt(baseCurve.Domain.T0)), Vector3d.ZAxis);

            List<Brep> output = new List<Brep>();

            List<object> type;

            double[] target = { double.MaxValue, 2, 2 };
            List<Curve> shatteredBase = divideBaseCurve(tempCurve, target, out type);

            for (int i = 0; i < shatteredBase.Count(); i++)
            {
                List<string> windowTypeList = Enum.GetValues(typeof(WindowType)).Cast<WindowType>().Select(v => v.ToString()).ToList();

                if (windowTypeList.IndexOf(type[i].ToString()) != -1)
                {
                    int index = windowTypeList.IndexOf(type[i].ToString());

                    List<Brep> tempWindow = drawWindow((WindowType)index, shatteredBase[i], height);

                    output.AddRange(tempWindow);

                    if (drawComplexModeling)
                    {
                        Curve banisterBase = (shatteredBase[i].Duplicate() as Curve);
                        banisterBase.Transform(Transform.Translation(basePlane.ZAxis * -50));

                        List<Brep> tempBanister = drawBanister(banisterBase, 25, 900);

                        output.AddRange(tempBanister);
                    }
                }
                else
                {
                    Curve tempWallBase = shatteredBase[i].Duplicate() as Curve;
                    Plane tempPlane = new Plane(tempWallBase.PointAt(tempWallBase.Domain.T0), new Vector3d(tempWallBase.PointAt(tempWallBase.Domain.T1) - tempWallBase.PointAt(tempWallBase.Domain.T0)), Vector3d.ZAxis);
                    tempWallBase.Transform(Transform.Translation(tempPlane.ZAxis * -50));

                    Brep tempWall = drawWall(tempWallBase, height, 150);

                    output.Add(tempWall);
                }

            }

            return output;
        }

        private static List<Brep> drawBanister(Curve baseCurve, double pipeWidth, double banisterHeight)
        {
            List<Brep> output = new List<Brep>();

            double divideLength = baseCurve.GetLength() / ((int)(baseCurve.GetLength() / 200));

            double[] divideByLengthParam = baseCurve.DivideByLength(divideLength, false);

            for (int i = 0; i < divideByLengthParam.Length; i++)
            {
                Point3d tempPoint = baseCurve.PointAt(divideByLengthParam[i]);

                Curve tempCenterCurve = new LineCurve(tempPoint, tempPoint + Vector3d.ZAxis * (banisterHeight));

                output.Add(drawPipe(tempCenterCurve, pipeWidth));
            }

            baseCurve.Transform(Transform.Translation(Vector3d.ZAxis * (banisterHeight - pipeWidth / 2)));

            output.Add(drawPipe(baseCurve, pipeWidth));

            return output;
        }

        private static Brep drawPipe(Curve baseCurve, double pipeWidth)
        {
            Plane tempPlane = new Plane(baseCurve.PointAt(baseCurve.Domain.T0), new Vector3d(baseCurve.PointAt(baseCurve.Domain.T1) - baseCurve.PointAt(baseCurve.Domain.T0)));

            Curve offsettedCurve1 = baseCurve.Duplicate() as Curve;
            Curve offsettedCurve2 = baseCurve.Duplicate() as Curve;

            offsettedCurve1.Transform(Transform.Translation(tempPlane.XAxis * pipeWidth / 2));
            offsettedCurve2.Transform(Transform.Translation(tempPlane.XAxis * pipeWidth / -2));

            Curve[] loftBase = { offsettedCurve1, offsettedCurve2 };

            Brep loft = Brep.CreateFromLoft(loftBase, Point3d.Unset, Point3d.Unset, LoftType.Normal, false)[0];

            loft.Transform(Transform.Translation(tempPlane.YAxis * pipeWidth / 2));

            Curve pathCurve = new LineCurve(Point3d.Origin, new Point3d(tempPlane.YAxis * pipeWidth));

            return loft.Faces[0].CreateExtrusion(pathCurve, true);
        }

        private static List<Curve> divideBaseCurve(Curve baseCurve, double[] target, out List<object> type)
        {
            List<double> outputParameter = new List<double>();
            List<object> outputType = new List<object>();

            double length = baseCurve.GetLength();
            double currentPos = 0;
            double[] minLength = { 2000, 2400, 5000 };
            double wallLength = 300;

            if (length < minLength[0])
            {
                outputParameter.Add(baseCurve.Domain.T1);
                outputType.Add((WindowType)0);
            }

            while (currentPos + minLength[0] < length)
            {
                double tempAdd = 0;
                double targetIndex = 0;

                for (int i = minLength.Length - 1; i >= 0; i--)
                {
                    if (target[i] != 0)
                    {
                        tempAdd = minLength[i];
                        target[i]--;
                        targetIndex = i;

                        break;
                    }
                }

                if (currentPos + tempAdd < length)
                {
                    currentPos = currentPos + tempAdd;

                    outputParameter.Add(currentPos);
                    outputType.Add((WindowType)targetIndex);

                    currentPos += wallLength;

                    outputParameter.Add(currentPos);
                    outputType.Add(WallType.wall);
                }
            }

            outputParameter.RemoveAt(outputParameter.Count() - 1);

            if (outputParameter.Count != 0)
            {
                double additionPerWindow = (length - outputParameter[outputParameter.Count() - 1]) / ((outputParameter.Count() + 1) / 2);

                for (int i = 0; i < outputParameter.Count(); i++)
                {
                    outputParameter[i] = outputParameter[i] + ((int)(i / 2) + 1) * additionPerWindow;
                }
            }

            List<Curve> output = baseCurve.Split(outputParameter).ToList();
            type = outputType;

            return output;
        }

        private static Brep drawWall(Curve baseCurve, double height, double wallDepth)
        {
            Vector3d tempPlusVector = new Vector3d(baseCurve.PointAt(1) - baseCurve.PointAt(0));
            tempPlusVector = tempPlusVector / tempPlusVector.Length;
            Plane tempPlane = new Plane(Point3d.Origin, tempPlusVector, Vector3d.ZAxis);
            Curve baseRectangle = offsetOneSide(baseCurve, height, Vector3d.ZAxis);
            Curve pathCurve = new LineCurve(Point3d.Origin, new Point3d(tempPlane.ZAxis * wallDepth));

            return Brep.CreatePlanarBreps(baseRectangle)[0].Faces[0].CreateExtrusion(pathCurve, true);
        }

        private static List<Brep> drawWindow(WindowType windowType, Curve baseCurve, double height)
        {
            List<Brep> output = new List<Brep>();
            double paneSize = 1200;
            double frameWidth = 30;

            //Get Frame
            Brep windowFrame = drawWindowFrame(baseCurve, height, 30, 100);

            //Get Pane
            double paneBaseStart;
            double paneBaseEnd;

            baseCurve.LengthParameter(frameWidth, out paneBaseStart);
            baseCurve.LengthParameter(baseCurve.GetLength() - frameWidth, out paneBaseEnd);

            Curve paneBase = new LineCurve(baseCurve.PointAt(paneBaseStart), baseCurve.PointAt(paneBaseEnd));
            paneBase.Transform(Transform.Translation(Vector3d.ZAxis * frameWidth));

            List<Brep> pane = drawPane(windowType, paneBase, height - frameWidth * 2, paneSize, 50, 30, 100);

            //Make Return

            output.AddRange(pane);
            output.Add(windowFrame);

            return output;
        }

        private static Brep drawWindowFrame(Curve baseCurve, double height, double frameWidth, double frameDepth)
        {
            List<Curve> curves = new List<Curve>();

            Vector3d curveVector = (new Vector3d(baseCurve.PointAt(1) - baseCurve.PointAt(0)));
            curveVector = curveVector / curveVector.Length;
            Plane tempPlane = new Plane(Point3d.Origin, curveVector, Vector3d.ZAxis);

            Curve boundaryCurve = offsetOneSide(baseCurve, height, Vector3d.ZAxis);

            curves.Add(boundaryCurve);
            curves.Add(offsetInside(boundaryCurve, frameWidth, tempPlane));

            Curve pathCurve = new LineCurve(tempPlane.Origin, tempPlane.Origin + new Point3d(tempPlane.ZAxis * frameDepth));

            return Brep.CreatePlanarBreps(curves)[0].Faces[0].CreateExtrusion(pathCurve, true);
        }

        private static List<Brep> drawPane(WindowType windowType, Curve baseCurve, double height, double paneSize, double paneFrameWidth, double paneFrameDepth, double windowFrameDepth)
        {
            List<Brep> output = new List<Brep>();

            Curve duplicatedCurve = baseCurve.Duplicate() as Curve;
            Vector3d curveVector = (new Vector3d(baseCurve.PointAt(1) - baseCurve.PointAt(0)));
            curveVector = curveVector / curveVector.Length;
            Plane tempPlane = new Plane(Point3d.Origin, curveVector, Vector3d.ZAxis);

            if ((int)windowType <= 2)
            {
                double StartDomain;
                baseCurve.LengthParameter(paneSize / 2, out StartDomain);
                double EndDomain;
                baseCurve.LengthParameter(baseCurve.GetLength() - paneSize / 2, out EndDomain);
                Line baseLine = new Line(baseCurve.PointAt(StartDomain), baseCurve.PointAt(EndDomain));

                List<Point3d> paneCenter = new List<Point3d>();

                for (int i = 0; i < (int)windowType; i++)
                    paneCenter.Add(baseLine.PointAt(i));

                foreach (Point3d i in paneCenter)
                {
                    Point3d tempPaneStart = new Point3d(i + new Point3d(-curveVector * paneSize * 0.5));
                    Point3d tempPaneEnd = new Point3d(i + new Point3d(curveVector * paneSize * 0.5));

                    double tempPaneStartDomain;
                    double tempPaneEndDomain;
                    duplicatedCurve.ClosestPoint(tempPaneStart, out tempPaneStartDomain);
                    duplicatedCurve.ClosestPoint(tempPaneEnd, out tempPaneEndDomain);

                    Curve tempPaneCurve = new LineCurve(tempPaneStart, tempPaneEnd);

                    Curve paneBoundary = offsetOneSide(tempPaneCurve, height, Vector3d.ZAxis);
                    paneBoundary.Transform(Transform.Translation(tempPlane.ZAxis * (windowFrameDepth / 2)));

                    Curve paneInside = offsetInside(paneBoundary, paneFrameWidth, tempPlane);

                    List<Curve> tempCurves = new List<Curve>();

                    tempCurves.Add(paneBoundary);
                    tempCurves.Add(paneInside);

                    Brep paneBaseBrep = Brep.CreatePlanarBreps(tempCurves)[0];

                    Curve tempPathCurve = new LineCurve(tempPlane.Origin, tempPlane.Origin + new Point3d(tempPlane.ZAxis * paneFrameDepth));

                    output.Add(paneBaseBrep.Faces[0].CreateExtrusion(tempPathCurve, true));

                    paneInside.Transform(Transform.Translation(tempPlane.ZAxis * paneFrameDepth / 2));
                    output.Add(Brep.CreatePlanarBreps(paneInside)[0]);

                    duplicatedCurve = removeAtInterval(duplicatedCurve, new Interval(tempPaneStartDomain, tempPaneEndDomain));

                }
            }

            if (duplicatedCurve.Domain.T0 != baseCurve.Domain.T0)
                duplicatedCurve = new LineCurve(duplicatedCurve.PointAt(duplicatedCurve.Domain.T0 - paneFrameWidth), duplicatedCurve.PointAt(duplicatedCurve.Domain.T1));

            if (duplicatedCurve.Domain.T1 != baseCurve.Domain.T1)
                duplicatedCurve = new LineCurve(duplicatedCurve.PointAt(duplicatedCurve.Domain.T0), duplicatedCurve.PointAt(duplicatedCurve.Domain.T1 + paneFrameWidth));

            Curve fixedCurve = offsetOneSide(duplicatedCurve, height, Vector3d.ZAxis);
            fixedCurve.Transform(Transform.Translation(tempPlane.ZAxis * (windowFrameDepth / 2 - paneFrameDepth)));
            Curve fixedInside = offsetInside(fixedCurve, paneFrameWidth, tempPlane);

            List<Curve> fixedCurves = new List<Curve>();

            fixedCurves.Add(fixedCurve);
            fixedCurves.Add(fixedInside);

            Brep fixedBaseBrep = Brep.CreatePlanarBreps(fixedCurves)[0];

            Curve pathCurve = new LineCurve(tempPlane.Origin, tempPlane.Origin + new Point3d(tempPlane.ZAxis * paneFrameDepth));
            output.Add(fixedBaseBrep.Faces[0].CreateExtrusion(pathCurve, true));

            Brep fixedInsdeBrep = Brep.CreatePlanarBreps(fixedCurve)[0];
            fixedInsdeBrep.Transform(Transform.Translation(tempPlane.ZAxis * paneFrameDepth / 2));

            output.Add(fixedInsdeBrep);

            return output;
        }

        private static Curve offsetOneSide(Curve baseCurve, double offsetDistance, Vector3d vector)
        {
            List<Curve> outputBase = new List<Curve>();
            Curve offsettedCurve = baseCurve.Duplicate() as Curve;
            offsettedCurve.Transform(Transform.Translation(vector / vector.Length * offsetDistance));

            outputBase.Add(baseCurve);
            outputBase.Add(new LineCurve(baseCurve.PointAt(baseCurve.Domain.T0), offsettedCurve.PointAt(baseCurve.Domain.T0)));
            outputBase.Add(offsettedCurve);
            outputBase.Add(new LineCurve(baseCurve.PointAt(baseCurve.Domain.T1), offsettedCurve.PointAt(baseCurve.Domain.T1)));

            return Curve.JoinCurves(outputBase)[0];
        }


        private static Curve offsetInside(Curve baseCurve, double offsetDistance, Plane plane)
        {
            if (baseCurve.ClosedCurveOrientation(plane) != CurveOrientation.CounterClockwise)
                baseCurve.Reverse();

            Curve[] ShatteredCurve = baseCurve.DuplicateSegments();

            List<Curve> offsettedCurveSet = new List<Curve>();

            foreach (Curve i in ShatteredCurve)
            {
                Curve tempCurve = i.Offset(plane, offsetDistance, 0, CurveOffsetCornerStyle.None)[0];

                offsettedCurveSet.Add(tempCurve);
            }

            List<Curve> outputBase = new List<Curve>();

            for (int i = 0; i < offsettedCurveSet.Count(); i++)
            {
                int h = (i + offsettedCurveSet.Count() - 1) % offsettedCurveSet.Count();
                int j = (i + offsettedCurveSet.Count() + 1) % offsettedCurveSet.Count();

                Point3d tempStartPoint = Rhino.Geometry.Intersect.Intersection.CurveCurve(offsettedCurveSet[h], offsettedCurveSet[i], 0, 0)[0].PointA;
                Point3d tempEndPoint = Rhino.Geometry.Intersect.Intersection.CurveCurve(offsettedCurveSet[j], offsettedCurveSet[i], 0, 0)[0].PointA;

                Curve tempCurve = new LineCurve(tempStartPoint, tempEndPoint);

                outputBase.Add(tempCurve);
            }

            Curve[] joinedCurve = Curve.JoinCurves(outputBase);

            return joinedCurve[0];
        }

        private static Curve removeAtInterval(Curve baseCurve, Interval interval)
        {
            List<double> splitDomain = new List<double>();

            splitDomain.Add(interval.Min);
            splitDomain.Add(interval.Max);

            Curve[] shatteredCurve = baseCurve.Split(splitDomain);

            foreach (Curve i in shatteredCurve)
            {
                if (interval.IncludesParameter(i.Domain.Mid) != true && i.GetLength() > 500)
                {
                    return i;
                }
            }

            return new LineCurve();
        }

        private enum WindowType { noPane, onePane, twoPanes };
        private enum WallType { wall };
    }
}
