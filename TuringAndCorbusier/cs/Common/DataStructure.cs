using System;
using System.Collections.Generic;
using Rhino.Geometry;
using System.Linq;
using Rhino.Display;
using System.IO;

namespace TuringAndCorbusier
{
    /// Consts, 고정값들

    public class Consts
    {
        public static double PilotiHeight { get { return 3300; } }
        public static double FloorHeight { get { return 2800; } }
        public static double RooftopHeight { get { return 2800; } }
        public static double AreaLimit { get { return 50 * 1000 * 1000; } }
        public static double balconyDepth { get { return 1200; } }
        public static double minimumArea { get { return 20 * 1000 * 1000; } }
        public static double corridorWidth { get { return 1300; } }
        public static double corridorWallHeight { get { return 1200; } }
        public static string connectionString
        {
            get
            {
                return "Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=oracle3.ejudata.co.kr)(PORT=1521))(CONNECT_DATA=(SERVER=DEDICATED)(SID = oracle3)));User Id=whruser_sh;Password=ejudata;";
            }
        }
        public static double exWallThickness { get { return 300; } }
        public static double inWallThickness { get { return 200; } }

        public static double balconyRate_Tower { get { return 1.16; } }
        public static double balconyRate_Corridor { get { return 1.1; } }
    }

    /// Enumerations

    public enum PlotType { 제1종일반주거지역, 제2종일반주거지역, 제3종일반주거지역, 상업지역 };

    public enum BuildingType { Apartment, RowHouses };

    // AG를 만들기 위해 필요한 재료들

    [Serializable]
    public class Plot
    {
        //Constructor, 생성자
        public bool ignoreNorth { get; set; } = false;
        public bool isSpecialCase { get; set; } = false;

        bool Adjusted = false;

        public Curve OriginalBoundary { get; set; }
        public List<double> OriginalRoadwidths { get; set; }

        public Plot()
        {
        }

        public Plot(Plot toCopy)
        {
            plotType = toCopy.PlotType;
            boundary = toCopy.boundary;
            AlignBoundary();
            surroundings = toCopy.surroundings;
            outrect = toCopy.outrect;
            SimplifiedSurroundings = toCopy.SimplifiedSurroundings;
            SimplifiedBoundary = toCopy.SimplifiedBoundary;
            layout = toCopy.layout;
            ignoreNorth = toCopy.ignoreNorth;
            isSpecialCase = toCopy.isSpecialCase;

        }

        public Plot(Curve boundary, List<Curve> layout, int[] surroundings)
        {
            this.boundary = boundary;
            AlignBoundary();
            this.surroundings = surroundings;
            this.layout = layout;
            Polyline tempPolyline;
            boundary.TryGetPolyline(out tempPolyline);

            Polyline simplifiedPolyline = tempPolyline;

            this.SimplifiedBoundary = simplifiedPolyline.ToNurbsCurve();

            this.SimplifiedSurroundings = surroundings;
        }
        /// <summary>
        /// kdg 정보로 생성한 plot
        /// layout 이 주변 건물, outrect가 외곽 사각형
        /// </summary>
        /// <param name="kdg"></param>
        /// <param name="surroundings"></param>
        public Plot(KDGinfo kdg)
        {
            this.boundary = kdg.boundary;
            AlignBoundary();
            this.layout = kdg.surrbuildings;
            this.outrect = kdg.outrect;

            Polyline tempPolyline;
            boundary.TryGetPolyline(out tempPolyline);

            int[] surroundings = new int[tempPolyline.GetSegments().Length];

            for (int i = 0; i < surroundings.Length; i++)
            {
                surroundings[i] = 4000;
            }

            this.surroundings = surroundings;

            Polyline simplifiedPolyline = tempPolyline;

            this.SimplifiedBoundary = boundary;
            this.SimplifiedSurroundings = surroundings;

            /*

            Polyline simplifiedPolyline = fishInvasion(tempPolyline);

            this.SimplifiedBoundary = simplifiedPolyline.ToNurbsCurve();

            this.SimplifiedSurroundings = RemapRoadWidth(tempPolyline, surroundings.ToList(), simplifiedPolyline).ToArray();
            */
        }

        public Plot(Curve boundary)
        {
            this.boundary = boundary;
            AlignBoundary();
            Polyline tempPolyline;
            boundary.TryGetPolyline(out tempPolyline);
             
            int[] surroundings = new int[tempPolyline.GetSegments().Length];

            for (int i = 0; i < surroundings.Length; i++)
            {
                surroundings[i] = 4000;
            }

            this.surroundings = surroundings;

            this.SimplifiedBoundary = boundary;
            this.SimplifiedSurroundings = surroundings;


            /*

            Polyline simplifiedPolyline = fishInvasion(tempPolyline);

            this.SimplifiedBoundary = simplifiedPolyline.ToNurbsCurve();

            this.SimplifiedSurroundings = RemapRoadWidth(tempPolyline, surroundings.ToList(), simplifiedPolyline).ToArray();
            */
        }

        public Plot(Curve boundary, int[] surroundings)
        {
            this.boundary = boundary;
            AlignBoundary();
            this.surroundings = surroundings;
            
            Polyline tempPolyline;
            boundary.TryGetPolyline(out tempPolyline);

            this.SimplifiedBoundary = boundary;
            this.SimplifiedSurroundings = surroundings;


          
            /*

            Polyline simplifiedPolyline = fishInvasion(tempPolyline);

            this.SimplifiedBoundary = simplifiedPolyline.ToNurbsCurve();

            this.SimplifiedSurroundings = RemapRoadWidth(tempPolyline, surroundings.ToList(), simplifiedPolyline).ToArray();
            */
        }

        //Field, 필드

        private PlotType plotType = PlotType.제1종일반주거지역;
        private Curve boundary;
        private int[] surroundings;
        public Curve outrect;
        //Method, 메소드

        public void Adjust()
        {
            if (Adjusted)
                return;


            //새로 지정한 대지선으로 다시 커브 그리기
            List<Curve> limitLines = boundary.DuplicateSegments().ToList();
            List<double> roadWidth = surroundings.Select(n=>(double)n).ToList();
            List<Point3d> newPoints = new List<Point3d>();
            for (int i = 0; i < limitLines.Count; i++)
            {
                newPoints.Add(limitLines[i].PointAtStart);
            }
            newPoints.Add(limitLines[limitLines.Count - 1].PointAtEnd);
            Curve newLand = new Polyline(newPoints).ToNurbsCurve();

            Gagak gagak = new Gagak();
            gagak.RefineEdge(newLand, roadWidth);

            OriginalBoundary = boundary;
            OriginalRoadwidths = surroundings.Select(n=>(double)n).ToList();

            boundary = gagak.finalLand;
            surroundings = gagak.newRoadWidth.Select(n => (int)n).ToArray();
            Adjusted = true;
        }

        public void AlignBoundary()
        {
            if (boundary.ClosedCurveOrientation(Plane.WorldXY) != CurveOrientation.CounterClockwise)
            {
                Curve x = boundary.DuplicateCurve();
                x.Reverse();
                boundary = x;
            }
        }

        public double GetArea()
        {
            Polyline plotPolyline;
            boundary.TryGetPolyline(out plotPolyline);

            List<Point3d> y = new List<Point3d>(plotPolyline);
            double area = 0;

            for (int i = 0; i < y.Count - 1; i++)
            {
                area += y[i].X * y[i + 1].Y;
                area -= y[i].Y * y[i + 1].X;
            }

            area /= 2;

            return (area < 0 ? -area : area);
        }


        public static List<int> RemapRoadWidth(Polyline originalPLine, List<int> roadWidth, Polyline newPLine)
        {
            Curve[] originalCrvs = originalPLine.ToNurbsCurve().DuplicateSegments();
            Curve[] newCrvs = newPLine.ToNurbsCurve().DuplicateSegments();

            List<List<int>> roadWidthList = new List<List<int>>();

            foreach (Curve i in newCrvs)
                roadWidthList.Add(new List<int>());

            for (int i = 0; i < originalCrvs.Count(); i++)
            {
                List<double> tempDistanceSet = new List<double>();

                for (int j = 0; j < newCrvs.Count(); j++)
                {
                    double tempClosestParam = 0;
                    newCrvs[j].ClosestPoint(originalCrvs[i].PointAt(originalCrvs[i].Domain.Mid), out tempClosestParam);

                    tempDistanceSet.Add(newCrvs[j].PointAt(tempClosestParam).DistanceTo(originalCrvs[i].PointAt(originalCrvs[i].Domain.Mid)));
                }

                List<double> tempDistanceSetCopy = new List<double>(tempDistanceSet);
                tempDistanceSetCopy.Sort();

                roadWidthList[tempDistanceSet.IndexOf(tempDistanceSetCopy[0])].Add(roadWidth[i]);
            }

            List<int> output = new List<int>();

            for (int i = 0; i < newCrvs.Count(); i++)
            {
                roadWidthList[i].Sort();

                output.Add(roadWidthList[i][0]);
            }

            return output;
        }

        public static Polyline fishInvasion(Polyline pLine)
        {
            Polyline currentPline = new Polyline();
            Polyline tempCalculatedPline = pLine;

            int counter1 = 0;

            while (currentPline.Length != tempCalculatedPline.Length && counter1 < pLine.GetSegments().Count() / 2)
            {
                currentPline = tempCalculatedPline;
                tempCalculatedPline = fish1(currentPline);
                counter1++;
            }

            currentPline = new Polyline();
            int counter2 = 0;

            while (currentPline.Length != tempCalculatedPline.Length && counter2 < pLine.GetSegments().Count() / 2)
            {
                currentPline = tempCalculatedPline;
                tempCalculatedPline = fish2(currentPline);

                counter2++;
            }

            currentPline = new Polyline();
            int counter3 = 0;

            while (currentPline.Length != tempCalculatedPline.Length && counter3 < pLine.GetSegments().Count() / 2)
            {
                currentPline = tempCalculatedPline;
                tempCalculatedPline = fish4(currentPline);
                counter3++;
            }

            return currentPline;
        }

        private static Polyline fish1(Polyline pLine)
        {
            try
            {
                Line[] segments = pLine.GetSegments();
                Curve[] segmentCrvs = new Curve[segments.Length];
                List<double> angles = internalAngle(pLine);
                List<Point3d> pts = new List<Point3d>();

                for (int i = 0; i < segments.Length; i++)
                {
                    segmentCrvs[i] = segments[i].ToNurbsCurve();
                    pts.Add(segments[i].PointAt(0));
                }

                int target = 0; double targetLength = double.MaxValue;

                for (int i = 0; i < pts.Count; i++)
                {
                    Point3d ptA = pts[(pts.Count + i - 1) % pts.Count];
                    Point3d ptB = pts[(pts.Count + i + 1) % pts.Count];
                    Curve lengthCurve = new LineCurve(ptA, ptB);
                    double length = lengthCurve.GetLength();

                    if (angles[i] < 180 && length < targetLength)
                    {
                        target = i;
                        targetLength = length;
                    }
                }

                if (targetLength < 20000)
                {
                    pts.RemoveAt(target);
                    pts.Add(pts[0]);
                    Polyline alt = new Polyline(pts);

                    return alt;
                }
                else
                    return pLine;
            }
            catch (System.Exception)
            {
                return pLine;
            }
        }

        private static Polyline fish2(Polyline pLine)
        {
            try
            {
                Line[] segments = pLine.GetSegments();
                Curve[] segmentCrvs = new Curve[segments.Length];
                List<double> angles = internalAngle(pLine);
                List<Point3d> pts = new List<Point3d>();

                for (int i = 0; i < segments.Length; i++)
                {
                    segmentCrvs[i] = segments[i].ToNurbsCurve();
                    pts.Add(segments[i].PointAt(0));
                }

                int target = 0; double targetDist = double.MaxValue;

                for (int i = 0; i < segments.Length; i++)
                {
                    Point3d ptA = pts[(pts.Count + i - 1) % pts.Count];
                    Point3d ptB = pts[(pts.Count + i + 1) % pts.Count];
                    Point3d ptMe = pts[(pts.Count + i) % pts.Count];
                    Line line = new Line(ptA, ptB);

                    double distance = line.DistanceTo(ptMe, false);

                    if (angles[i] < 180 && distance < targetDist)
                    {
                        target = i;
                        targetDist = distance;
                    }
                }

                if (targetDist < 10000)
                {
                    pts.RemoveAt(target);
                    pts.Add(pts[0]);
                    Polyline alt = new Polyline(pts);

                    return alt;
                }
                else
                    return pLine;
            }
            catch (System.Exception)
            {
                return pLine;
            }
        }

        private static Polyline fish3(Polyline pLine, double offsetDist)
        {
            try
            {
                Line[] segments = pLine.GetSegments();
                Curve[] segmentCrvs = new Curve[segments.Length];
                List<double> angles = internalAngle(pLine);
                List<Point3d> pts = new List<Point3d>();

                for (int i = 0; i < segments.Length; i++)
                {
                    segmentCrvs[i] = segments[i].ToNurbsCurve();
                    pts.Add(segments[i].PointAt(0));
                }

                double targetLength = double.MaxValue;
                int target = 0;

                for (int i = 0; i < segments.Length; i++)
                {
                    double angleA = angles[(segments.Length + i) % segments.Length] / 360 * 2 * Math.PI;
                    double angleB = angles[(segments.Length + i + 1) % segments.Length] / 360 * 2 * Math.PI;
                    double outputLength = segments[i].Length - Math.Cos(angleA * 0.5) * offsetDist - Math.Cos(angleB * 0.5) * offsetDist;

                    if (outputLength < targetLength)
                    {
                        targetLength = outputLength;
                        target = i;
                    }
                }

                if (targetLength < 10000)
                {
                    double lineALength = segments[(segments.Length + target - 1) % segments.Length].Length;
                    double lineBLength = segments[(segments.Length + target + 1) % segments.Length].Length;
                    Line lineMe = segments[(segments.Length + target) % segments.Length];

                    if (lineALength > lineBLength)
                    {
                        pts[(segments.Length + target) % segments.Length] = lineMe.PointAt(0.5);
                        pts.RemoveAt((segments.Length + target + 1) % segments.Length);
                        pts.Add(pts[0]);
                    }
                    else
                    {
                        pts[(segments.Length + target + 1) % segments.Length] = lineMe.PointAt(0.5);
                        pts.RemoveAt((segments.Length + target) % segments.Length);
                        pts.Add(pts[0]);
                    }

                    Polyline alt = new Polyline(pts);
                    return alt;
                }
                else
                    return pLine;
            }
            catch (System.Exception)
            {
                return pLine;
            }
        }

        private static Polyline fish4(Polyline pLine)
        {
            try
            {
                Line[] segments = pLine.GetSegments();
                Curve[] segmentCrvs = new Curve[segments.Length];
                List<double> angles = internalAngle(pLine);
                List<Point3d> pts = new List<Point3d>();

                for (int i = 0; i < segments.Length; i++)
                {
                    segmentCrvs[i] = segments[i].ToNurbsCurve();
                    pts.Add(segments[i].PointAt(0));
                }

                int target = 0; double targetAngle = double.MaxValue;

                for (int i = 0; i < segments.Length; i++)
                {
                    if (angles[i] < targetAngle)
                    {
                        targetAngle = angles[i];
                        target = i;
                    }
                }

                if (targetAngle < 90)
                {
                    Line lineA = segments[(segments.Length + target - 1) % segments.Length];
                    Line lineB = segments[(segments.Length + target) % segments.Length];

                    if (lineA.Length < lineB.Length)
                    {
                        double lengthFromPt = lineA.Length * Math.Cos(targetAngle * 2 * Math.PI / 360);
                        pts[target] = lineB.PointAt(lengthFromPt / lineB.Length);
                        pts.Add(pts[0]);
                    }
                    else
                    {
                        double lengthFromPt = lineB.Length * Math.Cos(targetAngle * 2 * Math.PI / 360);
                        pts[target] = lineA.PointAt((lineA.Length - lengthFromPt) / lineA.Length);
                        pts.Add(pts[0]);
                    }
                    Polyline alt = new Polyline(pts);
                    return alt;
                }
                else
                    return pLine;
            }
            catch (System.Exception)
            {
                return pLine;
            }
        }

        private static List<double> internalAngle(Polyline pLine)
        {
            Curve pCrv = pLine.ToNurbsCurve();
            Line[] segments = pLine.GetSegments();
            Curve[] segmentCrvs = new Curve[segments.Length];
            Circle[] circle = new Circle[segments.Length];
            Curve[] circleCrvs = new Curve[segments.Length];
            List<double> output = new List<double>();

            for (int i = 0; i < segments.Length; i++)
            {
                segmentCrvs[i] = segments[i].ToNurbsCurve();
                circle[i] = new Circle(segments[i].PointAt(0), 1);
                circleCrvs[i] = circle[i].ToNurbsCurve();
            }

            Brep boundary = Brep.CreatePlanarBreps(segmentCrvs)[0];

            for (int i = 0; i < circleCrvs.Length; i++)
            {
                var intersect = Rhino.Geometry.Intersect.Intersection.CurveCurve(circleCrvs[i], pCrv, 0.1, 0.1);
                List<double> paramsB = new List<double>();

                Curve[] pieces = circleCrvs[i].Split(boundary, 0);

                foreach (Curve j in pieces)
                {
                    double centerParam = j.Domain.Mid;
                    String containStr = pCrv.Contains(j.PointAt(centerParam)).ToString();
                    double length = j.GetLength();

                    if (containStr == "Inside")
                        output.Add(length / 2 / Math.PI * 360);
                }
            }
            return output;
        }
        //Property, 속성

        public List<Curve> layout { get; set; }
        public PlotType PlotType { get { return plotType; } set { plotType = value; } }
        public Curve Boundary { get { return boundary; } set { boundary = value; } }
        public Curve SimplifiedBoundary { get; set; }
        public int[] Surroundings
        {
            get { return surroundings; }
            set { surroundings = value; }
        }

        public int[] SimplifiedSurroundings { get; set; }

        public void UpdateSimplifiedSurroundings()
        {
            SimplifiedSurroundings = surroundings;
        }

    }

    public class ParameterSet
    {
        //Constructor, 생성자
        public ParameterSet()
        {
        }
        public ParameterSet(double[] parameters)
        {
            this.thisParameters = parameters;
            this.height = Math.Max(parameters[0], parameters[1]);
        }

        //Field, 필드

        double[] thisParameters;
        double height;
        public bool using1F = false;
        public bool setback = false;
        public CoreType fixedCoreType = null;
        //Method, 메소드

        //Property, 속성

        public double[] Parameters { get { return thisParameters; } }
        public double Stories { get { return height; } set { height = value; }} 

    }

    public class Target
    {
        //ConstructTor, 생성자

        public Target()
        {
            double[] tempArea = { 55, 85 };
            double[] tempRatio = { 1, 1 };
            Interval[] tempDomains = { new Interval(50, 59), new Interval(80, 90) };
            int[] tempMandatories = { 0, 0 };

            this.area = tempArea.ToList();
            this.ratio = tempRatio.ToList();
            this.domain = tempDomains.ToList();
            this.mandatoryCount = tempMandatories.ToList();
        }

        public Target(List<double> targetArea)
        {
            this.area = targetArea;
            List<double> tempRatio = new List<double>();

            for (int i = 0; i < targetArea.Count; i++)
            {
                tempRatio.Add(1);
            }

            this.ratio = tempRatio;
        }

        public Target(List<double> targetArea, List<double> targetRatio)
        {
            this.area = targetArea;
            this.ratio = targetRatio;
        }

        public Target(List<double> targetArea, List<double> targetRatio, List<Interval> domains , List<int> mandatories)
        {
            this.area = targetArea;
            this.ratio = targetRatio;
            this.domain = domains;
            this.mandatoryCount = mandatories;
        }

        public List<Unit> ToUnit(double width,double coreArea, double stories)
        {
            //except wall area

            List<double> SupplyTemp = SupplyArea(area, width, coreArea);
            List<double> SupplyMax = SupplyArea(domain.Select(n => n.Max).ToList(), width, coreArea);
            List<double> SupplyMin = SupplyArea(domain.Select(n => n.Min).ToList(), width, coreArea);

            List<Unit> units = new List<Unit>();
            for (int i = 0; i < SupplyTemp.Count; i++)
            {
                Unit tempUnit = new Unit();
                tempUnit.Area = SupplyTemp[i];
                tempUnit.Minimum = SupplyMin[i];
                tempUnit.Maximum = SupplyMax[i];
                tempUnit.Rate = ratio[i];
                tempUnit.Required = (int)Math.Ceiling((double)mandatoryCount[i] / stories); // 필요 유닛 수를 예상 층수로 나눔.
                tempUnit.Initialize();
                tempUnit.Length = SupplyTemp[i] / width;
                tempUnit.CoreArea = coreArea / 2;
                //if(tempUnit.Type == UnitType.Corridor) 아....복도진짜 ㅋ
                //    coreArea = Consts.corridorWidth / (width - Consts.corridorWidth) * SupplyTemp[i]
                units.Add(tempUnit);
            }

            return units;
           
        }

        private List<double> SupplyArea(List<double> area, double width, double coreArea)
        {
            List<double> supplyArea = area.Select(n => n / 0.91).ToList();

            //m2 to mm2
            supplyArea = supplyArea.Select(n => n * 1000 * 1000).ToList();

            for (int i = 0; i < supplyArea.Count; i++)
            {
                if (supplyArea[i] < Consts.AreaLimit)
                {
                    //서비스면적 10%
                    supplyArea[i] = supplyArea[i] * Consts.balconyRate_Corridor;
                    //코어&복도
                    supplyArea[i] = supplyArea[i] * (1 + Consts.corridorWidth / (width - Consts.corridorWidth));
                }
                else
                {
                    //서비스면적 18%
                    supplyArea[i] = supplyArea[i] * Consts.balconyRate_Tower;
                    //코어&복도
                    supplyArea[i] += coreArea / 2;
                }
            }
            return supplyArea;
        }
        //Field, 필드

        private List<double> area = new List<double>();
        private List<double> ratio = new List<double>();
        private List<Interval> domain = new List<Interval>();
        private List<int> mandatoryCount = new List<int>();
        //Method, 메소드

        //Property, 속성

        public List<double> Area { get { return area; } }
        public List<double> Ratio { get { return ratio; } }
        public List<Interval> Domain { get { return domain; } }
        public List<int> MandatoryCount { get { return mandatoryCount; } }
    }

    // ApartmentGenerator의 부모 클래스와 출력값 클래스

    public abstract class ApartmentGeneratorBase
    {
        protected CoreType randomCoreType;
        public abstract Apartment generator(Plot plot, ParameterSet parameterSet, Target target);
        public abstract double[] MinInput { get; set; }
        public abstract double[] MaxInput { get; set; }
        public abstract CoreType GetRandomCoreType();
        public abstract double[] GAParameterSet { get; }
        public abstract bool IsCoreProtrude { get; }
        public abstract string GetAGType { get; }
    }

    public class Apartment : IDisposable
    {
        //visible regulation
        public Curve[] topReg { get; set; }
        //Constructor, 생성자
        public Apartment(string AGType, Plot plot, BuildingType buildingType, ParameterSet parameterSet, Target target, List<List<Core>> core, List<List<List<Household>>> household, ParkingLotOnEarth parkingOnEarth, ParkingLotUnderGround parkingUnderGround, List<List<Curve>> buildingOutline, List<Curve> aptLines)
        {
            this.AGtype = AGType;
            this.Plot = plot;
            this.BuildingType = buildingType;
            this.ParameterSet = parameterSet;
            this.Target = target;
            this.Core = core;
            this.Household = household;
            this.HouseholdStatistics = getHouseholdStatistics(household);
            this.ParkingLotOnEarth = parkingOnEarth;
            this.ParkingLotUnderGround = parkingUnderGround;
            this.Green = Green;
            this.buildingOutline = buildingOutline;
            this.AptLines = aptLines;

            this.Commercial = new List<NonResidential>();
            this.PublicFacility = new List<NonResidential>();

        }

        public Apartment(string AGType, Plot plot, BuildingType buildingType, ParameterSet parameterSet, Target target, List<List<Core>> core, List<List<List<Household>>> household, List<NonResidential> commercial, List<NonResidential> publicFacility, ParkingLotOnEarth parkingOnEarth, ParkingLotUnderGround parkingUnderGround, List<Curve> Green, List<List<Curve>> buildingOutline, List<Curve> aptLines)
        {
            ///////20160516_추가된 생성자, 조경면적, 법정조경면적, 공용공간, 근생 추가

            this.AGtype = AGType;
            this.Plot = plot;
            this.BuildingType = buildingType;
            this.ParameterSet = parameterSet;
            this.Target = target;
            this.Core = core;
            this.Household = household;
            this.HouseholdStatistics = getHouseholdStatistics(household);
            this.Commercial = commercial;
            this.PublicFacility = publicFacility;
            this.ParkingLotOnEarth = parkingOnEarth;
            this.ParkingLotUnderGround = parkingUnderGround;
            this.Green = Green;
            this.buildingOutline = buildingOutline;
            this.AptLines = aptLines;
        }

        public Apartment(Apartment other)
        {
            ///////복사

            this.AGtype = other.AGtype;
            this.Plot = other.Plot;
            this.BuildingType = other.BuildingType;
            this.ParameterSet = other.ParameterSet;
            this.Target = other.Target;
            this.Core = other.Core;
            this.Household = other.Household;
            this.HouseholdStatistics = getHouseholdStatistics(other.Household);
            this.Commercial = other.Commercial;
            this.PublicFacility = other.PublicFacility;
            this.ParkingLotOnEarth = other.ParkingLotOnEarth;
            this.ParkingLotUnderGround = other.ParkingLotUnderGround;
            this.Green = other.Green;
            this.buildingOutline = other.buildingOutline;
            this.AptLines = other.AptLines;
        }

        public double GetParkingScore()
        {
            if (ParkingLotUnderGround.Count + ParkingLotOnEarth.GetCount() == 0)
                return 0;

            if (GetLegalParkingLotOfCommercial() + GetLegalParkingLotofHousing() == 1)
                return 0;

            double totalParkingScore = (double)(ParkingLotOnEarth.GetCount() + ParkingLotUnderGround.Count) / (GetLegalParkingLotOfCommercial() + GetLegalParkingLotofHousing());

            if (totalParkingScore >= 1)
                totalParkingScore = 1;

            double groundParkingScore = ParkingLotOnEarth.GetCount() / (ParkingLotOnEarth.GetCount() + ParkingLotUnderGround.Count);
            return totalParkingScore * 5 + groundParkingScore;
        }
        public double GetAxisAccuracy()
        {

            if (AptLines.Count == 0)
                return 0;

            Vector3d aptAxis = AptLines[0].TangentAtStart;
            //double angleTolerance = Math.PI * (double)5 / 180;
            if (aptAxis == Vector3d.Unset)
                return 0;

            if (GetGrossArea() == 0)
                return 0;

            Curve innerRect;
            Polyline plotPoly = new Polyline();
            plot.Boundary.TryGetPolyline(out plotPoly);
            Utility.InnerIsoDrawer isoDrawer = new Utility.InnerIsoDrawer(plotPoly, 2000, 0);
            Utility.Isothetic iso = isoDrawer.Draw();
            innerRect = iso.Outline.ToNurbsCurve();

            Vector3d[] innerRectAxis = innerRect.DuplicateSegments().Select(n => n.TangentAtStart).ToArray();

            double minimum = Math.PI;
            
            for (int i = 0; i < innerRectAxis.Length; i++)
            {
                double tempAngle = Vector3d.VectorAngle(aptAxis, innerRectAxis[i], Plane.WorldXY);
                if (tempAngle < minimum)
                {
                    minimum = tempAngle;
                }    
            }

            double resultValue = Math.Round(Math.PI - minimum, 2);
            if (resultValue > Math.PI)
                resultValue = Math.PI;

            else
            {
                if (resultValue < Math.PI / 4)
                {

                }
                else if (resultValue < Math.PI / 2)
                {
                    resultValue = Math.Abs(Math.PI / 2 - resultValue);
                }
                else if (resultValue < Math.PI / 4 * 3)
                {
                    resultValue = resultValue - Math.PI / 2;
                }
                else
                {
                    resultValue = Math.Abs(Math.PI - resultValue);
                }
            }

            return ((Math.PI - resultValue) / Math.PI)*10;
        }
        public double GetTargetAccuracy()
        {
            var target = Target.Area.OrderByDescending(n => n);
            if (HouseholdStatistics == null)
                return 0;
            if (HouseholdStatistics.Count == 0)
                return 0;
            var result = HouseholdStatistics.Select(n => n.ExclusiveArea / 1000000).OrderByDescending(n => n);

            double targetaccuracy = 0;

            if (result.Count() == 0)
                return 0;

            foreach (var r in result)
            {
                var distances = target.Select(n => Math.Abs(r - n)).ToList();
                if (distances.Min() <= 1)
                    targetaccuracy += 1;
                else if (distances.Min() <= 3)
                    targetaccuracy += 0.7;
                else if (distances.Min() <= 5)
                    targetaccuracy += 0.3;
                else if (distances.Min() <= 7)
                    targetaccuracy += 0.1;
            }

            //foreach (var t in target)
            //{
            //    var distances = result.Select(n => Math.Abs(t - n)).ToList();
            //    if (distances.Min() <= 1)
            //        targetaccuracy += 1;
            //    else if (distances.Min() <= 3)
            //        targetaccuracy += 0.7;
            //    else if (distances.Min() <= 5)
            //        targetaccuracy += 0.3;
            //    else if (distances.Min() <= 7)
            //        targetaccuracy += 0.1;   
            //}

            return targetaccuracy /= result.Count();
            
        }

        public void InitComplete()
        {
            IsNull = false;
            if (AGtype == null)
                return;

            if (Plot == null)
                return;

            if (Plot.Boundary == null)
                return;

            //if(Commercial[0].)
            

        }

        public Apartment(Plot plot)
        {
            this.IsNull = true;

            this.AGtype = "";
            this.Plot = plot;
            this.BuildingType = new BuildingType();
            //this.ParameterSet = new ParameterSet();
            this.ParkingLotOnEarth = new ParkingLotOnEarth();
            this.ParkingLotUnderGround = new ParkingLotUnderGround();
            this.Target = new Target();
            this.Core = new List<List<Core>>();
            this.Household = new List<List<List<Household>>>();
            this.Commercial = new List<NonResidential>();
            this.PublicFacility = new List<NonResidential>();
            this.Green = new List<Curve>();
            this.buildingOutline = new List<List<Curve>>();
            this.AptLines = new List<Curve>();
        }

        //Property, 속성

        private Plot plot;
      
        public int BuildingGroupCount { get; set; }
        public bool IsNull { get; private set; }
        public string AGtype { get; private set; }
        public Plot Plot { get { return plot; } private set { plot = new Plot(value); } }
        public BuildingType BuildingType { get; private set; }
        public ParameterSet ParameterSet { get; private set; }
        public ParkingLotOnEarth ParkingLotOnEarth { get; set; }
        public ParkingLotUnderGround ParkingLotUnderGround { get; set; }
        public Target Target { get; private set; }
        public List<List<Core>> Core { get; private set; }
        public List<List<List<Household>>> Household { get; private set; }
        public List<HouseholdStatistics> HouseholdStatistics { get; private set; }
        public List<NonResidential> Commercial { get; set; }
        public List<NonResidential> PublicFacility { get; set; }
        public List<Curve> Green { get; private set; }
        public List<List<Curve>> buildingOutline { get; private set; }
        public List<Curve> AptLines { get; private set; }

        //method, 메소드

        ////////////////////////////////////////////
        //////////  household statistics  //////////
        ////////////////////////////////////////////

        private List<HouseholdStatistics> getHouseholdStatistics(List<List<List<Household>>> hhp)
        {
            List<Household> all = new List<Household>();
            for (int i = 0; i < hhp.Count; i++)
            {
                for (int j = 0; j < hhp[i].Count; j++)
                {
                    for (int k = 0; k < hhp[i][j].Count; k++)
                    {
                        all.Add(hhp[i][j][k]);
                    }
                }
            }
            all = all.OrderBy(n => n.ExclusiveArea).ToList();
            List<Household> hhpDistinct = new List<Household>();
            List<int> hhpCount = new List<int>();

            for (int i = 0; i < all.Count; i++)
            {
                bool used = false;
                for (int j = 0; j < hhpDistinct.Count; j++)
                {
                    if (isHHPsimilar(all[i], hhpDistinct[j]))
                    {
                        hhpCount[j] += 1;
                        used = true;
                    }
                }
                if (!used)
                {
                    hhpDistinct.Add(all[i]);
                    hhpCount.Add(1);
                }
            }

            List<HouseholdStatistics> output = new List<HouseholdStatistics>();
            for (int i = 0; i < hhpDistinct.Count; i++)
            {
                output.Add(new HouseholdStatistics(hhpDistinct[i], hhpCount[i]));
            }

            return output;
        }

        private bool isHHPequal(Household a, Household b)
        {
            double dXa = a.XLengthA - b.XLengthA;
            double dXb = a.XLengthB - b.XLengthB;
            double dYa = a.YLengthA - b.YLengthA;
            double dYb = a.YLengthB - b.YLengthB;

            if (dXa == 0 && dXb == 0 && dYa == 0 && dYb == 0)
                return true;
            else
                return false;
        }

        private bool isHHPsimilar(Household a, Household b)
        {
            double aa = a.GetExclusiveArea();
            double bb = b.GetExclusiveArea();
            if (Math.Round(aa) == Math.Round(bb))
                return true;
            else
                return false;
            /*
            double dXa = Math.Abs(a.XLengthA - b.XLengthA);
            double dXb = Math.Abs(a.XLengthB - b.XLengthB);
            double dYa = Math.Abs(a.YLengthA - b.YLengthA);
            double dYb = Math.Abs(a.YLengthB - b.YLengthB);

            if (dXa < 100 && dXb < 100 && dYa < 100 && dYb < 100 && a.WallFactor.SequenceEqual(b.WallFactor))
                return true;
            else
                return false;
            */

        }

        //////////////////////////////////////

        public List<string> AreaTypeString()
        {
            string[] alphabetArray = { string.Empty, "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z" };

            List<int> areaType = new List<int>();
            List<string> output = new List<string>();

            for (int i = 0; i < HouseholdStatistics.Count; i++)
            {
                areaType.Add((int)(Math.Round(HouseholdStatistics[i].GetExclusiveArea() / 100000 / 3.3, 0)));
            }

            List<int> tempAreaCheckList = new List<int>();
            List<int> tempSameAreaCheck = new List<int>();

            for (int i = 0; i < areaType.Count(); i++)
            {
                int tempAreaType = areaType[i];
                int tempIndex = tempAreaCheckList.IndexOf(tempAreaType);

                if (tempIndex != -1)
                {
                    tempSameAreaCheck[tempIndex] += 1;
                    output.Add(alphabetArray[tempSameAreaCheck[tempIndex]]);
                }
                else
                {
                    tempAreaCheckList.Add(tempAreaType);
                    tempSameAreaCheck.Add(1);
                    output.Add(alphabetArray[1]);
                }
            }
            return output;
        }
        public double CalculateLegalGreen()
        {
            if (this.GetGrossArea() > 2000 * 1000000)
                return this.Plot.GetArea() * 0.15;
            else if (this.GetGrossArea() > 1000 * 1000000)
                return this.Plot.GetArea() * 0.1;
            else if (this.GetGrossArea() > 200 * 1000000)
                return this.Plot.GetArea() * 0.05;
            else
                return 0;
        }

        public double GetCommercialArea()
        {
            double output = 0;

            if (this.Commercial != null)
            {
                foreach (NonResidential i in this.Commercial)
                {
                    output += i.GetArea();

                }

                return output;
            }
            else
            {
                return 0;
            }
        }

        public double GetPublicFacilityArea()
        {
            double output = 0;

            if (this.PublicFacility != null)
            {
                foreach (NonResidential i in PublicFacility)
                {
                    output += i.GetArea();
                }


                return output;
            }
            else
            {
                return 0;
            }
        }

        public double GetGreenArea()
        {
            double output = 0;

            if (this.Green != null)
            {
                foreach (Curve i in Green)
                {
                    output += CommonFunc.getArea(i);
                }

                return output;
            }
            else
            {
                return 0;
            }
        }

        public List<Curve> drawCommercialArea()
        {
            List<Curve> output = new List<Curve>();

            foreach (NonResidential i in Commercial)
                output.Add(i.ToNurbsCurve());

            return output;
        }

        public List<Curve> drawPublicFacilities()
        {
            List<Curve> output = new List<Curve>();

            foreach (NonResidential i in PublicFacility)
                output.Add(i.ToNurbsCurve());

            return output;
        }

        public List<Curve> drawEachCore()
        {
            List<Curve> coreOutlines = new List<Curve>();

            try
            {
                for (int i = 0; i < this.Core.Count; i++)
                {
                    for (int j = 0; j < this.Core[i].Count; j++)
                    {
                        coreOutlines.Add(Core[i][j].DrawOutline());
                    }
                }
            }
            catch (Exception)
            {
                return coreOutlines;
            }

            return coreOutlines;
        }

        public List<Curve> drawEachHouse()
        {
            List<Curve> houseOutlines = new List<Curve>();

            try
            {
                for (int i = 0; i < this.Household.Count; i++)
                {
                    for (int j = 0; j < this.Household[i].Count; j++)
                    {
                        for (int k = 0; k < this.Household[i][j].Count; k++)
                        {
                            houseOutlines.Add(this.Household[i][j][k].GetOutline());
                        }

                    }
                }
            }
            catch (Exception)
            {
                return houseOutlines;
            }

            return houseOutlines;
        }



        public List<List<List<List<Line>>>> getLightingWindow()
        {
            List<List<List<List<Line>>>> output = new List<List<List<List<Line>>>>();

            for (int i = 0; i < this.Household.Count; i++)
            {
                List<List<List<Line>>> tempLine_i = new List<List<List<Line>>>();

                for (int j = 0; j < this.Household[i].Count; j++)
                {
                    List<List<Line>> tempLine_j = new List<List<Line>>();

                    for (int k = 0; k < this.Household[i][j].Count; k++)
                    {
                        List<Line> tempLine_k = new List<Line>();

                        for (int l = 0; l < this.Household[i][j][k].LightingEdge.Count; l++)
                        {
                            tempLine_k.Add(this.Household[i][j][k].LightingEdge[l]);
                        }

                        tempLine_j.Add(tempLine_k);
                    }

                    tempLine_i.Add(tempLine_j);
                }

                output.Add(tempLine_i);
            }

            return output;
        }

        public double GetBalconyArea()
        {
            double output = 0;

            foreach (List<List<Household>> i in this.Household)
            {
                foreach (List<Household> j in i)
                {
                    foreach (Household k in j)
                    {
                        output += k.GetBalconyArea();
                    }
                }
            }

            return output;
        }

        public double[] GetBuildingAreaPerApartment()
        {
            List<double> output = new List<double>();

            for (int i = 0; i < this.BuildingGroupCount; i++)
            {
                double tempSum = 0;

                foreach (var j in this.Core)
                {
                    foreach(var jj in j)
                        if(jj.BuildingGroupNum == i )tempSum += jj.GetArea();
                }

                foreach (var j in this.Household)
                {
                    foreach (var jj in j)
                        foreach (var jjj in jj)
                            if (jjj.BuildingGroupNum == i) tempSum += jjj.GetArea();
                }

                output.Add(tempSum);
            }

            return output.ToArray();
        }

        public double GetBuildingArea()
        {
            double[] buildingAreaPerApartment = this.GetBuildingAreaPerApartment();

            double output = 0;


            output = Household[0].Sum(n => n.Sum(m => m.GetArea() + m.CorridorArea));
            output += Core[0].Sum(n => n.GetArea());

            foreach (NonResidential i in Commercial)
            {
                output = output + i.AdditionalBuildingArea;
            }

            foreach (NonResidential i in PublicFacility)
            {
                output = output + i.AdditionalBuildingArea;
            }

            return output;
        }

        public double GetBuildingCoverage()
        {
            double buildingArea = GetBuildingArea();

            return buildingArea / this.Plot.GetArea() * 100;
        }


        public double GetExclusiveAreaSum()
        {
            double tempSum = 0;

            for (int i = 0; i < this.Household.Count; i++)
            {
                for (int j = 0; j < this.Household[i].Count; j++)
                {
                    for (int k = 0; k < this.Household[i][j].Count; k++)
                    {
                        tempSum += this.Household[i][j][k].GetExclusiveArea();
                    }
                }
            }

            return tempSum;
        }

        public double GetCoreAreaOnEarthSum() ///////////////////////////////////// commercial, public 포함
        {
            if (this.ParameterSet.using1F)
                return 0;

            double coreAreaSum = Core[0].Sum(n=>n.GetArea());

            return coreAreaSum;
        }

        public double GetCoreAreaSum()
        {
            double coreAreaSum = 0;

            foreach (List<Core> i in this.Core)
            {
                coreAreaSum += i.Sum(n => n.GetArea());
            }

            coreAreaSum -= Core.Last().Sum(n => n.GetArea());

            return coreAreaSum;
        }

        public double GetGrossArea()
        {
            double output = 0;



            //공급면적 * 세대

            //공급면적 = 코어 + 벽 + 전용

            //코어면적 = 1층,옥탑 코어 면적 * 세대/전체 비율

            //세대/전체 비율 = 현재 세대 전용면적 / 전체 전용면적

            double tempExclusiveAreaSum = GetExclusiveAreaSum();

            for (int i = 0; i < Household.Count; i++)
            {
                for (int j = 0; j < Household[i].Count; j++)
                {
                    for (int k = 0; k < Household[i][j].Count; k++)
                    {
                        double tempExclusiveArea = Household[i][j][k].GetExclusiveArea();
                        double rate = tempExclusiveArea / tempExclusiveAreaSum;

                        double GroundCoreAreaPerHouse = Core[0].Sum(n => n.GetArea()) * 2 * rate;
                        double coreAreaPerHouse = GroundCoreAreaPerHouse + (GetCoreAreaSum() - Core[0].Sum(n => n.GetArea()) * 2) * rate;
                        double sup = Household[i][j][k].GetWallArea() + Household[i][j][k].GetExclusiveArea() + coreAreaPerHouse;

                        output += sup;
                    }
                }

            }
            //double sup = Math.Round((CORE_AREA + houseHoldStatistic.GetWallArea() + houseHoldStatistic.GetExclusiveArea()) / 1000000, 2);

            //전체면적 - 발코니면적 + 복도면적 합
            //output += Household.Sum(n => n.Sum(m => m.Sum(o => o.GetWallArea() + o.GetExclusiveArea())));
            ////전체코어 - 옥탑코어
            //output += Core.Sum(n => n.Sum(m => m.GetArea())) - Core[0].Sum(n => n.GetArea());

            output += GetCommercialArea();
            output += GetPublicFacilityArea();

            return output;
        }

        public double GetGrossAreaRatio()
        {
            if (!IsNull)
            {
                double grossArea = GetGrossArea();

                return grossArea / this.Plot.GetArea() * 100;
            }
            else
            {
                return 0;
            }

        }


        public int GetLegalParkingLotofHousing()
        {
            double legalParkingLotByUnitNum = 0;
            double legalParkingLotByUnitSize = 0;

            for (int i = 0; i < this.Household.Count; i++)
            {
                for (int j = 0; j < this.Household[i].Count; j++)
                {
                    for (int k = 0; k < this.Household[i][j].Count; k++)
                    {
                        if (this.Household[i][j][k].GetExclusiveArea() > 60 * Math.Pow(10, 6))
                            legalParkingLotByUnitNum += 1;
                        else if (this.Household[i][j][k].GetExclusiveArea() > 30 * Math.Pow(10, 6))
                            legalParkingLotByUnitNum += 0.8;
                        else
                            legalParkingLotByUnitNum += 0.5;

                        if (this.Household[i][j][k].GetExclusiveArea() > 85 * Math.Pow(10, 6))
                            legalParkingLotByUnitSize = this.Household[i][j][k].GetExclusiveArea() / 75000000;
                        else
                            legalParkingLotByUnitSize = this.Household[i][j][k].GetExclusiveArea() / 65000000;
                    }

                }
            }

            if (legalParkingLotByUnitNum > legalParkingLotByUnitSize)
            {
                return (int)(legalParkingLotByUnitNum - (legalParkingLotByUnitNum % 1) + 1);
            }
            else
            {
                return (int)(legalParkingLotByUnitSize - (legalParkingLotByUnitSize % 1) + 1);
            }
        }
        public int GetLegalParkingLotOfCommercial()
        {
            int legalParkingLotByUnitSize = (int)(this.GetCommercialArea() / 134000000);

            return legalParkingLotByUnitSize;
        }


        public int GetHouseholdCount()
        {
            int output = 0;

            for (int i = 0; i < Household.Count(); i++)
            {
                for (int j = 0; j < Household[i].Count(); j++)
                {
                    output += Household[i][j].Count();
                }
            }

            return output;
        }

        // Flag: Has Dispose already been called?
        bool disposed = false;

        // Public implementation of Dispose pattern callable by consumers.
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // Protected implementation of Dispose pattern.
        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                // Free any other managed objects here.
                //
            }

            // Free any unmanaged objects here.
            //
            disposed = true;
        }

        ~Apartment()
        {
            Dispose(false);
        }
    }
    // ApartmentGenerator 세부내용

    public class CoreType
    {
        //public enum CoreType { Horizontal, Parallel, Vertical, Folded };

        //Constructor, 생성자
        private CoreType(string coreType)
        {
            this.coreType = coreType;
        }

        //operator

        public static bool operator ==(CoreType first, CoreType second)
        {
            bool status = false;
            if (first.coreType.ToString() == second.coreType.ToString())
            {
                status = true;
            }
            return status;
        }

        public static bool operator !=(CoreType first, CoreType second)
        {
            bool status = false;
            if (first.coreType.ToString() != second.coreType.ToString())
            {
                status = true;
            }
            return status;
        }


        //Field, 필드

        private string coreType;

        //Method, 메소드

        public static CoreType GerRandomCoreType()
        {
            Random myRandom = new Random();

            string[] coreTypeString = { "Horizontal", "Parallel", "Vertical", "Folded", "Vertical_AG1" };

            return new CoreType(coreTypeString[myRandom.Next(0, coreTypeString.Length)]);
        }

        public double GetWidth()
        {
            if (this.coreType == CoreType.Horizontal.ToString())
                return 7660;
            else if (this.coreType == CoreType.Parallel.ToString())
                return 5200;
            else if (this.coreType == CoreType.Vertical.ToString())
                return 2500;
            else if (this.coreType == CoreType.Folded.ToString())
                return 6060;
            else if (this.coreType == CoreType.Vertical_AG1.ToString())
                return 2500;
            else
                return 0;

        }

        public double GetDepth()
        {
            if (this.coreType == CoreType.Horizontal.ToString())
                return 4400;
            else if (this.coreType == CoreType.Parallel.ToString())
                return 4860;
            else if (this.coreType == CoreType.Vertical.ToString())
                return 8220;
            else if (this.coreType == CoreType.Folded.ToString())
                return 5800;
            else if (this.coreType == CoreType.Vertical_AG1.ToString())
                return 7920;
            else
                return 0;
        }

        public override string ToString()
        {
            return coreType;
            
        }

        //Property, 속성

        public static CoreType Horizontal { get { return new CoreType("Horizontal"); } }
        public static CoreType Parallel { get { return new CoreType("Parallel"); } }
        public static CoreType Vertical { get { return new CoreType("Vertical"); } }
        public static CoreType Folded { get { return new CoreType("Folded"); } }
        public static CoreType Vertical_AG1 { get { return new CoreType("Vertical_AG1"); } }
    }

    public class Core
    {
        public double Area { get; set; }

        //Constructor, 생성자
        public Core()
        {

        }

        public Core(Core anotherCoreProperty)
        {
            this.origin = anotherCoreProperty.origin;
            this.xDirection = anotherCoreProperty.xDirection;
            this.yDirection = anotherCoreProperty.yDirection;
            this.coreType = anotherCoreProperty.coreType;
            this.Stories = anotherCoreProperty.Stories;
            this.Area = anotherCoreProperty.Area;
        }

        public Core(Point3d origin, Vector3d xDirection, Vector3d yDirection, CoreType coreType, double stories, double coreInterpenetration)
        {
            this.origin = origin;
            this.xDirection = xDirection;
            this.yDirection = yDirection;
            this.coreType = coreType;
            this.Stories = stories;

        }

        //Field, 필드

        private Point3d origin;
        private Vector3d xDirection;
        private Vector3d yDirection;
        private CoreType coreType;

        //Method, 메소드

        public double GetArea()
        {
            //if (coreType == CoreType.Horizontal)
            //    return 7660 * 4400 - 3100 * 150 + (4250 + 4400 - this.CoreInterpenetration * 2) * 300;
            //else if (coreType == CoreType.Parallel)
            //    return 5200 * 4860 - 2700 * 300 + (4860 + 4560 - this.CoreInterpenetration * 2) * 300;
            //else if (coreType == CoreType.Folded)
            //    return 5800 * 6060;
            //else if (coreType == CoreType.Vertical)
            //    return 2500 * 8220;
            //else if (CoreType == CoreType.Vertical_AG1)
            //    return 2500 * 7920 + (7920 - this.CoreInterpenetration) * 300 * 2;
            //else
            //    return 0;

            return Area;
        }

        public Curve DrawOutline()
        {
            List<Point3d> outlinePoints = new List<Point3d>();


            Point3d pt = new Point3d(Origin);
            Vector3d x = new Vector3d(XDirection);
            Vector3d y = new Vector3d(YDirection);
            double width = CoreType.GetWidth();
            double depth = CoreType.GetDepth();

            outlinePoints.Add(pt);
            pt.Transform(Transform.Translation(Vector3d.Multiply(x, width)));
            outlinePoints.Add(pt);
            pt.Transform(Transform.Translation(Vector3d.Multiply(y, depth)));
            outlinePoints.Add(pt);
            pt.Transform(Transform.Translation(Vector3d.Multiply(x, -width)));
            outlinePoints.Add(pt);
            pt.Transform(Transform.Translation(Vector3d.Multiply(y, -depth)));
            outlinePoints.Add(pt);

            Polyline outlinePolyline = new Polyline(outlinePoints);
            Curve outlineCurve = outlinePolyline.ToNurbsCurve();
            return outlineCurve;
        }

        //주차배치용 확장
        public Curve DrawOutline(double aptWidth)
        {
            List<Point3d> outlinePoints = new List<Point3d>();


            Point3d pt = new Point3d(Origin);
            Vector3d x = new Vector3d(XDirection);
            Vector3d y = new Vector3d(YDirection);
            double width = CoreType.GetWidth();
            double depth = CoreType.GetDepth();

            if (depth < aptWidth)
                depth = aptWidth;

            outlinePoints.Add(pt);
            pt.Transform(Transform.Translation(Vector3d.Multiply(x, width)));
            outlinePoints.Add(pt);
            pt.Transform(Transform.Translation(Vector3d.Multiply(y, depth)));
            outlinePoints.Add(pt);
            pt.Transform(Transform.Translation(Vector3d.Multiply(x, -width)));
            outlinePoints.Add(pt);
            pt.Transform(Transform.Translation(Vector3d.Multiply(y, -depth)));
            outlinePoints.Add(pt);

            Polyline outlinePolyline = new Polyline(outlinePoints);
            Curve outlineCurve = outlinePolyline.ToNurbsCurve();
            return outlineCurve;
        }

        //Property, 속성
        public int BuildingGroupNum { get; set; }
        public Point3d Origin { get { return origin; } set { origin = value; } }
        public Vector3d XDirection { get { return xDirection; } set { xDirection = value; } }
        public Vector3d YDirection { get { return yDirection; } set { yDirection = value; } }
        public CoreType CoreType { get { return coreType; } set { coreType = value; } }
        public double Stories { get; set; }
        public double CoreInterpenetration { get; set; }
    }

    public class HouseholdStatistics
    {
        public bool isCorridorType { get; set; }
        public HouseholdStatistics()
        {

        }

        public HouseholdStatistics(Household household, int count)
        {
            this.isCorridorType = household.isCorridorType;
            this.origin = household.Origin;
            this.xDirection = household.XDirection;
            this.yDirection = household.YDirection;
            this.xLengthA = household.XLengthA;
            this.xLengthB = household.XLengthB;
            this.yLengthA = household.YLengthA;
            this.yLengthB = household.YLengthB;
            this.householdSizeType = household.HouseholdSizeType;
            this.exclusiveArea = household.GetExclusiveArea() + household.GetWallArea();
            this.lightingEdge = household.LightingEdge;
            this.entrancePoint = household.EntrancePoint;
            this.wallFactor = household.WallFactor;
            this.Count = count;
            this.CorridorArea = household.CorridorArea;
        }

        public HouseholdStatistics(List<HouseholdStatistics> householdStatistics)
        {
            this.isCorridorType = householdStatistics[0].isCorridorType;
            this.origin = householdStatistics[0].Origin;
            this.xDirection = householdStatistics[0].XDirection;
            this.yDirection = householdStatistics[0].YDirection;
            this.xLengthA = householdStatistics[0].XLengthA;
            this.xLengthB = householdStatistics[0].XLengthB;
            this.yLengthA = householdStatistics[0].YLengthA;
            this.yLengthB = householdStatistics[0].YLengthB;
            this.householdSizeType = householdStatistics[0].HouseholdSizeType;

            double exclusivesum = 0;
            int numbers = 0;
            foreach (var hhs in householdStatistics)
            {
                exclusivesum += hhs.exclusiveArea * hhs.Count;
                numbers += hhs.Count;
            }

            this.exclusiveArea = exclusivesum / numbers;


            this.lightingEdge = householdStatistics[0].LightingEdge;
            this.entrancePoint = householdStatistics[0].EntrancePoint;
            this.wallFactor = householdStatistics[0].WallFactor;
            this.Count = numbers;
        }

        //Field, 필드

        private Point3d origin;
        private Vector3d xDirection;
        private Vector3d yDirection;
        private double xLengthA;
        private double xLengthB;
        private double yLengthA;
        private double yLengthB;
        private int householdSizeType;
        private double exclusiveArea;
        private List<Line> lightingEdge;
        private Point3d entrancePoint;
        private List<double> wallFactor;
        //private List<int> connectedCoreIndex;


        //Method, 메소드

        public Household ToHousehold()
        {
            Household output = new Household(this.origin, this.xDirection, this.yDirection, this.XLengthA, this.XLengthB, this.YLengthA, this.YLengthB, this.HouseholdSizeType, this.ExclusiveArea, this.LightingEdge, this.EntrancePoint, this.WallFactor);
            output.isCorridorType = isCorridorType;
            return output;
        }

        public double GetArea()
        {
            return (XLengthA * YLengthA) - (XLengthB * YLengthB);
        }

        public double GetBalconyArea()
        {
            return GetArea() - GetExclusiveArea()-GetWallArea();
        }

        public double GetExclusiveArea()
        {
            if (isCorridorType)
                return (GetArea() - CorridorArea) * 0.91 / Consts.balconyRate_Corridor;
            else
                return GetArea() * 0.91 / Consts.balconyRate_Tower;

        }

        public double GetWallArea()
        {
            return GetArea() * 0.09;
        }


        //Property, 속성

        public int Count { get; private set; }
        public Point3d Origin { get { return origin; } set { origin = value; } }
        public Vector3d XDirection { get { return xDirection; } set { xDirection = value; } }
        public Vector3d YDirection { get { return yDirection; } set { yDirection = value; } }
        public double XLengthA { get { return xLengthA; } }
        public double XLengthB { get { return xLengthB; } }
        public double YLengthA { get { return yLengthA; } }
        public double YLengthB { get { return yLengthB; } }
        public int HouseholdSizeType { get { return householdSizeType; } }
        public double ExclusiveArea { get { return GetExclusiveArea(); } }
        //public List<int> ConnectedCoreIndex { get { return connectedCoreIndex; } }
        public List<Line> LightingEdge { get { return lightingEdge; } }
        public Point3d EntrancePoint { get { return entrancePoint; } }
        public List<double> WallFactor { get { return wallFactor; } }
        public double CorridorArea { get; set; }
    }

    public class Household
    {
        //Constructor, 생성자

        public int[] indexer = new int[] { 0, 0 };
        public bool isCorridorType { get; set; }
        public Household(Point3d origin, Vector3d xDirection, Vector3d yDirection, double xLengthA, double xLengthB, double yLengthA, double yLengthB, int householdSizeType, double exclusiveArea, List<Line> lightingEdge, Point3d entrancePoint, List<double> wallFactor)
        {
            this.Origin = origin;
            this.XDirection = xDirection;
            this.YDirection = yDirection;
            this.XLengthA = xLengthA;
            this.XLengthB = xLengthB;
            this.YLengthA = yLengthA;
            this.YLengthB = yLengthB;
            this.HouseholdSizeType = householdSizeType;
            //this.ExclusiveArea = exclusiveArea;
            //this.LightingEdge = lightingEdge;
            this.LightingEdge = lightingEdge;
            
            this.WallFactor = wallFactor;
            this.EntrancePoint = entrancePoint;
            this.CorridorArea = 0;
            //this.connectedCoreIndex = connectedCoreIndex;
        }

        public Household()
        {
            this.CorridorArea = 0;
        }

        public Household(Household household)
        {
            this.Origin = household.Origin;
            this.XDirection = household.XDirection;
            this.YDirection = household.YDirection;
            this.XLengthA = household.XLengthA;
            this.XLengthB = household.XLengthB;
            this.YLengthA = household.YLengthA;
            this.YLengthB = household.YLengthB;
            this.HouseholdSizeType = household.HouseholdSizeType;
            //this.ExclusiveArea = household.GetExclusiveArea() + household.GetWallArea();
            //this.LightingEdge = household.LightingEdge;
            this.WallFactor = household.WallFactor;
            this.EntrancePoint = household.EntrancePoint;
            this.CorridorArea = household.CorridorArea;
            this.isCorridorType = household.isCorridorType;
            this.LightingEdge = new List<Line>(household.LightingEdge);
            this.MoveableEdge = new List<Line>(household.MoveableEdge);

        }
        public Household(Household household,double downheight)
        {
            this.isCorridorType = household.isCorridorType;
            this.Origin = household.Origin - Vector3d.ZAxis * downheight;
            this.XDirection = household.XDirection;
            this.YDirection = household.YDirection;
            this.XLengthA = household.XLengthA;
            this.XLengthB = household.XLengthB;
            this.YLengthA = household.YLengthA;
            this.YLengthB = household.YLengthB;
            this.HouseholdSizeType = household.HouseholdSizeType;
            //this.ExclusiveArea = household.GetExclusiveArea() + household.GetWallArea();
           
            //this.LightingEdge = household.LightingEdge;
            this.WallFactor = household.WallFactor;
            this.EntrancePoint = household.EntrancePoint - Vector3d.ZAxis * downheight;
            this.CorridorArea = household.CorridorArea;

            this.LightingEdge = new List<Line>(household.LightingEdge);
            this.MoveableEdge = new List<Line>(household.MoveableEdge);

            LightingEdge.ForEach(n => n.Transform(Transform.Translation(-Vector3d.ZAxis * downheight)));
            MoveableEdge.ForEach(n => n.Transform(Transform.Translation(-Vector3d.ZAxis * downheight)));
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
        public override bool Equals(object obj)
        {
            if (obj is Household)
            {
                Household hhp = obj as Household;
                if (hhp.EntrancePoint.X == this.EntrancePoint.X && hhp.EntrancePoint.Y == this.EntrancePoint.Y && hhp.EntrancePoint.Z == this.EntrancePoint.Z)
                    return true;
            }
            return false;
        }
        //Field, 필드

        //Method, 메소드

        //not used
        public List<Line> GetLightingEdges()
        {
            //if (YLengthB == 0)
            //{
            //    return new List<Line>() { new Line(Origin + YDirection * YLengthB, XDirection * (XLengthA - XLengthB)), new Line(Origin - YDirection * (YLengthA - YLengthB), XDirection * XLengthA) };

            //}
            return new List<Line>() { new Line(Origin + YDirection * YLengthB, XDirection * (XLengthA - XLengthB)), new Line(Origin + XDirection * (XLengthA - XLengthB) - YDirection * (YLengthA-YLengthB), -XDirection * XLengthA) };
        }

        public Curve GetOutline()
        {
            List<Point3d> outlinePoints = new List<Point3d>();
            Point3d pt = new Point3d(this.Origin);
            Vector3d x = new Vector3d(this.XDirection);
            Vector3d y = new Vector3d(this.YDirection);
            double xa = this.XLengthA;
            double xb = this.XLengthB;
            double ya = this.YLengthA;
            double yb = this.YLengthB;

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

            if (outlineCurve.ClosedCurveOrientation(Vector3d.ZAxis) == CurveOrientation.CounterClockwise)
                outlineCurve.Reverse();

            return outlineCurve;

        }


        public double GetArea()
        {
            return (XLengthA * YLengthA) - (XLengthB * YLengthB);
        }

        public double GetBalconyArea()
        {
            return GetArea() - GetExclusiveArea() - CorridorArea - GetWallArea(); ;
        }

        public double GetExclusiveArea()
        {
            if (isCorridorType)
                return GetArea() * 0.91 / Consts.balconyRate_Corridor;
            else
                return GetArea() * 0.91 / Consts.balconyRate_Tower;

        }

        public double GetWallArea()
        {
            return GetArea() * 0.09;
        }

        public bool Contract(Curve testCurve)
        {
            if (testCurve.PointAtStart.Z != Origin.Z)
                testCurve.Translate(Vector3d.ZAxis * (Origin.Z - testCurve.PointAtStart.Z));
            
            if (testCurve.Contains(Origin) == PointContainment.Outside)
                return false;

            Curve tempOutline = GetOutline();

            var intersection = Curve.CreateBooleanIntersection(tempOutline, testCurve);

            if (intersection.Length != 1)
                return false;

            if (intersection[0] == tempOutline)
                return true;

            Point3d origin = Origin;

            
            //each side
            for (int i = 0; i < MoveableEdge.Count; i++)
            {
                Curve moveAble = MoveableEdge[i].ToNurbsCurve();
                int windowIndex = -1;
                for (int j = 0; j < LightingEdge.Count; j++)
                {
                    if (moveAble.PointAtStart == LightingEdge[j].From && moveAble.PointAtEnd == LightingEdge[j].To)
                        windowIndex = j;
                }


                //      ----front----
                //      |           |
                // ---- o           |
                // |                |     
                // |                |side    
                // |                |
                // |                |
                // |                |
                // -------back-------


                Curve testLine1;
                Curve testLine2;
                List<Curve> testLines;
                //front
                if (Vector3d.VectorAngle(moveAble.TangentAtStart,XDirection) <= 0.01*Math.PI)
                {
                    Point3d p1 = origin;
                    Point3d p2 = origin + XDirection * (XLengthA - XLengthB);
                    Vector3d v = YDirection * YLengthB;
                    testLine1 = new LineCurve(p1,p1+v);
                    testLine2 = new LineCurve(p2,p2+v);
                    testLines = new List<Curve> { testLine1, testLine2 };
                }

                //side                                                                                              
                else if (Vector3d.VectorAngle(moveAble.TangentAtStart, -YDirection) <= 0.01 * Math.PI)
                {
                    Point3d p1 = origin + YDirection * YLengthB;
                    Point3d p2 = origin - YDirection * (YLengthA - YLengthB);
                    Vector3d v = XDirection * (XLengthA - XLengthB);
                    testLine1 = new LineCurve(p1, p1 + v);
                    testLine2 = new LineCurve(p2, p2 + v);
                    testLines = new List<Curve> { testLine1, testLine2 };
                }

                //back
                else if (Vector3d.VectorAngle(moveAble.TangentAtStart,-XDirection) <= 0.01*Math.PI)
                {
                    Point3d p1 = origin + XDirection * (XLengthA - XLengthB);
                    Point3d p2 = origin - XDirection * XLengthB;
                    Vector3d v = -YDirection * (YLengthA - YLengthB);
                    testLine1 = new LineCurve(p1, p1 + v);
                    testLine2 = new LineCurve(p2, p2 + v);
                    testLines = new List<Curve> { testLine1, testLine2 };
                }
                else
                    continue;

                DebugPoints.Add(testLine1.PointAtEnd);
                DebugPoints.Add(testLine1.PointAtStart);
                DebugPoints.Add(testLine2.PointAtEnd);
                DebugPoints.Add(testLine2.PointAtStart);

                double minDistance = double.MaxValue;
                for (int j = 0; j < testLines.Count; j++)
                {
                    double d = CalculateSetBackDistance(testCurve, testLines[j]);
                    if (d < minDistance)
                        minDistance = d;
                }

                if (minDistance == double.MaxValue)
                    continue;

                //결과적용
                if (Vector3d.VectorAngle(moveAble.TangentAtStart, XDirection) <= 0.01 * Math.PI)
                {
                    if (windowIndex != -1)
                    {
                        Vector3d v = YDirection * -(YLengthB - minDistance);
                        Line l = LightingEdge[windowIndex];
                        LightingEdge[windowIndex] = new Line(l.From + v, l.To + v);
                    }
                    YLengthA -= YLengthB - minDistance;
                    YLengthB = minDistance;
                }

                //side                                                                                              
                else if (Vector3d.VectorAngle(moveAble.TangentAtStart, -YDirection) <= 0.01 * Math.PI)
                {
                    if (windowIndex != -1)
                    {
                        Vector3d v = XDirection * -(XLengthA - XLengthB - minDistance);
                        Line l = LightingEdge[windowIndex];
                        LightingEdge[windowIndex] = new Line(l.From + v, l.To + v);
                    }
                    XLengthA -= XLengthA - XLengthB - minDistance;
                }

                //back
                else if (Vector3d.VectorAngle(moveAble.TangentAtStart, -XDirection) <= 0.01 * Math.PI)
                {
                    if (windowIndex != -1)
                    {
                        Vector3d v = YDirection * (YLengthA - YLengthB - minDistance);
                        Line l = LightingEdge[windowIndex];
                        LightingEdge[windowIndex] = new Line(l.From + v, l.To + v);
                    }
                    YLengthA -= YLengthA - YLengthB - minDistance;
                }

            }
            return true;
        }

        private double CalculateSetBackDistance(Curve testCurve, Curve testline)
        {
            var inter = Rhino.Geometry.Intersect.Intersection.CurveCurve(testCurve, testline, 0, 0);
            double minDistanceBack = double.MaxValue;

            foreach (var e in inter)
            {
                Point3d p = e.PointA;
                DebugPoints.Add(p);
                double distance = p.DistanceTo(testline.PointAtStart);
                if (minDistanceBack > distance)
                    minDistanceBack = distance;
            }

            return minDistanceBack;
        }

        public void MoveLightingAndMoveAble()
        {
            for ( int i = 0; i < MoveableEdge.Count; i++)
            {
                double height = Origin.Z - MoveableEdge[i].From.Z;
                Line temp = new Line(MoveableEdge[i].From + Vector3d.ZAxis * height, MoveableEdge[i].To + Vector3d.ZAxis * height);
                MoveableEdge[i] = temp;
            }

            for (int i = 0; i < LightingEdge.Count; i++)
            {
                double height = Origin.Z - LightingEdge[i].From.Z;
                Line temp = new Line(LightingEdge[i].From + Vector3d.ZAxis * height, LightingEdge[i].To + Vector3d.ZAxis * height);
                LightingEdge[i] = temp;
            }
        }
        public List<Point3d> DebugPoints = new List<Point3d>();
        //Property, 속성
        public int BuildingGroupNum { get { return indexer[0]; } }
        public Point3d Origin { get; set; }
        public Vector3d XDirection { get; set; }
        public Vector3d YDirection { get; set; }
        public double XLengthA { get; set; }
        public double XLengthB { get; set; }
        public double YLengthA { get; set; }
        public double YLengthB { get; set; }
        public int HouseholdSizeType { get; set; }
        //임시로 get public
        public double ExclusiveArea { get { return GetExclusiveArea(); }}
        //public List<int> ConnectedCoreIndex { get { return connectedCoreIndex; } }
        public List<Line> LightingEdge { get; set; }
        public List<Line> MoveableEdge { get; set; }
        public List<double> WallFactor { get; set; }
        public Point3d EntrancePoint { get; set; }
        public double CorridorArea { get; set; }
    }


    public class NonResidential
    {
        public NonResidential(Point3d Origin, Vector3d XVector, Vector3d YVector, double XLength, double YLength, double AdditionalBuildingArea)
        {
            this.Origin = Origin;
            this.XVector = XVector;
            this.YVector = YVector;
            this.XLength = XLength;
            this.YLength = YLength;
            this.AdditionalBuildingArea = AdditionalBuildingArea;
        }


        public static List<NonResidential> createNonResidential(List<ParkingLine> parkingLinesToCombine, List<Curve> existingBuilding, out List<List<int>> removeIndex)
        {
            if (parkingLinesToCombine.Count >= 0)
            {
                List<NonResidential> output = new List<NonResidential>();
                List<List<int>> removeIndexOutput = new List<List<int>>();

                for (int i = 0; i < (int)(parkingLinesToCombine.Count() / 3); i++)
                {
                    List<ParkingLine> tempParkingLinesToCombine = new List<ParkingLine>();

                    List<int> tempRemoveIndex = new List<int>();

                    for (int j = i * 3; j < (i + 1) * 3; j++)
                    {
                        tempParkingLinesToCombine.Add(parkingLinesToCombine[j]);
                        tempRemoveIndex.Add(j);
                    }

                    removeIndexOutput.Add(tempRemoveIndex);

                    List<Curve> tempSegmentsOfReference = tempParkingLinesToCombine[0].Boundary.ToNurbsCurve().DuplicateSegments().ToList();
                    List<Point3d> tempPointsOfReference = (from j in tempSegmentsOfReference
                                                           select j.PointAt(j.Domain.T0)).ToList();

                    Point3d tempOrigin = tempPointsOfReference[0];
                    Vector3d tempXVector = new Vector3d(tempPointsOfReference[1] - tempPointsOfReference[0]);
                    Vector3d tempYVector = new Vector3d(tempPointsOfReference[3] - tempPointsOfReference[0]);
                    tempXVector = tempXVector / tempXVector.Length;
                    tempYVector = tempYVector / tempYVector.Length;

                    Curve tempRectangle = (new Rectangle3d(new Plane(tempOrigin, tempXVector, tempYVector), 2300 * 3, 5000)).ToNurbsCurve();

                    double OverlappedBuildingArea = 0;

                    foreach (Curve j in existingBuilding)
                    {
                        var tempIntersection = Rhino.Geometry.Intersect.Intersection.CurveCurve(tempRectangle, j, 0, 0);

                        if (tempIntersection.Count() != 0)
                        {
                            List<double> AParams = (from k in tempIntersection
                                                    select k.ParameterA).ToList();

                            List<double> BParams = (from k in tempIntersection
                                                    select k.ParameterB).ToList();

                            Curve[] shatteredCurvesA = tempRectangle.Split(AParams);
                            Curve[] shatteredCurvesB = j.Split(BParams);

                            List<Curve> CurvesToJoin = new List<Curve>();

                            for (int k = 0; k < shatteredCurvesA.Count(); k++)
                            {
                                if (j.Contains(shatteredCurvesA[k].PointAt(shatteredCurvesA[k].Domain.Mid)) != PointContainment.Outside)
                                    CurvesToJoin.Add(shatteredCurvesA[k]);
                            }

                            for (int k = 0; k < shatteredCurvesB.Count(); k++)
                            {
                                if (tempRectangle.Contains(shatteredCurvesB[k].PointAt(shatteredCurvesB[k].Domain.Mid)) != PointContainment.Outside)
                                    CurvesToJoin.Add(shatteredCurvesB[k]);
                            }

                            Curve[] tempJoinedCurves = Curve.JoinCurves(CurvesToJoin);

                            foreach (Curve k in tempJoinedCurves)
                            {
                                Curve tempCurve = k.DuplicateCurve();

                                if (!k.IsClosed)
                                    tempCurve.MakeClosed(0);

                                OverlappedBuildingArea += GetCurveArea(tempCurve);
                            }
                        }
                    }

                    double AdditionalBuildingArea = GetCurveArea(tempRectangle) - OverlappedBuildingArea;

                    output.Add(new NonResidential(tempOrigin, tempXVector, tempYVector, 2300 * 3, 5000, AdditionalBuildingArea));
                }

                removeIndex = removeIndexOutput;
                return output;
            }
            else
            {
                removeIndex = new List<List<int>>();
                return new List<NonResidential>();
            }
        }

        private static double GetCurveArea(Curve inputCurve)
        {
            if (inputCurve.IsClosed == true)
            {
                Polyline plotPolyline;
                inputCurve.TryGetPolyline(out plotPolyline);

                List<Point3d> y = new List<Point3d>(plotPolyline);
                double area = 0;

                for (int i = 0; i < y.Count - 1; i++)
                {
                    area += y[i].X * y[i + 1].Y;
                    area -= y[i].Y * y[i + 1].X;
                }

                area /= 2;

                return (area < 0 ? -area : area);
            }
            else
            {
                return 0;
            }
        }

        public Point3d Origin { get; private set; }
        public Vector3d XVector { get; private set; }
        public Vector3d YVector { get; private set; }
        public double XLength { get; private set; }
        public double YLength { get; private set; }
        public double AdditionalBuildingArea { get; private set; }

        public Point3d CenterPoint
        {
            get
            {
                return this.Origin + this.XLength * this.XVector / 2 + this.YLength * this.YVector / 2;
            }
        }

        public double GetArea()
        {
            return XLength * YLength;
        }

        public Curve ToNurbsCurve()
        {
            Rectangle3d tempRectangle = new Rectangle3d(new Plane(Origin, XVector, YVector), XLength, YLength);
            return tempRectangle.ToNurbsCurve();
        }

    }

    public class Regulation

    {

        bool high = false;
        double fakeHeight = 0;
        //Constructor, 생성자

        public Regulation(double stories)
        {
            this.height = Consts.PilotiHeight + Consts.FloorHeight * stories;
            high = true;
            if (stories >= 7 ) // apartment
            {
                this.distanceFromRoad = 3000;
                this.distanceFromPlot = 3000;
                this.BuildingType = BuildingType.Apartment;
            }

            else if (stories >= 5)//apartment(완화)
            {
                this.distanceFromRoad = 1500;
                this.distanceFromPlot = 1500;
                this.BuildingType = BuildingType.Apartment;
            }
            else//rowhouse
            {
                this.distanceFromRoad = 1000;
                this.distanceFromPlot = 750;
                this.BuildingType = BuildingType.RowHouses;
            }



            totalheight = stories;
        }

        //stories = max tempheight = tempstory?
        public Regulation(double storiesHigh,double storiesLow)
        {
            this.height = Consts.PilotiHeight + Consts.FloorHeight * (storiesLow);

            if (storiesHigh >= 7) // apartment
            {
                this.distanceFromRoad = 3000;
                this.distanceFromPlot = 3000;
                this.BuildingType = BuildingType.Apartment;
            }
            else if (storiesHigh >= 5)//apartment(완화)
            {
                this.distanceFromRoad = 1500;
                this.distanceFromPlot = 1500;
                this.BuildingType = BuildingType.Apartment;
            }
            else//rowhouse
            {
                this.distanceFromRoad = 1000;
                this.distanceFromPlot = 750;
                this.BuildingType = BuildingType.RowHouses;
            }

            totalheight = storiesHigh;
        }

        public Regulation(double stories, bool using1F)
        {
            this.height = using1F ? 0 : Consts.PilotiHeight + Consts.FloorHeight * stories;
            high = true;
            if (stories >= 7) // apartment
            {
                this.distanceFromRoad = 3000;
                this.distanceFromPlot = 3000;
                this.BuildingType = BuildingType.Apartment;
            }

            else if (stories >= 5)//apartment(완화)
            {
                this.distanceFromRoad = 1500;
                this.distanceFromPlot = 1500;
                this.BuildingType = BuildingType.Apartment;
            }
            else//rowhouse
            {
                this.distanceFromRoad = 1000;
                this.distanceFromPlot = 750;
                this.BuildingType = BuildingType.RowHouses;
            }



            totalheight = stories;
        }

        //stories = max tempheight = tempstory?
        public Regulation(double storiesHigh, double storiesLow, bool using1F)
        {
            this.height = using1F?0:Consts.PilotiHeight + Consts.FloorHeight * (storiesLow);

            if (storiesHigh >= 7) // apartment
            {
                this.distanceFromRoad = 3000;
                this.distanceFromPlot = 3000;
                this.BuildingType = BuildingType.Apartment;
            }
            else if (storiesHigh >= 5)//apartment(완화)
            {
                this.distanceFromRoad = 1500;
                this.distanceFromPlot = 1500;
                this.BuildingType = BuildingType.Apartment;
            }
            else//rowhouse
            {
                this.distanceFromRoad = 1000;
                this.distanceFromPlot = 750;
                this.BuildingType = BuildingType.RowHouses;
            }

            totalheight = storiesHigh;
        }

        //층수,동간거리 : 최상층 기준, 동간거리를 제외한 법규 : 최상층-1 기준 
        public void Fake()
        {
            fakeHeight = Consts.FloorHeight;
        }

        public void UnFake()
        {
            fakeHeight = 0;
        }


        //Field, 필드
        public double totalheight;
        public double height;
        private double distanceFromRoad; //건축선으로부터 건축물까지 띄어야 하는 거리, 완화
        private double distanceFromPlot; //인접대지 경계선부터 띄어야 하는 거리, 완화
        private double distanceFromNorth = 0.5; //일조에 의한 높이제한(정북방향)
        private double distanceByLighting = 0.25; //인접대지경계선(채광창), 완화
        private double distanceLW = 0.5; //채광창의 각 부분높이의 0.5배
        private double distanceLL = 0.8; //채광창과 채광창이 마주볼경우
        private double distanceWW = 4000; //측벽과 측벽

        //Method, 메소드

        public Curve RoadCenterLines(Plot plot)
        {
            //plot 의 boundary 와 roads 를 사용.
            var segments = plot.Boundary.DuplicateSegments();

            var roadwidths = plot.SimplifiedSurroundings.ToList();

            for (int i = 0; i < segments.Length; i++)
            {
               
                int j = i % roadwidths.Count;
                
                Curve temp = segments[i];
                var v = temp.TangentAtStart;
                v.Rotate(Math.PI / 2, Vector3d.ZAxis);
                if (roadwidths[j] == 21000)
                { }
                else
                    temp.Translate(v * roadwidths[j] / 2);
                segments[i] = temp;
            }

            List<Point3d> topoly = new List<Point3d>();
            for (int k = 0; k < segments.Length; k++)
            {
                int j = (k + 1) % segments.Length;
                Line li = new Line(segments[k].PointAtStart, segments[k].PointAtEnd);
                Line lj = new Line(segments[j].PointAtStart, segments[j].PointAtEnd);

                
                double paramA;
                double paramB;
                var intersect = Rhino.Geometry.Intersect.Intersection.LineLine(li, lj, out paramA, out paramB, 0, false);

                // 교점이 A 선 위에 있음
                bool isparamAonA = paramA >= 0 && paramA <= 1 ? true : false;
                // 교점이 B 선 위에 있음
                bool isparamBonB = paramB >= 0 && paramB <= 1 ? true : false;

                bool isRightSided = paramA > 1 && paramB > 1 ? true : false;
                bool isLeftSided = paramA < 0 && paramB < 0 ? true : false;
                // A 나 B 둘중 한 선의 위에.
                if (isparamAonA && !isparamBonB || !isparamAonA && isparamBonB)
                {
                    topoly.Add(li.PointAt(paramA));
                    //topoly.Add(segments[k].PointAtEnd);
                    //topoly.Add(segments[j].PointAtStart);
                    //k의 endpoint 에서 j의 startpoint로 선 긋는다.
                }
                // 두 선 위의 교점에.
                else if (isparamAonA && isparamBonB)
                {
                    //교점을 더함
                    topoly.Add(li.PointAt(paramA));
                }
                //외부에
                else
                {
                    //오른쪽 치우침
                    if (isRightSided)
                    {
                        topoly.Add(segments[k].PointAtEnd);
                        topoly.Add(segments[j].PointAtStart);
                    }
                    //왼쪽 치우침
                    else if (isLeftSided)
                    {
                        topoly.Add(segments[k].PointAtEnd);
                        topoly.Add(segments[j].PointAtStart);
                    }
                    //일반
                    else
                    topoly.Add(li.PointAt(paramA));
                }
            
            }

            topoly.Add(topoly[0]);

            var tempcurve = new PolylineCurve(topoly);
            var rotation = tempcurve.ClosedCurveOrientation(Vector3d.ZAxis);
            var selfintersection = Rhino.Geometry.Intersect.Intersection.CurveSelf(tempcurve, 0);

            var parameters = selfintersection.Select(n => n.ParameterA).ToList();

            parameters.AddRange(selfintersection.Select(n => n.ParameterB));

            var spl = tempcurve.Split(parameters);

            var f = CommonFunc.NewJoin(spl);


            var merged = f.Where(n => n.ClosedCurveOrientation(Vector3d.ZAxis) == rotation).ToList();
           


            return merged[0];

        }
        public Curve[] fromSurroundingsCurve(Plot plot)
        {
            //법규적용(대지안의 공지)

            Curve[] plotArr = plot.Boundary.DuplicateSegments();
            //extend plotArr
            Curve[] plotArrExtended = new Curve[plotArr.Length];
            Array.Copy(plotArr, plotArrExtended, plotArr.Length);

            for (int i = 0; i < plotArrExtended.Length; i++)
            {
                Curve tempCurve = plotArrExtended[i].Extend(CurveEnd.Both, 20000, CurveExtensionStyle.Line);

                plotArrExtended[i] = new LineCurve(tempCurve.PointAt(tempCurve.Domain.T0 - Math.Abs(tempCurve.Domain.T1 - tempCurve.Domain.T0)), tempCurve.PointAt(tempCurve.Domain.T1 + Math.Abs(tempCurve.Domain.T1 - tempCurve.Domain.T0)));
            }

            //reg start
            double[] distanceFromSurroundings = new double[plotArr.Length];

            for (int i = 0; i < plotArr.Length; i++)
            {
                if (plot.SimplifiedSurroundings[i] > 0)
                    distanceFromSurroundings[i] = distanceFromRoad;
                else if (plot.SimplifiedSurroundings[i] == 0)
                    distanceFromSurroundings[i] = DistanceFromPlot;
            }

            List<Point3d> ptsFromSurroundings = new List<Point3d>();

            for (int i = 0; i < plotArr.Length; i++)
            {
                int h = (i - 1 + plotArr.Length) % plotArr.Length;

                Curve curveH;
                Curve curveI;

                if (distanceFromSurroundings[h] != 0)
                {
                    curveH = plotArrExtended[h].Offset(Plane.WorldXY, distanceFromSurroundings[h], 0, CurveOffsetCornerStyle.None)[0];
                }
                else
                {
                    curveH = plotArrExtended[h];
                }

                if (distanceFromSurroundings[i] != 0)
                {
                    curveI = plotArrExtended[i].Offset(Plane.WorldXY, distanceFromSurroundings[i], 0, CurveOffsetCornerStyle.None)[0];
                }
                else
                {
                    curveI = plotArrExtended[i];
                }

                ///<summary>20160826 수정분 (접하는 2개 커브가 평행하고 하나는 대지와 하나는 도로와 면할때 Intersection이 생기지 않아 문제발생 >> 수정완료)</summary>
                {
                    var intersection = Rhino.Geometry.Intersect.Intersection.CurveCurve(curveH, curveI, 0, 0);

                    if (intersection.Count == 0)
                    {
                        Curve unextendedCurveH;
                        Curve unextendedCurveI;

                        if (distanceFromSurroundings[h] != 0)
                            unextendedCurveH = plotArr[h].Offset(Plane.WorldXY, distanceFromSurroundings[h], 0, CurveOffsetCornerStyle.None)[0];
                        else
                            unextendedCurveH = plotArr[h];

                        if (distanceFromSurroundings[i] != 0)
                            unextendedCurveI = plotArr[i].Offset(Plane.WorldXY, distanceFromSurroundings[i], 0, CurveOffsetCornerStyle.None)[0];
                        else
                            unextendedCurveI = plotArr[i];

                        ptsFromSurroundings.Add(unextendedCurveI.PointAtEnd);
                        ptsFromSurroundings.Add(unextendedCurveH.PointAtStart);
                    }
                    else
                    {
                        ptsFromSurroundings.Add(intersection[0].PointA);
                    }

                }
            }

            ptsFromSurroundings.Add(ptsFromSurroundings[0]);

            Curve fromSurroundingCurve = (new Polyline(ptsFromSurroundings)).ToNurbsCurve();

            return new Curve[] { fromSurroundingCurve };
        }
        public Curve[] fromNorthCurve(Plot plot)
        {
            //Curve roadCenter = RoadCenterLines(plot);
            
            double t = -1;

            //double LegalMaxF = high ? totalheight : height;
            if (plot.PlotType != PlotType.상업지역)
            {
                //move
            }

            else
            {
                t = 0;
            }

            double tempHeight = height;

            var tempRoadLine = plot.Boundary.DuplicateSegments();

            for (int i = 0; i < tempRoadLine.Length; i++)
            {
                if (tempRoadLine[i].TangentAtStart.X <= 0)
                    continue;

                double roadCenter = plot.Surroundings[i] / 2;
                double moveDistance = roadCenter * t;

                if (tempHeight < 9000)//숫자 -> 나중에 변수로
                    moveDistance += 1500;
                else
                    moveDistance += tempHeight / 2;

                if (moveDistance < 0)
                    moveDistance = 0;

                if (plot.Surroundings[i] == 21000)
                    moveDistance = 0;

               

                tempRoadLine[i].Translate(Vector3d.YAxis * t * moveDistance);
            }


            List<Point3d> wholePoints = new List<Point3d>();

            Point3d last = Point3d.Unset;
            for (int i = 0; i < tempRoadLine.Length; i++)
            {
                if (tempRoadLine[i].PointAtStart != last)
                {
                    last = tempRoadLine[i].PointAtStart;
                    wholePoints.Add(last);
                }
                if (tempRoadLine[i].PointAtEnd != last)
                {
                    last = tempRoadLine[i].PointAtEnd;
                    wholePoints.Add(last);
                }
            }

            Curve wholeCurve = new Polyline(wholePoints).ToNurbsCurve();

            List<TypeParam> tps = new List<TypeParam>();
            foreach (var p in wholeCurve.DuplicateSegments())
            {
                tps.Add(new TypeParam(p.Domain.Min, false));
            }
            tps.Add(new TypeParam(wholeCurve.Domain.Max, false));

            var self = Rhino.Geometry.Intersect.Intersection.CurveSelf(wholeCurve, 0);
            foreach (var e in self)
            {
                tps.Add(new TypeParam(e.ParameterA, true));
                tps.Add(new TypeParam(e.ParameterB, true));
            }

            tps = tps.OrderBy(n => n.Parameter).ToList();
            
            List<double> GroupA = new List<double>();
            List<double> GroupB = new List<double>();

            bool groupSwitch = true;
            foreach (var tp in tps)
            {
                if (tp.IsIntersectionParameter)
                {
                    GroupA.Add(tp.Parameter);
                    GroupB.Add(tp.Parameter);
                    groupSwitch = !groupSwitch;

                }
                else
                {
                    if (groupSwitch)
                        GroupA.Add(tp.Parameter);
                    else
                        GroupB.Add(tp.Parameter);
                }
            }

            if(GroupA.Count>0)
            GroupA.Add(GroupA[0]);
            if (GroupB.Count > 0)
            GroupB.Add(GroupB[0]);

            Curve resultA = new PolylineCurve(GroupA.Select(n => wholeCurve.PointAt(n)));
            Curve resultB = new PolylineCurve(GroupB.Select(n => wholeCurve.PointAt(n)));

            var checkSelfA = Rhino.Geometry.Intersect.Intersection.CurveSelf(resultA, 0);
            if(checkSelfA.Count == 0)
                return new Curve[] { resultA };

            var checkSelfB = Rhino.Geometry.Intersect.Intersection.CurveSelf(resultB, 0);
            if (checkSelfB.Count == 0)
                return new Curve[] { resultB };
            //tempRoadLine = GroupA.Count > GroupB.Count?new PolylineCurve(GroupA.Select(n=>tempRoadLine.PointAt(n))):new PolylineCurve(GroupB.Select(n => tempRoadLine.PointAt(n)));
            if (Curve.CreateBooleanDifference(resultA, plot.Boundary).Length == 0)
                return new Curve[] { resultA };

            if (Curve.CreateBooleanDifference(resultB, plot.Boundary).Length == 0)
                return new Curve[] { resultB };

            //var newCurve = Curve.CreateBooleanIntersection(tempRoadLine, plot.Boundary);

            return new Curve[] { };

            //for (int j = 0; j < newCurve.Length; j++)
            //    newCurve[j].Translate(Vector3d.ZAxis * tempHeight);

            //Rhino.RhinoDoc.ActiveDoc.Objects.Add(tempRoadLine);
            //return newCurve;
           

            //return copy.Select(n => n[0]).ToArray();
            //plot의 boundary, maxfloor, roadwidths, plottype 사용 , 도로중심선
        }
        private class TypeParam
        {
            public double Parameter = 0;
            public bool IsIntersectionParameter = false;

            public TypeParam(double parameter, bool isIntersectionParameter)
            {
                Parameter = parameter;
                IsIntersectionParameter = isIntersectionParameter;
            }
        }
        //not used
        public Curve[] fromNorthCurve2(Plot plot)
        {
            Curve roadCenter = RoadCenterLines(plot);
            Curve[] plotArr = roadCenter.DuplicateSegments();
            //상업 or 법규무시 체크시
            double newdistancefromnorth = DistanceFromNorth;
            if (plot.PlotType == PlotType.상업지역 || plot.ignoreNorth)
            { 
                newdistancefromnorth = 0;
            }

            double[] distanceFromNorth = new double[plotArr.Length];
            for (int i = 0; i < plotArr.Length; i++)
            {
                Vector3d tempVector = new Vector3d(plotArr[i].PointAt(plotArr[i].Domain.T1) - plotArr[i].PointAt(plotArr[i].Domain.T0));
                tempVector = tempVector / tempVector.Length;

                int h = (i + plotArr.Length - 1) % plotArr.Length;
                int j = (i + 1) % plotArr.Length;
                double moveDistance = 0;

                if (plot.SimplifiedSurroundings[j] >= 20000 || plot.SimplifiedSurroundings[i] >= 20000 || plot.SimplifiedSurroundings[h] >= 20000 || plot.SimplifiedSurroundings[i]==21000)
                { }
                else if (tempVector.X > 0 && plot.SimplifiedSurroundings[i] >= 0)
                {

                    double tempSineFactor = System.Math.Abs(tempVector.X);

                    double tempDistanceByRoad = (plot.SimplifiedSurroundings[i] / 2) * (tempVector.Length / System.Math.Abs(tempVector.X));
                    if (tempDistanceByRoad < newdistancefromnorth)
                        moveDistance = newdistancefromnorth - tempDistanceByRoad;
                }

                distanceFromNorth[i] = moveDistance;
            }

            List<Point3d> distanceFromNorthPts = new List<Point3d>();

            for (int i = 0; i < plotArr.Length; i++)
            {
                int h = (i - 1 + plotArr.Length) % plotArr.Length;
                int j = (i + 1 + plotArr.Length) % plotArr.Length;

                double distH = distanceFromNorth[h];
                double distI = distanceFromNorth[i];
                double distJ = distanceFromNorth[j];

                if (distI == 0)
                {
                    distanceFromNorthPts.Add(plotArr[i].PointAt(plotArr[i].Domain.T0));
                    distanceFromNorthPts.Add(plotArr[i].PointAt(plotArr[i].Domain.T1));
                }
                else
                {
                    Curve tempCurve = new LineCurve(plotArr[i].PointAt(plotArr[i].Domain.T0), plotArr[i].PointAt(plotArr[i].Domain.T1));
                    tempCurve.Transform(Transform.Translation(new Vector3d(0, -distI, 0)));

                    var tempIntersect1 = Rhino.Geometry.Intersect.Intersection.CurveCurve(tempCurve, plotArr[h], 0, 0);
                    var tempIntersect2 = Rhino.Geometry.Intersect.Intersection.CurveCurve(tempCurve, plotArr[j], 0, 0);

                    if (tempIntersect1.Count > 0)
                        distanceFromNorthPts.Add(tempIntersect1[0].PointA);
                    else if (tempIntersect1.Count <= 0)
                        distanceFromNorthPts.Add(tempCurve.PointAt(tempCurve.Domain.T0));

                    if (tempIntersect2.Count > 0)
                        distanceFromNorthPts.Add(tempIntersect2[0].PointA);
                    else if (tempIntersect2.Count <= 0)
                        distanceFromNorthPts.Add(tempCurve.PointAt(tempCurve.Domain.T1));
                }
            }

            distanceFromNorthPts.Add(distanceFromNorthPts[0]);

            Curve fromNorthCurve = (new Polyline(distanceFromNorthPts)).ToNurbsCurve();
            return new Curve[] { fromNorthCurve };
        }
       
        //채광방향 사선 규칙입니다.
        public Curve[] byLightingCurve(Plot plot, double angle)
        {
        
            //특례 적용 안하는경우 h * 0.5 , 8 층 이상인 경우
            if (!plot.isSpecialCase || totalheight >= 7)
                distanceByLighting = 0.5;
            //특례 적용 하는 경우 h * 0.25
            else 
                distanceByLighting = 0.25;

            //법규적용 인접대지경계선(채광창)
            Curve roadCenter = RoadCenterLines(plot);
            Curve[] plotArr = plot.Boundary.DuplicateSegments(); //roadCenter.DuplicateSegments();

            //대지 경계선을 구성 선분들로 분할합니다.
            Curve x = plot.Boundary;
            var rotation = x.ClosedCurveOrientation(Vector3d.ZAxis);
            Curve[] plotArrExtended = new Curve[plotArr.Length];
            Array.Copy(plotArr, plotArrExtended, plotArr.Length);

            double length = x.GetBoundingBox(false).Diagonal.Length;
            var cp = AreaMassProperties.Compute(x).Centroid;
            Vector3d dir = new Vector3d(Math.Cos(-angle), Math.Sin(-angle), 0);
            Curve baseline = new LineCurve(cp - dir * length, cp + dir * length);
            //아파트 라인 기준 각도로 기준 선을 뽑습니다 (- angle(Radian) 만큼 회전)

            double distance = DistanceByLighting;
            //처음에 설정한 계수에 높이(전체 건물 높이 - 필로티 높이)를 곱한 값


            //분할해둔 구성 선분들마다 , 이격거리 - 도로 너비/2 만큼씩 기준선 에 수직인 기준 벡터 방향으로 움직여줍니다.
            for (int i = 0; i < plotArrExtended.Length; i++)
            {
                int k = 1;
                double roadWidth = plot.Surroundings[i] / 2;
                if (roadWidth == 21000)
                    k = 0;
                double eachDistance = (distance - roadWidth)*k;
                if (eachDistance < 0)
                    eachDistance = 0;

                //기준 벡터 구하기
                //각 구성선분의 양 끝 점에서 기준선으로부터 최단거리의 점을 구하고, 방향 벡터를 구합니다.
                //double roadcenterdistance = plot.SimplifiedSurroundings[i] / 2;
                double ps;
                baseline.ClosestPoint(plotArrExtended[i].PointAtStart, out ps);
                double pe;
                baseline.ClosestPoint(plotArrExtended[i].PointAtEnd, out pe);

                Point3d cs = baseline.PointAt(ps);
                Point3d ce = baseline.PointAt(pe);
                Vector3d vs = cs - plotArrExtended[i].PointAtStart;
                Vector3d ve = ce - plotArrExtended[i].PointAtEnd;

                vs.Unitize();
                ve.Unitize();

                //선분의 중점에서 방향 벡터만큼 이동한 테스트 포인트 쌍을 생성합니다.

                var vstest = plotArrExtended[i].PointAtNormalizedLength(0.5) + vs;
                var vetest = plotArrExtended[i].PointAtNormalizedLength(0.5) + ve;

                Curve pre = plotArrExtended[i].DuplicateCurve();
                //특정 테스트 포인트가 도형 내부에 존재 한다면, 해당 방향으로 선을 움직입니다.
                if (x.Contains(vstest) == PointContainment.Inside)
                {
                    //v = vs
                    pre.Transform(Transform.Translation(vs * eachDistance));
                    plotArrExtended[i] = pre;
                }
                else if (x.Contains(vetest) == PointContainment.Inside)
                {
                    //v = ve
                    pre.Transform(Transform.Translation(ve * eachDistance));
                    plotArrExtended[i] = pre;
                }
                //내부의 점이 하나도 찍히지 않는 선..  기준 선보다 위쪽 or 아래쪽에 쏠려있는 선에 대해서.
                //어떤 예외가 또 있을지 잘 모르겠다.
                else
                {
                    pre.Transform(Transform.Translation(-vs * eachDistance));
                    plotArrExtended[i] = pre;
                }

            }

            //움직인 선분들로 새로 닫힌 꺾인선을 생성합니다. 

            List<Point3d> topoly = new List<Point3d>();
            for (int i = 0; i < plotArrExtended.Length; i++)
            {
                int j = (i + 1) % plotArrExtended.Length;
                Line li = new Line(plotArrExtended[i].PointAtStart, plotArrExtended[i].PointAtEnd);
                Line lj = new Line(plotArrExtended[j].PointAtStart, plotArrExtended[j].PointAtEnd);
                double paramA;
                double paramB;
                var intersect = Rhino.Geometry.Intersect.Intersection.LineLine(li, lj, out paramA, out paramB, 0,false);

                topoly.Add(li.PointAt(paramA)); 
            }

            topoly.Add(topoly[0]);

            Curve wholeCurve = new PolylineCurve(topoly);

            List<TypeParam> tps = new List<TypeParam>();
            foreach (var p in wholeCurve.DuplicateSegments())
            {
                tps.Add(new TypeParam(p.Domain.Min, false));
            }
            tps.Add(new TypeParam(wholeCurve.Domain.Max, false));

            var self = Rhino.Geometry.Intersect.Intersection.CurveSelf(wholeCurve, 0);
            foreach (var e in self)
            {
                tps.Add(new TypeParam(e.ParameterA, true));
                tps.Add(new TypeParam(e.ParameterB, true));
            }

            tps = tps.OrderBy(n => n.Parameter).ToList();

            List<double> GroupA = new List<double>();
            List<double> GroupB = new List<double>();

            bool groupSwitch = true;
            foreach (var tp in tps)
            {
                if (tp.IsIntersectionParameter)
                {
                    GroupA.Add(tp.Parameter);
                    GroupB.Add(tp.Parameter);
                    groupSwitch = !groupSwitch;

                }
                else
                {
                    if (groupSwitch)
                        GroupA.Add(tp.Parameter);
                    else
                        GroupB.Add(tp.Parameter);
                }
            }

            if (GroupA.Count > 0)
                GroupA.Add(GroupA[0]);
            if (GroupB.Count > 0)
                GroupB.Add(GroupB[0]);



            Curve resultA = new PolylineCurve(GroupA.Select(n => wholeCurve.PointAt(n)));
            Curve resultB = new PolylineCurve(GroupB.Select(n => wholeCurve.PointAt(n)));

            Curve test = new PolylineCurve();
            //Rhino.RhinoDoc.ActiveDoc.Objects.Add(resultA);
            //Rhino.RhinoDoc.ActiveDoc.Objects.Add(resultB);
            var checkSelfA = Rhino.Geometry.Intersect.Intersection.CurveSelf(resultA, 0);
            if (GroupA.Count>0)
                return new Curve[] { resultA };

            var checkSelfB = Rhino.Geometry.Intersect.Intersection.CurveSelf(resultB, 0);
            if (GroupB.Count > 0)
                return new Curve[] { resultB };
            //tempRoadLine = GroupA.Count > GroupB.Count?new PolylineCurve(GroupA.Select(n=>tempRoadLine.PointAt(n))):new PolylineCurve(GroupB.Select(n => tempRoadLine.PointAt(n)));
            if (Curve.CreateBooleanDifference(resultA, plot.Boundary).Length == 0)
                return new Curve[] { resultA };

            if (Curve.CreateBooleanDifference(resultB, plot.Boundary).Length == 0)
                return new Curve[] { resultB };

            #region dfasdf

            return new Curve[] { };

            //var selfintersection = Rhino.Geometry.Intersect.Intersection.CurveSelf(tempcurve, 0);
            //var parameters = selfintersection.Select(n => n.ParameterA).ToList();
            //var spl = tempcurve.Split(parameters);
            //foreach (var s in spl)
            //    s.MakeClosed(5);
            //var sameOrientations = spl.Where(n => n.ClosedCurveOrientation(Vector3d.ZAxis) == rotation).ToArray();

            //List<Curve> result = new List<Curve>();
            //for (int i = 0; i < sameOrientations.Length; i++)
            //{
            //    var newCurve = Curve.CreateBooleanIntersection(sameOrientations[i], plot.Boundary);
            //    result.AddRange(newCurve);
            //}
            #endregion
            //if (result.Count == 0)
            //{
            //    //?!
            //}

            //if (result.Count != 1)
            //{
            //    var areas = result.Select(n => AreaMassProperties.Compute(n).Area);
            //}
            //return result.ToArray();
        }

     
        public Curve[] JoinRegulations(Plot plot, double angle)
        {
            var k = CommonFunc.JoinRegulations(fromSurroundingsCurve(plot), fromNorthCurve(plot), byLightingCurve(plot, angle));
            return k;
        }

        //Property, 속성

        public double DistanceFromRoad { get { return distanceFromRoad; } }
        public double DistanceFromPlot { get { return distanceFromPlot; } }
        public double DistanceFromNorth { get { return distanceFromNorth * (height-fakeHeight); } }
        public double DistanceByLighting { get { return distanceByLighting * ((height-fakeHeight) - Consts.PilotiHeight); } }
        public double DistanceLW { get { return distanceLW * ((height - fakeHeight) - Consts.PilotiHeight); ; } }
        public double DistanceLL { get { return distanceLL *  ((height-fakeHeight) - Consts.PilotiHeight);; } }
        public double DistanceWW { get { return distanceWW; } }
        public double Lightingk { get { return distanceByLighting; } }
        public BuildingType BuildingType { get; private set; }
    }

    public class ParkingLine
    {
        //Constrcutor, 생성자

        public ParkingLine(Rectangle3d boundary)
        {
            this.Boundary = boundary;
        }
        public ParkingLine(Curve boundary)
        {
            Plane boundaryPlane = new Plane(boundary.PointAtStart, boundary.TangentAtStart, -boundary.TangentAtEnd);
            Point3d[] points = boundary.DuplicateSegments().Select(n => n.PointAtStart).ToArray();
            this.Boundary = new Rectangle3d(boundaryPlane, points[0], points[2]);
        }
        //method, 메소드

        public double GetArea()
        {
            return Boundary.Area;
        }

        //프로퍼티

        public Rectangle3d Boundary { get; private set; }
    }

    public class ParkingLotOnEarth
    {
        //Constrcutor, 생성자

        public ParkingLotOnEarth()
        {
            this.ParkingLines = new List<List<ParkingLine>>();
        }

        public ParkingLotOnEarth(List<List<ParkingLine>> parkingLines)
        {
            this.ParkingLines = parkingLines;
        }

        //method, 메소드

        public int GetCount()
        {
            int Count = 0;

            foreach (List<ParkingLine> i in this.ParkingLines)
                Count += i.Count();

            return Count;
        }

        //프로퍼티

        public List<List<ParkingLine>> ParkingLines { get; set; }
    }

    public class ParkingLotUnderGround
    {
        //Constrcutor, 생성자

        public ParkingLotUnderGround()
        {
            this.Count = 0;
            this.ParkingArea = 0;
            this.Floors = 0;
        }

        public ParkingLotUnderGround(int count, double area, int floors)
        {
            this.Count = count;
            this.ParkingArea = area;
            this.Floors = floors;
        }

        //프로퍼티
        public Curve Ramp { get; set; }
        public int Count { get; private set; }
        public double ParkingArea { get; private set; }
        public int Floors { get; private set; }
    }


    // 평면 라이브러리

    public class FloorPlanLibrary
    {
        public FloorPlanLibrary(string filePath)
        {
            FileStream fs;

            try
            {
                fs = new FileStream(filePath, FileMode.Open);
                
            }

            catch (IOException)
            {
                Console.WriteLine("파일을 열 수 없습니다.");
                return;
            }

            StreamReader r = new StreamReader(fs);

            string s;
            int l = 1;

            List<string> tempLibrary = new List<string>();

            while ((s = r.ReadLine()) != null)
            {
                tempLibrary.Add(s);
                l++;
            }

            List<double> dimensions = dimensionDecoder(tempLibrary[0]);
            double sampleXLengthA = dimensions[0];
            double sampleXLengthB = dimensions[1];
            double sampleYLengthA = dimensions[2];
            double sampleYLengthB = dimensions[3];
            List<Polyline> roomList = polylineDecoder(tempLibrary[1]);
            List<Polyline> bathroomList = polylineDecoder(tempLibrary[2]);
            List<string> tagNameList = stringDecoder(tempLibrary[3]);
            List<Point3d> tagPointList = pointDecoder(tempLibrary[4]);
            List<Point3d> doorCenterList = pointDecoder(tempLibrary[5]);
            List<Line> windowLineList = lineDecoder(tempLibrary[6]);
            int isCorner = Convert.ToInt32(tempLibrary[7]);
            int isP4 = Convert.ToInt32(tempLibrary[8]);

            r.Close();

            this.xLengthA = sampleXLengthA;
            this.xLengthB = sampleXLengthB;
            this.yLengthA = sampleYLengthA;
            this.yLengthB = sampleYLengthB;
            this.roomList = roomList;
            this.bathroomList = bathroomList;
            this.tagNameList = tagNameList;
            this.tagPointList = tagPointList;
            this.doorCenterList = doorCenterList;
            this.windowLineList = windowLineList;
            this.isCorner = isCorner;
            this.isP4 = isP4;
        }

        public double xLengthA { get; private set; }
        public double xLengthB { get; private set; }
        public double yLengthA { get; private set; }
        public double yLengthB { get; private set; }
        public List<Polyline> roomList { get; private set; }
        public List<Polyline> bathroomList { get; private set; }
        public List<string> tagNameList { get; private set; }
        public List<Point3d> tagPointList { get; private set; }
        public List<Point3d> doorCenterList { get; private set; }
        public List<Line> windowLineList { get; private set; }
        public int isCorner { get; private set; }
        public int isP4 { get; private set; }

        // Functions

        static List<double> dimensionDecoder(string Code)
        {
            string[] stringSeparators1 = new string[] { "[" };
            string[] stringSeparators2 = new string[] { "]" };
            string[] stringSeparators4 = new string[] { ", " };

            string[] pureText;

            Code = Code.Split(stringSeparators1, StringSplitOptions.RemoveEmptyEntries)[0];
            Code = Code.Split(stringSeparators2, StringSplitOptions.RemoveEmptyEntries)[0];
            pureText = Code.Split(stringSeparators4, StringSplitOptions.RemoveEmptyEntries);

            List<double> dimensions = new List<double>();

            for (int i = 0; i < pureText.Length; i++)
            {
                dimensions.Add(Convert.ToDouble(pureText[i]));
            }

            return dimensions;
        }

        static List<string> stringDecoder(string Code)
        {
            string[] stringSeparators1 = new string[] { "[" };
            string[] stringSeparators2 = new string[] { "]" };
            string[] stringSeparators4 = new string[] { ", " };

            string[] pureText;

            Code = Code.Split(stringSeparators1, StringSplitOptions.RemoveEmptyEntries)[0];
            Code = Code.Split(stringSeparators2, StringSplitOptions.RemoveEmptyEntries)[0];
            pureText = Code.Split(stringSeparators4, StringSplitOptions.RemoveEmptyEntries);

            List<string> strings = new List<string>();

            for (int i = 0; i < pureText.Length; i++)
            {
                strings.Add(pureText[i]);
            }

            return strings;
        }

        static List<Polyline> polylineDecoder(string Code)
        {
            if (Code == "[]")
                return new List<Polyline>();

            string[] stringSeparators1 = new string[] { "[{" };
            string[] stringSeparators2 = new string[] { "}]" };
            string[] stringSeparators4 = new string[] { ", " };
            string[] stringSeparators5 = new string[] { "}{" };

            string[] pureText1;

            pureText1 = Code.Split(stringSeparators1, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < pureText1.Length; i++)
            {
                pureText1[i] = pureText1[i].Split(stringSeparators2, StringSplitOptions.RemoveEmptyEntries)[0];
            }

            List<Polyline> polylines = new List<Polyline>();

            for (int i = 0; i < pureText1.Length; i++)
            {
                string[] pureText2;
                pureText2 = pureText1[i].Split(stringSeparators5, StringSplitOptions.RemoveEmptyEntries);

                List<Point3d> points = new List<Point3d>();

                for (int j = 0; j < pureText2.Length; j++)
                {
                    string[] pureText3;
                    pureText3 = pureText2[j].Split(stringSeparators4, StringSplitOptions.RemoveEmptyEntries);
                    points.Add(new Point3d(Convert.ToDouble(pureText3[0]), Convert.ToDouble(pureText3[1]), 0));
                }

                polylines.Add(new Polyline(points));
            }

            return polylines;
        }

        static List<Line> lineDecoder(string Code)
        {
            string[] stringSeparators1 = new string[] { "[{" };
            string[] stringSeparators2 = new string[] { "}]" };
            string[] stringSeparators4 = new string[] { ", " };
            string[] stringSeparators5 = new string[] { "}{" };

            string[] pureText1;

            pureText1 = Code.Split(stringSeparators1, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < pureText1.Length; i++)
            {
                pureText1[i] = pureText1[i].Split(stringSeparators2, StringSplitOptions.RemoveEmptyEntries)[0];
            }

            List<Line> lines = new List<Line>();

            for (int i = 0; i < pureText1.Length; i++)
            {
                string[] pureText2;
                pureText2 = pureText1[i].Split(stringSeparators5, StringSplitOptions.RemoveEmptyEntries);

                List<Point3d> points = new List<Point3d>();

                for (int j = 0; j < pureText2.Length; j++)
                {
                    string[] pureText3;
                    pureText3 = pureText2[j].Split(stringSeparators4, StringSplitOptions.RemoveEmptyEntries);
                    points.Add(new Point3d(Convert.ToDouble(pureText3[0]), Convert.ToDouble(pureText3[1]), 0));
                }

                lines.Add(new Line(points[0], points[1]));
            }

            return lines;
        }

        static List<Point3d> pointDecoder(string Code)
        {
            string[] stringSeparators1 = new string[] { "[{" };
            string[] stringSeparators2 = new string[] { "}]" };
            string[] stringSeparators4 = new string[] { ", " };
            string[] stringSeparators5 = new string[] { "}{" };

            string[] pureText;

            Code = Code.Split(stringSeparators1, StringSplitOptions.RemoveEmptyEntries)[0];
            Code = Code.Split(stringSeparators2, StringSplitOptions.RemoveEmptyEntries)[0];
            pureText = Code.Split(stringSeparators5, StringSplitOptions.RemoveEmptyEntries);

            List<Point3d> output = new List<Point3d>();

            for (int i = 0; i < pureText.Length; i++)
            {
                string[] tempString = pureText[i].Split(stringSeparators4, StringSplitOptions.RemoveEmptyEntries);
                Point3d tempPt1 = new Point3d(Convert.ToDouble(tempString[0]), Convert.ToDouble(tempString[1]), 0);
                output.Add(tempPt1);
            }

            return output;
        }
    }

    // 미리보기 기능 구현

    public class textConduit : Rhino.Display.DisplayConduit
    {
        public textConduit()
        {

        }

        public textConduit(System.Drawing.Color displayColor)
        {
            this.displayColor = displayColor;
        }

        protected override void CalculateBoundingBox(CalculateBoundingBoxEventArgs e)
        {
            base.CalculateBoundingBox(e);
            {
                List<Point3d> pointCloud = new List<Point3d>();

                foreach (Text3d i in TextToDisplay)
                {
                    pointCloud.Add(i.TextPlane.Origin);
                }

                e.BoundingBox.Union(new BoundingBox(pointCloud));

                Rhino.RhinoApp.Wait();
            }
        }

        protected override void PreDrawObjects(DrawEventArgs e)
        {
            base.PreDrawObjects(e);

            foreach (Text3d i in TextToDisplay)
            {
               
                e.Display.Draw3dText(i, displayColor);
            }
        }

        public List<Text3d> TextToDisplay { private get; set; }
        private System.Drawing.Color displayColor = System.Drawing.Color.Red;
    }



    public class CurveConduit : Rhino.Display.DisplayConduit
    {
        public string dimension = "";
        public Point3d dimPoint;
        public List<Point3d> debugPoints = new List<Point3d>();
        public List<Point3d> HouseholdOrigins = new List<Point3d>();
        public CurveConduit()
        {
            this.CurveToDisplay = new List<Curve>();
        }

        public CurveConduit(System.Drawing.Color displayColor)
        {
            this.displayColor = displayColor;
            CurveToDisplay = new List<Curve>();
        }

        protected override void CalculateBoundingBox(CalculateBoundingBoxEventArgs e)
        {
            base.CalculateBoundingBox(e);
            {
                if (CurveToDisplay == null)
                    return;
                foreach (Curve i in CurveToDisplay)
                {
                    e.BoundingBox.Union(i.GetBoundingBox(false));
                }
                Rhino.RhinoApp.Wait();
            }
        }

        protected override void PreDrawObjects(DrawEventArgs e)
        {
            base.PreDrawObjects(e);
            if (CurveToDisplay == null)
                return;

            foreach (Curve i in CurveToDisplay)
            {
                e.Display.DrawCurve(i, displayColor);
                if (dimension != "")
                    e.Display.DrawDot(i.PointAtNormalizedLength(0.5), dimension);
            }

            for (int i = 0; i < debugPoints.Count;i++)
            {
                //if (i < 2)//points end
                    e.Display.DrawPoint(debugPoints[i], PointStyle.X, 10, System.Drawing.Color.Green);
                //else if(i<4)//points start
                //    e.Display.DrawPoint(debugPoints[i], PointStyle.X, 10, System.Drawing.Color.Gold);
                //else
                //    e.Display.DrawPoint(debugPoints[i], PointStyle.X, 10, System.Drawing.Color.Azure);

            }

            for (int i = 0; i < HouseholdOrigins.Count; i++)
            {
                e.Display.DrawPoint(HouseholdOrigins[i], PointStyle.X, 10, System.Drawing.Color.Gold);
            }
        }

        public List<Curve> CurveToDisplay { protected get; set; }
        protected System.Drawing.Color displayColor = System.Drawing.Color.Red;
    }

    public class BrepConduit : Rhino.Display.DisplayConduit
    {
        DisplayMaterial tempMaterial = new DisplayMaterial(System.Drawing.Color.LightGray, 0);

        protected override void CalculateBoundingBox(CalculateBoundingBoxEventArgs e)
        {
            base.CalculateBoundingBox(e);

            foreach (Brep i in BrepToDisplay)
            {
                e.BoundingBox.Union(i.GetBoundingBox(true));
            }

            Rhino.RhinoApp.Wait();

        }

        protected override void PreDrawObjects(DrawEventArgs e)
        {
            base.PreDrawObjects(e);

            List<Surface> surfaces = new List<Surface>();

            foreach (Brep i in BrepToDisplay)
            {
                List<Surface> tempSurfaces = i.Surfaces.ToList();

                surfaces.AddRange(tempSurfaces);
            }

            foreach (Surface i in surfaces)
            {
                Mesh[] tempMesh = Mesh.CreateFromBrep(i.ToBrep());

                foreach (Mesh j in tempMesh)
                {
                    e.Display.DrawMeshShaded(j, tempMaterial);
                    e.Display.DrawMeshWires(j, System.Drawing.Color.Black);
                }
            }
        }

        public List<Brep> BrepToDisplay { private get; set; }
    }
}