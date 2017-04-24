using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rhino.Geometry;
using System.Collections;
using Rhino.Collections;

namespace TuringAndCorbusier
{
    class AG1 : ApartmentmentGeneratorBase
    {
        public override ApartmentGeneratorOutput generator(Plot plot, ParameterSet parameterSet, Target target)
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
            Regulation regulationLow = new Regulation(storiesLow);
            List<double> ratio = target.TargetRatio;
            List<double> area = target.TargetArea.Select(n => n * 1000 * 1000).ToList();
            double areaLimit = Consts.AreaLimit;
            BuildingType buildingType = regulationHigh.BuildingType;
            List<double> areaLength = new List<double>();
            for (int i = 0; i < area.Count; i++)
            {
                if (area[i] < areaLimit)
                    areaLength.Add(area[i] / (width - Consts.corridorWidth));
                else
                    areaLength.Add(area[i] / width);
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
            Curve[] plotArr = plotArrMaker(boundary);

            //법규 : 대지 안의 공지
            Curve surroundingsHigh = (new Regulation(storiesHigh)).fromSurroundingsCurve(plot, regulationHigh, plotArr);
            Curve surroundingsLow = (new Regulation(storiesLow)).fromSurroundingsCurve(plot, regulationLow, plotArr);

            //법규 : 일조에 의한 높이제한
            Curve northHigh = (new Regulation(storiesHigh)).fromNorthCurve(plot, regulationHigh, plotArr);
            Curve northLow = (new Regulation(storiesLow)).fromNorthCurve(plot, regulationLow, plotArr);

            ///////////////////////////////////////////
            //////////  additional settings  //////////
            ///////////////////////////////////////////

            //법규 : 인접대지경계선(채광창)
            Curve lightingHigh = byLightingCurve(plot, regulationHigh, plotArr, angleRadian);
            Curve lightingLow = byLightingCurve(plot, regulationLow, plotArr, angleRadian);

            ////
            //step 1 : create baselines for buildings
            //step 2 : create cores
            //법규 : low, 건물 기준선 : high

            //일부 조건(대지안의공지, 일조에 의한 높이제한)을 만족하는 경계선
            //모든 조건(대지안의공지, 일조에의한 높이제한, 인접대지경계선)을 만족하는 경계선
            Curve partialRegulationLow = CommonFunc.joinRegulations(surroundingsLow, northLow);
            Curve wholeRegulationLow = CommonFunc.joinRegulations(partialRegulationLow, lightingLow);

            ////

            ////
            //setp 3 : cut apartments with regulation(high)
            //법규 : high, 건물 기준선 : high

            //일부 조건(대지안의공지, 일조에 의한 높이제한)을 만족하는 경계선
            //모든 조건(대지안의공지, 일조에의한 높이제한, 인접대지경계선)을 만족하는 경계선
            Curve partialRegulationHigh = CommonFunc.joinRegulations(surroundingsHigh, northHigh);
            Curve wholeRegulationHigh = CommonFunc.joinRegulations(partialRegulationHigh, lightingHigh);

            ////////////////////////////////////
            //////////  apt baseline  //////////
            ////////////////////////////////////

            List<Line> baselines = baselineMaker(wholeRegulationHigh, parameterSet);
            List<Curve> aptLines = aptLineMaker(baselines, wholeRegulationLow, width, areaLength);


            ////////////////////////////////////////////////
            //////////  apt distribution & cores  //////////
            ////////////////////////////////////////////////

            List<int> cullLine = new List<int>();
            List<List<int>> areaTypeNumBuilding = areaTypeDistributor(area, areaLength, ratio, aptLines, width, areaLimit, out cullLine);
            aptLines = aptLineCuller(aptLines, cullLine);

            List<double> stretchFactor = stretchFactorMaker(areaTypeNumBuilding, aptLines, areaLength);

            ///////////////////////////////
            //////////  shorten  //////////
            ///////////////////////////////

            shortener(aptLines, stretchFactor, 1.3, out aptLines, out stretchFactor);

            double fromEndParam = 0.25;
            double coverageResize = coverageRatioChecker(parameterSet, plot, aptLines, area, areaTypeNumBuilding, areaLength, areaLimit, stretchFactor, partialRegulationHigh, fromEndParam);

            List<Curve> aptLinesTemp1 = new List<Curve>();
            for (int i = 0; i < aptLines.Count; i++)
            {
                aptLinesTemp1.Add(new Line(aptLines[i].PointAtLength(aptLines[i].GetLength() * coverageResize / 2), aptLines[i].PointAtLength(aptLines[i].GetLength() * (1 - coverageResize / 2))).ToNurbsCurve());
            }

            aptLines = aptLinesTemp1;
            stretchFactor = stretchFactor.Select(n => n * (1 - coverageResize)).ToList();

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
            List<double> coreProtrusionFactor = coreProtrusionFactorFinder(aptLines, houseEndParams, buildingAccessType, 0.25, parameterSet, width, partialRegulationHigh);
            List<int> cullAll = new List<int>();
            for (int i = 0; i < aptLines.Count; i++)
            {
                if (buildingAccessType[i] == 0 && (coreProtrusionFactor[i] != 1 || householdOrderBuilding[i].Count == 1))
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

            ///////////////////////////////
            //////////  outputs  //////////
            ///////////////////////////////

            //core properties
            List<List<CoreProperties>> coreProperties = corePropertiesMaker(parameterSet, buildingAccessType, aptLines, houseEndParams, coreProtrusionFactor, fromEndParam);

            //core outlines
            List<List<Rectangle3d>> coreOutlines = new List<List<Rectangle3d>>();
            for (int i = 0; i < coreProperties.Count; i++)
            {
                List<Rectangle3d> coreOutlinesTemp = new List<Rectangle3d>();
                for (int j = 0; j < coreProperties[i].Count; j++)
                {
                    Rectangle3d coreRect = new Rectangle3d(new Plane(coreProperties[i][j].Origin, coreProperties[i][j].XDirection, coreProperties[i][j].YDirection), coreProperties[i][j].CoreType.GetWidth(), coreProperties[i][j].CoreType.GetDepth());

                    coreOutlinesTemp.Add(coreRect);
                }
                coreOutlines.Add(coreOutlinesTemp);
            }

            //household properties
            List<List<List<HouseholdProperties>>> householdProperties = householdPropertiesMaker(parameterSet, buildingAccessType, aptLines, houseEndParams, coreProtrusionFactor, householdOrderBuilding, areaLength, stretchFactor, wholeRegulationHigh, Consts.minimumArea);


            //building outlines
            List<List<Curve>> buildingOutlines = buildingOutlineMaker(aptLines, buildingAccessType, width, parameterSet.CoreType.GetDepth());

            //parking lot
            //List<ParkingLot> parkingLot = parkingLotMaker(boundary, householdProperties, parameterSet.CoreType.GetWidth(), parameterSet.CoreType.GetDepth(), coreOutlines);
            ParkingLotOnEarth parkingLotOnEarth = parkingLotOnEarthMaker(boundary, householdProperties, parameterSet.CoreType.GetWidth(), parameterSet.CoreType.GetDepth(), coreOutlines);
            ParkingLotUnderGround parkingLotUnderGroud = new ParkingLotUnderGround();


            ApartmentGeneratorOutput result = new ApartmentGeneratorOutput("PT-1", plot, buildingType, parameterSet, target, coreProperties, householdProperties, parkingLotOnEarth, parkingLotUnderGroud, buildingOutlines, aptLines);
            //commercialArea
            //CommonFunc.createCommercialFacility(householdProperties, aptLines, plot, TuringAndCorbusierPlugIn.InstanceClass.page1Settings.MaxBuildingCoverage - result.GetBuildingCoverage(), TuringAndCorbusierPlugIn.InstanceClass.page1Settings.MaxFloorAreaRatio - result.GetGrossAreaRatio());

            return result;
        }

        ///////////////////////////////////////////
        //////////  GA initial settings  //////////
        ///////////////////////////////////////////

        //Parameter 최소값 ~ 최대값 {storiesHigh, storiesLow, width, angle, moveFactor}
        private double[] minInput = { 3, 3, 8500, 0, 0 };
        //private double[] minInput = { 6, 6, 10500, 0, 0 };
        private double[] maxInput = { 7, 7, 10500, 2 * Math.PI, 1 };

        //Parameter GA최적화 {mutation probability, elite percentage, initial boost, population, generation, fitness value, mutation factor(0에 가까울수록 변동 범위가 넓어짐)
        private double[] GAparameterset = { 0.2, 0.03, 4, 200, 7, 3, 1 };
        //private double[] GAparameterset = { 0.2, 0.03, 1, 100, 5, 3, 1 };

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
            CoreType[] tempCoreTypes = { CoreType.Parallel, CoreType.Horizontal, CoreType.Vertical_AG1 };
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
                double offsetDistance = regulation.DistanceByLighting * Math.Sin(convertedAngle) - 0.5 * plot.SimplifiedSurroundings[i];

                if (offsetDistance > 0)
                {
                    distanceByLighting[i] = offsetDistance;
                }
                else
                {
                    distanceByLighting[i] = 0;
                }
            }

            List<Point3d> ptsByLighting = new List<Point3d>();
            for (int i = 0; i < plotArr.Length; i++)
            {
                int h = (i - 1 + plotArr.Length) % plotArr.Length;
                Curve curveH;
                Curve curveI;
                if (distanceByLighting[h] != 0)
                {
                    curveH = plotArrExtended[h].Offset(Plane.WorldXY, distanceByLighting[h], 0, CurveOffsetCornerStyle.None)[0];
                }
                else
                {
                    curveH = plotArrExtended[h];
                }
                if (distanceByLighting[i] != 0)
                {
                    curveI = plotArrExtended[i].Offset(Plane.WorldXY, distanceByLighting[i], 0, CurveOffsetCornerStyle.None)[0];
                }
                else
                {
                    curveI = plotArrExtended[i];
                }

                ptsByLighting.Add(Rhino.Geometry.Intersect.Intersection.CurveCurve(curveH, curveI, 0, 0)[0].PointA);
            }

            ptsByLighting.Add(ptsByLighting[0]);
            Curve byLightingCurve = (new Polyline(ptsByLighting)).ToNurbsCurve();

            return byLightingCurve;
        }

        ////////////////////////////////////
        //////////  apt baseline  //////////
        ////////////////////////////////////

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
            while (yPos < maxP.Y - minP.Y)
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
                        if (wholeRegulation.Contains(baselineRaw[i].PointAt(intervals[j].Min)) == PointContainment.Inside)
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
                if (buildingAccessType[i] == 0 && (coreProtrusionFactor[i] != 1 || householdOrderBuilding[i].Count == 1))
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
            List<List<CoreProperties>> coreProperties = corePropertiesMaker(parameterSet, buildingAccessType, aptLines, houseEndParams, coreProtrusionFactor, fromEndParam);


            //coverage calculator
            double coreCoverage = 0;
            double houseCoverage = 0;

            for (int i = 0; i < coreProperties.Count; i++)
            {
                for (int j = 0; j < coreProperties[i].Count; j++)
                {
                    coreCoverage += coreProperties[i][j].GetArea();
                    double test = coreProperties[i][j].GetArea();
                }

            }

            for (int i = 0; i < aptLines.Count; i++)
                houseCoverage += aptLines[i].GetLength() * width;
            for (int i = 0; i < coreProperties.Count; i++)
            {
                for (int j = 0; j < coreProperties[i].Count; j++)
                    houseCoverage -= coreProperties[i][j].GetArea() * (1 - coreProtrusionFactor[i]);
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

                double factor = factorFinder(partialRegulation, coreBoundary[i], coreDepth, lineNormal);
                protrusionFactor.Add(factor);
            }
            return protrusionFactor;
        }
        private double factorFinder(Curve partialRegulation, Curve coreBoundary, double coreDepth, Vector3d normal)
        {
            //initial setting
            int searchResolution = 5;
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
                clone.Transform(Transform.Translation(Vector3d.Multiply(normal, mid * coreDepth)));
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

        ///////////////////////////////
        //////////  outputs  //////////
        ///////////////////////////////

        private List<List<CoreProperties>> corePropertiesMaker(ParameterSet parameterSet, List<int> buildingAccessType, List<Curve> aptLines, List<List<double>> houseEndParams, List<double> coreProtrusionFactor, double fromEndParam)
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
                    if (aptLines[i].PointAt(fromEndParam).DistanceTo(aptLines[i].PointAt(1 - fromEndParam)) > coreWidth)
                    {
                        Point3d core1 = aptLines[i].PointAt(fromEndParam);
                        core1.Transform(Transform.Translation(Vector3d.Multiply(tangentVec, -coreWidth / 2)));
                        core1.Transform(Transform.Translation(Vector3d.Multiply(verticalVec, width / 2 - ((coreDepth - Consts.corridorWidth) * (1 - coreProtrusionFactor[i]) + Consts.corridorWidth))));
                        coreOriTemp.Add(core1);
                        core1 = aptLines[i].PointAt(1 - fromEndParam);
                        core1.Transform(Transform.Translation(Vector3d.Multiply(tangentVec, -coreWidth / 2)));
                        core1.Transform(Transform.Translation(Vector3d.Multiply(verticalVec, width / 2 - ((coreDepth - Consts.corridorWidth) * (1 - coreProtrusionFactor[i]) + Consts.corridorWidth))));
                        coreOriTemp.Add(core1);
                    }
                    else
                    {
                        Point3d core1 = aptLines[i].PointAt(0.5);
                        core1.Transform(Transform.Translation(Vector3d.Multiply(tangentVec, -coreWidth / 2)));
                        core1.Transform(Transform.Translation(Vector3d.Multiply(verticalVec, width / 2 - ((coreDepth - Consts.corridorWidth) * (1 - coreProtrusionFactor[i]) + Consts.corridorWidth))));
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
                            corePoint.Transform(Transform.Translation(Vector3d.Multiply(verticalVec, width / 2 - ((coreDepth - Consts.corridorWidth) * (1 - coreProtrusionFactor[i]) + Consts.corridorWidth))));
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

            //combine information into coreProperties
            List<List<CoreProperties>> coreProperties = new List<List<CoreProperties>>();
            for (int i = 0; i < coreOri.Count; i++)
            {
                List<CoreProperties> corePropertiesTemp = new List<CoreProperties>();
                for (int j = 0; j < coreOri[i].Count; j++)
                {
                    corePropertiesTemp.Add(new CoreProperties(coreOri[i][j], coreVecX[i][j], coreVecY[i][j], parameterSet.CoreType, storiesHigh, parameterSet.CoreType.GetDepth() - coreProtrusionFactor[i] ));
                }
                coreProperties.Add(corePropertiesTemp);
            }
            return coreProperties;
        }

        private List<List<List<HouseholdProperties>>> householdPropertiesMaker(ParameterSet parameterSet, List<int> buildingAccessType, List<Curve> aptLines, List<List<double>> houseEndParams, List<double> coreProtrusionFactor, List<List<int>> householdOrderBuilding, List<double> areaLength, List<double> stretchFactor, Curve wholeRegulationHigh, double minimumArea)
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

            List<List<List<HouseholdProperties>>> householdProperties = new List<List<List<HouseholdProperties>>>();
            for (int i = 0; i < householdOrderBuilding.Count; i++)
            {
                List<List<HouseholdProperties>> householdPropertiesTemp = new List<List<HouseholdProperties>>();
                for (int j = 0; j < storiesHigh; j++)
                {
                    List<List<HouseholdProperties>> householdLow = householdLowMaker(buildingAccessType, aptLines, houseEndParams, coreProtrusionFactor, householdOrderBuilding, areaLength, stretchFactor, width, coreWidth, coreDepth, j);
                    List<List<HouseholdProperties>> householdHigh = householdHighMaker(householdLow, buildingAccessType, wholeRegulationHigh, householdOrderBuilding);
                    List<HouseholdProperties> householdPropertiesTempTemp = new List<HouseholdProperties>();
                    if (j < storiesLow)
                    {
                        for (int k = 0; k < householdOrderBuilding[i].Count; k++)
                        {
                            householdPropertiesTempTemp.Add(householdLow[i][k]);
                        }
                    }
                    else
                    {
                        for (int k = 0; k < householdOrderBuilding[i].Count; k++)
                        {
                            householdPropertiesTempTemp.Add(householdHigh[i][k]);
                        }
                    }
                    householdPropertiesTemp.Add(householdPropertiesTempTemp);
                }
                householdProperties.Add(householdPropertiesTemp);
            }
            householdProperties = householdPropertiesCuller(householdProperties, minimumArea);
            return householdProperties;
        }
        private List<List<List<HouseholdProperties>>> householdPropertiesCuller(List<List<List<HouseholdProperties>>> householdProperties, double minimumArea)
        {
            List<List<List<HouseholdProperties>>> output = new List<List<List<HouseholdProperties>>>();
            for (int i = 0; i < householdProperties.Count; i++)
            {
                List<List<HouseholdProperties>> outputTemp = new List<List<HouseholdProperties>>();
                for (int j = 0; j < householdProperties[i].Count; j++)
                {
                    List<HouseholdProperties> outputTempTemp = new List<HouseholdProperties>();
                    for (int k = 0; k < householdProperties[i][j].Count; k++)
                    {
                        if (householdProperties[i][j][k].GetArea() >= minimumArea)
                        {
                            outputTempTemp.Add(householdProperties[i][j][k]);
                        }

                    }
                    outputTemp.Add(outputTempTemp);
                }
                output.Add(outputTemp);
            }
            return output;
        }
        private List<List<HouseholdProperties>> householdLowMaker(List<int> buildingAccessType, List<Curve> aptLines, List<List<double>> houseEndParams, List<double> coreProtrusionFactor, List<List<int>> householdOrderBuilding, List<double> areaLength, List<double> stretchFactor, double width, double coreWidth, double coreDepth, double floor)
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
                        homeOriHouse.Transform(Transform.Translation(Vector3d.Multiply(verticalVec, width / 2 - ((coreDepth - Consts.corridorWidth) * (1 - coreProtrusionFactor[i]) + Consts.corridorWidth))));
                        homeVecXHouse = Vector3d.Multiply(tangentVec, -1);
                        //homeOriHouse.Transform(Transform.Translation(Vector3d.Multiply(homeVecXHouse, coreWidth / 2)));
                        wallFactor = new List<double>(new double[] { 1, 0.5, 1, 0.5 });
                    }
                    else
                    {
                        xaHouse = areaLength[targetAreaTypeRaw[i][j]] * stretchFactor[i];
                        xbHouse = coreWidth / 2;
                        yaHouse = width;
                        ybHouse = ((coreDepth - Consts.corridorWidth) * (1 - coreProtrusionFactor[i]) + Consts.corridorWidth);
                        exclusiveArea = exclusiveAreaCalculatorAG1(xaHouse, xbHouse, yaHouse, ybHouse, targetAreaTypeRaw[i][j], Consts.balconyDepth);
                        homeOriHouse = aptLines[i].PointAt(houseEndParams[i][((int)(j / 2) * 2) + 1]);
                        homeOriHouse.Transform(Transform.Translation(Vector3d.Multiply(verticalVec, width / 2 - ((coreDepth - Consts.corridorWidth) * (1 - coreProtrusionFactor[i]) + Consts.corridorWidth))));
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

            List<List<HouseholdProperties>> householdLow = new List<List<HouseholdProperties>>();
            for (int i = 0; i < homeOriRaw.Count; i++)
            {
                List<HouseholdProperties> householdLowTemp = new List<HouseholdProperties>();
                for (int j = 0; j < homeOriRaw[i].Count; j++)
                {
                    householdLowTemp.Add(new HouseholdProperties(homeOriRaw[i][j], homeVecXRaw[i][j], homeVecYRaw[i][j], xaRaw[i][j], xbRaw[i][j], yaRaw[i][j], ybRaw[i][j], targetAreaTypeRaw[i][j], exclusiveAreaRaw[i][j], lightingEdges[i][j], entrancePoints[i][j], wallFactorRaw[i][j]));
                }
                householdLow.Add(householdLowTemp);
            }

            return householdLow;
        }
        private List<List<HouseholdProperties>> householdHighMaker(List<List<HouseholdProperties>> householdLow, List<int> buildingAccessType, Curve wholeRegulationHigh, List<List<int>> householdOrderBuilding)
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

            List<List<HouseholdProperties>> householdHigh = new List<List<HouseholdProperties>>();
            for (int i = 0; i < homeOriUpper.Count; i++)
            {
                List<HouseholdProperties> householdHighTemp = new List<HouseholdProperties>();
                for (int j = 0; j < homeOriUpper[i].Count; j++)
                {
                    householdHighTemp.Add(new HouseholdProperties(homeOriUpper[i][j], homeVecXUpper[i][j], homeVecYUpper[i][j], xaUpper[i][j], xbUpper[i][j], yaUpper[i][j], ybUpper[i][j], targetAreaTypeUpper[i][j], exclusiveAreaUpper[i][j], lightingEdges[i][j], entrancePoints[i][j], householdLow[i][j].WallFactor));
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

        ParkingLotOnEarth parkingLotOnEarthMaker(Curve boundary, List<List<List<HouseholdProperties>>> householdProperties, double coreWidth, double coreDepth, List<List<Rectangle3d>> cores)
        {

            List<List<Point3d>> neatHomeOri = new List<List<Point3d>>();

            for (int i = 0; i < householdProperties.Count; i++)
            {
                List<Point3d> neatHomeOriTemp = new List<Point3d>();
                if (householdProperties[i][0].Count != 0)
                {
                    for (int j = 0; j < householdProperties[i][0].Count; j++)
                    {
                        neatHomeOriTemp.Add(householdProperties[i][0][j].Origin);
                    }
                    neatHomeOri.Add(neatHomeOriTemp);
                }
            }

            List<Point3d> neatCoreCenters = new List<Point3d>();

            for (int i = 0; i < cores.Count(); i++)
            {
                for (int j = 0; j < cores[i].Count(); j++)
                {
                    neatCoreCenters.Add(cores[i][j].Center);
                }
            }

            List<ParkingLine> parkingLots = new List<ParkingLine>();

            if (neatHomeOri.Count != 0)
            {
                Line firstLine = new Line(neatHomeOri[0][neatHomeOri[0].Count() - 1], neatHomeOri[0][0]);

                Vector3d tempTestVector = firstLine.ToNurbsCurve().TangentAt(0.5);
                tempTestVector.Transform(Transform.Rotation(Math.PI / 2, Point3d.Origin));

                Vector3d newVector = new Vector3d(neatCoreCenters[0] - firstLine.ClosestPoint(neatCoreCenters[0], false));
                newVector = newVector / newVector.Length;

                if (tempTestVector == newVector)
                {
                    firstLine = new Line(neatHomeOri[0][0], neatHomeOri[0][neatHomeOri[0].Count() - 1]);
                }

                List<double> distance = new List<double>();

                List<double> parkingLotOffset = new List<double>();
                parkingLotOffset.Add(0);
                List<int> parkingLotType = new List<int>();
                parkingLotType.Add(0);

                bool currentEncounterRoad = false;
                int currentDistanceNum = 0;

                for (int i = 1; i < neatHomeOri.Count(); i++)
                {
                    Point3d tempDistPoint = firstLine.ClosestPoint(neatHomeOri[i][0], false);
                    Curve tempDistCurve = new LineCurve(tempDistPoint, neatHomeOri[i][0]);

                    distance.Add(tempDistCurve.GetLength());
                }

                int testInfLoop = 0;
                while (currentDistanceNum < distance.Count())
                {
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
                                parkingLotType.Add(0);
                                currentEncounterRoad = true;
                            }
                            else if (l > 14000)
                            {
                                parkingLotOffset.Add(lastOffset + 8000);
                                parkingLotType.Add(1);
                                currentEncounterRoad = true;
                            }
                            else
                            {
                                if (l + coreDepth > 16000)
                                {
                                    parkingLotOffset.Add(distance[currentDistanceNum] - 5000);
                                    parkingLotOffset.Add(distance[currentDistanceNum]);
                                    parkingLotType.Add(0);
                                    parkingLotType.Add(0);
                                    currentEncounterRoad = false;
                                }
                                else
                                {
                                    if (l > 3000)
                                    {
                                        parkingLotOffset.Add(lastOffset + 11000);
                                        parkingLotOffset.Add(lastOffset + 16000);
                                        parkingLotType.Add(0);
                                        parkingLotType.Add(0);
                                        currentEncounterRoad = false;
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (l > 8000)
                            {
                                parkingLotOffset.Add(lastOffset + 5000);
                                parkingLotType.Add(0);
                                currentEncounterRoad = false;
                            }
                            else if (l > 6000)
                            {
                                parkingLotOffset.Add(lastOffset + 11000);
                                parkingLotType.Add(0);
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
                        return new ParkingLotOnEarth(parkingLots);
                    }
                }

                List<Curve> parkingLotBase = new List<Curve>();
                Curve firstCurve = firstLine.ToNurbsCurve();

                foreach (double i in parkingLotOffset)
                {
                    if (i == 0)
                    {
                        parkingLotBase.Add(firstCurve);
                    }
                    else
                    {
                        parkingLotBase.Add(firstCurve.Offset(Plane.WorldXY, -i, 0, CurveOffsetCornerStyle.None)[0]);
                    }
                }

                //20160114 추가분 시작부분 추가

                Curve firstParkingLotBaseCurve = parkingLotBase[0];
                Curve lastParkingLotBaseCurve = parkingLotBase[parkingLotBase.Count() - 1];

                double[] firstCurveOffsetDistance = { 16000, 22000 };
                List<Brep> offsettedFirstBase = new List<Brep>();

                foreach (double i in firstCurveOffsetDistance)
                {
                    Curve tempCurve = firstParkingLotBaseCurve.Offset(Plane.WorldXY, i, 0, CurveOffsetCornerStyle.None)[0];
                    Curve secondCurve = firstParkingLotBaseCurve.Offset(Plane.WorldXY, 50000, 0, CurveOffsetCornerStyle.None)[0];

                    Curve[] tempLoftBase = { tempCurve, secondCurve };

                    offsettedFirstBase.Add(Brep.CreateFromLoft(tempLoftBase, Point3d.Unset, Point3d.Unset, LoftType.Straight, false)[0]);
                }

                List<double> intersectLength = new List<double>();

                foreach (Brep i in offsettedFirstBase)
                {
                    Curve[] tempOutCurves;
                    Point3d[] tempOutPts;

                    Rhino.Geometry.Intersect.Intersection.CurveBrep(boundary, i, 0.1, out tempOutCurves, out tempOutPts);

                    double tempOutput = 0;

                    foreach (Curve j in tempOutCurves)
                    {
                        tempOutput = tempOutput + j.GetLength();
                    }

                    intersectLength.Add(tempOutput);
                }

                int selectNth_First = 0;

                for (int i = 1; i < intersectLength.Count(); i++)
                {
                    if (intersectLength[i] / intersectLength[0] > 0.5)
                    {
                        selectNth_First += 1;
                    }
                }

                if (selectNth_First == 0)
                {
                    parkingLotBase.Add(firstParkingLotBaseCurve.Offset(Plane.WorldXY, 11000, 0, CurveOffsetCornerStyle.None)[0]);
                    parkingLotType.Add(0);
                }
                else if (selectNth_First == 1)
                {
                    parkingLotBase.Add(firstParkingLotBaseCurve.Offset(Plane.WorldXY, 5000, 0, CurveOffsetCornerStyle.None)[0]);
                    parkingLotType.Add(0);
                    parkingLotBase.Add(firstParkingLotBaseCurve.Offset(Plane.WorldXY, 16000, 0, CurveOffsetCornerStyle.None)[0]);
                    parkingLotType.Add(0);
                }

                //20160115 추가분 끝부분 추가

                parkingLotBase.Add(lastParkingLotBaseCurve.Offset(Plane.WorldXY, -11000, 0, CurveOffsetCornerStyle.None)[0]);
                parkingLotType.Add(0);

                //기준선으로부터 쓸 수 있는 선 추출

                List<Rectangle3d> unnestedCoreList = new List<Rectangle3d>();

                foreach (List<Rectangle3d> i in cores)
                {
                    foreach (Rectangle3d j in i)
                    {
                        unnestedCoreList.Add(j);
                    }
                }

                List<Curve> extendedCore = new List<Curve>();

                foreach (Rectangle3d i in unnestedCoreList)
                {
                    Plane tempPlane = new Plane((i.PointAt(0) + i.PointAt(2)) / 2, (i.PointAt(1) + i.PointAt(2)) / 2, (i.PointAt(3) + i.PointAt(2) / 2));
                    double tempScaleFactor = (coreDepth / 2 + 5200) / (coreDepth / 2);
                    i.Transform(Transform.Scale(tempPlane, 1, tempScaleFactor, 1));
                    extendedCore.Add(i.ToNurbsCurve());
                }

                List<List<Curve>> parkingLotCurves = new List<List<Curve>>();

                for (int i = 0; i < parkingLotBase.Count; i++)
                {
                    double tempExtend = (new Vector3d(boundary.GetBoundingBox(true).Max - boundary.GetBoundingBox(true).Min)).Length;

                    Curve tempExtendedCurve = parkingLotBase[i].Extend(CurveEnd.Both, tempExtend, CurveExtensionStyle.Line);

                    tempExtendedCurve.Translate(Vector3d.Multiply(Vector3d.ZAxis, -tempExtendedCurve.PointAtEnd.Z));

                    tempExtendedCurve.Translate(Vector3d.Multiply(Vector3d.ZAxis, -tempExtendedCurve.PointAtEnd.Z));

                    List<double> splitterSet = SplitWithRegions(tempExtendedCurve, extendedCore);

                    var tempIntersection = Rhino.Geometry.Intersect.Intersection.CurveCurve(tempExtendedCurve, boundary, 0.1, 0.1);

                    for (int j = 0; j < tempIntersection.Count(); j++)
                    {
                        splitterSet.Add(tempIntersection[j].ParameterA);
                    }

                    Curve[] tempShatteredCurve = tempExtendedCurve.Split(splitterSet);

                    List<Curve> tempUsableCurve = new List<Curve>();

                    for (int j = 0; j < tempShatteredCurve.Length; j++)
                    {
                        if (j % 2 == 1)
                        {
                            tempUsableCurve.Add(tempShatteredCurve[j]);
                        }
                    }

                    parkingLotCurves.Add(tempUsableCurve);
                }

                //쓸 수 있는 선 2 (5000mm 이상으로 그릴 수 있는 2300mm이상길이의 선분 추출)

                List<List<Curve>> usableParkingLotCurve = new List<List<Curve>>();

                for (int i = 0; i < parkingLotCurves.Count(); i++)
                {
                    List<Curve> tempCurveSet = parkingLotCurves[i];

                    List<Curve> tempUsableParts = new List<Curve>();

                    foreach (Curve j in tempCurveSet)
                    {
                        double tempOffsetDistance = 0;

                        if (parkingLotType[i] == 0)
                            tempOffsetDistance = 5000;
                        else
                            tempOffsetDistance = 2000;

                        Curve[] tempLoftCurves = { j, j.Offset(Plane.WorldXY, tempOffsetDistance, 0, CurveOffsetCornerStyle.None)[0] };

                        Brep tempBrep = Brep.CreateFromLoft(tempLoftCurves, Point3d.Unset, Point3d.Unset, LoftType.Straight, false)[0];

                        Curve[] tempOutCurve;
                        Point3d[] tempOutPoint;

                        Rhino.Geometry.Intersect.Intersection.CurveBrep(boundary, tempBrep, 0.1, out tempOutCurve, out tempOutPoint);

                        List<double> tempSplitDomain = new List<double>();

                        foreach (Curve k in tempOutCurve)
                        {
                            List<double> divideDomain = new List<double>();
                            List<Point3d> dividePts = new List<Point3d>();

                            Polyline tempPLine;
                            k.TryGetPolyline(out tempPLine);
                            Line[] tempSegments = tempPLine.GetSegments();

                            for (int l = 0; l < tempSegments.Length; l++)
                            {
                                for (double m = 0; m < 1; m += 2000 / tempSegments[l].Length)
                                {
                                    dividePts.Add(tempSegments[l].PointAt(m));
                                }
                            }

                            dividePts.Add(k.PointAt(k.Domain.T1));

                            foreach (Point3d l in dividePts)
                            {
                                double tempSplitDomainContent = 0;

                                j.ClosestPoint(l, out tempSplitDomainContent);

                                tempSplitDomain.Add(tempSplitDomainContent);
                            }
                        }

                        Curve[] tempShatteredCurve = j.Split(tempSplitDomain);

                        double tempMinimumLength = 0;

                        if (parkingLotType[i] == 0)
                            tempMinimumLength = 2300;
                        else
                            tempMinimumLength = 6000;

                        foreach (Curve k in tempShatteredCurve)
                        {
                            if (k.GetLength() > tempMinimumLength)
                            {
                                tempUsableParts.Add(j);
                            }
                        }
                    }

                    usableParkingLotCurve.Add(tempUsableParts);
                }

                // 주차창 생성        

                for (int h = 0; h < usableParkingLotCurve.Count(); h++)
                {
                    List<Curve> tempCurve = usableParkingLotCurve[h];

                    if (parkingLotType[h] == 0)
                    {
                        foreach (Curve i in tempCurve)
                        {

                            Vector3d tempVector = new Vector3d(i.PointAt(i.Domain.T0) - i.PointAt(i.Domain.T1));
                            Vector3d tempCopyVector = new Vector3d(tempVector);
                            tempCopyVector.Transform(Transform.Rotation(Math.PI / 2, new Point3d(0, 0, 0)));

                            int tempCount = (int)(i.GetLength() - i.GetLength() % 2300) / 2300;

                            for (int j = 0; j < tempCount; j++)
                            {
                                double tempBaseParameter;
                                i.LengthParameter(2300 * j, out tempBaseParameter);

                                Point3d tempBasePoint = i.PointAt(tempBaseParameter);
                                Plane tempPlane = new Plane(tempBasePoint, tempVector, tempCopyVector);

                                Rectangle3d myRect = new Rectangle3d(tempPlane, -2300, 5000);

                                if (Rhino.Geometry.Intersect.Intersection.CurveCurve(myRect.ToNurbsCurve(), boundary, 0.1, 0.1).Count == 0)
                                {
                                    parkingLots.Add(new ParkingLine(myRect));
                                }
                            }
                        }
                    }
                    else
                    {
                        foreach (Curve i in tempCurve)
                        {
                            Vector3d tempVector = new Vector3d(i.PointAt(i.Domain.T0) - i.PointAt(i.Domain.T1));
                            Vector3d tempCopyVector = new Vector3d(tempVector);
                            tempCopyVector.Transform(Transform.Rotation(Math.PI / 2, new Point3d(0, 0, 0)));

                            int tempCount = (int)(i.GetLength() - i.GetLength() % 6000) / 6000;

                            for (int j = 0; j < tempCount; j++)
                            {
                                double tempBaseParameter;
                                i.LengthParameter(6000 * j, out tempBaseParameter);

                                Point3d tempBasePoint = i.PointAt(tempBaseParameter);
                                Plane tempPlane = new Plane(tempBasePoint, tempVector, tempCopyVector);

                                Rectangle3d myRect = new Rectangle3d(tempPlane, -6000, 2000);

                                if (Rhino.Geometry.Intersect.Intersection.CurveCurve(myRect.ToNurbsCurve(), boundary, 0.1, 0.1).Count == 0)
                                {
                                    parkingLots.Add(new ParkingLine(myRect));
                                }
                            }
                        }
                    }
                }
            }
            return new ParkingLotOnEarth(parkingLots);
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
    }
}
