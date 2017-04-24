using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rhino.Geometry;
using System.Collections;
using Rhino.Collections;

namespace TuringAndCorbusier
{
    class AG4 : ApartmentmentGeneratorBase
    {
        public override ApartmentGeneratorOutput generator(Plot plot, ParameterSet parameterSet, Target target)
        {
            ///////////////////////////////////////////////
            //////////  common initial settings  //////////
            ///////////////////////////////////////////////

            //입력"값" 부분
            double[] parameters = parameterSet.Parameters;
            double storiesHigh = Math.Max((int)parameters[0], (int)parameters[1]);
            //double storiesLow = Math.Min((int)parameters[0], (int)parameters[1]);
            double width = parameters[2];
            //double angleRadian = parameters[3];
            //double moveFactor = parameters[4];
            Regulation regulationHigh = new Regulation(storiesHigh);
            //Regulation regulationLow = new Regulation(storiesLow);
            List<double> ratio = target.TargetRatio;
            List<double> area = target.TargetArea.Select(n => n * 1000 * 1000).ToList();
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

            double wholeL = centerpolyline.Length;
            double coreWidth = parameterSet.CoreType.GetWidth();
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
            List<List<List<HouseholdProperties>>> householdProperties = new List<List<List<HouseholdProperties>>>();
            List<List<HouseholdStatistics>> householdStatistics = new List<List<HouseholdStatistics>>();
            List<List<CoreProperties>> coreProperties = new List<List<CoreProperties>>();
            coreAndHouses(unallocated, stretchedLength, target, parameterSet, lines, centerline, out householdProperties, out householdStatistics, out coreProperties);

            //building outlines
            List<List<Curve>> buildingOutlines = buildingOutlineMakerAG4(centerline, width);

            //parking lot
            ParkingLotOnEarth parkingLotOnEarth = new ParkingLotOnEarth(parkingLotMaker(centerline, width, coreWidth, coreProperties));
            ParkingLotUnderGround parkingLotUnderGround = new ParkingLotUnderGround();

            ApartmentGeneratorOutput result = new ApartmentGeneratorOutput("PT-3", plot, buildingType, parameterSet, target, coreProperties, householdProperties, parkingLotOnEarth, parkingLotUnderGround, buildingOutlines, new List<Curve>());
            return result;


        }

        ///////////////////////////////////////////
        //////////  GA initial settings  //////////
        ///////////////////////////////////////////

        //Parameter 최소값 ~ 최대값 {storiesHigh, storiesLow, width, angle, moveFactor}
        private double[] minInput = { 3, 3, 7000, 0, 0 };
        //private double[] minInput = { 6, 6, 10500, 0, 0 };
        private double[] maxInput = { 7, 7, 8000, 2 * Math.PI, 1 };

        //Parameter GA최적화 {mutation probability, elite percentage, initial boost, population, generation, fitness value, mutation factor(0에 가까울수록 변동 범위가 넓어짐)
        private double[] GAparameterset = { 0.8, 0.05, 1, 10, 1, 10, 1 };
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
                plotArrExtended[i] = plotArrExtended[i].Extend(-1, 2);
            }
            //

            double[] distanceByLighting = new double[plotArr.Length];
            for (int i = 0; i < plotArr.Length; i++)
            {
                Vector3d tempVector = new Vector3d(plotArr[i].PointAt(plotArr[i].Domain.T1) - plotArr[i].PointAt(plotArr[i].Domain.T0));
                Vector3d baseVector = new Vector3d(Math.Cos(angleRadian), Math.Sin(angleRadian), 0);
                double tempAngle = Math.Acos((tempVector.X * baseVector.X + tempVector.Y * baseVector.Y) / tempVector.Length / baseVector.Length);
                double convertedAngle = Math.PI - (Math.Abs(tempAngle - Math.PI));
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

        ////////////////////////////////////////
        //////////  maximum polyline  //////////
        ////////////////////////////////////////

        private Polyline maxPolylineFinder(Plot plot, Regulation regulationHigh, Curve[] plotArr, ParameterSet parameterSet)
        {
            double width = parameterSet.Parameters[2];

            int angleRes = 19;
            int axisRes = 11;
            int widthRes = 10;
            int subIter = 4;
            double convergenceFactor = 1.6;

            int angleResSub = 3;
            int axisResSub = 3;
            double alpha = 0.01;
            double maxL = 0;
            List<Point3d> solPoint = new List<Point3d>();
            double searchWider = 2;
            for (int i = 0; i < 4; i++)
            {
                solPoint.Add(new Point3d(0, i, 0));
            }

            double solAngle = 0;
            Polyline x;

            for (int i = 0; i < angleRes - 1; i++)
            {
                double angleRadian = Math.PI * i / angleRes;

                //법규 : 대지 안의 공지
                Curve surroundingsHigh = regulationHigh.fromSurroundingsCurve(plot, regulationHigh, plotArr);
                //Curve surroundingsLow = Regulation.fromSurroundingsCurve(plot, regulationLow, plotArr);

                //법규 : 일조에 의한 높이제한
                Curve northHigh = regulationHigh.fromNorthCurve(plot, regulationHigh, plotArr);
                //Curve northLow = Regulation.fromNorthCurve(plot, regulationLow, plotArr);

                ///////////////////////////////////////////
                //////////  additional settings  //////////
                ///////////////////////////////////////////

                //법규 : 인접대지경계선(채광창)
                Curve lighting1 = byLightingCurve(plot, regulationHigh, plotArr, angleRadian);
                Curve lighting2 = byLightingCurve(plot, regulationHigh, plotArr, angleRadian + Math.PI / 2);
                Curve lightingHigh = CommonFunc.joinRegulations(lighting1, lighting2);

                //일부 조건(대지안의공지, 일조에 의한 높이제한)을 만족하는 경계선
                //모든 조건(대지안의공지, 일조에의한 높이제한, 인접대지경계선)을 만족하는 경계선
                Curve partialRegulationHigh = CommonFunc.joinRegulations(surroundingsHigh, northHigh);
                Curve wholeRegulationHigh = CommonFunc.joinRegulations(partialRegulationHigh, lightingHigh);

                Curve xx = wholeRegulationHigh;
                xx.TryGetPolyline(out x);

                //rotate clone
                Polyline xClone = new Polyline(x);
                xClone.Transform(Transform.Rotation(angleRadian, new Point3d(0, 0, 0)));

                //find bounding box
                var bBox = xClone.BoundingBox;
                Point3d minP = bBox.Min;
                Point3d maxP = bBox.Max;
                double axisDist = (maxP.Y - minP.Y - 2 * alpha - width) / axisRes;

                for (int j = 0; j < axisRes + 1; j++)
                {
                    Line axis = new Line(new Point3d(minP.X - alpha, minP.Y + axisDist * (j) + alpha + width / 2, 0), new Point3d(maxP.X + alpha, minP.Y + axisDist * (j) + alpha + width / 2, 0));
                    Line axisUp = new Line(new Point3d(minP.X - alpha, minP.Y + axisDist * (j) + width + alpha, 0), new Point3d(maxP.X + alpha, minP.Y + axisDist * (j) + width + alpha, 0));
                    Line axisDown = new Line(new Point3d(minP.X - alpha, minP.Y + axisDist * (j) + alpha, 0), new Point3d(maxP.X + alpha, minP.Y + axisDist * (j) + alpha, 0));

                    List<Point3d> axisP = new List<Point3d>(intersectionPoints(axis, xClone));
                    List<Point3d> axisUpP = new List<Point3d>(intersectionPoints(axisUp, xClone));
                    List<Point3d> axisDownP = new List<Point3d>(intersectionPoints(axisDown, xClone));

                    //execute when there's only 2 intersection points
                    if (axisP.Count == 2 && axisUpP.Count == 2 && axisDownP.Count == 2)
                    {
                        List<double> lowX = new List<double>();
                        List<double> highX = new List<double>();
                        lowX.Add(Math.Min(axisP[0].X, axisP[1].X));
                        highX.Add(Math.Max(axisP[0].X, axisP[1].X));
                        lowX.Add(Math.Min(axisUpP[0].X, axisUpP[1].X));
                        highX.Add(Math.Max(axisUpP[0].X, axisUpP[1].X));
                        lowX.Add(Math.Min(axisDownP[0].X, axisDownP[1].X));
                        highX.Add(Math.Max(axisDownP[0].X, axisDownP[1].X));
                        double lowMax = lowX.Max();
                        double highMin = highX.Min();

                        //make  points on axis
                        double widthS = lowMax + width / 2 + alpha;
                        double widthE = highMin - width / 2 - alpha;
                        double widthDist = (widthE - widthS) / widthRes;
                        List<Point3d> widthPoints = new List<Point3d>();
                        for (int k = 0; k < widthRes + 1; k++)
                        {
                            widthPoints.Add(new Point3d(widthS + widthDist * (k) + alpha, minP.Y + axisDist * (j) + alpha + width / 2, 0));
                        }

                        //maximum rectangle with fixed width
                        List<double> xMin = new List<double>();
                        List<double> xMax = new List<double>();

                        for (int k = 0; k < widthPoints.Count; k++)
                        {

                            List<double> xTempMin = new List<double>();
                            List<double> xTempMax = new List<double>();
                            List<Point3d> unsortedP = new List<Point3d>();
                            List<Point3d> checkRectCorner = new List<Point3d>();
                            checkRectCorner.Add(new Point3d(widthPoints[k].X - width / 2 - alpha, minP.Y - alpha, 0));
                            checkRectCorner.Add(new Point3d(widthPoints[k].X - width / 2 - alpha, maxP.Y + alpha, 0));
                            checkRectCorner.Add(new Point3d(widthPoints[k].X + width / 2 + alpha, maxP.Y + alpha, 0));
                            checkRectCorner.Add(new Point3d(widthPoints[k].X + width / 2 + alpha, minP.Y - alpha, 0));
                            checkRectCorner.Add(new Point3d(widthPoints[k].X - width / 2 - alpha, minP.Y - alpha, 0));
                            Polyline checkRect = new Polyline(checkRectCorner);
                            Curve xCurve = xClone.ToNurbsCurve();
                            Curve splitter = checkRect.ToNurbsCurve();
                            List<Curve> insideCurve = new List<Curve>(splitCurve(xCurve, splitter));

                            foreach (Curve crv in insideCurve)
                            {
                                Polyline insidePolyline;
                                crv.TryGetPolyline(out insidePolyline);
                                List<Point3d> insidePoint = new List<Point3d>(insidePolyline);
                                foreach (Point3d pt in insidePoint)
                                {
                                    unsortedP.Add(pt);
                                }
                            }

                            for (int l = 0; l < unsortedP.Count; l++)
                            {
                                if (unsortedP[l].Y < minP.Y + axisDist * j + alpha + width / 2)
                                {
                                    xTempMin.Add(unsortedP[l].Y);
                                }
                                else
                                {
                                    xTempMax.Add(unsortedP[l].Y);
                                }
                            }
                            if (xTempMin.Count != 0)
                            {
                                xMin.Add(xTempMin.Max());
                            }
                            else
                            {
                                xMin.Add(widthPoints[k].Y);
                            }
                            if (xTempMax.Count != 0)
                            {
                                xMax.Add(xTempMax.Min());
                            }
                            else
                            {
                                xMax.Add(widthPoints[k].Y);
                            }

                        }

                        List<int> dir = new List<int>();
                        List<double> foldL = new List<double>();
                        for (int k = 0; k < widthPoints.Count; k++)
                        {
                            if (widthPoints[k].Y - xMin[k] < xMax[k] - widthPoints[k].Y)
                            {
                                dir.Add(1);
                                foldL.Add(xMax[k] - widthPoints[k].Y);
                            }
                            else
                            {
                                dir.Add(-1);
                                foldL.Add(widthPoints[k].Y - xMin[k]);
                            }
                        }

                        for (int a = 1; a < widthPoints.Count; a++)
                        {
                            for (int b = 0; b < a; b++)
                            {
                                if (foldL[b] + foldL[a] + widthDist * (a - b) > maxL && widthPoints[b].DistanceTo(widthPoints[a]) > regulationHigh.DistanceLL + width)
                                {
                                    maxL = foldL[b] + foldL[a] + widthDist * (a - b);
                                    solAngle = angleRadian;
                                    solPoint.Clear();
                                    Point3d tempPb = new Point3d(widthPoints[b]);
                                    Point3d tempPa = new Point3d(widthPoints[a]);
                                    tempPb.Transform(Transform.Translation(Vector3d.Multiply(new Vector3d(0, dir[b], 0), foldL[b])));
                                    tempPa.Transform(Transform.Translation(Vector3d.Multiply(new Vector3d(0, dir[a], 0), foldL[a])));
                                    solPoint.Add(tempPb);
                                    solPoint.Add(widthPoints[b]);
                                    solPoint.Add(widthPoints[a]);
                                    solPoint.Add(tempPa);
                                }
                            }
                        }

                    }
                }

            }

            //sub iter start

            List<Point3d> solPointNew = new List<Point3d>(solPoint);
            double solAngleNew = solAngle;
            int token = 0;

            while (token < subIter)
            {
                for (int i = -angleResSub; i < angleResSub + 1; i++)
                {
                    double angleRadian = solAngle + Math.PI / angleRes * i / angleResSub * searchWider;

                    //법규 : 대지 안의 공지
                    Curve surroundingsHigh = regulationHigh.fromSurroundingsCurve(plot, regulationHigh, plotArr);
                    //Curve surroundingsLow = Regulation.fromSurroundingsCurve(plot, regulationLow, plotArr);

                    //법규 : 일조에 의한 높이제한
                    Curve northHigh = regulationHigh.fromNorthCurve(plot, regulationHigh, plotArr);
                    //Curve northLow = Regulation.fromNorthCurve(plot, regulationLow, plotArr);

                    ///////////////////////////////////////////
                    //////////  additional settings  //////////
                    ///////////////////////////////////////////

                    //법규 : 인접대지경계선(채광창)
                    Curve lighting1 = byLightingCurve(plot, regulationHigh, plotArr, angleRadian);
                    Curve lighting2 = byLightingCurve(plot, regulationHigh, plotArr, angleRadian + Math.PI / 2);
                    Curve lightingHigh = CommonFunc.joinRegulations(lighting1, lighting2);

                    //일부 조건(대지안의공지, 일조에 의한 높이제한)을 만족하는 경계선
                    //모든 조건(대지안의공지, 일조에의한 높이제한, 인접대지경계선)을 만족하는 경계선
                    Curve partialRegulationHigh = CommonFunc.joinRegulations(surroundingsHigh, northHigh);
                    Curve wholeRegulationHigh = CommonFunc.joinRegulations(partialRegulationHigh, lightingHigh);

                    //rotate clone
                    Curve xx = wholeRegulationHigh;
                    xx.TryGetPolyline(out x);
                    Polyline xClone = new Polyline(x);
                    xClone.Transform(Transform.Rotation(angleRadian, new Point3d(0, 0, 0)));
                    List<Point3d> solPointClone = new List<Point3d>();
                    for (int j = 0; j < 4; j++)
                    {
                        Point3d solPointCloneTemp = solPoint[j];
                        solPointCloneTemp.Transform(Transform.Rotation(angleRadian, new Point3d(0, 0, 0)));
                        solPointClone.Add(solPointCloneTemp);
                    }

                    //find bounding box
                    var bBox = xClone.BoundingBox;
                    Point3d minP = bBox.Min;
                    Point3d maxP = bBox.Max;
                    double axisDist = (maxP.Y - minP.Y - 2 * alpha - width) / axisRes;
                    double axisDistSub = axisDist / axisResSub * searchWider;

                    for (int j = -axisResSub; j < axisResSub + 1; j++)
                    {
                        Line axis = new Line(new Point3d(minP.X, (solPointClone[1].Y + solPointClone[2].Y) / 2 + axisDistSub * j, 0), new Point3d(maxP.X, (solPointClone[1].Y + solPointClone[2].Y) / 2 + axisDistSub * j, 0));
                        Line axisUp = new Line(new Point3d(minP.X, (solPointClone[1].Y + solPointClone[2].Y) / 2 + axisDistSub * j + width / 2, 0), new Point3d(maxP.X, (solPointClone[1].Y + solPointClone[2].Y) / 2 + axisDistSub * j + width / 2, 0));
                        Line axisDown = new Line(new Point3d(minP.X, (solPointClone[1].Y + solPointClone[2].Y) / 2 + axisDistSub * j - width / 2, 0), new Point3d(maxP.X, (solPointClone[1].Y + solPointClone[2].Y) / 2 + axisDistSub * j - width / 2, 0));

                        List<Point3d> axisP = new List<Point3d>(intersectionPoints(axis, xClone));
                        List<Point3d> axisUpP = new List<Point3d>(intersectionPoints(axisUp, xClone));
                        List<Point3d> axisDownP = new List<Point3d>(intersectionPoints(axisDown, xClone));

                        //execute when there's only 2 intersection points
                        if (axisP.Count == 2 && axisUpP.Count == 2 && axisDownP.Count == 2)
                        {
                            List<double> lowX = new List<double>();
                            List<double> highX = new List<double>();
                            lowX.Add(Math.Min(axisP[0].X, axisP[1].X));
                            highX.Add(Math.Max(axisP[0].X, axisP[1].X));
                            lowX.Add(Math.Min(axisUpP[0].X, axisUpP[1].X));
                            highX.Add(Math.Max(axisUpP[0].X, axisUpP[1].X));
                            lowX.Add(Math.Min(axisDownP[0].X, axisDownP[1].X));
                            highX.Add(Math.Max(axisDownP[0].X, axisDownP[1].X));
                            double lowMax = lowX.Max();
                            double highMin = highX.Min();

                            //make  points on axis
                            double widthS = lowMax + width / 1.90 + alpha;
                            double widthE = highMin - width / 1.90 - alpha;
                            double widthDist = (widthE - widthS) / widthRes;
                            List<Point3d> widthPoints = new List<Point3d>();
                            for (int k = 0; k < widthRes + 1; k++)
                            {
                                widthPoints.Add(new Point3d(widthS + widthDist * k, (solPointClone[1].Y + solPointClone[2].Y) / 2 + axisDistSub * j, 0));
                            }

                            //maximum rectangle with fixed width
                            List<double> xMin = new List<double>();
                            List<double> xMax = new List<double>();

                            for (int k = 0; k < widthPoints.Count; k++)
                            {
                                List<double> xTempMin = new List<double>();
                                List<double> xTempMax = new List<double>();
                                List<Point3d> unsortedP = new List<Point3d>();
                                List<Point3d> checkRectCorner = new List<Point3d>();
                                checkRectCorner.Add(new Point3d(widthPoints[k].X - width / 2 - alpha, minP.Y - alpha, 0));
                                checkRectCorner.Add(new Point3d(widthPoints[k].X - width / 2 - alpha, maxP.Y + alpha, 0));
                                checkRectCorner.Add(new Point3d(widthPoints[k].X + width / 2 + alpha, maxP.Y + alpha, 0));
                                checkRectCorner.Add(new Point3d(widthPoints[k].X + width / 2 + alpha, minP.Y - alpha, 0));
                                checkRectCorner.Add(new Point3d(widthPoints[k].X - width / 2 - alpha, minP.Y - alpha, 0));
                                Polyline checkRect = new Polyline(checkRectCorner);
                                Curve xCurve = xClone.ToNurbsCurve();
                                Curve splitter = checkRect.ToNurbsCurve();
                                List<Curve> insideCurve = new List<Curve>(splitCurve(xCurve, splitter));
                                foreach (Curve crv in insideCurve)
                                {
                                    Polyline insidePolyline;
                                    crv.TryGetPolyline(out insidePolyline);
                                    List<Point3d> insidePoint = new List<Point3d>(insidePolyline);
                                    foreach (Point3d pt in insidePoint)
                                    {
                                        unsortedP.Add(pt);
                                    }
                                }



                                for (int l = 0; l < unsortedP.Count; l++)
                                {
                                    if (unsortedP[l].Y < (solPointClone[1].Y + solPointClone[2].Y) / 2 + axisDistSub * j)
                                    {
                                        xTempMin.Add(unsortedP[l].Y);
                                    }
                                    else
                                    {
                                        xTempMax.Add(unsortedP[l].Y);
                                    }
                                }
                                if (xTempMin.Count != 0)
                                {
                                    xMin.Add(xTempMin.Max());
                                }
                                else
                                {
                                    xMin.Add(widthPoints[k].Y);
                                }
                                if (xTempMax.Count != 0)
                                {
                                    xMax.Add(xTempMax.Min());
                                }
                                else
                                {
                                    xMax.Add(widthPoints[k].Y);
                                }

                            }

                            List<int> dir = new List<int>();
                            List<double> foldL = new List<double>();
                            for (int k = 0; k < widthPoints.Count; k++)
                            {
                                if (widthPoints[k].Y - xMin[k] < xMax[k] - widthPoints[k].Y)
                                {
                                    dir.Add(1);
                                    foldL.Add(xMax[k] - widthPoints[k].Y);
                                }
                                else
                                {
                                    dir.Add(-1);
                                    foldL.Add(widthPoints[k].Y - xMin[k]);
                                }
                            }

                            for (int a = 1; a < widthPoints.Count; a++)
                            {
                                for (int b = 0; b < a; b++)
                                {
                                    if (foldL[b] + foldL[a] + widthDist * (a - b) > maxL && widthPoints[b].DistanceTo(widthPoints[a]) > regulationHigh.DistanceLL + width)
                                    {
                                        maxL = foldL[b] + foldL[a] + widthDist * (a - b);
                                        solAngleNew = angleRadian;
                                        solPointNew.Clear();
                                        Point3d tempPb = new Point3d(widthPoints[b]);
                                        Point3d tempPa = new Point3d(widthPoints[a]);
                                        tempPb.Transform(Transform.Translation(Vector3d.Multiply(new Vector3d(0, dir[b], 0), foldL[b])));
                                        tempPa.Transform(Transform.Translation(Vector3d.Multiply(new Vector3d(0, dir[a], 0), foldL[a])));
                                        solPointNew.Add(tempPb);
                                        solPointNew.Add(widthPoints[b]);
                                        solPointNew.Add(widthPoints[a]);
                                        solPointNew.Add(tempPa);
                                    }
                                }
                            }
                        }
                    }
                }
                token++;
                searchWider *= 1 / convergenceFactor;
            }

            solPoint = solPointNew;
            solAngle = solAngleNew;
            Polyline solPoly = new Polyline(solPoint);
            solPoly.Transform(Transform.Rotation(-solAngle, new Point3d(0, 0, 0)));

            return solPoly;
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
        private void coreAndHouses(List<int> unallocated, List<double> stretchedLength, Target target, ParameterSet parameterSet, List<Line> lines, Curve centerline, out List<List<List<HouseholdProperties>>> householdProperties, out List<List<HouseholdStatistics>> householdStatistics, out List<List<CoreProperties>> coreProperties)
        {
            double storiesHigh = Math.Max((int)parameterSet.Parameters[0], (int)parameterSet.Parameters[1]);
            double coreWidth = parameterSet.CoreType.GetWidth();
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
                            wallFactors.Add(new List<double>(new double[] { 1, 0.5, 1, 0.5 }));
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

                        if (l1 < l2)
                        {
                            double tempL = l2;
                            l2 = l1;
                            l1 = tempL;
                            Vector3d tempVec = new Vector3d(v2);
                            v2 = new Vector3d(v1);
                            v1 = new Vector3d(tempVec);
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
                            wallFactors.Add(new List<double>(new double[] { 1, 0.5, 1, 1, 0.5, 1 }));
                        else if (l2 - width / 2 == 0)
                            wallFactors.Add(new List<double>(new double[] { 1, 1, 1, 0.5, 1 }));
                        else
                            wallFactors.Add(new List<double>(new double[] { 0, 0.5, 1, 1, 0.5, 1 }));

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
            //***이거 아직 어떻게 해야 할지 못정함. 수정 요망.
            List<Point3d> entBuilding = new List<Point3d>();
            for (int i = 0; i < homeShapeType.Count; i++)
            {
                entBuilding.Add(Point3d.Origin);
            }

            //household properties
            householdProperties = new List<List<List<HouseholdProperties>>>();
            List<List<HouseholdProperties>> hhpB = new List<List<HouseholdProperties>>();
            List<HouseholdProperties> hhpS = new List<HouseholdProperties>();

            for (int i = 0; i < homeOri.Count; i++)
            {
                hhpS.Add(new HouseholdProperties(homeOri[i], homeVecX[i], homeVecY[i], xa[i], xb[i], ya[i], yb[i], unallocated[i], exclusiveAreaCalculatorAG3(xa[i], xb[i], ya[i], yb[i], unallocated[i], Consts.balconyDepth), winBuilding[i], entBuilding[i], wallFactors[i]));
            }

            for (int j = 0; j < storiesHigh; j++)
            {
                List<HouseholdProperties> hhpSTemp = new List<HouseholdProperties>();
                for (int i = 0; i < hhpS.Count; i++)
                {
                    HouseholdProperties hhp = hhpS[i];
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

                    hhpSTemp.Add(new HouseholdProperties(ori, hhp.XDirection, hhp.YDirection, hhp.XLengthA, hhp.XLengthB, hhp.YLengthA, hhp.YLengthB, hhp.HouseholdSizeType, hhp.GetExclusiveArea(), winNew, ent, hhp.WallFactor));
                }
                hhpB.Add(hhpSTemp);
            }

            //hhpB.Add(hhpS);
            householdProperties.Add(hhpB);


            //household statistics
            householdStatistics = new List<List<HouseholdStatistics>>();
            List<List<HouseholdProperties>> cornerProperties = new List<List<HouseholdProperties>>();
            List<List<HouseholdProperties>> edgeProperties = new List<List<HouseholdProperties>>();
            for (int i = 0; i < target.TargetArea.Count; i++)
            {
                cornerProperties.Add(new List<HouseholdProperties>());
                edgeProperties.Add(new List<HouseholdProperties>());
                householdStatistics.Add(new List<HouseholdStatistics>());
            }

            for (int i = 0; i < homeShapeType.Count; i++)
            {
                if (homeShapeType[i] == 0)
                {
                    edgeProperties[unallocated[i]].Add(householdProperties[0][0][i]);
                }
                else
                {
                    cornerProperties[unallocated[i]].Add(householdProperties[0][0][i]);
                }
            }

            for (int i = 0; i < target.TargetArea.Count; i++)
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
            coreProperties = new List<List<CoreProperties>>();
            List<CoreProperties> cpB = new List<CoreProperties>();
            for (int i = 0; i < corePoint.Count; i++)
            {
                cpB.Add(new CoreProperties(corePoint[i], coreVecX[i], coreVecY[i], parameterSet.CoreType, parameterSet.Parameters[0], parameterSet.CoreType.GetDepth() - 0));
            }
            coreProperties.Add(cpB);
        }

        ///////////////////////////////
        //////////  outputs  //////////
        ///////////////////////////////


        private double exclusiveAreaCalculatorAG3(double xa, double xb, double ya, double yb, double targetArea, double balconyDepth)
        {
            double exclusiveArea = xa * ya - xb * yb;

            exclusiveArea -= (xa + xa - xb) * balconyDepth;

            if (targetArea <= Consts.AreaLimit)
            {
                exclusiveArea += (xa - xb) * balconyDepth;
            }

            return exclusiveArea;
            //***여기는 AG1에서 긁어왔으니 다시 만들 것
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

        private List<ParkingLine> parkingLotMaker(Curve centerline, double width, double coreWidth, List<List<CoreProperties>> coreProperties)
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

            //merge extended for z shape

            List<Rectangle3d> parkingLots = new List<Rectangle3d>();

            Curve[] segmentsStartTemp = innerLine.DuplicateSegments();
            List<Curve> worm = new List<Curve>(segmentsStartTemp);
            List<Curve> result = new List<Curve>();
            if (worm.Count == 5)
            {
                Vector3d tangent1 = worm[0].TangentAtStart;
                Vector3d tangent2 = worm[1].TangentAtStart;
                if (Vector3d.CrossProduct(tangent1, tangent2).Length < 0.1)
                {
                    Point3d tempEnd = worm[1].PointAtEnd;
                    Vector3d tempTangent = worm[1].TangentAtStart;
                    Vector3d tempTangentRev = Vector3d.Multiply(tempTangent, -1);
                    tempEnd.Transform(Transform.Translation(Vector3d.Multiply(tempTangentRev, width)));
                    Line tempLine = new Line(worm[0].PointAtStart, tempEnd);
                    Curve l = tempLine.ToNurbsCurve();
                    result.Add(l);
                    Point3d tempStart = worm[2].PointAtStart;
                    tempTangent = worm[2].TangentAtStart;
                    tempTangentRev = Vector3d.Multiply(tempTangent, -1);
                    tempStart.Transform(Transform.Translation(Vector3d.Multiply(tempTangent, width)));
                    tempLine = new Line(tempStart, worm[3].PointAtEnd);
                    l = tempLine.ToNurbsCurve();
                    result.Add(l);
                    result.Add(worm[4]);
                }
                else
                {
                    result.Add(worm[0]);
                    Point3d tempEnd = worm[2].PointAtEnd;
                    Vector3d tempTangent = worm[2].TangentAtStart;
                    Vector3d tempTangentRev = Vector3d.Multiply(tempTangent, -1);
                    tempEnd.Transform(Transform.Translation(Vector3d.Multiply(tempTangentRev, width)));
                    Line tempLine = new Line(worm[1].PointAtStart, tempEnd);
                    Curve l = tempLine.ToNurbsCurve();
                    result.Add(l);
                    Point3d tempStart = worm[3].PointAtStart;
                    tempTangent = worm[3].TangentAtStart;
                    tempTangentRev = Vector3d.Multiply(tempTangent, -1);
                    tempStart.Transform(Transform.Translation(Vector3d.Multiply(tempTangent, width)));
                    tempLine = new Line(tempStart, worm[4].PointAtEnd);
                    l = tempLine.ToNurbsCurve();
                    result.Add(l);
                }
            }
            else
            {
                for (int i = 0; i < worm.Count; i++)
                {
                    result.Add(worm[i]);
                }
            }

            segmentsStart = new List<Curve>(result);

            //break polyline and offset outwards by 5000

            List<Curve> segmentsEnd = new List<Curve>();
            for (int i = 0; i < 3; i++)
            {
                segmentsEnd.Add(segmentsStart[i].Offset(Plane.WorldXY, 5000 * offsetDirection, 1, CurveOffsetCornerStyle.Sharp)[0]);
            }

            List<Line> parkingStart = new List<Line>();
            List<Line> parkingEnd = new List<Line>();
            for (int i = 0; i < 3; i++)
            {
                Line l = new Line(segmentsStart[i].PointAtStart, segmentsStart[i].PointAtEnd);
                parkingStart.Add(l);
            }
            for (int i = 0; i < 3; i++)
            {
                Line l = new Line(segmentsEnd[i].PointAtStart, segmentsEnd[i].PointAtEnd);
                parkingEnd.Add(l);
            }

            //find core end points

            List<Point3d> coreEnds = new List<Point3d>();
            for (int i = 0; i < coreProperties[0].Count; i++)
            {
                Point3d core1 = coreProperties[0][i].Origin;
                Point3d core2 = coreProperties[0][i].Origin;
                core1.Transform(Transform.Translation(Vector3d.Multiply(coreProperties[0][i].XDirection, 0)));
                core2.Transform(Transform.Translation(Vector3d.Multiply(coreProperties[0][i].XDirection, coreWidth)));
                coreEnds.Add(core1);
                coreEnds.Add(core2);
            }




            List<List<Point3d>> coreDeletePoints = new List<List<Point3d>>();
            for (int i = 0; i < 3; i++)
            {
                coreDeletePoints.Add(new List<Point3d>());
            }

            for (int i = 0; i < 3; i++)
            {
                coreDeletePoints[i].Add(segmentsStart[i].PointAtStart);
            }
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < coreEnds.Count; j++)
                {
                    double dist = parkingStart[i].DistanceTo(coreEnds[j], true);
                    if (dist < 0.01)
                    {
                        coreDeletePoints[i].Add(coreEnds[j]);
                    }
                }
            }
            for (int i = 0; i < 3; i++)
            {
                coreDeletePoints[i].Add(segmentsStart[i].PointAtEnd);
            }

            //make parking lines

            List<Line> parkingLines = new List<Line>();
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < coreDeletePoints[i].Count - 1; j++)
                {
                    Line seg = new Line(coreDeletePoints[i][j], coreDeletePoints[i][j + 1]);
                    if (seg.Length < coreWidth - 0.1 || seg.Length > coreWidth + 0.1)
                    {
                        parkingLines.Add(seg);
                    }
                }
            }

            for (int i = 0; i < parkingLines.Count; i++)
            {
                int lotNum = (int)parkingLines[i].Length / 2300;
                Vector3d lotX = parkingLines[i].UnitTangent;
                Vector3d lotY = lotX;
                lotY.Rotate(Math.PI / 2 * (-offsetDirection), new Vector3d(0, 0, 1));
                Plane lotOri = new Plane(parkingLines[i].From, lotX, lotY);
                for (int j = 0; j < lotNum; j++)
                {
                    Plane lotOriTemp = lotOri;
                    lotOriTemp.Transform(Transform.Translation(Vector3d.Multiply(lotX, 2300 * j)));
                    parkingLots.Add(new Rectangle3d(lotOriTemp, 2300, 5000));
                }
            }

            //parallel parking

            if (width > 5000 + 2300 && width < 5000 + 5000)
            {
                for (int i = 0; i < parkingLines.Count; i++)
                {
                    int lotNum = (int)parkingLines[i].Length / 5000;
                    Vector3d lotX = parkingLines[i].UnitTangent;
                    Vector3d lotY = lotX;
                    lotY.Rotate(Math.PI / 2 * (-offsetDirection), new Vector3d(0, 0, 1));
                    Point3d Ori = parkingLines[i].From;
                    Ori.Transform(Transform.Translation(Vector3d.Multiply(lotY, 5000)));
                    Plane lotOri = new Plane(Ori, lotX, lotY);
                    for (int j = 0; j < lotNum; j++)
                    {
                        Plane lotOriTemp = lotOri;
                        lotOriTemp.Transform(Transform.Translation(Vector3d.Multiply(lotX, 5000 * j)));
                        parkingLots.Add(new Rectangle3d(lotOriTemp, 5000, 2300));
                    }
                }
            }

            //double parking

            if (width >= 5000 + 5000)
            {
                for (int i = 0; i < parkingLines.Count; i++)
                {
                    int lotNum = (int)parkingLines[i].Length / 2300;
                    Vector3d lotX = parkingLines[i].UnitTangent;
                    Vector3d lotY = lotX;
                    lotY.Rotate(Math.PI / 2 * (-offsetDirection), new Vector3d(0, 0, 1));
                    Point3d Ori = parkingLines[i].From;
                    Ori.Transform(Transform.Translation(Vector3d.Multiply(lotY, 5000)));
                    Plane lotOri = new Plane(Ori, lotX, lotY);
                    for (int j = 0; j < lotNum; j++)
                    {
                        Plane lotOriTemp = lotOri;
                        lotOriTemp.Transform(Transform.Translation(Vector3d.Multiply(lotX, 2300 * j)));
                        parkingLots.Add(new Rectangle3d(lotOriTemp, 2300, 5000));
                    }
                }
            }


            List<ParkingLine> parkingLotsOut = new List<ParkingLine>();
            for (int i = 0; i < parkingLots.Count; i++)
            {
                parkingLotsOut.Add(new ParkingLine(parkingLots[i]));
            }

            return parkingLotsOut;
        }
        public List<double> SplitWithRegions(Curve target, List<Curve> cutter)
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
