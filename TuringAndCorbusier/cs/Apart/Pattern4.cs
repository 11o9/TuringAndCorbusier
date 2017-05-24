using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rhino.Geometry;
using System.Collections;
using Rhino.Collections;

namespace TuringAndCorbusier
{
    class AG4 : ApartmentGeneratorBase
    {
        public override Apartment generator(Plot plot, ParameterSet parameterSet, Target target)
        {
            ///////////////////////////////////////////////
            //////////  common initial settings  //////////
            ///////////////////////////////////////////////

            randomCoreType = GetRandomCoreType();

            //입력"값" 부분
            double[] parameters = parameterSet.Parameters;
            double storiesHigh = Math.Max((int)parameters[0], (int)parameters[1]);
            //double storiesLow = Math.Min((int)parameters[0], (int)parameters[1]);
            double width = parameters[2];
            //double angleRadian = parameters[3];
            //double moveFactor = parameters[4];
            Regulation regulationHigh = new Regulation(storiesHigh);
            //Regulation regulationLow = new Regulation(storiesLow);
            List<double> ratio = target.Ratio;
            List<double> area = target.Area.Select(n => n * 1000 * 1000).ToList();
            //double areaLimit = Consts.AreaLimit;
            BuildingType buildingType = regulationHigh.BuildingType;
            List<double> areaLength = new List<double>();
            for (int i = 0; i < area.Count; i++)
            {
                areaLength.Add(area[i] / width);
            }

            ///////////////////////////
            //sort lists before start//
            RhinoList<double> ratioRL = new RhinoList<double>(ratio);
            ratioRL.Sort(area.ToArray());
            ratio = ratioRL.ToList();
            area.Sort();
            ///////////////////////////

            ////////////////////////////////////////
            //////////  maximum polyline  //////////
            ////////////////////////////////////////

            //입력 "대지경계" 부분
            Curve boundary = CommonFunc.adjustOrientation(plot.SimplifiedBoundary);
            Curve[] plotArr = plotArrMaker(boundary);

            //basic settings
            Polyline centerpolyline = maxPolylineFinder(plot, regulationHigh, plotArr, parameterSet);
            Curve centerline = centerpolyline.ToNurbsCurve();
            if (centerline == null)
                return new Apartment(plot);
            if (centerline.GetLength() > plot.Boundary.GetLength())
                return new Apartment(plot);

            double wholeL = centerpolyline.Length;
            double coreWidth = randomCoreType.GetWidth();
            Curve[] segments = centerline.DuplicateSegments();
            List<Line> lines = new List<Line>();
            List<Point3d> toMidline = new List<Point3d>();
            for (int i = 0; i < 3; i++)
            {
                Line l = new Line(segments[i].PointAtStart, segments[i].PointAtEnd);
                lines.Add(l);
            }

            //calculating total house numbers, surplus length, typeNum
            List<int> unallocated = unallocatedMaker(ratio, area, areaLength, coreWidth, wholeL);

            //stretch
            double sourceL = 0;
            for (int i = 0; i < unallocated.Count; i++)
            {
                sourceL += areaLength[unallocated[i]];
            }
            double targetL = wholeL - (int)((unallocated.Count + 1) / 2) * coreWidth;
            double stretchRatio = targetL / sourceL;
            List<double> stretchedLength = new List<double>();
            for (int i = 0; i < ratio.Count; i++)
            {
                stretchedLength.Add(areaLength[i] * stretchRatio);
            }

            //household properties
            //household statistics
            //core properties
            List<List<List<Household>>> household = new List<List<List<Household>>>();
            List<List<HouseholdStatistics>> householdStatistics = new List<List<HouseholdStatistics>>();
            List<List<Core>> core = new List<List<Core>>();
            coreAndHouses(unallocated, stretchedLength, target, parameterSet, lines, centerline, out household, out householdStatistics, out core);

            //building outlines
            List<List<Curve>> buildingOutlines = buildingOutlineMakerAG4(centerline, width);


            //parking lot
            //parkingline 

            List<Curve> centerSegments = centerline.DuplicateSegments().ToList();
            ParkingLotOnEarth parkingLotOnEarth = new ParkingLotOnEarth();
            #region GetParking
            //rotation check

            Polyline testLine = new Polyline(centerpolyline);
            testLine.Add(testLine[0]);
            Curve testCurve = testLine.ToNurbsCurve();

            if (testCurve.ClosedCurveOrientation(Plane.WorldXY) != CurveOrientation.CounterClockwise)
            {
                testLine.Reverse();
                testLine.RemoveAt(0);
            }
            else
            {
                testLine.RemoveAt(testLine.Count - 1);
            }
            //정렬 된 커브
            List<Curve> alignCurve = testLine.ToNurbsCurve().DuplicateSegments().ToList();
            //길이 수정
            alignCurve[0] = new LineCurve(alignCurve[0].PointAtStart, alignCurve[0].PointAtLength(alignCurve[0].GetLength() - width / 2));
            //중간커브 돌림
            alignCurve[1] = new LineCurve(alignCurve[1].PointAtLength(alignCurve[1].GetLength()-width/2), alignCurve[1].PointAtLength(width/2));
            alignCurve[2] = new LineCurve(alignCurve[2].PointAtLength(width / 2), alignCurve[2].PointAtEnd);

            for (int i = 0; i < alignCurve.Count; i++)
            {
                //each settings
                //coredepth
                double coreDepth = randomCoreType.GetDepth();
                //parkinglines
                //1. 건물 밑 라인
                List<Curve> parkingCurve = new List<Curve>() { alignCurve[i] };
                //2. 외부 라인
                List<Curve> parkingCurve2 = new List<Curve>() { alignCurve[i].DuplicateCurve() };
                Vector3d setBack = alignCurve[i].TangentAtStart;
                setBack.Rotate(-Math.PI / 2, Vector3d.ZAxis);
                parkingCurve.ForEach(n => n.Translate(setBack * coreDepth / 2));
                parkingCurve2.ForEach(n => n.Translate(-setBack * (coreDepth / 2 + 6000)));
                //distance
                double lineDistance = centerSegments.Max(n => n.GetLength()) * 1.3;
                //obstacles
                List<Curve> obstacles = core[0].Select(n => n.DrawOutline(width)).ToList();
                for (int j = 0; j < obstacles.Count; j++)
                {
                    obstacles[j].Translate(core[0][j].YDirection * (core[0][j].CoreType.GetDepth() - width) / 2);
                }
                if (i == 1)
                {
                    //도로확보
                    Vector3d roadDir = alignCurve[i].TangentAtStart;
                    roadDir.Rotate(Math.PI / 2, Vector3d.ZAxis);
                    Curve leftRoad = new PolylineCurve(new Point3d[] {
                    centerSegments[1].PointAtStart + roadDir * (width/2 + 1),
                    centerSegments[1].PointAtLength(6000+width/2) + roadDir * (width/2 + 1),
                    centerSegments[1].PointAtLength(6000+width/2) + roadDir * lineDistance,
                    centerSegments[1].PointAtStart + roadDir * lineDistance,
                    centerSegments[1].PointAtStart + roadDir * (width/2 + 1) });

                    Curve rightRoad = new PolylineCurve(new Point3d[] {
                    centerSegments[1].PointAtEnd + roadDir * (width/2 + 1),
                    centerSegments[1].PointAtLength(centerSegments[1].GetLength() - 6000 - width / 2) + roadDir * (width/2 + 1),
                    centerSegments[1].PointAtLength(centerSegments[1].GetLength() - 6000 - width / 2) + roadDir * lineDistance,
                    centerSegments[1].PointAtEnd + roadDir * lineDistance,
                    centerSegments[1].PointAtEnd + roadDir * (width/2 + 1)});

                    List<Curve> roads = new List<Curve>() { leftRoad, rightRoad };
                    obstacles.AddRange(roads);
                }

                ParkingModule pm1 = new ParkingModule();
                pm1.ParkingLines = parkingCurve;
                pm1.Boundary = plot.Boundary;
                pm1.UseInnerLoop = false;
                pm1.Obstacles = obstacles;
                pm1.LineType = ParkingLineType.SingleLine;
                pm1.Distance = 12000; // 직각 한줄 들어갈 폭
                pm1.CoreDepth = coreDepth;
                ParkingLotOnEarth underAptLine = pm1.GetParking();

                ParkingModule pm2 = new ParkingModule();
                pm2.ParkingLines = parkingCurve2;
                pm2.Boundary = plot.Boundary;
                pm2.Obstacles = obstacles;
                pm2.Distance = lineDistance;
                pm2.CoreDepth = coreDepth;
                ParkingLotOnEarth overGround = pm2.GetParking();

                parkingLotOnEarth.ParkingLines.AddRange(underAptLine.ParkingLines);
                parkingLotOnEarth.ParkingLines.AddRange(overGround.ParkingLines);
            }
            #endregion
            ParkingLotUnderGround parkingLotUnderGround = new ParkingLotUnderGround();

            List<Curve> aptLines = new List<Curve>();
            aptLines.Add(centerline);

            Apartment result = new Apartment(this.GetAGType, plot, buildingType, parameterSet, target, core, household, parkingLotOnEarth, parkingLotUnderGround, buildingOutlines, aptLines);
            return result;


        }

       
        #region GA Sttings
        ///////////////////////////////////////////
        //////////  GA initial settings  //////////
        ///////////////////////////////////////////

        //Parameter 최소값 ~ 최대값 {storiesHigh, storiesLow, width, angle, moveFactor}
        private double[] minInput = { 3, 3, CoreType.Vertical.GetDepth(), 0, 0 };
        private double[] maxInput = { 7, 7, CoreType.Vertical.GetDepth() + 1000, 2 * Math.PI, 1 };

        //Parameter GA최적화 {mutation probability, elite percentage, initial boost, population, generation, fitness value, mutation factor(0에 가까울수록 변동 범위가 넓어짐)
        private double[] GAparameterset = { 0.8, 0.05, 1, 20, 4, 10, 1 };
        //private double[] GAparameterset = { 0.2, 0.03, 1, 100, 5, 3, 1 };

        public override string GetAGType
        {
            get
            {
                return "PT-4";
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
            CoreType[] tempCoreTypes = { CoreType.Vertical };
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
        #endregion GA Sttings

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
                plotArrExtended[i] = plotArrExtended[i].Extend(CurveEnd.Both, plot.Boundary.GetBoundingBox(false).Diagonal.Length*1000, CurveExtensionStyle.Line);
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


        #region MaxPoly
        ////////////////////////////////////////
        //////////  maximum polyline  //////////
        ////////////////////////////////////////

        private Polyline maxPolylineFinder(Plot plot, Regulation regulationHigh, Curve[] plotArr, ParameterSet parameterSet)
        {
            //initial parameters
            double alpha = 1;
            int token = 0;
            int iterations = 3;
            double aptWidth = parameterSet.Parameters[2];

            double bestAngle = 0;
            double bestGap = 0.5;
            List<double> bestFold = new List<double>(new double[] { 0, 1 });
            double bestLen = 0;
            Polyline bestPolyline = new Polyline();

            //start
            while (token < iterations)
            {
                List<Curve> lines = new List<Curve>();

                List<double> angles = angleGen(7, 5, token, bestAngle, plot.Boundary);
                List<double> gaps = gapGen(10, 4, token, bestGap);
                List<double> folds = foldGen(10, 4, token, bestFold);

                //iteration start - angle
                for (int i = 0; i < angles.Count; i++)
                {
                    //법규 : 대지 안의 공지
                    Curve[] surroundingsHigh = regulationHigh.fromSurroundingsCurve(plot);
                    //Curve surroundingsLow = Regulation.fromSurroundingsCurve(plot, regulationLow, plotArr);

                    //법규 : 일조에 의한 높이제한
                    Curve[] northHigh = regulationHigh.fromNorthCurve(plot);
                    //Curve northLow = Regulation.fromNorthCurve(plot, regulationLow, plotArr);

                    //법규 : 인접대지경계선(채광창)
                    Curve lighting1 = byLightingCurve(plot, regulationHigh, plotArr, angles[i]);
                    Curve lighting2 = byLightingCurve(plot, regulationHigh, plotArr, angles[i] + Math.PI / 2);
                    Curve lightingHigh = CommonFunc.joinRegulations(lighting1, lighting2);

                    //일부 조건(대지안의공지, 일조에 의한 높이제한)을 만족하는 경계선
                    //모든 조건(대지안의공지, 일조에의한 높이제한, 인접대지경계선)을 만족하는 경계선
                    Curve partialRegulationHigh = CommonFunc.joinRegulations(surroundingsHigh[0], northHigh[0]);
                    Curve wholeRegulationHigh = CommonFunc.joinRegulations(partialRegulationHigh, lightingHigh);

                    Curve outline = wholeRegulationHigh;

                    //make boundingbox
                    Vector3d xVec = Vector3d.XAxis;
                    xVec.Rotate(angles[i], Vector3d.ZAxis);
                    Vector3d yVec = Vector3d.XAxis;
                    yVec.Rotate(angles[i] + Math.PI / 2, Vector3d.ZAxis);

                    Box box;
                    outline.GetBoundingBox(new Plane(Point3d.Origin, xVec, yVec), out box);
                    Point3d ori = box.GetCorners()[0];
                    Point3d xP = box.GetCorners()[1];
                    Point3d dia = box.GetCorners()[2];
                    Point3d yP = box.GetCorners()[3];

                    double xLen = ori.DistanceTo(xP) + alpha;
                    double yLen = ori.DistanceTo(yP) - aptWidth - alpha;
                    Point3d lineP1 = new Point3d(ori + yVec * (aptWidth + alpha) / 2 - xVec * alpha / 2);
                    Point3d lineP2 = new Point3d(lineP1 + xVec * xLen);

                    //iteration start - line gap
                    for (int j = 0; j < gaps.Count; j++)
                    {
                        //get segments
                        Curve baseLine = new Line(new Point3d(lineP1 + yVec * yLen * gaps[j]),
                          new Point3d(lineP2 + yVec * yLen * gaps[j])).ToNurbsCurve();

                        Curve splitter = new Rectangle3d(new Plane(Point3d.Origin, xVec, yVec),
                          new Point3d(lineP1 + yVec * yLen * gaps[j] - yVec * aptWidth / 2),
                          new Point3d(lineP2 + yVec * yLen * gaps[j] + yVec * aptWidth / 2)).ToNurbsCurve();

                        List<Curve> segs = splitCurve(outline, splitter);

                        //find segment end parameters
                        List<Point3d> segPoints = new List<Point3d>();
                        for (int k = 0; k < segs.Count; k++)
                        {
                            Polyline polyTemp;
                            segs[k].TryGetPolyline(out polyTemp);
                            segPoints.AddRange(new List<Point3d>(polyTemp));
                        }

                        List<double> segParams = new List<double>();
                        for (int k = 0; k < segPoints.Count; k++)
                        {
                            double t;
                            baseLine.ClosestPoint(segPoints[k], out t);
                            segParams.Add(t);
                        }

                        //find segments inside boundary
                        segParams = segParams.Distinct().ToList();
                        segParams.Sort();
                        List<Curve> usableSegs = new List<Curve>();

                        for (int k = 0; k < segParams.Count - 1; k++)
                        {
                            Curve segTemp = baseLine.Trim(segParams[k], segParams[k + 1]);
                            if (segTemp.GetLength() < aptWidth + alpha)
                                continue;
                            Vector3d segVec = segTemp.TangentAtStart;
                            segTemp = new Line(segTemp.PointAtLength(aptWidth / 2 + alpha / 2),
                              segTemp.PointAtLength(segTemp.GetLength() - aptWidth / 2 - alpha / 2)).ToNurbsCurve();

                            Curve segRect = new Rectangle3d(new Plane(Point3d.Origin, xVec, yVec),
                              new Point3d(segTemp.PointAtStart - yVec * aptWidth / 2),
                              new Point3d(segTemp.PointAtEnd + yVec * aptWidth / 2)).ToNurbsCurve();

                            //iteration start - 2 folds
                            if (Rhino.Geometry.Intersect.Intersection.CurveCurve(outline, segRect, 0, 0).Count == 0
                              && outline.Contains(segTemp.PointAtEnd) == Rhino.Geometry.PointContainment.Inside)
                            {
                                Line line = new Line(segTemp.PointAtStart, segTemp.PointAtEnd);

                                List<double> tempFolds = new List<double>();
                                Polyline tempPolyline = maxPolyline(aptWidth, outline, line, folds, regulationHigh, out tempFolds);
                                double tempLen = tempPolyline.ToNurbsCurve().GetLength();
                                if (tempLen > bestLen)
                                {
                                    bestAngle = angles[i];
                                    bestGap = gaps[j];
                                    bestFold = new List<double>(tempFolds);
                                    bestLen = tempLen;
                                    bestPolyline = new Polyline(tempPolyline);
                                }
                            }
                        }

                        lines.AddRange(usableSegs);
                    }
                }

                token++;
            }

            return bestPolyline;
        }

        private List<double> angleGen(int angleResIni, int angleRes, int iter, double bestAngle, Curve outline)
        {
            //initial parameters
            double searchBoundary = 2 * Math.PI / Math.Pow(angleRes, iter);
            if (iter > 0)
                searchBoundary *= angleRes / (double)angleResIni;

            int res = angleResIni;
            if (iter > 1)
                res = angleRes;

            List<double> angles = new List<double>();

            //generate angles
            for (int i = 0; i < res + 1; i++)
            {
                angles.Add(bestAngle + searchBoundary * (-1 / 2 + i / (double)res));
            }

            //additional options
            if (iter == 0)
            {
                Curve[] outlineSegs = outline.DuplicateSegments();
                for (int i = 0; i < outlineSegs.Length; i++)
                {
                    angles.Add(getAngle(Vector3d.XAxis, outlineSegs[i].TangentAtStart));
                }
            }

            return angles;
        }

        private List<double> gapGen(int gapResIni, int gapRes, int iter, double bestGap)
        {
            //initial parameters
            double searchBoundary = 1 / Math.Pow(gapRes, iter);
            if (iter > 0)
                searchBoundary *= gapRes / (double)gapResIni;

            int res = gapResIni;
            if (iter > 1)
                res = gapRes;

            List<double> gaps = new List<double>();

            //generate gaps
            for (int i = 0; i < res + 1; i++)
            {
                gaps.Add(bestGap + searchBoundary * (-1 / 2 + i / (double)res));
            }

            //additional options
            for (int i = gaps.Count - 1; i >= 0; i--)
            {
                if (gaps[i] > 1 || gaps[i] < 0)
                    gaps.RemoveAt(i);
            }

            return gaps;
        }

        private List<double> foldGen(int foldResIni, int foldRes, int iter, List<double> bestFolds)
        {
            //initial parameters
            double searchBoundary = 1 / Math.Pow(foldRes, iter);
            if (iter > 0)
                searchBoundary *= foldRes / (double)foldResIni;

            int res = foldResIni;
            if (iter > 1)
                res = foldRes;

            List<double> folds = new List<double>();

            //generate folds
            for (int i = 0; i < res + 1; i++)
            {
                folds.Add(bestFolds[0] + searchBoundary * (-1 / 2 + i / (double)res));
                folds.Add(bestFolds[1] + searchBoundary * (-1 / 2 + i / (double)res));
            }

            if (iter == 0)
            {
                folds.Clear();
                for (int i = 0; i <= res; i++)
                {
                    folds.Add(i / (double)res);
                }
            }

            //additional options
            for (int i = folds.Count - 1; i >= 0; i--)
            {
                if (folds[i] > 1 || folds[i] < 0)
                    folds.RemoveAt(i);
            }

            return folds;
        }
        #endregion MaxPoly

        private Polyline maxPolyline(double aptWidth, Curve outline, Line line, List<double> folds, Regulation regulation, out List<double> tempFolds)
        {
            tempFolds = new List<double>(new double[] { 0, 0 });

            //divide line and get points
            double[] lineParam = folds.ToArray();

            Point3d[] pts = lineParam.Select(n => line.PointAt(n)).ToArray();

            //vertical lines
            Vector3d norm = line.Direction;
            norm.Rotate(Math.PI / 2, Vector3d.ZAxis);
            norm.Unitize();
            Vector3d dir = line.Direction;
            dir.Unitize();

            List<double> verticalDists = new List<double>();
            List<Point3d> verticalPoints = new List<Point3d>();

            for (int i = 0; i < pts.Length; i++)
            {
                double h = outline.GetBoundingBox(false).Diagonal.Length;
                Curve splitterPos = new Rectangle3d(new Plane(pts[i], dir, norm),
                  new Point3d(pts[i] + norm * h + dir * aptWidth / 2),
                  new Point3d(pts[i] - dir * aptWidth / 2)).ToNurbsCurve();
                Curve splitterNeg = new Rectangle3d(new Plane(pts[i], dir, norm),
                  new Point3d(pts[i] + dir * aptWidth / 2),
                  new Point3d(pts[i] - norm * h - dir * aptWidth / 2)).ToNurbsCurve();

                List<Curve> segsPos = splitCurve(outline, splitterPos);
                List<Curve> segsNeg = splitCurve(outline, splitterNeg);
                List<Point3d> segPointsPos = new List<Point3d>();
                List<Point3d> segPointsNeg = new List<Point3d>();

                for (int j = 0; j < segsPos.Count; j++)
                {
                    Polyline polyTemp;
                    segsPos[j].TryGetPolyline(out polyTemp);
                    segPointsPos.AddRange(new List<Point3d>(polyTemp));
                }
                for (int j = 0; j < segsNeg.Count; j++)
                {
                    Polyline polyTemp;
                    segsNeg[j].TryGetPolyline(out polyTemp);
                    segPointsNeg.AddRange(new List<Point3d>(polyTemp));
                }

                double[] distsPos = segPointsPos.Select(n => new Vector3d(n - pts[i]) * norm).ToArray();
                double[] distsNeg = segPointsNeg.Select(n => new Vector3d(n - pts[i]) * norm).ToArray();

                double[] selectedDists = new double[] { distsPos.Min(), distsNeg.Max() };
                double maxDist = selectedDists[0];
                if (Math.Abs(selectedDists[0]) < Math.Abs(selectedDists[1]))
                    maxDist = selectedDists[1];
                verticalDists.Add(maxDist);
                verticalPoints.Add(new Point3d(pts[i] + maxDist * norm));
            }

            //max polyline
            double polyLength = 0;
            Polyline maxPolyline = new Polyline();
            for (int i = 0; i < pts.Length; i++)
            {
                for (int j = 0; j < i; j++)
                {
                    double tempLength = pts[i].DistanceTo(pts[j])
                      + Math.Abs(verticalDists[i]) + Math.Abs(verticalDists[j]);

                    bool windowDistOK = pts[i].DistanceTo(pts[j]) > regulation.DistanceLL+aptWidth;
                    bool regulationOK = (Math.Sign(verticalDists[i]) != Math.Sign(verticalDists[j])) || windowDistOK;
                    if (regulationOK && tempLength > polyLength)
                    {
                        polyLength = tempLength;

                        List<Point3d> polypts = new List<Point3d>();
                        polypts.Add(verticalPoints[i]);
                        polypts.Add(pts[i]);
                        polypts.Add(pts[j]);
                        polypts.Add(verticalPoints[j]);

                        tempFolds[0] = folds[i];
                        tempFolds[1] = folds[j];

                        maxPolyline = new Polyline(polypts);
                    }
                    else
                    {
                        int longerInd = i;
                        int shorterInd = j;
                        if (Math.Abs(verticalDists[i]) < Math.Abs(verticalDists[j]))
                        {
                            longerInd = j;
                            shorterInd = i;
                        }
                        tempLength -= Math.Abs(verticalDists[shorterInd]);

                        if (tempLength > polyLength)
                        {
                            polyLength = tempLength;

                            List<Point3d> polypts = new List<Point3d>();
                            polypts.Add(verticalPoints[longerInd]);
                            polypts.Add(pts[longerInd]);
                            polypts.Add(pts[shorterInd]);

                            tempFolds[0] = folds[i];
                            tempFolds[1] = folds[j];

                            maxPolyline = new Polyline(polypts);
                        }

                    }
                }
            }

            return maxPolyline;
        }

        private double getAngle(Vector3d baseVec, Vector3d targetVec)
        {
            double angle = Vector3d.VectorAngle(baseVec, targetVec);
            angle *= Math.Sign(Vector3d.CrossProduct(baseVec, targetVec).Z);
            return angle;
        }

        private List<Curve> splitCurve(Curve baseCurve, Curve splitter)
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

        private List<double> valMapper(List<Line> lines, List<double> source)
        {
            double a = lines[0].Length;
            double b = lines[1].Length;
            double c = lines[2].Length;
            double d = 0;
            int n = 0;


            List<double> target = new List<double>();
            for (int i = 0; i < source.Count; i++)
            {
                if (source[i] > a + b)
                {
                    n = 2;
                    d = (source[i] - (a + b)) / c;
                }
                else if (source[i] < a)
                {
                    n = 0;
                    d = source[i] / a;
                }
                else
                {
                    n = 1;
                    d = (source[i] - a) / b;
                }

                target.Add(n + d);

            }
            return target;
        }
        private List<int> unallocatedMaker(List<double> ratio, List<double> area, List<double> areaLength, double coreWidth, double wholeL)
        {
            List<double> tempTypeNum = new List<double>();
            double areaTimesRatioSum = 0;
            double ratioSum = 0;
            for (int i = 0; i < area.Count; i++)
            {
                areaTimesRatioSum += areaLength[i] * ratio[i];
            }
            for (int i = 0; i < area.Count; i++)
            {
                ratioSum += ratio[i];
            }
            double smallestTypeNum = ratio[0] * (wholeL - coreWidth) * 2 / (2 * areaTimesRatioSum + coreWidth * ratioSum);
            tempTypeNum.Add(smallestTypeNum);
            for (int i = 1; i < area.Count; i++)
            {
                double typeHouseNum = smallestTypeNum * ratio[i] / ratio[0];
                tempTypeNum.Add(typeHouseNum);
            }

            List<int> tempStackTypeNum = new List<int>();
            tempStackTypeNum.Add(0);
            double stack = 0;
            for (int i = 0; i < ratio.Count; i++)
            {
                stack += tempTypeNum[i];
                tempStackTypeNum.Add((int)stack);
            }

            List<int> typeNum = new List<int>();
            for (int i = 0; i < ratio.Count; i++)
            {
                typeNum.Add(tempStackTypeNum[i + 1] - tempStackTypeNum[i]);
            }


            //make a list of unallocated houses
            List<int> unallocated = new List<int>();
            for (int i = 0; i < typeNum.Count; i++)
            {
                for (int j = 0; j < typeNum[i]; j++)
                {
                    unallocated.Add(i);
                }
            }
            unallocated.Reverse();
            return unallocated;
        }
        private void coreAndHouses(List<int> unallocated, List<double> stretchedLength, Target target, ParameterSet parameterSet, List<Line> lines, Curve centerline, out List<List<List<Household>>> household, out List<List<HouseholdStatistics>> householdStatistics, out List<List<Core>> core)
        {
            double storiesHigh = Math.Max((int)parameterSet.Parameters[0], (int)parameterSet.Parameters[1]);
            double coreWidth = randomCoreType.GetWidth();
            double width = parameterSet.Parameters[2];

            List<double> houseEndVals = new List<double>();
            List<Point3d> houseEndPoints = new List<Point3d>();
            List<Vector3d> houseEndPerps = new List<Vector3d>();

            List<double> coreEndVals = new List<double>();
            List<Point3d> coreEndPoints = new List<Point3d>();
            List<Vector3d> coreEndPerps = new List<Vector3d>();

            List<int> coreType = new List<int>();
            List<Point3d> corePoint = new List<Point3d>();
            List<Vector3d> coreVecX = new List<Vector3d>();
            List<Vector3d> coreVecY = new List<Vector3d>();

            List<Point3d> homeOri = new List<Point3d>();
            List<Vector3d> homeVecX = new List<Vector3d>();
            List<Vector3d> homeVecY = new List<Vector3d>();

            List<double> xa = new List<double>();
            List<double> xb = new List<double>();
            List<double> ya = new List<double>();
            List<double> yb = new List<double>();

            List<Line> endlines = new List<Line>();
            List<Line> endlinesCore = new List<Line>();

            List<List<double>> wallFactors = new List<List<double>>();
            List<double> exclusiveArea = new List<double>();


            ////////// home shape type //////////
            //////////     0 : long    //////////
            //////////    1 : corner   //////////

            List<int> homeShapeType = new List<int>();

            /////////////////////////////////////

            int counter = 0;
            double trigger = 1;
            int clearCorner = 1;
            double narrowEnd = 1200;
            double cornerThreshold = 1500;

            while (counter < unallocated.Count && (trigger != 0 || clearCorner != 0))
            {
                //initialize
                houseEndVals.Clear();
                houseEndPoints.Clear();
                houseEndPerps.Clear();

                coreType.Clear();
                corePoint.Clear();
                coreVecX.Clear();
                coreVecY.Clear();

                homeOri.Clear();
                homeVecX.Clear();
                homeVecY.Clear();

                xa.Clear();
                xb.Clear();
                ya.Clear();
                yb.Clear();

                homeShapeType.Clear();
                wallFactors.Clear();
                exclusiveArea.Clear();

                counter += 1;
                trigger = 1;
                clearCorner = 0;

                //start

                double val = 0;
                houseEndVals.Add(val);
                for (int i = 0; i < unallocated.Count; i++)
                {
                    val += stretchedLength[unallocated[i]];
                    houseEndVals.Add(val);
                    if (i % 2 == 0 && coreWidth != 0)
                    {
                        val += coreWidth;
                        houseEndVals.Add(val);
                    }
                }

                for (int i = 0; i < houseEndVals.Count; i++)
                {
                    if ((lines[0].Length - cornerThreshold < houseEndVals[i] && lines[0].Length + cornerThreshold > houseEndVals[i]) || ((lines[0].Length + lines[1].Length) - cornerThreshold < houseEndVals[i] && (lines[0].Length + lines[1].Length) + cornerThreshold > houseEndVals[i]))
                    {
                        clearCorner += 1;
                    }
                }
                List<double> mappedVals = new List<double>(valMapper(lines, houseEndVals));
                for (int i = 0; i < mappedVals.Count; i++)
                {
                    houseEndPoints.Add(centerline.PointAt(mappedVals[i]));
                    houseEndPerps.Add(centerline.TangentAt(mappedVals[i]));
                }

                int counter2 = 0;
                for (int i = 0; i < mappedVals.Count - 1; i++)
                {
                    if (i % 3 == 1 && (int)mappedVals[i] != (int)mappedVals[i + 1])
                    {
                        counter2 += 1;
                    }
                }
                for (int i = 0; i < mappedVals.Count - 1; i++)
                {

                    if ((int)mappedVals[i] == (int)(mappedVals[(i + 1)] - 0.001))
                    {
                        double avrg = (mappedVals[i] + mappedVals[(i + 1)]) / 2;
                        Point3d midOri = centerline.PointAt(avrg);
                        Point3d sOri = centerline.PointAt(mappedVals[i]);
                        Point3d eOri = centerline.PointAt(mappedVals[i + 1]);
                        Vector3d midVec = centerline.TangentAt(avrg);
                        Vector3d verticalVec = midVec;
                        verticalVec.Rotate(Math.PI / 2, new Vector3d(0, 0, 1));
                        midOri.Transform(Transform.Translation(Vector3d.Multiply(verticalVec, width / 2)));
                        sOri.Transform(Transform.Translation(Vector3d.Multiply(verticalVec, width / 2)));
                        eOri.Transform(Transform.Translation(Vector3d.Multiply(verticalVec, width / 2)));
                        if (i % 3 == 1)
                        {
                            corePoint.Add(sOri);
                            coreVecX.Add(Vector3d.Multiply(midVec, 1));
                            coreVecY.Add(Vector3d.Multiply(verticalVec, -1));
                        }
                        else
                        {
                            if (i % 3 == 0)
                            {
                                homeOri.Add(eOri);
                            }
                            else
                            {
                                homeOri.Add(sOri);
                            }

                            homeVecX.Add(Vector3d.Multiply(midVec, (i % 3) - 1));
                            homeVecY.Add(verticalVec);
                            xa.Add(sOri.DistanceTo(eOri));
                            xb.Add(0);
                            ya.Add(width);
                            yb.Add(0);
                            homeShapeType.Add(0);
                            wallFactors.Add(new List<double>(new double[] { 1, 1, 1, 0.5 }));
                            exclusiveArea.Add(exclusiveAreaCalculatorAG4Edge(xa[xa.Count - 1], xb[xb.Count - 1], ya[ya.Count - 1], yb[yb.Count - 1], Consts.balconyDepth));
                        }
                    }
                    else
                    {
                        Point3d checkP = lines[(int)mappedVals[i]].PointAt(1);
                        Vector3d v1 = new Vector3d(houseEndPoints[i] - checkP);
                        Vector3d v2 = new Vector3d(houseEndPoints[(i + 1) % mappedVals.Count] - checkP);
                        double l1 = v1.Length;
                        double l2 = v2.Length;
                        if (Math.Max(l1, l2) < width / 2)
                        {
                            counter2 += 1;
                        }

                        v1.Unitize();
                        v2.Unitize();
                        checkP.Transform(Transform.Translation(Vector3d.Add(Vector3d.Multiply(v1, width / 2), Vector3d.Multiply(v2, width / 2))));
                        homeOri.Add(checkP);
                        homeShapeType.Add(1);
                        if (i % 3 == 2)
                        {
                            v1.Reverse();
                            homeVecX.Add(v1);
                            homeVecY.Add(v2);

                            xa.Add(l1 + width / 2);
                            xb.Add(l1 - width / 2);
                            ya.Add(l2 + width / 2);
                            yb.Add(l2 - width / 2);

                            if (l1 - width / 2 < narrowEnd)
                            {
                                counter2 += 1;
                            }
                        }
                        else
                        {
                            v2.Reverse();
                            homeVecX.Add(v2);
                            homeVecY.Add(v1);

                            xa.Add(l2 + width / 2);
                            xb.Add(l2 - width / 2);
                            ya.Add(l1 + width / 2);
                            yb.Add(l1 - width / 2);

                            if (l2 - width / 2 < narrowEnd)
                            {
                                counter2 += 1;
                            }
                        }

                        if (l2 - width / 2 > 0)
                        {
                            wallFactors.Add(new List<double>(new double[] { 1, 1, 1, 1, 0.5, 1 }));
                        }
                        else if (l2 - width / 2 == 0)
                        {
                            wallFactors.Add(new List<double>(new double[] { 1, 1, 1, 1, 1 }));
                        }
                        else
                        {
                            wallFactors.Add(new List<double>(new double[] { 0, 1, 1, 1, 0.5, 1 }));
                        }
                        exclusiveArea.Add(exclusiveAreaCalculatorAG4Edge(xa[xa.Count - 1], xb[xb.Count - 1], ya[ya.Count - 1], yb[yb.Count - 1], Consts.balconyDepth));
                    }
                }
                if (counter2 == 0)
                {
                    trigger = 0;
                }
                if (trigger != 0 || clearCorner != 0)
                {
                    unallocated.Add(unallocated[0]);
                    unallocated.RemoveAt(0);
                }
            }

            if (counter == unallocated.Count)
            {
                corePoint.Clear();
                coreVecX.Clear();
                coreVecY.Clear();

                homeOri.Clear();
                homeVecX.Clear();
                homeVecY.Clear();

                xa.Clear();
                xb.Clear();
                ya.Clear();
                yb.Clear();

                homeShapeType.Clear();
                wallFactors.Clear();
                exclusiveArea.Clear();
            }

            //windows
            List<List<Line>> winBuilding = new List<List<Line>>();
            for (int i = 0; i < homeShapeType.Count; i++)
            {
                List<Line> winHouse = new List<Line>();
                if (homeShapeType[i] == 1)
                {
                    Point3d winPt1 = homeOri[i];
                    winPt1.Transform(Transform.Translation(Vector3d.Multiply(homeVecX[i], -xb[i])));
                    winPt1.Transform(Transform.Translation(Vector3d.Multiply(homeVecY[i], yb[i] - ya[i])));
                    Point3d winPt2 = winPt1;
                    winPt2.Transform(Transform.Translation(Vector3d.Multiply(homeVecX[i], xa[i])));
                    Point3d winPt3 = winPt2;
                    Point3d winPt4 = winPt2;
                    winPt4.Transform(Transform.Translation(Vector3d.Multiply(homeVecY[i], ya[i])));
                    winHouse.Add(new Line(winPt1, winPt2));
                    winHouse.Add(new Line(winPt3, winPt4));
                }
                else
                {
                    Point3d winPt1 = homeOri[i];
                    winPt1.Transform(Transform.Translation(Vector3d.Multiply(homeVecY[i], yb[i])));
                    winPt1.Transform(Transform.Translation(Vector3d.Multiply(homeVecX[i], xa[i] - xb[i])));
                    Point3d winPt2 = winPt1;
                    winPt2.Transform(Transform.Translation(Vector3d.Multiply(homeVecX[i], -xa[i])));
                    Point3d winPt3 = winPt1;
                    winPt3.Transform(Transform.Translation(Vector3d.Multiply(homeVecY[i], -ya[i])));
                    Point3d winPt4 = winPt3;
                    winPt4.Transform(Transform.Translation(Vector3d.Multiply(homeVecX[i], -xa[i])));
                    ////////그런데, 복도 방향도 채광창 방향이라고 할 수 있을까?
                    winHouse.Add(new Line(winPt1, winPt2));
                    ////////
                    winHouse.Add(new Line(winPt3, winPt4));
                }
                winBuilding.Add(winHouse);
            }

            //entrance

            List<Point3d> entBuilding = new List<Point3d>();
            for (int i = 0; i < homeShapeType.Count; i++)
            {
                entBuilding.Add(Point3d.Origin);
            }

            //household properties
            household = new List<List<List<Household>>>();
            List<Household> hhpS = new List<Household>();

            for (int i = 0; i < homeOri.Count; i++)
            {
                hhpS.Add(new Household(homeOri[i], homeVecX[i], homeVecY[i], xa[i], xb[i], ya[i], yb[i], unallocated[i], exclusiveArea[i], winBuilding[i], entBuilding[i], wallFactors[i]));
            }

            for (int j = 0; j < storiesHigh; j++)
            {
                List<List<Household>> hhpB = new List<List<Household>>();
                List<Household> hhpSTemp = new List<Household>();
                for (int i = 0; i < hhpS.Count; i++)
                {
                    Household hhp = hhpS[i];
                    Point3d ori = hhp.Origin;
                    Point3d ent = hhp.EntrancePoint;
                    ori.Transform(Transform.Translation(Vector3d.Multiply(Consts.PilotiHeight + Consts.FloorHeight * j, Vector3d.ZAxis)));
                    ent.Transform(Transform.Translation(Vector3d.Multiply(Consts.PilotiHeight + Consts.FloorHeight * j, Vector3d.ZAxis)));
                    List<Line> win = hhp.LightingEdge;
                    List<Line> winNew = new List<Line>();
                    for (int k = 0; k < win.Count; k++)
                    {
                        Line winTemp = win[k];
                        winTemp.Transform(Transform.Translation(Vector3d.Multiply(Consts.PilotiHeight + Consts.FloorHeight * j, Vector3d.ZAxis)));
                        winNew.Add(winTemp);
                    }

                    hhpSTemp.Add(new Household(ori, hhp.XDirection, hhp.YDirection, hhp.XLengthA, hhp.XLengthB, hhp.YLengthA, hhp.YLengthB, hhp.HouseholdSizeType, hhp.GetExclusiveArea(), winNew, ent, hhp.WallFactor));
                }
                hhpB.Add(hhpSTemp);
                household.Add(hhpB);
            }

            //hhpB.Add(hhpS);
          


            //household statistics
            householdStatistics = new List<List<HouseholdStatistics>>();
            List<List<Household>> cornerProperties = new List<List<Household>>();
            List<List<Household>> edgeProperties = new List<List<Household>>();
            for (int i = 0; i < target.Area.Count; i++)
            {
                cornerProperties.Add(new List<Household>());
                edgeProperties.Add(new List<Household>());
                householdStatistics.Add(new List<HouseholdStatistics>());
            }

            for (int i = 0; i < homeShapeType.Count; i++)
            {
                if (homeShapeType[i] == 0)
                {
                    edgeProperties[unallocated[i]].Add(household[0][0][i]);
                }
                else
                {
                    cornerProperties[unallocated[i]].Add(household[0][0][i]);
                }
            }

            for (int i = 0; i < target.Area.Count; i++)
            {
                for (int j = 0; j < cornerProperties[i].Count; j++)
                {
                    householdStatistics[i].Add(new HouseholdStatistics(cornerProperties[i][j], 1 * (int)storiesHigh));
                }
                if (edgeProperties[i].Count > 0)
                {
                    householdStatistics[i].Add(new HouseholdStatistics(edgeProperties[i][0], edgeProperties[i].Count * (int)storiesHigh));
                }
            }

            //core properties
            core = new List<List<Core>>();
            for(int i =0; i<storiesHigh+2; i++)
            {
                double tempStoryHeight = (i == 0) ? 0 : Consts.PilotiHeight + Consts.FloorHeight * (i - 1);
                List<Core> cpB = new List<Core>();

                for (int j = 0; j < corePoint.Count; j++)
                {
                    Core oneCore = new Core(corePoint[j], coreVecX[j], coreVecY[j], randomCoreType, parameterSet.Parameters[0], randomCoreType.GetDepth() - 0);
                    oneCore.Origin = oneCore.Origin + Vector3d.ZAxis * tempStoryHeight;
                    oneCore.Stories = i;

                    //임시 면적
                    oneCore.Area = randomCoreType.GetDepth() * randomCoreType.GetWidth();

                    cpB.Add(oneCore);
                }
                core.Add(cpB);
            }          
        }

        ///////////////////////////////
        //////////  outputs  //////////
        ///////////////////////////////

        private double exclusiveAreaCalculatorAG4Corner(double xa, double xb, double ya, double yb, double balconyDepth)
        {
            double exclusiveArea = xa * ya - xb * yb;
            exclusiveArea -= (xa + ya) * balconyDepth;
            exclusiveArea += balconyDepth * balconyDepth * 2;
            return exclusiveArea;
        }

        private double exclusiveAreaCalculatorAG4Edge(double xa, double xb, double ya, double yb, double balconyDepth)
        {
            double exclusiveArea = xa * ya - xb * yb;
            exclusiveArea -= (xa + xa - xb) * balconyDepth;
            return exclusiveArea;
        }

        private List<List<Curve>> buildingOutlineMakerAG4(Curve centerline, double width)
        {
            List<List<Curve>> outline = new List<List<Curve>>();
            List<Curve> outlineTemp = new List<Curve>();

            List<Curve> offset = new List<Curve>();
            offset.Add(centerline.Offset(Plane.WorldXY, width / 2, 1, CurveOffsetCornerStyle.Sharp)[0]);
            offset.Add(centerline.Offset(Plane.WorldXY, -width / 2, 1, CurveOffsetCornerStyle.Sharp)[0]);
            List<Curve> segmentsStart = new List<Curve>();

            List<double> offsetDirs = new List<double>(new double[] { -1, 1 });

            List<double> offsetLength = new List<double>();
            foreach (Curve i in offset)
            {
                offsetLength.Add(i.GetLength());
            }

            Curve innerLine = offset[offsetLength.IndexOf(offsetLength.Min())];
            double offsetDirection = offsetDirs[offsetLength.IndexOf(offsetLength.Min())];

            //find building outline
            Polyline offset1;
            Polyline offset2;
            offset[0].TryGetPolyline(out offset1);
            offset[1].TryGetPolyline(out offset2);
            offset2.Reverse();
            List<Point3d> buildingOutlinePts = new List<Point3d>(offset1);
            List<Point3d> tempPts = new List<Point3d>(offset2);
            for (int i = 0; i < tempPts.Count(); i++)
            {
                buildingOutlinePts.Add(tempPts[i]);
            }
            buildingOutlinePts.Add(buildingOutlinePts[0]);
            Polyline buildingOutline = new Polyline(buildingOutlinePts);
            Curve outlineCurve = buildingOutline.ToNurbsCurve();
            outlineTemp.Add(outlineCurve);
            outline.Add(outlineTemp);
            return outline;
        }
    }
}