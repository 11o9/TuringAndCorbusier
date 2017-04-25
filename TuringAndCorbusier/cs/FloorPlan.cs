using System;
using System.Collections.Generic;
using Rhino.Geometry;
using System.Linq;
using Rhino;
using System.IO;
using Rhino.Display;
namespace TuringAndCorbusier
{
    public class FloorPlan
    {
        //Constructor, 생성자
        public FloorPlan(HouseholdProperties householdProperty, List<FloorPlanLibrary> fpls, string agType)
        {
            FloorPlanLibrary fpl = typeDetector(fpls, householdProperty, agType);
            double exWallThickness = 300; double inWallThickness = 200; double entDoorWidth = 900; double inDoorWidth = 900;

            List<List<Curve>> wallCurves; List<List<Curve>> tilingCurves; List<List<Curve>> doorCurves; List<List<Curve>> windowCurves; List<List<Curve>> centerLines; List<Dimension> dimensions; List<List<Curve>> balconyLines; List<Curve> caps;
            try
            {
                planDrawer(fpl, householdProperty, exWallThickness, inWallThickness, entDoorWidth, inDoorWidth, out wallCurves, out tilingCurves, out doorCurves, out windowCurves, agType);

                List<Polyline> allOutlines = new List<Polyline>();
                Polyline outline = outlineMaker(householdProperty.XLengthA, householdProperty.XLengthB, householdProperty.YLengthA, householdProperty.YLengthB, Vector3d.XAxis, Vector3d.YAxis, Point3d.Origin);
                allOutlines.AddRange(remapper(householdProperty, fpl, fpl.roomList));
                allOutlines.AddRange(remapper(householdProperty, fpl, fpl.bathroomList));
                allOutlines.Add(outline);
                centerLinesAndDimensions(allOutlines, outline4CenterLine(outline, householdProperty.WallFactor, exWallThickness), 1500, out centerLines, out dimensions);

                balconyLines = balconyLineMaker(householdProperty, exWallThickness);

                wallCurves = totalTransformation(wallCurves, householdProperty);
                tilingCurves = totalTransformation(tilingCurves, householdProperty);
                doorCurves = totalTransformation(doorCurves, householdProperty);
                windowCurves = totalTransformation(windowCurves, householdProperty);
                centerLines = totalTransformation(centerLines, householdProperty);

                for (int i = 0; i < dimensions.Count; i++)
                {
                    List<Curve> dimLineTemp = new List<Curve>();
                    dimLineTemp.Add(dimensions[i].DimensionLine.DuplicateCurve());
                    List<Curve> exLineTemp = new List<Curve>(dimensions[i].ExtensionLine);
                    List<List<Curve>> beforeT = new List<List<Curve>>();
                    beforeT.Add(dimLineTemp);
                    beforeT.Add(exLineTemp);
                    beforeT = totalTransformation(beforeT, householdProperty);
                    dimensions[i].DimensionLine = beforeT[0][0];
                    dimensions[i].ExtensionLine = beforeT[1];

                    Vector3d xVec = dimensions[i].NumberText.TextPlane.XAxis;
                    Vector3d yVec = dimensions[i].NumberText.TextPlane.YAxis;

                    double height = dimensions[i].NumberText.Height;

                    Point3d textPoint = (beforeT[0][0].PointAtStart + beforeT[0][0].PointAtEnd) / 2;
                    textPoint.Transform(Transform.Translation(yVec * (height + 100)));

                    Plane pln = new Plane(textPoint, xVec, yVec);
                    Text3d num = new Text3d(dimensions[i].NumberText.Text, pln, height);
                    dimensions[i].NumberText = num;
                }

                caps = capsMaker(householdProperty, exWallThickness, inWallThickness);
                List<List<Curve>> capsTemp = new List<List<Curve>>();
                capsTemp.Add(caps);
                capsTemp = totalTransformation(capsTemp, householdProperty);
                //caps = capsTemp[0];
                roomTags = GetRoomTag(householdProperty, fpl);
            }
            catch (Exception)
            {
                HouseholdProperties hhp = new HouseholdProperties(householdProperty);
                wallCurves = new List<List<Curve>>();
                wallCurves.Add(exWallMaker(hhp.XLengthA, hhp.XLengthB, hhp.YLengthA, hhp.YLengthB, hhp.WallFactor, exWallThickness, inWallThickness));
                wallCurves = totalTransformation(wallCurves, householdProperty);
                tilingCurves = new List<List<Curve>>();
                doorCurves = new List<List<Curve>>();
                windowCurves = new List<List<Curve>>();
                centerLines = new List<List<Curve>>();
                balconyLines = new List<List<Curve>>();
                dimensions = new List<Dimension>();
                caps = new List<Curve>();
                roomTags = new List<Text3d>();
            }


            this.householdProperty = householdProperty;
            this.walls = wallCurves;
            this.tilings = tilingCurves;
            this.doors = doorCurves;
            this.windows = windowCurves;
            this.all = mergeAllCurves(wallCurves, tilingCurves, doorCurves, windowCurves);
            this.centerLines = centerLines;
            this.balconyLines = balconyLines;
            this.dimensions = dimensions;
            this.caps = caps;
            this.roomTags = roomTags;
        }
        //Field, 필드
        public HouseholdProperties householdProperty { get; private set; }
        public List<List<Curve>> walls { get; private set; }
        public List<List<Curve>> tilings { get; private set; }
        public List<List<Curve>> doors { get; private set; }
        public List<List<Curve>> windows { get; private set; }
        public List<Curve> all { get; private set; }
        public List<List<Curve>> centerLines { get; private set; }
        public List<List<Curve>> balconyLines { get; private set; }
        public List<Dimension> dimensions { get; private set; }
        public List<Curve> caps { get; private set; }
        public List<Text3d> roomTags { get; private set; }

        //Method, 메소드

        public List<Text3d> GetRoomTag(HouseholdProperties tempHouseholdProperties, FloorPlanLibrary fpl)
        {
            List<Point3d> remappedPoints = remapper(tempHouseholdProperties, fpl, fpl.tagPointList);

            remappedPoints = totalTransformation(remappedPoints, tempHouseholdProperties);

            List<Text3d> output = new List<Text3d>();

            for (int i = 0; i < remappedPoints.Count(); i++)
            {
                Text3d tempText3d = new Text3d(fpl.tagNameList[i], new Plane(remappedPoints[i], Vector3d.ZAxis), 30);
                output.Add(tempText3d);
            }

            return output;
        }
        public BoundingBox GetBoundingBox()
        {
            List<Point3d> PointCloud = new List<Point3d>();

            foreach (List<Curve> i in this.centerLines)
            {
                foreach (Curve j in i)
                {
                    PointCloud.Add(j.PointAt(j.Domain.T0));
                    PointCloud.Add(j.PointAt(j.Domain.T1));
                }
            }

            BoundingBox tempBBox = new BoundingBox(PointCloud);

            Rhino.DocObjects.ObjectAttributes minAttri = new Rhino.DocObjects.ObjectAttributes();
            Rhino.DocObjects.ObjectAttributes maxAttri = new Rhino.DocObjects.ObjectAttributes();

            minAttri.ColorSource = Rhino.DocObjects.ObjectColorSource.ColorFromObject;
            maxAttri.ColorSource = Rhino.DocObjects.ObjectColorSource.ColorFromObject;
            minAttri.ObjectColor = System.Drawing.Color.Aquamarine;
            maxAttri.ObjectColor = System.Drawing.Color.Red;

            return new BoundingBox(PointCloud);
        }
        private FloorPlanLibrary typeDetector(List<FloorPlanLibrary> plantypes, HouseholdProperties householdproperty, string agType)
        {
            FloorPlanLibrary output = plantypes[0];
            double score = double.MaxValue;

            List<int> priority = new List<int>();
            List<double> scores = new List<double>();
            for (int i = 0; i < plantypes.Count; i++)
            {
                FloorPlanLibrary plantype = plantypes[i];

                //corner or edge
                int hhpIsCorner;
                if (householdproperty.WallFactor.SequenceEqual(new List<double>(new double[6] { 1, 1, 0.5, 1, 0.5, 1 })) || householdproperty.WallFactor.SequenceEqual(new List<double>(new double[4] { 1, 0.5, 1, 0.5 })))
                    hhpIsCorner = 0;
                else
                    hhpIsCorner = 1;

                //+,0,-
                bool isYBequal;
                if (Math.Sign(householdproperty.YLengthB) == Math.Sign(plantype.yLengthB))
                    isYBequal = true;
                else
                    isYBequal = false;

                bool isXBequal;
                if (Math.Sign(householdproperty.XLengthB) == Math.Sign(plantype.xLengthB))
                    isXBequal = true;
                else
                    isXBequal = false;

                //isBigger
                bool isBigger;
                double dXa = Math.Abs((householdproperty.XLengthA - householdproperty.XLengthB)) - Math.Abs((plantype.xLengthA - plantype.xLengthB));
                double dXb = Math.Abs(householdproperty.XLengthB) - Math.Abs(plantype.xLengthB);
                double dYa = Math.Abs((householdproperty.YLengthA - householdproperty.YLengthB)) - Math.Abs((plantype.yLengthA - plantype.yLengthB));
                double dYb = Math.Abs(householdproperty.YLengthB) - Math.Abs(plantype.yLengthB);
                if (dXa >= 0 && dXb >= 0 && dYa >= 0 && dYb >= 0)
                    isBigger = true;
                else
                    isBigger = false;

                //isRightType
                bool isRightType;
                if ((agType == "PT-4" && plantype.isP4 == 1) || (agType != "PT-4" && plantype.isP4 != 1))
                    isRightType = true;
                else
                    isRightType = false;

                //priority
                if (hhpIsCorner == plantype.isCorner && isBigger && isYBequal && isXBequal && isRightType)
                    priority.Add(0);
                else if (hhpIsCorner == plantype.isCorner && isYBequal && isXBequal && isRightType)
                    priority.Add(1);
                else
                    priority.Add(2);

                //scores
                scores.Add(dXa * dXa + dXb * dXb + dYa * dYa + dYb * dYb);
            }


            for (int i = 0; i < plantypes.Count; i++)
            {
                if (priority[i] == 0 && scores[i] < score)
                {
                    score = scores[i];
                    output = plantypes[i];
                }
            }
            if (score == double.MaxValue)
            {
                for (int i = 0; i < plantypes.Count; i++)
                {
                    if (priority[i] == 1 && scores[i] < score)
                    {
                        score = scores[i];
                        output = plantypes[i];
                    }
                }
            }
            if (score == double.MaxValue)
            {
                for (int i = 0; i < plantypes.Count; i++)
                {
                    if (priority[i] == 2 && scores[i] < score)
                    {
                        score = scores[i];
                        output = plantypes[i];
                    }
                }
            }
            return output;
        }
        private List<Curve> mergeAllCurves(List<List<Curve>> walls, List<List<Curve>> tilings, List<List<Curve>> doors, List<List<Curve>> windows)
        {
            List<Curve> output = new List<Curve>();
            foreach (List<Curve> i in walls)
            {
                output.AddRange(i);
            }
            foreach (List<Curve> i in tilings)
            {
                output.AddRange(i);
            }
            foreach (List<Curve> i in doors)
            {
                output.AddRange(i);
            }
            foreach (List<Curve> i in windows)
            {
                output.AddRange(i);
            }
            return output;
        }

        //////////////////////////
        ///////  remapper  ///////
        //////////////////////////

        private List<Polyline> remapper(HouseholdProperties hhp, FloorPlanLibrary fpl, List<Polyline> beforeRoom)
        {
            //B stands for before
            double xaB = fpl.xLengthA;
            double xbB = fpl.xLengthB;
            double yaB = fpl.yLengthA;
            double ybB = fpl.yLengthB;
            //A stands for after
            double xaA = hhp.XLengthA;
            double xbA = hhp.XLengthB;
            double yaA = hhp.YLengthA;
            double ybA = hhp.YLengthB;

            double scaleX1 = (xaA - xbA) / (xaB - xbB);
            double scaleX2;
            if (xbB != 0)
                scaleX2 = xbA / xbB;
            else
                scaleX2 = 0;
            double scaleY1;
            if (ybB != 0)
                scaleY1 = ybA / ybB;
            else
                scaleY1 = 0;
            double scaleY2 = (yaA - ybA) / (yaB - ybB);

            List<Polyline> afterRoom = new List<Polyline>();
            foreach (Polyline room in beforeRoom)
            {
                List<Point3d> points = new List<Point3d>(room);
                List<Point3d> pointsTemp = new List<Point3d>();
                foreach (Point3d point in points)
                {
                    Point3d pt = Point3d.Origin;
                    if (point.X >= 0 && point.Y >= 0)
                    {
                        pt.X = point.X * scaleX1;
                        pt.Y = point.Y * scaleY1;
                    }
                    else if (point.X <= 0 && point.Y <= 0)
                    {
                        pt.X = point.X * scaleX2;
                        pt.Y = point.Y * scaleY2;
                    }
                    else if (point.X >= 0 && point.Y <= 0)
                    {
                        pt.X = point.X * scaleX1;
                        pt.Y = point.Y * scaleY2;
                    }
                    pointsTemp.Add(pt);
                }
                afterRoom.Add(new Polyline(pointsTemp));
            }

            return afterRoom;
        }
        private List<Line> remapper(HouseholdProperties hhp, FloorPlanLibrary fpl, List<Line> beforeWindow)
        {
            //B stands for before
            double xaB = fpl.xLengthA;
            double xbB = fpl.xLengthB;
            double yaB = fpl.yLengthA;
            double ybB = fpl.yLengthB;
            //A stands for after
            double xaA = hhp.XLengthA;
            double xbA = hhp.XLengthB;
            double yaA = hhp.YLengthA;
            double ybA = hhp.YLengthB;

            double scaleX1 = (xaA - xbA) / (xaB - xbB);
            double scaleX2;
            if (xbB != 0)
                scaleX2 = xbA / xbB;
            else
                scaleX2 = 0;
            double scaleY1;
            if (ybB != 0)
                scaleY1 = ybA / ybB;
            else
                scaleY1 = 0;
            double scaleY2 = (yaA - ybA) / (yaB - ybB);

            List<Line> afterWindow = new List<Line>();
            foreach (Line window in beforeWindow)
            {
                List<Point3d> points = new List<Point3d>();
                points.Add(window.From);
                points.Add(window.To);
                List<Point3d> pointsTemp = new List<Point3d>();
                foreach (Point3d point in points)
                {
                    Point3d pt = Point3d.Origin;
                    if (point.X >= 0 && point.Y >= 0)
                    {
                        pt.X = point.X * scaleX1;
                        pt.Y = point.Y * scaleY1;
                    }
                    else if (point.X <= 0 && point.Y <= 0)
                    {
                        pt.X = point.X * scaleX2;
                        pt.Y = point.Y * scaleY2;
                    }
                    else if (point.X >= 0 && point.Y <= 0)
                    {
                        pt.X = point.X * scaleX1;
                        pt.Y = point.Y * scaleY2;
                    }
                    pointsTemp.Add(pt);
                }
                afterWindow.Add(new Line(pointsTemp[0], pointsTemp[1]));
            }

            return afterWindow;
        }
        private List<Point3d> remapper(HouseholdProperties hhp, FloorPlanLibrary fpl, List<Point3d> beforePoint)
        {
            //B stands for before
            double xaB = fpl.xLengthA;
            double xbB = fpl.xLengthB;
            double yaB = fpl.yLengthA;
            double ybB = fpl.yLengthB;
            //A stands for after
            double xaA = hhp.XLengthA;
            double xbA = hhp.XLengthB;
            double yaA = hhp.YLengthA;
            double ybA = hhp.YLengthB;

            double scaleX1 = (xaA - xbA) / (xaB - xbB);
            double scaleX2;
            if (xbB != 0)
                scaleX2 = xbA / xbB;
            else
                scaleX2 = 0;
            double scaleY1;
            if (ybB != 0)
                scaleY1 = ybA / ybB;
            else
                scaleY1 = 0;
            double scaleY2 = (yaA - ybA) / (yaB - ybB);

            List<Point3d> afterPoint = new List<Point3d>();
            foreach (Point3d point in beforePoint)
            {
                Point3d pt = Point3d.Origin;
                if (point.X >= 0 && point.Y >= 0)
                {
                    pt.X = point.X * scaleX1;
                    pt.Y = point.Y * scaleY1;
                }
                else if (point.X <= 0 && point.Y <= 0)
                {
                    pt.X = point.X * scaleX2;
                    pt.Y = point.Y * scaleY2;
                }
                else if (point.X >= 0 && point.Y <= 0)
                {
                    pt.X = point.X * scaleX1;
                    pt.Y = point.Y * scaleY2;
                }
                afterPoint.Add(pt);
            }

            return afterPoint;
        }

        //////////////////////////
        /////////  main  /////////
        //////////////////////////

        private void planDrawer(FloorPlanLibrary fpl, HouseholdProperties hhp, double exWallThickness, double inWallThickness, double entDoorWidth, double inDoorWidth, out List<List<Curve>> wallCurves, out List<List<Curve>> tilingCurves, out List<List<Curve>> doorCurves, out List<List<Curve>> windowCurves, string agType)
        {
            double xa = hhp.XLengthA;
            double xb = hhp.XLengthB;
            double ya = hhp.YLengthA;
            double yb = hhp.YLengthB;

            List<Polyline> rooms = remapper(hhp, fpl, fpl.roomList);
            List<Polyline> bathrooms = remapper(hhp, fpl, fpl.bathroomList);
            List<Line> windows = windowModifier(hhp, remapper(hhp, fpl, fpl.windowLineList));
            List<Point3d> doors = remapper(hhp, fpl, fpl.doorCenterList);
            doors = entranceAdjust(fpl, hhp, agType, doors);

            List<Polyline> allrooms = new List<Polyline>();
            allrooms.AddRange(rooms);
            allrooms.AddRange(bathrooms);

            //rule : outer wall line is in [0] and inner wall line is in [1]
            List<Curve> exWall = exWallMaker(xa, xb, ya, yb, hhp.WallFactor, exWallThickness, inWallThickness);
            List<Interval> exOuterCull = new List<Interval>();
            List<Interval> exInnerCull = new List<Interval>();
            List<List<Curve>> inWall = new List<List<Curve>>();

            foreach (Polyline room in allrooms)
            {
                List<Interval> exInnerCullTemp = new List<Interval>();
                List<Curve> inWallTemp = inWallMaker(room, exWall, inWallThickness, out exInnerCullTemp);
                exInnerCull.AddRange(exInnerCullTemp);
                inWall.Add(inWallTemp);
            }

            //trim in-wall outer wall
            List<Curve> inOuter = inTrimmer(inWall, allrooms);

            //create windows
            List<List<Curve>> windowDetails = new List<List<Curve>>();

            List<Line> windowLines = new List<Line>();
            foreach (Line line in windows)
            {
                double p1;
                double p2;
                exWall[0].ClosestPoint(line.From, out p1);
                exWall[0].ClosestPoint(line.To, out p2);
                windowLines.Add(new Line(exWall[0].PointAt(Math.Min(p1, p2)), exWall[0].PointAt(Math.Max(p1, p2))));
            }

            foreach (Line line in windowLines)
            {
                double startOuter;
                double endOuter;
                double startInner;
                double endInner;

                Point3d startPoint = line.From;
                Point3d endPoint = line.To;
                startPoint.Transform(Transform.Translation(line.UnitTangent * (exWallThickness + 200)));
                endPoint.Transform(Transform.Translation(-line.UnitTangent * (exWallThickness + 200)));

                exWall[0].ClosestPoint(startPoint, out startOuter);
                exWall[0].ClosestPoint(endPoint, out endOuter);
                exWall[1].ClosestPoint(startPoint, out startInner);
                exWall[1].ClosestPoint(endPoint, out endInner);

                exOuterCull.Add(new Interval(startOuter, endOuter));
                exInnerCull.Add(new Interval(startInner, endInner));

                Line lineTemp = line;
                Vector3d vec = line.UnitTangent;
                vec.Rotate(-Math.PI / 2, Vector3d.ZAxis);
                lineTemp.Transform(Transform.Translation(vec * exWallThickness / 2));
                windowDetails.Add(new Window(lineTemp, exWallThickness).All);
            }

            //create doors
            List<List<Curve>> doorDetails = new List<List<Curve>>();

            List<Point3d> doorEx = new List<Point3d>();
            List<Point3d> doorIn = new List<Point3d>();
            foreach (Point3d pt in doors)
            {
                if (exWall[0].Contains(pt) == PointContainment.Coincident)
                    doorEx.Add(pt);
                else
                    doorIn.Add(pt);
            }

            foreach (Point3d point in doorEx)
            {
                double at;
                double startOuter;
                double endOuter;
                double startInner;
                double endInner;

                exWall[0].ClosestPoint(point, out at);
                Vector3d vec = exWall[0].TangentAt(at);
                Point3d startPoint = new Point3d(point);
                Point3d endPoint = new Point3d(point);
                startPoint.Transform(Transform.Translation(-vec * entDoorWidth / 2));
                endPoint.Transform(Transform.Translation(vec * entDoorWidth / 2));

                exWall[0].ClosestPoint(startPoint, out startOuter);
                exWall[0].ClosestPoint(endPoint, out endOuter);
                exWall[1].ClosestPoint(startPoint, out startInner);
                exWall[1].ClosestPoint(endPoint, out endInner);

                exOuterCull.Add(new Interval(startOuter, endOuter));
                exInnerCull.Add(new Interval(startInner, endInner));

                vec.Rotate(-Math.PI / 2, Vector3d.ZAxis);
                startPoint.Transform(Transform.Translation(vec * exWallThickness / 2));
                endPoint.Transform(Transform.Translation(vec * exWallThickness / 2));
                doorDetails.Add(new Door(startPoint, endPoint, exWallThickness, Plane.WorldXY).All);
            }

            foreach (Point3d point in doorIn)
            {
                for (int i = 0; i < allrooms.Count; i++)
                {
                    if (allrooms[i].ToNurbsCurve().Contains(point) == PointContainment.Coincident)
                    {
                        double t;
                        allrooms[i].ToNurbsCurve().ClosestPoint(point, out t);
                        Vector3d vec = allrooms[i].TangentAt(t);
                        Point3d start = new Point3d(point);
                        Point3d end = new Point3d(point);

                        start.Transform(Transform.Translation(-vec * inDoorWidth / 2));
                        end.Transform(Transform.Translation(vec * inDoorWidth / 2));
                        doorDetails.Add(new Door(start, end, inWallThickness, Plane.WorldXY).All);
                        i = allrooms.Count;
                    }
                }
            }

            //delete overlapping exOuterWall
            exOuterCull.AddRange(overlapCuller(exWall[0], hhp.WallFactor));

            //ex-wall trim
            List<Curve> trimmedExOuter = exTrimmer(exWall[0], exOuterCull);
            List<Curve> trimmedExInner = exTrimmer(exWall[1], exInnerCull);

            //in-wall trim
            List<List<Interval>> inOuterCull = new List<List<Interval>>();
            List<List<Interval>> inInnerCull = new List<List<Interval>>();
            for (int i = 0; i < inWall.Count; i++)
            {
                inInnerCull.Add(new List<Interval>());
            }
            for (int i = 0; i < inOuter.Count; i++)
            {
                inOuterCull.Add(new List<Interval>());
            }

            for (int i = 0; i < inWall.Count; i++)
            {
                List<Interval> temp = new List<Interval>();
                foreach (Point3d pt in doorIn)
                {
                    double t;
                    inWall[i][1].ClosestPoint(pt, out t);
                    if (pt.DistanceTo(inWall[i][1].PointAt(t)) < inWallThickness / 2 + 1)
                    {
                        Point3d pt1 = new Point3d(pt);
                        Point3d pt2 = new Point3d(pt);
                        Vector3d vec = inWall[i][1].TangentAt(t);
                        pt1.Transform(Transform.Translation(vec * 450));
                        pt2.Transform(Transform.Translation(-vec * 450));
                        double start, end;
                        inWall[i][1].ClosestPoint(pt1, out start);
                        inWall[i][1].ClosestPoint(pt2, out end);
                        temp.Add(new Interval(start, end));
                    }
                }
                inInnerCull[i] = temp;
            }
            for (int i = 0; i < inOuter.Count; i++)
            {
                List<Interval> temp = new List<Interval>();
                foreach (Point3d pt in doorIn)
                {
                    double t;
                    inOuter[i].ClosestPoint(pt, out t);
                    if (pt.DistanceTo(inOuter[i].PointAt(t)) < inWallThickness / 2 + 1)
                    {
                        Point3d pt1 = new Point3d(pt);
                        Point3d pt2 = new Point3d(pt);
                        Vector3d vec = inOuter[i].TangentAt(t);
                        pt1.Transform(Transform.Translation(vec * inDoorWidth / 2));
                        pt2.Transform(Transform.Translation(-vec * inDoorWidth / 2));
                        double start, end;
                        inOuter[i].ClosestPoint(pt1, out start);
                        inOuter[i].ClosestPoint(pt2, out end);
                        temp.Add(new Interval(start, end));
                    }
                }
                inOuterCull[i] = temp;
            }

            //trim
            List<List<Curve>> trimmedInInner = new List<List<Curve>>();
            List<List<Curve>> trimmedInOuter = new List<List<Curve>>();
            for (int i = 0; i < inWall.Count; i++)
            {
                trimmedInInner.Add(exTrimmer(inWall[i][1], inInnerCull[i]));
            }
            for (int i = 0; i < inOuter.Count; i++)
            {
                trimmedInOuter.Add(exTrimmer(inOuter[i], inOuterCull[i]));
            }


            //create tilings
            List<Curve> tilings = new List<Curve>();
            foreach (Polyline room in bathrooms)
            {
                Polyline outlinePoly;
                exWall[1].TryGetPolyline(out outlinePoly);
                Curve roomInner = roomInnerBoundaryFinder(outlinePoly, room, inWallThickness);
                tilings.AddRange(tilingsMaker(roomInner));
            }

            //output
            wallCurves = new List<List<Curve>>();
            tilingCurves = new List<List<Curve>>();
            doorCurves = new List<List<Curve>>();
            windowCurves = new List<List<Curve>>();

            wallCurves.Add(trimmedExOuter);
            wallCurves.Add(trimmedExInner);
            wallCurves.AddRange(trimmedInOuter);
            wallCurves.AddRange(trimmedInInner);

            tilingCurves.Add(tilings);

            doorCurves.AddRange(doorDetails);

            windowCurves.AddRange(windowDetails);
        }

        private List<Curve> exWallMaker(double xa, double xb, double ya, double yb, List<double> wallFactor, double exWallThickness, double inWallThickness)
        {
            //create house outline
            List<Point3d> outlinePoints = new List<Point3d>();
            Point3d pt = Point3d.Origin;
            Vector3d x = Vector3d.XAxis;
            Vector3d y = Vector3d.YAxis;

            outlinePoints.Add(pt);
            pt.Transform(Transform.Translation(y * yb));
            outlinePoints.Add(pt);
            pt.Transform(Transform.Translation(x * (xa - xb)));
            outlinePoints.Add(pt);
            pt.Transform(Transform.Translation(y * (-ya)));
            outlinePoints.Add(pt);
            pt.Transform(Transform.Translation(x * (-xa)));
            outlinePoints.Add(pt);
            pt.Transform(Transform.Translation(y * (ya - yb)));
            outlinePoints.Add(pt);
            outlinePoints = Point3d.CullDuplicates(outlinePoints, 1).ToList();
            pt.Transform(Transform.Translation(x * xb));
            outlinePoints.Add(pt);

            //create wall lines
            Polyline outline = new Polyline(outlinePoints);
            Curve outer = outline.ToNurbsCurve();
            Curve inner = exInWallMaker(outline, wallFactor, exWallThickness, inWallThickness);

            List<Curve> output = new List<Curve>();
            output.Add(outer);
            output.Add(inner);

            return output;
        }
        private Polyline alignPolylineOrientation(Polyline polyline)
        {
            Polyline output = new Polyline(polyline);
            if (output.ToNurbsCurve().ClosedCurveOrientation(Vector3d.ZAxis) != CurveOrientation.CounterClockwise)
            {
                output.Reverse();
            }
            return output;
        }
        private List<Curve> inWallMaker(Polyline room, List<Curve> exWall, double thickness, out List<Interval> exWallCull)
        {
            room = alignPolylineOrientation(room);
            Line[] segs = room.GetSegments();
            List<Curve> segCurves = new List<Curve>();

            for (int i = 0; i < segs.Length; i++)
            {
                if (exWall[1].ToNurbsCurve().Contains((segs[i].From + segs[i].To) / 2) == PointContainment.Inside)
                {
                    segCurves.Add(segs[i].ToNurbsCurve());
                }
            }

            Curve inWallCurve = Curve.JoinCurves(segCurves)[0];
            Curve innerWall = Curve.JoinCurves(inWallCurve.Offset(Plane.WorldXY, thickness / 2, 0, CurveOffsetCornerStyle.Sharp))[0];
            Curve outerWall = Curve.JoinCurves(inWallCurve.Offset(Plane.WorldXY, -thickness / 2, 0, CurveOffsetCornerStyle.Sharp))[0];

            var intersectI = Rhino.Geometry.Intersect.Intersection.CurveCurve(innerWall, exWall[1], 0, 0);
            innerWall = innerWall.Trim(intersectI[0].ParameterA, intersectI[1].ParameterA);

            var intersectO = Rhino.Geometry.Intersect.Intersection.CurveCurve(outerWall, exWall[1], 0, 0);
            outerWall = outerWall.Trim(intersectO[0].ParameterA, intersectO[1].ParameterA);
            outerWall = outerWallSimplifier(outerWall);

            exWallCull = new List<Interval>();
            exWallCull.Add(new Interval(Math.Min(intersectI[0].ParameterB, intersectO[0].ParameterB), Math.Max(intersectI[0].ParameterB, intersectO[0].ParameterB)));
            exWallCull.Add(new Interval(Math.Min(intersectI[1].ParameterB, intersectO[1].ParameterB), Math.Max(intersectI[1].ParameterB, intersectO[1].ParameterB)));

            List<Curve> output = new List<Curve>();
            output.Add(outerWall);
            output.Add(innerWall);
            return output;
        }
        private Curve outerWallSimplifier(Curve outerWall)
        {
            Polyline polyline;
            outerWall.TryGetPolyline(out polyline);
            if (polyline.Count == 5)
            {
                List<Point3d> temp = new List<Point3d>();
                temp.Add(polyline[0]);
                temp.Add(polyline[2]);
                temp.Add(polyline[4]);
                polyline = new Polyline(temp);
            }
            else if (polyline.Count == 8)
            {
                List<Point3d> temp = new List<Point3d>();
                temp.Add(polyline[0]);
                temp.Add(polyline[2]);
                temp.Add(polyline[5]);
                temp.Add(polyline[7]);
                polyline = new Polyline(temp);
            }

            return polyline.ToNurbsCurve();
        }
        private List<Curve> inTrimmer(List<List<Curve>> inWall, List<Polyline> rooms)
        {
            //find parameters of intersection point
            List<List<double>> intersectionParams = new List<List<double>>();
            for (int i = 0; i < inWall.Count; i++)
            {
                List<double> paramsTemp = new List<double>();
                for (int j = 0; j < inWall.Count; j++)
                {
                    if (i != j)
                    {
                        Curve a = inWall[i][0];
                        Curve b = inWall[j][0];
                        var intersect = Rhino.Geometry.Intersect.Intersection.CurveCurve(a, b, 0, 0);
                        for (int k = 0; k < intersect.Count; k++)
                        {
                            paramsTemp.Add(intersect[k].ParameterA);
                        }
                    }
                }
                intersectionParams.Add(paramsTemp);
            }

            //trim the walls
            List<Curve> output = new List<Curve>();
            for (int i = 0; i < inWall.Count; i++)
            {
                List<Curve> curvesTemp = inWall[i][0].Split(intersectionParams[i]).ToList();
                foreach (Curve crv in curvesTemp)
                {
                    bool doAdd = true;
                    foreach (Polyline room in rooms)
                    {
                        if (room.ToNurbsCurve().Contains(crv.PointAt(crv.Domain.Mid)) == PointContainment.Inside)
                        {
                            doAdd = false;
                            continue;
                        }
                    }
                    if (doAdd)
                        output.Add(crv);
                }
            }

            return output;
        }
        private List<Curve> exTrimmer(Curve exWall, List<Interval> exWallCull)
        {
            List<double> newInterval = new List<double>();
            newInterval.Add(exWall.Domain.Min);
            List<Interval> exWallCullClone = new List<Interval>(exWallCull);
            exWallCullClone.Sort();
            for (int i = 0; i < exWallCullClone.Count; i++)
            {
                newInterval.Add(exWallCullClone[i].Min);
                newInterval.Add(exWallCullClone[i].Max);
            }
            newInterval.Add(exWall.Domain.Max);

            List<Curve> output = new List<Curve>();
            for (int i = 0; i < newInterval.Count; i += 2)
            {
                output.Add(exWall.ToNurbsCurve(new Interval(newInterval[i], newInterval[i + 1])));
            }
            return output;
        }
        private class Window
        {
            //Constructor
            public Window(Line windowLine, double exWallThickness)
            {
                Point3d windowEnd1 = windowLine.ToNurbsCurve().PointAtStart;
                Point3d windowEnd2 = windowLine.ToNurbsCurve().PointAtEnd;
                List<Curve> tempFrame = new List<Curve>();

                Curve tempWindowWallCurve = new LineCurve(windowEnd1, windowEnd2).ToNurbsCurve();
                Point3d windowOffset1 = Point3d.Add(windowEnd1, tempWindowWallCurve.TangentAtStart * (exWallThickness + 200));
                Point3d windowOffset2 = Point3d.Add(windowEnd2, tempWindowWallCurve.TangentAtEnd * -(exWallThickness + 200));
                Point3d windowCenterPt = tempWindowWallCurve.PointAt(tempWindowWallCurve.Domain.Mid);

                Point3d windowFrameOffset1 = Point3d.Add(windowOffset1, tempWindowWallCurve.TangentAtStart * 100);
                Point3d windowFrameOffset2 = Point3d.Add(windowOffset2, tempWindowWallCurve.TangentAtEnd * -100);

                Curve tempFrameLine1 = new LineCurve(windowOffset1, windowFrameOffset1).ToNurbsCurve();
                Curve tempFrameLine2 = new LineCurve(windowOffset2, windowFrameOffset2).ToNurbsCurve();
                Curve frameLine1 = tempFrameLine1.Offset(Plane.WorldXY, exWallThickness / 2, 0, CurveOffsetCornerStyle.None)[0];
                Curve frameLine5 = tempFrameLine2.Offset(Plane.WorldXY, exWallThickness / 2, 0, CurveOffsetCornerStyle.None)[0];
                Curve frameLine2 = tempFrameLine1.Offset(Plane.WorldXY, -exWallThickness / 2, 0, CurveOffsetCornerStyle.None)[0];
                Curve frameLine6 = tempFrameLine2.Offset(Plane.WorldXY, -exWallThickness / 2, 0, CurveOffsetCornerStyle.None)[0];
                Curve frameLine3 = new LineCurve(frameLine1.PointAtStart, frameLine2.PointAtStart).ToNurbsCurve();
                Curve frameLine4 = new LineCurve(frameLine1.PointAtEnd, frameLine2.PointAtEnd).ToNurbsCurve();
                Curve frameLine7 = new LineCurve(frameLine5.PointAtStart, frameLine6.PointAtStart).ToNurbsCurve();
                Curve frameLine8 = new LineCurve(frameLine5.PointAtEnd, frameLine6.PointAtEnd).ToNurbsCurve();
                List<Curve> frameList01 = new List<Curve>();
                List<Curve> frameList02 = new List<Curve>();
                frameList01.Add(frameLine1);
                frameList01.Add(frameLine2);
                //frameList01.Add(frameLine3);
                frameList01.Add(frameLine4);
                frameList02.Add(frameLine5);
                frameList02.Add(frameLine6);
                //frameList02.Add(frameLine7);
                frameList02.Add(frameLine8);
                Curve frame01 = Curve.JoinCurves(frameList01)[0];
                Curve frame02 = Curve.JoinCurves(frameList02)[0];

                tempFrame.Add(frame01);
                tempFrame.Add(frame02);

                List<Curve> tempGlass = new List<Curve>();
                List<Curve> tempDividingLine = new List<Curve>();

                if (windowOffset1.DistanceTo(windowOffset2) <= 700)
                {
                    Curve tempGlassLine1 = new LineCurve(windowFrameOffset1, windowFrameOffset2).ToNurbsCurve();
                    Curve glassLine1 = tempGlassLine1.Offset(Plane.WorldXY, 15, 0, CurveOffsetCornerStyle.None)[0];
                    Curve glassLine2 = tempGlassLine1.Offset(Plane.WorldXY, -15, 0, CurveOffsetCornerStyle.None)[0];
                    Curve glassLine3 = new LineCurve(glassLine1.PointAtStart, glassLine2.PointAtStart).ToNurbsCurve();
                    Curve glassLine4 = new LineCurve(glassLine1.PointAtEnd, glassLine2.PointAtEnd).ToNurbsCurve();
                    List<Curve> glassList01 = new List<Curve>();
                    glassList01.Add(glassLine1);
                    glassList01.Add(glassLine2);
                    glassList01.Add(glassLine3);
                    glassList01.Add(glassLine4);
                    Curve windowGlass01 = Curve.JoinCurves(glassList01)[0];

                    tempGlass.Add(windowGlass01);
                }
                if (700 < windowOffset1.DistanceTo(windowOffset2) && windowOffset1.DistanceTo(windowOffset2) <= 2000)
                {
                    Point3d windowCenterOffsetPt1 = Point3d.Add(windowCenterPt, tempWindowWallCurve.TangentAtStart * 300);
                    Point3d windowCenterOffsetPt2 = Point3d.Add(windowCenterPt, -tempWindowWallCurve.TangentAtStart * 300);
                    Curve windowCenterDividingLine = new LineCurve(windowCenterOffsetPt1, windowCenterOffsetPt2).ToNurbsCurve();
                    windowCenterDividingLine.Transform(Rhino.Geometry.Transform.Rotation(Math.PI / 2, Vector3d.ZAxis, windowCenterPt));

                    Point3d glassOffset1 = Point3d.Add(windowCenterPt, tempWindowWallCurve.TangentAtStart * 50);
                    Point3d glassOffset2 = Point3d.Add(windowCenterPt, tempWindowWallCurve.TangentAtEnd * -50);

                    Curve glassLine1 = new LineCurve(windowFrameOffset1, glassOffset1).ToNurbsCurve();
                    Curve glassLine5 = new LineCurve(windowFrameOffset2, glassOffset2).ToNurbsCurve();
                    Curve glassLine2 = glassLine1.Offset(Plane.WorldXY, 30, 0, CurveOffsetCornerStyle.None)[0];
                    Curve glassLine6 = glassLine5.Offset(Plane.WorldXY, 30, 0, CurveOffsetCornerStyle.None)[0];
                    Curve glassLine3 = new LineCurve(glassLine1.PointAtStart, glassLine2.PointAtStart).ToNurbsCurve();
                    Curve glassLine4 = new LineCurve(glassLine1.PointAtEnd, glassLine2.PointAtEnd).ToNurbsCurve();
                    Curve glassLine7 = new LineCurve(glassLine5.PointAtStart, glassLine6.PointAtStart).ToNurbsCurve();
                    Curve glassLine8 = new LineCurve(glassLine5.PointAtEnd, glassLine6.PointAtEnd).ToNurbsCurve();
                    List<Curve> glassList11 = new List<Curve>();
                    List<Curve> glassList12 = new List<Curve>();
                    glassList11.Add(glassLine1);
                    glassList11.Add(glassLine2);
                    glassList11.Add(glassLine3);
                    glassList11.Add(glassLine4);
                    glassList12.Add(glassLine5);
                    glassList12.Add(glassLine6);
                    glassList12.Add(glassLine7);
                    glassList12.Add(glassLine8);
                    Curve windowGlass11 = Curve.JoinCurves(glassList11)[0];
                    Curve windowGlass12 = Curve.JoinCurves(glassList12)[0];

                    tempGlass.Add(windowGlass11);
                    tempGlass.Add(windowGlass12);


                    tempDividingLine.Add(windowCenterDividingLine);
                }
                if (2000 < windowOffset1.DistanceTo(windowOffset2) && windowOffset1.DistanceTo(windowOffset2) <= 8000)
                {
                    Point3d windowQuarterPt1 = tempWindowWallCurve.PointAt((tempWindowWallCurve.Domain.Mid + tempWindowWallCurve.Domain.T0) / 2);
                    Point3d windowQuarterPt2 = tempWindowWallCurve.PointAt((tempWindowWallCurve.Domain.Mid + tempWindowWallCurve.Domain.T1) / 2);

                    Point3d windowQuarterOffsetPt1 = Point3d.Add(windowQuarterPt1, tempWindowWallCurve.TangentAtStart * 300);
                    Point3d windowQuarterOffsetPt2 = Point3d.Add(windowQuarterPt1, -tempWindowWallCurve.TangentAtStart * 300);
                    Point3d windowQuarterOffsetPt3 = Point3d.Add(windowQuarterPt2, tempWindowWallCurve.TangentAtStart * 300);
                    Point3d windowQuarterOffsetPt4 = Point3d.Add(windowQuarterPt2, -tempWindowWallCurve.TangentAtStart * 300);
                    Curve windowQuarterDividingLine1 = new LineCurve(windowQuarterOffsetPt1, windowQuarterOffsetPt2).ToNurbsCurve();
                    Curve windowQuarterDividingLine2 = new LineCurve(windowQuarterOffsetPt3, windowQuarterOffsetPt4).ToNurbsCurve();
                    windowQuarterDividingLine1.Transform(Rhino.Geometry.Transform.Rotation(Math.PI / 2, Vector3d.ZAxis, windowQuarterPt1));
                    windowQuarterDividingLine2.Transform(Rhino.Geometry.Transform.Rotation(Math.PI / 2, Vector3d.ZAxis, windowQuarterPt2));

                    Point3d glassOffset1 = Point3d.Add(windowQuarterPt1, tempWindowWallCurve.TangentAtStart * 50);
                    Point3d glassOffset2 = Point3d.Add(windowQuarterPt1, tempWindowWallCurve.TangentAtStart * -50);
                    Point3d glassOffset3 = Point3d.Add(windowQuarterPt2, tempWindowWallCurve.TangentAtStart * 50);
                    Point3d glassOffset4 = Point3d.Add(windowQuarterPt2, tempWindowWallCurve.TangentAtStart * -50);
                    Curve glassLine1 = new LineCurve(windowFrameOffset1, glassOffset1).ToNurbsCurve();
                    Curve glassLine5 = new LineCurve(glassOffset3, glassOffset2).ToNurbsCurve();
                    Curve glassLine9 = new LineCurve(glassOffset4, windowFrameOffset2).ToNurbsCurve();
                    Curve glassLine2 = glassLine1.Offset(Plane.WorldXY, 30, 0, CurveOffsetCornerStyle.None)[0];
                    Curve glassLine6 = glassLine5.Offset(Plane.WorldXY, 30, 0, CurveOffsetCornerStyle.None)[0];
                    Curve glassLine10 = glassLine9.Offset(Plane.WorldXY, 30, 0, CurveOffsetCornerStyle.None)[0];
                    Curve glassLine3 = new LineCurve(glassLine1.PointAtStart, glassLine2.PointAtStart).ToNurbsCurve();
                    Curve glassLine4 = new LineCurve(glassLine1.PointAtEnd, glassLine2.PointAtEnd).ToNurbsCurve();
                    Curve glassLine7 = new LineCurve(glassLine5.PointAtStart, glassLine6.PointAtStart).ToNurbsCurve();
                    Curve glassLine8 = new LineCurve(glassLine5.PointAtEnd, glassLine6.PointAtEnd).ToNurbsCurve();
                    Curve glassLine11 = new LineCurve(glassLine9.PointAtStart, glassLine10.PointAtStart).ToNurbsCurve();
                    Curve glassLine12 = new LineCurve(glassLine9.PointAtEnd, glassLine10.PointAtEnd).ToNurbsCurve();
                    List<Curve> glassList21 = new List<Curve>();
                    List<Curve> glassList22 = new List<Curve>();
                    List<Curve> glassList23 = new List<Curve>();
                    glassList21.Add(glassLine1);
                    glassList21.Add(glassLine2);
                    glassList21.Add(glassLine3);
                    glassList21.Add(glassLine4);
                    glassList22.Add(glassLine5);
                    glassList22.Add(glassLine6);
                    glassList22.Add(glassLine7);
                    glassList22.Add(glassLine8);
                    glassList23.Add(glassLine9);
                    glassList23.Add(glassLine10);
                    glassList23.Add(glassLine11);
                    glassList23.Add(glassLine12);
                    Curve windowGlass21 = Curve.JoinCurves(glassList21)[0];
                    Curve windowGlass22 = Curve.JoinCurves(glassList22)[0];
                    Curve windowGlass23 = Curve.JoinCurves(glassList23)[0];

                    tempGlass.Add(windowGlass21);
                    tempGlass.Add(windowGlass22);
                    tempGlass.Add(windowGlass23);
                    tempDividingLine.Add(windowQuarterDividingLine1);
                    tempDividingLine.Add(windowQuarterDividingLine2);
                }

                this.frame = tempFrame;
                this.glass = tempGlass;
                this.dividingLine = tempDividingLine;

            }

            //Fields

            List<Curve> frame;
            List<Curve> glass;
            List<Curve> dividingLine;

            //Methods

            //Properties

            public List<Curve> Frame { get { return this.frame; } }
            public List<Curve> Glass { get { return this.glass; } }
            public List<Curve> DividingLine { get { return this.dividingLine; } }
            public List<Curve> All
            {
                get
                {
                    List<Curve> allCrvs = new List<Curve>();

                    allCrvs.AddRange(frame);
                    allCrvs.AddRange(glass);
                    allCrvs.AddRange(dividingLine);

                    return allCrvs;
                }
            }
        }
        private class Door
        {
            //Constructor
            public Door(Point3d hinge, Point3d doorEnd, double inWallThickness, Plane householdPlane)
            {
                Curve tempDoorCenterCurve = new Line(hinge, doorEnd).ToNurbsCurve();
                Point3d doorOffset1 = Point3d.Add(hinge, tempDoorCenterCurve.TangentAtStart * 20);
                Point3d doorOffset2 = Point3d.Add(doorEnd, tempDoorCenterCurve.TangentAtEnd * -20);
                Point3d doorOffset3 = Point3d.Add(hinge, tempDoorCenterCurve.TangentAtStart * 50);
                Point3d doorOffset4 = Point3d.Add(doorEnd, tempDoorCenterCurve.TangentAtEnd * -50);

                List<Curve> tempFrame = new List<Curve>();

                tempFrame.Add(new Line(hinge, doorOffset1).ToNurbsCurve().Offset(householdPlane, -inWallThickness / 2, 0, 0)[0]);
                tempFrame.Add(new Line(doorOffset2, doorEnd).ToNurbsCurve().Offset(householdPlane, -inWallThickness / 2, 0, 0)[0]);
                tempFrame.Add(new Line(doorOffset1, doorOffset3).ToNurbsCurve());
                tempFrame.Add(new Line(doorOffset4, doorOffset2).ToNurbsCurve());
                tempFrame.Add(new Line(hinge, doorOffset3).ToNurbsCurve().Offset(householdPlane, inWallThickness / 2, 0, 0)[0]);
                tempFrame.Add(new Line(doorOffset4, doorEnd).ToNurbsCurve().Offset(householdPlane, inWallThickness / 2, 0, 0)[0]);
                //tempFrame.Add(new Line(tempFrame[0].PointAtStart, tempFrame[4].PointAtStart).ToNurbsCurve());
                //tempFrame.Add(new Line(tempFrame[1].PointAtEnd, tempFrame[5].PointAtEnd).ToNurbsCurve());
                tempFrame.Add(new Line(tempFrame[0].PointAtEnd, tempFrame[2].PointAtStart).ToNurbsCurve());
                tempFrame.Add(new Line(tempFrame[1].PointAtStart, tempFrame[3].PointAtEnd).ToNurbsCurve());
                tempFrame.Add(new Line(tempFrame[2].PointAtEnd, tempFrame[4].PointAtEnd).ToNurbsCurve());
                tempFrame.Add(new Line(tempFrame[3].PointAtStart, tempFrame[5].PointAtStart).ToNurbsCurve());

                List<Curve> tempDoorTeritory = new List<Curve>();

                Curve tempDoorTeritory1 = new Line(doorOffset1, doorOffset2).ToNurbsCurve();
                Curve tempDoorTeritory2 = tempDoorTeritory1.Duplicate() as Curve;
                tempDoorTeritory2.Transform(Rhino.Geometry.Transform.Rotation(Math.PI / 2, Vector3d.ZAxis, doorOffset1));
                Point3d tempDoorTeritory2EndPt = tempDoorTeritory2.PointAtEnd;
                Curve tempDoorTeritoryCenter = tempDoorTeritory1.Duplicate() as Curve;
                tempDoorTeritoryCenter.Transform(Rhino.Geometry.Transform.Rotation(Math.PI / 4, Vector3d.ZAxis, doorOffset1));
                Point3d tempDoorTeritoryCenterEndPt = tempDoorTeritoryCenter.PointAtEnd;
                Curve tempDoorTeritory3 = new Arc(doorOffset2, tempDoorTeritoryCenterEndPt, tempDoorTeritory2EndPt).ToNurbsCurve();

                tempDoorTeritory.Add(tempDoorTeritory1);
                tempDoorTeritory.Add(tempDoorTeritory2);
                tempDoorTeritory.Add(tempDoorTeritory3);

                List<Curve> tempRail = new List<Curve>();
                List<Point3d> railPoints = new List<Point3d>();

                Curve tempDoorTeritory4 = tempDoorTeritory2.Offset(householdPlane, 30, 0, Rhino.Geometry.CurveOffsetCornerStyle.None).ToList()[0];
                Point3d railPoint1 = tempDoorTeritory2.PointAtStart;
                Point3d railPoint2 = tempDoorTeritory2.PointAtEnd;
                Point3d railPoint3 = tempDoorTeritory4.PointAtEnd;
                Point3d railPoint4 = tempDoorTeritory4.PointAtStart;

                railPoints.Add(railPoint1);
                railPoints.Add(railPoint2);
                railPoints.Add(railPoint3);
                railPoints.Add(railPoint4);
                railPoints.Add(railPoint1);

                Polyline tempRailbox = new Polyline(railPoints);
                tempRail.Add(tempDoorTeritory4);

                this.frame = tempFrame;
                this.doorTeritory = tempDoorTeritory;
                this.rail = tempRail;

            }

            //Fields

            List<Curve> frame;
            List<Curve> doorTeritory;
            List<Curve> rail;

            //Methods

            //Properties

            public List<Curve> Frame { get { return this.frame; } }
            public List<Curve> DoorTeritory { get { return this.doorTeritory; } }
            public List<Curve> Rail { get { return this.rail; } }
            public List<Curve> All
            {
                get
                {
                    List<Curve> allCrvs = new List<Curve>();

                    allCrvs.AddRange(frame);
                    allCrvs.AddRange(doorTeritory);
                    allCrvs.AddRange(rail);

                    return allCrvs;
                }
            }
        }

        public class Dimension2
        {
            public List<Curve> curves = new List<Curve>();
            public Text3d length;

            public bool isHorizontal = false;


            public Dimension2(Point3d start, Point3d end)
            {
                Line line = new Line(start, end);
                Point3d[] pa = { start, start + Vector3d.XAxis * -50 + Vector3d.YAxis * -125, start + Vector3d.XAxis * 50 + Vector3d.YAxis * -125, start };
                Point3d[] pa2 = { end, end + Vector3d.XAxis * -50 + Vector3d.YAxis * 125, end + Vector3d.XAxis * 50 + Vector3d.YAxis * 125, end };
                Polyline arrow = new Polyline(pa.ToList());
                Polyline arrow2 = new Polyline(pa2.ToList());

                if (end.Y == start.Y)
                    isHorizontal = true;

                double rad = Vector3d.VectorAngle(Vector3d.XAxis, new Vector3d(start - end));

                Plane textpln = new Plane(line.PointAt(0.5), Vector3d.XAxis, Vector3d.YAxis);

                textpln.Rotate(rad, Vector3d.ZAxis);


                Text3d text = new Text3d(Math.Round(line.Length).ToString(),textpln,0);
                text.Height = 1500;
                
                length = text;

                this.curves.Add(line.ToNurbsCurve());
                this.curves.Add(arrow.ToNurbsCurve());
                this.curves.Add(arrow2.ToNurbsCurve());

            }

            public Dimension2(Point3d start, Point3d end, double height)
            {
                Vector3d dir = end - start;
                dir.Unitize();

                if (end.Y == start.Y)
                    isHorizontal = true;

                Vector3d up = new Vector3d(dir);


                if(up.X > 0)
                    up.Rotate(RhinoMath.ToRadians(90),Vector3d.ZAxis);
                else
                    up.Rotate(RhinoMath.ToRadians(-90), Vector3d.ZAxis);

                Vector3d down = new Vector3d(-up);


                Line line = new Line(start+  up * height, end+ up * height);




                Line startside = new Line(start, up * height);
                Line endside = new Line(end, up * height);

                Line startsideextend = new Line(startside.To, up * 150);
                Line endsideextend = new Line(endside.To, up * 150);


                Point3d ps = startside.To;
                Point3d ps2 = endside.To;

                Point3d[] pa = { ps, ps + up * -50 + dir * 125, ps + up * 50 + dir * 125, ps };
                Point3d[] pa2 = { ps2, ps2 + up * -50 + dir * -125, ps2 + up * 50 + dir * -125, ps2 };
                Polyline arrow = new Polyline(pa.ToList());
                Polyline arrow2 = new Polyline(pa2.ToList());


                double rad = Vector3d.VectorAngle(Vector3d.XAxis, new Vector3d(end - start));

                Vector3d textdir = dir;

                if (dir.X < 0 || (dir.Y < 0 || dir.Y > 0))
                    textdir *= -1;



                Plane textpln = new Plane(line.PointAt(0.5), textdir, up);


                Text3d text = new Text3d(Math.Round(line.Length).ToString(), textpln, 1500);

                length = text;

                this.curves.Add(startsideextend.ToNurbsCurve());
                this.curves.Add(endsideextend.ToNurbsCurve());
                this.curves.Add(line.ToNurbsCurve());
                this.curves.Add(startside.ToNurbsCurve());
                this.curves.Add(endside.ToNurbsCurve());
                this.curves.Add(arrow.ToNurbsCurve());
                this.curves.Add(arrow2.ToNurbsCurve());

            }



            public List<Guid> Print()
            {
                List<Guid> result = new List<Guid>();
                foreach (var c in curves)
                {
                    result.Add(LoadManager.getInstance().DrawObjectWithSpecificLayer(c, LoadManager.NamedLayer.Guide));
                }

                result.Add(LoadManager.getInstance().DrawObjectWithSpecificLayer(length, LoadManager.NamedLayer.Guide));
                
                return result;

            }

        }

        public class Dimension
        {
            //Constructor
           


            public Dimension(List<Point3d> endPoints, Point3d side, double length)
            {
                double start = length - 100;
                double end = length + 100;

                //dimension line
                Line line = new Line(endPoints[0], endPoints[1]);
                Vector3d direction = new Vector3d(side - line.ClosestPoint(side, false));
                direction.Unitize();
                Curve dimensionLine = line.ToNurbsCurve();
                dimensionLine.Transform(Transform.Translation(direction * length));

                //extension line
                List<Curve> extensionLine = new List<Curve>();
                foreach (Point3d p in endPoints)
                {
                    Point3d startP = new Point3d(p);
                    startP.Transform(Transform.Translation(direction * start));
                    Point3d endP = new Point3d(p);
                    endP.Transform(Transform.Translation(direction * end));
                    extensionLine.Add(new Line(startP, endP).ToNurbsCurve());
                }

                List<Curve> dimLineTemp = new List<Curve>();
                dimLineTemp.Add(dimensionLine);
                List<Curve> exLineTemp = new List<Curve>(extensionLine);
                List<List<Curve>> allLinesTemp = new List<List<Curve>>();
                allLinesTemp.Add(dimLineTemp);
                allLinesTemp.Add(exLineTemp);

                //number
                double height = 200;
                Vector3d yVec = new Vector3d(direction);
                if (yVec.Y < 0)
                    yVec = -yVec;
                Vector3d xVec = new Vector3d(yVec);
                xVec.Rotate(-Math.PI / 2, Vector3d.ZAxis);

                Point3d textPoint = (dimensionLine.PointAtStart + dimensionLine.PointAtEnd) / 2;
                textPoint.Transform(Transform.Translation(yVec * (height + 100)));

                Plane pln = new Plane(textPoint, xVec, yVec);
                double fifties = line.Length - line.Length % 50;
                Text3d num = new Text3d(fifties.ToString(), pln, height);

                this.dimensionLine = dimensionLine;
                this.extensionLine = extensionLine;
                this.numberText = num;
            }

            public Dimension(List<Point3d> endPoints, Point3d side, double length, bool upper)
            {
                double start = length - 100;
                double end = length + 100;

                //dimension line
                Line line = new Line(endPoints[0], endPoints[1]);
                Vector3d direction = new Vector3d(side - line.ClosestPoint(side, false));
                direction.Unitize();
                Curve dimensionLine = line.ToNurbsCurve();
                dimensionLine.Transform(Transform.Translation(direction * length));

                //extension line
                List<Curve> extensionLine = new List<Curve>();
                foreach (Point3d p in endPoints)
                {
                    Point3d startP = new Point3d(p);
                    startP.Transform(Transform.Translation(direction * start));
                    Point3d endP = new Point3d(p);
                    endP.Transform(Transform.Translation(direction * end));
                    extensionLine.Add(new Line(startP, endP).ToNurbsCurve());
                }

                List<Curve> dimLineTemp = new List<Curve>();
                dimLineTemp.Add(dimensionLine);
                List<Curve> exLineTemp = new List<Curve>(extensionLine);
                List<List<Curve>> allLinesTemp = new List<List<Curve>>();
                allLinesTemp.Add(dimLineTemp);
                allLinesTemp.Add(exLineTemp);

                //number
                double height = 200;
                Vector3d yVec = new Vector3d(direction);
                if (yVec.Y < 0)
                    yVec = -yVec;
                Vector3d xVec = new Vector3d(yVec);
                xVec.Rotate(-Math.PI / 2, Vector3d.ZAxis);

                Point3d textPoint = (dimensionLine.PointAtStart + dimensionLine.PointAtEnd) / 2;
                if (upper == true)
                    textPoint.Transform(Transform.Translation(yVec * (height + 100)));
                else
                    textPoint.Transform(Transform.Translation(-yVec * (height + 100)));

                Plane pln = new Plane(textPoint, xVec, yVec);
                double fifties = line.Length - line.Length % 50;
                Text3d num = new Text3d(fifties.ToString(), pln, height);

                this.dimensionLine = dimensionLine;
                this.extensionLine = extensionLine;
                this.numberText = num;
            }

            public Dimension(List<Point3d> endPoints, Point3d side, double length, string str)
            {
                double start = length - 100;
                double end = length + 100;

                //dimension line
                Line line = new Line(endPoints[0], endPoints[1]);
                Vector3d direction = new Vector3d(side - line.ClosestPoint(side, false));
                direction.Unitize();
                Curve dimensionLine = line.ToNurbsCurve();
                dimensionLine.Transform(Transform.Translation(direction * length));

                //extension line
                List<Curve> extensionLine = new List<Curve>();
                foreach (Point3d p in endPoints)
                {
                    Point3d startP = new Point3d(p);
                    startP.Transform(Transform.Translation(direction * start));
                    Point3d endP = new Point3d(p);
                    endP.Transform(Transform.Translation(direction * end));
                    extensionLine.Add(new Line(startP, endP).ToNurbsCurve());
                }

                List<Curve> dimLineTemp = new List<Curve>();
                dimLineTemp.Add(dimensionLine);
                List<Curve> exLineTemp = new List<Curve>(extensionLine);
                List<List<Curve>> allLinesTemp = new List<List<Curve>>();
                allLinesTemp.Add(dimLineTemp);
                allLinesTemp.Add(exLineTemp);

                //number
                double height = 200;
                Vector3d yVec = new Vector3d(direction);
                if (yVec.Y < 0)
                    yVec = -yVec;
                Vector3d xVec = new Vector3d(yVec);
                xVec.Rotate(-Math.PI / 2, Vector3d.ZAxis);

                Point3d textPoint = (dimensionLine.PointAtStart + dimensionLine.PointAtEnd) / 2;
                textPoint.Transform(Transform.Translation(yVec * (height + 100)));

                Plane pln = new Plane(textPoint, xVec, yVec);
                Text3d num = new Text3d(str, pln, height);

                this.dimensionLine = dimensionLine;
                this.extensionLine = extensionLine;
                this.numberText = num;
            }
            //Fields

            Curve dimensionLine;
            List<Curve> extensionLine;
            Text3d numberText;

            //Methods

            //Properties
            public Curve DimensionLine
            {
                get { return this.dimensionLine; }
                set { this.dimensionLine = value; }
            }
            public List<Curve> ExtensionLine
            {
                get { return this.extensionLine; }
                set { this.extensionLine = value; }
            }
            public Text3d NumberText
            {
                get { return this.numberText; }
                set { this.numberText = value; }
            }
        }
        private List<Line> windowModifier(HouseholdProperties hhp, List<Line> windowLines)
        {

            //cull if window position is not good
            List<List<Curve>> goodPositionTemp = new List<List<Curve>>();
            List<Curve> tempCurve = new List<Curve>();
            for (int i = 0; i < windowLines.Count; i++)
            {
                tempCurve.Add(windowLines[i].ToNurbsCurve());
            }

            goodPositionTemp.Add(tempCurve);
            List<Curve> goodPosition = totalTransformation(goodPositionTemp, hhp)[0];

            List<Line> goodLines = new List<Line>();
            for (int i = 0; i < goodPosition.Count; i++)
            {
                bool isGood = false;
                foreach (Line line in hhp.LightingEdge)
                {
                    Point3d mid = goodPosition[i].PointAtLength(goodPosition[i].GetLength() / 2);
                    if (line.ClosestPoint(mid, true).DistanceTo(mid) < 1)
                        isGood = true;
                }
                if (isGood)
                    goodLines.Add(windowLines[i]);
            }

            //split if window is too long (limit : 8000)
            List<Line> output = new List<Line>();
            foreach (Line line in goodLines)
            {
                List<Line> linesTemp = new List<Line>();
                int splitNum = (int)line.Length / 8000 + 1;
                for (int i = 0; i < splitNum; i++)
                {
                    double t = (double)splitNum;
                    linesTemp.Add(new Line(line.PointAt(i / t), line.PointAt((i + 1) / t)));
                }
                output.AddRange(linesTemp);
            }
            return output;
        }
        private List<Curve> tilingsMaker(Curve roomBoundary)
        {
            List<Curve> output = new List<Curve>();
            var bbox = roomBoundary.GetBoundingBox(Plane.WorldXY);
            double gap = 200;
            for (double i = bbox.Min.X + gap; i < bbox.Max.X; i += gap)
            {
                Line line = new Line(new Point3d(i, bbox.Min.Y - 1, 0), new Point3d(i, bbox.Max.Y + 1, 0));
                var intersection = Rhino.Geometry.Intersect.Intersection.CurveCurve(roomBoundary, line.ToNurbsCurve(), 0, 0);
                if (intersection.Count % 2 == 0)
                {
                    for (int j = 0; j < intersection.Count; j += 2)
                    {
                        output.Add(new Line(intersection[j].PointA, intersection[j + 1].PointA).ToNurbsCurve());
                    }
                }
            }
            for (double i = bbox.Min.Y + gap; i < bbox.Max.Y; i += gap)
            {
                Line line = new Line(new Point3d(bbox.Min.X - 1, i, 0), new Point3d(bbox.Max.X + 1, i, 0));
                var intersection = Rhino.Geometry.Intersect.Intersection.CurveCurve(roomBoundary, line.ToNurbsCurve(), 0, 0);
                if (intersection.Count % 2 == 0)
                {
                    for (int j = 0; j < intersection.Count; j += 2)
                    {
                        output.Add(new Line(intersection[j].PointA, intersection[j + 1].PointA).ToNurbsCurve());
                    }
                }
            }
            return output;
        }
        private Curve roomInnerBoundaryFinder(Polyline exInner, Polyline room, double inThick)
        {
            room = alignPolylineOrientation(room);
            Curve roomInner = Curve.JoinCurves(room.ToNurbsCurve().Offset(Plane.WorldXY, inThick / 2, 0, CurveOffsetCornerStyle.Sharp))[0];

            return joinRegulations(roomInner, exInner.ToNurbsCurve());
        }
        private Curve joinRegulations(Curve firstCurve, Curve secondCurve)
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

            Curve combinationCurve = Curve.JoinCurves(usableCrvs)[0];
            return combinationCurve;
        }
        private Curve exInWallMaker(Polyline outline, List<double> wallFactor, double exWallThickness, double inWallThickness)
        {
            //make list of lines
            List<Point3d> pts = outline.ToList();
            List<Line> lines = new List<Line>();
            for (int i = 0; i < pts.Count - 1; i++)
            {
                lines.Add(new Line(pts[i], pts[i + 1]));
            }

            //shift line list
            List<Line> linesTemp = new List<Line>();
            for (int i = 0; i < lines.Count; i++)
            {
                linesTemp.Add(lines[i]);
            }
            lines = new List<Line>(linesTemp);

            //offset lines and add the first to last(for intersect lineline operation)
            linesTemp.Clear();
            for (int i = 0; i < lines.Count; i++)
            {
                Vector3d vec = lines[i].UnitTangent;
                vec.Rotate(-Math.PI / 2, Vector3d.ZAxis);
                Line lineTemp = lines[i];
                if (wallFactor[i] == 0.5)
                    lineTemp.Transform(Transform.Translation(vec * wallFactor[i] * inWallThickness));
                else
                    lineTemp.Transform(Transform.Translation(vec * wallFactor[i] * exWallThickness));

                linesTemp.Add(lineTemp);
            }
            linesTemp.Add(linesTemp[0]);
            lines = new List<Line>(linesTemp);

            //intersection points
            List<Point3d> ptsNew = new List<Point3d>();
            for (int i = 0; i < lines.Count - 1; i++)
            {
                double a, b;
                bool check = Rhino.Geometry.Intersect.Intersection.LineLine(lines[i], lines[i + 1], out a, out b);
                if (check)
                    ptsNew.Add(lines[i].PointAt(a));
                else
                    ptsNew.Add(lines[i].PointAt(1));
            }
            ptsNew.Add(ptsNew[0]);

            return new Polyline(ptsNew).ToNurbsCurve();
        }
        private List<Interval> overlapCuller(Curve outline, List<double> wallFactor)
        {
            Polyline poly;
            outline.TryGetPolyline(out poly);
            List<Line> lines = poly.GetSegments().ToList();

            List<Interval> output = new List<Interval>();
            for (int i = 0; i < wallFactor.Count; i++)
            {
                if (wallFactor[i] == 0.5)
                {
                    Point3d a = lines[i].From;
                    Point3d b = lines[i].To;
                    double paramA, paramB;
                    outline.ClosestPoint(a, out paramA);
                    outline.ClosestPoint(b, out paramB);
                    if (paramA == 3 && paramB == 0)
                        paramB = 4;
                    output.Add(new Interval(paramA, paramB));
                }
            }
            return output;
        }

        private List<Point3d> entranceAdjust(FloorPlanLibrary fpl, HouseholdProperties hhp, string agType, List<Point3d> doorPoints)
        {
            Polyline outline = outlineMaker(hhp.XLengthA, hhp.XLengthB, hhp.YLengthA, hhp.YLengthB, Vector3d.XAxis, Vector3d.YAxis, Point3d.Origin);

            double param = double.MaxValue;
            for (int i = 0; i < doorPoints.Count; i++)
            {
                if (outline.ClosestPoint(doorPoints[i]).DistanceTo(doorPoints[i]) < 0.01)
                {

                    param = outline.ClosestParameter(doorPoints[i]);
                    doorPoints.RemoveAt(i);
                    break;
                }
            }

            if (param == double.MaxValue)
            {
                return doorPoints;
            }

            if (agType == "PT-1")
            {
                if (hhp.WallFactor == new List<double>(new double[] { 1, 1, 0.5, 1, 0.5, 1 }) && (int)param == 0)
                {
                    Point3d ptTemp = new Point3d(hhp.Origin);
                    ptTemp.Transform(Transform.Translation(hhp.YDirection * Consts.corridorWidth / 2));
                    doorPoints.Add(ptTemp);
                }
                else
                {
                    //add original door point
                    doorPoints.Add(outline.PointAt(param));
                }
            }
            else if (agType == "PT-3")
            {
                //add original door point
                doorPoints.Add(outline.PointAt(param));
            }
            else if (agType == "PT-4")
            {
                if (hhp.WallFactor == new List<double>(new double[] { 1, 0.5, 1, 0.5 }))
                {
                    Point3d ptTemp = new Point3d(hhp.Origin);
                    ptTemp.Transform(Transform.Translation(-hhp.YDirection * 4500));
                }
                else
                {
                    Point3d ptTemp = new Point3d(hhp.Origin);
                    ptTemp.Transform(Transform.Translation(hhp.YDirection * hhp.YLengthB + hhp.XDirection * 4500));
                }
            }

            return doorPoints;
        }

        //////////////////////////
        //////  transform  ///////
        //////////////////////////

        private List<List<Curve>> totalTransformation(List<List<Curve>> curves, HouseholdProperties hhp)
        {
            bool doMirror = false;
            if (Math.Sign(getAngle(hhp.XDirection, hhp.YDirection)) < 0)
                doMirror = true;

            double angle = getAngle(Vector3d.XAxis, hhp.XDirection);

            curves = curves.Select(n => transformations(n, angle, doMirror, new Vector3d(hhp.Origin))).ToList();
            return curves;
        }
        private List<Point3d> totalTransformation(List<Point3d> points, HouseholdProperties hhp)
        {
            bool doMirror = false;
            if (Math.Sign(getAngle(hhp.XDirection, hhp.YDirection)) < 0)
                doMirror = true;

            double angle = getAngle(Vector3d.XAxis, hhp.XDirection);

            points = points.Select(n => transformations(n, angle, doMirror, new Vector3d(hhp.Origin))).ToList();
            return points;
        }
        private double getAngle(Vector3d baseVec, Vector3d targetVec)
        {
            double angle = Vector3d.VectorAngle(baseVec, targetVec);
            angle *= Math.Sign(Vector3d.CrossProduct(baseVec, targetVec).Z);
            return angle;
        }
        private List<Curve> transformations(List<Curve> curves, double angle, bool doMirror, Vector3d moveVec)
        {
            return translator(rotator(mirrorZX(curves, doMirror), angle), moveVec);
        }
        private Point3d transformations(Point3d point, double angle, bool doMirror, Vector3d moveVec)
        {
            return translator(rotator(mirrorZX(point, doMirror), angle), moveVec);
        }
        private List<Curve> rotator(List<Curve> curves, double angle)
        {
            List<Curve> output = new List<Curve>();
            if (curves != null)
            {
                foreach (Curve curve in curves)
                {
                    if (curve != null)
                    {
                        Curve temp = curve.DuplicateCurve();
                        temp.Rotate(angle, Vector3d.ZAxis, Point3d.Origin);
                        output.Add(temp);
                    }
                }
            }
            return output;
        }
        private Point3d rotator(Point3d point, double angle)
        {
            if (point != null)
            {
                Point3d temp = new Point3d(point);
                temp.Transform(Transform.Rotation(angle, Vector3d.ZAxis, Point3d.Origin));

                return temp;
            }

            return point;
        }
        private List<Curve> mirrorZX(List<Curve> curves, bool doMirror)
        {
            List<Curve> output = new List<Curve>();
            if (doMirror && curves != null)
            {
                for (int i = 0; i < curves.Count; i++)
                {
                    if (curves[i] != null)
                    {
                        Curve temp = curves[i].DuplicateCurve();
                        temp.Transform(Transform.Mirror(Plane.WorldZX));
                        output.Add(temp);
                    }
                }
            }
            else
                output = curves;
            return output;
        }
        private Point3d mirrorZX(Point3d point, bool doMirror)
        {
            if (doMirror)
            {
                if (point != null)
                {
                    Point3d temp = new Point3d(point);
                    temp.Transform(Transform.Mirror(Plane.WorldZX));
                    return temp;
                }

                return point;
            }
            else
                return point;
        }
        private List<Curve> translator(List<Curve> curves, Vector3d moveVec)
        {
            List<Curve> output = new List<Curve>();
            if (curves != null)
            {
                foreach (Curve curve in curves)
                {
                    if (curve != null)
                    {
                        Curve temp = curve.DuplicateCurve();
                        temp.Transform(Transform.Translation(moveVec));
                        output.Add(temp);
                    }
                }
            }
            return output;
        }
        private Point3d translator(Point3d point, Vector3d moveVec)
        {
            if (point != null)
            {
                Point3d temp = new Point3d(point);
                temp.Transform(Transform.Translation(moveVec));
                return temp;
            }

            return point;
        }

        //////////////////////////
        /////      caps      /////
        //////////////////////////

        private List<Curve> capsMaker(HouseholdProperties hhp, double exWallThickness, double inWallThickness)
        {
            List<double> wf = hhp.WallFactor;
            Vector3d x = hhp.XDirection; Vector3d y = hhp.YDirection;
            double xa = hhp.XLengthA; double xb = hhp.XLengthB; double ya = hhp.YLengthA; double yb = hhp.YLengthB;
            double e = exWallThickness; double i = inWallThickness;

            List<Curve> output = new List<Curve>();
            if (wf.SequenceEqual(new List<double>(new double[] { 1, 0.5, 1, 1, 0.5, 1 })))//corner, yb>0
            {
                Point3d pt = hhp.Origin;
                List<Point3d> crvPts = new List<Point3d>();

                pt.Transform(Transform.Translation(yb * y));
                crvPts.Add(pt);
                pt.Transform(Transform.Translation(i / 2 * y));
                crvPts.Add(pt);
                pt.Transform(Transform.Translation((xa - xb) * x));
                crvPts.Add(pt);
                pt.Transform(Transform.Translation(-i / 2 * y));
                crvPts.Add(pt);
                output.Add(new Polyline(crvPts).ToNurbsCurve());

                pt = hhp.Origin;
                crvPts.Clear();

                pt.Transform(Transform.Translation(-xb * x));
                crvPts.Add(pt);
                pt.Transform(Transform.Translation(-i / 2 * x));
                crvPts.Add(pt);
                pt.Transform(Transform.Translation((yb - ya) * y));
                crvPts.Add(pt);
                pt.Transform(Transform.Translation(i / 2 * x));
                crvPts.Add(pt);
                output.Add(new Polyline(crvPts).ToNurbsCurve());
            }
            else if (wf.SequenceEqual(new List<double>(new double[] { 1, 1, 1, 0.5, 1 })))//corner, yb=0
            {
                Point3d pt = hhp.Origin;
                List<Point3d> crvPts = new List<Point3d>();

                pt.Transform(Transform.Translation(-xb * x));
                crvPts.Add(pt);
                pt.Transform(Transform.Translation(-i / 2 * x));
                crvPts.Add(pt);
                pt.Transform(Transform.Translation((yb - ya) * y));
                crvPts.Add(pt);
                pt.Transform(Transform.Translation(i / 2 * x));
                crvPts.Add(pt);
                output.Add(new Polyline(crvPts).ToNurbsCurve());
            }
            else if (wf.SequenceEqual(new List<double>(new double[] { 0, 0.5, 1, 1, 0.5, 1 })))//corner, yb<0
            {
                Point3d pt = hhp.Origin;
                List<Point3d> crvPts = new List<Point3d>();

                pt.Transform(Transform.Translation(-xb * x));
                crvPts.Add(pt);
                pt.Transform(Transform.Translation(-i / 2 * x));
                crvPts.Add(pt);
                pt.Transform(Transform.Translation((yb - ya) * y));
                crvPts.Add(pt);
                pt.Transform(Transform.Translation(i / 2 * x));
                crvPts.Add(pt);
                output.Add(new Polyline(crvPts).ToNurbsCurve());

                pt = hhp.Origin;
                crvPts.Clear();

                crvPts.Add(pt);
                pt.Transform(Transform.Translation(e * x));
                crvPts.Add(pt);
                pt.Transform(Transform.Translation((yb + i / 2) * y));
                crvPts.Add(pt);
                pt.Transform(Transform.Translation((xa - xb - e) * x));
                crvPts.Add(pt);
                pt.Transform(Transform.Translation(-i / 2 * y));
                crvPts.Add(pt);
                output.Add(new Polyline(crvPts).ToNurbsCurve());

            }
            else if (wf.SequenceEqual(new List<double>(new double[] { 1, 0.5, 1, 0.5 })))//edge, yb>0
            {
                Point3d pt = hhp.Origin;
                List<Point3d> crvPts = new List<Point3d>();

                pt.Transform(Transform.Translation(yb * y + (xa - xb) * x));
                crvPts.Add(pt);
                pt.Transform(Transform.Translation(i / 2 * x));
                crvPts.Add(pt);
                pt.Transform(Transform.Translation(-ya * y));
                crvPts.Add(pt);
                pt.Transform(Transform.Translation(-i / 2 * x));
                crvPts.Add(pt);
                output.Add(new Polyline(crvPts).ToNurbsCurve());

                pt = hhp.Origin;
                crvPts.Clear();

                pt.Transform(Transform.Translation(-xb * x));
                crvPts.Add(pt);
                pt.Transform(Transform.Translation(-i / 2 * x));
                crvPts.Add(pt);
                pt.Transform(Transform.Translation((yb - ya) * y));
                crvPts.Add(pt);
                pt.Transform(Transform.Translation(i / 2 * x));
                crvPts.Add(pt);
                output.Add(new Polyline(crvPts).ToNurbsCurve());
            }
            else if (wf.SequenceEqual(new List<double>(new double[] { 1, 1, 0.5, 1, 0.5, 1 })))//edge, yb=0
            {
                Point3d pt = hhp.Origin;
                List<Point3d> crvPts = new List<Point3d>();

                pt.Transform(Transform.Translation(yb * y + (xa - xb) * x));
                crvPts.Add(pt);
                pt.Transform(Transform.Translation(i / 2 * x));
                crvPts.Add(pt);
                pt.Transform(Transform.Translation(-ya * y));
                crvPts.Add(pt);
                pt.Transform(Transform.Translation(-i / 2 * x));
                crvPts.Add(pt);
                output.Add(new Polyline(crvPts).ToNurbsCurve());

                pt = hhp.Origin;
                crvPts.Clear();

                pt.Transform(Transform.Translation(-xb * x));
                crvPts.Add(pt);
                pt.Transform(Transform.Translation(-i / 2 * x));
                crvPts.Add(pt);
                pt.Transform(Transform.Translation((yb - ya) * y));
                crvPts.Add(pt);
                pt.Transform(Transform.Translation(i / 2 * x));
                crvPts.Add(pt);
                output.Add(new Polyline(crvPts).ToNurbsCurve());
            }

            return output;
        }

        //////////////////////////
        /////  center lines  /////
        //////////////////////////

        private void centerLinesAndDimensions(List<Polyline> allOutlines, Polyline outline, double extraLength, out List<List<Curve>> centerLines, out List<Dimension> dimensions)
        {
            //find all points of polyline
            List<Point3d> allPoints = new List<Point3d>();
            for (int i = 0; i < allOutlines.Count; i++)
            {
                allPoints.AddRange(new List<Point3d>(allOutlines[i].ToList()));
            }
            allPoints.AddRange(new List<Point3d>(outline.ToList()));
            List<double> posX = allPoints.Select(p => p.X).Distinct().ToList();
            List<double> posY = allPoints.Select(p => p.Y).Distinct().ToList();
            posX.Sort();
            posY.Sort();

            //get x and y coordinate of points
            double xMin = posX.Min() - extraLength;
            double xMax = posX.Max() + extraLength;
            double yMin = posY.Min() - extraLength;
            double yMax = posY.Max() + extraLength;

            //boundaries of x and y
            List<Point3d> boundaryPoints = new List<Point3d>(outline.ToList());
            List<double> posXB = boundaryPoints.Select(p => p.X).Distinct().ToList();
            List<double> posYB = boundaryPoints.Select(p => p.Y).Distinct().ToList();
            double xMinB = posXB.Min();
            double xMaxB = posXB.Max();
            double yMinB = posYB.Min();
            double yMaxB = posYB.Max();

            for (int i = posX.Count - 1; i >= 0; i--)
            {
                if (posX[i] > xMaxB || posX[i] < xMinB)
                    posX.RemoveAt(i);
            }
            for (int i = posY.Count - 1; i >= 0; i--)
            {
                if (posY[i] > yMaxB || posY[i] < yMinB)
                    posY.RemoveAt(i);
            }

            //make vertical and horizontal lines
            List<Line> vertical = new List<Line>();
            List<Line> horizontal = new List<Line>();

            for (int i = 0; i < posX.Count; i++)
            {
                vertical.Add(new Line(new Point3d(posX[i], yMin, 0), new Point3d(posX[i], yMax, 0)));
            }
            for (int i = 0; i < posY.Count; i++)
            {
                horizontal.Add(new Line(new Point3d(xMin, posY[i], 0), new Point3d(xMax, posY[i], 0)));
            }

            //cull the lines on the outline
            List<Point3d> exPoints = new List<Point3d>(allOutlines[allOutlines.Count - 1].ToList());
            for (int i = vertical.Count - 2; i >= 1; i--)
            {
                for (int j = 0; j < exPoints.Count; j++)
                {
                    if (vertical[i].DistanceTo(exPoints[j], false) < 1)
                    {
                        vertical.RemoveAt(i);
                        posX.RemoveAt(i);
                        break;
                    }
                }
            }
            for (int i = horizontal.Count - 2; i >= 1; i--)
            {
                for (int j = 0; j < exPoints.Count; j++)
                {
                    if (horizontal[i].DistanceTo(exPoints[j], false) < 1)
                    {
                        horizontal.RemoveAt(i);
                        posY.RemoveAt(i);
                        break;
                    }
                }
            }

            //merge all the lines and convert into dotted lines
            List<Curve> allLines = new List<Curve>();
            allLines.AddRange(vertical.Select(l => l.ToNurbsCurve()).ToList());
            allLines.AddRange(horizontal.Select(l => l.ToNurbsCurve()).ToList());

            List<List<Curve>> convertedCurves = new List<List<Curve>>();
            List<Curve> output = new List<Curve>();
            List<double> centerLinesParams = new List<double>(new double[] { 800, 60, 60, 60 });
            foreach (Curve curve in allLines)
            {
                convertedCurves.Add(curveConverter(curve, centerLinesParams));
                output.AddRange(curveConverter(curve, centerLinesParams));
            }
            centerLines = convertedCurves;

            //make points for dimensions
            List<Point3d> xAxisP = new List<Point3d>();
            List<Point3d> yAxisP = new List<Point3d>();
            for (int i = 0; i < posX.Count; i++)
            {
                xAxisP.Add(new Point3d(posX[i], yMinB, 0));
            }
            for (int i = 0; i < posY.Count; i++)
            {
                yAxisP.Add(new Point3d(xMaxB, posY[i], 0));
            }

            //make dimensions
            dimensions = new List<Dimension>();

            int counter = 0;
            for (int i = 0; i < xAxisP.Count - 1; i++)
            {
                List<Point3d> endPts = new List<Point3d>();
                endPts.Add(xAxisP[i]);
                endPts.Add(xAxisP[i + 1]);
                if (endPts[0].DistanceTo(endPts[1]) >= 50)
                {
                    bool isUpper = true;
                    if (counter % 2 == 0)
                        isUpper = false;
                    dimensions.Add(new Dimension(endPts, new Point3d(posX[i], yMin, 0), 2000, isUpper));
                    counter++;
                }
            }

            counter = 0;
            for (int i = 0; i < yAxisP.Count - 1; i++)
            {
                List<Point3d> endPts = new List<Point3d>();
                endPts.Add(yAxisP[i]);
                endPts.Add(yAxisP[i + 1]);
                if (endPts[0].DistanceTo(endPts[1]) >= 50)
                {
                    bool isUpper = true;
                    if (counter % 2 == 0)
                        isUpper = false;
                    dimensions.Add(new Dimension(endPts, new Point3d(xMax, posY[i], 0), 2000, isUpper));
                    counter++;
                }

            }

        }
        private List<Curve> curveConverter(Curve curve, List<double> parameters)
        {
            int token = 0;
            double length = 0;
            List<double> splitParams = new List<double>();
            while (length < curve.GetLength())
            {
                double t;
                length += parameters[token % (parameters.Count)];
                curve.LengthParameter(length, out t);
                splitParams.Add(t);
                token += 1;
            }

            List<Curve> segs = curve.Split(splitParams).ToList();
            List<Curve> output = new List<Curve>();
            for (int i = 0; i < segs.Count; i += 2)
            {
                output.Add(segs[i]);
            }

            return output;
        }
        private Polyline outlineMaker(double xa, double xb, double ya, double yb, Vector3d x, Vector3d y, Point3d ori)
        {
            //create house outline
            List<Point3d> outlinePoints = new List<Point3d>();
            Point3d pt = Point3d.Origin;

            outlinePoints.Add(pt);
            pt.Transform(Transform.Translation(y * yb));
            outlinePoints.Add(pt);
            pt.Transform(Transform.Translation(x * (xa - xb)));
            outlinePoints.Add(pt);
            pt.Transform(Transform.Translation(y * (-ya)));
            outlinePoints.Add(pt);
            pt.Transform(Transform.Translation(x * (-xa)));
            outlinePoints.Add(pt);
            pt.Transform(Transform.Translation(y * (ya - yb)));
            outlinePoints.Add(pt);
            outlinePoints = Point3d.CullDuplicates(outlinePoints, 1).ToList();
            pt.Transform(Transform.Translation(x * xb));
            outlinePoints.Add(pt);

            Polyline output = new Polyline(outlinePoints);
            output.Transform(Transform.Translation(new Vector3d(ori)));

            return output;
        }
        private Polyline outline4CenterLine(Polyline outline, List<double> wallFactor, double exWallThickness)
        {
            //make list of lines
            List<Point3d> pts = outline.ToList();
            List<Line> lines = new List<Line>();
            for (int i = 0; i < pts.Count - 1; i++)
            {
                lines.Add(new Line(pts[i], pts[i + 1]));
            }

            //shift line list
            List<Line> linesTemp = new List<Line>();
            for (int i = 0; i < lines.Count; i++)
            {
                linesTemp.Add(lines[i]);
            }
            lines = new List<Line>(linesTemp);

            //offset lines and add the first to last(for intersect lineline operation)
            linesTemp.Clear();
            for (int i = 0; i < lines.Count; i++)
            {
                Vector3d vec = lines[i].UnitTangent;
                vec.Rotate(-Math.PI / 2, Vector3d.ZAxis);
                Line lineTemp = lines[i];
                if (wallFactor[i] == 1)
                    lineTemp.Transform(Transform.Translation(vec * exWallThickness / 2));
                else if (wallFactor[i] == 0)
                    lineTemp.Transform(Transform.Translation(-vec * exWallThickness / 2));

                linesTemp.Add(lineTemp);

            }
            linesTemp.Add(linesTemp[0]);
            lines = new List<Line>(linesTemp);

            //intersection points
            List<Point3d> ptsNew = new List<Point3d>();
            for (int i = 0; i < lines.Count - 1; i++)
            {
                double a, b;
                bool check = Rhino.Geometry.Intersect.Intersection.LineLine(lines[i], lines[i + 1], out a, out b);
                if (check)
                    ptsNew.Add(lines[i].PointAt(a));
                else
                    ptsNew.Add(lines[i].PointAt(1));
            }
            ptsNew.Add(ptsNew[0]);

            return new Polyline(ptsNew);
        }

        //////////////////////////
        /////  balcony lines  ////
        //////////////////////////

        private List<List<Curve>> balconyLineMaker(HouseholdProperties hhp, double exWallThickness)
        {
            Polyline outline = outlineMaker(hhp.XLengthA, hhp.XLengthB, hhp.YLengthA, hhp.YLengthB, hhp.XDirection, hhp.YDirection, hhp.Origin);

            //find balcony lines
            List<Curve> balcony = new List<Curve>();
            foreach (Line line in hhp.LightingEdge)
            {
                Vector3d verticalVec = line.UnitTangent;
                verticalVec.Rotate(Math.PI / 2, Vector3d.ZAxis);
                Point3d testPoint = line.PointAt(0.5);
                testPoint.Transform(Transform.Translation(verticalVec));
                if (outline.ToNurbsCurve().Contains(testPoint) == PointContainment.Outside)
                    verticalVec = -verticalVec;

                Curve temp = line.ToNurbsCurve();
                temp.Trim(new Interval(Math.Min(temp.Domain.T0, temp.Domain.T1) + exWallThickness, Math.Max(temp.Domain.T0, temp.Domain.T1) - exWallThickness));
                temp.Translate(verticalVec * Consts.balconyDepth);
                balcony.Add(temp);
            }

            //convert curves into hidden lines
            List<List<Curve>> output = new List<List<Curve>>();
            List<double> hiddenLines = new List<double>(new double[] { 150, 150 });
            foreach (Curve curve in balcony)
            {
                output.Add(curveConverter(curve, hiddenLines));
            }

            return output;
        }
    }
}