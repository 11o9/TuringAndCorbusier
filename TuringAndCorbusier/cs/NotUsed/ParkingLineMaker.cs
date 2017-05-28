using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rhino.Geometry;

namespace TuringAndCorbusier
{
    public class ParkingLineMaker
    {

        public static List<List<ParkingLine>> parkingLineMaker(string AgType, List<List<Core>> cores, Plot plot, double ApartmentWidth, Curve CenterCurve, out List<ParkingLotType> parkingLotType, out List<Curve> roadCenterCurve, out double angle)
        {
            roadCenterCurve = new List<Curve>();
            parkingLotType = new List<ParkingLotType>();
            angle = 0;

            try
            {
                List<Curve> coreBoundary = new List<Curve>();

                foreach (List<Core> i in cores)
                {
                    foreach (Core j in i)
                    {
                        coreBoundary.Add(GetCoreBoundary(j));
                    }
                }

                if (AgType == "PT-1" || AgType == "PT-3")
                {
                    List<List<Core>> tempCore = new List<List<Core>>(cores);

                    if (AgType == "PT-3")
                        tempCore = AlignCore_PT3(cores);

                    List<double> outDist;
                    Curve outFirstCurve;
                    double coreDepth;
                    double tempAngle = 0;
                    ParkingLotBaseLineMaker_XDirection(tempCore, plot.Boundary, out outDist, out outFirstCurve, out coreDepth, out tempAngle);

                    List<Curve> tempRoadCenter = new List<Curve>();
                    List<ParkingLotType> tempParkingLotType = new List<ParkingLotType>();
                    List<List<ParkingLine>> tempParkingLines = parkingLineMaker_PT1_PT3(cores, outDist, coreDepth, outFirstCurve, plot, out tempParkingLotType, out tempRoadCenter);

                    roadCenterCurve = tempRoadCenter;
                    parkingLotType = tempParkingLotType;
                    angle = tempAngle;

                    return tempParkingLines;
                }
                else if (AgType == "PT-4")
                {
                    List<ParkingLotType> outParkingLotType = new List<ParkingLotType>();
                    List<List<ParkingLine>> tempParkingLines = parkingLineMaker_PT4(cores, ApartmentWidth, CenterCurve, plot, out outParkingLotType);

                    parkingLotType = outParkingLotType;
                    return tempParkingLines;
                }
                else
                {
                    return new List<List<ParkingLine>>();
                }
            }
            catch (Exception)
            {
                return new List<List<ParkingLine>>();
            }

        }

        public static List<List<ParkingLine>> parkingLineMaker(string AgType, List<List<Core>> cores, Plot plot, double ApartmentWidth, Curve CenterCurve)
        {
            try
            {
                List<Curve> coreBoundary = new List<Curve>();

                foreach (List<Core> i in cores)
                {
                    foreach (Core j in i)
                    {
                        coreBoundary.Add(GetCoreBoundary(j));
                    }
                }

                if (AgType == "PT-1" || AgType == "PT-3")
                {
                    List<List<Core>> tempCore = new List<List<Core>>(cores);

                    if (AgType == "PT-3")
                        tempCore = AlignCore_PT3(cores);

                    List<double> outDist;
                    Curve outFirstCurve;
                    double coreDepth;
                    double Angle;
                    ParkingLotBaseLineMaker_XDirection(tempCore, plot.Boundary, out outDist, out outFirstCurve, out coreDepth, out Angle);

                    List<List<ParkingLine>> tempParkingLines = parkingLineMaker_PT1_PT3(cores, outDist, coreDepth, outFirstCurve, plot);
                    
                    return tempParkingLines;
                }
                else if (AgType == "PT-4")
                {
                    List<ParkingLotType> outParkingLotType = new List<ParkingLotType>();
                    List<List<ParkingLine>> tempParkingLines = parkingLineMaker_PT4(cores, ApartmentWidth, CenterCurve, plot, out outParkingLotType);

                    return tempParkingLines;
                }
                else
                {
                    return new List<List<ParkingLine>>();
                }
            }
            catch (Exception)
            {
                return new List<List<ParkingLine>>();
            }

        }

        private static List<List<ParkingLine>> parkingLineMaker_PT1_PT3(List<List<Core>> core, List<double> distance, double coreDepth, Curve firstLine, Plot plot, out List<ParkingLotType> parkingType, out List<Curve> roadCenter)
        {
            List<ParkingLine> parkingLots = new List<ParkingLine>();

            List<double> parkingLotOffset = new List<double>();
            parkingLotOffset.Add(0);
            List<ParkingLotType> parkingLotType = new List<ParkingLotType>();
            parkingLotType.Add(ParkingLotType.perpendicular);
            List<RoadDirection> roadDirection = new List<RoadDirection>();
            roadDirection.Add(RoadDirection.backward);

            bool currentEncounterRoad = false;
            int currentDistanceNum = 0;

            int testInfLoop = 0;
            while (currentDistanceNum < distance.Count())
            {
                //Rhino.RhinoApp.WriteLine("parkingLineMaker_PT1_PT3");

                double lastOffset = parkingLotOffset[parkingLotOffset.Count() - 1];
                double l = distance[currentDistanceNum] - lastOffset - coreDepth;

                if (distance[currentDistanceNum] - lastOffset < 5000)
                {
                    currentDistanceNum = currentDistanceNum + 1;
                }
                else
                {
                    if (currentEncounterRoad == false)
                    {
                        if (l > 17000)
                        {
                            parkingLotOffset.Add(lastOffset + 11000);
                            parkingLotType.Add(ParkingLotType.perpendicular);
                            roadDirection.Add(RoadDirection.forward);
                            currentEncounterRoad = true;
                        }
                        else if (l > 14000)
                        {
                            parkingLotOffset.Add(lastOffset + 8000);
                            parkingLotType.Add(ParkingLotType.parallel);
                            roadDirection.Add(RoadDirection.forward);
                            currentEncounterRoad = true;
                        }
                        else
                        {
                            if (l + coreDepth > 16000)
                            {
                                parkingLotOffset.Add(distance[currentDistanceNum] - 5000);
                                parkingLotOffset.Add(distance[currentDistanceNum]);
                                parkingLotType.Add(ParkingLotType.perpendicular);
                                parkingLotType.Add(ParkingLotType.perpendicular);
                                roadDirection.Add(RoadDirection.forward);
                                roadDirection.Add(RoadDirection.backward);
                                currentEncounterRoad = false;
                            }
                            else if (l > 3000)
                            {
                                parkingLotOffset.Add(lastOffset + 11000);
                                parkingLotOffset.Add(lastOffset + 16000);
                                parkingLotType.Add(ParkingLotType.perpendicular);
                                parkingLotType.Add(ParkingLotType.perpendicular);
                                roadDirection.Add(RoadDirection.forward);
                                roadDirection.Add(RoadDirection.backward);
                                currentEncounterRoad = false;

                            }
                        }
                    }
                    else
                    {
                        if (l > 8000)
                        {
                            parkingLotOffset.Add(lastOffset + 5000);
                            parkingLotType.Add(ParkingLotType.perpendicular);
                            roadDirection.Add(RoadDirection.backward);
                            currentEncounterRoad = false;
                        }
                        else if (l > 6000)
                        {
                            parkingLotOffset.Add(lastOffset + 11000);
                            parkingLotType.Add(ParkingLotType.perpendicular);
                            roadDirection.Add(RoadDirection.forward);
                            currentEncounterRoad = true;
                        }
                    }

                    lastOffset = parkingLotOffset[parkingLotOffset.Count() - 1];

                    if (lastOffset > distance[currentDistanceNum])
                    {
                        currentDistanceNum = currentDistanceNum + 1;
                    }
                }
                testInfLoop += 1;

                if (testInfLoop > 100)
                {
                    roadCenter = new List<Curve>();
                    parkingType = new List<ParkingLotType>();
                    return new List<List<ParkingLine>>();
                }
            }

            //AddFirstCurve

            parkingLotOffset.Add(-5000);
            parkingLotType.Add(ParkingLotType.perpendicular);
            roadDirection.Add(RoadDirection.forward);

            parkingLotOffset.Add(-16000);
            parkingLotType.Add(ParkingLotType.perpendicular);
            roadDirection.Add(RoadDirection.backward);

            List<Curve> usableCurveBase = new List<Curve>();

            foreach (double i in parkingLotOffset)
            {
                if (i == 0)
                    usableCurveBase.Add(firstLine);
                else
                    usableCurveBase.Add(firstLine.Offset(Plane.WorldXY, i, 0, CurveOffsetCornerStyle.None)[0]);
            }

            List<Curve> roadCenterCurve = new List<Curve>();

            for (int i = 0; i < usableCurveBase.Count(); i++)
            {
                double tempRoadWidth = 6000;

                if (parkingLotType[i] == ParkingLotType.parallel)
                    tempRoadWidth = 3000;

                if (roadDirection[i] == RoadDirection.forward)
                    tempRoadWidth *= -1;

                roadCenterCurve.Add(usableCurveBase[i].Offset(Plane.WorldXY, tempRoadWidth / 2, 0, CurveOffsetCornerStyle.None)[0]);
            }

            roadCenter = roadCenterCurve;

            List<Curve> coreBoundaryList = new List<Curve>();

            for (int i = 0; i < core.Count(); i++)
            {
                for (int j = 0; j < core[i].Count(); j++)
                {
                    coreBoundaryList.Add(GetCoreBoundary(core[i][j]));
                }
            }

            List<List<ParkingLine>> output = new List<List<ParkingLine>>();
            List<ParkingLotType> outParkingType = new List<ParkingLotType>();

            for (int i = 0; i < usableCurveBase.Count; i++)
            {
                List<Curve> parkingLotAvailableCurve = FindParkingLotAvailableCurve(usableCurveBase[i], coreBoundaryList, plot, 20000, parkingLotType[i], roadDirection[i]);

                for (int j = 0; j < parkingLotAvailableCurve.Count(); j++)
                {
                    if (plot.Boundary.Contains(parkingLotAvailableCurve[j].PointAt(parkingLotAvailableCurve[j].Domain.Mid)) == PointContainment.Inside)
                    {
                        output.Add(createParkingLine(parkingLotAvailableCurve[j], parkingLotType[i]));
                        outParkingType.Add(parkingLotType[i]);
                    }
                }
            }

            parkingType = outParkingType;

            return output;
        }

        private static List<List<ParkingLine>> parkingLineMaker_PT4(List<List<Core>> core, double apartmentWidth, Curve CenterCurve, Plot plot, out List<ParkingLotType> parkingLotType)
        {

            Curve newCenter = alignOpendCurveOrientation(CenterCurve);

            List<Curve> offsettedCenters = new List<Curve>();
            List<double> offsettedCenterLength = new List<double>();

            for (int i = -1; i < 2; i += 2)
            {
                Curve[] tempOffsettedCurves = newCenter.Offset(Plane.WorldXY, apartmentWidth / 2 * i, 0, CurveOffsetCornerStyle.Sharp);

                tempOffsettedCurves = Curve.JoinCurves(tempOffsettedCurves);
                List<double> offsettedCurvesLength = (from j in tempOffsettedCurves
                                                      select j.GetLength()).ToList();

                offsettedCenters.Add(tempOffsettedCurves[offsettedCurvesLength.IndexOf(offsettedCurvesLength.Max())]);

                offsettedCenterLength.Add(offsettedCurvesLength.Min());
            }

            Curve usableCurve = offsettedCenters[offsettedCenterLength.IndexOf(offsettedCenterLength.Min())];
            Curve tempExtendedUsableCurve = usableCurve.Extend(CurveEnd.Both, CurveExtensionStyle.Line, new List<Curve>(new Curve[] { plot.Boundary }));

            usableCurve = simplifyExtendedCurve(usableCurve, tempExtendedUsableCurve);

            List<Curve> usableCurveSegments = usableCurve.DuplicateSegments().ToList();
            Curve secondUsableCurveSegment = usableCurveSegments[1];
            usableCurveSegments.RemoveAt(1);

            int MainRoadMakingSegmentIndex = 0;

            if (usableCurveSegments.Count >= 2)
            {
                List<double> offsettedCurveSegmentDistanceToBoundary = new List<double>();

                for (int i = 0; i < usableCurveSegments.Count(); i++)
                {
                    Curve tempOffsettedCurve = offsetByDistance(usableCurveSegments[i], 11000);

                    var tempIntersection = Rhino.Geometry.Intersect.Intersection.CurveCurve(tempOffsettedCurve, plot.Boundary, 0, 0);

                    if (tempIntersection.Count() != 0)
                    {
                        List<double> intersectionParams = (from j in tempIntersection
                                                           select j.ParameterB).ToList();

                        Curve[] shatteredBoundary = plot.Boundary.Split(intersectionParams);

                        double tempDistance = double.MaxValue;

                        foreach (Curve j in shatteredBoundary)
                        {
                            if (tempOffsettedCurve.Contains(j.PointAt(j.Domain.Mid)) != PointContainment.Outside)
                            {
                                Curve[] tempShatteredCurveSegments = j.DuplicateSegments();
                                List<Point3d> tempPointList = (from k in tempShatteredCurveSegments
                                                               select k.PointAt(k.Domain.T0)).ToList();
                                tempPointList.Add(j.PointAt(j.Domain.T1));

                                foreach (Point3d k in tempPointList)
                                {
                                    double tempClosestParameterOut = new double();
                                    usableCurveSegments[i].ClosestPoint(k, out tempClosestParameterOut);

                                    double tempTempDistance = (new LineCurve(k, usableCurveSegments[i].PointAt(tempClosestParameterOut))).GetLength();

                                    if (tempTempDistance <= tempDistance)
                                    {
                                        tempDistance = tempTempDistance;
                                    }
                                }
                            }
                        }

                        offsettedCurveSegmentDistanceToBoundary.Add(tempDistance);
                    }
                    else
                    {
                        offsettedCurveSegmentDistanceToBoundary.Add(double.MaxValue);
                    }
                }

                MainRoadMakingSegmentIndex = offsettedCurveSegmentDistanceToBoundary.IndexOf(offsettedCurveSegmentDistanceToBoundary.Min());
            }

            double MainroadWidth = 5000;
            usableCurveSegments.Insert(1, secondUsableCurveSegment);

            List<Curve> coreBoundaries = new List<Curve>();

            for (int i = 0; i < core.Count(); i++)
            {
                for (int j = 0; j < core[i].Count(); j++)
                {
                    coreBoundaries.Add(GetCoreBoundary(core[i][j]));
                }
            }

            List<ParkingLotType> outParkingLotType = new List<ParkingLotType>();
            List<List<ParkingLine>> outParkingLines = CreateParkingLine_PT4(usableCurveSegments, coreBoundaries, MainroadWidth, MainRoadMakingSegmentIndex, plot, out outParkingLotType);

            parkingLotType = outParkingLotType;

            return outParkingLines;
        }

        public static List<List<ParkingLine>> CreateParkingLine_PT4(List<Curve> centerCurves, List<Curve> Cores, double mainRoadWidth, int mainRoadMakingSegmentIndex, Plot plot, out List<ParkingLotType> outParkingLotType)
        {
            if (centerCurves.Count == 0)
            {
                outParkingLotType = new List<ParkingLotType>();
                return new List<List<ParkingLine>>();
            }

            List<double> centerCurveLength = (from i in centerCurves
                                              select i.GetLength()).ToList();

            List<List<ParkingLine>> output = new List<List<ParkingLine>>();
            List<ParkingLotType> parkingLotType = new List<ParkingLotType>();

            //// 중심 주차장 생성
            {
                Curve secondUsableCurveSegment = centerCurves[1].DuplicateCurve();

                if (mainRoadMakingSegmentIndex == 0)
                    secondUsableCurveSegment = SplitCurveEnd(secondUsableCurveSegment, SplitSide.Start, mainRoadWidth);
                else if (mainRoadMakingSegmentIndex == 1)
                    secondUsableCurveSegment = SplitCurveEnd(secondUsableCurveSegment, SplitSide.End, mainRoadWidth);

                Curve tempChecker = offsetByDistance(secondUsableCurveSegment, centerCurveLength.Max() + 10000);

                var intersection = Rhino.Geometry.Intersect.Intersection.CurveCurve(tempChecker, plot.Boundary, 0, 0);

                List<double> intersectParams = (from i in intersection
                                                select i.ParameterB).ToList();

                Curve[] ShatteredBoundary = tempChecker.Split(intersectParams);

                List<Curve> curveInsideChecker = (from i in ShatteredBoundary
                                                  where tempChecker.Contains(i.PointAt(i.Domain.Mid)) != PointContainment.Outside
                                                  select i).ToList();

                List<Point3d> checkPoints = new List<Point3d>();

                for (int i = 0; i < curveInsideChecker.Count(); i++)
                {
                    Curve[] tempSegments = curveInsideChecker[i].DuplicateSegments();

                    checkPoints.AddRange(from j in tempSegments
                                         select j.PointAt(j.Domain.T0));

                    checkPoints.Add(curveInsideChecker[i].PointAt(curveInsideChecker[i].Domain.T1));
                }

                double tempMaxDistance = double.MinValue;

                for (int i = 0; i < checkPoints.Count(); i++)
                {
                    double tempOutParam;
                    secondUsableCurveSegment.ClosestPoint(checkPoints[i], out tempOutParam);

                    double tempDistance = secondUsableCurveSegment.PointAt(tempOutParam).DistanceTo(checkPoints[i]);

                    if (tempDistance > tempMaxDistance)
                        tempMaxDistance = tempDistance;
                }

                List<Curve> parkingLotBaseLine = new List<Curve>();
                List<ParkingLotType> parkingLotTypes = new List<ParkingLotType>();
                List<RoadDirection> roadDirections = new List<RoadDirection>();

                Curve firstCurve = centerCurves[1].Offset(Plane.WorldXY, -5000, 0, CurveOffsetCornerStyle.None)[0];

                firstCurve.Reverse();
                parkingLotBaseLine.Add(firstCurve);
                parkingLotTypes.Add(ParkingLotType.perpendicular);
                roadDirections.Add(RoadDirection.forward);

                double currentDistance = 11000;

                while (currentDistance <= tempMaxDistance)
                {
                    Curve tempCurve = secondUsableCurveSegment.Offset(Plane.WorldXY, currentDistance, 0, CurveOffsetCornerStyle.None)[0];

                    parkingLotBaseLine.Add(tempCurve);
                    parkingLotTypes.Add(ParkingLotType.perpendicular);
                    roadDirections.Add(RoadDirection.forward);

                    currentDistance += 5000;
                    tempCurve = secondUsableCurveSegment.Offset(Plane.WorldXY, currentDistance, 0, CurveOffsetCornerStyle.None)[0];

                    parkingLotBaseLine.Add(tempCurve);
                    parkingLotTypes.Add(ParkingLotType.perpendicular);
                    roadDirections.Add(RoadDirection.backward);

                    currentDistance += 11000;

                    /*
 

                    for(int i = 0; i < 2; i++)
                    {
                        parkingLotBaseLine.Add(tempCurve);
                        parkingLotTypes.Add(ParkingLotType.perpendicular);                        
                    }
                    roadDirections.Add(RoadDirection.backward);
                    roadDirections.Add(RoadDirection.forward);

                    currentDistance += 6000;
                    */
                }

                List<List<ParkingLine>> tempOutput = new List<List<ParkingLine>>();
                List<ParkingLotType> tempParkingType = new List<ParkingLotType>();

                for (int i = 0; i < parkingLotBaseLine.Count; i++)
                {
                    List<Curve> parkingLotAvailableCurve = FindParkingLotAvailableCurve(parkingLotBaseLine[i], Cores, plot, 0, parkingLotTypes[i], roadDirections[i]);

                    for (int j = 0; j < parkingLotAvailableCurve.Count(); j++)
                    {
                        if (plot.Boundary.Contains(parkingLotAvailableCurve[j].PointAt(parkingLotAvailableCurve[j].Domain.Mid)) == PointContainment.Inside)
                        {
                            tempOutput.Add(createParkingLine(parkingLotAvailableCurve[j], parkingLotTypes[i]));
                            tempParkingType.Add(parkingLotTypes[i]);
                        }
                    }
                }

                output.AddRange(tempOutput);
                parkingLotType.AddRange(tempParkingType);
            }

            if (mainRoadMakingSegmentIndex == 1)
                mainRoadMakingSegmentIndex = 2;

            double MainRoadCurveOffsetDistance = 5000;

            if (mainRoadWidth <= 6000)
                MainRoadCurveOffsetDistance += 6000 - mainRoadWidth;

            Curve mainRoadCurve = centerCurves[mainRoadMakingSegmentIndex];
            mainRoadCurve.Reverse();

            if (MainRoadCurveOffsetDistance != 0)
                mainRoadCurve = centerCurves[mainRoadMakingSegmentIndex].Offset(Plane.WorldXY, MainRoadCurveOffsetDistance, 0, CurveOffsetCornerStyle.None)[0];

            List<Curve> mainroadPArkingLotAvailableCurve = FindParkingLotAvailableCurve(mainRoadCurve, Cores, plot, 0, ParkingLotType.perpendicular, RoadDirection.forward);

            for (int j = 0; j < mainroadPArkingLotAvailableCurve.Count(); j++)
            {
                if (plot.Boundary.Contains(mainroadPArkingLotAvailableCurve[j].PointAt(mainroadPArkingLotAvailableCurve[j].Domain.Mid)) == PointContainment.Inside)
                {
                    output.Add(createParkingLine(mainroadPArkingLotAvailableCurve[j], ParkingLotType.perpendicular));
                    parkingLotType.Add(ParkingLotType.perpendicular);
                }
            }

            if (mainRoadMakingSegmentIndex == 0)
            {
                try
                {
                    Curve subParkingLotCurve = centerCurves[2].Offset(Plane.WorldXY, -11000, 0, CurveOffsetCornerStyle.None)[0];
                    subParkingLotCurve.Reverse();

                    List<Curve> subParkingLotAvailableCurve = FindParkingLotAvailableCurve(subParkingLotCurve, Cores, plot, 0, ParkingLotType.perpendicular, RoadDirection.forward);

                    for (int j = 0; j < subParkingLotAvailableCurve.Count(); j++)
                    {
                        if (plot.Boundary.Contains(subParkingLotAvailableCurve[j].PointAt(subParkingLotAvailableCurve[j].Domain.Mid)) == PointContainment.Inside)
                        {
                            output.Add(createParkingLine(subParkingLotAvailableCurve[j], ParkingLotType.perpendicular));
                            parkingLotType.Add(ParkingLotType.perpendicular);
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show(ex.ToString());
                }
            }
            else if (mainRoadMakingSegmentIndex == 2)
            {
                try
                {
                    Curve subParkingLotCurve = centerCurves[0].Offset(Plane.WorldXY, -11000, 0, CurveOffsetCornerStyle.None)[0];
                    subParkingLotCurve.Reverse();

                    List<Curve> subParkingLotAvailableCurve = FindParkingLotAvailableCurve(subParkingLotCurve, Cores, plot, 0, ParkingLotType.perpendicular, RoadDirection.forward);

                    for (int j = 0; j < subParkingLotAvailableCurve.Count(); j++)
                    {
                        if (plot.Boundary.Contains(subParkingLotAvailableCurve[j].PointAt(subParkingLotAvailableCurve[j].Domain.Mid)) == PointContainment.Inside)
                        {
                            output.Add(createParkingLine(subParkingLotAvailableCurve[j], ParkingLotType.perpendicular));
                            parkingLotType.Add(ParkingLotType.perpendicular);
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show(ex.ToString());
                }
            }

            outParkingLotType = parkingLotType;
            return output;
        }

        public enum SplitSide { Start, End, Both }

        private static Curve SplitCurveEnd(Curve curve, SplitSide splitSide, double length)
        {
            Curve newCurve = curve;

            if (splitSide == SplitSide.Start || splitSide == SplitSide.Both)
            {
                double newStartParameter;
                newCurve.LengthParameter(length, out newStartParameter);

                if (newStartParameter >= newCurve.Domain.T1)
                    return new LineCurve(newCurve.PointAt(newCurve.Domain.T1), newCurve.PointAt(newCurve.Domain.T1));

                Interval newInterval = new Interval(newStartParameter, newCurve.Domain.T1);
                newCurve = newCurve.ToNurbsCurve(newInterval);
            }

            if (splitSide == SplitSide.End || splitSide == SplitSide.Both)
            {
                double newStartParameter;
                newCurve.LengthParameter(newCurve.GetLength() - length, out newStartParameter);

                if (newStartParameter <= newCurve.Domain.T0)
                    return new LineCurve(newCurve.PointAt(newCurve.Domain.T0), newCurve.PointAt(newCurve.Domain.T0));

                Interval newInterval = new Interval(newCurve.Domain.T0, newStartParameter);
                newCurve = newCurve.ToNurbsCurve(newInterval);
            }

            return newCurve;
        }

        private static Curve alignOpendCurveOrientation(Curve baseCurve)
        {
            Curve output = baseCurve.DuplicateCurve();
            Curve newCurveToTest = baseCurve.DuplicateCurve();

            if (newCurveToTest.IsClosed != true)
                newCurveToTest.MakeClosed(0);

            if (newCurveToTest.ClosedCurveOrientation(Vector3d.ZAxis) != CurveOrientation.CounterClockwise)
                output.Reverse();

            return output;
        }

        private static Curve simplifyExtendedCurve(Curve baseCurve, Curve ExtendedCurve)
        {
            List<Point3d> pointSet = new List<Point3d>();

            pointSet.Add(ExtendedCurve.PointAt(ExtendedCurve.Domain.T0));

            Curve[] baseCurveSegments = baseCurve.DuplicateSegments();

            for (int i = 1; i < baseCurveSegments.Count(); i++)
                pointSet.Add(baseCurveSegments[i].PointAt(baseCurveSegments[i].Domain.T0));

            pointSet.Add(ExtendedCurve.PointAt(ExtendedCurve.Domain.T1));

            List<Curve> outputSegments = new List<Curve>();

            for (int i = 0; i < pointSet.Count() - 1; i++)
                outputSegments.Add(new LineCurve(pointSet[i], pointSet[i + 1]));

            return Curve.JoinCurves(outputSegments)[0];
        }

        private static List<List<ParkingLine>> parkingLineMaker_PT1_PT3(List<List<Core>> core, List<double> distance, double coreDepth, Curve firstLine, Plot plot)
        {
            List<ParkingLine> parkingLots = new List<ParkingLine>();

            List<double> parkingLotOffset = new List<double>();
            parkingLotOffset.Add(0);
            List<ParkingLotType> parkingLotType = new List<ParkingLotType>();
            parkingLotType.Add(ParkingLotType.perpendicular);
            List<RoadDirection> roadDirection = new List<RoadDirection>();
            roadDirection.Add(RoadDirection.backward);

            bool currentEncounterRoad = false;
            int currentDistanceNum = 0;

            int testInfLoop = 0;
            while (currentDistanceNum < distance.Count())
            {

                //Rhino.RhinoApp.WriteLine("parkingLineMaker_PT1_PT3");

                double lastOffset = parkingLotOffset[parkingLotOffset.Count() - 1];
                double l = distance[currentDistanceNum] - lastOffset - coreDepth;

                if (distance[currentDistanceNum] - lastOffset < 5000)
                {
                    currentDistanceNum = currentDistanceNum + 1;
                }
                else
                {
                    if (currentEncounterRoad == false)
                    {
                        if (l > 17000)
                        {
                            parkingLotOffset.Add(lastOffset + 11000);
                            parkingLotType.Add(ParkingLotType.perpendicular);
                            roadDirection.Add(RoadDirection.forward);
                            currentEncounterRoad = true;
                        }
                        else if (l > 14000)
                        {
                            parkingLotOffset.Add(lastOffset + 8000);
                            parkingLotType.Add(ParkingLotType.parallel);
                            roadDirection.Add(RoadDirection.forward);
                            currentEncounterRoad = true;
                        }
                        else
                        {
                            if (l + coreDepth > 16000)
                            {
                                parkingLotOffset.Add(distance[currentDistanceNum] - 5000);
                                parkingLotOffset.Add(distance[currentDistanceNum]);
                                parkingLotType.Add(ParkingLotType.perpendicular);
                                parkingLotType.Add(ParkingLotType.perpendicular);
                                roadDirection.Add(RoadDirection.forward);
                                roadDirection.Add(RoadDirection.backward);
                                currentEncounterRoad = false;
                            }
                            else if (l > 3000)
                            {
                                parkingLotOffset.Add(lastOffset + 11000);
                                parkingLotOffset.Add(lastOffset + 16000);
                                parkingLotType.Add(ParkingLotType.perpendicular);
                                parkingLotType.Add(ParkingLotType.perpendicular);
                                roadDirection.Add(RoadDirection.forward);
                                roadDirection.Add(RoadDirection.backward);
                                currentEncounterRoad = false;

                            }
                        }
                    }
                    else
                    {
                        if (l > 8000)
                        {
                            parkingLotOffset.Add(lastOffset + 5000);
                            parkingLotType.Add(ParkingLotType.perpendicular);
                            roadDirection.Add(RoadDirection.backward);
                            currentEncounterRoad = false;
                        }
                        else if (l > 6000)
                        {
                            parkingLotOffset.Add(lastOffset + 11000);
                            parkingLotType.Add(ParkingLotType.perpendicular);
                            roadDirection.Add(RoadDirection.forward);
                            currentEncounterRoad = true;
                        }
                    }

                    lastOffset = parkingLotOffset[parkingLotOffset.Count() - 1];

                    if (lastOffset > distance[currentDistanceNum])
                    {
                        currentDistanceNum = currentDistanceNum + 1;
                    }
                }
                testInfLoop += 1;

                if (testInfLoop > 100)
                {
                    return new List<List<ParkingLine>>();
                }
            }

            //AddFirstCurve

            parkingLotOffset.Add(-5000);
            parkingLotType.Add(ParkingLotType.perpendicular);
            roadDirection.Add(RoadDirection.forward);

            parkingLotOffset.Add(-16000);
            parkingLotType.Add(ParkingLotType.perpendicular);
            roadDirection.Add(RoadDirection.backward);

            List<Curve> usableCurveBase = new List<Curve>();

            foreach (double i in parkingLotOffset)
            {
                if (i == 0)
                    usableCurveBase.Add(firstLine);
                else
                {
                    try
                    {
                        if(firstLine!=null)
                        usableCurveBase.Add(firstLine.Offset(Plane.WorldXY, i, 0, CurveOffsetCornerStyle.None)[0]);

                    }
                    catch (ArgumentNullException e)
                    {
                        continue;
                    }
                }
            }

            if (firstLine == null)
            {
                return new List<List<ParkingLine>>();
            }
            List<Curve> roadCenter = new List<Curve>();

            for (int i = 0; i < usableCurveBase.Count(); i++)
            {
                double tempRoadWidth = 6000;

                if (parkingLotType[i] == ParkingLotType.parallel)
                    tempRoadWidth = 3000;

                if (roadDirection[i] == RoadDirection.forward)
                    tempRoadWidth *= -1;

                roadCenter.Add(usableCurveBase[i].Offset(Plane.WorldXY, tempRoadWidth / 2, 0, CurveOffsetCornerStyle.None)[0]);
            }



            List<Curve> coreBoundaryList = new List<Curve>();

            for (int i = 0; i < core.Count(); i++)
            {
                for (int j = 0; j < core[i].Count(); j++)
                {
                    coreBoundaryList.Add(GetCoreBoundary(core[i][j]));
                }
            }

            List<List<ParkingLine>> output = new List<List<ParkingLine>>();
            List<ParkingLotType> typeOutput = new List<ParkingLotType>();

            for (int i = 0; i < usableCurveBase.Count; i++)
            {
                List<Curve> parkingLotAvailableCurve = FindParkingLotAvailableCurve(usableCurveBase[i], coreBoundaryList, plot, 20000, parkingLotType[i], roadDirection[i]);

                for (int j = 0; j < parkingLotAvailableCurve.Count(); j++)
                {
                    if (plot.Boundary.Contains(parkingLotAvailableCurve[j].PointAt(parkingLotAvailableCurve[j].Domain.Mid)) == PointContainment.Inside)
                        output.Add(createParkingLine(parkingLotAvailableCurve[j], parkingLotType[i]));
                }
            }

            return output;
        }

        private static List<Curve> MergeHousehold(List<Household> inputHousehold)
        {
            List<Brep> householdPropertyBrep = new List<Brep>();

            for (int i = 0; i < inputHousehold.Count(); i++)
                householdPropertyBrep.Add(Brep.CreatePlanarBreps(inputHousehold[i].GetOutline())[0]);

            householdPropertyBrep = Brep.JoinBreps(householdPropertyBrep, 0).ToList();

            List<Curve> output = new List<Curve>();

            foreach (Brep i in householdPropertyBrep)
            {
                List<BrepEdge> Edges = i.Edges.ToList();

                List<Curve> nakedBrepEdge = new List<Curve>();

                List<Brep> brepFaceBrep = (from j in i.Faces
                                           select j.DuplicateFace(false)).ToList();

                foreach (BrepEdge j in Edges)
                {
                    Point3d tempCenterPoint = j.PointAt(j.Domain.Mid);

                    if (Rhino.Geometry.Intersect.Intersection.ProjectPointsToBreps(brepFaceBrep, new Point3d[] { tempCenterPoint }, Vector3d.ZAxis, 0).Count() <= 1)
                        nakedBrepEdge.Add(j.ToNurbsCurve());
                }

                output.AddRange(Curve.JoinCurves(nakedBrepEdge).ToList());
            }

            return output;
        }

        public enum ParkingLotType { perpendicular, parallel };
        private enum RoadDirection { forward, backward }

        private static List<ParkingLine> createParkingLine(Curve parkingLineBase, ParkingLotType parkingLotType)
        {
            List<ParkingLine> output = new List<ParkingLine>();

            double parkingLotWidth = double.MaxValue;
            double parkingLotHeigth = 1;

            if (parkingLotType == ParkingLotType.perpendicular)
            {
                parkingLotWidth = 2300;
                parkingLotHeigth = -5000;
            }
            else if (parkingLotType == ParkingLotType.parallel)
            {
                parkingLotWidth = 6000;
                parkingLotHeigth = -2000;
            }


            int parkingLotCount = (int)(parkingLineBase.GetLength() / parkingLotWidth);

            double lengthToParam = (parkingLineBase.Domain.T1 - parkingLineBase.Domain.T0) / parkingLineBase.GetLength() * parkingLotWidth;

            for (int i = 0; i < parkingLotCount; i++)
            {
                double startParam = parkingLineBase.Domain.Mid + lengthToParam * (-(double)parkingLotCount / 2 + i);
                double endParam = parkingLineBase.Domain.Mid + lengthToParam * (-(double)parkingLotCount / 2 + i + 1);

                Curve tempCurve = new LineCurve(parkingLineBase.PointAt(startParam), parkingLineBase.PointAt(endParam));
                Curve offsettedCurve = tempCurve.Offset(Plane.WorldXY, parkingLotHeigth, 0, CurveOffsetCornerStyle.None)[0];

                Vector3d tempPlaneX = new Vector3d(tempCurve.PointAt(tempCurve.Domain.T1) - tempCurve.PointAt(tempCurve.Domain.T0));
                Vector3d tempPlaneY = new Vector3d(offsettedCurve.PointAt(offsettedCurve.Domain.T0) - tempCurve.PointAt(tempCurve.Domain.T0));

                Rectangle3d tempRectangle = new Rectangle3d(new Plane(tempCurve.PointAt(tempCurve.Domain.T0), tempPlaneX, tempPlaneY), tempCurve.PointAt(tempCurve.Domain.T0), offsettedCurve.PointAt(offsettedCurve.Domain.T1));

                output.Add(new ParkingLine(tempRectangle));
            }

            return output;
        }

        private static Interval getOffsetDistance(ParkingLotType parkingLotType, RoadDirection roadDirection)
        {
            double start;
            double end;

            if (roadDirection == RoadDirection.forward)
            {
                start = 0;

                if (parkingLotType == ParkingLotType.perpendicular)
                    end = -5000 - 6000;
                else
                    end = -2000 - 3000;
            }
            else
            {
                if (parkingLotType == ParkingLotType.perpendicular)
                {
                    start = 6000;
                    end = -5000;
                }
                else
                {
                    start = 3000;
                    end = -2000;
                }
            }

            return new Interval(start, end);
        }

        private static Curve offsetByDistance(Curve baseCurve, double offsetDistance)
        {
            Curve curveStart = baseCurve;
            Curve curveEnd;

            if (offsetDistance == 0)
                return curveStart;

            curveEnd = baseCurve.Offset(Plane.WorldXY, offsetDistance, 0, CurveOffsetCornerStyle.None)[0];

            Curve side1 = new LineCurve(curveStart.PointAt(curveStart.Domain.T1), curveEnd.PointAt(curveEnd.Domain.T1));
            Curve side2 = new LineCurve(curveEnd.PointAt(curveEnd.Domain.T0), curveStart.PointAt(curveStart.Domain.T0));

            return Curve.JoinCurves(new Curve[] { curveStart, side1, curveEnd, side2 })[0];
        }

        private static Curve offsetByInterval(Curve baseCurve, Interval offsetDistance)
        {
            Curve curveStart;
            Curve curveEnd;

            if (offsetDistance.T0 != 0)
                curveStart = baseCurve.Offset(Plane.WorldXY, offsetDistance.T0, 0, CurveOffsetCornerStyle.None)[0];
            else
                curveStart = baseCurve;

            if (offsetDistance.T1 != 0)
                curveEnd = baseCurve.Offset(Plane.WorldXY, offsetDistance.T1, 0, CurveOffsetCornerStyle.None)[0];
            else
                curveEnd = baseCurve;

            if (offsetDistance.T0 == offsetDistance.T1)
                return curveStart;

            Curve side1 = new LineCurve(curveStart.PointAt(curveStart.Domain.T1), curveEnd.PointAt(curveEnd.Domain.T1));
            Curve side2 = new LineCurve(curveEnd.PointAt(curveEnd.Domain.T0), curveStart.PointAt(curveStart.Domain.T0));

            return Curve.JoinCurves(new Curve[] { curveStart, side1, curveEnd, side2 })[0];
        }

        private static List<Interval> checkIntersection(Curve Checker, Curve CheckerBase, List<Curve> curveToIntersect)
        {
            List<Interval> output = new List<Interval>();

            foreach (Curve i in curveToIntersect)
            {
                var intersection = Rhino.Geometry.Intersect.Intersection.CurveCurve(i, Checker, 0, 0);

                if (intersection.Count != 0)
                {

                    List<double> parameterset = (from intersectionEvent in intersection
                                                 select intersectionEvent.ParameterA).ToList();

                    Curve[] shatteredCurve = i.Split(parameterset);

                    List<Guid> guidList = new List<Guid>();

                    List<Curve> ShatteredCurveInChecker = new List<Curve>();

                    foreach (Curve j in shatteredCurve)
                    {
                        if (Checker.Contains(j.PointAt(j.Domain.Mid)) == PointContainment.Inside)
                        {
                            ShatteredCurveInChecker.Add(j);
                        }
                    }

                    shatteredCurve = Curve.JoinCurves(ShatteredCurveInChecker);

                    foreach (Curve j in shatteredCurve)
                    {
                        List<double> tempClosestParameters = new List<double>();

                        Curve[] tempSegments = j.DuplicateSegments();

                        if (tempSegments.Count() <= 0)
                        {
                            tempSegments = new Curve[] { j };
                        }

                        foreach (Curve k in tempSegments)
                        {
                            double tempOutStart;
                            double tempOutEnd;

                            CheckerBase.ClosestPoint(k.PointAt(k.Domain.T0), out tempOutStart);
                            CheckerBase.ClosestPoint(k.PointAt(k.Domain.T1), out tempOutEnd);

                            tempClosestParameters.Add(tempOutStart);
                            tempClosestParameters.Add(tempOutEnd);
                        }

                        if (tempClosestParameters.Count >= 2)
                        {
                            tempClosestParameters.Sort();

                            output.Add(new Interval(tempClosestParameters[0], tempClosestParameters[tempClosestParameters.Count - 1]));
                        }

                    }

                }
                else
                {
                    List<double> tempClosestParameters = new List<double>();

                    Curve[] tempSegments = i.DuplicateSegments();

                    if (tempSegments.Count() <= 0)
                    {
                        tempSegments = new Curve[] { i };
                    }

                    foreach (Curve k in tempSegments)
                    {
                        if (Checker.Contains(k.PointAt(k.Domain.Mid)) == PointContainment.Inside)
                        {

                            double tempOutStart;
                            double tempOutEnd;

                            CheckerBase.ClosestPoint(k.PointAt(k.Domain.T0), out tempOutStart);
                            CheckerBase.ClosestPoint(k.PointAt(k.Domain.T1), out tempOutEnd);

                            tempClosestParameters.Add(tempOutStart);
                            tempClosestParameters.Add(tempOutEnd);
                        }
                    }

                    if (tempClosestParameters.Count >= 2)
                    {
                        tempClosestParameters.Sort();

                        output.Add(new Interval(tempClosestParameters[0], tempClosestParameters[tempClosestParameters.Count - 1]));
                    }
                }
            }

            List<Guid> outGuidList = new List<Guid>();

            return output;
        }

        private static List<Interval> combineIntervals(List<Interval> intervalListToCombine)
        {
            Rhino.Collections.RhinoList<Interval> rhinoList = new Rhino.Collections.RhinoList<Interval>(intervalListToCombine);
            double[] intervalStart = (from interval in intervalListToCombine
                                      select interval.Min).ToArray();

            rhinoList.Sort(intervalStart);

            int currentIndex = 0;

            while (currentIndex <= rhinoList.Count - 2)
            {
                if (rhinoList[currentIndex].Min <= rhinoList[currentIndex + 1].Min && rhinoList[currentIndex].Max >= rhinoList[currentIndex + 1].Min)
                {
                    if (rhinoList[currentIndex].Max >= rhinoList[currentIndex + 1].Max)
                    {
                        rhinoList.RemoveAt(currentIndex + 1);
                    }
                    else
                    {
                        rhinoList[currentIndex] = new Interval(rhinoList[currentIndex].Min, rhinoList[currentIndex + 1].Max);
                        rhinoList.RemoveAt(currentIndex + 1);
                    }
                }
                else
                {
                    currentIndex++;
                }
            }

            return rhinoList.ToList();
        }

        private static List<Interval> reverseInterval(double T0, double T1, List<Interval> Intervals)
        {
            List<Interval> output = new List<Interval>();

            if (Intervals.Count != 0)
            {
                if (T0 < Intervals[0].Min)
                    output.Add(new Interval(T0, Intervals[0].Min));

                for (int i = 0; i < Intervals.Count - 1; i++)
                {
                    output.Add(new Interval(Intervals[i].Max, Intervals[i + 1].Min));
                }

                if (T1 > Intervals[Intervals.Count - 1].Max)
                    output.Add(new Interval(Intervals[Intervals.Count - 1].Max, T1));
            }
            else
            {
                output.Add(new Interval(T0, T1));
            }

            return output;
        }
        private static List<Curve> curveFromIntervals(Curve baseCurve, List<Interval> intervalList)
        {
            List<Curve> output = new List<Curve>();

            Interval curveInterval = new Interval(baseCurve.Domain.T0, baseCurve.Domain.T1);

            for (int i = 0; i < intervalList.Count(); i++)
            {
                if (curveInterval.IncludesInterval(intervalList[i]))
                {
                    output.Add(baseCurve.ToNurbsCurve(intervalList[i]));
                }
            }

            return output;
        }

        private static List<Curve> FindParkingLotAvailableCurve(Curve parkingLotBase, List<Curve> cores, Plot plot, double ExtendLength, ParkingLotType parkingLotType, RoadDirection roadDirection)
        {
            Curve newCurve = parkingLotBase.DuplicateCurve();

            if (ExtendLength != 0)
                newCurve = parkingLotBase.Extend(CurveEnd.Both, ExtendLength, CurveExtensionStyle.Line);

            newCurve = new LineCurve(newCurve.PointAt(newCurve.Domain.T0), newCurve.PointAt(newCurve.Domain.T1));

            Interval roadOffsetDistance = getOffsetDistance(parkingLotType, roadDirection);

            Curve tempChecker = offsetByInterval(newCurve, roadOffsetDistance);

            List<Curve> CurveToIntersect = new List<Curve>(cores);
            CurveToIntersect.Add(plot.Boundary);
            //Rhino.RhinoDoc.ActiveDoc.Objects.Add(plot.Boundary);
            List<Interval> tempIntersection = checkIntersection(tempChecker, newCurve, CurveToIntersect);

            tempIntersection = combineIntervals(tempIntersection);

            List<Interval> tempReverseIntersection = reverseInterval(newCurve.Domain.T0, newCurve.Domain.T1, tempIntersection);

            return curveFromIntervals(newCurve, tempReverseIntersection);
        }

        private static Curve GetCoreBoundary(Core core)
        {
            Point3d firstPoint = core.Origin - core.YDirection * Consts.exWallThickness - core.XDirection * Consts.exWallThickness;
            Point3d secondPoint = firstPoint + core.XDirection * (core.Width + 2 * Consts.exWallThickness);
            Point3d thirdPoint = secondPoint + core.YDirection * (core.Depth + 2 * Consts.exWallThickness);
            Point3d fourthPoint = firstPoint + core.YDirection * (core.Depth + 2 * Consts.exWallThickness);

            Point3d[] pointSet = { core.Origin, secondPoint, thirdPoint, fourthPoint, core.Origin };

            return new PolylineCurve(pointSet);
        }

        private class LineSD
        {
            public LineSD(Point3d start, Vector3d direction)
            {
                this.Start = start;
                this.Direction = direction;
            }

            public bool OnSameLineCurve(Point3d anotherOrigin, Vector3d anotherVector)
            {
                if (this.Start == anotherOrigin)
                {
                    return true;
                }
                else if (this.Direction / this.Direction.Length != anotherVector / anotherVector.Length)
                {
                    return false;
                }
                else
                {
                    double tempMultiplyFactor_X = (anotherOrigin.X - this.Start.X) / this.Direction.X;
                    double tempMultiplyFactor_Y = (anotherOrigin.Y - this.Start.Y) / this.Direction.Y;

                    if (tempMultiplyFactor_X == tempMultiplyFactor_Y)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }

            public bool OnSameLineCurve(LineSD anotherLineSD)
            {
                Point3d anotherOrigin = anotherLineSD.Start;
                Vector3d anotherVector = anotherLineSD.Direction;

                try
                {
                    if (this.Direction.IsParallelTo(anotherVector) == -1)
                    {
                        return false;
                    }
                    else if (this.Start == anotherOrigin)
                    {
                        return true;
                    }
                    else
                    {
                        double tempMultiplyFactor_X = Math.Round((anotherOrigin.X - this.Start.X) / this.Direction.X, 2);
                        double tempMultiplyFactor_Y = Math.Round((anotherOrigin.Y - this.Start.Y) / this.Direction.Y, 2);

                        if (tempMultiplyFactor_X == tempMultiplyFactor_Y)
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                }
                catch (Exception)
                {
                    return false;
                }
            }

            public static bool OnSameLineCurve(Point3d firstOrigin, Vector3d firstVector, Point3d secondOrigin, Vector3d secondVector)
            {


                if (firstOrigin == secondOrigin)
                {
                    return true;
                }
                else if (firstVector / firstVector.Length != secondVector / secondVector.Length)
                {
                    return false;
                }
                else
                {
                    double tempMultiplyFactor_X = Math.Round((secondOrigin.X - firstOrigin.X) / firstVector.X, 2);
                    double tempMultiplyFactor_Y = Math.Round((secondOrigin.Y - firstOrigin.Y) / firstVector.Y, 2);

                    if (tempMultiplyFactor_X == tempMultiplyFactor_Y)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }

            public Point3d Start { get; private set; }
            public Vector3d Direction { get; private set; }

        }

        private static double getAngle(Vector3d baseVec, Vector3d targetVec)
        {
            double angle = Vector3d.VectorAngle(baseVec, targetVec);
            angle *= Math.Sign(Vector3d.CrossProduct(baseVec, targetVec).Z);
            return angle;
        }

        private static List<List<Core>> AlignCore_PT3(List<List<Core>> core)
        {
            List<List<Core>> output = new List<List<Core>>();

            for (int i = 0; i < core.Count(); i++)
            {
                output.Add(new List<Core>());
            }

            Core baseCore = core[0][0];
            bool isBaseAllocated = false;

            for (int i = 0; i < core.Count(); i++)
            {
                for (int j = 0; j < core[i].Count(); j++)
                {
                    if (core[i][j] != null && isBaseAllocated == false)
                    {
                        Core tempCore = core[i][j];

                        Vector3d newXDirection = tempCore.XDirection;
                        Vector3d newYDirection = tempCore.YDirection;
                        Point3d newOrigin = tempCore.Origin;

                        bool tempIfSomethingIsChanged = false;

                        if (getAngle(newXDirection, newYDirection) != Math.PI * 0.5)
                        {
                            newYDirection.Reverse();
                            newOrigin.Transform(Transform.Translation(tempCore.YDirection * tempCore.Depth));
                            tempIfSomethingIsChanged = true;
                        }

                        if (tempIfSomethingIsChanged == true)
                        {
                            baseCore = new Core(tempCore);
                            baseCore.Origin = newOrigin;
                            baseCore.XDirection = newXDirection;
                            baseCore.YDirection = newYDirection;
                            isBaseAllocated = true;
                        }
                        else
                        {
                            baseCore = core[i][j];
                            output[i].Add(baseCore);
                            isBaseAllocated = true;
                        }
                    }
                    else
                    {
                        Core tempCore = core[i][j];

                        Vector3d newXDirection = tempCore.XDirection;
                        Vector3d newYDirection = tempCore.YDirection;
                        Point3d newOrigin = tempCore.Origin;

                        bool tempIfSomethingIsChanged = false;

                        if (baseCore.XDirection.IsParallelTo(tempCore.XDirection) == -1)
                        {
                            newXDirection.Reverse();
                            newOrigin.Transform(Transform.Translation(tempCore.XDirection * tempCore.Width));
                            tempIfSomethingIsChanged = true;
                        }

                        if (baseCore.YDirection.IsParallelTo(tempCore.YDirection) == -1)
                        {
                            newYDirection.Reverse();
                            newOrigin.Transform(Transform.Translation(tempCore.YDirection * tempCore.Depth));
                            tempIfSomethingIsChanged = true;
                        }

                        if (tempIfSomethingIsChanged)
                        {
                            Core changedCore = new Core(tempCore);
                            baseCore.Origin = newOrigin;
                            baseCore.XDirection = newXDirection;
                            baseCore.YDirection = newYDirection;
                            output[i].Add(changedCore);
                        }
                        else
                        {
                            output[i].Add(core[i][j]);
                        }
                    }
                }
            }

            return output;
        }

        private static bool VectorIntersection(Point3d A, Vector3d Avector, Point3d B, Vector3d Bvector, ref Vector3d ATransformVector)
        {
            //Returns false If Intersection can't be made

            if (Avector.IsParallelTo(Bvector) != 0)
                return false;

            ATransformVector = (((B.X - A.X) * Bvector.Y - (B.Y - A.Y) * Bvector.X)) / (Avector.X * Bvector.Y - Avector.Y * Bvector.X) * Avector;

            return true;
        }

        private static bool CalculateVectorDistance(Vector3d Avector, Vector3d Bvector, ref double distance)
        {
            if (Avector.IsParallelTo(Bvector) != 0)
            {
                distance = -(Avector.Length - Avector.IsParallelTo(Bvector) * Bvector.Length);
            }
            else
            {
                return false;
            }
            return true;
        }

        private static double calculateLastVectorDistance(LineSD GuideLineSD, Vector3d GuideYvector, Curve boundary)
        {
            List<Point3d> plotPoints = new List<Point3d>();
            List<Curve> plotSegments = boundary.DuplicateSegments().ToList();
            List<double> vectorDistance = new List<double>();

            foreach (Curve i in plotSegments)
            {
                plotPoints.Add(i.PointAt(i.Domain.T0));
            }

            for (int i = 0; i < plotPoints.Count(); i++)
            {
                Vector3d tempAtransform = new Vector3d();

                VectorIntersection(GuideLineSD.Start, GuideYvector, plotPoints[i], GuideLineSD.Direction, ref tempAtransform);

                double tempVectorDistance = 0;

                CalculateVectorDistance(tempAtransform, GuideYvector, ref tempVectorDistance);

                vectorDistance.Add(tempVectorDistance);

                Rhino.Display.Text3d tempText3d = new Rhino.Display.Text3d(tempVectorDistance.ToString(), new Plane(plotPoints[i], Vector3d.ZAxis), 2000);
            }

            return vectorDistance.Min();
        }

        private static void ParkingLotBaseLineMaker_XDirection(List<List<Core>> cores, Curve Boundary, out List<double> distance, out Curve firstCurve, out double coreDepth, out double Angle)
        {
            Rhino.Collections.RhinoList<LineSD> unExtendedOutput = new Rhino.Collections.RhinoList<LineSD>();
            List<Vector3d> Yvector = new List<Vector3d>();
            coreDepth = 0;

            for (int i = 0; i < cores.Count(); i++)
            {
                for (int j = 0; j < cores[i].Count(); j++)
                {
                    if (unExtendedOutput.Count == 0 && cores[i][j] != null)
                    {
                        unExtendedOutput.Add(new LineSD(cores[i][j].Origin, cores[i][j].XDirection / cores[i][j].XDirection.Length));
                        Yvector.Add(cores[i][j].YDirection);
                        coreDepth = cores[i][j].Depth;
                    }
                    else if (cores[i][j] != null)
                    {
                        LineSD tempLineSD = new LineSD(cores[i][j].Origin, cores[i][j].XDirection / cores[i][j].XDirection.Length);

                        bool tempBool = false;

                        for (int k = 0; k < unExtendedOutput.Count(); k++)
                        {
                            bool tempOnSameLine = unExtendedOutput[k].OnSameLineCurve(tempLineSD);

                            if (tempOnSameLine)
                            {
                                tempBool = true;
                                break;
                            }
                        }

                        if (tempBool == false)
                        {
                            unExtendedOutput.Add(tempLineSD);
                            Yvector.Add(cores[i][j].YDirection);
                        }
                    }
                }
            }
            double[] VectorDistance = new double[unExtendedOutput.Count];

            for (int i = 0; i < unExtendedOutput.Count(); i++)
            {
                Vector3d tempOutput = new Vector3d();

                bool tempVectorIntersection = VectorIntersection(unExtendedOutput[0].Start, Yvector[0], unExtendedOutput[i].Start, unExtendedOutput[i].Direction, ref tempOutput);
                double tempDistance = double.MinValue;
                if (tempVectorIntersection)
                {

                    CalculateVectorDistance(Yvector[0], tempOutput, ref tempDistance);

                    if (tempOutput == new Vector3d(0, 0, 0))
                    {
                        tempDistance = 0;
                    }


                    VectorDistance[i] = tempDistance;
                }

            }

            double LastVectorDIstance = calculateLastVectorDistance(unExtendedOutput[0], Yvector[0], Boundary);

            List<double> VectorDistanceList = new List<double>(VectorDistance);
            unExtendedOutput.Sort(VectorDistance);
            unExtendedOutput.Reverse();

            //AddLastCurve

            VectorDistanceList.Add(LastVectorDIstance - coreDepth);

            VectorDistanceList.Sort();
            VectorDistanceList.Reverse();

            distance = (from i in VectorDistanceList
                        select VectorDistanceList[0] - i).ToList();

            LineCurve tempFirstCurve = new LineCurve(unExtendedOutput[0].Start, unExtendedOutput[0].Direction + unExtendedOutput[0].Start);
            Curve tempExtendedFirst = tempFirstCurve.Extend(CurveEnd.Both, CurveExtensionStyle.Line, new Curve[] { Boundary });

            if (tempExtendedFirst == null)
            {
                firstCurve = null;
                Angle = 0;
                return;
            }
                

            firstCurve = new LineCurve(tempExtendedFirst.PointAt(tempExtendedFirst.Domain.T0), tempExtendedFirst.PointAt(tempExtendedFirst.Domain.T1));



            Angle = getAngle(Vector3d.XAxis, unExtendedOutput[0].Direction);

            return;
        }


    }
}
