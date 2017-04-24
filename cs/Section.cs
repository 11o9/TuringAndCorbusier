using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rhino.Geometry;
using Rhino.Collections;

namespace TuringAndCorbusier
{
    class Section
    {
        private Section(List<Curve> Boundary, List<Curve> Room, List<Curve> Core, List<Surroundinginfo> surrounding, List<RoomNamecard> roomNamecard)
        {
            this.Boundary = Boundary;
            this.Room = Room;
            this.roomNamecard = roomNamecard;
            this.Core = Core;
            this.Surrounding = surrounding;
        }

        public List<Curve> Boundary { get; private set; }
        public List<Curve> Room { get; private set; }
        public List<Curve> Core { get; private set; }
        public List<Surroundinginfo> Surrounding { get; private set; }
        public List<RoomNamecard> roomNamecard { get; private set; }

        public static Section drawSection(List<Curve> baseCurve, List<List<List<HouseholdProperties>>> households, List<List<CoreProperties>> cores, Plot plot)
        {
            double storyHeight = 2800;
            double floorLow = 200;
            double wallThickness = 200;

            List<int> index = new List<int>();

            Curve perpCurve = new LineCurve(Point3d.Origin, Point3d.Origin);

            List<Curve> Boundary = new List<Curve>();
            List<Curve> Room = new List<Curve>();
            List<Curve> Core = new List<Curve>();
            List<RoomNamecard> roomName = new List<RoomNamecard>();

            List<Curve> JoinedBoundaryCrv = new List<Curve>();
            List<Curve> CoreBase = new List<Curve>();

            List<double> uniqueParameter = getUniqueParameter(baseCurve, plot, out perpCurve, out index);

            Curve groundCurve = new LineCurve(Point3d.Origin, new Point3d(perpCurve.GetLength(), 0, 0));

            for (int i = 0; i < index.Count(); i++)
            {
                CoreProperties tempCoreProperty = cores[index[i]][0];

                Point3d tempCoreStart = tempCoreProperty.Origin;
                Point3d tempCoreEnd = tempCoreProperty.Origin + tempCoreProperty.YDirection * tempCoreProperty.CoreType.GetDepth();

                double tempStartParam; double tempEndParam;

                perpCurve.ClosestPoint(tempCoreStart, out tempStartParam);
                perpCurve.ClosestPoint(tempCoreEnd, out tempEndParam);

                Curve tempCoreBase = new LineCurve(groundCurve.PointAt(tempStartParam), groundCurve.PointAt(tempEndParam));

                CoreBase.Add(offsetOneSide(tempCoreBase, Consts.PilotiHeight + Consts.FloorHeight * (tempCoreProperty.Stories + 1)));
            }


            for (int i = 0; i < uniqueParameter.Count(); i++)
            {
                List<Brep> boundary = new List<Brep>();
                int tempIntersectIndex = 0;

                for(int j = 0; j < households[i].Count(); j++)
                {
                    HouseholdProperties tempHousehold = households[i][j][tempIntersectIndex];
                    double widthAsParameter = tempHousehold.YLengthA * (groundCurve.Domain.T1 - groundCurve.Domain.T0) / groundCurve.GetLength();

                    Point3d tempStart = groundCurve.PointAt(uniqueParameter[i] - widthAsParameter / 2);
                    Point3d tempEnd = groundCurve.PointAt(uniqueParameter[i] + widthAsParameter / 2);

                    Curve tempBase = new LineCurve(tempStart, tempEnd);
                    tempBase.Transform(Transform.Translation(new Vector3d(0, tempHousehold.Origin.Z, 0)));                
                    
                    Curve tempLoftBase = tempBase.DuplicateCurve();
                    tempLoftBase.Transform(Transform.Translation(new Vector3d(0, storyHeight, 0)));

                    Curve[] tempLoftBaseSet = { tempBase, tempLoftBase };
                    Brep tempLoftedBrep = Brep.CreateFromLoft(tempLoftBaseSet, Point3d.Unset, Point3d.Unset, LoftType.Straight, false)[0];
                    boundary.Add(tempLoftedBrep);

                    Curve roomBase = tempBase.DuplicateCurve();
                    roomBase.Transform(Transform.Translation(new Vector3d(0, floorLow, 0)));

                    double tempRoombaseStart; double tempRoombaseEnd;

                    roomBase.LengthParameter(wallThickness, out tempRoombaseStart);
                    roomBase.LengthParameter(roomBase.GetLength() - wallThickness, out tempRoombaseEnd);

                    roomBase = new LineCurve(roomBase.PointAt(tempRoombaseStart), roomBase.PointAt(tempRoombaseEnd));

                    Curve room = offsetOneSide(roomBase, storyHeight - floorLow * 2);

                    RoomNamecard tempNamecard = RoomNamecard.CreateRoomNamecard(roomBase.PointAt(roomBase.Domain.Mid) + new Point3d(0, (storyHeight - floorLow * 2) / 2, 0), tempHousehold.GetArea(), storyHeight - floorLow * 2);

                    Room.Add(room);
                    roomName.Add(tempNamecard);
                    
                }
                
                Brep[] joinedBoundary = Brep.JoinBreps(boundary, 0.1);

                foreach (Brep j in joinedBoundary)
                {
                    Curve[] tempNakedEdge = j.DuplicateNakedEdgeCurves(true, true);

                    Boundary.AddRange(tempNakedEdge.ToList());

                    JoinedBoundaryCrv.AddRange(Curve.JoinCurves(tempNakedEdge).ToList());
                }
            }

            List<Surroundinginfo> surrounding = checkSurrounding(perpCurve, plot, groundCurve);

            return new Section(Boundary, Room, Core, surrounding, roomName);
        }

        private static List<Curve> getOuterCurve(Curve baseCurve, List<Curve> boundaryCurve)
        {
            List<Curve> output = new List<Curve>();
            output.Add(baseCurve);

            for (int i = 0; i < boundaryCurve.Count(); i++)
            {
                List<Curve> tempOutput = new List<Curve>();

                Curve tempBoundary = boundaryCurve[i];

                if (tempBoundary.IsClosed == true)
                {
                    for (int j = 0; j < output.Count(); j++)
                    {
                        Curve tempTarget = output[j];

                        var tempIntersection = Rhino.Geometry.Intersect.Intersection.CurveCurve(tempTarget, tempBoundary, 0, 0);

                        List<double> IntersectParam = new List<double>();

                        foreach (Rhino.Geometry.Intersect.IntersectionEvent k in tempIntersection)
                            IntersectParam.Add(k.ParameterA);

                        Curve[] shatteredCurve = tempTarget.Split(IntersectParam);

                        foreach (Curve k in shatteredCurve)
                        {
                            if (tempBoundary.Contains(k.PointAt(k.Domain.Mid)) == PointContainment.Outside)
                            {
                                tempOutput.Add(k);
                            }
                        }
                    }
                }

                output = tempOutput;
            }

            return output;
        }

        private static List<Surroundinginfo> checkSurrounding(Curve perpCurve, Plot plot, Curve targetCurve)
        {
            List<Surroundinginfo> output = new List<Surroundinginfo>();

            if (perpCurve.ClosedCurveOrientation(Vector3d.ZAxis) != CurveOrientation.CounterClockwise)
            {
                perpCurve.Reverse();
            }

            Curve[] plotSegments = plot.Boundary.DuplicateSegments();

            List<double> intersectParameter = new List<double>();
            List<int> intersectIndex = new List<int>();
            List<double> offsetDistance = new List<double>();

            for (int i = 0; i < plotSegments.Count(); i++)
            {
                var tempIntersection = Rhino.Geometry.Intersect.Intersection.CurveCurve(perpCurve, plotSegments[i], 0, 0);

                if (tempIntersection.Count() != 0)
                {
                    Vector3d perpCurveVector = new Vector3d(perpCurve.PointAt(perpCurve.Domain.T1) - perpCurve.PointAt(perpCurve.Domain.T0));
                    Vector3d tempCurveVector = new Vector3d(plotSegments[i].PointAt(plotSegments[i].Domain.T1) - plotSegments[i].PointAt(plotSegments[i].Domain.T0) );

                    offsetDistance.Add(plot.Surroundings[i] * 1000 * perpCurveVector.Length * tempCurveVector.Length / Vector3d.Multiply(perpCurveVector, tempCurveVector));

                    intersectParameter.Add(tempIntersection[0].ParameterA);

                    intersectIndex.Add(i);
                }
            }

            /// offsetDIstance 계산부

            if (intersectParameter.Count() < 2)
                return new List<Surroundinginfo>();
            else
            {
                List<double> CopyIntersectParameter = new List<double>(intersectParameter);
                CopyIntersectParameter.Sort();

                double[] copyIntersectMinMax = { CopyIntersectParameter[0], CopyIntersectParameter[CopyIntersectParameter.Count() - 1] };
                int[] copyIntersectIndex = new int[copyIntersectMinMax.Length];
                double[] copyOffsetDistance = new double[copyIntersectMinMax.Length];

                for(int i = 0; i < copyIntersectMinMax.Count(); i++)
                {
                    copyIntersectIndex[i] = intersectParameter.IndexOf(copyIntersectMinMax[i]);
                    copyOffsetDistance[i] = offsetDistance[copyIntersectIndex[i]];
                }

                for(int i = 0; i < intersectParameter.Count(); i++)
                {
                    string tempString = "인접대지경계";

                    if (copyOffsetDistance[i] != 0)
                        tempString = "대지경계";

                    Point3d basePoint = targetCurve.PointAt(intersectParameter[i]);
                    Point3d baseEnd = new Point3d(basePoint.X, basePoint.Y + 3300, basePoint.Z);

                    Surroundinginfo tempInfo = new Surroundinginfo(new LineCurve(basePoint, baseEnd), tempString);

                    output.Add(tempInfo);
                }

                for(int i = 0; i< intersectParameter.Count(); i++)
                {
                    if (copyOffsetDistance[i] > 0)
                    {
                        int multiplyFactor = (int)Math.Pow(-1, i);
                        string tempString = "인접도로 경계";

                        Point3d basePoint = targetCurve.PointAt(multiplyFactor * copyOffsetDistance[i]);
                        Point3d baseEnd = new Point3d(basePoint.X, basePoint.Y + 3300, basePoint.Z);

                        Surroundinginfo tempInfo = new Surroundinginfo(new LineCurve(basePoint, baseEnd), tempString);

                        output.Add(tempInfo);
                    }
                }
            }

            return output;

        }


        private static Curve offsetOneSide(Curve baseCurve, double offsetDIstance)
        {
            Curve offsettedCurve = baseCurve.DuplicateCurve();
            offsettedCurve.Transform(Transform.Translation(new Vector3d(0, offsetDIstance, 0)));

            Point3d ptA = offsettedCurve.PointAt(offsettedCurve.Domain.T0);
            Point3d ptB = offsettedCurve.PointAt(offsettedCurve.Domain.T1);
            Point3d ptC = baseCurve.PointAt(baseCurve.Domain.T1);
            Point3d ptD = baseCurve.PointAt(baseCurve.Domain.T0);

            Curve thirdCurve = new LineCurve(offsettedCurve.PointAt(offsettedCurve.Domain.T0), baseCurve.PointAt(baseCurve.Domain.T0));
            Curve fourthCurve = new LineCurve(offsettedCurve.PointAt(offsettedCurve.Domain.T1), baseCurve.PointAt(baseCurve.Domain.T1));

            Point3d[] ptList = { ptA, ptB, ptC, ptD, ptA };

            return new Polyline(ptList).ToNurbsCurve();
        }

        private static List<int> getUniqueIndexByHeight(List<HouseholdProperties> households)
        {
            List<int> output = new List<int>();

            List<double> height = new List<double>();

            foreach (HouseholdProperties i in households)
                height.Add(i.Origin.Z);

            List<double> culledHeight = height.Distinct().ToList();

            foreach (double i in culledHeight)
            {
                List<int> tempIndex = EveryIndexOf(height, i);

                int tempReturnIndex = -1;
                double tempMinWidth = double.MaxValue;

                foreach (int j in tempIndex)
                {
                    if (households[j].YLengthA < tempMinWidth)
                    {
                        tempMinWidth = households[j].YLengthA;
                        tempReturnIndex = j;
                    }
                }

                output.Add(tempReturnIndex);
            }

            return output;
        }

        private static List<int> EveryIndexOf(List<double> source, double target)
        {
            List<int> output = new List<int>();

            for (int i = 0; i < source.Count(); i++)
            {
                if (source[i] == target)
                {
                    output.Add(i);
                }
            }

            return output;
        }

        private static List<double> getUniqueParameter(List<Curve> baseCurve, Plot plot, out Curve perpendicularCurve, out List<int> indexOfTheParameter)
        {
            List<double> output = new List<double>();

            //////////////////////////////////////////

            BoundingBox boundaryBox = plot.Boundary.GetBoundingBox(false);
            double extendLength = boundaryBox.Corner(true, true, true).DistanceTo(boundaryBox.Corner(false, false, true));

            Curve tempCurve = baseCurve[(int)(baseCurve.Count() / 2)];
            Point3d centerPoint = tempCurve.PointAt(tempCurve.Domain.Mid);
            Vector3d centerPointPerpVector = tempCurve.TangentAt(tempCurve.Domain.Mid);
            centerPointPerpVector.Transform(Transform.Rotation(Math.PI / 2, Point3d.Origin));
            perpendicularCurve = new LineCurve(centerPoint, centerPoint + centerPointPerpVector);

            double perpCurveStart = -extendLength; double perpCurveEnd = extendLength;

            perpendicularCurve = new LineCurve(perpendicularCurve.PointAt(perpCurveStart), perpendicularCurve.PointAt(perpCurveEnd));

            List<Point3d> pointOnPerpCurve = new List<Point3d>();

            foreach (Curve i in baseCurve)
            {
                double tempParameter;
                perpendicularCurve.ClosestPoint(i.PointAt(i.Domain.Mid), out tempParameter);

                pointOnPerpCurve.Add(perpendicularCurve.PointAt(tempParameter));
            }

            List<Point3d> uniquePointOnPerpCurve = Point3d.CullDuplicates(pointOnPerpCurve, 1000).ToList();
            List<double> uniquePointParameter = new List<double>();

            foreach (Point3d i in uniquePointOnPerpCurve)
            {
                double tempParameter;
                perpendicularCurve.ClosestPoint(i, out tempParameter);

                uniquePointParameter.Add(tempParameter);
            }

            RhinoList<Point3d> rhinoUniquePointOnPerpCurve = new RhinoList<Point3d>(uniquePointOnPerpCurve);

            rhinoUniquePointOnPerpCurve.Sort(uniquePointParameter.ToArray());
            uniquePointParameter.Sort();

            List<int> tempIndexOfParameter = new List<int>();

            foreach (Point3d i in rhinoUniquePointOnPerpCurve)
            {
                int index = pointOnPerpCurve.IndexOf(i);

                tempIndexOfParameter.Add(index);
            }

            ///

            indexOfTheParameter = tempIndexOfParameter;

            return uniquePointParameter;
        }


    }

    public class Surroundinginfo
    {
        public Surroundinginfo(Curve curve, string info)
        {
            this.curve = curve;
            this.SurroundingInfo = info;
        }

        public Curve curve { get; private set; }
        public string SurroundingInfo { get; private set; }
    }

    public class RoomNamecard
    {
        private RoomNamecard(List<Curve> Form, List<Rhino.Display.Text3d> Text)
        {
            this.Form = Form;
            this.Text = Text;
        }

        public List<Curve> Form { get; private set; }
        public List<Rhino.Display.Text3d> Text { get; private set; }

        public static RoomNamecard CreateRoomNamecard(Point3d CenterPoint, double RoomSize_squareMilimeter, double storiesHeight)
        {
            List<Curve> Form = CreateNamecardForm(CenterPoint);

            Rhino.Display.Text3d roomType = centeredText("주거시설", 400, CenterPoint + new Point3d(0, 250, 0));
            Rhino.Display.Text3d roomSize = centeredText(Math.Round(RoomSize_squareMilimeter / 1000000, 0).ToString() + "m\xB2", 400, CenterPoint + new Point3d(-500, -250, 0));
            Rhino.Display.Text3d roomHeight = centeredText("CH : " + Math.Round(storiesHeight, 0).ToString(), 400, CenterPoint + new Point3d(-500, -250, 0));

            Rhino.Display.Text3d[] text = { roomType, roomSize, roomHeight };

            return new RoomNamecard(Form, text.ToList());
        }

        private static Rhino.Display.Text3d centeredText(string text, double height, Point3d center)
        {
            Rhino.Display.Text3d tempRoomType = new Rhino.Display.Text3d(text, Plane.WorldXY, height);

            Vector3d tempVector = new Vector3d(center - tempRoomType.BoundingBox.Center);

            tempRoomType.TextPlane = new Plane(tempRoomType.TextPlane.Origin + tempVector, Vector3d.ZAxis);

            return tempRoomType;
        }


        private static List<Curve> CreateNamecardForm(Point3d CenterPoint)
        {
            List<Curve> output = new List<Curve>();

            Rectangle3d outLine = new Rectangle3d(Plane.WorldXY, new Interval(-2000, 2000), new Interval(-500, 500));

            List<Curve> box = outLine.ToNurbsCurve().DuplicateSegments().ToList();
            output.AddRange(box);

            output.Add(new LineCurve(new Point3d(-2000, 0, 0), new Point3d(2000, 0, 0)));

            output.Add(new LineCurve(Point3d.Origin, new Point3d(0, -500, 0)));

            for(int i = 0; i < output.Count(); i++)
            {
                Curve tempCurveToMove = output[i].DuplicateCurve();

                tempCurveToMove.Transform(Transform.Translation(new Vector3d(CenterPoint)));
                output[i] = tempCurveToMove;
            }

            return output;
        }
        
    }
}
