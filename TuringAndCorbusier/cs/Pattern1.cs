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
        public override Apartment generator(Plot plot, ParameterSet parameterSet, Target target)
        {
            ///////////////////////////////////////////////
            //////////  common initial settings  //////////
            ///////////////////////////////////////////////

            //입력"값" 부분
            double[] parameters = parameterSet.Parameters;
            double storiesHigh = Math.Max((int)parameters[0], (int)parameters[1]);
            double storiesLow = Math.Min((int)parameters[0], (int)parameters[1]);
            double width = parameters[2];
            double angleRadian = parameters[3];
            double moveFactor = parameters[4];
            Regulation regulationHigh = new Regulation(storiesHigh);
            Regulation regulationLow = new Regulation(storiesHigh, storiesLow);
            List<double> ratio = target.TargetRatio;
            List<double> area = target.TargetArea.Select(n => n / 0.91 * 1000 * 1000).ToList();
            double areaLimit = Consts.AreaLimit;
            BuildingType buildingType = regulationHigh.BuildingType;
            List<double> areaLength = new List<double>();

            double coreWidth = parameterSet.CoreType.GetWidth();
            double coreDepth = parameterSet.CoreType.GetDepth();

            double corearea = coreWidth * coreDepth;


            for (int i = 0; i < area.Count; i++)
            {
                if (area[i] < areaLimit)
                {
                    //서비스면적 10%
                    area[i] = area[i] * Consts.balconyRate_Corridor;
                    //코어&복도
                    area[i] += Consts.corridorWidth * width / 4;

                }
                else
                {

                    //서비스면적 18%
                    area[i] = area[i] * Consts.balconyRate_Stair;
                    //코어&복도
                    area[i] += parameters[i] + corearea / 2;

                }
            }

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

            ///////////////////////////
            //sort lists before start//
            RhinoList<double> ratioRL = new RhinoList<double>(ratio);
            ratioRL.Sort(area.ToArray());
            ratio = ratioRL.ToList();
            area.Sort();
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
            //setp 3 : cut apartments with regulation(high)
            //법규 : high, 건물 기준선 : high

            //일부 조건(대지안의공지, 일조에 의한 높이제한)을 만족하는 경계선
            //모든 조건(대지안의공지, 일조에의한 높이제한, 인접대지경계선)을 만족하는 경계선
            Curve[] partialRegulationHigh = CommonFunc.JoinRegulation(surroundingsHigh, northHigh);
            Curve[] wholeRegulationHigh = CommonFunc.JoinRegulations(northHigh, surroundingsHigh, lightingHigh);


            ////////////////////////////////////
            //////////  apt baseline  //////////
            ////////////////////////////////////

            //List<Line> baselines = baselineMaker(wholeRegulationHigh, parameterSet);
            List<Curve> aptLines = NewLineMaker(wholeRegulationHigh, parameterSet);



            #region new code
            ////////////////////////////////////
            //////////  zzzzzzzzzzz   //////////
            ////////////////////////////////////


            List<List<UnitType>> isclearance;
            List<List<double>> lengths = AptPacking(area, aptLines.Select(n => n.GetLength()).ToList(), ratio, width, corearea, out isclearance);
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

            List<List<List<Household>>> hhps = new List<List<List<Household>>>();

            List<List<Household>> Low = new List<List<Household>>();
            #region GetLow


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
                            Household corridor = hhg.Generate(UnitType.Corridor,buildingnum,houseindex);
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

                        corep.Origin = op;
                        corep.Stories = 0;
                        corep.XDirection = -tempBuildingCorridorUnits[0].XDirection;
                        corep.YDirection = tempBuildingCorridorUnits[0].YDirection;
                        corep.CoreType = parameterSet.CoreType;

                        corep.BuildingGroupNum = j;
                        cps.Add(corep);
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
                            corep.CoreType = parameterSet.CoreType;

                            corep.BuildingGroupNum = j;
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
            for (int i = 0; i < storiesHigh; i++)
            {

                double tempStoryHeight = Consts.PilotiHeight + i * Consts.FloorHeight;
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
                double tempStoryHeight = (i == 0) ? 0 : Consts.PilotiHeight + Consts.FloorHeight * (i - 1);
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
            #endregion

            Curve centerCurve = new LineCurve(Point3d.Origin, Point3d.Origin);
            if (aptLines.Count() != 0)
                centerCurve = aptLines[0];

            Plot forParking = new Plot(plot);
            var segs = forParking.Boundary.DuplicateSegments();

            List<Curve> regions = new List<Curve>();
            for (int i = 0; i < segs.Length; i++)
            {
                if (forParking.Surroundings[i] == 0)
                {
                    var p1 = segs[i].PointAtStart;
                    var p2 = segs[i].PointAtEnd;
                    var v = segs[i].TangentAtStart;
                    v.Rotate(-Math.PI / 2, Vector3d.ZAxis);
                    segs[i].Translate(v * 5000);
                    var region = new Polyline(new Point3d[] { segs[i].PointAtStart, segs[i].PointAtEnd, p2, p1, segs[i].PointAtStart });
                    regions.Add(region.ToNurbsCurve());
                }
            }

            if (regions.Count != 0)
            {
                Curve original = forParking.Boundary.DuplicateCurve();
                var diff = Curve.CreateBooleanDifference(original, regions);

                if (diff.Length == 0)
                { }
                else
                {
                    forParking.Boundary = diff[0];
                    forParking.SimplifiedBoundary = diff[0];
                }
                //Rhino.RhinoDoc.ActiveDoc.Objects.Add(forParking.Boundary);
            }
            ParkingLotOnEarth parkingLotOnEarth = new ParkingLotOnEarth(ParkingLineMaker.parkingLineMaker(this.GetAGType, cpss, forParking, parameters[2], centerCurve)); //parkingLotOnEarthMaker(boundary, household, parameterSet.CoreType.GetWidth(), parameterSet.CoreType.GetDepth(), coreOutlines);
            ParkingLotUnderGround parkingLotUnderGroud = new ParkingLotUnderGround();

            for (int i = parkingLotOnEarth.ParkingLines.Count - 1; i >= 0; i--)
            {
                for (int j = parkingLotOnEarth.ParkingLines[i].Count - 1; j >= 0; j--)
                {
                    var testpoint = new Point3d(parkingLotOnEarth.ParkingLines[i][j].Boundary.Center.X, parkingLotOnEarth.ParkingLines[i][j].Boundary.Center.Y, 0);

                    if (forParking.Boundary.Contains(testpoint) == PointContainment.Outside)
                    {
                        parkingLotOnEarth.ParkingLines[i].RemoveAt(j);
                    }
                }
            }


            //하아..
            var result = new Apartment(GetAGType, plot, buildingType, parameterSet, target, cpss, hhps, parkingLotOnEarth, parkingLotUnderGroud, new List<List<Curve>>(), aptLines);
            result.BuildingGroupCount = buildingnum;
            return result;
            #endregion

            #region hide 
            //////////////////////////////////////////////////
            ////////////  apt distribution & cores  //////////
            //////////////////////////////////////////////////

            //List<int> cullLine = new List<int>();
            //List<List<int>> areaTypeNumBuilding = areaTypeDistributor(area, areaLength, ratio, aptLines, width, areaLimit, out cullLine);
            //aptLines = aptLineCuller(aptLines, cullLine);

            //List<double> stretchFactor = stretchFactorMaker(areaTypeNumBuilding, aptLines, areaLength);

            /////////////////////////////////
            ////////////  shorten  //////////
            /////////////////////////////////


            ////가운데 숫자가 stretch max, 1이면 안늘어나지않을까
            //shortener(aptLines, stretchFactor, 1.0, out aptLines, out stretchFactor);

            //double fromEndParam = 0.25;
            //double coverageResize = coverageRatioChecker(parameterSet, plot, aptLines, area, areaTypeNumBuilding, areaLength, areaLimit, stretchFactor, partialRegulationHigh, fromEndParam);

            //List<Curve> aptLinesTemp1 = new List<Curve>();
            //for (int i = 0; i < aptLines.Count; i++)
            //{
            //    aptLinesTemp1.Add(new Line(aptLines[i].PointAtLength(aptLines[i].GetLength() * coverageResize / 2), aptLines[i].PointAtLength(aptLines[i].GetLength() * (1 - coverageResize / 2))).ToNurbsCurve());
            //}

            //aptLines = aptLinesTemp1;
            //stretchFactor = stretchFactor.Select(n => n * (1 - coverageResize)).ToList();

            //List<List<int>> householdOrderBuilding = householdOrderBuildingMaker(areaTypeNumBuilding, aptLines, area, width);
            //List<List<double>> houseEndParams = houseEndParamsMaker(areaLength, householdOrderBuilding);

            //////////////////////////////////////////////////
            ////////////  cull and split buildings  //////////
            //////////////////////////////////////////////////

            //int limitIndex;
            //List<int> mixedBuildingIndex = findIndexOfMixedBuilding(area, areaTypeNumBuilding, areaLimit, out limitIndex);

            //List<int> buildingAccessType = new List<int>();
            //List<Curve> aptLinesTemp = new List<Curve>();
            //List<List<int>> areaTypeNumBuildingTemp = new List<List<int>>();
            //List<List<int>> householdOrderBuildingTemp = new List<List<int>>();
            //List<List<double>> houseEndParamsTemp = new List<List<double>>();
            //List<double> stretchFactorTemp = new List<double>();

            //for (int i = 0; i < aptLines.Count; i++)
            //{
            //    if (mixedBuildingIndex.Contains(i))
            //    {
            //        int splitIndex = 0;
            //        while (householdOrderBuilding[i][splitIndex] < limitIndex)
            //        {
            //            splitIndex += 1;
            //        }
            //        //
            //        List<Curve> splitCurve = aptLines[i].Split(houseEndParams[i][splitIndex]).ToList();
            //        aptLinesTemp.AddRange(splitCurve);
            //        //
            //        List<int> areaTypeUnder = new List<int>();
            //        List<int> areaTypeOver = new List<int>();
            //        for (int j = 0; j < area.Count; j++)
            //        {
            //            if (j < limitIndex)
            //            {
            //                areaTypeUnder.Add(areaTypeNumBuilding[i][j]);
            //                areaTypeOver.Add(0);
            //            }
            //            else
            //            {
            //                areaTypeUnder.Add(0);
            //                areaTypeOver.Add(areaTypeNumBuilding[i][j]);
            //            }
            //        }
            //        areaTypeNumBuildingTemp.Add(areaTypeUnder);
            //        areaTypeNumBuildingTemp.Add(areaTypeOver);
            //        //
            //        householdOrderBuildingTemp.Add(householdOrderBuilding[i].GetRange(0, splitIndex));
            //        householdOrderBuildingTemp.Add(householdOrderBuilding[i].GetRange(splitIndex, householdOrderBuilding[i].Count - splitIndex));
            //        //
            //        houseEndParamsTemp.Add(houseEndParams[i].GetRange(0, splitIndex + 1).Select(n => n / houseEndParams[i][splitIndex]).ToList());
            //        houseEndParamsTemp.Add(houseEndParams[i].GetRange(splitIndex, houseEndParams[i].Count - splitIndex).Select(n => (n - houseEndParams[i][splitIndex]) / (houseEndParams[i][houseEndParams[i].Count - 1] - houseEndParams[i][splitIndex])).ToList());
            //        //
            //        stretchFactorTemp.Add(stretchFactor[i]);
            //        stretchFactorTemp.Add(stretchFactor[i]);
            //    }
            //    else
            //    {
            //        aptLinesTemp.Add(aptLines[i]);
            //        areaTypeNumBuildingTemp.Add(areaTypeNumBuilding[i]);
            //        householdOrderBuildingTemp.Add(householdOrderBuilding[i]);
            //        houseEndParamsTemp.Add(houseEndParams[i]);
            //        stretchFactorTemp.Add(stretchFactor[i]);
            //    }
            //}

            //List<Curve> aptLinesTempTemp = new List<Curve>();
            //for (int i = 0; i < aptLinesTemp.Count; i++)
            //{
            //    Polyline polylineTemp;
            //    aptLinesTemp[i].TryGetPolyline(out polylineTemp);
            //    aptLinesTempTemp.Add(polylineTemp.ToNurbsCurve().DuplicateCurve());
            //}

            //for (int i = 0; i < aptLinesTemp.Count; i++)
            //{
            //    if (householdOrderBuildingTemp[i][0] < limitIndex)
            //    {
            //        buildingAccessType.Add(0);
            //    }
            //    else
            //    {
            //        buildingAccessType.Add(1);
            //    }
            //}

            //aptLines = aptLinesTempTemp;
            //areaTypeNumBuilding = areaTypeNumBuildingTemp;
            //householdOrderBuilding = householdOrderBuildingTemp;
            //houseEndParams = houseEndParamsTemp;
            //stretchFactor = stretchFactorTemp;

            ////core protrusion factor
            //List<double> coreProtrusionFactor = coreProtrusionFactorFinder(aptLines, houseEndParams, buildingAccessType, 0.25, parameterSet, width, partialRegulationHigh);
            //List<int> cullAll = new List<int>();
            //for (int i = 0; i < aptLines.Count; i++)
            //{
            //    double testFactor = getMaxDepth(parameterSet.CoreType) / parameterSet.CoreType.GetDepth();
            //    if (buildingAccessType[i] == 0 && (coreProtrusionFactor[i] != testFactor || householdOrderBuilding[i].Count == 1))
            //    {
            //        cullAll.Add(i);
            //    }
            //}
            //for (int i = cullAll.Count - 1; i >= 0; i--)
            //{
            //    buildingAccessType.RemoveAt(cullAll[i]);
            //    aptLines.RemoveAt(cullAll[i]);
            //    areaTypeNumBuilding.RemoveAt(cullAll[i]);
            //    householdOrderBuilding.RemoveAt(cullAll[i]);
            //    houseEndParams.RemoveAt(cullAll[i]);
            //    stretchFactor.RemoveAt(cullAll[i]);
            //    coreProtrusionFactor.RemoveAt(cullAll[i]);
            //}

            /////////////////////////////////
            ////////////  outputs  //////////
            /////////////////////////////////

            ////core properties
            //List<List<Core>> core = coreMaker(parameterSet, buildingAccessType, aptLines, houseEndParams, coreProtrusionFactor, fromEndParam);

            ////core outlines
            //List<List<Rectangle3d>> coreOutlines = new List<List<Rectangle3d>>();
            //for (int i = 0; i < core.Count; i++)
            //{
            //    List<Rectangle3d> coreOutlinesTemp = new List<Rectangle3d>();
            //    for (int j = 0; j < core[i].Count; j++)
            //    {
            //        Rectangle3d coreRect = new Rectangle3d(new Plane(core[i][j].Origin, core[i][j].XDirection, core[i][j].YDirection), core[i][j].CoreType.GetWidth(), core[i][j].CoreType.GetDepth());

            //        coreOutlinesTemp.Add(coreRect);
            //    }
            //    coreOutlines.Add(coreOutlinesTemp);
            //}

            ////household properties
            //List<List<List<Household>>> household = householdMaker(parameterSet, buildingAccessType, aptLines, houseEndParams, coreProtrusionFactor, householdOrderBuilding, areaLength, stretchFactor, wholeRegulationHigh, Consts.minimumArea);


            ////building outlines
            //List<List<Curve>> buildingOutlines = buildingOutlineMaker(aptLines, buildingAccessType, width, parameterSet.CoreType.GetDepth());

            ////parking lot
            ////List<ParkingLot> parkingLot = parkingLotMaker(boundary, household, parameterSet.CoreType.GetWidth(), parameterSet.CoreType.GetDepth(), coreOutlines);

            //Curve centerCurve = new LineCurve(Point3d.Origin, Point3d.Origin);
            //if (aptLines.Count() != 0)
            //    centerCurve = aptLines[0];

            //ParkingLotOnEarth parkingLotOnEarth = new ParkingLotOnEarth(ParkingLineMaker.parkingLineMaker(this.GetAGType, core, plot, parameters[2], centerCurve)); //parkingLotOnEarthMaker(boundary, household, parameterSet.CoreType.GetWidth(), parameterSet.CoreType.GetDepth(), coreOutlines);
            //ParkingLotUnderGround parkingLotUnderGroud = new ParkingLotUnderGround();

            //Apartment result = new Apartment(this.GetAGType, plot, buildingType, parameterSet, target, core, household, parkingLotOnEarth, parkingLotUnderGroud, buildingOutlines, aptLines);
            ////commercialArea
            ////CommonFunc.createCommercialFacility(household, aptLines, plot, TuringAndCorbusierPlugIn.InstanceClass.page1Settings.MaxBuildingCoverage - result.GetBuildingCoverage(), TuringAndCorbusierPlugIn.InstanceClass.page1Settings.MaxFloorAreaRatio - result.GetGrossAreaRatio());
            #endregion
            //return result;
        }

        ///////////////////////////////////////////
        //////////  GA initial settings  //////////
        ///////////////////////////////////////////

        //Parameter 최소값 ~ 최대값 {storiesHigh, storiesLow, width, angle, moveFactor}
        private double[] minInput = { 3, 3, 5000, 0, 0 };
        //private double[] minInput = { 6, 6, 10500, 0, 0 };
        private double[] maxInput = { 7, 7, 10500, 2 * Math.PI, 1 };

        //Parameter GA최적화 {mutation probability, elite percentage, initial boost, population, generation, fitness value, mutation factor(0에 가까울수록 변동 범위가 넓어짐)
        private double[] GAparameterset = { 0.2, 0.03, 4, 50, 7, 3, 1 }; //원본
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

        ///////////////////////////////////////////////
        //////////  common initial settings  //////////
        ///////////////////////////////////////////////

        private Curve[] plotArrMaker(Curve boundary)
        {
            //Polyline plotPolyline;
            //boundary.TryGetPolyline(out plotPolyline);

            //Line[] plotSegmentsArr = plotPolyline.GetSegments();
            //Curve[] plotArr = new Curve[plotPolyline.GetSegments().Length];

            //for (int i = 0; i < plotSegmentsArr.Length; i++)
            //{
            //    plotArr[i] = plotSegmentsArr[i].ToNurbsCurve();
            //}
            return boundary.DuplicateSegments();
        }

        ///////////////////////////////////////////
        //////////  additional settings  //////////
        ///////////////////////////////////////////
        //y = aptcrv

        //////////////////////////////////
        //////////  apt baseline  //////////
        ////////////////////////////////////

        private List<Curve> NewLineMaker(Curve[] regCurve, ParameterSet parameterSet)
        {

            Curve[] RegCurve = regCurve.Select(n => n.DuplicateCurve()).ToArray();
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

                //param.Select(n => n + lengthremain / 2 - z / 2).ToList();
                param[0] += z;
                for (int i = 0; i < param.Count; i++)
                {
                    param[i] += lengthremain / 2 - z / 2;
                    Line templine = new Line(new Point3d(boundingbox.Min.X, boundingbox.Min.Y + param[i], 0), new Point3d(boundingbox.Max.X, boundingbox.Min.Y + param[i], 0));
                    result.Add(templine);
                }

                for (int i = 0; i < result.Count; i++)
                {
                    Line tempr = result[i];

                    tempr.Transform(Transform.Rotation(-angleRadian, Vector3d.ZAxis, rotatecenter));

                    result[i] = tempr;
                }

                List<Curve> up = new List<Curve>();
                List<Curve> down = new List<Curve>();

                up = result.Select(n => n.ToNurbsCurve().DuplicateCurve()).ToList();
                down = result.Select(n => n.ToNurbsCurve().DuplicateCurve()).ToList();

                Vector3d v = result[0].UnitTangent;
                v.Rotate(Math.PI / 2, Vector3d.ZAxis);
                v.Unitize();



                for (int i = 0; i < up.Count; i++)
                {
                    up[i].Transform(Transform.Translation(v * z / 2));
                    down[i].Transform(Transform.Translation(-v * z / 2));



                    var i1 = Rhino.Geometry.Intersect.Intersection.CurveCurve(up[i], regulationCurve, 0, 0);
                    var i2 = Rhino.Geometry.Intersect.Intersection.CurveCurve(down[i], regulationCurve, 0, 0);

                    var splitparams = i1.Select(n => n.ParameterA).ToList();
                    var splitparams2 = i2.Select(n => n.ParameterA).ToList();

                    splitparams.AddRange(splitparams2);

                    var splted1 = up[i].Split(splitparams);
                    var splted2 = down[i].Split(splitparams);
                    var spltedmain = result[i].ToNurbsCurve().Split(splitparams);

                    List<Curve> survivor = new List<Curve>();

                    for (int j = 0; j < splted1.Length; j++)
                    {
                        if (regulationCurve.Contains(splted1[j].PointAtNormalizedLength(0.5)) == PointContainment.Inside
                            && regulationCurve.Contains(splted2[j].PointAtNormalizedLength(0.5)) == PointContainment.Inside)
                        {
                            survivor.Add(spltedmain[j]);
                        }
                    }

                    aptlines.AddRange(Curve.JoinCurves(survivor));




                    //if (i1.Count <= 1 && i2.Count <= 1)
                    //    continue;

                    //else if (i1.Count > 0 && i2.Count == 0)
                    //{

                    //}

                    //else if (i1.Count == 0 && i2.Count > 0)
                    //{

                    //}

                    //else
                    //{
                    //    var i1a = i1.Max(n => n.ParameterA);
                    //    var i1b = i1.Min(n => n.ParameterA);

                    //    var i2a = i2.Max(n => n.ParameterA);
                    //    var i2b = i2.Min(n => n.ParameterA);

                    //    var minA = i1a > i2a ? i2a : i1a;
                    //    var minB = i1b < i2b ? i2b : i1b;

                    //    var p1 = up[i].PointAt(minA);
                    //    var p2 = up[i].PointAt(minB);
                    //    var p3 = down[i].PointAt(minB);
                    //    var p4 = down[i].PointAt(minA);


                    //    var temp = new Polyline(new Point3d[] { p1, p2, p3, p4, p1 }).ToNurbsCurve();

                    //    var tempresult = result[i].ToNurbsCurve();

                    //    var lastintersection = Rhino.Geometry.Intersect.Intersection.CurveCurve(tempresult, temp, 0, 0);

                    //    var interval = lastintersection.Select(n => n.ParameterA).ToList();

                    //    var aptline = tempresult.Trim(interval[0], interval[1]);
                    //    aptlines.Add(aptline);
                    //}
                }

            }

            aptlines = aptlines.Select(n => new LineCurve(n.PointAtStart, n.PointAtEnd) as Curve).ToList();
            return aptlines;
        }

        private List<Line> baselineMaker(Curve regCurve, ParameterSet parameterSet)
        {
            Curve regulationCurve = regCurve.DuplicateCurve();
            double[] parameters = parameterSet.Parameters;
            double storiesHigh = Math.Max((int)parameters[0], (int)parameters[1]);
            double storiesLow = Math.Min((int)parameters[0], (int)parameters[1]);
            double width = parameters[2];
            double angleRadian = parameters[3];
            double moveFactor = parameters[4];
            Regulation regulationHigh = new Regulation(storiesHigh);

            Polyline regulationPolyline;
            regulationCurve.TryGetPolyline(out regulationPolyline);
            List<Point3d> check = new List<Point3d>(regulationPolyline);

            //create raw baselines
            double alpha = 10;
            regulationCurve.Rotate(angleRadian, Vector3d.ZAxis, Point3d.Origin);
            var boundingbox = regulationCurve.GetBoundingBox(false);
            Point3d minP = boundingbox.Min;
            Point3d maxP = boundingbox.Max;
            //double yPos = minP.Y + moveFactor * (width + regulationHigh.DistanceLL + Consts.corridorWidth);//corridor!!!!!!
            double yPos = minP.Y + moveFactor * (width + regulationHigh.DistanceLL);
            List<Line> rawLines = new List<Line>();
            while (yPos < maxP.Y)
            {
                rawLines.Add(new Line(new Point3d(minP.X - alpha, yPos, 0), new Point3d(maxP.X + alpha, yPos, 0)));
                //yPos += width + regulationHigh.DistanceLL + Consts.corridorWidth;//corridor!!!!!!
                yPos += width + regulationHigh.DistanceLL;
            }
            List<Line> rawLinesTemp = new List<Line>();

            for (int i = 0; i < rawLines.Count; i++)
            {
                Line lineTemp = rawLines[i];
                lineTemp.Transform(Transform.Rotation(-angleRadian, Vector3d.ZAxis, Point3d.Origin));
                rawLinesTemp.Add(lineTemp);
            }
            rawLines = rawLinesTemp;

            return rawLines;
        }

        private List<Curve> aptLineMaker(List<Line> baselines, Curve wholeRegulation, double width, List<double> areaLength)
        {
            List<Curve> baselineRaw = baselines.Select(n => n.ToNurbsCurve().DuplicateCurve()).ToList();
            List<List<Curve>> splitCurveSets = splitCurveSetMaker(baselineRaw, wholeRegulation, width);
            List<Curve> aptLines = new List<Curve>();
            for (int i = 0; i < baselineRaw.Count; i++)
            {
                List<Curve> aptLinesTemp = new List<Curve>();
                if (splitCurveSets[i].Count != 0)
                {
                    List<Interval> intervals = intervalMaker(baselineRaw[i], splitCurveSets[i]);
                    for (int j = 0; j < intervals.Count; j++)
                    {
                        if (wholeRegulation.Contains(baselineRaw[i].PointAt(intervals[j].Min)) != PointContainment.Outside)
                        {
                            //cutting cap
                            Curve before = new Line(baselineRaw[i].PointAt(intervals[j].Min), baselineRaw[i].PointAt(intervals[j].Max)).ToNurbsCurve();
                            //double capL = Consts.exWallThickness - Consts.inWallThickness / 2;
                            double capL = 1000;
                            if (before.GetLength() > 2 * capL)
                            {
                                Curve after = new Line(before.PointAtLength(capL), before.PointAtLength(before.GetLength() - capL)).ToNurbsCurve();
                                aptLines.Add(after);
                            }
                        }
                    }
                }
            }

            //cull short
            double shortestLength = 2 * areaLength[0];
            List<Curve> culledLines = new List<Curve>();
            for (int i = 0; i < aptLines.Count; i++)
            {
                if (aptLines[i].GetLength() > shortestLength)
                {
                    culledLines.Add(aptLines[i]);
                }
            }
            return culledLines;
        }
        private List<List<Curve>> splitCurveSetMaker(List<Curve> baselineRaw, Curve wholeRegulation, double width)
        {
            List<List<Curve>> splitCurveSets = new List<List<Curve>>();
            for (int i = 0; i < baselineRaw.Count; i++)
            {
                Point3d pt1 = baselineRaw[i].PointAtStart;
                Point3d pt2 = baselineRaw[i].PointAtEnd;
                Point3d pt3 = baselineRaw[i].PointAtEnd;
                Point3d pt4 = baselineRaw[i].PointAtStart;
                Vector3d vec = baselineRaw[i].TangentAtStart;
                vec.Rotate(Math.PI / 2, Vector3d.ZAxis);
                //pt1.Transform(Transform.Translation(Vector3d.Multiply(vec, width / 2 + Consts.corridorWidth)));//corridor!!!!!!
                //pt2.Transform(Transform.Translation(Vector3d.Multiply(vec, width / 2 + Consts.corridorWidth)));//corridor!!!!!!
                pt1.Transform(Transform.Translation(Vector3d.Multiply(vec, width / 2)));
                pt2.Transform(Transform.Translation(Vector3d.Multiply(vec, width / 2)));
                pt3.Transform(Transform.Translation(Vector3d.Multiply(vec, -width / 2)));
                pt4.Transform(Transform.Translation(Vector3d.Multiply(vec, -width / 2)));

                List<Point3d> ptsTemp = new List<Point3d>();
                ptsTemp.Add(pt1);
                ptsTemp.Add(pt2);
                ptsTemp.Add(pt3);
                ptsTemp.Add(pt4);
                ptsTemp.Add(pt1);
                Polyline polylineTemp = new Polyline(ptsTemp);

                List<Curve> splitCurve = splitCurveMaker(wholeRegulation, polylineTemp.ToNurbsCurve());
                splitCurveSets.Add(splitCurve);
            }
            return splitCurveSets;
        }
        private List<Curve> splitCurveMaker(Curve baseCurve, Curve splitter)
        {
            var intersectionEvents = Rhino.Geometry.Intersect.Intersection.CurveCurve(baseCurve, splitter, 0, 0);

            List<double> intersectDomain = new List<double>();

            foreach (Rhino.Geometry.Intersect.IntersectionEvent i in intersectionEvents)
            {
                intersectDomain.Add(i.ParameterA);
            }

            Curve[] splittedBase = baseCurve.Split(intersectDomain);

            List<Curve> output = new List<Curve>();

            foreach (Curve i in splittedBase)
            {
                if (splitter.Contains(i.PointAt(i.Domain.Mid)) == PointContainment.Inside)
                    output.Add(i);
            }

            return output;
        }
        private List<Interval> intervalMaker(Curve baselineRaw, List<Curve> splitCurveSets)
        {
            List<Interval> intervals = new List<Interval>();
            List<Interval> outputTemp = new List<Interval>();
            List<Interval> output = new List<Interval>();
            for (int i = 0; i < splitCurveSets.Count; i++)
            {
                Polyline polylineTemp;
                splitCurveSets[i].TryGetPolyline(out polylineTemp);
                List<Point3d> ptsTemp = new List<Point3d>(polylineTemp);
                List<double> paramTemp = new List<double>();
                for (int j = 0; j < ptsTemp.Count; j++)
                {
                    double doubleTemp;
                    baselineRaw.ClosestPoint(ptsTemp[j], out doubleTemp, baselineRaw.GetLength());
                    paramTemp.Add(doubleTemp);
                }

                intervals.Add(new Interval(paramTemp.Min(), paramTemp.Max()));
            }
            intervals.Sort();
            intervals.ToArray();
            for (int i = 0; i < intervals.Count - 1; i++)
            {
                if (Interval.FromIntersection(intervals[i], intervals[i + 1]).Length != 0)
                {
                    intervals[i + 1] = Interval.FromUnion(intervals[i], intervals[i + 1]);
                }
                else
                {
                    outputTemp.Add(intervals[i]);
                }
            }
            outputTemp.Add(intervals[intervals.Count - 1]);

            for (int i = 0; i < outputTemp.Count - 1; i++)
            {
                output.Add(new Interval(outputTemp[i].Max, outputTemp[i + 1].Min));
            }

            return output;
        }

        ////////////////////////////////////////////////
        //////////  apt distribution & cores  //////////
        ////////////////////////////////////////////////

        private List<List<int>> areaTypeDistributor(List<double> area, List<double> areaLength, List<double> ratio, List<Curve> aptLines, double width, double areaLimit, out List<int> cull)
        {
            //initial settings
            List<double> aptLinesLength = aptLines.Select(k => k.GetLength()).ToList();
            List<int> areaTypeNumTotal = new List<int>();

            //total number of area types
            double totalLength = aptLinesLength.Sum();


            //여기부터
            //double s = 0;
            ////totalLength = 모든 aptlines 길이 총합
            ////s = 유닛길이 * 유닛비율 총 합(((비율이 세대수 비율이 아니고 면적 비율이었나..!)))
            //for (int i = 0; i < ratio.Count; i++)
            //{
            //    s += areaLength[i] * ratio[i];
            //}

            ////n1 = 총길이 * 첫번째 유닛 비율 / (유닛길이 * 유닛비율 총 합)
            ////ss = 
            //double n1 = totalLength * ratio[0] / s;
            //List<double> stackNum = new List<double>();
            //double ss = 0;
            //stackNum.Add(0);
            //for (int i = 0; i < ratio.Count; i++)
            //{
            //    var check = n1 * ratio[i] / ratio[0];
            //    ss += n1 * ratio[i] / ratio[0];
            //    stackNum.Add(ss);
            //}

            //여기까지    면적비 -> 세대수 비 위해 변경

            //각 길이 * 비율 합
            double oneunitlength = 0;

            for (int i = 0; i < ratio.Count; i++)
            {
                oneunitlength += areaLength[i] * ratio[i];
            }

            double totalcount = totalLength / oneunitlength;
            List<double> stackNum = new List<double>();


            double stacked = 0;
            stackNum.Add(stacked);
            for (int i = 0; i < ratio.Count; i++)
            {
                double eachcount = totalcount * ratio[i];

                double balancedcount = 0;

                //복도형이 아니라면
                if (areaLimit > width * areaLength[i])
                {
                    //2로 나눈 나머지가 1.5이상,
                    if (eachcount % 2 >= 1.5)
                        balancedcount = Math.Ceiling(eachcount);
                    else
                        balancedcount = eachcount - eachcount % 2;
                }
                //복도형이면
                else
                {
                    balancedcount = Math.Round(eachcount);
                }
                stacked += balancedcount;
                stackNum.Add(stacked);
            }



            List<int> n = new List<int>();
            for (int i = 0; i < ratio.Count; i++)
            {
                n.Add((int)stackNum[i + 1] - (int)stackNum[i]);
            }
            areaTypeNumTotal = n;

            //delete some houses to get free space
            double token = aptLines.Count;
            while (token > 0)
            {
                areaTypeNumTotal[areaTypeNumTotal.IndexOf(areaTypeNumTotal.Max())] -= 1;
                token -= 1;
            }

            //number of area types on each building
            List<List<int>> areaTypeNumBuildingRaw = areaTypeNumBuildingMaker(areaTypeNumTotal, areaLength, aptLinesLength, out cull);
            List<List<int>> areaTypeNumBuilding = areaTypeNumBuildingModifier(area, areaTypeNumBuildingRaw, areaLimit);

            return areaTypeNumBuilding;
        }
        private List<List<int>> areaTypeNumBuildingMaker(List<int> areaTypeNumTotal, List<double> areaLength, List<double> aptLinesLength, out List<int> cull)
        {
            //initialize
            List<List<int>> areaTypeNumBuilding = new List<List<int>>();
            for (int i = 0; i < aptLinesLength.Count; i++)
            {
                List<int> blank = new List<int>();
                for (int j = 0; j < areaLength.Count; j++)
                {
                    blank.Add(0);
                }
                areaTypeNumBuilding.Add(blank);
            }

            //make houses into index list
            List<int> unsortedHouseIndices = new List<int>();
            for (int i = 0; i < areaTypeNumTotal.Count; i++)
            {
                int token = 0;
                while (token < areaTypeNumTotal[i])
                {
                    unsortedHouseIndices.Add(i);
                    token += 1;
                }
            }

            //area types on each building
            //distribute houses in unsorted
            int jj = 0;
            for (int i = 0; i < unsortedHouseIndices.Count; i++)
            {
                if (jj < aptLinesLength.Count)
                {
                    if (aptLinesLength[jj] > areaLength[unsortedHouseIndices[i]])
                    {
                        aptLinesLength[jj] -= areaLength[unsortedHouseIndices[i]];
                        areaTypeNumBuilding[jj][unsortedHouseIndices[i]] += 1;
                    }
                    else
                    {
                        jj += 1;
                        if (jj < aptLinesLength.Count)
                        {
                            aptLinesLength[jj] -= areaLength[unsortedHouseIndices[i]];
                            areaTypeNumBuilding[jj][unsortedHouseIndices[i]] += 1;
                        }
                    }
                }
            }

            //remove building with 0 household
            cull = new List<int>();
            List<List<int>> areaTypeNumBuildingTemp = new List<List<int>>();
            for (int i = 0; i < areaTypeNumBuilding.Count; i++)
            {
                int sum = areaTypeNumBuilding[i].Sum();
                if (sum != 0)
                {
                    areaTypeNumBuildingTemp.Add(areaTypeNumBuilding[i]);
                }
                else
                {
                    cull.Add(i);
                }
            }
            areaTypeNumBuilding = areaTypeNumBuildingTemp;

            return areaTypeNumBuilding;
        }
        private List<List<int>> areaTypeNumBuildingModifier(List<double> area, List<List<int>> areaTypeNumBuildingRaw, double areaLimit)
        {
            //find limit index in area
            int limitIndex = 0;
            while (area[limitIndex] <= areaLimit)
            {
                limitIndex += 1;
                if (limitIndex == area.Count)
                    break;
            }

            //find min and max index of area for each building
            //{min, max}

            List<List<int>> areaIndicesBoundary = new List<List<int>>();
            for (int i = 0; i < areaTypeNumBuildingRaw.Count; i++)
            {
                List<int> indicesTemp = new List<int>();
                for (int j = 0; j < area.Count; j++)
                {
                    if (areaTypeNumBuildingRaw[i][j] != 0)
                    {
                        indicesTemp.Add(j);
                    }
                }
                List<int> indicesBoundaryTemp = new List<int>();
                indicesBoundaryTemp.Add(indicesTemp.Min());
                indicesBoundaryTemp.Add(indicesTemp.Max());
                areaIndicesBoundary.Add(indicesBoundaryTemp);
            }

            //find number of houses under and over area limit
            List<int> underLimit = new List<int>();
            for (int i = 0; i < areaTypeNumBuildingRaw.Count; i++)
            {
                int sum = 0;
                for (int j = 0; j < limitIndex; j++)
                {
                    sum += areaTypeNumBuildingRaw[i][j];
                }
                underLimit.Add(sum);
            }
            List<int> overLimit = new List<int>();
            for (int i = 0; i < areaTypeNumBuildingRaw.Count; i++)
            {
                int sum = 0;
                for (int j = limitIndex; j < area.Count; j++)
                {
                    sum += areaTypeNumBuildingRaw[i][j];
                }
                overLimit.Add(sum);
            }

            //adjust
            //type 1 : corridor - max index is smaller than limit index
            //type 2 : mixed - min index < limit index <= max index
            //type 3 : core - limit index <= max index

            for (int i = 0; i < areaTypeNumBuildingRaw.Count; i++)
            {
                if (areaIndicesBoundary[i][1] < limitIndex)
                {
                    //do noting
                }
                else if (areaIndicesBoundary[i][0] < limitIndex && limitIndex <= areaIndicesBoundary[i][1])
                {
                    if (overLimit[i] % 2 == 1)
                    {
                        areaTypeNumBuildingRaw[i][areaIndicesBoundary[i][0]] += 1;
                        areaTypeNumBuildingRaw[i][areaIndicesBoundary[i][1]] -= 1;
                    }
                }
                else if (limitIndex <= areaIndicesBoundary[i][0])
                {
                    if (overLimit[i] % 2 == 1)
                    {
                        areaTypeNumBuildingRaw[i][areaIndicesBoundary[i][0]] += 2;
                        areaTypeNumBuildingRaw[i][areaIndicesBoundary[i][1]] -= 1;
                    }
                }
            }

            return areaTypeNumBuildingRaw;
        }
        private List<Curve> aptLineCuller(List<Curve> aptLines, List<int> cullLine)
        {
            List<Curve> aptLinesTemp = new List<Curve>();
            for (int i = 0; i < aptLines.Count; i++)
            {
                if (cullLine.Contains(i) == false)
                {
                    aptLinesTemp.Add(aptLines[i]);
                }
            }
            aptLines = aptLinesTemp;

            return aptLines;
        }

        private List<double> stretchFactorMaker(List<List<int>> areaTypeNumBuilding, List<Curve> aptLines, List<double> areaLength)
        {
            List<double> sourceLength = new List<double>();
            for (int i = 0; i < aptLines.Count; i++)
            {
                sourceLength.Add(0);
            }
            List<double> aptLineLength = aptLines.Select(n => n.GetLength()).ToList();
            List<double> stretchFactor = new List<double>();

            //stretch factor
            for (int i = 0; i < areaTypeNumBuilding.Count; i++)
            {
                List<int> unallocated = new List<int>();
                for (int j = 0; j < areaLength.Count; j++)
                {
                    sourceLength[i] += areaTypeNumBuilding[i][j] * areaLength[j];
                }
                stretchFactor.Add(aptLineLength[i] / sourceLength[i]);
            }
            return stretchFactor;
        }
        private List<List<int>> householdOrderBuildingMaker(List<List<int>> areaTypeNumBuilding, List<Curve> aptLines, List<double> area, double width)
        {
            List<List<int>> householdOrderBuilding = new List<List<int>>();

            //list of areas of houses on each building
            for (int i = 0; i < areaTypeNumBuilding.Count; i++)
            {
                List<int> householdOrder = new List<int>();
                for (int j = 0; j < area.Count; j++)
                {
                    for (int k = 0; k < areaTypeNumBuilding[i][j]; k++)
                    {
                        householdOrder.Add(j);
                    }
                }
                householdOrderBuilding.Add(householdOrder);
            }
            return householdOrderBuilding;
        }
        private List<List<double>> houseEndParamsMaker(List<double> areaLength, List<List<int>> householdOrderBuilding)
        {
            //stack parameters
            List<List<double>> stack = new List<List<double>>();
            for (int i = 0; i < householdOrderBuilding.Count; i++)
            {
                double sum = 0;
                List<double> stackTemp = new List<double>();
                stackTemp.Add(sum);
                for (int j = 0; j < householdOrderBuilding[i].Count; j++)
                {
                    sum += areaLength[householdOrderBuilding[i][j]];
                    stackTemp.Add(sum);
                }
                double totalL = stackTemp[householdOrderBuilding[i].Count];
                stackTemp = stackTemp.Select(n => n / totalL).ToList();
                stack.Add(stackTemp);
            }
            return stack;
        }

        ////////////////////////////////////////
        //////////  shorten aptlines  //////////
        ////////////////////////////////////////

        private void shortener(List<Curve> aptLines, List<double> stretchFactor, double factorLimit, out List<Curve> aptLinesNew, out List<double> stretchFactorNew)
        {
            List<Curve> aptLinesTemp = new List<Curve>();
            List<double> stretchFactorTemp = new List<double>();
            for (int i = 0; i < stretchFactor.Count; i++)
            {
                if (stretchFactor[i] > factorLimit)
                {
                    double resizeFactor = factorLimit / stretchFactor[i];
                    Curve curve = aptLines[i].DuplicateCurve();
                    double beforeLength = curve.GetLength();
                    double afterLength = beforeLength * resizeFactor;
                    double cutLength = (beforeLength - afterLength) / 2;
                    Curve curveNew = new Line(curve.PointAtLength(cutLength), curve.PointAtLength(beforeLength - cutLength)).ToNurbsCurve();
                    aptLinesTemp.Add(curveNew);
                    stretchFactorTemp.Add(stretchFactor[i] * resizeFactor);
                }
                else
                {
                    aptLinesTemp.Add(aptLines[i]);
                    stretchFactorTemp.Add(stretchFactor[i]);
                }
            }
            aptLinesNew = aptLinesTemp;
            stretchFactorNew = stretchFactorTemp;
        }

        private double coverageRatioChecker(ParameterSet parameterSet, Plot plot, List<Curve> aptLines_, List<double> area_, List<List<int>> areaTypeNumBuilding_, List<double> areaLength_, double areaLimit, List<double> stretchFactor_, Curve partialRegulationHigh, double fromEndParam)
        {
            //initial settings
            double width = parameterSet.Parameters[2];
            List<Curve> aptLines = new List<Curve>(aptLines_);
            List<double> area = new List<double>(area_);
            List<List<int>> areaTypeNumBuilding = new List<List<int>>(areaTypeNumBuilding_);
            List<double> areaLength = new List<double>(areaLength_);
            List<double> stretchFactor = new List<double>(stretchFactor_);

            List<List<int>> householdOrderBuilding = householdOrderBuildingMaker(areaTypeNumBuilding, aptLines, area, width);
            List<List<double>> houseEndParams = houseEndParamsMaker(areaLength, householdOrderBuilding);

            ////////////////////////////////////////////////
            //////////  cull and split buildings  //////////
            ////////////////////////////////////////////////

            int limitIndex;
            List<int> mixedBuildingIndex = findIndexOfMixedBuilding(area, areaTypeNumBuilding, areaLimit, out limitIndex);

            List<int> buildingAccessType = new List<int>();
            List<Curve> aptLinesTemp = new List<Curve>();
            List<List<int>> areaTypeNumBuildingTemp = new List<List<int>>();
            List<List<int>> householdOrderBuildingTemp = new List<List<int>>();
            List<List<double>> houseEndParamsTemp = new List<List<double>>();
            List<double> stretchFactorTemp = new List<double>();

            for (int i = 0; i < aptLines.Count; i++)
            {
                if (mixedBuildingIndex.Contains(i))
                {
                    int splitIndex = 0;
                    while (householdOrderBuilding[i][splitIndex] < limitIndex)
                    {
                        splitIndex += 1;
                    }
                    //
                    List<Curve> splitCurve = aptLines[i].Split(houseEndParams[i][splitIndex]).ToList();
                    aptLinesTemp.AddRange(splitCurve);
                    //
                    List<int> areaTypeUnder = new List<int>();
                    List<int> areaTypeOver = new List<int>();
                    for (int j = 0; j < area.Count; j++)
                    {
                        if (j < limitIndex)
                        {
                            areaTypeUnder.Add(areaTypeNumBuilding[i][j]);
                            areaTypeOver.Add(0);
                        }
                        else
                        {
                            areaTypeUnder.Add(0);
                            areaTypeOver.Add(areaTypeNumBuilding[i][j]);
                        }
                    }
                    areaTypeNumBuildingTemp.Add(areaTypeUnder);
                    areaTypeNumBuildingTemp.Add(areaTypeOver);
                    //
                    householdOrderBuildingTemp.Add(householdOrderBuilding[i].GetRange(0, splitIndex));
                    householdOrderBuildingTemp.Add(householdOrderBuilding[i].GetRange(splitIndex, householdOrderBuilding[i].Count - splitIndex));
                    //
                    houseEndParamsTemp.Add(houseEndParams[i].GetRange(0, splitIndex + 1).Select(n => n / houseEndParams[i][splitIndex]).ToList());
                    houseEndParamsTemp.Add(houseEndParams[i].GetRange(splitIndex, houseEndParams[i].Count - splitIndex).Select(n => (n - houseEndParams[i][splitIndex]) / (houseEndParams[i][houseEndParams[i].Count - 1] - houseEndParams[i][splitIndex])).ToList());
                    //
                    stretchFactorTemp.Add(stretchFactor[i]);
                    stretchFactorTemp.Add(stretchFactor[i]);
                }
                else
                {
                    aptLinesTemp.Add(aptLines[i]);
                    areaTypeNumBuildingTemp.Add(areaTypeNumBuilding[i]);
                    householdOrderBuildingTemp.Add(householdOrderBuilding[i]);
                    houseEndParamsTemp.Add(houseEndParams[i]);
                    stretchFactorTemp.Add(stretchFactor[i]);
                }
            }

            List<Curve> aptLinesTempTemp = new List<Curve>();
            for (int i = 0; i < aptLinesTemp.Count; i++)
            {
                Polyline polylineTemp;
                aptLinesTemp[i].TryGetPolyline(out polylineTemp);
                aptLinesTempTemp.Add(polylineTemp.ToNurbsCurve().DuplicateCurve());
            }

            for (int i = 0; i < aptLinesTemp.Count; i++)
            {
                if (householdOrderBuildingTemp[i][0] < limitIndex)
                {
                    buildingAccessType.Add(0);
                }
                else
                {
                    buildingAccessType.Add(1);
                }
            }

            aptLines = aptLinesTempTemp;
            areaTypeNumBuilding = areaTypeNumBuildingTemp;
            householdOrderBuilding = householdOrderBuildingTemp;
            houseEndParams = houseEndParamsTemp;
            stretchFactor = stretchFactorTemp;

            //core protrusion factor
            List<double> coreProtrusionFactor = coreProtrusionFactorFinder(aptLines, houseEndParams, buildingAccessType, fromEndParam, parameterSet, width, partialRegulationHigh);
            List<int> cullAll = new List<int>();
            for (int i = 0; i < aptLines.Count; i++)
            {
                double testFactor = getMaxDepth(parameterSet.CoreType) / parameterSet.CoreType.GetDepth();
                if (buildingAccessType[i] == 0 && (coreProtrusionFactor[i] != testFactor || householdOrderBuilding[i].Count == 1))
                {
                    cullAll.Add(i);
                }
            }
            for (int i = cullAll.Count - 1; i >= 0; i--)
            {
                buildingAccessType.RemoveAt(cullAll[i]);
                aptLines.RemoveAt(cullAll[i]);
                areaTypeNumBuilding.RemoveAt(cullAll[i]);
                householdOrderBuilding.RemoveAt(cullAll[i]);
                houseEndParams.RemoveAt(cullAll[i]);
                stretchFactor.RemoveAt(cullAll[i]);
                coreProtrusionFactor.RemoveAt(cullAll[i]);
            }

            //core properties
            List<List<Core>> core = coreMaker(parameterSet, buildingAccessType, aptLines, houseEndParams, coreProtrusionFactor, fromEndParam);


            //coverage calculator
            double coreCoverage = 0;
            double houseCoverage = 0;

            for (int i = 0; i < core.Count; i++)
            {
                for (int j = 0; j < core[i].Count; j++)
                {
                    coreCoverage += core[i][j].GetArea();
                    double test = core[i][j].GetArea();
                }

            }

            for (int i = 0; i < aptLines.Count; i++)
                houseCoverage += aptLines[i].GetLength() * width;
            for (int i = 0; i < core.Count; i++)
            {
                for (int j = 0; j < core[i].Count; j++)
                    houseCoverage -= core[i][j].GetArea() * (1 - coreProtrusionFactor[i]);
            }

            double totalCoverage = coreCoverage + houseCoverage;
            double totalArea = CommonFunc.getArea(plot.Boundary);
            double totalCoverageRatio = totalCoverage / totalArea;
            double maxCoverageRatio = TuringAndCorbusierPlugIn.InstanceClass.page1Settings.MaxBuildingCoverage / 100;

            if (houseCoverage == 0)
                return 0;

            //resize factor - ratio of house coverage to remove
            double resizeFactor = Math.Max(totalCoverageRatio - maxCoverageRatio, 0) / (houseCoverage / totalCoverage);
            return resizeFactor;

        }

        ////////////////////////////////////////////////
        //////////  cull and split buildings  //////////
        ////////////////////////////////////////////////

        private List<int> findIndexOfMixedBuilding(List<double> area, List<List<int>> areaTypeNumBuilding, double areaLimit, out int limitIndex)
        {
            List<int> output = new List<int>();

            //find limit index in area
            limitIndex = 0;
            while (area[limitIndex] <= areaLimit)
            {
                limitIndex += 1;
                if (limitIndex == area.Count)
                    break;
            }

            //find min and max index of area for each building
            //{min, max}

            List<List<int>> areaIndicesBoundary = new List<List<int>>();
            for (int i = 0; i < areaTypeNumBuilding.Count; i++)
            {
                List<int> indicesTemp = new List<int>();
                for (int j = 0; j < area.Count; j++)
                {
                    if (areaTypeNumBuilding[i][j] != 0)
                    {
                        indicesTemp.Add(j);
                    }
                }
                List<int> indicesBoundaryTemp = new List<int>();
                indicesBoundaryTemp.Add(indicesTemp.Min());
                indicesBoundaryTemp.Add(indicesTemp.Max());
                areaIndicesBoundary.Add(indicesBoundaryTemp);
            }

            //find number of houses under and over area limit
            List<int> underLimit = new List<int>();
            for (int i = 0; i < areaTypeNumBuilding.Count; i++)
            {
                int sum = 0;
                for (int j = 0; j < limitIndex; j++)
                {
                    sum += areaTypeNumBuilding[i][j];
                }
                underLimit.Add(sum);
            }
            List<int> overLimit = new List<int>();
            for (int i = 0; i < areaTypeNumBuilding.Count; i++)
            {
                int sum = 0;
                for (int j = limitIndex; j < area.Count; j++)
                {
                    sum += areaTypeNumBuilding[i][j];
                }
                overLimit.Add(sum);
            }

            //type 2 : mixed - min index < limit index <= max index
            for (int i = 0; i < areaTypeNumBuilding.Count; i++)
            {
                if (areaIndicesBoundary[i][0] < limitIndex && limitIndex <= areaIndicesBoundary[i][1])
                {
                    output.Add(i);
                }
            }

            return output;
        }
        private int limitIndexFinder(List<double> area, double areaLimit)
        {
            //find limit index in area
            int limitIndex = 0;
            while (area[limitIndex] <= areaLimit)
            {
                limitIndex += 1;
                if (limitIndex == area.Count)
                    break;
            }
            return limitIndex;
        }
        private List<double> coreProtrusionFactorFinder(List<Curve> aptLines, List<List<double>> houseEndParams, List<int> buildingAccessType, double fromEndParam, ParameterSet parameterSet, double width, Curve partialRegulation)
        {
            double coreWidth = parameterSet.CoreType.GetWidth();
            double coreDepth = parameterSet.CoreType.GetDepth();
            List<Curve> coreBoundary = new List<Curve>();
            List<double> protrusionFactor = new List<double>();
            double maxDepth = getMaxDepth(parameterSet.CoreType);

            //make base boundary
            for (int i = 0; i < aptLines.Count; i++)
            {
                Vector3d lineTangent = aptLines[i].TangentAtStart;
                Vector3d lineNormal = new Vector3d(lineTangent);
                lineNormal.Rotate(Math.PI / 2, Vector3d.ZAxis);
                if (buildingAccessType[i] == 0)
                {
                    if (aptLines[i].PointAt(fromEndParam).DistanceTo(aptLines[i].PointAt(1 - fromEndParam)) > coreWidth)
                    {
                        Point3d pt = aptLines[i].PointAt(fromEndParam);
                        pt.Transform(Transform.Translation(Vector3d.Multiply(lineTangent, -coreWidth / 2)));
                        pt.Transform(Transform.Translation(Vector3d.Multiply(lineNormal, width / 2 - coreDepth)));
                        Rectangle3d testRect = new Rectangle3d(new Plane(pt, lineTangent, lineNormal), coreWidth + aptLines[i].GetLength() * (1 - 2 * fromEndParam), coreDepth);
                        Curve testCurve = testRect.ToNurbsCurve().DuplicateCurve();
                        coreBoundary.Add(testCurve);
                    }
                    else
                    {
                        Point3d pt = aptLines[i].PointAt(0.5);
                        pt.Transform(Transform.Translation(Vector3d.Multiply(lineTangent, -coreWidth / 2)));
                        pt.Transform(Transform.Translation(Vector3d.Multiply(lineNormal, width / 2 - coreDepth)));
                        Rectangle3d testRect = new Rectangle3d(new Plane(pt, lineTangent, lineNormal), coreWidth, coreDepth);
                        Curve testCurve = testRect.ToNurbsCurve().DuplicateCurve();
                        coreBoundary.Add(testCurve);
                    }
                }
                else
                {
                    Point3d pt = aptLines[i].PointAt(houseEndParams[i][1]);
                    double dist = pt.DistanceTo(aptLines[i].PointAt(houseEndParams[i][houseEndParams[i].Count - 2]));
                    pt.Transform(Transform.Translation(Vector3d.Multiply(lineTangent, -coreWidth / 2)));
                    pt.Transform(Transform.Translation(Vector3d.Multiply(lineNormal, width / 2 - coreDepth)));
                    Rectangle3d testRect = new Rectangle3d(new Plane(pt, lineTangent, lineNormal), coreWidth + dist, coreDepth);
                    Curve testCurve = testRect.ToNurbsCurve().DuplicateCurve();
                    coreBoundary.Add(testCurve);
                }
            }

            //core protrusion factor
            for (int i = 0; i < aptLines.Count; i++)
            {
                Vector3d lineTangent = aptLines[i].TangentAtStart;
                Vector3d lineNormal = new Vector3d(lineTangent);
                lineNormal.Rotate(Math.PI / 2, Vector3d.ZAxis);

                double factor = factorFinder(partialRegulation, coreBoundary[i], maxDepth, lineNormal) * maxDepth / coreDepth;
                protrusionFactor.Add(factor);
            }
            return protrusionFactor;
        }
        private double factorFinder(Curve partialRegulation, Curve coreBoundary, double maxDepth, Vector3d normal)
        {
            //initial setting상속
            int searchResolution = 7;
            double low = 0;
            double high = 1;
            double mid = 1;
            int token = 0;
            int extremumChecker = 0;

            while (token < searchResolution)
            {
                mid = (low + high) / 2;

                //intersection check
                Curve clone = coreBoundary.DuplicateCurve();
                clone.Transform(Transform.Translation(Vector3d.Multiply(normal, mid * maxDepth)));
                if (Rhino.Geometry.Intersect.Intersection.CurveCurve(partialRegulation, clone, 0, 0).Count > 1)
                {
                    high = mid;
                }
                else
                {
                    low = mid;
                    extremumChecker += 1;
                }
                token += 1;
            }
            if (extremumChecker == searchResolution)
            {
                mid = 1;
            }
            else if (extremumChecker == 0)
            {
                mid = 0;
            }

            return mid;
        }
        private double getMaxDepth(CoreType coretype)
        {
            double output = double.NaN;
            if (coretype == CoreType.Horizontal)
                output = coretype.GetDepth() - Consts.corridorWidth;
            else if (coretype == CoreType.Parallel)
                output = coretype.GetDepth() - Consts.corridorWidth;
            else if (coretype == CoreType.Vertical_AG1)
                output = coretype.GetDepth() - 3660;
            return output;
        }

        ///////////////////////////////
        //////////  outputs  //////////
        ///////////////////////////////

        private List<List<Core>> coreMaker(ParameterSet parameterSet, List<int> buildingAccessType, List<Curve> aptLines, List<List<double>> houseEndParams, List<double> coreProtrusionFactor, double fromEndParam)
        {
            double[] parameters = parameterSet.Parameters;
            double storiesHigh = Math.Max((int)parameters[0], (int)parameters[1]);
            double width = parameters[2];
            double coreWidth = parameterSet.CoreType.GetWidth();
            double coreDepth = parameterSet.CoreType.GetDepth();

            List<List<Point3d>> coreOri = new List<List<Point3d>>();
            List<List<Vector3d>> coreVecX = new List<List<Vector3d>>();
            List<List<Vector3d>> coreVecY = new List<List<Vector3d>>();
            List<List<double>> coreStories = new List<List<double>>();


            for (int i = 0; i < buildingAccessType.Count; i++)
            {
                Vector3d tangentVec = aptLines[i].TangentAtStart;
                Vector3d verticalVec = new Vector3d(tangentVec);
                verticalVec.Rotate(Math.PI / 2, Vector3d.ZAxis);

                List<Point3d> coreOriTemp = new List<Point3d>();
                List<Vector3d> coreVecXTemp = new List<Vector3d>();
                List<Vector3d> coreVecYTemp = new List<Vector3d>();
                List<double> coreStoriesTemp = new List<double>();


                if (buildingAccessType[i] == 0)
                {
                    double surplusDist = new Regulation(storiesHigh).DistanceWW / 2;//***이건 코어 높이니까 한 층 높이를 더해야 할까?
                    if (aptLines[i].PointAt(fromEndParam).DistanceTo(aptLines[i].PointAt(1 - fromEndParam)) > coreWidth + new Regulation(storiesHigh).DistanceWW + surplusDist)
                    {
                        Point3d core1 = aptLines[i].PointAt(fromEndParam);
                        core1.Transform(Transform.Translation(Vector3d.Multiply(tangentVec, -coreWidth / 2)));
                        core1.Transform(Transform.Translation(Vector3d.Multiply(verticalVec, width / 2 - coreDepth * (1 - coreProtrusionFactor[i]))));
                        coreOriTemp.Add(core1);
                        core1 = aptLines[i].PointAt(1 - fromEndParam);
                        core1.Transform(Transform.Translation(Vector3d.Multiply(tangentVec, -coreWidth / 2)));
                        core1.Transform(Transform.Translation(Vector3d.Multiply(verticalVec, width / 2 - coreDepth * (1 - coreProtrusionFactor[i]))));
                        coreOriTemp.Add(core1);
                    }
                    else
                    {
                        Point3d core1 = aptLines[i].PointAt(0.5);
                        core1.Transform(Transform.Translation(Vector3d.Multiply(tangentVec, -coreWidth / 2)));
                        core1.Transform(Transform.Translation(Vector3d.Multiply(verticalVec, width / 2 - coreDepth * (1 - coreProtrusionFactor[i]))));
                        coreOriTemp.Add(core1);
                    }
                }
                else
                {
                    for (int j = 0; j < houseEndParams[i].Count; j++)
                    {
                        if (j % 2 == 1)
                        {
                            Point3d corePoint = aptLines[i].PointAt(houseEndParams[i][j]);
                            corePoint.Transform(Transform.Translation(Vector3d.Multiply(tangentVec, -coreWidth / 2)));
                            corePoint.Transform(Transform.Translation(Vector3d.Multiply(verticalVec, width / 2 - coreDepth * (1 - coreProtrusionFactor[i]))));
                            coreOriTemp.Add(corePoint);
                        }
                    }
                }
                for (int j = 0; j < coreOriTemp.Count; j++)
                {
                    coreVecXTemp.Add(tangentVec);
                    coreVecYTemp.Add(verticalVec);
                    coreStoriesTemp.Add(storiesHigh);
                }
                coreOri.Add(coreOriTemp);
                coreVecX.Add(coreVecXTemp);
                coreVecY.Add(coreVecYTemp);
                coreStories.Add(coreStoriesTemp);
            }

            //combine information into core
            List<List<Core>> core = new List<List<Core>>();
            for (int i = 0; i < coreOri.Count; i++)
            {
                List<Core> coreTemp = new List<Core>();
                for (int j = 0; j < coreOri[i].Count; j++)
                {
                    coreTemp.Add(new Core(coreOri[i][j], coreVecX[i][j], coreVecY[i][j], parameterSet.CoreType, storiesHigh, parameterSet.CoreType.GetDepth() * (1 - coreProtrusionFactor[i])));
                }
                core.Add(coreTemp);
            }
            return core;
        }

        private List<List<List<Household>>> householdMaker(ParameterSet parameterSet, List<int> buildingAccessType, List<Curve> aptLines, List<List<double>> houseEndParams, List<double> coreProtrusionFactor, List<List<int>> householdOrderBuilding, List<double> areaLength, List<double> stretchFactor, Curve wholeRegulationHigh, double minimumArea)
        {
            //initial settings
            double[] parameters = parameterSet.Parameters;
            double storiesHigh = Math.Max((int)parameters[0], (int)parameters[1]);
            double storiesLow = Math.Min((int)parameters[0], (int)parameters[1]);
            double width = parameters[2];
            double coreWidth = parameterSet.CoreType.GetWidth();
            double coreDepth = parameterSet.CoreType.GetDepth();

            List<List<List<Point3d>>> homeOri = new List<List<List<Point3d>>>();
            List<List<List<Vector3d>>> homeVecX = new List<List<List<Vector3d>>>();
            List<List<List<Vector3d>>> homeVecY = new List<List<List<Vector3d>>>();
            List<List<List<double>>> xa = new List<List<List<double>>>();
            List<List<List<double>>> xb = new List<List<List<double>>>();
            List<List<List<double>>> ya = new List<List<List<double>>>();
            List<List<List<double>>> yb = new List<List<List<double>>>();
            List<List<List<int>>> targetAreaType = new List<List<List<int>>>();
            List<List<List<double>>> exclusiveArea = new List<List<List<double>>>();
            List<List<List<List<int>>>> connectedCoreIndex = new List<List<List<List<int>>>>();

            //combine

            List<List<List<Household>>> household = new List<List<List<Household>>>();
            for (int i = 0; i < householdOrderBuilding.Count; i++)
            {
                List<List<Household>> householdTemp = new List<List<Household>>();
                for (int j = 0; j < storiesHigh; j++)
                {
                    List<List<Household>> householdLow = householdLowMaker(buildingAccessType, aptLines, houseEndParams, coreProtrusionFactor, householdOrderBuilding, areaLength, stretchFactor, width, coreWidth, coreDepth, j);
                    List<List<Household>> householdHigh = householdHighMaker(householdLow, buildingAccessType, wholeRegulationHigh, householdOrderBuilding);
                    List<Household> householdTempTemp = new List<Household>();
                    if (j < storiesLow)
                    {
                        for (int k = 0; k < householdOrderBuilding[i].Count; k++)
                        {
                            householdTempTemp.Add(householdLow[i][k]);
                        }
                    }
                    else
                    {
                        for (int k = 0; k < householdOrderBuilding[i].Count; k++)
                        {
                            //foreach (var hhptt in householdHigh[i])
                            //{
                            //    if(househol)
                            //}
                            if (k == householdHigh[i].Count)
                                break;
                            householdTempTemp.Add(householdHigh[i][k]);
                        }
                    }
                    householdTemp.Add(householdTempTemp);
                }
                household.Add(householdTemp);
            }
            household = householdCuller(household, minimumArea);
            return household;
        }
        private List<List<List<Household>>> householdCuller(List<List<List<Household>>> household, double minimumArea)
        {
            List<List<List<Household>>> output = new List<List<List<Household>>>();
            for (int i = 0; i < household.Count; i++)
            {
                List<List<Household>> outputTemp = new List<List<Household>>();
                for (int j = 0; j < household[i].Count; j++)
                {
                    List<Household> outputTempTemp = new List<Household>();
                    for (int k = 0; k < household[i][j].Count; k++)
                    {
                        if (household[i][j][k].GetArea() >= minimumArea)
                        {
                            outputTempTemp.Add(household[i][j][k]);
                        }

                    }
                    outputTemp.Add(outputTempTemp);
                }
                output.Add(outputTemp);
            }
            return output;
        }
        private List<List<Household>> householdLowMaker(List<int> buildingAccessType, List<Curve> aptLines, List<List<double>> houseEndParams, List<double> coreProtrusionFactor, List<List<int>> householdOrderBuilding, List<double> areaLength, List<double> stretchFactor, double width, double coreWidth, double coreDepth, double floor)
        {
            List<List<Point3d>> homeOriRaw = new List<List<Point3d>>();
            List<List<Vector3d>> homeVecXRaw = new List<List<Vector3d>>();
            List<List<Vector3d>> homeVecYRaw = new List<List<Vector3d>>();
            List<List<double>> xaRaw = new List<List<double>>();
            List<List<double>> xbRaw = new List<List<double>>();
            List<List<double>> yaRaw = new List<List<double>>();
            List<List<double>> ybRaw = new List<List<double>>();
            List<List<double>> exclusiveAreaRaw = new List<List<double>>();
            List<List<int>> targetAreaTypeRaw = new List<List<int>>(householdOrderBuilding);
            List<List<List<double>>> wallFactorRaw = new List<List<List<double>>>();

            for (int i = 0; i < aptLines.Count; i++)
            {
                Vector3d tangentVec = aptLines[i].TangentAtStart;
                Vector3d verticalVec = new Vector3d(tangentVec);
                verticalVec.Rotate(Math.PI / 2, Vector3d.ZAxis);

                List<Point3d> homeOriFloor = new List<Point3d>();
                List<Vector3d> homeVecXFloor = new List<Vector3d>();
                List<Vector3d> homeVecYFloor = new List<Vector3d>();
                List<double> xaFloor = new List<double>();
                List<double> xbFloor = new List<double>();
                List<double> yaFloor = new List<double>();
                List<double> ybFloor = new List<double>();
                List<double> exclusiveAreaFloor = new List<double>();
                List<List<double>> wallFactorFloor = new List<List<double>>();

                for (int j = 0; j < householdOrderBuilding[i].Count; j++)
                {
                    Point3d homeOriHouse;
                    Vector3d homeVecXHouse;
                    Vector3d homeVecYHouse = verticalVec;
                    double xaHouse;
                    double xbHouse;
                    double yaHouse;
                    double ybHouse;
                    double exclusiveArea;
                    List<double> wallFactor;

                    if (buildingAccessType[i] == 0)
                    {
                        xaHouse = areaLength[targetAreaTypeRaw[i][j]] * stretchFactor[i];
                        xbHouse = 0;
                        yaHouse = width - Consts.corridorWidth;
                        ybHouse = 0;
                        exclusiveArea = exclusiveAreaCalculatorAG1(xaHouse, xbHouse, yaHouse, ybHouse, targetAreaTypeRaw[i][j], Consts.balconyDepth);
                        homeOriHouse = aptLines[i].PointAt(houseEndParams[i][j + 1]);
                        homeOriHouse.Transform(Transform.Translation(Vector3d.Multiply(verticalVec, width / 2 - coreDepth * (1 - coreProtrusionFactor[i]))));
                        homeVecXHouse = Vector3d.Multiply(tangentVec, -1);
                        //homeOriHouse.Transform(Transform.Translation(Vector3d.Multiply(homeVecXHouse, coreWidth / 2)));
                        wallFactor = new List<double>(new double[] { 1, 0.5, 1, 0.5 });
                    }
                    else
                    {
                        xaHouse = areaLength[targetAreaTypeRaw[i][j]] * stretchFactor[i];
                        xbHouse = coreWidth / 2;
                        yaHouse = width;
                        ybHouse = coreDepth * (1 - coreProtrusionFactor[i]);
                        exclusiveArea = exclusiveAreaCalculatorAG1(xaHouse, xbHouse, yaHouse, ybHouse, targetAreaTypeRaw[i][j], Consts.balconyDepth);
                        homeOriHouse = aptLines[i].PointAt(houseEndParams[i][((int)(j / 2) * 2) + 1]);
                        homeOriHouse.Transform(Transform.Translation(Vector3d.Multiply(verticalVec, width / 2 - coreDepth * (1 - coreProtrusionFactor[i]))));
                        homeVecXHouse = Vector3d.Multiply(tangentVec, -(0.5 - j % 2) * 2);
                        homeOriHouse.Transform(Transform.Translation(Vector3d.Multiply(homeVecXHouse, coreWidth / 2)));
                        wallFactor = new List<double>(new double[] { 1, 1, 0.5, 1, 0.5, 1 });
                    }
                    homeOriHouse.Transform(Transform.Translation(Vector3d.Multiply(Consts.PilotiHeight + Consts.FloorHeight * floor, Vector3d.ZAxis)));
                    homeOriFloor.Add(homeOriHouse);
                    homeVecXFloor.Add(homeVecXHouse);
                    homeVecYFloor.Add(homeVecYHouse);
                    xaFloor.Add(xaHouse);
                    xbFloor.Add(xbHouse);
                    yaFloor.Add(yaHouse);
                    ybFloor.Add(ybHouse);
                    exclusiveAreaFloor.Add(exclusiveArea);
                    wallFactorFloor.Add(wallFactor);
                }
                homeOriRaw.Add(homeOriFloor);
                homeVecXRaw.Add(homeVecXFloor);
                homeVecYRaw.Add(homeVecYFloor);
                xaRaw.Add(xaFloor);
                xbRaw.Add(xbFloor);
                yaRaw.Add(yaFloor);
                ybRaw.Add(ybFloor);
                exclusiveAreaRaw.Add(exclusiveAreaFloor);
                wallFactorRaw.Add(wallFactorFloor);
            }

            ////////windows and entrances////////
            List<List<List<Line>>> lightingEdges = lightingEdgesMaker(homeOriRaw, homeVecXRaw, homeVecYRaw, xaRaw, xbRaw, yaRaw, ybRaw);
            List<List<Point3d>> entrancePoints = entrancePointsMaker(homeOriRaw, homeVecXRaw, homeVecYRaw, xaRaw, xbRaw, yaRaw, ybRaw);

            List<List<Household>> householdLow = new List<List<Household>>();
            for (int i = 0; i < homeOriRaw.Count; i++)
            {
                List<Household> householdLowTemp = new List<Household>();
                for (int j = 0; j < homeOriRaw[i].Count; j++)
                {
                    householdLowTemp.Add(new Household(homeOriRaw[i][j], homeVecXRaw[i][j], homeVecYRaw[i][j], xaRaw[i][j], xbRaw[i][j], yaRaw[i][j], ybRaw[i][j], targetAreaTypeRaw[i][j], exclusiveAreaRaw[i][j], lightingEdges[i][j], entrancePoints[i][j], wallFactorRaw[i][j]));
                }
                householdLow.Add(householdLowTemp);
            }

            return householdLow;
        }
        private List<List<Household>> householdHighMaker(List<List<Household>> householdLow, List<int> buildingAccessType, Curve wholeRegulationHigh, List<List<int>> householdOrderBuilding)
        {
            //upper type
            List<List<Point3d>> homeOriUpper = new List<List<Point3d>>();
            List<List<Vector3d>> homeVecXUpper = new List<List<Vector3d>>();
            List<List<Vector3d>> homeVecYUpper = new List<List<Vector3d>>();
            List<List<double>> xaUpper = new List<List<double>>();
            List<List<double>> xbUpper = new List<List<double>>();
            List<List<double>> yaUpper = new List<List<double>>();
            List<List<double>> ybUpper = new List<List<double>>();
            List<List<double>> exclusiveAreaUpper = new List<List<double>>();
            List<List<int>> targetAreaTypeUpper = new List<List<int>>(householdOrderBuilding);

            for (int i = 0; i < householdLow.Count; i++)
            {
                List<Point3d> homeOriFloor = new List<Point3d>();
                List<Vector3d> homeVecXFloor = new List<Vector3d>();
                List<Vector3d> homeVecYFloor = new List<Vector3d>();
                List<double> xaFloor = new List<double>();
                List<double> xbFloor = new List<double>();
                List<double> yaFloor = new List<double>();
                List<double> ybFloor = new List<double>();
                List<double> exclusiveAreaFloor = new List<double>();

                for (int j = 0; j < householdLow[i].Count; j++)
                {
                    //draw outline
                    List<Point3d> outlinePoints = new List<Point3d>();
                    Point3d pt = new Point3d(householdLow[i][j].Origin);
                    Vector3d x = new Vector3d(householdLow[i][j].XDirection);
                    Vector3d y = new Vector3d(householdLow[i][j].YDirection);
                    double xa = householdLow[i][j].XLengthA;
                    double xb = householdLow[i][j].XLengthB;
                    double ya = householdLow[i][j].YLengthA;
                    double yb = householdLow[i][j].YLengthB;
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
                    Point3d.CullDuplicates(outlinePoints, 0);
                    pt.Transform(Transform.Translation(Vector3d.Multiply(x, xb)));
                    outlinePoints.Add(pt);
                    Polyline outlinePolyline = new Polyline(outlinePoints);
                    Curve outlineCurve = outlinePolyline.ToNurbsCurve();
                    outlineCurve.Transform(Transform.Translation(Vector3d.Multiply(Vector3d.ZAxis, -pt.Z)));

                    //cutting check
                    double xaTemp = xa;
                    double xbTemp = xb;
                    double yaTemp = ya;
                    double ybTemp = yb;
                    var intersection = Rhino.Geometry.Intersect.Intersection.CurveCurve(wholeRegulationHigh, outlineCurve, 0, 0);

                    if (wholeRegulationHigh.Contains(pt) == PointContainment.Outside)
                        continue;
                    //Rhino.RhinoDoc.ActiveDoc.Objects.AddCurve(outlineCurve);
                    //Rhino.RhinoDoc.ActiveDoc.Objects.AddCurve(wholeRegulationHigh);

                    if (intersection.Count > 1)
                    {
                        if (buildingAccessType[i] == 0)
                        {
                            List<double> dists = new List<double>();
                            for (int k = 0; k < intersection.Count; k++)
                            {
                                dists.Add(Vector3d.Multiply(Vector3d.Multiply(y, -1), new Vector3d(intersection[k].PointA - householdLow[i][j].Origin)));
                            }
                            yaTemp = dists.Min();
                        }
                        else
                        {
                            List<double> distsPositive = new List<double>();
                            List<double> distsNegative = new List<double>();
                            distsPositive.Add(yaTemp - ybTemp);
                            distsNegative.Add(-ybTemp);

                            for (int k = 0; k < intersection.Count; k++)
                            {
                                double dist = Vector3d.Multiply(Vector3d.Multiply(y, -1), new Vector3d(intersection[k].PointA - householdLow[i][j].Origin));
                                if (dist > 1)
                                {
                                    distsPositive.Add(dist);
                                }
                                else if (dist < 1)
                                {
                                    distsNegative.Add(dist);
                                }
                            }
                            if (intersection.Count == 2 && distsPositive.Count == 2 && distsNegative.Count == 2)
                            {
                                xaTemp = 0;
                                xbTemp = 0;
                                yaTemp = 0;
                                ybTemp = 0;
                            }
                            else
                            {
                                double posMin = distsPositive.Min();
                                double negMax = distsNegative.Max();
                                yaTemp = posMin - negMax;
                                ybTemp = -negMax;
                            }

                        }
                    }
                    double exclusiveArea = exclusiveAreaCalculatorAG1(xaTemp, xbTemp, yaTemp, ybTemp, targetAreaTypeUpper[i][j], Consts.balconyDepth);

                    homeOriFloor.Add(pt);
                    homeVecXFloor.Add(x);
                    homeVecYFloor.Add(y);
                    xaFloor.Add(xaTemp);
                    xbFloor.Add(xbTemp);
                    yaFloor.Add(yaTemp);
                    ybFloor.Add(ybTemp);
                    exclusiveAreaFloor.Add(exclusiveArea);
                }
                homeOriUpper.Add(homeOriFloor);
                homeVecXUpper.Add(homeVecXFloor);
                homeVecYUpper.Add(homeVecYFloor);
                xaUpper.Add(xaFloor);
                xbUpper.Add(xbFloor);
                yaUpper.Add(yaFloor);
                ybUpper.Add(ybFloor);
                exclusiveAreaUpper.Add(exclusiveAreaFloor);
            }

            ////////windows and entrances////////
            List<List<List<Line>>> lightingEdges = lightingEdgesMaker(homeOriUpper, homeVecXUpper, homeVecYUpper, xaUpper, xbUpper, yaUpper, ybUpper);
            List<List<Point3d>> entrancePoints = entrancePointsMaker(homeOriUpper, homeVecXUpper, homeVecYUpper, xaUpper, xbUpper, yaUpper, ybUpper);

            List<List<Household>> householdHigh = new List<List<Household>>();
            for (int i = 0; i < homeOriUpper.Count; i++)
            {
                List<Household> householdHighTemp = new List<Household>();
                for (int j = 0; j < homeOriUpper[i].Count; j++)
                {
                    householdHighTemp.Add(new Household(homeOriUpper[i][j], homeVecXUpper[i][j], homeVecYUpper[i][j], xaUpper[i][j], xbUpper[i][j], yaUpper[i][j], ybUpper[i][j], targetAreaTypeUpper[i][j], exclusiveAreaUpper[i][j], lightingEdges[i][j], entrancePoints[i][j], householdLow[i][j].WallFactor));
                }
                householdHigh.Add(householdHighTemp);
            }

            return householdHigh;
        }
        private double exclusiveAreaCalculatorAG1(double xa, double xb, double ya, double yb, double targetArea, double balconyDepth)
        {
            double exclusiveArea = xa * ya - xb * yb;

            exclusiveArea -= (xa + xa - xb) * balconyDepth;

            if (targetArea <= Consts.AreaLimit)
            {
                exclusiveArea += (xa - xb) * balconyDepth;
            }

            return exclusiveArea;
        }
        private List<List<List<Line>>> lightingEdgesMaker(List<List<Point3d>> homeOri, List<List<Vector3d>> homeVecX, List<List<Vector3d>> homeVecY, List<List<double>> xa, List<List<double>> xb, List<List<double>> ya, List<List<double>> yb)
        {
            List<List<List<Line>>> lightingWindows = new List<List<List<Line>>>();
            for (int i = 0; i < homeOri.Count; i++)
            {
                List<List<Line>> winBuilding = new List<List<Line>>();
                for (int j = 0; j < homeOri[i].Count; j++)
                {
                    List<Line> winHouse = new List<Line>();
                    Point3d winPt1 = homeOri[i][j];
                    Point3d winPt2 = homeOri[i][j];
                    winPt1.Transform(Transform.Translation(Vector3d.Multiply(yb[i][j], homeVecY[i][j])));
                    winPt2.Transform(Transform.Translation(Vector3d.Add(Vector3d.Multiply(yb[i][j], homeVecY[i][j]), Vector3d.Multiply(xa[i][j] - xb[i][j], homeVecX[i][j]))));
                    winHouse.Add(new Line(winPt1, winPt2));
                    winPt2.Transform(Transform.Translation(Vector3d.Multiply(-ya[i][j], homeVecY[i][j])));
                    Point3d winPt3 = winPt2;
                    Point3d winPt4 = winPt2;
                    winPt4.Transform(Transform.Translation(Vector3d.Multiply(-xa[i][j], homeVecX[i][j])));
                    winHouse.Add(new Line(winPt4, winPt3));
                    winBuilding.Add(winHouse);
                }
                lightingWindows.Add(winBuilding);
            }
            return lightingWindows;
        }
        private List<List<Point3d>> entrancePointsMaker(List<List<Point3d>> homeOri, List<List<Vector3d>> homeVecX, List<List<Vector3d>> homeVecY, List<List<double>> xa, List<List<double>> xb, List<List<double>> ya, List<List<double>> yb)
        {
            List<List<Point3d>> entrancePoints = new List<List<Point3d>>();
            for (int i = 0; i < homeOri.Count; i++)
            {
                List<Point3d> entBuilding = new List<Point3d>();
                for (int j = 0; j < homeOri[i].Count; j++)
                {
                    Point3d ent = homeOri[i][j];
                    ent.Transform(Transform.Translation(Vector3d.Multiply(homeVecX[i][j], -xb[i][j] / 2)));
                    entBuilding.Add(ent);
                }
                entrancePoints.Add(entBuilding);
            }
            return entrancePoints;
        }

        private List<List<Curve>> buildingOutlineMaker(List<Curve> aptLines, List<int> buildingAccessType, double width, double coreDepth)
        {
            List<List<Curve>> buildingOutlines = new List<List<Curve>>();
            for (int i = 0; i < aptLines.Count; i++)
            {
                List<Curve> outlineTemp = new List<Curve>();
                Curve outline;
                if (buildingAccessType[i] == 0)
                {
                    List<Point3d> tempOutlinePts = new List<Point3d>();
                    Curve offset1 = aptLines[i].Offset(Plane.WorldXY, width / 2, 0, CurveOffsetCornerStyle.None)[0];
                    Curve offset2 = aptLines[i].Offset(Plane.WorldXY, -width / 2 + coreDepth, 0, CurveOffsetCornerStyle.None)[0];
                    tempOutlinePts.Add(offset1.PointAtStart);
                    tempOutlinePts.Add(offset1.PointAtEnd);
                    tempOutlinePts.Add(offset2.PointAtEnd);
                    tempOutlinePts.Add(offset2.PointAtStart);
                    tempOutlinePts.Add(offset1.PointAtStart);
                    outline = new Polyline(tempOutlinePts).ToNurbsCurve().DuplicateCurve();
                    outlineTemp.Add(outline);
                    buildingOutlines.Add(outlineTemp);
                }
                else
                {
                    List<Point3d> tempOutlinePts = new List<Point3d>();
                    Curve offset1 = aptLines[i].Offset(Plane.WorldXY, width / 2, 0, CurveOffsetCornerStyle.None)[0];
                    Curve offset2 = aptLines[i].Offset(Plane.WorldXY, -width / 2, 0, CurveOffsetCornerStyle.None)[0];
                    tempOutlinePts.Add(offset1.PointAtStart);
                    tempOutlinePts.Add(offset1.PointAtEnd);
                    tempOutlinePts.Add(offset2.PointAtEnd);
                    tempOutlinePts.Add(offset2.PointAtStart);
                    tempOutlinePts.Add(offset1.PointAtStart);
                    outline = new Polyline(tempOutlinePts).ToNurbsCurve().DuplicateCurve();
                    outlineTemp.Add(outline);
                    buildingOutlines.Add(outlineTemp);
                }
            }
            return buildingOutlines;
        }

        private Rectangle3d innerRectFinder(Polyline boundary, int angleRes, int gridRes, int ratioRes, int binaryIterNum)
        {
            //initial settings
            double bestArea = 0;
            Rectangle3d innerRect = Rectangle3d.Unset;

            //iteration start
            List<double> angles = searchAngle(boundary, 0, Math.PI * 2, angleRes, true);
            for (int i = 0; i < angles.Count; i++)
            {
                Polyline boundaryClone = new Polyline(boundary);
                boundaryClone.Transform(Transform.Rotation(angles[i], Point3d.Origin));
                List<Point3d> points = searchPoint(boundaryClone, boundaryClone.BoundingBox.Center, boundaryClone.BoundingBox.Diagonal.X, boundaryClone.BoundingBox.Diagonal.Y, gridRes);
                for (int j = 0; j < points.Count; j++)
                {
                    double maxHeight, maxWidth;
                    maxDims(points[j], boundaryClone, out maxHeight, out maxWidth);
                    List<double> ratios = searchRatio(Math.Max(1, bestArea / Math.Pow(maxHeight, 2)), Math.Min(10, Math.Pow(maxWidth, 2) / bestArea), ratioRes);
                    for (int k = 0; k < ratios.Count; k++)
                    {
                        double minSearchWidth = Math.Sqrt(bestArea * ratios[k]);
                        double maxSearchWidth = Math.Min(maxWidth, maxHeight * ratios[k]);
                        if (minSearchWidth < maxSearchWidth)
                        {
                            double widthTemp = widthFinder(boundary, bestArea, ratios[k], points[j], minSearchWidth, maxSearchWidth, binaryIterNum);
                            if (bestArea < widthTemp * widthTemp * ratios[k])
                            {
                                bestArea = widthTemp * widthTemp * ratios[k];
                                innerRect = rectMaker(angles[i], points[j], ratios[k], widthTemp);
                            }
                        }
                    }
                }
            }

            return innerRect;
        }

        private List<double> searchAngle(Polyline boundary, double minAngle, double maxAngle, int angleRes, bool edgeAlign)
        {
            //initial settings
            double midAngle = (maxAngle + minAngle) / 2;
            List<double> angles = new List<double>();

            for (int i = -angleRes; i < angleRes + 1; i++)
            {
                angles.Add(midAngle + (maxAngle - minAngle) / (angleRes * 2) * i);
            }

            if (edgeAlign)
            {
                if ((int)boundary.ToNurbsCurve().ClosedCurveOrientation(new Vector3d(0, 0, 1)) == -1)
                {
                    boundary.Reverse();
                }
                Line[] edges = boundary.GetSegments();
                for (int i = 0; i < edges.Length; i++)
                {
                    double tempAngle = Vector3d.VectorAngle(edges[i].UnitTangent, Vector3d.XAxis) * Math.Sign(Vector3d.CrossProduct(edges[i].UnitTangent, Vector3d.XAxis).Z);
                    angles.Add(tempAngle);
                }
            }

            return angles;
        }

        private List<Point3d> searchPoint(Polyline boundary, Point3d centerPoint, double width, double height, int gridRes)
        {
            //initial settings
            double alpha = 1;
            Point3d minP = new Point3d(centerPoint.X - width / 2 + alpha, centerPoint.Y - height / 2 + alpha, 0);
            Point3d maxP = new Point3d(centerPoint.X + width / 2 - alpha, centerPoint.Y + height / 2 - alpha, 0);
            double dX = (maxP.X - minP.X) / (gridRes * 2);
            double dY = (maxP.Y - minP.Y) / (gridRes * 2);
            List<Point3d> gridPoints = new List<Point3d>();

            //make grid points
            for (int i = -gridRes; i < gridRes + 1; i++)
            {
                gridPoints.Add(new Point3d(centerPoint.X + dX * i, centerPoint.Y + dY * i, 0));
            }

            //make mid points; mid points are center points of inner rectangles
            List<Point3d> mids = intersectionMids(gridPoints, boundary);

            return mids;
        }

        private List<Point3d> intersectionMids(List<Point3d> gridPoints, Polyline boundary)
        {
            //initial setting
            double alpha = 1;
            var box = boundary.BoundingBox;

            //make vertical&horizontal lines crossing outline
            List<Line> lines = new List<Line>();
            for (int i = 0; i < gridPoints.Count; i++)
            {
                lines.Add(new Line(new Point3d(gridPoints[i].X, box.Min.Y - alpha, 0), new Point3d(gridPoints[i].X, box.Max.Y + alpha, 0)));
                lines.Add(new Line(new Point3d(box.Min.X - alpha, gridPoints[i].Y, 0), new Point3d(box.Max.X + alpha, gridPoints[i].Y, 0)));
            }

            //find mid points of intersection segments
            List<Point3d> mids = new List<Point3d>();
            for (int i = 0; i < lines.Count; i++)
            {
                Curve curveA = lines[i].ToNurbsCurve();
                Curve curveB = boundary.ToNurbsCurve();
                List<Point3d> points = new List<Point3d>();
                int k = Rhino.Geometry.Intersect.Intersection.CurveCurve(curveA, curveB, 0, 0).Count;
                for (int j = 0; j < k; j++)
                {
                    points.Add(Rhino.Geometry.Intersect.Intersection.CurveCurve(curveA, curveB, 0, 0)[j].PointA);
                }
                if (points.Count % 2 == 0)
                {
                    for (int j = 0; j < points.Count; j += 2)
                    {
                        mids.Add(new Point3d((points[j].X + points[j + 1].X) / 2, (points[j].Y + points[j + 1].Y) / 2, 0));
                    }
                }
            }
            return mids;
        }

        private void maxDims(Point3d point, Polyline boundary, out double maxHeight, out double maxWidth)
        {
            //initial setting
            double alpha = 1;
            var box = boundary.BoundingBox;

            //get max height and max width
            Line midlineY = new Line(new Point3d(point.X, box.Min.Y - alpha, 0), new Point3d(point.X, box.Max.Y + alpha, 0));
            Line midlineX = new Line(new Point3d(box.Min.X - alpha, point.Y, 0), new Point3d(box.Max.X + alpha, point.Y, 0));
            List<Point3d> widthP = new List<Point3d>(intersectionPoints(midlineX, boundary));
            List<Point3d> heightP = new List<Point3d>(intersectionPoints(midlineY, boundary));
            List<double> widthV = new List<double>();
            List<double> heightV = new List<double>();
            foreach (Point3d k in widthP)
            {
                widthV.Add(k.DistanceTo(point));
            }
            foreach (Point3d k in heightP)
            {
                heightV.Add(k.DistanceTo(point));
            }
            maxWidth = widthV.Min() * 2;
            maxHeight = heightV.Min() * 2;
        }

        private List<Point3d> intersectionPoints(Line gridL, Polyline outline)
        {
            List<Point3d> no = new List<Point3d>();
            List<Point3d> mids = new List<Point3d>();
            Curve curveA = gridL.ToNurbsCurve();
            Curve curveB = outline.ToNurbsCurve();
            List<Point3d> points = new List<Point3d>();
            int k = Rhino.Geometry.Intersect.Intersection.CurveCurve(curveA, curveB, 0, 0).Count;
            for (int i = 0; i < k; i++)
            {
                points.Add(Rhino.Geometry.Intersect.Intersection.CurveCurve(curveA, curveB, 0, 0)[i].PointA);
            }
            if (points.Count % 2 == 1)
                return no;
            else
                return points;
        }

        private List<double> searchRatio(double minRatio, double maxRatio, int ratioRes)
        {
            //initial setting
            List<double> ratios = new List<double>();

            //make ratios
            if (minRatio < maxRatio)
            {
                for (int i = 0; i < ratioRes + 1; i++)
                {
                    ratios.Add(minRatio * Math.Pow(maxRatio / minRatio, (double)i / ratioRes));
                }
            }

            return ratios;
        }

        private double widthFinder(Polyline boundary, double bestArea, double ratio, Point3d point, double minSearchWidth, double maxSearchWidth, int binaryIterNum)
        {
            double low = minSearchWidth;
            double high = maxSearchWidth;
            double mid = (low + high) / 2;

            for (int i = 0; i < binaryIterNum; i++)
            {
                mid = (low + high) / 2;
                if (isRectangleInside(point, mid, ratio, boundary))
                    low = mid;
                else
                    high = mid;
            }

            return mid;
        }

        private bool isRectangleInside(Point3d midPoint, double width, double ratio, Polyline boundary)
        {
            Rectangle3d rect = new Rectangle3d(Plane.WorldXY, new Point3d(midPoint.X - width / 2, midPoint.Y - width / 2 * ratio, 0), new Point3d(midPoint.X + width / 2, midPoint.Y + width / 2 * ratio, 0));
            List<Point3d> no = new List<Point3d>();
            List<Point3d> mids = new List<Point3d>();
            Curve curveA = rect.ToNurbsCurve();
            Curve curveB = boundary.ToNurbsCurve();
            List<Point3d> points = new List<Point3d>();
            int k = Rhino.Geometry.Intersect.Intersection.CurveCurve(curveA, curveB, 0, 0).Count;
            if (k == 0 && width * width / ratio < areaOfPolygon(boundary))
                return true;
            else
                return false;
        }
        private double areaOfPolygon(Polyline x)
        {
            List<Point3d> y = new List<Point3d>(x);
            double area = 0;
            for (int i = 0; i < y.Count - 1; i++)
            {
                area += y[i].X * y[i + 1].Y;
                area -= y[i].Y * y[i + 1].X;
            }

            area /= 2;
            return (area < 0 ? -area : area);
        }
        private Rectangle3d rectMaker(double angle, Point3d point, double ratio, double width)
        {
            Point3d ori = new Point3d(point);
            ori.Transform(Transform.Translation(new Vector3d(-width / 2, -width / ratio / 2, 0)));
            Rectangle3d rect = new Rectangle3d(new Plane(ori, Vector3d.ZAxis), width, width / ratio);
            rect.Transform(Transform.Rotation(-angle, Point3d.Origin));
            return rect;
        }

        private List<double> SplitWithRegions(Curve target, List<Curve> cutter)
        {
            List<double> intersectParam = new List<double>();
            foreach (Curve i in cutter)
            {
                var tempIntersection = Rhino.Geometry.Intersect.Intersection.CurveCurve(target, i, 0.1, 0.1);

                foreach (Rhino.Geometry.Intersect.IntersectionEvent j in tempIntersection)
                {
                    intersectParam.Add(j.ParameterA);
                }
            }

            return intersectParam;
        }



        private List<List<double>> AptPacking(List<double> unit, List<double> aptLine, List<double> unitRate, double aptWidth, double corearea, out List<List<UnitType>> isclearance)
        {
            #region 비율 조정

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
                if (unit[i] <= Consts.minimumArea)
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



            List<double> aptLineLengths = aptLine;
            List<Unit> units = new List<Unit>();

            for (int i = 0; i < eachUnitLength.Length; i++)
            {
                UnitType type = unit[i] > Consts.AreaLimit ? UnitType.Tower : UnitType.Corridor;
                Unit tempunit = new Unit(unit[i], eachUnitLength[i], type);
                units.Add(tempunit);
            }

            //Unit unit1 = new Unit(20, 5234, UnitType.Corridor);
            //Unit unit2 = new Unit(55, 12324, UnitType.Tower);
            //Unit unit3 = new Unit(70, 17065, UnitType.Tower);
            List<double> rates = balancedRate.Select(n=>(double)n/100).ToList();


            UnitDistributor distributor = new UnitDistributor(aptLineLengths, units, rates);
            distributor.DistributeByRate();

            List<List<UnitType>> types = new List<List<UnitType>>();
            List<List<double>> positions = new List<List<double>>();
            foreach (ApartLine line in distributor.aptLines)
            {
                //POSITION 먼저 구해야 정렬된 TYPE 값 얻음
                var thisLinePositions = line.Units.Positions();
                positions.Add(thisLinePositions);

                var thisLineClearances = line.Units.GetTypes();
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


        #region UnitDistributor


        class UnitDistributor
        {
            public List<ApartLine> aptLines;
            List<Unit> units;
            double[] unitrates;
            double[] added;

            public UnitDistributor(List<double> aptLineLengths, List<Unit> units, List<double> unitRates)
            {
                aptLines = aptLineLengths.Select(n => new ApartLine(n)).ToList();
                this.units = units;
                added = units.Select(n => 0.0).ToArray();
                unitrates = unitRates.ToArray();
            }

            public void DistributeByRate()
            {
                for (int k = 0; k < aptLines.Count; k++)
                {
                    while (true)
                    {
                        double minValue = double.MaxValue;
                        int minIndex = -1;
                        for (int i = 0; i < unitrates.Length; i++)
                        {
                            double[] tempPrediction = new double[unitrates.Length];

                            for (int j = 0; j < tempPrediction.Length; j++)
                            {
                                //선택한비율 index와 예측비율값 칸 index 같으면 +1 값 아니면 그대로
                                tempPrediction[j] = (added[j] + ((j == i) ? 1 : 0)) / (added.Sum() + 1);
                            }
                            double[] different = new double[tempPrediction.Length];
                            for (int j = 0; j < different.Length; j++)
                            {
                                different[j] = Math.Abs(tempPrediction[j] - unitrates[j]);
                            }

                            if (minValue > different.Sum())
                            {
                                minValue = different.Sum();
                                minIndex = i;
                            }
                        }


                        //유닛 넣기
                        //성공 - 다음
                        //실패 - 1. 작은면적 들어갈 공간 있음 - 넣음
                        //       2. 작은면적 들어갈 공간 없음 - 끝
                        if (minIndex == -1)
                            minIndex = 0;
                        var result = aptLines[k].Add(units[minIndex]);
                        if (result)
                        {
                            added[minIndex]++;
                            //RhinoApp.WriteLine("{0} index added(by rate), result : {1},{2}", minIndex, added[0], added[1]);
                        }

                        else
                        {

                            bool nothingAdded = true;
                            for (int i = units.Count-1; i >=0 ; i--)
                            {
                                var addResult = aptLines[k].Add(units[i]);
                                if (addResult)
                                {
                                    added[i]++;
                                    nothingAdded = false;
                                    break;
                                }
                            }

                            if(nothingAdded)
                            {
                                //홀수 tower 체크
                                int towerCount = aptLines[k].Units.GetTypes().Where(n => n == UnitType.Tower).Count();

                                //타워가 홀수개 존재하면
                                if (towerCount % 2 != 0)
                                {
                                    int towerindex = aptLines[k].Units.Contains.FindIndex(n => n.Type == UnitType.Tower);
                                    aptLines[k].Units.Contains.RemoveAt(towerindex);


                                    for (int i = units.Count - 1; i >= 0; i--)
                                    {
                                        if (units[i].Type == UnitType.Tower)
                                            continue;

                                        while (true)
                                        {
                                            var addResult = aptLines[k].Add(units[i]);
                                            if (addResult)
                                            {
                                                added[i]++;
                                                nothingAdded = false;
                                            }
                                            else
                                                break;
                                        }
                                    }

                                }
                                //RhinoApp.WriteLine("Nothing Added, Finish at {0}", k);
                                if(nothingAdded)
                                break;
                            }
                        }
                    }
                }
            }

        }

        public enum UnitType
        {
            Corridor = 0,
            Tower,
            Clearance
        }

        class UnitCollection
        {
            public List<Unit> Contains { get; set; }
            public double Length { get { return GetLengthSum(); } }
            bool sorted = false;
            public UnitCollection()
            {
                Contains = new List<Unit>();
            }

            public List<UnitType> GetTypes()
            {
                //SandwichClearanceAppropriately();
                return Contains.Select(n => n.Type).ToList();
            }

            double GetLengthSum()
            {
                return Contains.Sum(n => n.Length);
            }

            public void Add(Unit unit)
            {
                int insertIndex = Contains.Count - 1;

                if (insertIndex == -1)
                {
                    Contains.Add(unit);
                    return;
                }

                for (int i = 0; i < Contains.Count; i++)
                {
                    if (Contains[i].Type == unit.Type)
                    {
                        insertIndex = i;
                        break;
                    }

                    if (i == Contains.Count - 1)
                    {
                        Contains.Add(unit);
                        return;
                    }
                }
                Contains.Insert(insertIndex, unit);
            }

            public void Remove(Unit unit)
            {
                Contains.Remove(unit);
            }

            public List<double> Positions()
            {
                SandwichClearanceAppropriately();
                List<double> positions = new List<double>();
                positions.Add(0);
                for (int i = 0; i < Contains.Count; i++)
                {
                    double position = Contains.Take(i + 1).Sum(n => n.Length);
                    positions.Add(position);
                }
                //positions.Add(Length);
                return positions;
            }

            public List<bool> Clearances()
            {
                SandwichClearanceAppropriately();
                return Contains.Select(n => n.Type == UnitType.Clearance ? true : false).ToList();
            }

            void SandwichClearanceAppropriately()
            {
                if (sorted)
                    return;

                sorted = true;

                Contains.Sort((Unit a, Unit b) => ((int)a.Type).CompareTo((int)b.Type));

                //패티 숫자를 구한다. add 하면서 자동으로 넣어준 패티들.
                int pattyCount = Contains.Where(n => n.Type == UnitType.Clearance).Count();

                //번 길이!
                double corridorBunLength = Contains.Where(n => n.Type == UnitType.Corridor).Sum(n => n.Length);
                double towerBunLength = Contains.Where(n => n.Type == UnitType.Tower).Sum(n => n.Length);

                //각 번의 요소 count, 나중에 끼워넣을 위치 구하기 위함
                int corridorCount = Contains.Where(n => n.Type == UnitType.Corridor).Count();
                int towerCount = Contains.Where(n => n.Type == UnitType.Tower).Count();

                //번 별로 필요한 패티 숫자
                int corridorBurgerPattyCount = (int)Math.Floor((corridorBunLength / 70000));
                int towerBurgerPattyCount = (int)Math.Floor((towerBunLength / 70000));



                //필요 패티와 전체 패티 숫자 비교, 맞춤
                //if (corridorBurgerPattyCount + towerBurgerPattyCount != pattyCount)
                //{
                //    while (corridorBurgerPattyCount + towerBurgerPattyCount > pattyCount)
                //    {
                //        pattyCount++;
                //        Add(new Unit(0, 5000, UnitType.Clearance));
                //    }

                //    while (corridorBurgerPattyCount + towerBurgerPattyCount < pattyCount)
                //    {
                //        pattyCount--;
                //        if (Contains.Last().Type == UnitType.Clearance)
                //            Contains.RemoveAt(Contains.Count - 1);

                //    }
                //}

                //1.번이 두종류면 사이에 패티 위치시킴 

                if (corridorCount != 0 && towerCount != 0 && Length >= 70000)
                {
                    int[] corr = GetStartEnd(UnitType.Corridor);
                    Contains.Insert(corr[1] + 1, new Unit(0, 5000, UnitType.Clearance));
                    Contains.RemoveAt(Contains.Count - 1);
                }

                //corridor 번의 시작/끝
                int[] corridorIndex = GetStartEnd(UnitType.Corridor);

                //번이 들어갈 간격
                double offset = (double)corridorCount / (double)(corridorBurgerPattyCount + 1);
                
                //패티 개수만큼 돌며 패티 삽입
                for (int i = 0; i < corridorBurgerPattyCount; i++)
                {
                    //시작 인덱스 + offset + 누적추가분
                    int index = corridorIndex[0] + (int)Math.Ceiling(offset) * (i + 1) + i;
                    //끝 인덱스 + 누적 추가분
                    if (index >= corridorIndex[1] + 1 + i)
                        index = corridorIndex[1] + 1 + i;
                    Contains.Insert(index, new Unit(0, 5000, UnitType.Clearance));
                    Contains.RemoveAt(Contains.Count - 1);

                }

                int[] towerIndex = GetStartEnd(UnitType.Tower);
                offset =  (double)towerCount / (double)(towerBurgerPattyCount + 1);
                offset = Math.Ceiling(offset);

                if (offset % 2 != 0)
                    offset++;
                for (int i = 0; i < towerBurgerPattyCount; i++)
                {
                    //시작 인덱스 + offset + 누적추가분
                    int index = towerIndex[0] + (int)offset * (i + 1) + i;
                    //끝 인덱스 + 1 + 누적 추가분
                    if (index >= towerIndex[1] + 1 + i)
                        index = towerIndex[1] + 1 + i;

                    if (index == Contains.Count)
                        break;

                    Contains.Insert(index, new Unit(0, 5000, UnitType.Clearance));
                    Contains.RemoveAt(Contains.Count - 1);
                }



            }

            int[] GetStartEnd(UnitType type)
            {
                int[] indexes = new int[2] { -1, -1 };

                for (int i = 0; i < Contains.Count; i++)
                {
                    if (Contains[i].Type == type)
                    {
                        if (indexes[0] == -1)
                            indexes[0] = i;
                        indexes[1] = i;
                    }
                }

                return indexes;
            }
        }

        class ApartLine
        {
            public double TotalLength { get; set; }
            public UnitCollection Units { get; set; }
            public double LeftLength { get { return GetLeftLength(); } }
            double tempClearancePredict = 0;
            public ApartLine(double length)
            {
                TotalLength = length;
                Units = new UnitCollection();
            }

            double GetLeftLength()
            {
                return
                  TotalLength - Units.Length;
            }

            public bool Add(Unit unit)
            {
                
                if (LeftLength >= unit.Length)
                {
                    Units.Add(unit);

                    if (ClearancePredict() != tempClearancePredict && unit.Type != UnitType.Clearance)
                    {
                        tempClearancePredict = ClearancePredict();
                        Unit clearance = new Unit(0, 5000, UnitType.Clearance);
                        Units.Add(clearance);

                        if (LeftLength < 0)
                        {
                            Units.Remove(unit);
                            Units.Remove(clearance);
                            return false;
                        }
                    }

               
                    return true;
                }
                else
                {
                    return false;
                }
            }

            double ClearancePredict()
            {
                double tempLength = TotalLength - LeftLength;
                double div = tempLength / 70000;
                return Math.Floor(div);
            }
        }

        class Unit
        {
            public double Length { get; set; }
            public double Area { get; set; }
            public UnitType Type { get; set; }

            public Unit(double area, double length, UnitType type)
            {
                Length = length;
                Area = area;
                Type = type;
            }
        }




        class HouseholdGenerator
        {
           
            Vector3d XDirection;
            Vector3d YDirection;
            double Width;
            double Length;
            int householdSizetype = 0;
            Curve baseCurve;
            double coreWidth;
            double coreDepth;



            public void Initialize(Curve baseCurve)
            {
                this.baseCurve = baseCurve;
                XDirection = -baseCurve.TangentAtStart;
                YDirection = new Vector3d(XDirection);
                YDirection.Rotate(Math.PI / 2, Vector3d.ZAxis);
                Length = baseCurve.GetLength();
            }

            public HouseholdGenerator(double width, double coreWidth, double coreDepth)
            {
                Width = width;
                this.coreWidth = coreWidth;
                this.coreDepth = coreDepth;
            }

            public Household Generate(UnitType type,int buildingnum, int houseindex)
            {
                Household temp = new Household();
                temp.XDirection = XDirection;
                temp.YDirection = YDirection;
                temp.XLengthA = Length;

                switch (type)
                {
                    case UnitType.Corridor:
                        temp.isCorridorType = true;
                        temp.YLengthA = Width - Consts.corridorWidth;
                        temp.Origin = baseCurve.PointAtEnd + temp.YDirection * (Width / 2 - Consts.corridorWidth);
                        temp.WallFactor = new List<double>(new double[] { 1, 0.5, 1, 0.5 });
                        temp.EntrancePoint = baseCurve.PointAtNormalizedLength(0.5) - temp.YDirection * Consts.corridorWidth;
                        temp.CorridorArea = Consts.corridorWidth * Length;
                        temp.indexer = new int[] { buildingnum, houseindex };
                        break;
                    case UnitType.Tower:
                        temp.isCorridorType = false;
                        temp.XLengthB = coreWidth / 2;
                        temp.YLengthA = Width;
                        temp.YLengthB = coreDepth;
                        temp.WallFactor = new List<double>(new double[] { 1, 1, 0.5, 1, 0.5, 1 });
                        //짝수인경우 : 홀수인경우
                        var p = (houseindex % 2 == 0) ? baseCurve.PointAtEnd : baseCurve.PointAtStart;
                        temp.XDirection *= (houseindex % 2 == 0) ? 1 : -1;
                        p += temp.YDirection * Width / 2;
                        p -= temp.YDirection * temp.YLengthB;
                        p += temp.XDirection * coreWidth / 2;
                        temp.Origin = p;
                        temp.CorridorArea = 0;
                        temp.EntrancePoint = temp.Origin - temp.YDirection * temp.YLengthB / 2;
                        temp.indexer = new int[] { buildingnum, houseindex };

                        break;

                    default:
                        temp.XLengthA = 0;
                        break;
                }

                return temp;
            }
        }

        #endregion
    }
}
