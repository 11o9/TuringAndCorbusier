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
                output.AddRange(DrawHouse(hhp,true));
            }
            foreach (var cp in cps)
            {
                output.Add(DrawCore(cp));
            }

            output.AddRange(DrawCorridor(agOut));

            return output;
        }

        public static List<Brep> DrawHouse(Household hhp, bool toMesh)
        {
            double height = Consts.FloorHeight;
            Curve outline = hhp.GetOutline();

            if (outline == null)
                return new List<Brep>();

            Curve outlineUp = outline.DuplicateCurve();
            outlineUp.Translate(Vector3d.ZAxis * height);

            var floors = CurveTools.ToPolyline(outline);
            var ceilings = CurveTools.ToPolyline(outlineUp);

            Brep[] floor = Brep.CreatePlanarBreps(outline);
            Brep[] ceiling = Brep.CreatePlanarBreps(outlineUp);

            List<Point3d> points = new List<Point3d>();
            for (int i = 0; i < floors.Count; i++)
            {
                points.Add(floors[i]);
                points.Add(ceilings[i]);
            }
            
            List<Brep> wallBreps = new List<Brep>();

            for (int i = 0; i < points.Count; i+=2)
            {
                int start = i;
                int second = (i + 2) % points.Count;
                int third = (i + 3) % points.Count;
                int end = (i + 1) % points.Count;

                Point3d[] tomesh = { points[start], points[second], points[third], points[end], points[start] };

                if (points[start] == points[second])
                    continue;

                var tempSideRectangle = Brep.CreatePlanarBreps(new Polyline(tomesh).ToNurbsCurve());
          
                if (tempSideRectangle.Length > 0)
                    wallBreps.AddRange(tempSideRectangle);
                else
                {
                    int c = tomesh.Length;
                    int d = tempSideRectangle.Length;
                }
            }

            var union = Brep.CreateBooleanUnion(wallBreps, 0.1);
            var x = union.OrderByDescending(n => n.GetArea()).First();

            double windowSide = 300;
            double windowLow = 300;
            double windowHeight = 2100;
            double windowDepth = 200;

            List<Brep> wins = new List<Brep>();

            for (int i = 0; i < hhp.LightingEdge.Count; i++)
            {
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
            wins.Add(x);
            wins.AddRange(floor);
            wins.AddRange(ceiling);
            //1. 양쪽에서 300씩 , 위400 아래300 사각형.빵꾸. 안쪽으로 200
            //->창틀
            //2. 사방 30씩, 안쪽으로 100
            //->창문와꾸
            //3. 창 안쪽에서 20, 창문와꾸 30, 바깥쪽50 ,길이 1200 * 2 
            //4. ㅊ


            return wins;



        }

        public static List<Brep> DrawHouse(Household hhp)
        {
            double height = Consts.FloorHeight;
            Curve outline = hhp.GetOutline();

            if (outline == null)
                return new List<Brep>();

            Curve outlineUp = outline.DuplicateCurve();
            outlineUp.Translate(Vector3d.ZAxis * height);

            LineCurve rail = new LineCurve(outline.PointAtStart, outline.PointAtStart + Vector3d.ZAxis * height);
            Brep[] houseLofts = Brep.CreateFromSweep(rail, outline, true, 0.5);

            Brep[] p1 = Brep.CreatePlanarBreps(outline);
            Brep[] p2 = Brep.CreatePlanarBreps(outlineUp);


            //Brep[] houseLofts = Brep.CreateFromLoft(new List<Curve> { outline, outlineUp }, Point3d.Unset, Point3d.Unset, LoftType.Straight, true);
            if (houseLofts.Length == 0)
                return new List<Brep>();
            Brep x = houseLofts[0];

            List<Brep> wins = new List<Brep>();
            wins.AddRange(houseLofts);
            wins.AddRange(p1);
            wins.AddRange(p2);
         
            double windowSide = 300;
            double windowLow = 300;
            double windowHeight = 2100;
            double windowDepth = 200;



            //for (int i = 0; i < hhp.LightingEdge.Count; i++)
            //{
            //    Line tempLine = hhp.LightingEdge[i];

            //    Point3d midPoint = tempLine.PointAt(0.5);
            //    Vector3d windowVec = tempLine.UnitTangent;
            //    windowVec.Rotate(Math.PI / 2, Vector3d.ZAxis);
            //    midPoint.Transform(Transform.Translation(Vector3d.Multiply(windowVec, 10)));

            //    if (hhp.GetOutline().Contains(midPoint) == Rhino.Geometry.PointContainment.Inside)
            //    {
            //        tempLine.Flip();
            //    }

            //    tempLine.Extend(-windowSide, -windowSide);

            //    Curve tempCurve = tempLine.ToNurbsCurve();
            //    tempCurve.Translate(Vector3d.Multiply(Vector3d.ZAxis, windowLow));
            //    Point3d pt1 = tempCurve.PointAtStart;
            //    Point3d pt2 = tempCurve.PointAtEnd;
            //    Point3d pt3 = tempCurve.PointAtEnd;
            //    pt3.Transform(Transform.Translation(Vector3d.Multiply(Vector3d.ZAxis, windowHeight)));
            //    Point3d pt4 = tempCurve.PointAtStart;
            //    pt4.Transform(Transform.Translation(Vector3d.Multiply(Vector3d.ZAxis, windowHeight)));
            //    Point3d pt5 = tempCurve.PointAtStart;
            //    List<Point3d> windowPoints = new List<Point3d>();
            //    windowPoints.Add(pt1);
            //    windowPoints.Add(pt2);
            //    windowPoints.Add(pt3);
            //    windowPoints.Add(pt4);
            //    windowPoints.Add(pt5);
            //    Polyline windowPolyline = new Polyline(windowPoints);
            //    Curve win = windowPolyline.ToNurbsCurve();


            //    Plane windowPlane;
            //    win.TryGetPlane(out windowPlane);
            //    Vector3d windowNormal = windowPlane.Normal;
            //    Curve windowCurve = win.Duplicate() as Curve;
            //    windowCurve.Translate(Vector3d.Multiply(windowNormal, windowDepth));
            //    Surface windowSurface = Surface.CreateExtrusion(windowCurve, Vector3d.Multiply(windowNormal, -windowDepth * 2));
            //    Brep windowBrep = windowSurface.ToBrep();
            //    Brep[] withHolesTemp = x.Split(windowBrep, 1);

            //    if (withHolesTemp.Length != 0)
            //    {
            //        x = withHolesTemp[0];
            //    }

            //    Curve duplicatedWindowCurve = (win.Duplicate() as Curve);
            //    duplicatedWindowCurve.Transform(Transform.Translation(windowNormal * (windowDepth - 100)));

            //    Curve windowCurveBottom = duplicatedWindowCurve.DuplicateSegments()[0];
            //    Curve heightCurve = duplicatedWindowCurve.DuplicateSegments()[1];

            //    wins.AddRange(DrawWindowAll(windowCurveBottom, heightCurve.GetLength(), false));

            //    Curve[] tempLoftBase = { win, duplicatedWindowCurve };

            //    Curve pathCurve = new LineCurve(Point3d.Origin, new Point3d(windowNormal * (-windowDepth + 100)));
            //    wins.Add(Brep.JoinBreps(Brep.CreateFromLoft(tempLoftBase, Point3d.Unset, Point3d.Unset, LoftType.Normal, false), 0)[0]);
            //}


            //1. 양쪽에서 300씩 , 위400 아래300 사각형.빵꾸. 안쪽으로 200
            //->창틀
            //2. 사방 30씩, 안쪽으로 100
            //->창문와꾸
            //3. 창 안쪽에서 20, 창문와꾸 30, 바깥쪽50 ,길이 1200 * 2 
            //4. ㅊ



            //Brep w1 = Brep.create  hhp.LightingEdge[0]

            return wins;

        }

        public static Brep DrawCore(Core hhp)
        {
            double height = hhp.Stories == 0 ? Consts.PilotiHeight : Consts.FloorHeight;
            Vector3d x = hhp.XDirection;
            Vector3d y = hhp.YDirection;
            double width = hhp.Width;
            double depth = hhp.Depth;

            PolylineCurve plc = new PolylineCurve(new Point3d[] { hhp.Origin, hhp.Origin + x * width, hhp.Origin + x * width + y * depth, hhp.Origin + y * depth, hhp.Origin });

            if(plc.ClosedCurveOrientation(Plane.WorldXY) == CurveOrientation.CounterClockwise)
                plc.Reverse();
            return Extrusion.Create(plc, height, true).ToBrep();
            
        }

        public static List<Brep> DrawCorridor(Apartment apartment)
        {
            List<Brep> output = new List<Brep>();
            #region P1Corridor
            if (apartment.AGtype == "PT-1")
            {
                List<Brep> courtCorridorBrep = new List<Brep>();
                List<Curve> corridorLines = new List<Curve>();

                foreach (var hh in apartment.Household)
                {
                    foreach (var h in hh)
                    {
                        foreach (var household in h)
                        {
                            if (household.isCorridorType)
                            {
                                Curve corridorLine = new LineCurve(household.Origin, household.Origin + household.XDirection * household.XLengthA);
                                corridorLines.Add(corridorLine);
                            }
                        }
                    }
                }

                corridorLines = Curve.JoinCurves(corridorLines).ToList();


                for(int i = 0; i < corridorLines.Count;i++)
                {
                    Curve offset = corridorLines[i].DuplicateCurve();
                    Vector3d dir = offset.TangentAtStart;
                    dir.Rotate(Math.PI / 2, Vector3d.ZAxis);
                    offset.Transform(Transform.Translation(dir * Consts.corridorWidth));

                    PolylineCurve floor = new PolylineCurve(new Point3d[] { corridorLines[i].PointAtStart, offset.PointAtStart, offset.PointAtEnd, corridorLines[i].PointAtEnd, corridorLines[i].PointAtStart });

                    Brep corridorFloorBrep = Brep.CreatePlanarBreps(new List<Curve> { floor }).First();

                    PolylineCurve corridorWall = new PolylineCurve(new Point3d[] { corridorLines[i].PointAtStart, offset.PointAtStart, offset.PointAtEnd, corridorLines[i].PointAtEnd });

                    Surface corridorWallSurface = Surface.CreateExtrusion(corridorWall, Vector3d.Multiply(Vector3d.ZAxis, Consts.corridorWallHeight));

                    Brep corridorWallBrep = corridorWallSurface.ToBrep();


                    courtCorridorBrep.Add(corridorFloorBrep);
                    courtCorridorBrep.Add(corridorWallBrep);
                }
                output = courtCorridorBrep;
            }
            #endregion
            #region P3Corridor
            else if (apartment.AGtype == "PT-3")
            {
                bool isUsing1F = apartment.ParameterSet.using1F;
                List<Brep> courtCorridorBrep = new List<Brep>();
                List<Curve> centerLine = apartment.AptLines;
                double width = apartment.ParameterSet.Parameters[2];

                for (int i = 0; i < apartment.Household.Count; i++)
                {
                    if (i == 0 && isUsing1F)
                        continue;

                    for (int j = 0; j < apartment.Household[i].Count; j++)
                    {
                        List<Household> currentDongHouseholds = apartment.Household[i][j];
                        Curve currentCenterLine = centerLine[j].DuplicateCurve();

                        if ((int)currentCenterLine.ClosedCurveOrientation(new Vector3d(0, 0, 1)) == -1)
                        {
                            currentCenterLine.Reverse();
                        }
                        Curve currentInnerLine = currentCenterLine.Offset(Plane.WorldXY, width / 2, 1, CurveOffsetCornerStyle.Sharp)[0];

                        if ((int)currentInnerLine.ClosedCurveOrientation(new Vector3d(0, 0, 1)) == -1)
                        {
                            currentInnerLine.Reverse();
                        }

                        Curve corridorOutline = currentInnerLine.Offset(Plane.WorldXY, Consts.corridorWidth, 1, CurveOffsetCornerStyle.Sharp)[0];

                        Brep corridorFloorBrep = Brep.CreatePlanarBreps(new List<Curve> { currentInnerLine, corridorOutline }).First();
                        Surface corridorWallSurface = Surface.CreateExtrusion(corridorOutline, Vector3d.Multiply(Vector3d.ZAxis, Consts.corridorWallHeight));
                        Brep corridorWallBrep = corridorWallSurface.ToBrep();

                        corridorWallBrep.Translate(Vector3d.Multiply(Vector3d.ZAxis, currentDongHouseholds[0].Origin.Z));
                        corridorFloorBrep.Translate(Vector3d.Multiply(Vector3d.ZAxis, currentDongHouseholds[0].Origin.Z));
                        courtCorridorBrep.Add(corridorWallBrep);
                        courtCorridorBrep.Add(corridorFloorBrep);
                    }
                }
                output = courtCorridorBrep;
            }
        
            #endregion P3Corridor

            return output;
        }
     


public static List<Guid> DrawFoundation(Apartment apartment)
        {
            List<Guid> result = new List<Guid>();
            Plot plot = apartment.Plot;
            var LHGreen = System.Drawing.Color.FromArgb(127, 255, 0);

            Brep[] green = Brep.CreatePlanarBreps(plot.Boundary);
            Brep[] white = Brep.CreatePlanarBreps(plot.outrect);

            for (int i = 0; i < green.Length; i++)
            {
                var att = new Rhino.DocObjects.ObjectAttributes() { ColorSource = Rhino.DocObjects.ObjectColorSource.ColorFromMaterial, MaterialSource = Rhino.DocObjects.ObjectMaterialSource.MaterialFromObject };

                int index = Rhino.RhinoDoc.ActiveDoc.Materials.Find("LHGreen", true);
                if (index == -1)
                {
                    Rhino.DocObjects.Material matt = new Rhino.DocObjects.Material();
                    matt.DiffuseColor = LHGreen;
                    matt.Name = "LHGreen";
                    matt.CommitChanges();

                    Rhino.RhinoDoc.ActiveDoc.Materials.Add(matt);
                    att.MaterialIndex = Rhino.RhinoDoc.ActiveDoc.Materials.Find("LHGreen", true);
                }
                else
                {
                    att.MaterialIndex = index;
                }
                var extrusion = Extrusion.Create(plot.Boundary, 1, true).ToBrep();
                Rhino.RhinoDoc.ActiveDoc.Objects.AddBrep(extrusion, att);
            }

            for (int i = 0; i < white.Length; i++)
            {
                var att = new Rhino.DocObjects.ObjectAttributes() { ColorSource = Rhino.DocObjects.ObjectColorSource.ColorFromMaterial, MaterialSource = Rhino.DocObjects.ObjectMaterialSource.MaterialFromObject };

                int index = Rhino.RhinoDoc.ActiveDoc.Materials.Find("LHWhite", true);
                if (index == -1)
                {
                    Rhino.DocObjects.Material matt = new Rhino.DocObjects.Material();
                    matt.DiffuseColor = System.Drawing.Color.White;
                    matt.Name = "LHWhite";
                    matt.CommitChanges();

                    Rhino.RhinoDoc.ActiveDoc.Materials.Add(matt);
                    att.MaterialIndex = Rhino.RhinoDoc.ActiveDoc.Materials.Find("LHWhite", true);
                }
                else
                {
                    att.MaterialIndex = index;
                }

                Rhino.RhinoDoc.ActiveDoc.Objects.Add(white[i], att);
            }

            return result;

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
