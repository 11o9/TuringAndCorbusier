using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rhino.Geometry;
using System.Collections;
using Rhino.Collections;


namespace TuringAndCorbusier
{
    class AG1 : ApartmentGeneratorBase
    {
        CurveConduit regulationDebug = new CurveConduit(System.Drawing.Color.Red);
        


        public override Apartment generator(Plot plot, ParameterSet parameterSet, Target target)
        {
            ///////////////////////////////////////////////
            //////////  common initial settings  //////////
            ///////////////////////////////////////////////

            // for using 1F
            //입력"값" 부분
            randomCoreType = GetRandomCoreType();
            double pilotiHeight = Consts.PilotiHeight;
            //#######################################################################################################################
            if (parameterSet.using1F)
            {
                randomCoreType = parameterSet.fixedCoreType;
                pilotiHeight = 0;
            }
            //#######################################################################################################################


            double[] parameters = parameterSet.Parameters;
            double storiesHigh = Math.Max((int)parameters[0], (int)parameters[1]);
            double storiesLow = Math.Min((int)parameters[0], (int)parameters[1]);
            double width = parameters[2];
            double angleRadian = parameters[3];
            double moveFactor = parameters[4];
            Regulation regulationHigh = new Regulation(storiesHigh);
            Regulation regulationLow = new Regulation(storiesHigh, storiesLow);
            //List<double> ratio = target.Ratio;
            //List<double> area = target.Area.Select(n => n / 0.91 * 1000 * 1000).ToList();
            //double areaLimit = Consts.AreaLimit;
            BuildingType buildingType = regulationHigh.BuildingType;
            List<double> areaLength = new List<double>();

            double coreWidth = randomCoreType.GetWidth();
            double coreDepth = randomCoreType.GetDepth();

            double corearea = coreWidth * coreDepth;


            //for (int i = 0; i < area.Count; i++)
            //{
            //    if (area[i] < areaLimit)
            //    {
            //        //서비스면적 10%
            //        area[i] = area[i] * Consts.balconyRate_Corridor;
            //        //코어&복도
            //        area[i] = area[i] * (1 + Consts.corridorWidth / (width - Consts.corridorWidth));

            //    }
            //    else
            //    {

            //        //서비스면적 18%
            //        area[i] = area[i] * Consts.balconyRate_Tower;
            //        //코어&복도
            //        area[i] += corearea / 2;

            //    }
            //}

            //최고층수 추가, 목표세대수 / 층수 해서 라인 수 찾으려고.
            List<Unit> units = target.ToUnit(width, corearea, storiesHigh);

         

            //#######################################################################################################################
            if (parameterSet.using1F && !parameterSet.setback)
            {
               
                if (regulationHigh.byLightingCurve(plot, angleRadian).Length == 0 || regulationHigh.fromNorthCurve(plot).Length == 0)
           
                {
                 
                    return null;
                }

                regulationHigh = new Regulation(storiesHigh, true);
                regulationLow = new Regulation(storiesHigh, storiesLow, true);
            }
            else if (parameterSet.setback && !parameterSet.using1F)
            {
                if (regulationHigh.byLightingCurve(plot, angleRadian).Length == 0 || regulationHigh.fromNorthCurve(plot).Length == 0)
                {
                    return null;
                }
                //최상층 법규선 - 동간거리 사용
                regulationHigh = new Regulation(storiesHigh);

                //최상층 바로 아래층 법규선 - 동간거리 제외한 각종 법규선 사용하여 최상층까지 올림
                regulationHigh.Fake();
                //마지막에 최상층과 최상층법규선 대조, 외곽선 후퇴. //unfake
            }
            //#######################################################################################################################
            else
            {
                while (true)
                {
                    //var lightingv = regulationHigh.byLightingCurve(plot, angleRadian)[0].ClosedCurveOrientation(Vector3d.ZAxis);
                    //var plotv = plot.Boundary.ClosedCurveOrientation(Vector3d.ZAxis);
                    if (regulationHigh.byLightingCurve(plot, angleRadian).Length == 0 || regulationHigh.fromNorthCurve(plot).Length == 0)
                    //if (lightingv != plotv)
                    {
                        storiesHigh--;
                        if (storiesHigh <= storiesLow)
                            break;
                        regulationHigh = new Regulation(storiesHigh);
                        regulationLow = new Regulation(storiesHigh, storiesLow);
                    }
                    else
                        break;
                   
                }
        
            }

            ///////////////////////////
            //sort lists before start//
            //RhinoList<double> ratioRL = new RhinoList<double>(ratio);
            //ratioRL.Sort(area.ToArray());
            //ratio = ratioRL.ToList();
            //area.Sort();
            ///////////////////////////

            //입력 "대지경계" 부분
            Curve boundary = CommonFunc.adjustOrientation(plot.SimplifiedBoundary);

            //이거바꿨음
            Curve[] plotArr = plot.Boundary.DuplicateSegments();
            //법규 : 대지 안의 공지
            Curve[] surroundingsHigh = regulationHigh.fromSurroundingsCurve(plot);
            Curve[] surroundingsLow = regulationLow.fromSurroundingsCurve(plot);

            //법규 : 일조에 의한 높이제한
            Curve[] northHigh = regulationHigh.fromNorthCurve(plot);
            Curve[] northLow = regulationLow.fromNorthCurve(plot);

            ///////////////////////////////////////////
            //////////  additional settings  //////////
            ///////////////////////////////////////////
            
            //법규 : 인접대지경계선(채광창)
            Curve[] lightingHigh = regulationHigh.byLightingCurve(plot, angleRadian);
            Curve[] lightingLow = regulationLow.byLightingCurve(plot, angleRadian);

            ////
            //step 1 : create baselines for buildings
            //step 2 : create cores
            //법규 : low, 건물 기준선 : high

            //일부 조건(대지안의공지, 일조에 의한 높이제한)을 만족하는 경계선
            //모든 조건(대지안의공지, 일조에의한 높이제한, 인접대지경계선)을 만족하는 경계선
            Curve[] partialRegulationLow = CommonFunc.JoinRegulation(surroundingsLow, northLow);
            Curve[] wholeRegulationLow = CommonFunc.JoinRegulation(partialRegulationLow, lightingLow);

            ////

            ////
            //setp 3 : cuts with regulation(high)
            //법규 : high, 건물 기준선 : high

            //일부 조건(대지안의공지, 일조에 의한 높이제한)을 만족하는 경계선
            //모든 조건(대지안의공지, 일조에의한 높이제한, 인접대지경계선)을 만족하는 경계선
            Curve[] partialRegulationHigh = CommonFunc.JoinRegulation(surroundingsHigh, northHigh);
            Curve[] wholeRegulationHigh = CommonFunc.JoinRegulations(northHigh, surroundingsHigh, lightingHigh);


            //regulation debug
            //regulationDebug.CurveToDisplay = lightingHigh.ToList();
            //regulationDebug.Enabled = true;

            ////////////////////////////////////
            //////////  apt baseline  //////////
            ////////////////////////////////////

            //List<Line> baselines = baselineMaker(wholeRegulationHigh, parameterSet);
            List<Line> parkingLines = new List<Line>();
            List<Curve> aptLines = NewLineMaker(wholeRegulationHigh, parameterSet, out parkingLines);



            #region new code
            ////////////////////////////////////
            //////////  zzzzzzzzzzz   //////////
            ////////////////////////////////////

            #region UnitPacking
            List<List<UnitType>> isclearance;
            List<List<double>> lengths = AptPacking(units, aptLines.Select(n => n.GetLength()).ToList(), width, corearea, storiesHigh, plot.GetArea(), out isclearance);
            
            
            //shorten
            for (int i = 0; i < aptLines.Count; i++)
            {
                if (lengths[i].Count == 0)
                    continue;

                double lengthleft = aptLines[i].GetLength() - lengths[i].Last();
                if (lengthleft > 0)
                {
                    var newstart = aptLines[i].PointAtLength(lengthleft / 2);
                    var newend = aptLines[i].PointAtLength(aptLines[i].GetLength() - lengthleft / 2);
                    aptLines[i].SetStartPoint(newstart);
                    aptLines[i].SetEndPoint(newend);
                }
            }

            #endregion

            #region GetLow
            List<List<Household>> Low = new List<List<Household>>();

            //buildingnumber..
            int buildingnum = -1;

            for (int i = 0; i < aptLines.Count; i++)
            {

                buildingnum++;
                List<Household> z = new List<Household>();

                List<Curve> shattered = new List<Curve>();
                List<double> lengthparams = new List<double>();
                double lengthstack = 0;
                for (int j = 0; j < lengths[i].Count - 1; j++)
                {
                    //lengthstack += lengths[i][j];
                    lengthstack = lengths[i][j];
                    double param = 0;
                    double length = aptLines[i].GetLength();
                    aptLines[i].LengthParameter(lengthstack, out param);
                    lengthparams.Add(param);
                }
                if (lengthparams == null || lengthparams.Count == 0)
                    continue;

                shattered = aptLines[i].Split(lengthparams).ToList();

                int houseindex = 0;

                if (shattered.Count != isclearance[i].Count)
                    continue;

                HouseholdGenerator hhg = new HouseholdGenerator(width, coreWidth, coreDepth);

                for (int j = 0; j < isclearance[i].Count; j++)
                {

                    //조각j로 초기화
                    hhg.Initialize(shattered[j]);
                    #region create new hhp
                    //Household temp = new Household();
                    //temp.XDirection = -shattered[j].TangentAtStart;
                    //var y = new Vector3d(shattered[j].TangentAtStart);
                    //y.Rotate(Math.PI / 2, Vector3d.ZAxis);
                    //temp.YDirection = y;
                    //temp.XLengthA = shattered[j].GetLength();
                    //temp.HouseholdSizeType = 0;
                    #endregion
                    if (isclearance[i][j] != UnitType.Clearance)
                    {
                        //복도형
                        if (isclearance[i][j] == UnitType.Corridor)
                        {
                            Household corridor = hhg.Generate(UnitType.Corridor, buildingnum, houseindex);
                            z.Add(corridor);
                            houseindex++;
                            if (isclearance[i].Count > 1)
                            {
                                int next = (j + 1) % isclearance[i].Count;
                                if (isclearance[i][next] == UnitType.Tower)
                                {
                                    houseindex = 0;
                                    buildingnum++;
                                }
                            }
                        }
                        //일반
                        else
                        {
                            Household tower = hhg.Generate(UnitType.Tower, buildingnum, houseindex);
                            z.Add(tower);
                            houseindex++;
                        }
                    }
                    //clearance
                    else
                    {
                        buildingnum++;
                        houseindex = 0;
                        continue;
                    }
                }


                Low.Add(z);
            }

            #endregion

            #region core
            List<Core> cps = new List<Core>();
            List<List<Core>> cpss = new List<List<Core>>();
            for (int i = 0; i < Low.Count; i++)
            {
                if (Low[i].Count == 0)
                    continue;

                int maxindex = Low[i].Max(n => n.BuildingGroupNum);
                int minindex = Low[i].Min(n => n.BuildingGroupNum);
                // var templowbuildingcount = Low[i].Max(n => n.indexer[0]);

                for (int j = minindex; j <= maxindex; j++)
                {
                    var tempBuildingCorridorUnits = Low[i].Where(n => n.indexer[0] == j && n.isCorridorType).ToList();

                    if (tempBuildingCorridorUnits.Count != 0)
                    {
                        double wholeLength = tempBuildingCorridorUnits[0].XLengthA * tempBuildingCorridorUnits.Count;

                        Core corep = new Core();

                        var op = new Point3d(tempBuildingCorridorUnits.Last().Origin);
                        op += tempBuildingCorridorUnits[0].XDirection * (wholeLength / 2 + coreWidth / 2);

                        //코어 안튀어나가게 하기위한 임시방편
                        op += tempBuildingCorridorUnits[0].YDirection * (-coreDepth + Consts.corridorWidth);

                        corep.Origin = op;
                        corep.Stories = 0;
                        corep.XDirection = -tempBuildingCorridorUnits[0].XDirection;
                        corep.YDirection = tempBuildingCorridorUnits[0].YDirection;
                        corep.CoreType = randomCoreType;
                        corep.Width = randomCoreType.GetWidth();
                        corep.Depth = randomCoreType.GetDepth();

                        corep.BuildingGroupNum = j;
                        corep.Area = corearea;


                        cps.Add(corep);

                        //이거뭐지....왜.. // getArea가 이상했음
                        double aaarea = corep.GetArea();


                    }

                    var tempBuildingTowerUnits = Low[i].Where(n => n.indexer[0] == j && !n.isCorridorType).ToList();

                    for (int k = 0; k < tempBuildingTowerUnits.Count; k++)
                    {
                        int houseindex = tempBuildingTowerUnits[k].indexer[1];

                        if (houseindex % 2 == 0)
                        {

                            Core corep = new Core();

                            corep.Origin = new Point3d(tempBuildingTowerUnits[k].Origin);
                            corep.Stories = 0;
                            corep.XDirection = -tempBuildingTowerUnits[k].XDirection;
                            corep.YDirection = tempBuildingTowerUnits[k].YDirection;
                            corep.CoreType = randomCoreType;
                            corep.Depth = randomCoreType.GetDepth();
                            corep.Width = randomCoreType.GetWidth();

                            corep.BuildingGroupNum = j;
                            corep.Area = corearea;
                            cps.Add(corep);
                        }
                    }

                }
            }
            #endregion

            #region expectedfar

            //var plotarea = plot.GetArea();
            //var firstfloorarea = Low.Sum(n => n.Sum(m => m.ExclusiveArea + m.CorridorArea));
            //var firstfloorcore = cps.Sum(n => n.GetArea());
            //var legalfar = 2.0;
            //if (plot.PlotType == PlotType.제1종일반주거지역)
            //    legalfar = 1.5;
            //if (plot.PlotType == PlotType.제3종일반주거지역)
            //    legalfar = 2.5;
            //if (plot.PlotType == PlotType.상업지역)
            //    legalfar = 13;
            //while ((firstfloorarea + firstfloorcore) / plotarea * storiesHigh  > legalfar )
            //{
            //    storiesHigh--;
            //}

            #endregion 

            #region stack

            List<List<List<Household>>> hhps = new List<List<List<Household>>>();
            for (int i = 0; i < storiesHigh; i++)
            {

         
                double tempStoryHeight = pilotiHeight + i * Consts.FloorHeight;
                Regulation tempStoryReg = new Regulation(storiesHigh, i);
                Curve[] Reg = wholeRegulationHigh;

                if (Reg.Length == 0)
                { bool ohno = true; }
                List<List<Household>> tempfloor = new List<List<Household>>();

                foreach (var xx in Low)
                {
                    var newline = new List<Household>();
                    foreach (var x in xx)
                    {
                        foreach (var r in Reg)
                        {
                            var newhhp = new Household(x);
                            newhhp.Origin = x.Origin + Vector3d.ZAxis * tempStoryHeight;
                            newhhp.MoveLightingAndMoveAble();
                            Curve outline = newhhp.GetOutline();
                            //법규체크?
                            var intersect = Rhino.Geometry.Intersect.Intersection.CurveCurve(r, outline, 0, 0);
                            if (intersect.Count > 1)
                                newhhp.Origin = Point3d.Origin;

                            newline.Add(newhhp);
                        }
                    }

                    tempfloor.Add(newline);
                }
                hhps.Add(tempfloor);
            }


            for (int i = 0; i < storiesHigh + 2; i++)
            {
    
                //1층 사용시 필로티코어 만들지 않음.
                if (parameterSet.using1F && i == 0)
                    continue;
                double tempStoryHeight = (i == 0) ? 0 : pilotiHeight + Consts.FloorHeight * (i - 1);
                Regulation tempStoryReg = new Regulation(storiesHigh, i);
                Curve[] Reg = tempStoryReg.JoinRegulations(plot, angleRadian);
                List<Core> tempfloor = new List<Core>();

                foreach (var x in cps)
                {
                    var newcp = new Core(x);
                    newcp.Origin = x.Origin + Vector3d.ZAxis * tempStoryHeight;
                    newcp.Stories = i;

                    tempfloor.Add(newcp);
                }
                cpss.Add(tempfloor);
            }



            //################################################################################################
            if (parameterSet.setback)
            {

                if (hhps.Count == 0 || cpss.Count <= 3)
                { }
                else
                {
                    //최상층 외곽선 후퇴옵션 사용하는 경우 최상층 조정...?
                    //용적 제대로 나올지 미지수

                    List<Household> topHouses = new List<Household>();
                    hhps.Last().ToList().ForEach(n => topHouses.AddRange(n));

                    List<Core> topCores = cpss.Last().ToList();

                    regulationHigh.UnFake();
                    Curve[] topNorth = regulationHigh.fromNorthCurve(plot);

                    //일조사선 걸리는 코어들 제거, 연결된 house 제거
                    List<Curve> topCoreOutlines = topCores.Select(n => n.DrawOutline()).ToList();
                    topCoreOutlines.ForEach(n => n.Translate(Vector3d.ZAxis * -n.PointAtStart.Z));
                    List<bool> remove = new List<bool>();
                    topCoreOutlines.ForEach(n => remove.Add(false));

                    for (int i = 0; i < topNorth.Length; i++)
                    {
                        Curve tempNorth = topNorth[i];
                        for (int j = 0; j < topCoreOutlines.Count; j++)
                        {
                            if (remove[j])
                                continue;
                            var collision = Curve.PlanarCurveCollision(tempNorth, topCoreOutlines[j], Plane.WorldXY, 0);
                            if (collision)
                                remove[j] = true;
                        }
                    }

                    for (int i = remove.Count - 1; i >= 0; i--)
                    {
                        if (remove[i])
                        {
                            foreach (var hh in hhps.Last())
                            {
                                List<int> toRemove = new List<int>();
                                foreach (var h in hh)
                                {
                                    Curve outline = h.GetOutline();
                                    Curve coreoutline = cpss[cpss.Count - 2][i].DrawOutline();
                                    var col = Curve.PlanarCurveCollision(outline, coreoutline, Plane.WorldXY, 0);
                                    if (col)
                                    {
                                        toRemove.Add(hh.IndexOf(h));
                                    }
                                }

                                for (int j = toRemove.Count - 1; j >= 0; j--)
                                {
                                    hh.RemoveAt(toRemove[j]);
                                }
                            }
                            cpss.Last().RemoveAt(i);
                            //cpss[cpss.Count - 2].RemoveAt(i);
                        }
                    }
                    northHigh = regulationHigh.fromNorthCurve(plot);
                    surroundingsHigh = regulationHigh.fromSurroundingsCurve(plot);
                    lightingHigh = regulationHigh.byLightingCurve(plot, angleRadian);
                    wholeRegulationHigh = CommonFunc.JoinRegulations(northHigh, surroundingsHigh, lightingHigh);

                    if (wholeRegulationHigh.Length == 1)
                    {
                        foreach (var hh in hhps.Last())
                        {
                            List<int> removeIndex = new List<int>();
                            foreach (var h in hh)
                            {
                                var contractResult = h.Contract(wholeRegulationHigh[0]);
                                if (!contractResult)
                                    removeIndex.Add(hh.IndexOf(h));
                            }

                            for (int i = removeIndex.Count - 1; i >= 0; i--)
                            {
                                hh.RemoveAt(removeIndex[i]);
                            }
                        }

                        if (hhps.Last().Count == 0)
                            hhps.RemoveAt(hhps.Count - 1);
                    }
                }
            }
            //################################################################################################

            #endregion

            #region parkingregacy

            //Curve centerCurve = new LineCurve(Point3d.Origin, Point3d.Origin);
            //if (aptLines.Count() != 0)
            //    centerCurve = aptLines[0];

            //Plot forParking = new Plot(plot);
            //var segs = forParking.Boundary.DuplicateSegments();

            //List<Curve> regions = new List<Curve>();
            //for (int i = 0; i < segs.Length; i++)
            //{
            //    if (forParking.Surroundings[i] == 0)
            //    {
            //        var p1 = segs[i].PointAtStart;
            //        var p2 = segs[i].PointAtEnd;
            //        var v = segs[i].TangentAtStart;
            //        v.Rotate(-Math.PI / 2, Vector3d.ZAxis);
            //        segs[i].Translate(v * 5000);
            //        var region = new Polyline(new Point3d[] { segs[i].PointAtStart, segs[i].PointAtEnd, p2, p1, segs[i].PointAtStart });
            //        regions.Add(region.ToNurbsCurve());
            //    }
            //}

            //if (regions.Count != 0)
            //{
            //    Curve original = forParking.Boundary.DuplicateCurve();
            //    var diff = Curve.CreateBooleanDifference(original, regions);

            //    if (diff.Length == 0)
            //    { }
            //    else
            //    {
            //        forParking.Boundary = diff[0];
            //        forParking.SimplifiedBoundary = diff[0];
            //    }
            //    //Rhino.RhinoDoc.ActiveDoc.Objects.Add(forParking.Boundary);
            //}


            //for (int i = parkingLotOnEarth.ParkingLines.Count - 1; i >= 0; i--)
            //{
            //    for (int j = parkingLotOnEarth.ParkingLines[i].Count - 1; j >= 0; j--)
            //    {
            //        var testpoint = new Point3d(parkingLotOnEarth.ParkingLines[i][j].Boundary.Center.X, parkingLotOnEarth.ParkingLines[i][j].Boundary.Center.Y, 0);

            //        if (forParking.Boundary.Contains(testpoint) == PointContainment.Outside)
            //        {
            //            parkingLotOnEarth.ParkingLines[i].RemoveAt(j);
            //        }
            //    }
            //}

            //ParkingLotOnEarth parkingLotOnEarth = new ParkingLotOnEarth(ParkingLineMaker.parkingLineMaker(this.GetAGType, cpss, forParking, parameters[2], centerCurve)); //parkingLotOnEarthMaker(boundary, household, parameterSet.CoreType.GetWidth(), parameterSet.CoreType.GetDepth(), coreOutlines);

            #endregion

            #region NewParking

            ParkingModule pm = new ParkingModule();

            #region ParkingSetup
            ///setups for parking
            //1. curves
            var parkingCurves = parkingLines.Select(n => n.ToNurbsCurve() as Curve).ToList();

            Vector3d setBack = parkingLines.Count > 0 ? parkingLines[0].Direction : Vector3d.Zero;
            setBack.Unitize();
            setBack.Rotate(-Math.PI / 2, Vector3d.ZAxis);
            parkingCurves.ForEach(n => n.Translate(setBack * width / 2));

            //2. obstacles
            List<Curve> obstacles = cpss[0].Select(n => n.DrawOutline(width)).ToList();
            for (int i = 0; i < obstacles.Count; i++)
            {
                obstacles[i].Translate(cpss[0][i].YDirection * (cpss[0][i].Depth - width) / 2);
            }
            #endregion

            //pattern 1 parking settings
            pm.ParkingLines = parkingCurves; // 만약 라인마다 다른 depth로 뽑고싶다면 따로따로
            pm.Obstacles = obstacles;
            pm.Boundary = plot.Boundary;

            pm.Distance = width + regulationHigh.DistanceLL;
            pm.CoreDepth = coreDepth;
            pm.AddFront = true;

            //한줄짜리일때 주차 변형
            if (aptLines.Count == 1)
            {
                pm.UseInnerLoop = false;
                pm.LineType = ParkingLineType.SingleOneline;
            }
            //get parking
            ParkingLotOnEarth parkingLot = pm.GetParking();
            //if using1f
            if (parameterSet.using1F)
                parkingLot = new ParkingLotOnEarth();

            #endregion


            //finalize
            Apartment result = new Apartment(GetAGType, plot, buildingType, parameterSet, target, cpss, hhps, parkingLot, new ParkingLotUnderGround(), new List<List<Curve>>(), aptLines);

            //#######################################################################################################################
            if (parameterSet.using1F||parameterSet.setback)
            {

            }
            //#######################################################################################################################

            else
            {
                Finalizer finalizer = new Finalizer(result);
                result = finalizer.Finilize();
            }
            
            //ParkingDistributor.Distribute(ref result);
            //하아..
            //var result = new Apartment(GetAGType, plot, buildingType, parameterSet, target, cpss, hhps, parkingLot, parkingLotUnderGroud, new List<List<Curve>>(), aptLines);
            result.BuildingGroupCount = buildingnum;
            result.topReg = wholeRegulationHigh;
            return result;
            #endregion

        }

        #region GASetting
        ///////////////////////////////////////////
        //////////  GA initial settings  //////////
        ///////////////////////////////////////////

        //Parameter 최소값 ~ 최대값 {storiesHigh, storiesLow, width, angle, moveFactor}
        private double[] minInput = { 3, 3, 5000, 0, 0 };
        //private double[] minInput = { 6, 6, 10500, 0, 0 };
        private double[] maxInput = { TuringAndCorbusierPlugIn.InstanceClass.page1Settings.MaxFloors-1, TuringAndCorbusierPlugIn.InstanceClass.page1Settings.MaxFloors - 1, 13000, 2 * Math.PI, 1 };

        //Parameter GA최적화 {mutation probability, elite percentage, initial boost, population, generation, fitness value, mutation factor(0에 가까울수록 변동 범위가 넓어짐)
        private double[] GAparameterset = { 0.1, 0.05, 3, 120, 2, 3, 1 }; //원본
                                                                          //private double[] GAparameterset = { 0.2, 0.03, 1, 5, 1, 3, 1 }; //테스트


        //private double[] GAparameterset = { 0.2, 0.03, 1, 100, 5, 3, 1 };

        public override string GetAGType
        {
            get
            {
                return "PT-1";
            }
        }

        public override double[] MinInput
        {
            get { return minInput; }
            set
            {
                double[] valueArr = value as double[];

                if (valueArr.Length == minInput.Length)
                {
                    minInput = valueArr;
                }
            }
        }

        private static Random AGRandom = new Random();

        public override CoreType GetRandomCoreType()
        {
            CoreType[] tempCoreTypes = { CoreType.Parallel, CoreType.Horizontal };
            return tempCoreTypes[(int)(AGRandom.Next(0, tempCoreTypes.Length))];
        }

        public override bool IsCoreProtrude { get { return true; } }

        public override double[] MaxInput
        {
            get { return maxInput; }
            set
            {
                double[] valueArr = value as double[];

                if (valueArr.Length == maxInput.Length)
                {
                    maxInput = valueArr;
                }
            }
        }

        public override double[] GAParameterSet
        {
            get
            {
                return GAparameterset;
            }
        }
        #endregion GASetting

        //////////////////////////////////
        //////////  apt baseline  //////////
        ////////////////////////////////////

        //new line maker
        private List<Curve> NewLineMaker(Curve[] regCurve, ParameterSet parameterSet, out List<Line> ParkingLines)
        {

            Curve[] RegCurve = regCurve.Select(n => n.DuplicateCurve()).ToArray();
            List<Line> parkingLines = new List<Line>();
            List<Curve> aptlines = new List<Curve>();
            foreach (var regulationCurve in RegCurve)
            {

                Curve temp = regulationCurve.DuplicateCurve();
                double[] parameters = parameterSet.Parameters;
                double storiesHigh = Math.Max((int)parameters[0], (int)parameters[1]);
                double storiesLow = Math.Min((int)parameters[0], (int)parameters[1]);
                double width = parameters[2];
                double angleRadian = parameters[3];
                double moveFactor = parameters[4];
                Regulation reg = new Regulation(storiesHigh);
                //Curve regZero = reg.


                Polyline regpoly;
                temp.TryGetPolyline(out regpoly);

                Point3d rotatecenter = regpoly.CenterPoint();
                temp.Rotate(angleRadian, Vector3d.ZAxis, rotatecenter);
                var boundingbox = temp.GetBoundingBox(false);

                double ygap = boundingbox.Max.Y - boundingbox.Min.Y;

                List<double> param = new List<double>();
                List<Line> result = new List<Line>();
                double linecount = 1;
                double unitlength = width;
                double z = width;
                double y = reg.DistanceLL;
                param.Add(0);
                while (unitlength < ygap)
                {
                    linecount++;
                    unitlength = z * linecount + y * (linecount - 1);
                    if (unitlength < ygap)
                        param.Add(unitlength);
                }

                if (unitlength > ygap)
                    unitlength -= z + y;

                double lengthremain = ygap - unitlength;
                param[0] += z;
                #region After

                List<double> wholeLengths = new List<double>();
                List<Curve> maximumLines = new List<Curve>();
                double MaxLength = double.MinValue;
                double MaxPosition = 0;
                double step = 500;

                for (int k = 0; k < ygap / step; k++)
                {
                    List<Line> tempResult = new List<Line>();

                    double[] tempParam = param.ToArray();

                    //parameter[i] + 단위길이 - width/2 위치에 라인 생성
                    for (int i = 0; i < param.Count; i++)
                    {
                        tempParam[i] = param[i] + k * step - z / 2;
                        Line templine = new Line(new Point3d(boundingbox.Min.X, boundingbox.Min.Y + tempParam[i], 0), new Point3d(boundingbox.Max.X, boundingbox.Min.Y + tempParam[i], 0));
                        tempResult.Add(templine);
                    }


                    //생성된 라인을 역회전 하여 기존 상태로 되돌림.
                    for (int i = 0; i < tempResult.Count; i++)
                    {
                        Line tempr = tempResult[i];
                        tempr.Transform(Transform.Rotation(-angleRadian, Vector3d.ZAxis, rotatecenter));
                        tempResult[i] = tempr;
                    }


                    List<Curve> tempAptlines = new List<Curve>();

                    //offset한 라인마다 충돌체크,길이조정
                    for (int i = 0; i < tempResult.Count; i++)
                    {
                        Curve[] inner = InnerRegion(regulationCurve, tempResult[i].ToNurbsCurve(), z);
                        tempAptlines.AddRange(inner);
                    }

                    //결과 값의 길이 확인
                    var AvailableLength = GetAvailableLength(tempAptlines);
                    //wholeLines.AddRange(aptlines);
                    wholeLengths.Add(AvailableLength);
                    if (MaxLength < AvailableLength)
                    {
                        MaxLength = AvailableLength;
                        maximumLines = tempAptlines;
                        MaxPosition = k * step;
                        parkingLines = tempResult;
                    }

                }

                #endregion
                #region Before


                //param.Select(n => n + lengthremain / 2 - z / 2).ToList();
                //param[0] += z;
                //for (int i = 0; i < param.Count; i++)
                //{
                //    param[i] += lengthremain / 2 - z / 2;
                //    Line templine = new Line(new Point3d(boundingbox.Min.X, boundingbox.Min.Y + param[i], 0), new Point3d(boundingbox.Max.X, boundingbox.Min.Y + param[i], 0));
                //    result.Add(templine);
                //}

                //for (int i = 0; i < result.Count; i++)
                //{
                //    Line tempr = result[i];

                //    tempr.Transform(Transform.Rotation(-angleRadian, Vector3d.ZAxis, rotatecenter));

                //    result[i] = tempr;
                //}
                //List<Curve> up = new List<Curve>();
                //List<Curve> down = new List<Curve>();

                //up = result.Select(n => n.ToNurbsCurve().DuplicateCurve()).ToList();
                //down = result.Select(n => n.ToNurbsCurve().DuplicateCurve()).ToList();

                //Vector3d v = result[0].UnitTangent;
                //v.Rotate(Math.PI / 2, Vector3d.ZAxis);
                //v.Unitize();



                //for (int i = 0; i < up.Count; i++)
                //{
                //    up[i].Transform(Transform.Translation(v * z / 2));
                //    down[i].Transform(Transform.Translation(-v * z / 2));



                //    var i1 = Rhino.Geometry.Intersect.Intersection.CurveCurve(up[i], regulationCurve, 0, 0);
                //    var i2 = Rhino.Geometry.Intersect.Intersection.CurveCurve(down[i], regulationCurve, 0, 0);

                //    var splitparams = i1.Select(n => n.ParameterA).ToList();
                //    var splitparams2 = i2.Select(n => n.ParameterA).ToList();

                //    splitparams.AddRange(splitparams2);

                //    var splted1 = up[i].Split(splitparams);
                //    var splted2 = down[i].Split(splitparams);
                //    var spltedmain = result[i].ToNurbsCurve().Split(splitparams);

                //    List<Curve> survivor = new List<Curve>();

                //    for (int j = 0; j < splted1.Length; j++)
                //    {
                //        if (regulationCurve.Contains(splted1[j].PointAtNormalizedLength(0.5)) == PointContainment.Inside
                //            && regulationCurve.Contains(splted2[j].PointAtNormalizedLength(0.5)) == PointContainment.Inside)
                //        {
                //            survivor.Add(spltedmain[j]);
                //        }
                //    }

                //    aptlines.AddRange(Curve.JoinCurves(survivor));
                //}
                #endregion

                aptlines.AddRange(maximumLines);
            }
            ParkingLines = parkingLines;
            aptlines = aptlines.Select(n => new LineCurve(n.PointAtStart, n.PointAtEnd) as Curve).ToList();
            return aptlines;
        }
        public Curve[] InnerRegion(Curve outside, Curve baseCurve, double regionWidth)
        {
            //check upper, lower bound
            int underzero = 5;

            Curve up = baseCurve.DuplicateCurve();
            Curve down = baseCurve.DuplicateCurve();

            Vector3d vu = up.TangentAtStart * regionWidth / 2;
            vu.Rotate(Math.PI / 2, Vector3d.ZAxis);
            Vector3d vd = -vu;

            up.Translate(vu);
            down.Translate(vd);

            var iu = Rhino.Geometry.Intersect.Intersection.CurveCurve(up, outside, 0, 0);
            var id = Rhino.Geometry.Intersect.Intersection.CurveCurve(down, outside, 0, 0);

            List<double> parameters = new List<double>();
            parameters.AddRange(iu.Select(n => Math.Round(n.ParameterA, underzero)));
            parameters.AddRange(id.Select(n => Math.Round(n.ParameterA, underzero)));

            var su = up.Split(parameters);
            var sd = down.Split(parameters);

            bool[] inu = su.Select(n => outside.Contains(n.PointAtNormalizedLength(0.5)) == PointContainment.Inside).ToArray();
            bool[] ind = sd.Select(n => outside.Contains(n.PointAtNormalizedLength(0.5)) == PointContainment.Inside).ToArray();

            if (inu.Length != ind.Length)
            //why?
            {
                return new Curve[0];
            }

            var sb = baseCurve.Split(parameters);
            List<Curve> result = new List<Curve>();
            List<Curve> boxes = new List<Curve>();
            for (int i = 0; i < inu.Length; i++)
            {
                if (inu[i] && ind[i])
                {
                    //1번,3번 segments 가 사이드선
                    boxes.Add(new Polyline(
                      new Point3d[] { su[i].PointAtStart, su[i].PointAtEnd, sd[i].PointAtEnd, sd[i].PointAtStart, su[i].PointAtStart }
                      ).ToNurbsCurve());

                    result.Add(sb[i]);
                }
            }


            // check side
            Vector3d testv = -baseCurve.TangentAtStart;
            //makebox
            for (int i = 0; i < boxes.Count; i++)
            {
                //외곽 기준선의 vertex들
                var outps = outside.DuplicateSegments().Select(n => n.PointAtStart).ToList();

                List<double> lefts = new List<double>();
                List<double> rights = new List<double>();

                for (int j = 0; j < outps.Count; j++)
                {
                    //박스 i 가 점 j 를 포함하면?
                    if (boxes[i].Contains(outps[j]) == PointContainment.Inside)
                    {
                        var boxsegments = boxes[i].DuplicateSegments();
                        Point3d testleft = outps[j] - testv;

                        double param;
                        result[i].ClosestPoint(outps[j], out param);
                        //외곽 기준선이 testpoint 를 포함하면?
                        if (outside.Contains(testleft) == PointContainment.Inside)
                        {
                            //왼쪽 선 수정
                            lefts.Add(param);
                        }
                        else
                        {
                            //오른쪽 선 수정
                            rights.Add(param);
                        }

                    }
                }

                //lefts 중 max, rights 중 min 으로 선 조정....
                var newstart = lefts.Count > 0 ? result[i].PointAt(lefts.Max()) : result[i].PointAtStart;
                var newend = rights.Count > 0 ? result[i].PointAt(rights.Min()) : result[i].PointAtEnd;
                result[i] = new LineCurve(newstart, newend);

            }

            return result.ToArray();

        }
        public double GetAvailableLength(IEnumerable<Curve> curves)
        {
            double minlength = 8000;
            var list = curves.ToList();
            double result = 0;
            for (int i = 0; i < list.Count; i++)
            {
                var d = list[i].GetLength() >= minlength ? list[i].GetLength() : 0;
                result += d;
            }

            return result;
        }
        //


        ///////////////////////////////
        //////////  outputs  //////////


        private List<List<double>> AptPacking(List<Unit> unitlist, List<double> aptLine, double aptWidth, double corearea, double floors, double plotArea, out List<List<UnitType>> isclearance)
        {
            #region 비율 조정

            List<double> unit = unitlist.Select(n => n.Area).ToList();
            List<double> unitRate = unitlist.Select(n => n.Rate).ToList();

            double[] eachUnitLength = unit.Select(n => n / aptWidth).ToArray();
            double[] eachUnitLengthbyRate = new double[eachUnitLength.Length];
            Array.Copy(eachUnitLength, eachUnitLengthbyRate, eachUnitLength.Length);
            for (int i = 0; i < eachUnitLength.Length; i++)
                eachUnitLengthbyRate[i] *= unitRate[i];

            double unitLength = eachUnitLengthbyRate.Sum();
            double expectedClearance = 0;
            double wholeLength = aptLine.Sum() - expectedClearance;
            double rawCount = Math.Round(wholeLength / unitLength, 2);
            double[] eachUnitrawCount = new double[unit.Count];
            for (int i = 0; i < eachUnitrawCount.Length; i++)
                eachUnitrawCount[i] = rawCount * unitRate[i];

            double[] balanced = new double[eachUnitrawCount.Length];
            for (int i = 0; i < balanced.Length; i++)
            {
                //복도형
                if (unit[i] <= Consts.AreaLimit)
                    balanced[i] = Math.Round(eachUnitrawCount[i]);

                //그외
                else
                {
                    if (eachUnitrawCount[i] % 2 >= 1.5)
                        balanced[i] = Math.Ceiling(eachUnitrawCount[i]);
                    else
                        balanced[i] = eachUnitrawCount[i] - eachUnitrawCount[i] % 2;
                }
            }

            double[] balancedRate = balanced.ToList().Select(n => Math.Round(n / balanced.Sum() * 100, 2)).ToArray();
            #endregion

            //remap rate
            for (int i = 0; i < unitlist.Count; i++)
            {
                unitlist[i].Rate = balancedRate[i] / 100;
            }


            List<double> aptLineLengths = aptLine;

            UnitDistributor distributor = new UnitDistributor(aptLineLengths, unitlist);
            //distributor.DistributeByRate(); not in use
            distributor.DistributeUnit();
            List<List<UnitType>> types = new List<List<UnitType>>();
            List<List<double>> positions = new List<List<double>>();

            // 확장
            foreach (ApartLine line in distributor.aptLines)
            {
                line.ExpandUnits();
            }

            // 용적률 계산.. 하려면 대지면적 알아야.......
            // 잠깐 코어가 몇개 들어가는지 어떻게 계산하지? 1층 코어를 더해줘야하는디..

            double expectedFA = 0;
            double expectedCV = 0;
            foreach (ApartLine line in distributor.aptLines)
            {
                int coreCount = line.CoreCount(); //맞을지모르겠음
                double coreOnly = corearea * coreCount;
                double eachFloor = line.FloorAreaSum();
                expectedFA += eachFloor * floors + coreOnly;

                double cv = line.SupplyAreaSum();
                expectedCV += cv;
            }


            //면적 초과분 (1층코어 + 각 층 면적)  -  기준 면적 (1층코어 + 각 층 면적) = 오차 면적 (각 층 면적 d) / 층수 = d
            //코어개수는 변하지 않으므로.

            double legalFA = ((TuringAndCorbusierPlugIn.InstanceClass.page1Settings.MaxFloorAreaRatio*0.999) / 100) * plotArea;

            //3종에서 용적률 오차 맞춰주기위함   252%정도로 나오니까 249로 강제 변환
            if (TuringAndCorbusierPlugIn.InstanceClass.page1Settings.MaxFloorAreaRatio == 250)
                legalFA *= (double)249 / 252;


            if (expectedFA >= legalFA)
            {
                //fordebug
                double expectedFAR = expectedFA / plotArea;
                double legalFAR = legalFA / plotArea;
                //area
                double lengthToReduce = expectedFA - legalFA;
                //eachfloor
                lengthToReduce /= floors;
                //length
                lengthToReduce /= aptWidth;
                //service
                lengthToReduce *= Consts.balconyRate_Tower;

                foreach(ApartLine line in distributor.aptLines)
                {
                    //line 에 lengthToReduce 넣고 뺌

                    lengthToReduce = line.ContractUnit(lengthToReduce);

                    if (lengthToReduce <= 0.1) //tolerance
                        break;
                }

                if (lengthToReduce >=0.1)
                    lengthToReduce = lengthToReduce;
                 
            }


            //건폐율 제한
            double legalCV = ((TuringAndCorbusierPlugIn.InstanceClass.page1Settings.MaxBuildingCoverage * 0.999) / 100) * plotArea;
            if (expectedCV >= legalCV)
            {
                //fordebug
                double expectedCVR = expectedCV / plotArea;
                double legalCVR = legalCV / plotArea;

                double lengthToReduce = expectedCV - legalCV;
                //length
                lengthToReduce /= aptWidth;
                foreach (ApartLine line in distributor.aptLines)
                {
                    //line 에 lengthToReduce 넣고 뺌

                    lengthToReduce = line.ContractUnit(lengthToReduce);

                    if (lengthToReduce <= 0.1) //tolerance
                        break;
                }

            }
            foreach (ApartLine line in distributor.aptLines)
            {
                //POSITION 먼저 구해야 정렬된 TYPE 값 얻음
                var thisLinePositions = line.Container.Positions();
                positions.Add(thisLinePositions);

                var thisLineClearances = line.Container.GetTypes();
                types.Add(thisLineClearances.ToList());
            }



            #region MyRegion



            //List<List<bool>> clearance = new List<List<bool>>();

            //List<int> countfromlastclearance = new List<int>();

            //countfromlastclearance = aptLine.Select(n => 0).ToList();

            //double[] eachUnitLength = unit.Select(n => n / aptWidth).ToArray();
            //double[] eachUnitLengthbyRate = new double[eachUnitLength.Length];
            //Array.Copy(eachUnitLength, eachUnitLengthbyRate, eachUnitLength.Length);
            //for (int i = 0; i < eachUnitLength.Length; i++)
            //    eachUnitLengthbyRate[i] *= unitRate[i];

            //double unitLength = eachUnitLengthbyRate.Sum();
            //double expectedClearance = 0;
            //double wholeLength = aptLine.Sum() - expectedClearance;
            //double rawCount = Math.Round(wholeLength / unitLength, 2);
            //double[] eachUnitrawCount = new double[unit.Count];
            //for (int i = 0; i < eachUnitrawCount.Length; i++)
            //    eachUnitrawCount[i] = rawCount * unitRate[i];

            //double[] balanced = new double[eachUnitrawCount.Length];
            //for (int i = 0; i < balanced.Length; i++)
            //{
            //    //복도형
            //    if (unit[i] <= Consts.minimumArea)
            //        balanced[i] = Math.Round(eachUnitrawCount[i]);

            //    //그외
            //    else
            //    {
            //        if (eachUnitrawCount[i] % 2 >= 1.5)
            //            balanced[i] = Math.Ceiling(eachUnitrawCount[i]);
            //        else
            //            balanced[i] = eachUnitrawCount[i] - eachUnitrawCount[i] % 2;
            //    }
            //}

            //double[] balancedRate = balanced.ToList().Select(n => Math.Round(n / balanced.Sum() * 100, 2)).ToArray();




            //int tryCount = 0;


            //double[] targetCount = new double[balanced.Length];
            //balanced.CopyTo(targetCount, 0);

            //double[] tempCount = new double[targetCount.Length];
            //for (int i = 0; i < tempCount.Length; i++)
            //    tempCount[i] = 0;

            //double[] aptLineFilled = new double[aptLine.Count];
            //for (int i = 0; i < aptLineFilled.Length; i++)
            //    aptLineFilled[i] = 0;

            //double[] aptLineLeft = new double[aptLine.Count];
            //aptLine.CopyTo(aptLineLeft, 0);

            //List<List<double>> items = new List<List<double>>();
            //for (int i = 0; i < aptLine.Count; i++)
            //{
            //    items.Add(new List<double> { });
            //    clearance.Add(new List<bool>());
            //}

            //double[] eachUnitRequiredLength = new double[balanced.Length];
            //for (int i = 0; i < balanced.Length; i++)
            //    eachUnitRequiredLength[i] = balanced[i] * eachUnitLength[i];

            //while (true)
            //{
            //    // min 2 lines max 4 lines after 4 lines add clearance
            //    bool done = false;
            //    for (int j = 0; j < aptLine.Count; j++)
            //    {
            //        for (int i = 0; i < targetCount.Length; i++)
            //        {
            //            //복도형.....!!
            //            ////이거고쳐야할듯. 하나씩 붙여나가고 50m마다 이격 추가, 완료 후 적절히 정렬. 
            //            if (unit[i] <= Consts.AreaLimit && targetCount[i] - tempCount[i] > 0)
            //            {
            //                //double MAXcount = 60000 / eachUnitLength[i];
            //                //double mindistance = double.MaxValue;
            //                //int mindistancek = (int)Math.Ceiling(MAXcount);
            //                //for (int k = (int)Math.Ceiling(MAXcount); k > 2; k--)
            //                //{
            //                //    double mod = targetCount[i] % k;
            //                //    double distance = k - Math.Round(mod);
            //                //    if (distance < mindistance)
            //                //    {
            //                //        mindistancek = k;
            //                //        mindistance = distance;
            //                //    }
            //                //}

            //                //전체 유닛 길이가..?
            //                double count = 1; // mindistancek > targetCount[i] - tempCount[i] ? targetCount[i] - tempCount[i] : mindistancek;
            //                double x = eachUnitLength[i] * count;


            //                //채워져있음, 목표길이에 이격거리 추가.
            //                if (aptLineFilled[j] != 0)
            //                {
            //                    // x += 5000.0;
            //                }

            //                //채울수있음
            //                if (aptLineLeft[j] >= x)
            //                {
            //                    //채워져있음
            //                    if (aptLineFilled[j] / 50000 > 0)
            //                    {
            //                        int clearanceCount = (int)Math.Round(aptLineFilled[j] / 50000);
            //                        if (clearance[j].Where(n => true).Count() < clearanceCount)
            //                        {
            //                            items[j].Add(5000.0);
            //                            clearance[j].Add(true);
            //                            countfromlastclearance[j] = 0;
            //                        }
            //                    }
            //                    aptLineLeft[j] -= x;
            //                    aptLineFilled[j] += x;
            //                    tempCount[i] += count;
            //                    countfromlastclearance[j] += (int)count;
            //                    for (int k = 0; k < count; k++)
            //                    {
            //                        items[j].Add(eachUnitLength[i]);
            //                        clearance[j].Add(false);
            //                    }
            //                    done = true;

            //                }
            //            }



            //            //기본 2개씩, 전용 65m2이상 -> 4세대 max , 나머지 ->8세대 max 

            //            //나머지 8개씩.
            //            else if (targetCount[i] - tempCount[i] >= 2)
            //            {
            //                int maxCount = 6;
            //                double areaCut = 65000000 / 0.91 + corearea / 2;
            //                if (unit[i] < areaCut)
            //                {
            //                    maxCount = 8;
            //                }

            //                //for (int j = 0; j < aptLine.Count; j++)
            //                //{
            //                if (countfromlastclearance[j] <= maxCount - 2)
            //                {
            //                    //2칸 붙야
            //                    List<double> units = new List<double> { eachUnitLength[i], eachUnitLength[i] };
            //                    List<bool> unitsbool = new List<bool> { false, false };
            //                    countfromlastclearance[j] += 2;
            //                    items[j].AddRange(units);
            //                    clearance[j].AddRange(unitsbool);
            //                    aptLineLeft[j] -= units.Sum();
            //                    aptLineFilled[j] += units.Sum();
            //                    tempCount[i] += 2;
            //                }
            //                else
            //                {
            //                    //이격 넣고 초기화
            //                    countfromlastclearance[j] = 0;
            //                    items[j].Add(5000);
            //                    clearance[j].Add(true);
            //                    aptLineLeft[j] -= 5000;
            //                    aptLineFilled[j] += 5000;
            //                }
            //                done = true;
            //                //}
            //            }
            //        }


            //    }

            //    if (done)
            //    {
            //        //tryCount++;
            //        continue;
            //    }
            //    if (!done)
            //        break;
            //}


            #endregion

            isclearance = types;
            return positions;

        }




    }
        
}