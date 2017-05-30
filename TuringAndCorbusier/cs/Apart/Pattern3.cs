using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rhino.Geometry;
using System.Collections;
using Rhino.Collections;

namespace TuringAndCorbusier
{
    class AG3 : ApartmentGeneratorBase
    {
        public override Apartment generator(Plot plot, ParameterSet parameterSet, Target target)
        {
            randomCoreType = GetRandomCoreType();


            double pilotiHeight = Consts.PilotiHeight;
            //#######################################################################################################################
            if (parameterSet.using1F)
            {
                randomCoreType = parameterSet.fixedCoreType;
                pilotiHeight = 0;
            }
            //#######################################################################################################################

            ///////////////////////////////////////////////
            //////////  common initial settings  //////////
            ///////////////////////////////////////////////

            //입력"값" 부분
            double[] parameters = parameterSet.Parameters;
            double storiesHigh = Math.Max((int)parameters[0], (int)parameters[1]);
            //double storiesLow = Math.Min((int)parameters[0], (int)parameters[1]);
            double width = parameters[2];
            double angleRadian = parameters[3];
            double moveFactor = parameters[4];
            Regulation regulationHigh = new Regulation(storiesHigh);
            //Regulation regulationLow = new Regulation(storiesLow);
            List<double> ratio = target.Ratio;
            List<double> area = target.Area.Select(n => n * 1000 * 1000).ToList();
            double areaLimit = Consts.AreaLimit;
            BuildingType buildingType = regulationHigh.BuildingType;
            List<double> areaLength = new List<double>();
            for (int i = 0; i < area.Count; i++)
            {
                if (area[i] < areaLimit)
                    areaLength.Add(area[i] / width);
                else
                    areaLength.Add(area[i] / width);
            }

            //#######################################################################################################################
            if (parameterSet.using1F && !parameterSet.setback)
            {
                if (regulationHigh.byLightingCurve(plot, angleRadian).Length == 0 || regulationHigh.fromNorthCurve(plot).Length == 0)
                {
                    return null;
                }

                regulationHigh = new Regulation(storiesHigh, true);
                //regulationLow = new Regulation(storiesHigh, storiesLow, true);
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

            ///////////////////////////
            //sort lists before start//
            RhinoList<double> ratioRL = new RhinoList<double>(ratio);
            ratioRL.Sort(area.ToArray());
            ratio = ratioRL.ToList();
            area.Sort();
            ///////////////////////////

            //입력 "대지경계" 부분
            Curve boundary = CommonFunc.adjustOrientation(plot.SimplifiedBoundary);

            /////////////////////////////////////////
            //////////  maximum rectangle  //////////
            /////////////////////////////////////////

            bool valid;
            Rectangle3d outlineRect = maxInnerRect(boundary, regulationHigh, plot, width, randomCoreType, out valid);
            if (!valid)
            {
                isvalid = false;
                return new Apartment(plot);
            }

            //create outline curve
            Curve outlineCurve = outlineRect.ToNurbsCurve();
            if ((int)outlineCurve.ClosedCurveOrientation(new Vector3d(0, 0, 1)) == -1)
            {
                outlineCurve.Reverse();
            }

            //create centerLine and inner line
            Curve centerLineCurve = outlineCurve.Offset(Plane.WorldXY, width / 2, 1, CurveOffsetCornerStyle.Sharp)[0];
            Curve inlineCurve = outlineCurve.Offset(Plane.WorldXY, width, 1, CurveOffsetCornerStyle.Sharp)[0];

            //calculate typeNum and created list of unallocated houses
            List<int> typeNum = numberOfHousesForEachType(areaLength, ratio, centerLineCurve);
            List<int> unallocated = new List<int>();
            for (int i = 0; i < typeNum.Count; i++)
            {
                for (int j = 0; j < typeNum[i]; j++)
                {
                    unallocated.Add(i);
                }
            }
            unallocated.Reverse();

            //initiate rectangle

            Polyline centerLinePolyline;
            centerLineCurve.TryGetPolyline(out centerLinePolyline);
            Curve[] lines = centerLineCurve.DuplicateSegments();
            int clockwise = 1;
            if (lines[0].GetLength() > lines[1].GetLength())
            {
                Array.Reverse(lines);
                centerLineCurve.Reverse();
                centerLinePolyline.Reverse();
                clockwise = -clockwise;

                for (int i = 0; i < lines.Length; i++)
                {
                    lines[i].Reverse();
                }
            }

            ////////////////////////////////////////
            //////////  apt distribution  //////////
            ////////////////////////////////////////

            //find parameters of end points
            List<int> targetAreaIndices = new List<int>();
            List<double> parametersOnCurve = GetHouseEndParameters(unallocated, areaLength, centerLineCurve, width, out targetAreaIndices);

            ///////////////////////////////
            //////////  outputs  //////////
            ///////////////////////////////

            //set right coreType
            bool isSatisfingWW = regulationHigh.DistanceLL + 2 * randomCoreType.GetWidth() + width < Math.Min(lines[0].GetLength(), lines[1].GetLength());
            bool isSquareCoreAvailable = width*2 +regulationHigh.DistanceLW + CoreType.Folded.GetDepth() < Math.Min(lines[0].GetLength(), lines[1].GetLength());
            if (!isSquareCoreAvailable)
                parameterSet.fixedCoreType = randomCoreType = CoreType.CourtShortEdge;
            else
                parameterSet.fixedCoreType = randomCoreType = CoreType.Folded;
            //Draw cores and households
            List<List<Core>> cores = MakeCores(parameterSet, inlineCurve, isSatisfingWW);
            List<List<List<Household>>> households = MakeHouseholds(parameterSet, lines, centerLinePolyline, parametersOnCurve, targetAreaIndices, area.Count);
           
            //복도면적..?
            double eachfloorCorridorArea = inlineCurve.GetLength() * Consts.corridorWidth - (Consts.corridorWidth * Consts.corridorWidth) * 4;
            double corridorAreaSum = eachfloorCorridorArea * storiesHigh;

            foreach (var hhh in households)
            {
                double tempFloorAreaSum = 0;
                foreach (var hh in hhh)
                    foreach (var h in hh)
                        tempFloorAreaSum += h.ExclusiveArea;

                foreach (var hh in hhh)
                    foreach (var h in hh)
                    {
                        double areaRate = h.ExclusiveArea / tempFloorAreaSum;
                        h.CorridorArea = eachfloorCorridorArea * areaRate;
                    }
            }

            //################################################################################################
            if (parameterSet.setback)
            {
                //최상층 외곽선 후퇴옵션 사용하는 경우 최상층 조정...?
                //용적 제대로 나올지 미지수

                List<Household> topHouses = new List<Household>();
                households.Last().ToList().ForEach(n => topHouses.AddRange(n));

                List<Core> topCores = cores.Last().ToList();

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
                        foreach (var hh in households.Last())
                        {
                            List<int> toRemove = new List<int>();
                            foreach (var h in hh)
                            {
                                Curve outline = h.GetOutline();
                                Curve coreoutline = cores[cores.Count - 2][i].DrawOutline();
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
                        cores.Last().RemoveAt(i);
                        //cpss[cpss.Count - 2].RemoveAt(i);
                    }
                }

                Curve[] northHigh = regulationHigh.fromNorthCurve(plot);
                Curve[] surroundingsHigh = regulationHigh.fromSurroundingsCurve(plot);
                Curve[] lightingHigh = regulationHigh.byLightingCurve(plot, angleRadian);
                Curve[] wholeRegulationHigh = CommonFunc.JoinRegulations(northHigh, surroundingsHigh, lightingHigh);

                if (wholeRegulationHigh.Length == 1)
                {
                    foreach (var hh in households.Last())
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

                    if (households.Last().Count == 0)
                        households.RemoveAt(households.Count - 1);
                }
            }
            //################################################################################################


            //building outlines
            Polyline outlinePolyline;
            outlineCurve.TryGetPolyline(out outlinePolyline);
            List<List<Curve>> buildingOutlines = buildingOutlineMakerAG3(outlinePolyline, width);

            ////////////////////////////////////////////////////////////////////////////////////////
            #region parking
            //parking lot
            ParkingModule pm = new ParkingModule();

            //1.setups for parking
            #region setup
            //parking curves setting
            Curve parkingLine = inlineCurve.Offset(Plane.WorldXY, cores[0][0].Depth / 2, 1, CurveOffsetCornerStyle.Sharp)[0];
            Curve[] parkingLineSegments = parkingLine.DuplicateSegments();
            Curve[] shortSegments = parkingLineSegments.OrderBy(n => n.GetLength()).Take(2).ToArray();
            //세배 길이의 라인으로
            List<Line> toLine = shortSegments.Select(n => new Line(n.PointAtStart - n.TangentAtStart * n.GetLength(), n.PointAtEnd + n.TangentAtStart * n.GetLength())).ToList();
            //둘중 하나 역으로
            toLine[1] = new Line(toLine[1].To, toLine[1].From);
            List<Curve> parkingCurves = toLine.Select(n => n.ToNurbsCurve() as Curve).ToList();

            Vector3d setBack = parkingCurves.Count > 0 ? parkingCurves[0].TangentAtStart : Vector3d.Zero;
            setBack.Rotate(-Math.PI / 2, Vector3d.ZAxis);
            parkingCurves.ForEach(n => n.Translate(setBack * width / 2));


            //obstacles setting

            List<Curve> obstacles = cores[0].Select(n => n.DrawOutline(width)).ToList();
            for (int i = 0; i < obstacles.Count; i++)
            {
                obstacles[i].Translate(cores[0][i].YDirection * (cores[0][i].Depth - width) / 2);
            }

            //distance setting
            double lineDistance = toLine[0].From.DistanceTo(toLine[1].From);

            //coredepth
            double coreDepth = randomCoreType.GetDepth();

            #endregion
            //p3 parking setting
            pm.ParkingLines = parkingCurves;
            pm.Obstacles = obstacles;
            pm.Boundary = plot.Boundary;
            pm.Distance = lineDistance;
            pm.CoreDepth = coreDepth;
            pm.AddBack = true;

            ParkingLotOnEarth parkingLotOnEarth = pm.GetParking();
            ParkingLotUnderGround parkingLotUnderGround = new ParkingLotUnderGround();
            #endregion


            List<Curve> aptLines = new List<Curve>();
            aptLines.Add(centerLineCurve);

            Apartment result = new Apartment(this.GetAGType, plot, buildingType, parameterSet, target, cores, households, parkingLotOnEarth, parkingLotUnderGround, buildingOutlines, aptLines);

            //#######################################################################################################################
            if (parameterSet.using1F || parameterSet.setback)
            {

            }
            //#######################################################################################################################

            else
            {
                Finalizer finalizer = new Finalizer(result);
                result = finalizer.Finilize();
            }

            return result;


        }

        #region New Apartment Generating Methods
        private List<List<Core>> MakeCores(ParameterSet parameterSet, Curve inlineCurve, bool isSatisfingWW)
        {
            //wwregbool 잠시 비활성화
            isSatisfingWW = false;
            double pilotiHeight = Consts.PilotiHeight;
            
            //#######################################################################################################################
            if (parameterSet.using1F)
            {
                randomCoreType = parameterSet.fixedCoreType;
                pilotiHeight = 0;
            }
            //#######################################################################################################################
            
            //initial settings
            Polyline inlinePolyline;
            inlineCurve.TryGetPolyline(out inlinePolyline);
            Curve[] lines = inlineCurve.DuplicateSegments();

            //reverse Curve orientation & set index0 shortEdge
            if (lines[0].GetLength() > lines[1].GetLength())
            {
                lines.Reverse();
                for (int i = 0; i < lines.Length; i++)
                {
                    lines[i].Reverse();
                }
            }

            Curve minEdge = lines[0];
            double[] parameters = parameterSet.Parameters;
            double storiesHigh = Math.Max((int)parameters[0], (int)parameters[1]);
            double width = parameters[2];
            double coreWidth = randomCoreType.GetWidth();
            double coreDepth = randomCoreType.GetDepth();
            bool isShortEdgeOver16 = false;
            if (randomCoreType == CoreType.CourtShortEdge)
            {
                coreWidth = minEdge.GetLength();
                isSatisfingWW = false;
                if (minEdge.GetLength() > 16000)
                {
                    isShortEdgeOver16 = true;
                    coreWidth = CoreType.CourtShortEdge.GetWidth();
                }
            }


            Vector3d courtX = new Vector3d(lines[0].PointAtEnd - lines[0].PointAtStart);
            Vector3d courtY = new Vector3d(lines[3].PointAtStart - lines[3].PointAtEnd);
            courtX.Unitize();
            courtY.Unitize();

            List<Point3d> coreOrigins = new List<Point3d>();
            List<Vector3d> coreXVectors = new List<Vector3d>();
            List<Vector3d> coreYVectors = new List<Vector3d>();

            //Draw groundFloor cores
            //1
            if(!isShortEdgeOver16)
                coreOrigins.Add(lines[0].PointAtStart);
            else
                coreOrigins.Add((lines[0].PointAtStart + lines[0].PointAtEnd) / 2 - courtX * coreWidth/2);
            coreXVectors.Add(Vector3d.Multiply(courtX, 1));
            coreYVectors.Add(Vector3d.Multiply(courtY, 1));
            if (isSatisfingWW)
            {
                //2
                coreOrigins.Add(lines[1].PointAtStart);
                coreXVectors.Add(Vector3d.Multiply(courtX, -1));
                coreYVectors.Add(Vector3d.Multiply(courtY, 1));
            }
            //3
            if (!isShortEdgeOver16)
                coreOrigins.Add(lines[2].PointAtStart);
            else
                coreOrigins.Add((lines[2].PointAtStart + lines[2].PointAtEnd) / 2 + courtX * coreWidth / 2);
            coreXVectors.Add(Vector3d.Multiply(courtX, -1));
            coreYVectors.Add(Vector3d.Multiply(courtY, -1));
            if (isSatisfingWW)
            {
                //4
                coreOrigins.Add(lines[3].PointAtStart);
                coreXVectors.Add(Vector3d.Multiply(courtX, 1));
                coreYVectors.Add(Vector3d.Multiply(courtY, -1));
            }

                //stack
                List<List<Core>> outputCores = new List<List<Core>>();

            for (int i = 0; i < storiesHigh + 2; i++)
            {
                //1층 사용시 필로티코어 만들지 않음.
                if (parameterSet.using1F && i == 0)
                    continue;

                double tempStoryHeight = (i == 0) ? 0 : pilotiHeight + Consts.FloorHeight * (i - 1);
                List<Core> currentFloorCores = new List<Core>();

                for (int j = 0; j < coreOrigins.Count; j++)
                {
                    Core oneCore = new Core(coreOrigins[j], coreXVectors[j], coreYVectors[j], randomCoreType, storiesHigh, coreDepth);
                    oneCore.Origin = oneCore.Origin + Vector3d.ZAxis * tempStoryHeight;
                    oneCore.Stories = i;
                    oneCore.Width = coreWidth;
                    oneCore.Depth = coreDepth;

                    //임시 면적
                    oneCore.Area = coreWidth * coreDepth;

                    currentFloorCores.Add(oneCore);
                }

                outputCores.Add(currentFloorCores);
            }

            return outputCores;
        }

        private List<List<List<Household>>> MakeHouseholds(ParameterSet parameterSet, Curve[] lines, Polyline centerLine, List<double> mappedVals, List<int> targetAreaType, int typeN)
        {
            double pilotiHeight = Consts.PilotiHeight;
            //#######################################################################################################################
            if (parameterSet.using1F)
            {
                randomCoreType = parameterSet.fixedCoreType;
                pilotiHeight = 0;
            }
            //#######################################################################################################################

            //initial settings
            double[] parameters = parameterSet.Parameters;
            double storiesHigh = Math.Max((int)parameters[0], (int)parameters[1]);
            double width = parameters[2];
            double coreWidth = randomCoreType.GetWidth();
            double coreDepth = randomCoreType.GetDepth();
            bool isCorner = false;

            bool isShortEdgeCore = randomCoreType == CoreType.CourtShortEdge;
            bool isOnShortEdge = false;
            if (lines[0].GetLength() < lines[1].GetLength())
                isOnShortEdge = true;

            List<double> minCornerCheck = new List<double>();
            int entranceLength = 2000;
            int entranceCheck = 0;

            List<List<List<Household>>> output = new List<List<List<Household>>>();
            List<Household> outputS = new List<Household>();

            for (int i = 0; i < mappedVals.Count; i++)
            {
                Point3d homeOriH;
                Vector3d homeVecXH;
                Vector3d homeVecYH;
                double xaH;
                double xbH;
                double yaH;
                double ybH;
                List<Line> windowsH = new List<Line>();
                List<Line> moveableH = new List<Line>();
                Point3d ent = new Point3d();
                List<double> wallFactor;
                //int targetAreaTypeH = new List<int>();
                //double exclusiveAreaH = new List<double>();
                if ((int)(mappedVals[i] - 0.01) == (int)(mappedVals[(i + 1) % mappedVals.Count] - 0.01)) // if not corner
                {
                    double avrg = (mappedVals[i] + mappedVals[(i + 1) % mappedVals.Count]) / 2.0;
                    Point3d midOri = centerLine.PointAt(avrg);
                    Vector3d midVec = centerLine.TangentAt(avrg);
                    minCornerCheck.Add(Consts.corridorWidth + 0.1);
                    Vector3d verticalVec = midVec;
                    verticalVec.Rotate(-Math.PI / 2, new Vector3d(0, 0, 1));
                    midOri.Transform(Transform.Translation(Vector3d.Multiply(verticalVec, width / 2)));

                    //
                    Random r = new Random(i);
                    //int randInt = r.Next(10) % 2;
                    int randInt = 1;
                    List<Point3d> selOri = new List<Point3d>();
                    selOri.Add(centerLine.PointAt(mappedVals[i]));
                    selOri.Add(centerLine.PointAt(mappedVals[(i + 1) % mappedVals.Count]));
                    xaH = selOri[0].DistanceTo(selOri[1]);
                    xbH = 0;
                    yaH = width;
                    ybH = 0;
                    Point3d realOri = selOri[randInt];
                    realOri.Transform(Transform.Translation(Vector3d.Multiply(verticalVec, width / 2)));
                    homeOriH = realOri;
                    midVec.Rotate(Math.PI * randInt, new Vector3d(0, 0, 1));
                    //

                    homeVecXH = Vector3d.Multiply(1, midVec);
                    homeVecYH = verticalVec;

                    //windows
                    Point3d winPt1 = homeOriH;
                    winPt1.Transform(Transform.Translation(Vector3d.Multiply(homeVecYH, ybH)));
                    winPt1.Transform(Transform.Translation(Vector3d.Multiply(homeVecXH, xaH - xbH)));
                    Point3d winPt2 = winPt1;
                    winPt2.Transform(Transform.Translation(Vector3d.Multiply(homeVecXH, -xaH)));
                    Point3d winPt3 = winPt1;
                    winPt3.Transform(Transform.Translation(Vector3d.Multiply(homeVecYH, -yaH)));
                    Point3d winPt4 = winPt3;
                    winPt4.Transform(Transform.Translation(Vector3d.Multiply(homeVecXH, -xaH)));
                    ////////그런데, 복도 방향도 채광창 방향이라고 할 수 있을까?
                    if(isShortEdgeCore && !isOnShortEdge)
                    windowsH.Add(new Line(winPt1, winPt2));
                    ////////
                    windowsH.Add(new Line(winPt3, winPt4));
                    moveableH.Add(new Line(winPt3, winPt4));

                    //entrance points
                    ent = new Point3d(homeOriH);
                    //***혹시 여기에서도 레퍼런스된 점을 그대로 옮겨서 깽판나는지 확인 필요
                    ent.Transform(Transform.Translation(Vector3d.Multiply(homeVecXH, -xbH / 2)));

                    //this is edge type
                    isCorner = false;

                    wallFactor = new List<double>(new double[] { 1, 0.5, 1, 0.5 });
                }
                else //if is corner
                {
                    if (isOnShortEdge)
                        isOnShortEdge = false;
                    else
                        isOnShortEdge = true;

                    Point3d checkP = lines[(int)(mappedVals[i] - 0.01)].PointAtEnd;
                    Point3d sOri = centerLine.PointAt(mappedVals[i]);
                    Point3d eOri = centerLine.PointAt(mappedVals[(mappedVals.Count + i + 1) % mappedVals.Count]);

                    Vector3d v1 = new Vector3d(sOri - checkP);
                    Vector3d v2 = new Vector3d(eOri - checkP);
                    double l1 = v1.Length;
                    double l2 = v2.Length;
                    minCornerCheck.Add(Math.Max(l1, l2));
                    v1.Unitize();
                    v2.Unitize();
                    checkP.Transform(Transform.Translation(v1 * width / 2 + v2 * width / 2));
                    homeOriH = checkP;
                    if (l1 > l2)
                    {
                        v1.Reverse();
                        homeVecXH = v1;
                        homeVecYH = v2;
                    }
                    else
                    {
                        v2.Reverse();
                        homeVecXH = v2;
                        homeVecYH = v1;
                    }
                    xaH = Math.Max(l1, l2) + width / 2;
                    xbH = Math.Max(l1, l2) - width / 2;
                    yaH = Math.Min(l1, l2) + width / 2;
                    ybH = Math.Min(l1, l2) - width / 2;

                    if (Math.Max(l1, l2) < width / 2 + entranceLength)
                    {
                        //entranceCheck = 1;
                    }

                    if (Math.Abs(ybH) < 10)
                        ybH = 0;

                    //windows
                    Point3d winPt1 = homeOriH + homeVecYH * ybH + homeVecXH * (xaH - xbH);
                    Point3d winPt2 = winPt1 - homeVecYH * yaH;
                    Point3d winPt3 = winPt2;
                    Point3d winPt4 = winPt3 + homeVecXH * -xaH;

                    windowsH.Add(new Line(winPt1, winPt2));
                    windowsH.Add(new Line(winPt3, winPt4));

                    //moveables == windows
                    moveableH = new List<Line>(windowsH);

                    //entrance points
                    ent = new Point3d(homeOriH);
                    ent.Transform(Transform.Translation(Vector3d.Multiply(homeVecXH, -xbH / 2)));

                    //this is corner type
                    isCorner = true;

                    if (ybH > 0)
                        wallFactor = new List<double>(new double[] { 1, 0.5, 1, 1, 0.5, 1 });
                    else if (ybH == 0)
                        wallFactor = new List<double>(new double[] { 1, 1, 1, 0.5, 1 });
                    else
                        wallFactor = new List<double>(new double[] { 0, 0.5, 1, 1, 0.5, 1 });
                }
                if (entranceCheck == 0 && minCornerCheck.Min() > Consts.corridorWidth)
                {
                    if (isCorner)
                    {
                        Household tempHP = new Household(homeOriH, homeVecXH, homeVecYH, xaH, xbH, yaH, ybH, targetAreaType[i], exclusiveAreaCalculatorAG3Corner(xaH, xbH, yaH, ybH, targetAreaType[i], Consts.balconyDepth), windowsH, ent, wallFactor);
                        tempHP.MoveableEdge = new List<Line>(moveableH);
                        outputS.Add(tempHP);
                        //cornerProperties[targetAreaType[i]].Add(tempHP);
                    }
                    else
                    {
                        Household tempHP = new Household(homeOriH, homeVecXH, homeVecYH, xaH, xbH, yaH, ybH, targetAreaType[i], exclusiveAreaCalculatorAG3Edge(xaH, xbH, yaH, ybH, targetAreaType[i], Consts.balconyDepth), windowsH, ent, wallFactor);
                        tempHP.MoveableEdge = new List<Line>(moveableH);
                        outputS.Add(tempHP);
                        //edgeProperties[targetAreaType[i]].Add(tempHP);
                    }
                }
            }

            for (int j = 0; j < storiesHigh; j++)
            {
                List<List<Household>> outputB = new List<List<Household>>();
                List<Household> outputSTemp = new List<Household>();
                for (int i = 0; i < outputS.Count; i++)
                {
                    Household hp = outputS[i];
                    Point3d ori = hp.Origin;
                    Point3d ent = hp.EntrancePoint;
                    ori.Transform(Transform.Translation(Vector3d.Multiply(pilotiHeight + Consts.FloorHeight * j, Vector3d.ZAxis)));
                    ent.Transform(Transform.Translation(Vector3d.Multiply(pilotiHeight + Consts.FloorHeight * j, Vector3d.ZAxis)));
                    List<Line> win = new List<Line>(hp.LightingEdge); 

                    Household newTemp = new Household(ori, hp.XDirection, hp.YDirection, hp.XLengthA, hp.XLengthB, hp.YLengthA, hp.YLengthB, hp.HouseholdSizeType, hp.GetExclusiveArea() + hp.GetWallArea(), win, ent, hp.WallFactor);
                    newTemp.MoveableEdge = new List<Line>(hp.MoveableEdge);

                    //라이팅 등 위치 재조정
                    newTemp.MoveLightingAndMoveAble();

                    outputSTemp.Add(newTemp);
                }
                outputB.Add(outputSTemp);
                output.Add(outputB);
            }
            return output;
        }

        #endregion

        #region GA Settings
        ///////////////////////////////////////////
        //////////  GA initial settings  //////////
        ///////////////////////////////////////////
        public bool isvalid = true;
        //Parameter 최소값 ~ 최대값 {storiesHigh, storiesLow, width, angle, moveFactor}
        private double[] minInput = { 3, 3, 7000, 0, 0 };
        //private double[] minInput = { 6, 6, 10500, 0, 0 };
        private double[] maxInput = { TuringAndCorbusierPlugIn.InstanceClass.page1Settings.MaxFloors - 1, TuringAndCorbusierPlugIn.InstanceClass.page1Settings.MaxFloors - 1, 10500, 2 * Math.PI, 1 };

        //Parameter GA최적화 {mutation probability, elite percentage, initial boost, population, generation , fitness value, mutation factor(0에 가까울수록 변동 범위가 넓어짐)
        //private double[] GAparameterset = { 0.8, 0.05, 1, 20, 4, 10, 1 };
        private double[] GAparameterset = { 0.2, 0.03, 1, 25, 1, 3, 1 };

        public override string GetAGType
        {
            get
            {
                return "PT-3";
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
            CoreType[] tempCoreTypes = { CoreType.Folded };
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
        #endregion GA Settings

        ///////////////////////////////////////////////
        //////////  common initial settings  //////////
        ///////////////////////////////////////////////

        private Curve[] plotArrMaker(Curve boundary)
        {
            Polyline plotPolyline;
            boundary.TryGetPolyline(out plotPolyline);

            Line[] plotSegmentsArr = plotPolyline.GetSegments();
            Curve[] plotArr = new Curve[plotPolyline.GetSegments().Length];

            for (int i = 0; i < plotSegmentsArr.Length; i++)
            {
                plotArr[i] = plotSegmentsArr[i].ToNurbsCurve();
            }
            return plotArr;
        }


        #region LightingRegulation
        ///////////////////////////////////////////
        //////////  additional settings  //////////
        ///////////////////////////////////////////

        private Curve byLightingCurve(Plot plot, Regulation regulation, Curve[] plotArr, double angleRadian)
        {
            //법규적용 인접대지경계선(채광창)

            //extend plotArr
            Curve[] plotArrExtended = new Curve[plotArr.Length];
            Array.Copy(plotArr, plotArrExtended, plotArr.Length);

            for (int i = 0; i < plotArrExtended.Length; i++)
            {
                Curve tempCurve = plotArrExtended[i].Extend(CurveEnd.Both, 20000, CurveExtensionStyle.Line);

                plotArrExtended[i] = new LineCurve(tempCurve.PointAt(tempCurve.Domain.T0 - Math.Abs(tempCurve.Domain.T1 - tempCurve.Domain.T0)), tempCurve.PointAt(tempCurve.Domain.T1 + Math.Abs(tempCurve.Domain.T1 - tempCurve.Domain.T0)));
            }
            //

            double[] distanceByLighting = new double[plotArr.Length];
            for (int i = 0; i < plotArr.Length; i++)
            {
                Vector3d tempVector = new Vector3d(plotArr[i].PointAt(plotArr[i].Domain.T1) - plotArr[i].PointAt(plotArr[i].Domain.T0));
                Vector3d baseVector = new Vector3d(Math.Cos(angleRadian), Math.Sin(angleRadian), 0);
                //검증할 필요 있음

                double tempAngle = Math.Acos((tempVector.X * baseVector.X + tempVector.Y * baseVector.Y) / tempVector.Length / baseVector.Length);
                double convertedAngle = Math.PI - (Math.Abs(tempAngle - Math.PI));
                ///검증할 필요 있음
                double offsetDistance = ((plot.PlotType == PlotType.상업지역) ? 0 : regulation.DistanceByLighting) * Math.Cos(convertedAngle) - 0.5 * plot.SimplifiedSurroundings[i];

                if (offsetDistance > 0)
                {
                    distanceByLighting[i] = offsetDistance;
                }
                else
                {
                    distanceByLighting[i] = 0;
                }
                //Vector3d tempVector = new Vector3d(plotArr[i].PointAt(plotArr[i].Domain.T1) - plotArr[i].PointAt(plotArr[i].Domain.T0));
                //Vector3d baseVector = new Vector3d(Math.Cos(angleRadian), Math.Sin(angleRadian), 0);
                //double tempAngle = Math.Acos((tempVector.X * baseVector.X + tempVector.Y * baseVector.Y) / tempVector.Length / baseVector.Length);
                //double convertedAngle = Math.PI - (Math.Abs(tempAngle - Math.PI));
                //double offsetDistance = regulation.DistanceByLighting * Math.Sin(convertedAngle) - 0.5 * plot.SimplifiedSurroundings[i];

                //if (offsetDistance > 0)
                //{
                //    distanceByLighting[i] = offsetDistance;
                //}
                //else
                //{
                //    distanceByLighting[i] = 0;
                //}
            }

            List<Point3d> ptsByLighting = new List<Point3d>();
            for (int i = 0; i < plotArr.Length; i++)
            {
                int j = (i - 1 + plotArr.Length) % plotArr.Length;

                Vector3d iVec = plotArrExtended[i].TangentAtStart;
                Vector3d jVec = plotArrExtended[j].TangentAtStart;

                Vector3d iVert = new Vector3d(iVec);
                iVert.Rotate(-Math.PI / 2, Vector3d.ZAxis);
                iVert.Unitize();
                Vector3d jVert = new Vector3d(jVec);
                jVert.Rotate(-Math.PI / 2, Vector3d.ZAxis);
                jVert.Unitize();

                Point3d pt = plotArr[i].PointAtStart;
                Point3d iP = new Point3d(pt + iVert * distanceByLighting[i]);
                Point3d jP = new Point3d(pt + jVert * distanceByLighting[j]);

                double a, b;
                Line iLine = new Line(iP, iVec);
                Line jLine = new Line(jP, jVec);
                Rhino.Geometry.Intersect.Intersection.LineLine(iLine, jLine, out a, out b);
                ptsByLighting.Add(iLine.PointAt(a));

            }

            ptsByLighting.Add(ptsByLighting[0]);
            Curve byLightingCurve = (new Polyline(ptsByLighting)).ToNurbsCurve();

            return byLightingCurve;
        }
        #endregion LightingRegulation


        #region MaxRectangle
        /////////////////////////////////////////
        //////////  maximum rectangle  //////////
        /////////////////////////////////////////

        private Rectangle3d maxInnerRect(Curve curve, Regulation regulationHigh, Plot plot, double aptWidth, CoreType core, out bool valid)
        {
            Polyline boundary;
            curve.TryGetPolyline(out boundary);

            double solAngle, solRatio, solWidth;
            Point3d solPoint;
            initialParameters(boundary, plot, regulationHigh, aptWidth, core, 6, 7, 7, 6, out solAngle, out solPoint, out solRatio, out solWidth);

            if (solPoint == Point3d.Unset)
            {
                valid = false;
                return rectMaker(solAngle, Point3d.Origin, solRatio, solWidth);
            }

            valid = true;
            int token = 1;
            while (token < 5)
            {
                betterParameters(boundary, plot, regulationHigh, aptWidth, core, solAngle, Math.PI / Math.Pow(2, token), solPoint, Math.Pow(0.8, token), solRatio, 1 + Math.Pow(0.2, token), solWidth, 3, 5, 4, 4, out solAngle, out solPoint, out solRatio, out solWidth);
                token += 1;
            }

            return rectMaker(solAngle, solPoint, solRatio, solWidth);
        }

        private Polyline regulationBoundaryMaker(Curve boundary, Regulation regulationHigh, Plot plot, double angleRadian)
        {
            Curve[] plotArr = plotArrMaker(plot.SimplifiedBoundary);

            //법규 : 대지 안의 공지
            Curve[] surroundingsHigh = regulationHigh.fromSurroundingsCurve(plot);

            //법규 : 일조에 의한 높이제한
            Curve[] northHigh = regulationHigh.fromNorthCurve(plot);

            //법규 : 인접대지경계선(채광창)



            Curve[] lighting1 = regulationHigh.byLightingCurve(plot, angleRadian);
            Curve[] lighting2 = regulationHigh.byLightingCurve(plot, angleRadian + Math.PI / 2);
            //Curve[] lightingHigh = CommonFunc.JoinRegulation(lighting1, lighting2);

            Curve[] lightingHigh = CommonFunc.JoinRegulations(new Curve[] { plot.Boundary }, lighting1, lighting2);
            //일부 조건(대지안의공지, 일조에 의한 높이제한)을 만족하는 경계선
            //모든 조건(대지안의공지, 일조에 의한 높이제한, 인접대지경계선)을 만족하는 경계선
            //Curve partialRegulationHigh = CommonFunc.joinRegulations(surroundingsHigh[0], northHigh[0]);
            Curve[] wholeRegulationHigh = CommonFunc.JoinRegulations(surroundingsHigh, northHigh, lightingHigh);

            if (wholeRegulationHigh.Length == 0)
                return new Polyline();

            Polyline output;
            Curve regulationMax = wholeRegulationHigh.OrderByDescending(n => AreaMassProperties.Compute(n).Area).ToList()[0];
            regulationMax.TryGetPolyline(out output);
            return output;
        }

        private void betterParameters(Polyline boundary, Plot plot, Regulation regulationHigh, double aptWidth, CoreType core, double iniAngle, double angleRange, Point3d iniPoint, double gridFactor, double iniRatio, double ratioFactor, double iniWidth, int angleRes, int gridRes, int ratioRes, int binaryIterNum, out double solAngle, out Point3d solPoint, out double solRatio, out double solWidth)
        {
            //initial settings
            double bestArea = iniWidth * iniWidth / iniRatio;
            solWidth = iniWidth;
            solAngle = iniAngle;
            solRatio = iniRatio;
            solPoint = iniPoint;

            //iteration start
            List<double> angles = searchAngle(boundary, iniAngle, angleRange, angleRes, false);
            for (int i = 0; i < angles.Count; i++)
            {
                Polyline boundaryClone = regulationBoundaryMaker(boundary.ToNurbsCurve(), regulationHigh, plot, -angles[i]);
                if (boundaryClone.Count == 0)
                    continue;
                //Rhino.RhinoDoc.ActiveDoc.Objects.AddCurve(boundaryClone.ToNurbsCurve());
                Point3d pointClone = new Point3d(iniPoint);
                boundaryClone.Transform(Transform.Rotation(angles[i], Point3d.Origin));
                pointClone.Transform(Transform.Rotation(angles[i], Point3d.Origin));
                List<Point3d> points = searchPoint(boundaryClone, pointClone, boundaryClone.BoundingBox.Diagonal.X * gridFactor, boundaryClone.BoundingBox.Diagonal.Y * gridFactor, gridRes);
                for (int j = 0; j < points.Count; j++)
                {
                    double maxHeight, maxWidth;
                    maxDims(points[j], boundaryClone, out maxHeight, out maxWidth);
                    List<double> ratios = searchRatio(iniRatio / ratioFactor, iniRatio * ratioFactor, ratioRes);
                    for (int k = 0; k < ratios.Count; k++)
                    {
                        double minSearchWidth = Math.Sqrt(bestArea * ratios[k]);
                        double maxSearchWidth = Math.Min(maxWidth, maxHeight * ratios[k]);
                        if (minSearchWidth < maxSearchWidth)
                        {
                            double widthTemp = widthFinder(boundaryClone, bestArea, ratios[k], points[j], minSearchWidth, maxSearchWidth, binaryIterNum);

                            if (bestArea < widthTemp * widthTemp / ratios[k] && aptWidth * 2 + Math.Max(CoreType.CourtShortEdge.GetWidth(),regulationHigh.DistanceLL)< Math.Min(widthTemp, widthTemp / ratios[k]))
                            {
                                double t = aptWidth + Consts.corridorWidth;
                                double area = 2 * t * (widthTemp + widthTemp / ratios[k] - 2 * t) + 4 * (core.GetWidth() - Consts.corridorWidth) * (core.GetDepth() - Consts.corridorWidth);
                                if (area / CommonFunc.getArea(plot.Boundary) < TuringAndCorbusierPlugIn.InstanceClass.page1Settings.MaxBuildingCoverage / 100)
                                {

                                    bestArea = widthTemp * widthTemp / ratios[k];
                                    solWidth = widthTemp;
                                    solAngle = angles[i];
                                    solPoint = points[j];
                                    solPoint.Transform(Transform.Rotation(-angles[i], Point3d.Origin));
                                    solRatio = ratios[k];
                                }
                            }
                        }
                    }
                }
            }
        }

        private void initialParameters(Polyline boundary, Plot plot, Regulation regulationHigh, double aptWidth, CoreType core, int angleRes, int gridRes, int ratioRes, int binaryIterNum, out double solAngle, out Point3d solPoint, out double solRatio, out double solWidth)
        {
            //initial settings
            double bestArea = 0;
            solWidth = 0;
            solAngle = 0;
            solRatio = 0;
            solPoint = Point3d.Unset;

            //iteration start
            List<double> angles = searchAngle(boundary, 0, Math.PI * 2, angleRes, true);
            for (int i = 0; i < angles.Count; i++)
            {
                Polyline boundaryClone = regulationBoundaryMaker(boundary.ToNurbsCurve(), regulationHigh, plot, -angles[i]);
                if (boundaryClone.Count == 0)
                    continue;
                boundaryClone.Transform(Transform.Rotation(angles[i], Point3d.Origin));
                List<Point3d> points = searchPoint(boundaryClone, boundaryClone.BoundingBox.Center, boundaryClone.BoundingBox.Diagonal.X, boundaryClone.BoundingBox.Diagonal.Y, gridRes);
                for (int j = 0; j < points.Count; j++)
                {
                    //Rhino.RhinoDoc.ActiveDoc.Objects.AddCurve(new Circle(points[j], 1000).ToNurbsCurve());
                    double maxHeight, maxWidth;
                    maxDims(points[j], boundaryClone, out maxHeight, out maxWidth);
                    List<double> ratios = searchRatio(Math.Max(1, bestArea / Math.Pow(maxHeight, 2)), Math.Min(10, Math.Pow(maxWidth, 2) / bestArea), ratioRes);
                    for (int k = 0; k < ratios.Count; k++)
                    {
                        double minSearchWidth = Math.Sqrt(bestArea * ratios[k]);
                        double maxSearchWidth = Math.Min(maxWidth, maxHeight * ratios[k]);
                        if (minSearchWidth < maxSearchWidth)
                        {
                            double widthTemp = widthFinder(boundaryClone, bestArea, ratios[k], points[j], minSearchWidth, maxSearchWidth, binaryIterNum);
                            //Rhino.RhinoDoc.ActiveDoc.Objectrs.AddCurve(rectMaker(angles[i], points[j], ratios[k], widthTemp).ToNurbsCurve());
                            if (bestArea < widthTemp * widthTemp / ratios[k] && aptWidth * 2 + Math.Max(CoreType.CourtShortEdge.GetWidth(), regulationHigh.DistanceLL) < Math.Min(widthTemp, widthTemp / ratios[k]))
                            //if(true)
                            {
                                double t = aptWidth + Consts.corridorWidth;
                                double area = 2 * t * (widthTemp + widthTemp / ratios[k] - 2 * t) + 4 * (core.GetWidth() - Consts.corridorWidth) * (core.GetDepth() - Consts.corridorWidth);
                                if (area / CommonFunc.getArea(plot.Boundary) < TuringAndCorbusierPlugIn.InstanceClass.page1Settings.MaxBuildingCoverage / 100)
                                {
                                    bestArea = widthTemp * widthTemp / ratios[k];
                                    solWidth = widthTemp;
                                    solAngle = angles[i];
                                    solPoint = points[j];
                                    solPoint.Transform(Transform.Rotation(-angles[i], Point3d.Origin));
                                    solRatio = ratios[k];
                                }

                            }

                        }
                    }
                }
            }
        }

        private List<double> searchAngle(Polyline boundary, double midAngle, double angleRange, int angleRes, bool edgeAlign)
        {
            //initial settings
            List<double> angles = new List<double>();

            for (int i = -angleRes; i < angleRes + 1; i++)
            {
                angles.Add(midAngle + angleRange / (angleRes * 2) * i);
            }

            if (edgeAlign)
            {
                if ((int)boundary.ToNurbsCurve().ClosedCurveOrientation(new Vector3d(0, 0, 1)) == -1)
                {
                    boundary.Reverse();
                }
                List<Line> edges = boundary.GetSegments().ToList();
                RhinoList<Line> outSegRL = new RhinoList<Line>(edges);
                List<double> outlineSegLength = edges.Select(n => n.Length).ToList();
                outSegRL.Sort(outlineSegLength.ToArray());
                edges = outSegRL.ToList();
                edges.Reverse();
                for (int i = 0; i < Math.Min(20, edges.Count); i++)
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
                if (lines[i] != null)
                {
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

            }
            return mids;
        }

        private void maxDims(Point3d point, Polyline boundary, out double maxHeight, out double maxWidth)
        {
            //initial setting
            double alpha = 1;
            var box = boundary.BoundingBox;

            //get max height and max width
            Line centerLineY = new Line(new Point3d(point.X, box.Min.Y - alpha, 0), new Point3d(point.X, box.Max.Y + alpha, 0));
            Line centerLineX = new Line(new Point3d(box.Min.X - alpha, point.Y, 0), new Point3d(box.Max.X + alpha, point.Y, 0));
            List<Point3d> widthP = new List<Point3d>(intersectionPoints(centerLineX, boundary));
            List<Point3d> heightP = new List<Point3d>(intersectionPoints(centerLineY, boundary));
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
            if (widthV.Count == 0)
                maxWidth = 0;
            else
                maxWidth = widthV.Min() * 2;
            if (heightV.Count == 0)
                maxHeight = 0;
            else
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
            {
                return no;
            }
            else
            {
                return points;
            }

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
                {
                    low = mid;
                }
                else
                {
                    high = mid;
                }
            }

            if (isRectangleInside(point, mid, ratio, boundary))
                return mid;
            else
                return 0;
        }

        private bool isRectangleInside(Point3d midPoint, double width, double ratio, Polyline boundary)
        {
            Rectangle3d rect = new Rectangle3d(Plane.WorldXY, new Point3d(midPoint.X - width / 2, midPoint.Y - width / 2 / ratio, 0), new Point3d(midPoint.X + width / 2, midPoint.Y + width / 2 / ratio, 0));
            Curve curveA = rect.ToNurbsCurve();
            Curve curveB = boundary.ToNurbsCurve();
            List<Point3d> points = new List<Point3d>();
            int k = Rhino.Geometry.Intersect.Intersection.CurveCurve(curveA, curveB, 0, 0).Count;
            if (k == 0 && width * width / ratio < areaOfPolygon(boundary))
            {
                return true;
            }
            else
            {
                return false;
            }
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
            ori.Transform(Transform.Rotation(angle, Point3d.Origin));
            ori.Transform(Transform.Translation(new Vector3d(-width / 2, -width / ratio / 2, 0)));
            Rectangle3d rect = new Rectangle3d(new Plane(ori, Vector3d.ZAxis), width, width / ratio);
            rect.Transform(Transform.Rotation(-angle, Point3d.Origin));
            return rect;
        }
        #endregion MaxRectangle


        ////////////////////////////////////////////////
        //////////  apt distribution & cores  //////////
        ////////////////////////////////////////////////

        private List<int> numberOfHousesForEachType(List<double> areaLength, List<double> ratio, Curve baseline)
        {
            List<int> result = new List<int>();
            double totalLength = baseline.GetLength();
            double s = 0;
            for (int i = 0; i < ratio.Count; i++)
            {
                s += areaLength[i] * ratio[i];
            }
            double n1 = totalLength * ratio[0] / s;
            List<double> stackNum = new List<double>();
            double ss = 0;
            stackNum.Add(0);
            for (int i = 0; i < ratio.Count; i++)
            {
                ss += n1 * ratio[i] / ratio[0];
                stackNum.Add(ss);
            }
            List<int> n = new List<int>();
            for (int i = 0; i < ratio.Count; i++)
            {
                n.Add((int)stackNum[i + 1] - (int)stackNum[i]);
            }
            return n;
        }
        private List<double> GetHouseEndParameters(List<int> unallocatedLengthIndices, List<double> areaLength, Curve centerLineCurve, double width, out List<int> targetAreaIndices)
        {
            //
            List<Curve> centerLineSegments = centerLineCurve.DuplicateSegments().ToList();
            Polyline centerLinePolyline;
            centerLineCurve.TryGetPolyline(out centerLinePolyline);

            //at least (entranceLength)mm of wall facing corridor needed to put entrance

            double val = 0;

            //initiate outputs

            List<double> houseEndVals = new List<double>();
            List<double> mappedVals = new List<double>();
            double nineHouseR = 1.5;

            //stretch
 


            //less than 10 units
            #region Less10Case
            if (5 < unallocatedLengthIndices.Count && unallocatedLengthIndices.Count < 10)
            {
                unallocatedLengthIndices.Sort((a, b) => -a.CompareTo(b));

                double sourceL = 0;
                for (int i = 0; i < unallocatedLengthIndices.Count; i++)
                {
                    sourceL += areaLength[unallocatedLengthIndices[i]];
                }
                double targetL = centerLineCurve.GetLength();
                double stretchRatio = targetL / sourceL;
                List<double> stretchedLength = new List<double>();
                for (int i = 0; i < areaLength.Count; i++)
                {
                    stretchedLength.Add(areaLength[i] * stretchRatio);
                }
              

                List<int> indices = new List<int>();
                //junction 2 : specific solutions for 6~9 houses
                if (unallocatedLengthIndices.Count == 6)
                {
                    indices = new int[] { 4, 0, 3, 5, 1, 2 }.ToList();
                    val = centerLineSegments[0].GetLength() / 2;
                }
                else if (unallocatedLengthIndices.Count == 7)
                {
                    indices = new int[] { 0, 2, 5, 6, 4, 3, 1 }.ToList();
                    val = centerLineSegments[0].GetLength() / 2;
                }
                else if (unallocatedLengthIndices.Count == 8)
                {
                    indices = new int[] { 5, 0, 7, 2, 6, 1, 4, 3 }.ToList();
                    val = (centerLineSegments[0].GetLength() + centerLineSegments[1].GetLength() / 2)
                        - stretchedLength[unallocatedLengthIndices[indices[0]]] - stretchedLength[unallocatedLengthIndices[indices[1]]];
                }
                else if (unallocatedLengthIndices.Count == 9)
                {
                    //junction 3 : rectangle ratio
                    if (centerLineSegments[1].GetLength() / centerLineSegments[0].GetLength() > nineHouseR)
                    {
                        indices = new int[] { 0, 7, 4, 3, 8, 2, 5, 6, 1 }.ToList();
                        val = centerLineSegments[0].GetLength() / 2;
                    }
                    else
                    {
                        indices = new int[] { 6, 7, 8, 5, 4, 1, 0, 2, 3 }.ToList();
                        val = (centerLineSegments[0].GetLength() + centerLineSegments[1].GetLength() / 2)
                            - stretchedLength[unallocatedLengthIndices[indices[0]]] - stretchedLength[unallocatedLengthIndices[indices[1]]];

                    }
                }

                targetAreaIndices = new List<int>();

                for (int i = 0; i < indices.Count; i++)
                {
                    targetAreaIndices.Add(unallocatedLengthIndices[i]);
                }

                //value mapping
                houseEndVals.Add(val);
                for (int i = 0; i < unallocatedLengthIndices.Count - 1; i++)
                {
                    val += stretchedLength[unallocatedLengthIndices[indices[i]]];
                    houseEndVals.Add(val % ((centerLineSegments[0].GetLength() + centerLineSegments[1].GetLength()) * 2));
                }
                mappedVals = new List<double>(valMapper(centerLineSegments[0].GetLength(), centerLineSegments[1].GetLength(), houseEndVals));

            }
            #endregion Less10Case

            //more than 10 units
            #region More10Case
            else if (10 <= unallocatedLengthIndices.Count)
            {
                double cornerMinLength = 1500;
                double minCornerUnitEdgeLength = width / 2 + cornerMinLength;

                //preserve top 4 areas for corners
                unallocatedLengthIndices.Sort((a, b) => -a.CompareTo(b));
                List<int> cornerIndices = unallocatedLengthIndices.Take(4).ToList();
                unallocatedLengthIndices.RemoveRange(0, 4);

                //put others on edges
                List<double> edgeLength = centerLineSegments.Select(n => n.GetLength()).ToList();
                List<List<int>> edgeIndices = new List<List<int>>();
                for (int i = 0; i < 4; i++)
                {
                    List<int> blank = new List<int>();
                    edgeIndices.Add(blank);
                }


                foreach (int i in unallocatedLengthIndices)
                {
                    int longestRemainEdgeIndex = edgeLength.IndexOf(edgeLength.Max());
                    edgeLength[longestRemainEdgeIndex] -= areaLength[i];
                    edgeIndices[longestRemainEdgeIndex].Add(i);
                }

                //sort corners
                List<double> leftLengthAtCorner = new List<double>();

                for (int i = 0; i < 4; i++)
                    leftLengthAtCorner.Add(edgeLength[i] / 2 + edgeLength[(4 + i -1) % 4] / 2);

                cornerIndices.Sort((a, b) => leftLengthAtCorner[a].CompareTo(leftLengthAtCorner[b]));


                //make unallocated list
                cornerIndices.Reverse();
                unallocatedLengthIndices.Reverse();
                unallocatedLengthIndices.AddRange(cornerIndices);
                cornerIndices.Reverse();
                unallocatedLengthIndices.Reverse();

                //adjust corners
                List<double> cornerLength = new List<double>();
                for (int i = 0; i < 4; i++)
                {
                    cornerLength.Add(areaLength[cornerIndices[i]]);
                }

                List<double> edgeStart = new List<double>(pushpush(width, edgeLength, cornerLength, edgeIndices, cornerMinLength, 5, 5));
                List<double> edgeEnd = new List<double>();
                for (int i = 0; i < 4; i++)
                {
                    edgeEnd.Add(areaLength[cornerIndices[(i + 1) % 4]] - edgeStart[(i + 1) % 4]);
                }

                //stretch
                List<double> sourceEdgeL = new List<double>();
                for (int i = 0; i < 4; i++)
                {
                    double l = 0;
                    for (int j = 0; j < edgeIndices[i].Count; j++)
                    {
                        l += areaLength[edgeIndices[i][j]];
                    }
                    sourceEdgeL.Add(l);
                }

                List<double> targetEdgeL = new List<double>();
                for (int i = 0; i < 4; i++)
                {
                  
                    if (edgeStart[i] < minCornerUnitEdgeLength)
                        edgeStart[i] = minCornerUnitEdgeLength;

                    double currentTargetEdgeLength = centerLineSegments[i].GetLength() - edgeStart[i] - edgeEnd[i];
                    targetEdgeL.Add(currentTargetEdgeLength);
                }

                List<double> stretchEdgeRatio = new List<double>();
                for (int i = 0; i < centerLineSegments.Count; i++)
                {
                    stretchEdgeRatio.Add(targetEdgeL[i] / sourceEdgeL[i]);
                }

                List<List<double>> stretchedAreaLength = new List<List<double>>();
                for (int i = 0; i < 4; i++)
                {
                    List<double> x = new List<double>();
                    for (int j = 0; j < areaLength.Count; j++)
                    {
                        x.Add(areaLength[j] * stretchEdgeRatio[i]);
                    }
                    stretchedAreaLength.Add(x);
                }

                //value mapping
                for (int i = 0; i < 4; i++)
                {
                    val = edgeStart[i];
                    for (int j = 0; j < i; j++)
                    {
                        val += centerLineSegments[j].GetLength();
                    }
                    houseEndVals.Add(val);
                    for (int j = 0; j < edgeIndices[i].Count; j++)
                    {
                        val += stretchedAreaLength[i][edgeIndices[i][j]];
                        houseEndVals.Add(val);
                    }
                }

                //target area type
                targetAreaIndices = new List<int>();
                for (int i = 0; i < 4; i++)
                {
                    for (int j = 0; j < edgeIndices[i].Count; j++)
                    {
                        targetAreaIndices.Add(edgeIndices[i][j]);
                    }
                    targetAreaIndices.Add(cornerIndices[i]);
                    //***혹시 여기서 i가 아니라 i+1이 들어가야 할 수도 있으니 나중에 확인할 것. 
                }

                mappedVals = new List<double>(valMapper(centerLineSegments[0].GetLength(), centerLineSegments[1].GetLength(), houseEndVals));
            }
            #endregion More10Case

            else//which means, less than 6 houses
            {
                targetAreaIndices = new List<int>();
            }
            return mappedVals;
        }
        private List<double> valMapper(double centerRectWidth, double centerRectHeight, List<double> startToHouseEndPtLengths)
        {
            double centerRectLength = 2*(centerRectWidth + centerRectHeight);
            List<double> mappedValue = new List<double>();
            for (int i = 0; i < startToHouseEndPtLengths.Count; i++)
            {
                int rectHalfIndex = (int)(startToHouseEndPtLengths[i] / (centerRectLength/2))%2;
                double halfStartToHouseEndPt = startToHouseEndPtLengths[i] - (centerRectLength/2) * rectHalfIndex;
                double endParamOnCurrentLine = 0;
                int lineParamOnHalf = 0;

                bool isOnHeight = halfStartToHouseEndPt > centerRectWidth;
                if (isOnHeight)
                {
                    lineParamOnHalf = 1;
                    halfStartToHouseEndPt -= centerRectWidth;
                    endParamOnCurrentLine = halfStartToHouseEndPt / centerRectHeight;
                }
                else
                {
                    lineParamOnHalf = 0;
                    endParamOnCurrentLine = halfStartToHouseEndPt / centerRectWidth;
                }
                mappedValue.Add((rectHalfIndex * 2 + lineParamOnHalf + endParamOnCurrentLine) % 4);
            }
            return mappedValue;
        }

        private List<double> pushpush(double width, List<double> edgeLength, List<double> cornerLength, List<List<int>> edgeIndices, double minL, int resolutionCount, int iterationCount)
        {
            //initial setting
            List<int> edgeNum = edgeIndices.Select(n => n.Count).ToList();
            double deviation = double.PositiveInfinity;


            List<double> adjustableLength = new List<double>();   //Secure min Length.
            for (int i = 0; i < 4; i++)
            {
                adjustableLength.Add(cornerLength[i] - 2 * minL - width);
            }


            List<double> solution = new List<double>();  //Set intitial solution.
            for (int i = 0; i < 4; i++)
            {
                solution.Add(cornerLength[i] / 2);
            }

            //loop start
            int token = 1;
            while (token != iterationCount)
            {
                for (int i = 0; i < 4; i++)
                {
                    double resoultionCurrent = adjustableLength[i] / Math.Pow(2, token + 1) / resolutionCount;
                    double deviationCurrent = deviation;

                    List<double> solutionHere = new List<double>(solution);

                    for (int j = 0; j < 2 * resolutionCount + 1; j++)
                    {
                        //new solution to test
                        List<double> solClone = new List<double>(solution);
                        solClone[i] = solution[i] + ((j - resolutionCount) * resoultionCurrent);

                        //new solution standard deviation
                        List<double> fitnessVal = new List<double>();
                        for (int k = 0; k < 4; k++)
                        {
                            fitnessVal.Add((edgeLength[k] - solClone[k] - (cornerLength[(k + 1) % 4] - solClone[(k + 1) % 4]) + width) / edgeNum[k]);
                        }
                        double average = fitnessVal.Average();
                        double sumOfSquaresOfDifferences = fitnessVal.Select(val => (val - average) * (val - average)).Sum();
                        double deviationNew = Math.Sqrt(sumOfSquaresOfDifferences / fitnessVal.Count);

                        //find the best solution in this 'for loop'
                        if (deviationNew < deviationCurrent)
                        {
                            deviationCurrent = deviationNew;
                            solutionHere = solClone;
                        }
                    }

                    //compare with old solution
                    if (deviationCurrent < deviation)
                    {
                        deviation = deviationCurrent;
                        solution = solutionHere;
                    }
                }
                token += 1;
            }

            //no corner sd check
            double deviationCorner = double.PositiveInfinity;
            List<double> solCorner = new List<double>(solution);
            int cornerInd = 0;
            for (int i = 0; i < 16; i++)
            {
                List<int> k = new List<int>();
                k.Add(i % 2);
                k.Add(((int)i / 2) % 2);
                k.Add(((int)i / 4) % 2);
                k.Add(((int)i / 8) % 2);
                for (int j = 0; j < 4; j++)
                {
                    solCorner[j] = width + (cornerLength[j] - width) * k[j] - width / 2;
                }

                //new solution standard deviation
                List<double> fitnessVal = new List<double>();
                for (int j = 0; j < 4; j++)
                {
                    fitnessVal.Add((edgeLength[j] - solCorner[j] - (cornerLength[(j + 1) % 4] - solCorner[(j + 1) % 4]) + width) / edgeNum[j]);
                }
                double average = fitnessVal.Average();
                double sumOfSquaresOfDifferences = fitnessVal.Select(val => (val - average) * (val - average)).Sum();
                double deviationNew = Math.Sqrt(sumOfSquaresOfDifferences / fitnessVal.Count);

                //find the best solution in this 'for loop'
                if (deviationNew < deviationCorner)
                {
                    deviationCorner = deviationNew;
                    cornerInd = i;
                }

            }
            List<double> solThis = new List<double>();
            List<int> d = new List<int>();
            d.Add(cornerInd % 2);
            d.Add(((int)cornerInd / 2) % 2);
            d.Add(((int)cornerInd / 4) % 2);
            d.Add(((int)cornerInd / 8) % 2);
            for (int j = 0; j < 4; j++)
            {
                solThis.Add(width + (cornerLength[j] - width) * d[j] - width / 2);
            }

            if (deviationCorner < deviation)
            {
                solution = solThis;
            }

            return solution;
        }

        ///////////////////////////////
        //////////  outputs  //////////
        ///////////////////////////////
        private double exclusiveAreaCalculatorAG3Corner(double xa, double xb, double ya, double yb, double targetArea, double balconyDepth)
        {
            double exclusiveArea = xa * ya - xb * yb;
            exclusiveArea -= (xa + ya) * balconyDepth;
            exclusiveArea += balconyDepth * balconyDepth * 2;
            return exclusiveArea;
        }

        private double exclusiveAreaCalculatorAG3Edge(double xa, double xb, double ya, double yb, double targetArea, double balconyDepth)
        {
            double exclusiveArea = xa * ya - xb * yb;
            exclusiveArea -= (xa + xa - xb) * balconyDepth;
            return exclusiveArea;
        }

        private List<List<Curve>> buildingOutlineMakerAG3(Polyline outline, double width)
        {
            List<List<Curve>> buildingOutlines = new List<List<Curve>>();
            List<Curve> buildingOutlinesB = new List<Curve>();

            List<Point3d> outPoints = new List<Point3d>(outline);
            outPoints.RemoveAt(4);

            List<Point3d> inPoints = new List<Point3d>();
            for (int i = 0; i < 4; i++)
            {
                Vector3d xx = new Vector3d(outPoints[(i + 3) % 4] - outPoints[i]);
                Vector3d yy = new Vector3d(outPoints[(i + 1) % 4] - outPoints[i]);
                xx.Unitize();
                yy.Unitize();
                Point3d tempPoint = new Point3d(outPoints[i]);
                tempPoint.Transform(Transform.Translation(Vector3d.Multiply(xx, width + Consts.corridorWidth)));
                tempPoint.Transform(Transform.Translation(Vector3d.Multiply(yy, width + Consts.corridorWidth)));
                inPoints.Add(tempPoint);
            }
            inPoints.Add(inPoints[0]);
            inPoints.Reverse();
            List<Point3d> buildingPoints = new List<Point3d>();

            for (int i = 0; i < 4; i++)
            {
                buildingPoints.Add(outPoints[i]);
            }
            buildingPoints.Add(outPoints[0]);
            for (int i = 0; i < 5; i++)
            {
                buildingPoints.Add(inPoints[i]);
            }
            buildingPoints.Add(outPoints[0]);

            buildingOutlinesB.Add(new Polyline(buildingPoints).ToNurbsCurve());
            buildingOutlines.Add(buildingOutlinesB);
            return buildingOutlines;
        }
    }
}
