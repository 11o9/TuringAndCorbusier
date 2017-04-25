using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System.Linq;

namespace TuringAndCorbusier
{
    public class Elevation
    {
        private Elevation(List<Curve> outlineCurve, List<Curve> windowDetail, List<Curve> banister, List<Curve> core)
        {
            this.outlineCurve = outlineCurve;
            this.windowDetail = windowDetail;
            this.banister = banister;
            this.core = core;
        }

        public List<Curve> outlineCurve { get; private set; }
        public List<Curve> windowDetail { get; private set; }
        public List<Curve> banister { get; private set; }
        public List<Curve> core { get; private set; }

        public BoundingBox GetBoundingBox()
        {
            List<Curve> allCurves = new List<Curve>(outlineCurve);
            allCurves.AddRange(windowDetail);
            allCurves.AddRange(banister);

            List<Point3d> tempPts = new List<Point3d>();

            foreach(Curve i in allCurves)
            {
                tempPts.Add(i.PointAt(i.Domain.T0));
                tempPts.Add(i.PointAt(i.Domain.T1));
            }

            BoundingBox tempBoundingBox = new BoundingBox(tempPts);

            return tempBoundingBox;
        }

        public static Elevation drawElevation(Curve baseCurve, List<List<Household>> houseHolds, List<Core> cores)
        {
            double storiesHeight = Consts.FloorHeight;
            double pilotiHeight = Consts.PilotiHeight;

            double windowSide = 300;
            double windowLow = 300;
            double windowHeight = 2100;

            Curve groundCurve = new LineCurve(new Point3d(0, 0, 0), new Point3d(baseCurve.GetLength(), 0, 0));

            List<Curve> outlineCurve = new List<Curve>();
            List<Curve> windowDetail = new List<Curve>();
            List<Curve> banister = new List<Curve>();
            List<Curve> core = new List<Curve>();

            List<double> widthList = new List<double>();

            foreach (List<Household> i in houseHolds)
            {
                foreach(Household j in i)
                    widthList.Add(j.YLengthA);
            }

            List<double> uniqueWidthList = widthList.Distinct().ToList();
            uniqueWidthList.Sort();

            List<Brep> tempElevationBase = new List<Brep>();
            List<Curve> BalconyBase = new List<Curve>();
            List<double> pilotiParameter = new List<double>();
            List<Curve> coreBase = new List<Curve>();

            for(int h = 0; h < houseHolds.Count(); h++)
            {
                for(int i = 0; i < houseHolds[h].Count(); i++)
                {
                    Point3d start = houseHolds[h][i].Origin + houseHolds[h][i].XDirection * (-houseHolds[h][i].XLengthB);
                    Point3d end = houseHolds[h][i].Origin + houseHolds[h][i].XDirection * (houseHolds[h][i].XLengthA - houseHolds[h][i].XLengthB);

                    double startDomain; double endDomain;

                    baseCurve.ClosestPoint(start, out startDomain);
                    baseCurve.ClosestPoint(end, out endDomain);

                    if (h == 0)
                    {
                        pilotiParameter.Add((endDomain - baseCurve.Domain.T0) * baseCurve.GetLength() + groundCurve.Domain.T0);
                        pilotiParameter.Add((startDomain - baseCurve.Domain.T0) * baseCurve.GetLength() + groundCurve.Domain.T0);
                    }

                    Curve tempHousingBase = new LineCurve(groundCurve.PointAt((startDomain - baseCurve.Domain.T0) * baseCurve.GetLength() + groundCurve.Domain.T0), groundCurve.PointAt((endDomain - baseCurve.Domain.T0)*baseCurve.GetLength() + groundCurve.Domain.T0));

                    tempHousingBase.Transform(Transform.Translation(new Vector3d(0, start.Z, 0)));
                    Curve tempHousingTop = tempHousingBase.DuplicateCurve();
                    tempHousingTop.Transform(Transform.Translation(new Vector3d(0, storiesHeight, 0)));

                    Curve[] tempLoftBase = { tempHousingBase, tempHousingTop };

                    Brep tempHousingBrep = Brep.CreateFromLoft(tempLoftBase, Point3d.Unset, Point3d.Unset, LoftType.Straight, false)[0];

                    Curve tempBalconyBase = tempHousingBase.DuplicateCurve();
                    tempBalconyBase.Transform(Transform.Translation(new Vector3d(0, windowLow, 0)));

                    double balconyBaseStart; double balconyBaseEnd;

                    tempBalconyBase.LengthParameter(windowSide, out balconyBaseStart);
                    tempBalconyBase.LengthParameter(tempBalconyBase.GetLength() - windowSide, out balconyBaseEnd);

                    tempBalconyBase = new LineCurve(tempBalconyBase.PointAt(balconyBaseStart), tempBalconyBase.PointAt(balconyBaseEnd));

                    tempElevationBase.Add(tempHousingBrep);
                    BalconyBase.Add(tempBalconyBase);

                }
            }

            for (int i = 0; i < cores.Count(); i++)
            {
                Point3d tempCoreStart = cores[i].Origin;
                Point3d tempCoreEnd = cores[i].Origin + cores[i].XDirection * cores[i].CoreType.GetWidth();

                double startDomain; double endDomain;

                baseCurve.ClosestPoint(tempCoreStart, out startDomain);
                baseCurve.ClosestPoint(tempCoreEnd, out endDomain);

                Curve tempCoreBase = new LineCurve(groundCurve.PointAt((startDomain - baseCurve.Domain.T0) * baseCurve.GetLength() + groundCurve.Domain.T0), groundCurve.PointAt((endDomain - baseCurve.Domain.T0) * baseCurve.GetLength() + groundCurve.Domain.T0));

                coreBase.Add(tempCoreBase);
            }

            List<List<Brep>> elevationBrepSortedByWidth = new List<List<Brep>>();

            for(int i = 0; i < uniqueWidthList.Count(); i++)
            {
                elevationBrepSortedByWidth.Add(new List<Brep>());
            }

            for (int i = 0; i < widthList.Count; i++)
            {
                int tempWidthIndex = uniqueWidthList.IndexOf(widthList[i]);

                elevationBrepSortedByWidth[tempWidthIndex].Add(tempElevationBase[i]);
            }

            //elevationBrepSortedByWidth[elevationBrepSortedByWidth.Count() - 1].AddRange(DrawPiloti(groundCurve, pilotiParameter.Distinct().ToList(), pilotiHeight));

            List<Curve> joinedOutLineCurve = new List<Curve>();

            foreach (List<Brep> i in elevationBrepSortedByWidth)
            {
                Brep[] joinedBreps = Brep.JoinBreps(i, 2);
                
                foreach(Brep j in joinedBreps)
                {
                    outlineCurve.AddRange(j.DuplicateNakedEdgeCurves(true, true).ToList());
                    joinedOutLineCurve.AddRange(Curve.JoinCurves(j.DuplicateNakedEdgeCurves(true, true).ToList()).ToList());
                }
            }

            for(int i = 0; i < coreBase.Count(); i++)
            {
                Curve coreBoundary = offsetOneSide(coreBase[i], (cores[i].Stories + 1) * Consts.FloorHeight + Consts.PilotiHeight);

                core.AddRange(getOuterCurve(coreBoundary, joinedOutLineCurve));
            }

            foreach (Curve h in BalconyBase)
            {
                double[] target = { double.MaxValue, 2, 2 };
                List<object> typeList = new List<object>();
                List<Curve> splittedBaseCurve = divideBaseCurve(h, target, out typeList);

                for (int i = 0; i < splittedBaseCurve.Count(); i++)
                {
                    List<string> windowTypeList = Enum.GetValues(typeof(WindowType)).Cast<WindowType>().Select(v => v.ToString()).ToList();

                    if (windowTypeList.IndexOf(typeList[i].ToString()) != -1)
                    {
                        int index = windowTypeList.IndexOf(typeList[i].ToString());

                        List<Curve> tempWindow = drawWindow((WindowType)index, splittedBaseCurve[i], windowHeight);

                        windowDetail.AddRange(tempWindow);

                        List<Curve> tempBanister = drawBanister(splittedBaseCurve[i], 25, 900);

                        banister.AddRange(tempBanister);
                    }

                }
            }

            return new Elevation(outlineCurve, windowDetail, banister, core);
        }

        private static List<Curve> getOuterCurve(Curve baseCurve, List<Curve> boundaryCurve)
        {
            List<Curve> output = new List<Curve>();
            output.Add(baseCurve);

            for(int i = 0; i < boundaryCurve.Count(); i++)
            {
                List<Curve> tempOutput = new List<Curve>();

                Curve tempBoundary = boundaryCurve[i];

                if (tempBoundary.IsClosed == true)
                {
                    for (int j = 0; j < output.Count(); j++)
                    {
                        Curve tempTarget = output[j];

                        var tempIntersection = Rhino.Geometry.Intersect.Intersection.CurveCurve(tempTarget, tempBoundary, 0, 0);

                        List<double> IntersectParam = new List<double>();

                        foreach(Rhino.Geometry.Intersect.IntersectionEvent k in tempIntersection)
                            IntersectParam.Add(k.ParameterA);

                        Curve[] shatteredCurve = tempTarget.Split(IntersectParam);

                        foreach(Curve k in shatteredCurve)
                        {
                            if (tempBoundary.Contains(k.PointAt(k.Domain.Mid)) == PointContainment.Outside)
                            {
                                tempOutput.Add(k);
                            }
                        }
                    }
                }

                output = tempOutput;
            }

            return output;
        }

        private static List<Brep> DrawPiloti(Curve baseCurve, IEnumerable<double> parameter, double pilotiHeight)
        {
            List<Brep> output = new List<Brep>();

            double pilotiWidth = 300;

            double convertedPilotiWidth;

            baseCurve.LengthParameter(pilotiWidth, out convertedPilotiWidth);
            convertedPilotiWidth = convertedPilotiWidth - baseCurve.Domain.T0;

            //첫부분 필로티 생성)

            Curve firstPilotiCurve = new LineCurve(baseCurve.PointAt(parameter.ToList()[0] ), baseCurve.PointAt(parameter.ToList()[0] + convertedPilotiWidth ));
            Curve firstLoftBase = firstPilotiCurve.DuplicateCurve();
            firstLoftBase.Transform(Transform.Translation(new Vector3d(0, pilotiHeight, 0)));

            Curve[] firstLoftBaseSet = { firstPilotiCurve, firstLoftBase };

            Brep firstPilotiBrep = Brep.CreateFromLoft(firstLoftBaseSet, Point3d.Unset, Point3d.Unset, LoftType.Normal, false)[0];

            output.Add(firstPilotiBrep);

            //중간부분 필로티 생성)

            for (int i = 1; i < parameter.Count() - 1; i++)
            {
                Curve tempCurve = new LineCurve(baseCurve.PointAt(parameter.ToList()[i] - convertedPilotiWidth / 2), baseCurve.PointAt(parameter.ToList()[i] + convertedPilotiWidth / 2));
                Curve tempLoftBase = tempCurve.DuplicateCurve();
                tempLoftBase.Transform(Transform.Translation(new Vector3d(0, pilotiHeight, 0)));

                Curve[] tempLoftBaseSet = { tempCurve, tempLoftBase };

                Brep tempPilotiBrep = Brep.CreateFromLoft(tempLoftBaseSet, Point3d.Unset, Point3d.Unset, LoftType.Normal, false )[0];

                output.Add(tempPilotiBrep);
                /*

                Curve tempCurve_2 = new LineCurve(baseCurve.PointAt(parameter.ToList()[i]), baseCurve.PointAt(parameter.ToList()[i] + convertedPilotiWidth / 2));
                Curve tempLoftBase_2 = tempCurve_2.DuplicateCurve();
                tempLoftBase_2.Transform(Transform.Translation(new Vector3d(0, pilotiHeight, 0)));

                Curve[] tempLoftBaseSet_2 = { tempCurve_2, tempLoftBase_2 };

                Brep tempPilotiBrep_2 = Brep.CreateFromLoft(tempLoftBaseSet_2, Point3d.Unset, Point3d.Unset, LoftType.Normal, false)[0];

                output.Add(tempPilotiBrep_2);
                */
            }

            //마지막부분 필로티 생성)

            Curve lastPilotiCurve = new LineCurve(baseCurve.PointAt(parameter.ToList()[parameter.Count() - 1] - convertedPilotiWidth ), baseCurve.PointAt(parameter.ToList()[parameter.Count() - 1]));
            Curve lastLoftBase = lastPilotiCurve.DuplicateCurve();
            lastLoftBase.Transform(Transform.Translation(new Vector3d(0, pilotiHeight, 0)));

            Curve[] lastLoftBaseSet = { lastPilotiCurve, lastLoftBase };

            Brep lastPilotiBrep = Brep.CreateFromLoft(lastLoftBaseSet, Point3d.Unset, Point3d.Unset, LoftType.Normal, false)[0];

            output.Add(lastPilotiBrep);

            return output;
        }

        private static List<Curve> divideBaseCurve(Curve baseCurve, double[] target, out List<object> type)
        {
            List<double> outputParameter = new List<double>();
            List<object> outputType = new List<object>();

            double length = baseCurve.GetLength();
            double currentPos = 0;
            double[] minLength = { 2000, 2400, 5000 };
            double wallLength = 300;

            if (length < minLength[0])
            {
                outputParameter.Add(baseCurve.Domain.T1);
                outputType.Add((WindowType)0);
            }

            while (currentPos + minLength[0] < length)
            {
                double tempAdd = 0;
                double targetIndex = 0;

                for (int i = minLength.Length - 1; i >= 0; i--)
                {
                    if (target[i] != 0)
                    {
                        tempAdd = minLength[i];
                        target[i] --;
                        targetIndex = i;

                        break;
                    }
                }

                if (currentPos + tempAdd < length)
                {
                    currentPos = currentPos + tempAdd;

                    outputParameter.Add(currentPos);
                    outputType.Add((WindowType)targetIndex);

                    currentPos += wallLength;

                    outputParameter.Add(currentPos);
                    outputType.Add(WallType.wall);
                }
            }

            outputParameter.RemoveAt(outputParameter.Count() - 1);

            if (outputParameter.Count != 0)
            {
                double additionPerWindow = (length - outputParameter[outputParameter.Count() - 1]) / ((outputParameter.Count() + 1) / 2);

                for (int i = 0; i < outputParameter.Count(); i++)
                {
                    outputParameter[i] = outputParameter[i] + ((int)(i / 2) + 1) * additionPerWindow;
                }
            }

            List<Curve> output = baseCurve.Split(outputParameter).ToList();
            type = outputType;

            return output;
        }

        private  static List<Curve> drawWindow(WindowType windowType,Curve baseCurve, double height)
        {
            double PaneSize = 1200;
            double frameWidth = 30;

            Vector3d tempUnitVector = (new Vector3d(baseCurve.PointAt(baseCurve.Domain.T1) - baseCurve.PointAt(baseCurve.Domain.T0))) / baseCurve.GetLength();

            List<double> splitDomain = new List<double>();

            for (int i = 0; i < ((int)windowType); i++)
            {
                double tempParameter;
                baseCurve.LengthParameter(i * baseCurve.GetLength() + Math.Pow(-1, i) * PaneSize, out tempParameter);

                splitDomain.Add(tempParameter);
            }

            Curve[] tempShatteredCurve = baseCurve.Split(splitDomain);

            List<Curve> outputBase = new List<Curve>();

            foreach (Curve i in tempShatteredCurve)
                outputBase.Add(offsetOneSide(i, height));

            List<Curve> output = new List<Curve>();

            foreach(Curve i in outputBase)
            {
                output.AddRange(offsetInside(i, frameWidth).DuplicateSegments().ToList());
                output.AddRange(i.DuplicateSegments().ToList());
            }

            return output;
        }

        private static Curve offsetInside(Curve baseCurve, double offsetDIstance)
        {
            Curve Curve1 = Curve.JoinCurves(baseCurve.Offset(Plane.WorldXY, offsetDIstance, 0, CurveOffsetCornerStyle.Sharp))[0];
            Curve Curve2 = Curve.JoinCurves(baseCurve.Offset(Plane.WorldXY, -offsetDIstance, 0, CurveOffsetCornerStyle.Sharp))[0];

            if (Curve1.GetLength() > Curve2.GetLength())
                return Curve2;
            else
                return Curve1;
        }

        private static Curve offsetOneSide(Curve baseCurve, double offsetDIstance)
        {
            Curve offsettedCurve = baseCurve.DuplicateCurve();
            offsettedCurve.Transform(Transform.Translation(new Vector3d(0, offsetDIstance, 0)));

            Point3d ptA = offsettedCurve.PointAt(offsettedCurve.Domain.T0);
            Point3d ptB = offsettedCurve.PointAt(offsettedCurve.Domain.T1);
            Point3d ptC = baseCurve.PointAt(baseCurve.Domain.T1);
            Point3d ptD = baseCurve.PointAt(baseCurve.Domain.T0);

            Curve thirdCurve = new LineCurve(offsettedCurve.PointAt(offsettedCurve.Domain.T0), baseCurve.PointAt(baseCurve.Domain.T0));
            Curve fourthCurve = new LineCurve(offsettedCurve.PointAt(offsettedCurve.Domain.T1), baseCurve.PointAt(baseCurve.Domain.T1));

            Point3d[] ptList = { ptA, ptB, ptC, ptD, ptA };

            return new Polyline(ptList).ToNurbsCurve();
        }

        private static List<Curve> drawBanister(Curve baseCurve, double pipeWidth, double banisterHeight)
        {
            List<Curve> output = new List<Curve>();

            double divideLength = baseCurve.GetLength() / ((int)(baseCurve.GetLength() / 200));
            double[] divideByLengthParam = baseCurve.DivideByLength(divideLength, false);

            for(int i = 0; i < divideByLengthParam.Length; i++)
            {
                Point3d tempPoint = baseCurve.PointAt(divideByLengthParam[i]);
                Point3d nextPoint = new Point3d(tempPoint + new Point3d(0,banisterHeight,0));

                Curve tempCurve = new LineCurve(tempPoint, nextPoint);

                output.Add(tempCurve);
            }

            Curve handRail = baseCurve.DuplicateCurve();
            handRail.Transform(Transform.Translation(new Vector3d(0, banisterHeight, 0)));

            output.Add(handRail);

            return output;
        }

        private enum WindowType { noPane, onePane, twoPanes };
        private enum WallType { wall };
    }
}