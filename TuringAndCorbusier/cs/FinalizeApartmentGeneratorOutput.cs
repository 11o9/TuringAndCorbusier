using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rhino.Geometry;
using Rhino.Collections;
using Rhino;

namespace TuringAndCorbusier
{
    public class FinalizeApartment
    {
        public static List<Apartment> finalizeAGoutput(Apartment agOutput, double LegalFloorAreaRatio, double LegalBuildingCoverage, bool onlyCreateUnderGround)
        {
            Apartment tempBasicAgOutput = agOutput;

            /*
            if(IsRoadCenterMeetStreet(agOutput))
                agOutput.Plot.Boundary = OffsetBoundaryToMakeRoad(agOutput.Plot.Boundary, agOutput.Plot.Surroundings);
            */

            if (agOutput.Plot.GetArea() / 1000000 / 3.3 >= 300 || onlyCreateUnderGround == true)
            {
                List<double> reducePercentage = new List<double>(new double[] { 0, 5, 10 });

                List<Curve> roadCenterCurve = new List<Curve>();
                List<ParkingLineMaker.ParkingLotType> parkingLotTypes = new List<ParkingLineMaker.ParkingLotType>();
                double Angle;

                Curve centerCurve = new LineCurve(Point3d.Origin, Point3d.Origin);
                if (agOutput.AptLines.Count() != 0)
                    centerCurve = agOutput.AptLines[0];

                ParkingLineMaker.parkingLineMaker(agOutput.AGtype, agOutput.Core, agOutput.Plot, agOutput.ParameterSet.Parameters[2], centerCurve, out parkingLotTypes, out roadCenterCurve, out Angle);

                bool IsRoadCenterMeetStreet = CheckRoadCenterMeetStreet(agOutput);

                Curve Boundary = agOutput.Plot.Boundary;

                if (IsRoadCenterMeetStreet == false && agOutput.AGtype != "PT-4")
                {
                    Curve tempBoundary = Boundary;
                    Polyline tempBoundaryPolyline;
                    tempBoundary.TryGetPolyline(out tempBoundaryPolyline);

                    Boundary = CreateRoadClass.createRoad(tempBoundaryPolyline, Angle).ToNurbsCurve();
                }

                List<Apartment> tempApartmentSet = duplicateAndReduceAGoutput(agOutput, reducePercentage);
                List<Apartment> outputApartmentSet = new List<Apartment>();

                for (int i = 0; i < tempApartmentSet.Count(); i++)
                {
                    List<Curve> tempRoadCenterCurve = new List<Curve>();
                    List<ParkingLineMaker.ParkingLotType> tempParkingLotTypes = new List<ParkingLineMaker.ParkingLotType>();
                    double tempAngle;

                    Curve tempCenterCurve = new LineCurve(Point3d.Origin, Point3d.Origin);
                    if (tempApartmentSet[i].AptLines.Count() != 0)
                        tempCenterCurve = tempApartmentSet[i].AptLines[0];

                    List<List<ParkingLine>> tempParkingLines = ParkingLineMaker.parkingLineMaker(tempApartmentSet[i].AGtype, tempApartmentSet[i].Core, new Plot(Boundary, agOutput.Plot.Surroundings), agOutput.ParameterSet.Parameters[2], tempCenterCurve, out tempParkingLotTypes, out tempRoadCenterCurve, out tempAngle);
                    int tempParkingLotCount = 0;

                    foreach (List<ParkingLine> j in tempParkingLines)
                        tempParkingLotCount += j.Count();

                    List<List<ParkingLine>> tempParkingLineCopy = new List<List<ParkingLine>>(tempParkingLines);

                    Curve tempRamp = (new Rectangle3d()).ToNurbsCurve();
                    bool IsRampAvailable = CreateUnderGroundParking_Ramp(ref tempParkingLines, ref tempParkingLotTypes, ref tempRamp);

                    int tempMaxParkingLotOnEarth = 0;

                    for (int j = 0; j < tempParkingLineCopy.Count(); j++)
                        tempMaxParkingLotOnEarth += tempParkingLineCopy[j].Count();

                    if (IsRampAvailable == true)
                    {
                        tempParkingLines = tempParkingLineCopy;

                        List<Household> tempHouseholdOnSecondFloor = new List<Household>();

                        for (int j = 0; j < tempApartmentSet[i].Household.Count(); j++)
                        {
                            if (tempApartmentSet[i].Household[j].Count != 0)
                                tempHouseholdOnSecondFloor.AddRange(tempApartmentSet[i].Household[j][0]);
                        }

                        double tempGrossAreaRemain = tempApartmentSet[i].Plot.GetArea() * (LegalFloorAreaRatio / 100) - tempApartmentSet[i].GetGrossArea();
                        double tempBuildingAreaRemain = tempApartmentSet[i].Plot.GetArea() * (LegalBuildingCoverage / 100) - tempApartmentSet[i].GetBuildingArea();

                        List<NonResidential> tempCommercials = new List<NonResidential>();

                        if (i != 0)
                        {
                            tempCommercials = CreateCommercial(ref tempParkingLines, tempHouseholdOnSecondFloor, ref tempGrossAreaRemain, ref tempBuildingAreaRemain);
                        }
                        int tempParkingLinesCount = 0;

                        for (int j = 0; j < tempParkingLines.Count(); j++)
                            tempParkingLinesCount += tempParkingLines[j].Count();

                        tempApartmentSet[i].ParkingLotOnEarth = new ParkingLotOnEarth(tempParkingLines);
                        tempApartmentSet[i].Commercial = tempCommercials;

                        int tempNeededParkingLotUnderGround = tempApartmentSet[i].GetLegalParkingLotofHousing();
                        tempNeededParkingLotUnderGround += tempApartmentSet[i].GetLegalParkingLotOfCommercial();
                        tempNeededParkingLotUnderGround -= tempApartmentSet[i].ParkingLotOnEarth.GetCount();

                        double UnderGroundFloor = (double)tempNeededParkingLotUnderGround / (double)tempMaxParkingLotOnEarth;

                        double UnderGroundArea = agOutput.Plot.GetArea() * UnderGroundFloor;

                        if (UnderGroundFloor % 1 != 0)
                            UnderGroundFloor = (int)UnderGroundFloor + 1;

                        if (tempNeededParkingLotUnderGround > tempParkingLinesCount)
                            tempApartmentSet[i].ParkingLotUnderGround = new ParkingLotUnderGround(tempNeededParkingLotUnderGround, UnderGroundArea, (int)UnderGroundFloor);
                        else
                            tempApartmentSet[i].ParkingLotUnderGround = new ParkingLotUnderGround();

                        outputApartmentSet.Add(tempApartmentSet[i]);
                    }
                    else
                    {
                        List<List<ParkingLine>> tempParkingline = ParkingLineMaker.parkingLineMaker(tempApartmentSet[i].AGtype, tempApartmentSet[i].Core, new Plot(Boundary, agOutput.Plot.Surroundings), agOutput.ParameterSet.Parameters[2], tempCenterCurve, out tempParkingLotTypes, out tempRoadCenterCurve, out tempAngle);


                        List<Household> tempHouseholdOnSecondFloor = new List<Household>();

                        for (int j = 0; j < tempApartmentSet[i].Household.Count(); j++)
                        {
                            if (tempApartmentSet[i].Household[j].Count != 0)
                                tempHouseholdOnSecondFloor.AddRange(tempApartmentSet[i].Household[j][0]);
                        }

                        double tempGrossAreaRemain = tempApartmentSet[i].Plot.GetArea() * (LegalFloorAreaRatio / 100) - tempApartmentSet[i].GetGrossArea();
                        double tempBuildingAreaRemain = tempApartmentSet[i].Plot.GetArea() * (LegalBuildingCoverage / 100) - tempApartmentSet[i].GetBuildingArea();

                        List<NonResidential> tempCommercials = new List<NonResidential>();

                        if (i != 0)
                        {
                            tempCommercials = CreateCommercial(ref tempParkingline, tempHouseholdOnSecondFloor, ref tempGrossAreaRemain, ref tempBuildingAreaRemain);
                        }

                        int tempParkingLinesCount = 0;

                        for (int j = 0; j < tempParkingline.Count(); j++)
                            tempParkingLinesCount += tempParkingline[j].Count();

                        tempApartmentSet[i].ParkingLotOnEarth = new ParkingLotOnEarth(tempParkingline);
                        tempApartmentSet[i].Commercial = tempCommercials;

                        int tempNeededParkingLot = tempApartmentSet[i].GetLegalParkingLotofHousing();
                        tempNeededParkingLot += tempApartmentSet[i].GetLegalParkingLotOfCommercial();

                        outputApartmentSet.Add(tempApartmentSet[i]);

                    }
                }

                return outputApartmentSet;
            }
            else
            {
                List<double> reducePercentage = new List<double>(new double[] { 0, 5 });

                List<Curve> roadCenterCurve = new List<Curve>();
                List<ParkingLineMaker.ParkingLotType> parkingLotTypes = new List<ParkingLineMaker.ParkingLotType>();
                double Angle;

                Curve centerCurve = new LineCurve(Point3d.Origin, Point3d.Origin);
                if (agOutput.AptLines.Count() != 0)
                    centerCurve = agOutput.AptLines[0];

                ParkingLineMaker.parkingLineMaker(agOutput.AGtype, agOutput.Core, agOutput.Plot, agOutput.ParameterSet.Parameters[2], centerCurve, out parkingLotTypes, out roadCenterCurve, out Angle);


                bool IsRoadCenterMeetStreet = CheckRoadCenterMeetStreet(agOutput);

                Curve Boundary = agOutput.Plot.Boundary;

                if (IsRoadCenterMeetStreet == false)
                {
                    Curve tempBoundary = Boundary;
                    Polyline tempBoundaryPolyline;
                    tempBoundary.TryGetPolyline(out tempBoundaryPolyline);

                    Boundary = CreateRoadClass.createRoad(tempBoundaryPolyline, Angle).ToNurbsCurve();
                }

                List<Apartment> tempApartmentSet = duplicateAndReduceAGoutput(agOutput, reducePercentage);
                List<Apartment> outputApartmentSet = new List<Apartment>();

                List<ParkingLineMaker.ParkingLotType> tempParkingLotTypes;
                List<Curve> tempRoadCenterCurve;
                double tempAngle;

                tempApartmentSet[0].ParkingLotOnEarth = new ParkingLotOnEarth(ParkingLineMaker.parkingLineMaker(agOutput.AGtype, agOutput.Core, agOutput.Plot, agOutput.ParameterSet.Parameters[2], centerCurve, out tempParkingLotTypes, out tempRoadCenterCurve, out tempAngle));

                outputApartmentSet.Add(tempApartmentSet[0]);

                for (int i = 1; i < tempApartmentSet.Count(); i++)
                {
                    List<List<ParkingLine>> tempParkingline = ParkingLineMaker.parkingLineMaker(agOutput.AGtype, agOutput.Core, agOutput.Plot, agOutput.ParameterSet.Parameters[2], centerCurve, out tempParkingLotTypes, out tempRoadCenterCurve, out tempAngle);

                    List<Household> tempHouseholdOnSecondFloor = new List<Household>();

                    for (int j = 0; j < tempApartmentSet[i].Household.Count(); j++)
                    {
                        if (tempApartmentSet[i].Household[j].Count != 0)
                            tempHouseholdOnSecondFloor.AddRange(tempApartmentSet[i].Household[j][0]);
                    }

                    double tempGrossAreaRemain = tempApartmentSet[i].Plot.GetArea() * (LegalFloorAreaRatio / 100) - tempApartmentSet[i].GetGrossArea();
                    double tempBuildingAreaRemain = tempApartmentSet[i].Plot.GetArea() * (LegalBuildingCoverage / 100) - tempApartmentSet[i].GetBuildingArea();

                    List<NonResidential> tempCommercials = new List<NonResidential>();

                    if (i != 0)
                    {
                        tempCommercials = CreateCommercial(ref tempParkingline, tempHouseholdOnSecondFloor, ref tempGrossAreaRemain, ref tempBuildingAreaRemain);
                    }

                    int tempParkingLinesCount = 0;

                    for (int j = 0; j < tempParkingline.Count(); j++)
                        tempParkingLinesCount += tempParkingline[j].Count();

                    tempApartmentSet[i].ParkingLotOnEarth = new ParkingLotOnEarth(tempParkingline);
                    tempApartmentSet[i].Commercial = tempCommercials;

                    outputApartmentSet.Add(tempApartmentSet[i]);

                }
                return outputApartmentSet;
            }
        }

        public int GetLegalParkingLotofHousing(List<List<List<Household>>> Household)
        {
            double legalParkingLotByUnitNum = 0;
            double legalParkingLotByUnitSize = 0;

            for (int i = 0; i < Household.Count; i++)
            {
                for (int j = 0; j < Household[i].Count; j++)
                {
                    for (int k = 0; k < Household[i][j].Count; k++)
                    {
                        if (Household[i][j][k].GetExclusiveArea() > 60 * Math.Pow(10, 6))
                            legalParkingLotByUnitNum += 1;
                        else if (Household[i][j][k].GetExclusiveArea() > 30 * Math.Pow(10, 6))
                            legalParkingLotByUnitNum += 0.8;
                        else
                            legalParkingLotByUnitNum += 0.5;

                        if (Household[i][j][k].GetExclusiveArea() > 85 * Math.Pow(10, 6))
                            legalParkingLotByUnitSize = Household[i][j][k].GetExclusiveArea() / 75000000;
                        else
                            legalParkingLotByUnitSize = Household[i][j][k].GetExclusiveArea() / 65000000;
                    }

                }
            }

            if (legalParkingLotByUnitNum > legalParkingLotByUnitSize)
                return (int)legalParkingLotByUnitNum + 1;
            else
                return (int)legalParkingLotByUnitSize;
        }

        private static List<Apartment> duplicateAndReduceAGoutput(Apartment AGoutput, List<double> reducePercentage)
        {
            List<Apartment> output = new List<Apartment>();

            for (int i = 0; i < reducePercentage.Count(); i++)
            {
                if (AGoutput.GetGrossAreaRatio() > TuringAndCorbusierPlugIn.InstanceClass.page1Settings.MaxFloorAreaRatio * (100 - reducePercentage[i]) / 100)
                {
                    double currentFloorAreaRatio = AGoutput.GetGrossAreaRatio();
                    Plot tempPlot = AGoutput.Plot;
                    List<List<List<Household>>> household = CloneHhp(AGoutput.Household);
                    List<List<Core>> core = CloneCP(AGoutput.Core);
                    List<Curve> aptLines = AGoutput.AptLines;

                    CommonFunc.reduceFloorAreaRatio_Mixref(ref household, ref core, ref currentFloorAreaRatio, tempPlot, TuringAndCorbusierPlugIn.InstanceClass.page1Settings.MaxFloorAreaRatio * (100 - reducePercentage[i]) / 100,true);

                    double[] parameters = (double[])AGoutput.ParameterSet.Parameters.Clone();
                    parameters[0] = parametersetStoriesChange(parameters[0], CommonFunc.toplevel(core));
                    parameters[1] = parametersetStoriesChange(parameters[1], CommonFunc.toplevel(core));

                    ParameterSet newParameterset = new ParameterSet(parameters, AGoutput.ParameterSet.agName, AGoutput.ParameterSet.CoreType);

                    Apartment tempApartment = new Apartment(AGoutput.AGtype, tempPlot, AGoutput.BuildingType, newParameterset, AGoutput.Target, core, household, new ParkingLotOnEarth(), new ParkingLotUnderGround(), AGoutput.buildingOutline, AGoutput.AptLines);

                    output.Add(tempApartment);
                }
            }

            if (output.Count == 0)
                output.Add(AGoutput);

            return output;
        }

        private static double parametersetStoriesChange(double existingStory, double newStory)
        {
            if (existingStory < newStory)
                return existingStory;
            else
                return newStory;
        }


        public static List<List<List<Household>>> CloneHhp(List<List<List<Household>>> cloneBase)
        {
            List<List<List<Household>>> output = new List<List<List<Household>>>();

            for (int i = 0; i < cloneBase.Count(); i++)
            {
                List<List<Household>> tempOutput = new List<List<Household>>();

                for (int j = 0; j < cloneBase[i].Count(); j++)
                {
                    List<Household> tempTempOutput = new List<Household>();

                    for (int k = 0; k < cloneBase[i][j].Count(); k++)
                    {
                        tempTempOutput.Add(cloneBase[i][j][k]);
                    }

                    tempOutput.Add(tempTempOutput);
                }

                output.Add(tempOutput);
            }

            return output;
        }

        public static List<List<Core>> CloneCP(List<List<Core>> cloneBase)
        {
            List<List<Core>> output = new List<List<Core>>();

            for (int i = 0; i < cloneBase.Count(); i++)
            {
                List<Core> tempOutput = new List<Core>();

                for (int j = 0; j < cloneBase[i].Count(); j++)
                {
                    tempOutput.Add(new Core(cloneBase[i][j]));
                }

                output.Add(tempOutput);
            }

            return output;
        }

        private static Curve CreateOutlineOnEarth(Household household)
        {
            Household newHousehold = new Household(household);

            newHousehold.Origin = new Point3d(newHousehold.Origin.X, newHousehold.Origin.Y, 0);

            return newHousehold.GetOutline();
        }

        private static List<NonResidential> CreateCommercial(ref List<List<ParkingLine>> parkingLines, List<Household> buildingOutline, ref double grossAreaRemain, ref double buildingAreaRemain)
        {
            List<Curve> buildingOutlines = (from i in buildingOutline
                                            select CreateOutlineOnEarth(i)).ToList();

            List<NonResidential> output = new List<NonResidential>();

            for (int i = 0; i < parkingLines.Count(); i++)
            {
                List<List<int>> tempRemoveIndex;

                List<NonResidential> tempNonresidentials = NonResidential.createNonResidential(parkingLines[i], buildingOutlines, out tempRemoveIndex);


                for (int j = tempNonresidentials.Count() - 1; j >= 0; j--)
                {
                    double Area = tempNonresidentials[j].GetArea();
                    double BuildingArea = tempNonresidentials[j].AdditionalBuildingArea;

                    if (tempNonresidentials[j].GetArea() <= grossAreaRemain && tempNonresidentials[j].AdditionalBuildingArea <= buildingAreaRemain)
                    {
                        output.Add(tempNonresidentials[j]);
                        grossAreaRemain -= tempNonresidentials[j].GetArea();
                        buildingAreaRemain -= tempNonresidentials[j].AdditionalBuildingArea;

                        for (int k = tempRemoveIndex[j].Count() - 1; k >= 0; k--)
                            parkingLines[i].RemoveAt(tempRemoveIndex[j][k]);

                    }
                }
            }

            return output;
        }

        private static bool CreateUnderGroundParking_Ramp(ref List<List<ParkingLine>> parkingLines, ref List<ParkingLineMaker.ParkingLotType> parkingLotTypes, ref Curve Ramp)
        {
            if (parkingLines.Count <= 0)
                return false;

            List<int> parkingLineCount = (from i in parkingLines
                                          select i.Count()).ToList();
            List<int> parkingLineCountCopy = new List<int>(parkingLineCount);
            parkingLineCountCopy.Sort();

            int Index = parkingLineCount.IndexOf(parkingLineCountCopy.Max());

            int I = parkingLotTypes.Count();

            while (true)
            {
                if (parkingLotTypes[Index] == ParkingLineMaker.ParkingLotType.perpendicular)
                    break;

                Index = parkingLineCount.IndexOf(parkingLineCountCopy[(parkingLineCountCopy.IndexOf(parkingLineCount[Index]) - 1 + parkingLineCountCopy.Count()) % parkingLineCountCopy.Count()]);
            }

            if (parkingLines[Index].Count() >= 16)
            {
                List<ParkingLine> removedParkingLines = new List<ParkingLine>();

                for (int i = 0; i < 16; i++)
                {
                    removedParkingLines.Add(parkingLines[Index][parkingLines[Index].Count() - 1]);
                    parkingLines[Index].RemoveAt(parkingLines[Index].Count() - 1);
                }

                removedParkingLines.Reverse();

                List<Curve> tempSegmentsOfStart = removedParkingLines[0].Boundary.ToNurbsCurve().DuplicateSegments().ToList();
                List<Point3d> tempPointsOfStart = (from i in tempSegmentsOfStart
                                                   select i.PointAt(i.Domain.T0)).ToList();

                Point3d tempOrigin = tempPointsOfStart[0];
                Vector3d tempXVector = new Vector3d(tempPointsOfStart[1] - tempPointsOfStart[0]);
                Vector3d tempYVector = new Vector3d(tempPointsOfStart[3] - tempPointsOfStart[0]);
                tempXVector = tempXVector / tempXVector.Length;
                tempYVector = tempYVector / tempYVector.Length;

                tempOrigin = tempOrigin + tempXVector * (16 * 2300 - 20000 / 2);
                Rectangle3d tempRampRectangle = new Rectangle3d(new Plane(tempOrigin, tempXVector, tempYVector), 20000, 5000);

                Ramp = tempRampRectangle.ToNurbsCurve();

                return true;
            }
            else
            {
                Ramp = (new Rectangle3d()).ToNurbsCurve();
                return false;
            }

        }

        private static bool CheckRoadCenterMeetStreet(Apartment agOutput)
        {
            if (agOutput.AGtype == "PT-4")
                return true;

            List<Curve> roadCenterCurve = new List<Curve>();
            List<ParkingLineMaker.ParkingLotType> parkingLotTypes = new List<ParkingLineMaker.ParkingLotType>();
            double Angle;

            Curve centerCurve = new LineCurve(Point3d.Origin, Point3d.Origin);
            if (agOutput.AptLines.Count() != 0)
                centerCurve = agOutput.AptLines[0];

            ParkingLineMaker.parkingLineMaker(agOutput.AGtype, agOutput.Core, agOutput.Plot, agOutput.ParameterSet.Parameters[2], centerCurve, out parkingLotTypes, out roadCenterCurve, out Angle);

            Polyline plotBoundaryPolyline;
            agOutput.Plot.Boundary.TryGetPolyline(out plotBoundaryPolyline);
            plotBoundaryPolyline = new Polyline(new List<Point3d>(plotBoundaryPolyline));

            for (int i = 0; i < roadCenterCurve.Count(); i++)
            {
                var tempIntersection = Rhino.Geometry.Intersect.Intersection.CurveCurve(plotBoundaryPolyline.ToNurbsCurve(), roadCenterCurve[i], 0, 0);

                if (tempIntersection.Count() != 0)
                {
                    bool tempIsMeetStreet = false;

                    for (int j = 0; j < tempIntersection.Count(); j++)
                    {
                        if (agOutput.Plot.Surroundings[(int)tempIntersection[j].ParameterA] >= 4)
                            tempIsMeetStreet = true;
                    }

                    if (tempIsMeetStreet == false)
                        return false;
                }
            }

            return true;

        }

        public class CreateRoadClass
        {
            public static Polyline createRoad(Polyline x, double y)
            {
                //make perpendicular axis
                Point3d parkCenter = new Point3d();
                List<Curve> axisCrvSet = MakeAxisLine(x, y, out parkCenter);
                Curve test2 = axisCrvSet[1];

                //split boundary into 2PolyCrv by perpendicular axis
                List<Curve> testCrvSet = SplitInto2Crv(x, test2);
                Curve newRoadLine = testCrvSet[0];
                Curve newInner = testCrvSet[1];

                //offset new road line, SET OFFSET DISTANCE HERE
                double offsetDistance = 5;
                List<Curve> newParkingLine = newRoadLine.Offset(parkCenter, Plane.WorldXY.Normal, offsetDistance, 0, CurveOffsetCornerStyle.Sharp).ToList();
                List<Curve> newParkingPoly = Rhino.Geometry.Curve.JoinCurves(newParkingLine).ToList();
                Curve extendedCrv = newParkingPoly[0];
                Curve newOuter = CrvStickToCrv(extendedCrv, x.ToNurbsCurve());

                //make new boundary
                Polyline final = TailChaser(newInner, newOuter);

                return final;
            }

            private static List<Curve> MakeAxisLine(Polyline boundary, double axisAngle, out Point3d centerPt)
            {
                Vector3d baseAxis = Plane.WorldXY.XAxis;
                baseAxis.Transform(Transform.Rotation(axisAngle, Plane.WorldXY.Origin));
                Vector3d perpendicularAxis = baseAxis;
                perpendicularAxis.Transform(Transform.Rotation(Math.PI / 2, Plane.WorldXY.Origin));
                centerPt = boundary.CenterPoint();

                BoundingBox tempBounding = new BoundingBox(new List<Point3d>(boundary));
                LineCurve diagonal = new LineCurve(tempBounding.Max, tempBounding.Min);
                double axisLength = diagonal.GetLength() / 2;
                LineCurve test1 = new LineCurve(centerPt - baseAxis * axisLength, centerPt + baseAxis * axisLength);
                LineCurve test2 = new LineCurve(centerPt - perpendicularAxis * axisLength, centerPt + perpendicularAxis * axisLength);
                List<Curve> testLineSet = new List<Curve>();
                testLineSet.Add(test1);
                testLineSet.Add(test2);

                return testLineSet;

            }

            private static List<List<Point3d>> PtDivideByLine(List<Point3d> candidatePtList, Curve divider)
            {
                List<Point3d> dexterPts = new List<Point3d>();
                List<Point3d> sinisterPts = new List<Point3d>();
                Vector3d testAxis = new Vector3d(divider.PointAtEnd - divider.PointAtStart);
                testAxis.Transform(Transform.Rotation(-Math.PI / 2, Plane.WorldXY.Origin));

                for (int i = 0; i < candidatePtList.Count(); i++)
                {
                    Point3d candidatePt = candidatePtList[i];
                    double tempParam = new Double();
                    divider.ClosestPoint(candidatePt, out tempParam);
                    Point3d testPt = divider.PointAt(tempParam);
                    Vector3d tend = new Vector3d(candidatePt - testPt);
                    double decider = Rhino.Geometry.Vector3d.Multiply(tend, testAxis);
                    if (decider >= 0)
                        dexterPts.Add(candidatePt);
                    else
                        sinisterPts.Add(candidatePt);
                }

                List<List<Point3d>> allPts = new List<List<Point3d>>();
                allPts.Add(dexterPts);
                allPts.Add(sinisterPts);
                return allPts;
            }

            private static List<Curve> DeleteCrossline(Polyline boundary, Curve testLine, List<Point3d> indexPts)
            {
                List<Curve> tempRemain = new List<Curve>();
                for (int i = 0; i < indexPts.Count(); i++)
                {
                    int tempIndex = boundary.ClosestIndex(indexPts[i]);
                    Curve splitSegment = boundary.SegmentAt(tempIndex).ToNurbsCurve();
                    var tempIntersection = Rhino.Geometry.Intersect.Intersection.CurveCurve(splitSegment, testLine, 0, 0);
                    if (tempIntersection.Count() == 0)
                        tempRemain.Add(splitSegment);
                }
                return tempRemain;
            }

            private static List<Curve> SplitInto2Crv(Polyline divided, Curve divider)
            {

                List<Point3d> dividedVertex = new List<Point3d>(divided);
                dividedVertex.RemoveAt(0);
                List<List<Point3d>> dividedPt = PtDivideByLine(dividedVertex, divider);

                List<Point3d> dexterPts = dividedPt[0];
                List<Point3d> sinisterPts = dividedPt[1];

                List<Point3d> mainPts = new List<Point3d>();
                List<Point3d> subPts = new List<Point3d>();
                int dexterNumber = dexterPts.Count();
                int sinisterNumber = sinisterPts.Count();
                if (dexterNumber <= sinisterNumber)
                {
                    mainPts = dexterPts;
                    subPts = sinisterPts;
                }
                else
                {
                    mainPts = sinisterPts;
                    subPts = dexterPts;
                }

                List<Curve> tempMain = DeleteCrossline(divided, divider, mainPts);
                List<Curve> tempSub = DeleteCrossline(divided, divider, subPts);

                Curve mainCrv = Rhino.Geometry.Curve.JoinCurves(tempMain).ElementAt(0);
                Curve subCrv = Rhino.Geometry.Curve.JoinCurves(tempSub).ElementAt(0);

                List<Curve> resultCrvs = new List<Curve>();
                resultCrvs.Add(mainCrv);
                resultCrvs.Add(subCrv);

                return resultCrvs;
            }

            private static Curve ConnectPtToCrv(Point3d connectPt, Curve connectCrv)
            {
                double tempParam = new Double();
                connectCrv.ClosestPoint(connectPt, out tempParam);
                Point3d ptOnCrv = connectCrv.PointAt(tempParam);
                LineCurve connection = new LineCurve(connectPt, ptOnCrv);
                return connection;
            }

            private static double closestParam(Point3d connectPt, Curve connectCrv)
            {
                double tempParam = new Double();
                connectCrv.ClosestPoint(connectPt, out tempParam);
                return tempParam;
            }
            private static Curve CrvStickToCrv(Curve sticker, Curve boundary)
            {
                Point3d stickerStart = sticker.PointAtStart;
                Point3d stickerEnd = sticker.PointAtEnd;
                Curve connection1 = ConnectPtToCrv(stickerStart, boundary);
                Curve connection2 = ConnectPtToCrv(stickerEnd, boundary);

                List<Curve> tempCrvSet = new List<Curve>();
                tempCrvSet.Add(sticker);
                tempCrvSet.Add(connection1);
                tempCrvSet.Add(connection2);

                List<Curve> mergedCrvList = Rhino.Geometry.Curve.JoinCurves(tempCrvSet).ToList();
                Curve result = mergedCrvList[0];

                return result;
            }

            private static Polyline TailChaser(Curve curve1, Curve curve2)
            {
                Polyline InnerPoly = new Polyline();
                Polyline OuterPoly = new Polyline();
                curve1.TryGetPolyline(out InnerPoly);
                curve2.TryGetPolyline(out OuterPoly);
                List<Point3d> InnerPts = new List<Point3d>(InnerPoly);
                List<Point3d> OuterPts = new List<Point3d>(OuterPoly);

                OuterPts.AddRange(InnerPts);
                OuterPts.Add(OuterPts[0]);
                Polyline final = new Polyline(OuterPts);

                return final;
            }
        }
    }
}
