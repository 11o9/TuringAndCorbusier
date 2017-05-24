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

        Apartment apt;
        public bool setBacked = false;
        public bool using1f = false;

        double ugpRequired = 0;

        public Finalizer(Apartment apt)
        {
            this.apt = apt;
        }

        public Finalizer(Apartment apt, int depth)
        {
            this.apt = apt;
            this.depth = depth + 1;
        }
        public Apartment Finilize()
        {
            //WriteLine("[{0}]Start Finalize",depth);
            ////far check
            //WriteLine("[{0}]FAR Check...", depth);
            if (IsSuitable())
            {
                //WriteLine("[{0}]FAR Enough...", depth);

                //WriteLine("[{0}]GP Check...", depth);
                //suit
                //ground parking enough
                //generate ground parking

                if (ParkingsEnough())
                {
                    //WriteLine("[{0}]GP Enough...", depth);
                    //WriteLine("[{0}]return value", depth);
                    return apt;
                }
                else
                {
                    //WriteLine("[{0}]GP Lack...", depth);
                    ////can make undergroundparking
                    //WriteLine("[{0}]UGP Check...", depth);
                    UnderGroundParkingModule ugpm = new UnderGroundParkingModule(apt.Plot.Boundary, (int)ugpRequired);
                    bool canUGP = ugpm.CheckPrecondition();
                    if (canUGP)
                    {
                        //UnderGroundParkingModule ...
                        //WriteLine("[{0}]UGP Creating", depth);
                        //WriteLine("[{0}]return value", depth);
                        bool ugpResult = ugpm.Calculate();
                        if (ugpResult)
                        {
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
                                return Reduce();

                           
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
                                {
                                    return apt;//with new ugp
                                }
                            }
                        }
                        else
                        {
                            return Reduce();
                        }
                        
                    }
                    else
                    {
                        //WriteLine("[{0}]UGP Impossible", depth);
                        //WriteLine("[{0}]return Reduced value", depth);
                        return Reduce();
                    }
                }

                
            }
            else
            {
                //WriteLine("[{0}]FAR Lack...", depth);
                //WriteLine("[{0}]Check SetBack...", depth);
                if (!setBacked)
                {
                    //WriteLine("[{0}]Can Set Back...", depth);
                    //WriteLine("[{0}]Set Back...", depth);
                    Apartment setBacked = SetBack(apt);
                    Finalizer fnz = new Finalizer(setBacked, depth);
                    fnz.setBacked = true;
                    //set back and Recursive
                    //WriteLine("[{0}]return SetBack...", depth);
                    return fnz.Finilize();
                }
                else
                {
                    //WriteLine("[{0}]Can't Set Back...", depth);
                    //WriteLine("[{0}]Check Using1F...", depth);
                    if (!using1f)
                    {
                        //WriteLine("[{0}]Can Using1F...", depth);
                        //WriteLine("[{0}]Using 1F...", depth);
                        //지상주차 계산도 다시 해야함.
                        Apartment using1f = Add1F(apt);
                        Finalizer fnz = new Finalizer(using1f,depth);
                        fnz.setBacked = true;
                        fnz.using1f = true;
                        //use1f and Recursive
                        //WriteLine("[{0}]return Using 1F...", depth);
                        return fnz.Finilize();
                    }
                    else
                    {
                        //WriteLine("[{0}]can't do anything , return basic value", depth);
                        return apt;
                    }
                }
              
            }

            return apt;
        }

        bool IsSuitable()
        {
            //이미 해볼거 다 해봐서 방법이 없음 , 주차만들고 땡
            if (setBacked && using1f)
                return true;

            double tempFAR = apt.GetGrossAreaRatio();
            double FARlimit = TuringAndCorbusierPlugIn.InstanceClass.page1Settings.MaxFloorAreaRatio;
            
            if (tempFAR >= FARlimit*0.9 && tempFAR< FARlimit)
                return true;
            else
                return false;
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

        //내부변수로대체
        //bool CanUGP()
        //{
        //    double d = random.NextDouble();
        //    if (d > 0.5)
        //        return true;
        //    else
        //        return false;
        //}

        Apartment Add1F(Apartment apt)
        {
            bool canAdd1F = apt.Household.Count < 6;
            if (!canAdd1F)
                return apt;

            if (apt.Core.Count == 0 || apt.Core[0].Count == 0)
                return apt;
            
            ParameterSet temp = apt.ParameterSet;
            temp.using1F = true;
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
                //case "PT-3"://미구현
                //    AG3 ag3 = new AG3();
                //    Apartment a3 = ag3.generator(apt.Plot, temp, apt.Target);
                //    return a3 == null ? apt : a3;
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
            bool canSetBack = apt.Household.Count < 6;
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
                //case "PT-3"://미구현
                //    AG3 ag3 = new AG3();
                //    Apartment a3 = ag3.generator(apt.Plot, temp, apt.Target);
                //    return a3 == null ? apt : a3;
                //case "PT-4"://미구현
                //    AG1 ag4 = new AG1();
                //    Apartment a4 = ag4.generator(apt.Plot, temp, apt.Target);
                //    return a4 == null ? apt : a4;
                default:
                    return apt;
            }

        }


        Apartment Reduce()
        {
            //reduce code
            return apt;
        }

    }
}
