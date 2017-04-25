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
    }

    /// Enumerations

    public enum PlotType { 제1종일반주거지역, 제2종일반주거지역 };

    public enum BuildingType { Apartment, RowHouses };

    // AG를 만들기 위해 필요한 재료들

    public class Plot
    {
        //Constructor, 생성자

        public Plot(Plot other)
        {
            boundary = other.boundary;
            surroundings = other.surroundings;
            SimplifiedBoundary = other.SimplifiedBoundary;
            SimplifiedSurroundings = other.SimplifiedSurroundings;
        }

        public Plot(Curve boundary)
        {
            this.boundary = boundary;

            Polyline tempPolyline;
            boundary.TryGetPolyline(out tempPolyline);

            int[] surroundings = new int[tempPolyline.GetSegments().Length];

            for (int i = 0; i < surroundings.Length; i++)
            {
                surroundings[i] = 4000;
            }

            this.surroundings = surroundings;

            Polyline simplifiedPolyline = fishInvasion(tempPolyline);

            this.SimplifiedBoundary = simplifiedPolyline.ToNurbsCurve();

            this.SimplifiedSurroundings = RemapRoadWidth(tempPolyline, surroundings.ToList(), simplifiedPolyline).ToArray();
        }

        public Plot(Curve boundary, int[] surroundings)
        {
            this.boundary = boundary;
            this.surroundings = surroundings;

            Polyline tempPolyline;
            boundary.TryGetPolyline(out tempPolyline);

            Polyline simplifiedPolyline = fishInvasion(tempPolyline);

            this.SimplifiedBoundary = simplifiedPolyline.ToNurbsCurve();

            this.SimplifiedSurroundings = RemapRoadWidth(tempPolyline, surroundings.ToList(), simplifiedPolyline).ToArray();
        }

        //Field, 필드

        private PlotType plotType = PlotType.제1종일반주거지역;
        private Curve boundary;
        private int[] surroundings;

        //Method, 메소드

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
                counter1 ++;
            }

            currentPline = new Polyline();
            int counter2 = 0;

            while (currentPline.Length != tempCalculatedPline.Length && counter2 < pLine.GetSegments().Count() / 2)
            {
                currentPline = tempCalculatedPline;
                tempCalculatedPline = fish2(currentPline);

                counter2 ++;
            }

            currentPline = new Polyline();
            int counter3 = 0;

            while (currentPline.Length != tempCalculatedPline.Length && counter3 < pLine.GetSegments().Count() / 2)
            {
                currentPline = tempCalculatedPline;
                tempCalculatedPline = fish4(currentPline);
                counter3 ++;
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

        public PlotType PlotType { get { return plotType; } set { plotType = value; } }
        public Curve Boundary { get { return boundary; } }
        public Curve SimplifiedBoundary { get; private set; }
        public int[] Surroundings
        {
            get { return surroundings; }
            set { surroundings = value; }
        }
        public int[] SimplifiedSurroundings { get; private set; }
    }

    public class ParameterSet
    {
        //Constructor, 생성자

        public ParameterSet(double[] parameters, string agName, CoreType coreType)
        {
            this.agName = agName;
            this.thisParameters = parameters;
            this.height = Math.Max(parameters[0], parameters[1]);
            this.coreType = coreType;
        }

        public ParameterSet(double[] parameters, string agName, int heightIndex, CoreType coreType)
        {
            this.agName = agName;
            this.thisParameters = parameters;
            this.height = parameters[heightIndex];
            this.coreType = coreType;
        }

        //Field, 필드

        double[] thisParameters;
        double height;
        CoreType coreType;

        //Method, 메소드

        //Property, 속성

        public string agName { get; private set; }
        public double[] Parameters { get { return thisParameters; } }
        public double Stories { get { return height; } }
        public CoreType CoreType { get { return coreType; } }

    }

    public class Target
    {
        //ConstructTor, 생성자

        public Target()
        {
            double[] tempTargetArea = { 55, 85 };
            double[] tempTargetRatio = { 1, 1 };

            this.targetArea = tempTargetArea.ToList();
            this.targetRatio = tempTargetRatio.ToList();
        }

        public Target(List<double> targetArea)
        {
            this.targetArea = targetArea;
            List<double> tempTargetRatio = new List<double>();

            for (int i = 0; i < targetArea.Count; i++)
            {
                tempTargetRatio.Add(1);
            }

            this.targetRatio = tempTargetRatio;
        }

        public Target(List<double> targetArea, List<double> targetRatio)
        {
            this.targetArea = targetArea;
            this.targetRatio = targetRatio;
        }

        //Field, 필드

        private List<double> targetArea = new List<double>();
        private List<double> targetRatio = new List<double>();

        //Method, 메소드

        //Property, 속성

        public List<double> TargetArea { get { return targetArea; } }
        public List<double> TargetRatio { get { return targetRatio; } }
    }

    // ApartmentGenerator의 부모 클래스와 출력값 클래스

    public abstract class ApartmentmentGeneratorBase
    {
        public abstract ApartmentGeneratorOutput generator(Plot plot, ParameterSet parameterSet, Target target);
        public abstract double[] MinInput { get; set; }
        public abstract double[] MaxInput { get; set; }
        public abstract CoreType GetRandomCoreType();
        public abstract double[] GAParameterSet { get; }
        public abstract bool IsCoreProtrude { get; }
    }

    public class ApartmentGeneratorOutput : IDisposable
    {
        //Constructor, 생성자
        public ApartmentGeneratorOutput(string AGType, Plot plot, BuildingType buildingType, ParameterSet parameterSet, Target target, List<List<CoreProperties>> coreProperties, List<List<List<HouseholdProperties>>> householdProperties, ParkingLotOnEarth parkingOnEarth, ParkingLotUnderGround parkingUnderGround, List<List<Curve>> buildingOutline, List<Curve> aptLines)
        {
            this.AGtype = AGType;
            this.Plot = plot;
            this.BuildingType = buildingType;
            this.ParameterSet = parameterSet;
            this.Target = target;
            this.CoreProperties = coreProperties;
            this.HouseholdProperties = householdProperties;
            this.HouseholdStatistics = getHouseholdStatistics(householdProperties);
            this.ParkingLotOnEarth = parkingOnEarth;
            this.ParkingLotUnderGround = parkingUnderGround;
            this.Green = Green;
            this.buildingOutline = buildingOutline;
            this.AptLines = aptLines;

            this.Commercial = new List<HouseholdProperties>();
            this.PublicFacility = new List<HouseholdProperties>();

        }

        public ApartmentGeneratorOutput(string AGType, Plot plot, BuildingType buildingType, ParameterSet parameterSet, Target target, List<List<CoreProperties>> coreProperties, List<List<List<HouseholdProperties>>> householdProperties, List<HouseholdProperties> commercial, List<HouseholdProperties> publicFacility, ParkingLotOnEarth parkingOnEarth, ParkingLotUnderGround parkingUnderGround, List<Curve> Green, List<List<Curve>> buildingOutline, List<Curve> aptLines)
        {
            ///////20160516_추가된 생성자, 조경면적, 법정조경면적, 공용공간, 근생 추가

            this.AGtype = AGType;
            this.Plot = plot;
            this.BuildingType = buildingType;
            this.ParameterSet = parameterSet;
            this.Target = target;
            this.CoreProperties = coreProperties;
            this.HouseholdProperties = householdProperties;
            this.HouseholdStatistics = getHouseholdStatistics(householdProperties);
            this.Commercial = commercial;
            this.PublicFacility = publicFacility;
            this.ParkingLotOnEarth = parkingOnEarth;
            this.ParkingLotUnderGround = parkingUnderGround;
            this.Green = Green;
            this.buildingOutline = buildingOutline;
            this.AptLines = aptLines;
        }

        public ApartmentGeneratorOutput()
        {
            this.IsNull = true;
        }

        //Property, 속성


        public bool IsNull { get; private set; }
        public string AGtype { get; private set; }
        public Plot Plot { get; private set; }
        public BuildingType BuildingType { get; private set; }
        public ParameterSet ParameterSet { get; private set; }
        public ParkingLotOnEarth ParkingLotOnEarth { get; private set; }
        public ParkingLotUnderGround ParkingLotUnderGround { get; private set; }
        public Target Target { get; private set; }
        public List<List<CoreProperties>> CoreProperties { get; private set; }
        public List<List<List<HouseholdProperties>>> HouseholdProperties { get; private set; }
        public List<HouseholdStatistics> HouseholdStatistics { get; private set; }
        public List<HouseholdProperties> Commercial { get; private set; }
        public List<HouseholdProperties> PublicFacility { get; private set; }
        public List<Curve> Green { get; private set; }
        public List<List<Curve>> buildingOutline { get; private set; }
        public List<Curve> AptLines { get; private set; }

        //method, 메소드

        ////////////////////////////////////////////
        //////////  household statistics  //////////
        ////////////////////////////////////////////

        private List<HouseholdStatistics> getHouseholdStatistics(List<List<List<HouseholdProperties>>> hhp)
        {
            List<HouseholdProperties> all = new List<HouseholdProperties>();
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

            List<HouseholdProperties> hhpDistinct = new List<HouseholdProperties>();
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

        private bool isHHPequal(HouseholdProperties a, HouseholdProperties b)
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

        private bool isHHPsimilar(HouseholdProperties a, HouseholdProperties b)
        {
            double dXa = Math.Abs(a.XLengthA - b.XLengthA);
            double dXb = Math.Abs(a.XLengthB - b.XLengthB);
            double dYa = Math.Abs(a.YLengthA - b.YLengthA);
            double dYb = Math.Abs(a.YLengthB - b.YLengthB);

            if (dXa < 100 && dXb < 100 && dYa < 100 && dYb < 100 && a.WallFactor.SequenceEqual(b.WallFactor))
                return true;
            else
                return false;
        }

        //////////////////////////////////////

        public List<string> AreaTypeString()
        {
            string[] alphabetArray = { string.Empty, "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z" };

            List<int> areaType = new List<int>();
            List<string> output = new List<string>();

            for (int i = 0; i < HouseholdStatistics.Count; i++)
            {
                areaType.Add((int)(Math.Round(HouseholdStatistics[i].GetExclusiveArea() / 100000, 0)));
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
                    output.Add(alphabetArray[tempSameAreaCheck[tempIndex]] + "타입");
                }
                else
                {
                    tempAreaCheckList.Add(tempAreaType);
                    tempSameAreaCheck.Add(1);
                    output.Add(alphabetArray[1] + "타입");
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
                foreach (HouseholdProperties i in this.Commercial)
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
                foreach (HouseholdProperties i in PublicFacility)
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

        public List<Curve> drawEachCore()
        {

            List<Curve> coreOutlines = new List<Curve>();

            for (int i = 0; i < this.CoreProperties.Count; i++)
            {
                for (int j = 0; j < this.CoreProperties[i].Count; j++)
                {
                    List<Point3d> outlinePoints = new List<Point3d>();


                    Point3d pt = new Point3d(this.CoreProperties[i][j].Origin);
                    Vector3d x = new Vector3d(this.CoreProperties[i][j].XDirection);
                    Vector3d y = new Vector3d(this.CoreProperties[i][j].YDirection);
                    double width = this.CoreProperties[i][j].CoreType.GetWidth();
                    double depth = this.CoreProperties[i][j].CoreType.GetDepth();

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
                    coreOutlines.Add(outlineCurve);
                }
            }
            return coreOutlines;
        }

        public List<Curve> drawEachHouse()
        {
            List<Curve> houseOutlines = new List<Curve>();
            for (int i = 0; i < this.HouseholdProperties.Count; i++)
            {
                for (int j = 0; j < this.HouseholdProperties[i].Count; j++)
                {
                    for (int k = 0; k < this.HouseholdProperties[i][j].Count; k++)
                    {
                        houseOutlines.Add(this.HouseholdProperties[i][j][k].GetOutline());
                    }

                }
            }
            return houseOutlines;
        }

        public List<Curve> drawPublicFacilities()
        {
            List<Curve> output = new List<Curve>();

            foreach (HouseholdProperties i in this.PublicFacility)
            {
                List<Point3d> outlinePoints = new List<Point3d>();
                Point3d pt = new Point3d(i.Origin);
                Vector3d x = new Vector3d(i.XDirection);
                Vector3d y = new Vector3d(i.YDirection);
                double xa = i.XLengthA;
                double xb = i.XLengthB;
                double ya = i.YLengthA;
                double yb = i.YLengthB;

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

                output.Add(outlineCurve);
            }

            return output;
        }

        public List<Curve> drawCommercialArea()
        {
            List<Curve> output = new List<Curve>();

            foreach (HouseholdProperties i in this.Commercial)
            {
                List<Point3d> outlinePoints = new List<Point3d>();
                Point3d pt = new Point3d(i.Origin);
                Vector3d x = new Vector3d(i.XDirection);
                Vector3d y = new Vector3d(i.YDirection);
                double xa = i.XLengthA;
                double xb = i.XLengthB;
                double ya = i.YLengthA;
                double yb = i.YLengthB;

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

                output.Add(outlineCurve);
            }

            return output;
        }

        public List<List<List<List<Line>>>> getLightingWindow()
        {
            List<List<List<List<Line>>>> output = new List<List<List<List<Line>>>>();

            for (int i = 0; i < this.HouseholdProperties.Count; i++)
            {
                List<List<List<Line>>> tempLine_i = new List<List<List<Line>>>();

                for (int j = 0; j < this.HouseholdProperties[i].Count; j++)
                {
                    List<List<Line>> tempLine_j = new List<List<Line>>();

                    for (int k = 0; k < this.HouseholdProperties[i][j].Count; k++)
                    {
                        List<Line> tempLine_k = new List<Line>();

                        for (int l = 0; l < this.HouseholdProperties[i][j][k].LightingEdge.Count; l++)
                        {
                            tempLine_k.Add(this.HouseholdProperties[i][j][k].LightingEdge[l]);
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

            foreach (List<List<HouseholdProperties>> i in this.HouseholdProperties)
            {
                foreach (List<HouseholdProperties> j in i)
                {
                    foreach (HouseholdProperties k in j)
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

            for (int i = 0; i < this.CoreProperties.Count; i++)
            {
                double tempSum = 0;

                foreach (CoreProperties j in this.CoreProperties[i])
                {
                    tempSum += j.GetArea();
                }

                foreach (HouseholdProperties j in this.HouseholdProperties[i][0])
                {
                    tempSum += j.GetArea();
                }

                output.Add(tempSum);
            }

            return output.ToArray();
        }

        public double GetBuildingArea()
        {
            double[] buildingAreaPerApartment = this.GetBuildingAreaPerApartment();

            double output = 0;

            foreach (double i in buildingAreaPerApartment)
            {
                output = output + i;
            }

            return output;
        }

        public double GetBuildingCoverage()
        {
            double buildingArea = GetBuildingArea();

            return buildingArea / this.Plot.GetArea() * 100;
        }

        public double[] GetGrossAreaPerApartment()
        {
            List<double> output = new List<double>();

            for (int i = 0; i < this.HouseholdProperties.Count; i++)
            {
                double tempSum = 0;
                for (int j = 0; j < this.HouseholdProperties[i].Count; j++)
                {
                    for (int k = 0; k < this.HouseholdProperties[i][j].Count; k++)
                    {
                        tempSum += this.HouseholdProperties[i][j][k].GetExclusiveArea();
                        tempSum += this.HouseholdProperties[i][j][k].GetWallArea();
                    }
                }

                foreach (CoreProperties j in this.CoreProperties[i])
                {
                    tempSum = tempSum + j.GetArea() * (j.Stories + 1);
                }

                output.Add(tempSum);
            }

            return output.ToArray();
        }

        public double GetExclusiveAreaSum()
        {
            double tempSum = 0;

            for (int i = 0; i < this.HouseholdProperties.Count; i++)
            {
                for (int j = 0; j < this.HouseholdProperties[i].Count; j++)
                {
                    for (int k = 0; k < this.HouseholdProperties[i][j].Count; k++)
                    {
                        tempSum += this.HouseholdProperties[i][j][k].GetExclusiveArea();
                    }
                }
            }

            return tempSum;
        }

        public double GetCoreAreaOnEarthSum() ///////////////////////////////////// commercial, public 포함
        {
            double coreAreaSum = 0;

            foreach (List<CoreProperties> i in this.CoreProperties)
            {
                foreach (CoreProperties j in i)
                {
                    coreAreaSum += j.GetArea();
                }
            }

            return coreAreaSum;
        }

        public double GetCoreAreaSum()
        {
            double coreAreaSum = 0;

            foreach (List<CoreProperties> i in this.CoreProperties)
            {
                foreach (CoreProperties j in i)
                {
                    coreAreaSum += j.GetArea() * (j.Stories + 1);
                }
            }

            return coreAreaSum;
        }

        public double GetGrossArea()
        {
            double[] grossAreaPerApartment = this.GetGrossAreaPerApartment();

            double output = 0;

            foreach (double i in grossAreaPerApartment)
            {
                output = output + i;
            }

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

        public double GetLegalParkingLotNums()
        {
            double legalParkingLotByUnitNum = 0;
            double legalParkingLotByUnitSize = 0;

            for (int i = 0; i < this.HouseholdProperties.Count; i++)
            {
                for (int j = 0; j < this.HouseholdProperties[i].Count; j++)
                {
                    for (int k = 0; k < this.HouseholdProperties[i][j].Count; k++)
                    {
                        if (this.HouseholdProperties[i][j][k].GetExclusiveArea() > 60 * Math.Pow(10, 6))
                            legalParkingLotByUnitNum = legalParkingLotByUnitNum + 1;
                        else
                            legalParkingLotByUnitNum = legalParkingLotByUnitNum + 0.8;

                        if (this.HouseholdProperties[i][j][k].GetExclusiveArea() > 85 * Math.Pow(10, 6))
                            legalParkingLotByUnitNum = this.HouseholdProperties[i][j][k].GetExclusiveArea() / 750000;
                        else
                            legalParkingLotByUnitNum = this.HouseholdProperties[i][j][k].GetExclusiveArea() / 650000;
                    }

                }
            }

            if (legalParkingLotByUnitNum > legalParkingLotByUnitSize)
            {
                return legalParkingLotByUnitNum - (legalParkingLotByUnitNum % 1) + 1;
            }
            else
            {
                return legalParkingLotByUnitSize - (legalParkingLotByUnitSize % 1) + 1;
            }
        }

        public bool IsParkingLotSufficient()
        {
            if (this.ParkingLotOnEarth.GetCount() + this.ParkingLotUnderGround.GetCount() >= this.GetLegalParkingLotNums())
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public int GetHouseholdCount()
        {
            int output = 0;

            for (int i = 0; i < HouseholdProperties.Count(); i++)
            {
                for (int j = 0; j < HouseholdProperties[i].Count(); j++)
                {
                    output += HouseholdProperties[i][j].Count();
                }
            }

            return output;
        }

        public double GetRooftopCoreArea()
        {
            double output = 0;

            foreach (List<CoreProperties> i in CoreProperties)
            {
                foreach (CoreProperties j in i)
                    output += j.GetArea();
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

        ~ApartmentGeneratorOutput()
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

    public class CoreProperties
    {
        //Constructor, 생성자

        public CoreProperties(Point3d origin, Vector3d xDirection, Vector3d yDirection, CoreType coreType, double stories, double coreInterpenetration)
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
            if (coreType == CoreType.Horizontal)
                return 7660 * 4400 - 3100 * 150 + (4250 + 4400 - this.CoreInterpenetration * 2) * 300;
            else if (coreType == CoreType.Parallel)
                return 5200 * 4860 - 2700 * 300 + (4860 + 4560 - this.CoreInterpenetration * 2) * 300;
            else if (coreType == CoreType.Folded)
                return 5800 * 6060;
            else if (coreType == CoreType.Vertical)
                return 2500 * 8220;
            else if (CoreType == CoreType.Vertical_AG1)
                return 2500 * 7920 + (7920 - this.CoreInterpenetration) * 300 * 2;
            else
                return 0;
        }

        //Property, 속성

        public Point3d Origin { get { return origin; } }
        public Vector3d XDirection { get { return xDirection; } }
        public Vector3d YDirection { get { return yDirection; } }
        public CoreType CoreType { get { return coreType; } }
        public double Stories { get; set; }
        public double CoreInterpenetration { get; set; }
    }

    public class HouseholdStatistics
    {
        public HouseholdStatistics(HouseholdProperties householdProperties, int count)
        {
            this.origin = householdProperties.Origin;
            this.xDirection = householdProperties.XDirection;
            this.yDirection = householdProperties.YDirection;
            this.xLengthA = householdProperties.XLengthA;
            this.xLengthB = householdProperties.XLengthB;
            this.yLengthA = householdProperties.YLengthA;
            this.yLengthB = householdProperties.YLengthB;
            this.householdSizeType = householdProperties.HouseholdSizeType;
            this.exclusiveArea = householdProperties.GetExclusiveArea();
            this.lightingEdge = householdProperties.LightingEdge;
            this.entrancePoint = householdProperties.EntrancePoint;
            this.wallFactor = householdProperties.WallFactor;
            this.Count = count;
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

        public HouseholdProperties ToHouseholdProperties()
        {
            HouseholdProperties output = new HouseholdProperties(this.origin, this.xDirection, this.yDirection, this.XLengthA, this.XLengthB, this.YLengthA, this.YLengthB, this.HouseholdSizeType, this.ExclusiveArea, this.LightingEdge, this.EntrancePoint, this.WallFactor);
            return output;
        }

        public double GetArea()
        {
            return (xLengthA * YLengthA) - (xLengthB * yLengthB);
        }

        public double GetBalconyArea()
        {
            return this.GetArea() - this.exclusiveArea;
        }

        public double GetExclusiveArea()
        {
            return exclusiveArea * 0.91;
        }

        public double GetWallArea()
        {
            return exclusiveArea * 0.09;
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
        public double ExclusiveArea { get { return exclusiveArea; } }
        //public List<int> ConnectedCoreIndex { get { return connectedCoreIndex; } }
        public List<Line> LightingEdge { get { return lightingEdge; } }
        public Point3d EntrancePoint { get { return entrancePoint; } }
        public List<double> WallFactor { get { return wallFactor; } }
    }

    public class HouseholdProperties
    {
        //Constructor, 생성자

        public HouseholdProperties(Point3d origin, Vector3d xDirection, Vector3d yDirection, double xLengthA, double xLengthB, double yLengthA, double yLengthB, int householdSizeType, double exclusiveArea, List<Line> lightingEdge, Point3d entrancePoint, List<double> wallFactor)
        {
            this.Origin = origin;
            this.XDirection = xDirection;
            this.YDirection = yDirection;
            this.XLengthA = xLengthA;
            this.XLengthB = xLengthB;
            this.YLengthA = yLengthA;
            this.YLengthB = yLengthB;
            this.HouseholdSizeType = householdSizeType;
            this.ExclusiveArea = exclusiveArea;
            this.LightingEdge = lightingEdge;
            this.WallFactor = wallFactor;
            this.EntrancePoint = entrancePoint;
            //this.connectedCoreIndex = connectedCoreIndex;
        }

        public HouseholdProperties(HouseholdProperties householdProperties)
        {
            this.Origin = householdProperties.Origin;
            this.XDirection = householdProperties.XDirection;
            this.YDirection = householdProperties.YDirection;
            this.XLengthA = householdProperties.XLengthA;
            this.XLengthB = householdProperties.XLengthB;
            this.YLengthA = householdProperties.YLengthA;
            this.YLengthB = householdProperties.YLengthB;
            this.HouseholdSizeType = householdProperties.HouseholdSizeType;
            this.ExclusiveArea = householdProperties.GetExclusiveArea() + householdProperties.GetWallArea();
            this.LightingEdge = householdProperties.LightingEdge;
            this.WallFactor = householdProperties.WallFactor;
            this.EntrancePoint = householdProperties.EntrancePoint;
        }

        //Field, 필드

        //Method, 메소드
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
            return outlineCurve;

        }


        public double GetArea()
        {
            return (XLengthA * YLengthA) - (XLengthB * YLengthB);
        }

        public double GetBalconyArea()
        {
            return this.GetArea() - this.ExclusiveArea;
        }

        public double GetExclusiveArea()
        {
            return ExclusiveArea * 0.91;
        }

        public double GetWallArea()
        {
            return ExclusiveArea * 0.09;
        }

        //Property, 속성

        public Point3d Origin { get; set; }
        public Vector3d XDirection { get; set; }
        public Vector3d YDirection { get; set; }
        public double XLengthA { get; set; }
        public double XLengthB { get; set; }
        public double YLengthA { get; set; }
        public double YLengthB { get; set; }
        public int HouseholdSizeType { get; set; }
        public double ExclusiveArea { private get; set; }
        //public List<int> ConnectedCoreIndex { get { return connectedCoreIndex; } }
        public List<Line> LightingEdge { get; set; }
        public List<double> WallFactor { get; private set; }
        public Point3d EntrancePoint { get; set; }
    }

    public class Regulation

    {
        //Constructor, 생성자

        public Regulation(double stories)
        {
            this.height = Consts.FloorHeight * stories;

            if (stories >= 5)//apartment
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
        }

        //Field, 필드

        private double height;
        private double distanceFromRoad; //건축선으로부터 건축물까지 띄어야 하는 거리, 완화
        private double distanceFromPlot; //인접대지 경계선부터 띄어야 하는 거리, 완화
        private double distanceFromNorth = 0.5; //일조에 의한 높이제한(정북방향)
        private double distanceByLighting = 0.25; //인접대지경계선(채광창), 완화
        private double distanceLW = 0.5; //채광창의 각 부분높이의 0.5배
        private double distanceLL = 0.8; //채광창과 채광창이 마주볼경우
        private double distanceWW = 4000; //측벽과 측벽

        //Method, 메소드

        public Curve fromSurroundingsCurve(Plot plot, Regulation regulation, Curve[] plotArr)
        {
            //법규적용(대지안의 공지)

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
                    distanceFromSurroundings[i] = regulation.distanceFromRoad;
                else if (plot.SimplifiedSurroundings[i] == 0)
                    distanceFromSurroundings[i] = regulation.DistanceFromPlot;
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

                ptsFromSurroundings.Add(Rhino.Geometry.Intersect.Intersection.CurveCurve(curveH, curveI, 0, 0)[0].PointA);
            }

            ptsFromSurroundings.Add(ptsFromSurroundings[0]);

            Curve fromSurroundingCurve = (new Polyline(ptsFromSurroundings)).ToNurbsCurve();

            return fromSurroundingCurve;
        }

        public Curve fromNorthCurve(Plot plot, Regulation regulation, Curve[] plotArr)
        {
            //법규적용(일조에 의한 높이제한)

            double[] distanceFromNorth = new double[plotArr.Length];
            for (int i = 0; i < plotArr.Length; i++)
            {
                Vector3d tempVector = new Vector3d(plotArr[i].PointAt(plotArr[i].Domain.T1) - plotArr[i].PointAt(plotArr[i].Domain.T0));
                tempVector = tempVector / tempVector.Length;

                double moveDistance = 0;

                if (tempVector.X > 0 && plot.SimplifiedSurroundings[i] >= 0)
                {
                    double tempSineFactor = System.Math.Abs(tempVector.X);

                    double tempDistanceByRoad = plot.SimplifiedSurroundings[i] * 1000 / 2 * tempVector.Length / System.Math.Abs(tempVector.X);
                    if (tempDistanceByRoad < regulation.DistanceFromNorth)
                        moveDistance = regulation.DistanceFromNorth - tempDistanceByRoad;
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
            return fromNorthCurve;
        }



        //Property, 속성

        public double DistanceFromRoad { get { return distanceFromRoad; } }
        public double DistanceFromPlot { get { return distanceFromPlot; } }
        public double DistanceFromNorth { get { return distanceFromNorth * (Consts.PilotiHeight + height); } }
        public double DistanceByLighting { get { return distanceByLighting * height; } }
        public double DistanceLW { get { return distanceLW * height; } }
        public double DistanceLL { get { return distanceLL * height; } }
        public double DistanceWW { get { return distanceWW; } }

        public BuildingType BuildingType { get; private set; }
    }

    public class ParkingLine
    {
        //Constrcutor, 생성자

        public ParkingLine(Rectangle3d boundary)
        {
            this.Boundary = boundary;
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
            this.ParkingLines = new List<ParkingLine>();
        }

        public ParkingLotOnEarth(List<ParkingLine> parkingLines)
        {
            this.ParkingLines = parkingLines;
        }

        //method, 메소드

        public double GetAreaSum()
        {
            double output = 0;

            foreach (ParkingLine i in this.ParkingLines)
                output += i.GetArea();

            return output;
        }

        public double GetCount()
        {
            return ParkingLines.Count;
        }

        //프로퍼티

        public List<ParkingLine> ParkingLines { get; private set; }
    }

    public class ParkingLotUnderGround
    {
        //Constrcutor, 생성자

        public ParkingLotUnderGround()
        {
            this.ParkingLines = new List<ParkingLine>();
            this.ParkingArea = 0;
            this.Floors = -1;
        }

        public ParkingLotUnderGround(List<ParkingLine> parkingLines, double parkingArea, int floors)
        {
            this.ParkingLines = parkingLines;
            this.ParkingArea = parkingArea;
            this.Floors = floors;
        }

        //method, 메소드

        public double GetAreaSum()
        {
            double output = 0;

            foreach (ParkingLine i in this.ParkingLines)
                output += i.GetArea();

            return output;
        }

        public double GetCount()
        {
            return ParkingLines.Count;
        }

        //프로퍼티

        public List<ParkingLine> ParkingLines { get; private set; }
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

            r.Close();

            this.libraryString = filePath;
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
        }

        public string libraryString { get; private set; }
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
        public CurveConduit()
        {

        }

        public CurveConduit(System.Drawing.Color displayColor)
        {
            this.displayColor = displayColor;
        }

        protected override void CalculateBoundingBox(CalculateBoundingBoxEventArgs e)
        {
            base.CalculateBoundingBox(e);
            {
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

            foreach (Curve i in CurveToDisplay)
            {
                e.Display.DrawCurve(i, displayColor);
            }
        }

        public List<Curve> CurveToDisplay { private get; set; }
        private System.Drawing.Color displayColor = System.Drawing.Color.Red;
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