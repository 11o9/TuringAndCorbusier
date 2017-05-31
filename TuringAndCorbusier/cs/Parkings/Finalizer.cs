using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static Rhino.RhinoApp;
using Rhino.Geometry;
namespace TuringAndCorbusier
{
    class Finalizer
    {
        //for test
        Random random = new Random();
        int depth = 0;

        //field
        Apartment apt;
        private bool setBacked = false;
        private bool using1f = false;
        private bool subtracted = false;
        private FARstatus farStatus = FARstatus.Undefined;

        private double ugpRequired = 0;

        //enum
        private enum FARstatus { Lack, Suit ,Over, Undefined } 

        //constructor
        public Finalizer(Apartment apt)
        {
            this.apt = apt;
        }

        public Finalizer(Apartment apt, int depth)
        {
            this.apt = apt;
            this.depth = depth + 1;
        }

        //main
        public Apartment Finalize()
        {
            if (apt.AptLines.Count == 0) //seive: null
                return apt;

            double farCaculated;
            CheckFARStatus(out farCaculated);

            //finalize
            if (farStatus == FARstatus.Suit)
            {
                if (ParkingsEnough())
                    return apt;

                //make undergroundParking
                UnderGroundParkingModule ugpm = new UnderGroundParkingModule(apt.Plot.Boundary, (int)ugpRequired);
                bool canUGP = ugpm.CheckPrecondition();

                if (!canUGP) //seive: cannot make underground parking
                    return apt;

                bool isCaculateduccessfully = ugpm.Calculate(); //seive: cannot calculate
                if (!isCaculateduccessfully)
                    return apt;

                //if passed all seives
                Vector3d aptdir = Vector3d.Unset;
                if (apt.AptLines.Count > 0)
                {
                    for (int i = 0; i < apt.AptLines.Count; i++)
                    {
                        aptdir = apt.AptLines[0].TangentAtStart;
                        if (aptdir != Vector3d.Zero)
                            break;
                    }
                }

                if (aptdir == Vector3d.Zero)
                    aptdir = Vector3d.Unset;

                List<Curve> obstacles = new List<Curve>();
                obstacles.AddRange(apt.Core[0].Select(n => n.DrawOutline()));
                List<Curve> householdOutlines = new List<Curve>();
                for (int i = 0; i < apt.Household[0].Count; i++)
                {
                    for (int j = 0; j < apt.Household[0][i].Count; j++)
                    {
                        Curve tempOutline = apt.Household[0][i][j].GetOutline();
                        tempOutline.Translate(-Vector3d.ZAxis * tempOutline.PointAtStart.Z);
                        householdOutlines.Add(tempOutline);
                    }
                }

                obstacles.AddRange(householdOutlines);
                Curve ramp = ugpm.DrawRamp(apt.Plot, aptdir, obstacles);
                if (ramp == null)
                    return apt;

                apt.ParkingLotUnderGround = new ParkingLotUnderGround((int)ugpm.EachFloorParkingCount * ugpm.Floors, ugpm.EachFloorArea * ugpm.Floors, ugpm.Floors);
                apt.ParkingLotUnderGround.Ramp = ramp;

                //overlap check & replace parkings (ref)
                while (true)
                {
                    bool overlapResult = ugpm.OverlapCheck(ref apt);
                    if (overlapResult)
                    {
                        ramp = ugpm.DrawRamp(apt.Plot, aptdir, obstacles);
                        apt.ParkingLotUnderGround.Ramp = ramp;
                        //something changed
                        //re finalize
                    }
                    else
                        return apt;//with new ugp
                }
            }

            if (farStatus == FARstatus.Over)
            {
                if (apt.AGtype == "PT-1")
                    return apt;

                Apartment reduced = Reduce(apt);
                Finalizer finalizer = new Finalizer(reduced, depth);
                finalizer.subtracted = true;

                return finalizer.Finalize();
            }
                        

            if (farStatus == FARstatus.Lack)
            {
                if (setBacked || using1f || subtracted)
                {
                    Finalizer fnz = new Finalizer(apt, depth);
                    fnz.farStatus = FARstatus.Suit;
                    return fnz.Finalize();
                }

                //처리방법 선택: 지금은 랜덤
                Random rand = new Random();
                double d = rand.NextDouble();

                if (d < 0.5) //한 층 더 올리고 최상층 후퇴
                {
                    Apartment setBacked = SetBack(apt);
                    Finalizer fnz = new Finalizer(setBacked, depth);
                    fnz.setBacked = true;
                    //set back and Recurse

                    return fnz.Finalize();
                }

                else //1층 사용
                {
                    //지상주차 계산도 다시 해야함.
                    Apartment using1f = Add1F(apt);
                    Finalizer fnz = new Finalizer(using1f, depth);
                    fnz.using1f = true;

                    //use1f and Recurse
                    return fnz.Finalize();
                }
            }

            return apt;
        }


        //sub
        private void CheckFARStatus(out double farCaculated)
        {
            double currentFAR = apt.GetGrossAreaRatio();
            double targetFAR = TuringAndCorbusierPlugIn.InstanceClass.page1Settings.MaxFloorAreaRatio;
            farCaculated = currentFAR;

            if (farStatus != FARstatus.Undefined)
                return;


            if (currentFAR < targetFAR * 0.9)
            {
                farStatus = FARstatus.Lack;
                return;
            }

            if (currentFAR > targetFAR)
            {
                farStatus = FARstatus.Over;
                return;
            }

            else
                farStatus = FARstatus.Suit;
        }

        bool ParkingsEnough()
        {
            double parkingCount = 0;
            if (apt.ParkingLotOnEarth.ParkingLines.Count == 0)
            {
                parkingCount = apt.ParkingLotUnderGround.Count;
            }
            else
            {
                parkingCount = apt.ParkingLotOnEarth.ParkingLines[0].Count + apt.ParkingLotUnderGround.Count;
            }
            
            double parkingRequired = apt.GetLegalParkingLotofHousing() + apt.GetLegalParkingLotOfCommercial();

            if (parkingCount >= parkingRequired)
                return true;
            else
            {
                ugpRequired = parkingRequired - parkingCount < 10? 10 : parkingRequired - parkingCount;
                return false;
            }
        }


        Apartment Add1F(Apartment apt)
        {
            bool canAdd1F = apt.Household.Count != TuringAndCorbusierPlugIn.InstanceClass.regSettings.EaseFloor - 1;
            if (!canAdd1F)
                return apt;

            if (apt.Core.Count == 0 || apt.Core[0].Count == 0)
                return apt;
            
            ParameterSet temp = apt.ParameterSet;
            temp.using1F = true;
            temp.fixedCoreType = apt.Core[0][0].CoreType;
            //temp.Parameters[0]++;
            temp.Parameters[1]++;

            //가능하면 agtype enum으로 만들면 ..
            switch (apt.AGtype)
            {
                case "PT-1":
                    AG1 ag1 = new AG1();
                    Apartment a1 = ag1.generator(apt.Plot, temp, apt.Target);
                    return a1 == null ? apt : a1;
                case "PT-3":
                    AG3 ag3 = new AG3();
                    Apartment a3 = ag3.generator(apt.Plot, temp, apt.Target);
                    return a3 == null ? apt : a3;
                //case "PT-4"://미구현
                //    AG1 ag4 = new AG1();
                //    Apartment a4 = ag4.generator(apt.Plot, temp, apt.Target);
                //    return a4 == null ? apt : a4;
                default:
                    return apt;
            }
           
        }

        Apartment SetBack(Apartment apt)
        {
            bool canSetBack = apt.Household.Count < TuringAndCorbusierPlugIn.InstanceClass.page1Settings.MaxFloors - 1;
            if (!canSetBack)
                return apt;

            if (apt.Core.Count == 0 || apt.Core[0].Count == 0)
                return apt;

            ParameterSet temp = apt.ParameterSet;
            temp.setback = true;
            temp.fixedCoreType = apt.Core[0][0].CoreType;
            temp.Parameters[0]++;
            temp.Parameters[1]++;

            //가능하면 agtype enum으로 만들면 ..
            switch (apt.AGtype)
            {
                case "PT-1":
                    AG1 ag1 = new AG1();
                    Apartment a1 = ag1.generator(apt.Plot, temp, apt.Target);
                    return a1 == null ? apt : a1;
                case "PT-3":
                    AG3 ag3 = new AG3();
                    Apartment a3 = ag3.generator(apt.Plot, temp, apt.Target);
                    return a3 == null ? apt : a3;
                //case "PT-4"://미구현
                //    AG1 ag4 = new AG1();
                //    Apartment a4 = ag4.generator(apt.Plot, temp, apt.Target);
                //    return a4 == null ? apt : a4;
                default:
                    return apt;
            }

        }

        Apartment Reduce(Apartment aptOverFAR)
        {
            if (aptOverFAR.AGtype == "PT-3")
            {
                double currentFA = aptOverFAR.GetGrossAreaRatio() / 100.0 * aptOverFAR.Plot.GetArea();
                double targetFA = TuringAndCorbusierPlugIn.InstanceClass.page1Settings.MaxFloorAreaRatio / 100.0 * aptOverFAR.Plot.GetArea();

                if (targetFA > currentFA)
                    return aptOverFAR;


                //initial setting
                double toReduceArea = targetFA - currentFA;
                double aptWidth = aptOverFAR.ParameterSet.Parameters[2];

                List<Core> topFloorCore = aptOverFAR.Core.Last();
                List<Household> topFloorHouseholds = aptOverFAR.Household.Last().First();
                int initialHouseCount = topFloorHouseholds.Count;
                int initialCoreCount = topFloorCore.Count;
                double currentFloorZ = topFloorHouseholds.First().Origin.Z;
                double currentCoreZ = topFloorCore.First().Origin.Z;


                //Core lengthParam setting
                Curve courtCenterLine = aptOverFAR.AptLines[0];
                Curve courtInnerLine = courtCenterLine.Offset(Plane.WorldXY, aptWidth / 2, 1, CurveOffsetCornerStyle.Sharp)[0];
                courtInnerLine.Translate(Vector3d.ZAxis * (currentCoreZ - courtInnerLine.PointAtStart.Z));

                List <Interval> entranceIntervals = new List<Interval>();

                for (int i = 0; i < topFloorCore.Count; i++)
                {
                    Core tempCore = topFloorCore[i];

                    if (tempCore.CoreType == CoreType.CourtShortEdge)
                    {
                        Point3d CoreStart = tempCore.Origin;
                        Point3d CoreEnd = tempCore.Origin + tempCore.XDirection * tempCore.Width;

                        double startParam, endParam;
                        courtInnerLine.ClosestPoint(CoreStart, out startParam);
                        courtInnerLine.ClosestPoint(CoreEnd, out endParam);

                        entranceIntervals.Add(new Interval(startParam, endParam));
                    }

                    else
                    {
                        Point3d CoreStart = tempCore.Origin + tempCore.YDirection * tempCore.Depth;
                        Point3d CoreEnd = tempCore.Origin + tempCore.XDirection * tempCore.Width;

                        double startParam, endParam;
                        courtInnerLine.ClosestPoint(CoreStart, out startParam);
                        courtInnerLine.ClosestPoint(CoreEnd, out endParam);

                        entranceIntervals.Add(new Interval(startParam, endParam));
                    }
                }

                courtInnerLine.Translate(Vector3d.ZAxis * (currentFloorZ - currentCoreZ));

                //Subtract
                int currentCoreCount = initialCoreCount;

                for (int i = 0; i < initialHouseCount; i++)
                {
                    //subtract household
                    if (currentFA < targetFA)
                        break;

                    int indexFromLast = initialHouseCount - 1 - i;
                    Household currentHouse = topFloorHouseholds[indexFromLast];

                    double reduceArea = currentHouse.GetArea();
                    topFloorHouseholds.RemoveAt(indexFromLast);

                    currentFA -= reduceArea;
                    toReduceArea -= reduceArea;

                    //subtract core
                    if (toReduceArea > 0 && topFloorCore.First().Area * currentCoreCount> toReduceArea)
                    {
                        if (topFloorHouseholds.Count == 0)
                            break;

                        Household firstHouse = topFloorHouseholds.First();
                        Household endHouse = topFloorHouseholds.Last();


                        Point3d houseStart = firstHouse.Origin + firstHouse.XDirection * firstHouse.XLengthA;
                        if (Math.Abs(firstHouse.YLengthB) > 0.5)
                            houseStart = firstHouse.Origin + firstHouse.YDirection * firstHouse.YLengthB;

                        Point3d houseEnd = endHouse.Origin;
                        if (Math.Abs(firstHouse.YLengthB) > 0.5)
                            houseEnd = endHouse.Origin - endHouse.XDirection * endHouse.XLengthB;

                        
                        double houseStartParam, houseEndParam;
                        courtInnerLine.ClosestPoint(houseStart, out houseStartParam);
                        courtInnerLine.ClosestPoint(houseEnd, out houseEndParam);

                        Interval houseInterval = new Interval(houseStartParam, houseEndParam);

                        for (int j = 0; j < topFloorCore.Count; j++)
                        {
                            int indexFromLastCore = initialCoreCount - 1- j;
                            Core currentCore = topFloorCore[indexFromLastCore];
                            Interval currentInterval = entranceIntervals[indexFromLastCore];

                            bool CanUseEntrance = currentInterval.Max > houseInterval.Min;
                            if (CanUseEntrance)
                                continue;

                            double reduceAreaCore = currentCore.Area;
                            topFloorCore.RemoveAt(indexFromLastCore);
                            entranceIntervals.RemoveAt(indexFromLastCore);

                            currentFA -= reduceAreaCore;
                            toReduceArea -= reduceAreaCore;
                        }
                    }
                }

                if (topFloorHouseholds.Count == 0) //나중에 여러 동 만들 경우 수정해야 함
                {
                    aptOverFAR.Household.RemoveAt(aptOverFAR.Household.Count - 1);
                    aptOverFAR.Core.RemoveAt(aptOverFAR.Core.Count - 1);
                }
                return aptOverFAR;
            }


            if (aptOverFAR.AGtype == "PT-4")
            {
                double currentFA = aptOverFAR.GetGrossAreaRatio() / 100.0 * aptOverFAR.Plot.GetArea();
                double targetFA = TuringAndCorbusierPlugIn.InstanceClass.page1Settings.MaxFloorAreaRatio / 100.0 * aptOverFAR.Plot.GetArea();

                if (targetFA > currentFA)
                    return aptOverFAR;

                double toReduceArea = targetFA - currentFA;

                List<Household> topFloorHouseholds = aptOverFAR.Household.Last().First();
                List<Core> topFloorCores = aptOverFAR.Core.Last();

                int initialHouseCount = topFloorHouseholds.Count;
                for (int i = 0; i < initialHouseCount; i++)
                {
                    if (currentFA < targetFA)
                        break;

                    int indexFromLast = initialHouseCount - 1 - i;
                    Household currentHouse = topFloorHouseholds[indexFromLast];
                    int coreIndex = indexFromLast / 2;

                    double reduceArea = currentHouse.GetArea();
                    topFloorHouseholds.RemoveAt(indexFromLast);

                    if (indexFromLast % 2 == 0)
                    {
                        reduceArea += topFloorCores[coreIndex].GetArea();
                        topFloorCores.RemoveAt(coreIndex);
                    }

                    currentFA -= reduceArea;
                }

                if (topFloorHouseholds.Count == 0) //나중에 여러 동 만들 경우 수정해야 함
                {
                    aptOverFAR.Household.RemoveAt(aptOverFAR.Household.Count - 1);
                    aptOverFAR.Core.RemoveAt(aptOverFAR.Core.Count - 1);
                }
                return aptOverFAR;
            }

            return aptOverFAR;
        }
    }
}
