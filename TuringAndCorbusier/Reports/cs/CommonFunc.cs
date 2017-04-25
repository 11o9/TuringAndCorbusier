using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rhino.Geometry;
using System.Windows.Forms;
using Microsoft.Win32;
using System.IO.Ports;
using Oracle.ManagedDataAccess.Client;
using Rhino.Collections;



namespace TuringAndCorbusier
{

    class CommonFunc
    {        
        class myFunc
        {
            public static List<double> internalAngle(Polyline pLine)
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
        }

        public static string GetApartmentType(ApartmentGeneratorOutput agOutput)
        {
            if (agOutput.AGtype == "PT-1")
                return "판상형";
            else if (agOutput.AGtype == "PT-3")
                return "중정형";
            else if (agOutput.AGtype == "PT-4")
                return "ㄷ자형";
            else
                return "null";
        }

        public static Curve scaleCurve(Curve input, double ScaleFactor)
        {
            Curve inputCopy = input.DuplicateCurve();

            inputCopy.Transform(Transform.Scale(input.GetBoundingBox(false).Center, ScaleFactor));

            return inputCopy;
        }


        public static double getArea(Curve input)
        {
            Polyline plotPolyline;
            input.TryGetPolyline(out plotPolyline);

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

        public static double GetBuildingCoverage_outSideAGOutput(List<List<List<HouseholdProperties>>> householdProperties, List<List<CoreProperties>> coreProperties, Plot plot)
        {
            double ExclusiveAreaSum = 0;

            foreach (List<List<HouseholdProperties>> i in householdProperties)
            {

                    foreach (HouseholdProperties k in i[0])
                    {
                        ExclusiveAreaSum += k.GetExclusiveArea();
                    }
                
            }

            foreach (List<CoreProperties> i in coreProperties)
            {
                foreach (CoreProperties j in i)
                {
                    ExclusiveAreaSum += j.GetArea();
                }
            }

            return ExclusiveAreaSum / plot.GetArea() * 100;
        }

        public static double GetGrossArea_outSideAGOutput(List<List<List<HouseholdProperties>>> householdProperties, List<List<CoreProperties>> coreProperties, Plot plot)
        {
            double ExclusiveAreaSum = 0;

            foreach (List<List<HouseholdProperties>> i in householdProperties)
            {
                foreach (List<HouseholdProperties> j in i)
                {
                    foreach (HouseholdProperties k in j)
                    {
                        ExclusiveAreaSum += k.GetExclusiveArea();
                        ExclusiveAreaSum += k.GetWallArea();
                    }
                }
            }

            foreach (List<CoreProperties> i in coreProperties)
            {
                foreach (CoreProperties j in i)
                {
                    double dkdk = j.GetArea();
                    ExclusiveAreaSum += j.GetArea() * (j.Stories + 1);

                }
            }

            return ExclusiveAreaSum / plot.GetArea() * 100;
        }

        public static double GetGrossArea_outSideAGOutput(List<List<List<HouseholdProperties>>> householdProperties, List<List<CoreProperties>> coreProperties, List<HouseholdProperties> publicFacilitires, List<HouseholdProperties> commercialFacilities , Plot plot)
        {
            double ExclusiveAreaSum = 0;

            foreach(List<List<HouseholdProperties>> i in householdProperties)
            {
                foreach(List<HouseholdProperties> j in i)
                {
                    foreach(HouseholdProperties k in j)
                    {
                        ExclusiveAreaSum += k.GetExclusiveArea();
                        ExclusiveAreaSum += k.GetWallArea();
                    }
                }
            }

            foreach(HouseholdProperties i in publicFacilitires)
            {
                ExclusiveAreaSum += i.GetExclusiveArea();
            }

            foreach(HouseholdProperties i in commercialFacilities)
            {
                ExclusiveAreaSum += i.GetExclusiveArea();
            }

            foreach(List<CoreProperties> i in coreProperties)
            {
                foreach(CoreProperties j in i)
                {
                    ExclusiveAreaSum += j.GetArea() * (j.Stories + 1);
                }
            }

            return ExclusiveAreaSum / plot.GetArea() * 100;
        }
        
        public static void finalizePatternOutput(ref List<List<List<HouseholdProperties>>> householdProperties,ref List<List<CoreProperties>> coreProperties, ref double currentFloorAreaRatio, ref double currentBuildingCoverage, Plot plot, List<Curve> AptCurves, double legalFloorAreaRatio, double legalBuildingCoverage, out List<HouseholdProperties> PublicFacility, out List<HouseholdProperties> CommercialArea)
        {
            double maximumCommercialRatio = 10;
           

            double grossAreaRemain = plot.GetArea() * (legalFloorAreaRatio -  currentFloorAreaRatio) / 100;
            double thisFloorAreaRatio = currentFloorAreaRatio;
            double thisBuildingCoverage = currentBuildingCoverage;

            //용적률 튀어나가는 녀석들 수정

            if (thisFloorAreaRatio > legalFloorAreaRatio)
            {
                int tempBuildingIndex = 0;

                while (thisFloorAreaRatio > legalFloorAreaRatio )
                {
                    List<HouseholdProperties> tempHouseholdProperty = householdProperties[tempBuildingIndex][householdProperties[tempBuildingIndex].Count - 1];
                    List<CoreProperties> tempCoreProperty = coreProperties[tempBuildingIndex];

                    double expectedRemoveArea = 0;

                    foreach (HouseholdProperties i in tempHouseholdProperty)
                    {
                        expectedRemoveArea += i.GetExclusiveArea();
                        expectedRemoveArea += i.GetWallArea();
                    }

                    foreach (CoreProperties i in tempCoreProperty)
                        expectedRemoveArea += i.GetArea();

                    householdProperties[tempBuildingIndex].RemoveAt(householdProperties[tempBuildingIndex].Count - 1);

                    foreach (CoreProperties i in tempCoreProperty)
                        i.Stories -= 1;

                    grossAreaRemain += expectedRemoveArea;
                    thisFloorAreaRatio = (plot.GetArea() * legalFloorAreaRatio / 100 - grossAreaRemain) / plot.GetArea() * 100;

                    tempBuildingIndex = (tempBuildingIndex + 1 + householdProperties.Count()) % householdProperties.Count();
                }

            }

            //20160519 기준 서울특별시 주택조례 주민공동이용시설에 관한 규정 << 세대수 * 2.5제곱미터 * 1.25(지자체별로 상이)

            bool IsSomethingRemoved = false;

            if (householdPropertyCounter(householdProperties) >= 100)
            {
                int currentHouseholdCount = householdPropertyCounter(householdProperties);
                double currentNeededPublicFacilityArea = currentHouseholdCount * 2500000 * 1.25;

                int tempBuildingIndex = 0;

                while(grossAreaRemain - currentNeededPublicFacilityArea < 0 && currentHouseholdCount > 100)
                {
                    List<HouseholdProperties> tempHouseholdProperty = householdProperties[tempBuildingIndex][householdProperties[tempBuildingIndex].Count - 1];
                    List<CoreProperties> tempCoreProperty = coreProperties[tempBuildingIndex];

                    double expectedRemoveArea = 0;

                    foreach (HouseholdProperties i in tempHouseholdProperty)
                    {
                        expectedRemoveArea += i.GetExclusiveArea();
                        expectedRemoveArea += i.GetWallArea();
                    }

                foreach (CoreProperties i in tempCoreProperty)
                        expectedRemoveArea += i.GetArea();

                    int expectedHouseholdCount = currentHouseholdCount - tempHouseholdProperty.Count() ;
                    double expectedPublicFacilityArea = expectedHouseholdCount * 2500000 * 1.25;

                    if (plot.GetArea() * legalFloorAreaRatio / 100 - grossAreaRemain + expectedRemoveArea + expectedPublicFacilityArea > plot.GetArea() * legalFloorAreaRatio / 100 * (100 - maximumCommercialRatio) / 100)
                    {
                        householdProperties[tempBuildingIndex].RemoveAt(householdProperties[tempBuildingIndex].Count - 1);
                        
                        foreach(CoreProperties i in tempCoreProperty)
                            i.Stories -= 1;

                        grossAreaRemain += expectedRemoveArea;
                        thisFloorAreaRatio = (plot.GetArea() * maximumCommercialRatio / 100 - grossAreaRemain) / plot.GetArea() * 100;

                        currentHouseholdCount -= tempHouseholdProperty.Count();
                        currentNeededPublicFacilityArea -= expectedPublicFacilityArea;

                        IsSomethingRemoved = true;
                    }

                    if (tempBuildingIndex == householdProperties.Count - 1)
                    {
                        if (IsSomethingRemoved == false)
                            break;
                        else
                            IsSomethingRemoved = false;
                    }
                             
                    tempBuildingIndex = (tempBuildingIndex + 1 + householdProperties.Count()) % householdProperties.Count();
                }

                List<HouseholdProperties> nonResi = createCommercialFacility(householdProperties, AptCurves, plot, legalBuildingCoverage - thisBuildingCoverage , legalFloorAreaRatio - thisFloorAreaRatio);
                List<HouseholdProperties> publicFacility = new List<HouseholdProperties>();
                List<HouseholdProperties> commercialArea = new List<HouseholdProperties>();

                double tempNeededPublicFacilityArea = 0;
                int currentIndex = 0;

                for(int i = 0; i < nonResi.Count(); i++)
                {
                    if (tempNeededPublicFacilityArea < currentNeededPublicFacilityArea)
                    {
                        publicFacility.Add(nonResi[currentIndex]);
                        tempNeededPublicFacilityArea += nonResi[currentIndex].GetArea();
                    }
                    commercialArea.Add(nonResi[i]);
                }

                PublicFacility = publicFacility;
                CommercialArea = commercialArea;

                return;
            }
            else
            {
                PublicFacility = new List<HouseholdProperties>();
                CommercialArea = new List<HouseholdProperties>();

                return;
            }         
        }
        
        private static int householdPropertyCounter(List<List<List<HouseholdProperties>>> ObjectToCount)
        {
            int output = 0;

            foreach(List<List<HouseholdProperties>> i in ObjectToCount)
            {
                foreach(List<HouseholdProperties> j in i)
                {
                    output += j.Count();
                }
            }

            return output;
        }


        public static List<HouseholdProperties> createCommercialFacility(List<List<List<HouseholdProperties>>> householdProperties, List<Curve> apartmentBaseCurves, Plot plot, double buildingCoverageReamin, double grossAreaRatioRemain)
        {
            double grossAreaRemain = plot.GetArea() * grossAreaRatioRemain / 100;
            double buildingAreaRemain = plot.GetArea() * buildingCoverageReamin / 100;

            List <HouseholdProperties> output = new List<HouseholdProperties>();

            List<double> distance = new List<double>();

            for (int i = 0; i < apartmentBaseCurves.Count(); i++)
            {
                double tempParameter = new double();
                plot.SimplifiedBoundary.ClosestPoint(apartmentBaseCurves[i].PointAt(apartmentBaseCurves[i].Domain.Mid), out tempParameter);

                distance.Add(plot.SimplifiedBoundary.PointAt(tempParameter).DistanceTo(apartmentBaseCurves[i].PointAt(apartmentBaseCurves[i].Domain.Mid)));
            }

            List<double> sortedDistance = new List<double>(distance);
            sortedDistance.Sort();

            var distanceGrade = (
                from tempDist in distance
                select sortedDistance.IndexOf(tempDist)              
                ).ToList();

            for(int i = 0; i < distance.Count(); i++)
            {
                int buildingIndex = sortedDistance.IndexOf(distance[i]);
                sortedDistance[buildingIndex] = 0;

                int tempColor = 255;

                if (grossAreaRemain >= 0 && householdProperties[buildingIndex].Count != 0)
                {
                    
                    for (int j = 0; j < householdProperties[buildingIndex][0].Count(); j++)
                    {


                        if(grossAreaRemain - householdProperties[buildingIndex][0][j].GetArea() >= 0)
                        {
                            HouseholdProperties tempHouseholdProperties = new HouseholdProperties(householdProperties[buildingIndex][0][j]);

                            double LastHouseholdProperteis = tempHouseholdProperties.GetArea();

                            if (tempHouseholdProperties.YLengthB != 0) 
                            {
                                tempHouseholdProperties.XLengthA = tempHouseholdProperties.XLengthA - tempHouseholdProperties.XLengthB;
                                tempHouseholdProperties.XLengthB = 0;
                            }

                            tempHouseholdProperties.Origin = new Point3d(tempHouseholdProperties.Origin.X, tempHouseholdProperties.Origin.Y, 0);

                            if(buildingAreaRemain >= 0)
                            {
                                double maximumProtrusion = 2000;

                                Point3d tempBaseStart = new Point3d(tempHouseholdProperties.Origin - tempHouseholdProperties.YDirection * (tempHouseholdProperties.YLengthA - tempHouseholdProperties.YLengthB));
                                Point3d tempBaseEnd = new Point3d(tempBaseStart - tempHouseholdProperties.YDirection * maximumProtrusion);

                                Curve tempBase = new LineCurve(tempBaseStart, tempBaseEnd);
                                Curve tempAnotherBase = new LineCurve(tempBaseStart + tempHouseholdProperties.XDirection * tempHouseholdProperties.XLengthA, tempBaseEnd + tempHouseholdProperties.XDirection * tempHouseholdProperties.XLengthA);
                                Curve tempSide = new LineCurve(tempBaseStart, tempBaseStart + tempHouseholdProperties.XDirection * tempHouseholdProperties.XLengthA);
                                Curve tempAnotherSide = new LineCurve(tempBaseEnd, tempBaseEnd + tempHouseholdProperties.XDirection * tempHouseholdProperties.XLengthA);

                                Curve[] tempBoundaryCrvs = { tempBase, tempAnotherBase, tempSide, tempAnotherSide };

                                Curve tempBoundary = Curve.JoinCurves(tempBoundaryCrvs)[0];

                                var tempIntersection = Rhino.Geometry.Intersect.Intersection.CurveCurve(tempBoundary, plot.SimplifiedBoundary, 0, 0);

                                var parameters = (from intersectionEvent in tempIntersection
                                                 select intersectionEvent.ParameterB).ToList();

                                Curve[] shatteredCurves = plot.SimplifiedBoundary.Split(parameters);

                                List<Curve> tempInnerBoundary = new List<Curve>();

                                foreach(Curve k in shatteredCurves)
                                {
                                    if(tempBoundary.Contains(k.PointAt(k.Domain.Mid)) != PointContainment.Outside)
                                    {
                                        tempInnerBoundary.Add(k);
                                    }
                                }

                                List<Point3d> pointSet = new List<Point3d>();

                                foreach(Curve k in tempInnerBoundary)
                                {
                                    pointSet.Add(k.PointAt(k.Domain.T0));

                                    foreach (Curve l in k.DuplicateSegments())
                                        pointSet.Add(l.PointAt(l.Domain.T0));

                                    pointSet.Add(k.PointAt(k.Domain.T1));
                                }

                                List<double> pointSetDistance = new List<double>();

                                foreach(Point3d k in pointSet)
                                {
                                    double outParameter;
                                    tempSide.ClosestPoint(k, out outParameter);

                                    pointSetDistance.Add(tempSide.PointAt(outParameter).DistanceTo(k));
                                }

                                pointSetDistance.Add(maximumProtrusion);

                                tempHouseholdProperties.YLengthA += pointSetDistance.Min();
                                buildingAreaRemain -= pointSetDistance[0] * tempSide.GetLength();
                            }

                            output.Add(tempHouseholdProperties);
                            double thisArea = tempHouseholdProperties.GetArea();
                            grossAreaRemain -= tempHouseholdProperties.GetArea();
                        }
                        else
                        {
                            break;
                        }
                    }
                }
                else
                {
                    break;
                }
            }

            return output;
        }
        
        


        public static string GetPlotBoundaryVector(List<Point3d> plotBoundary)
        {
            string output = "";

            for(int i = 0; i < plotBoundary.Count(); i++)
            {
                if (i != 0)
                    output += "/";

                output += plotBoundary[i].X.ToString() + "," + plotBoundary[i].Y.ToString() + "," + plotBoundary[i].Z.ToString();
            }

            return output;
        }

        public static int GetLastPreDesignNo(List<string> idColumnName, List<string> idColumnCode)
        {
            const string connectionString = "Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=oracle3.ejudata.co.kr)(PORT=1521))(CONNECT_DATA=(SERVER=DEDICATED)(SID = oracle3)));User Id=whruser_sh;Password=ejudata;";

            List<int> PRE_DESIGN_NO_INDEX = new List<int>();

            string readSql = "select * FROM TD_DESIGN_DETAIL";

            if (idColumnName.Count() != 0)
                readSql += " WHERE";

            for (int i = 0; i < idColumnName.Count(); i++)
            {
                if (i != 0)
                    readSql += " AND";

                readSql += " " + idColumnName[i] + "=" + idColumnCode[i] + " ";
            }

            using (OracleConnection connection = new OracleConnection(connectionString))
            {
                try
                {
                    connection.Open();

                    OracleCommand readComm = new OracleCommand(readSql, connection);
                    OracleDataReader reader = readComm.ExecuteReader();

                    while(reader.Read())
                    {
                        PRE_DESIGN_NO_INDEX.Add(int.Parse(reader["REGI_PRE_DESIGN_NO"].ToString()));
                    }
                }
                catch (OracleException ex)
                {
                    MessageBox.Show(ex.ToString());
                }
            }

            PRE_DESIGN_NO_INDEX.Sort();

            if (PRE_DESIGN_NO_INDEX.Count() == 0)
                return -1;

            return PRE_DESIGN_NO_INDEX[PRE_DESIGN_NO_INDEX.Count() - 1];
        }

        public static string GetProjectNameFromServer(List<string> idColumnName, List<string> idColumnCode)
        {
            string output = "";

            string readSql = "select * FROM TN_REGI_MASTER";

            if (idColumnName.Count() != 0)
                readSql += " WHERE";

            for (int i = 0; i < idColumnName.Count(); i++)
            {
                if (i != 0)
                    readSql += " AND";

                readSql += " " + idColumnName[i] + "=" + idColumnCode[i] + " ";
            }

            using (OracleConnection connection = new Oracle.ManagedDataAccess.Client.OracleConnection(Consts.connectionString))
            {
                try
                {
                    connection.Open();

                    OracleCommand readComm = new OracleCommand(readSql, connection);
                    OracleDataReader reader = readComm.ExecuteReader();

                    while (reader.Read())
                    {
                        output = reader["REGI_BIZNS_NM"].ToString();
                    }
                }
                catch (OracleException ex)
                {
                    MessageBox.Show(ex.ToString());
                }
            }

            return output;
        }

        public static string GetPlotTypeFromServer(List<string> idColumnName, List<string> idColumnCode)
        {
            string output = "";

            string readSql = "select * FROM TN_PREV_ASSET";

            if (idColumnName.Count() != 0)
                readSql += " WHERE";

            for (int i = 0; i < idColumnName.Count(); i++)
            {
                if (i != 0)
                    readSql += " AND";

                readSql += " " + idColumnName[i] + "=" + idColumnCode[i] + " ";
            }

            using (OracleConnection connection = new OracleConnection(Consts.connectionString))
            {
                try
                {
                    connection.Open();

                    OracleCommand readComm = new OracleCommand(readSql, connection);
                    OracleDataReader reader = readComm.ExecuteReader();

                    while (reader.Read())
                    {
                        output = reader["USE_REGION_CD"].ToString();
                    }
                }
                catch (OracleException ex)
                {
                    MessageBox.Show(ex.ToString());
                }
            }

            return output;
        }

        public static double GetManualAreaFromServer(List<string> idColumnName, List<string> idColumnCode)
        {
            List<string> areaList = new List<string>();

            string readSql = "select * FROM TN_PREV_ASSET";

            if (idColumnName.Count() != 0)
                readSql += " WHERE";

            for (int i = 0; i < idColumnName.Count(); i++)
            {
                if (i != 0)
                    readSql += " AND";

                readSql += " " + idColumnName[i] + "=" + idColumnCode[i] + " ";
            }

            using (OracleConnection connection = new OracleConnection(Consts.connectionString))
            {
                try
                {
                    connection.Open();

                    OracleCommand readComm = new OracleCommand(readSql, connection);
                    OracleDataReader reader = readComm.ExecuteReader();

                    while(reader.Read())
                    {
                        areaList.Add(reader["LAND_AREA"].ToString());
                    }
                }
                catch (OracleException ex)
                {
                    MessageBox.Show(ex.ToString());
                }
            }

            double areaMassSum = 0;

            foreach(string i in areaList)
            {
                areaMassSum += double.Parse(i);
            }

            return areaMassSum;
        }

        public static string GetAddressFromServer(List<string> idColumnName, List<string> idColumnCode)
        {
            List<string> addressList = new List<string>();

            string readSql = "select * FROM TN_PREV_ASSET";

            if (idColumnName.Count() != 0)
                readSql += " WHERE";

            for (int i = 0; i < idColumnName.Count(); i++)
            {
                if (i != 0)
                    readSql += " AND";

                readSql += " " + idColumnName[i] + "=" + idColumnCode[i] + " ";
            }

            using (OracleConnection connection = new OracleConnection(Consts.connectionString))
            {
                try
                {
                    connection.Open();

                    OracleCommand readComm = new OracleCommand(readSql, connection);
                    OracleDataReader reader = readComm.ExecuteReader();

                    while(reader.Read())
                    {

                        string tempLandCode = reader["LAND_CD"].ToString();
                        string tempReadSql = "select * FROM TN_LAW_LAND WHERE LAND_CD=" + tempLandCode;

                        using (OracleConnection tempConnection = new OracleConnection(Consts.connectionString))
                        {
                            tempConnection.Open();

                            OracleCommand tempCommand = new OracleCommand(tempReadSql, tempConnection);
                            OracleDataReader tempReader = tempCommand.ExecuteReader();

                            while (tempReader.Read())
                            {
                                addressList.Add(tempReader["LAND_SIDO_NM"].ToString());
                                addressList.Add(tempReader["LAND_SIGUNGU_NM"].ToString());
                                addressList.Add(tempReader["LAND_DONG_NM"].ToString());

                                if (int.Parse(tempReader["DEPTH_LV"].ToString()) == 4)
                                    addressList.Add(tempReader["LAND_RI_NM"].ToString());
                            }
                        }

                        addressList.Add(reader["BNBUN"].ToString()+ "-"+ reader["BUBUN"].ToString());
                    }
                }
                catch (OracleException ex)
                {
                    MessageBox.Show(ex.ToString());
                }
            }



            string address = "";

            foreach(string i in addressList)
            {
                address += i;
                address += " ";
            }

            return address;
        }

        public static bool checkDesignMasterPresence (List<string> idColumnName, List<string> idColumnCode)
        {
            string readSql = "select * FROM TD_DESIGN_MASTER";

            if (idColumnName.Count() != 0)
                readSql += " WHERE";

            for (int i = 0; i < idColumnName.Count(); i++)
            {
                if (i != 0)
                    readSql += " AND";

                readSql += " " + idColumnName[i] + "=" + idColumnCode[i] + " ";
            }

            using (OracleConnection connection = new OracleConnection(Consts.connectionString))
            {
                try
                {
                    connection.Open();

                    OracleCommand tempCommand = new OracleCommand(readSql, connection);
                    OracleDataReader tempReader = tempCommand.ExecuteReader();

                    bool output =  tempReader.Read();

                    tempCommand.Dispose();

                    return output;
                }
                catch(Exception ex)
                {
                    MessageBox.Show(ex.ToString());

                    return false;
                }
            }
        }

        public static void AddTdDesignMaster(List<string> idColumnName, List<string> idColumnCode, string userID, int GROSS_AREA_RATIO_REG, int BUILDING_COVERAGE_REG, int STOREIS_REG, string PLOT_BOUNDARY_VECTOR, string PLOT_SURROUNDINGS_VECTOR, double PLOTAREA_PLAN)
        {
            //20160516 확인 및 수정완료, 추후 건축선 후퇴 구현 후 plotarea_Excluded에 입력


            string sql = "INSERT INTO TD_DESIGN_MASTER(REGI_MST_NO,REGI_SUB_MST_NO,PROJECT_NAME,PLOT_ADDRESS,PLOT_TYPE_CD,PLOT_BOUNDARY_VECTOR,PLOT_SURROUNDINGS_VECTOR,PLOTAREA_MANUAL,PLOTAREA_EXCLUDED,PLOTAREA_PLAN,GROSS_AREA_RATIO_REG,BUILDING_COVERAGE_REG,STOREIS_REG,FRST_REGIST_DT,FRST_REGISTER_ID) VALUES(:p_REGI_MST_NO,:p_REGI_SUB_MST_NO,:p_PROJECT_NAME,:p_PLOT_ADDRESS,:p_PLOT_TYPE_CD,:p_PLOT_BOUNDARY_VECTOR,:p_PLOT_SURROUNDINGS_VECTOR,:p_PLOTAREA_MANUAL,:p_PLOTAREA_EXCLUDED,:p_PLOTAREA_PLAN,:p_GROSS_AREA_RATIO_REG,:p_BUILDING_COVERAGE_REG,:p_STOREIS_REG,SYSDATE,:p_FRST_REGISTER_ID)";

            using (OracleConnection connection = new OracleConnection(Consts.connectionString))
            {
                try
                {
                    connection.Open();

                    OracleCommand comm = new OracleCommand(sql, connection);

                    OracleParameter p_REGI_MST_NO = new OracleParameter();
                    OracleParameter p_REGI_SUB_MST_NO = new OracleParameter();
                    OracleParameter p_PROJECT_NAME = new OracleParameter();
                    OracleParameter p_PLOT_ADDRESS = new OracleParameter();
                    OracleParameter p_PLOT_TYPE_CD = new OracleParameter();
                    OracleParameter p_PLOT_BOUNDARY_VECTOR = new OracleParameter();
                    OracleParameter p_PLOT_SURROUNDINGS_VECTOR = new OracleParameter();
                    OracleParameter p_PLOTAREA_MANUAL = new OracleParameter();
                    OracleParameter p_PLOTAREA_EXCLUDED = new OracleParameter();
                    OracleParameter p_PLOTAREA_PLAN = new OracleParameter();
                    OracleParameter p_GROSS_AREA_RATIO_REG = new OracleParameter();
                    OracleParameter p_BUILDING_COVERAGE_REG = new OracleParameter();
                    OracleParameter p_STOREIS_REG = new OracleParameter();
                    OracleParameter p_FRST_REGIST_DT = new OracleParameter();
                    OracleParameter p_FRST_REGISTER_ID = new OracleParameter();

                    p_REGI_MST_NO.DbType = System.Data.DbType.String;
                    p_REGI_MST_NO.Value = idColumnCode[0].ToString();
                    p_REGI_MST_NO.ParameterName = "p_REGI_MST_NO";

                    p_REGI_SUB_MST_NO.DbType = System.Data.DbType.String;
                    p_REGI_SUB_MST_NO.Value = idColumnCode[1].ToString();
                    p_REGI_SUB_MST_NO.ParameterName = "p_REGI_SUB_MST_NO";

                    p_PROJECT_NAME.DbType = System.Data.DbType.String; 
                    p_PROJECT_NAME.Value = GetProjectNameFromServer(idColumnName, idColumnCode);
                    p_PROJECT_NAME.ParameterName = "p_PROJECT_NAME";

                    p_PLOT_ADDRESS.DbType = System.Data.DbType.String;
                    p_PLOT_ADDRESS.Value = GetAddressFromServer(idColumnName, idColumnCode);
                    p_PLOT_ADDRESS.ParameterName = "p_PLOT_ADDRESS";

                    p_PLOT_TYPE_CD.DbType = System.Data.DbType.String;
                    p_PLOT_TYPE_CD.Value = GetPlotTypeFromServer(idColumnName, idColumnCode);
                    p_PLOT_TYPE_CD.ParameterName = "p_PLOT_TYPE_CD";

                    p_PLOT_BOUNDARY_VECTOR.DbType = System.Data.DbType.String;
                    p_PLOT_BOUNDARY_VECTOR.Value = PLOT_BOUNDARY_VECTOR;
                    p_PLOT_BOUNDARY_VECTOR.ParameterName = "p_PLOT_BOUNDARY_VECTOR";

                    p_PLOT_SURROUNDINGS_VECTOR.DbType = System.Data.DbType.String;
                    p_PLOT_SURROUNDINGS_VECTOR.Value = PLOT_SURROUNDINGS_VECTOR;
                    p_PLOT_SURROUNDINGS_VECTOR.ParameterName = "p_PLOT_SURROUNDINGS_VECTOR";

                    p_PLOTAREA_MANUAL.DbType = System.Data.DbType.Decimal;
                    p_PLOTAREA_MANUAL.Value = Math.Round(GetManualAreaFromServer(idColumnName, idColumnCode), 2).ToString();
                    p_PLOTAREA_MANUAL.ParameterName = "p_PLOTAREA_MANUAL";

                    p_PLOTAREA_EXCLUDED.DbType = System.Data.DbType.Decimal;          /////////////////////////////////////////////////////////////
                    p_PLOTAREA_EXCLUDED.Value = "0";
                    p_PLOTAREA_EXCLUDED.ParameterName = "p_PLOTAREA_EXCLUDED";

                    p_PLOTAREA_PLAN.DbType = System.Data.DbType.Decimal;
                    p_PLOTAREA_PLAN.Value = Math.Round(PLOTAREA_PLAN / 1000000, 2).ToString() ;
                    p_PLOTAREA_PLAN.ParameterName = "p_PLOTAREA_PLAN";

                    p_GROSS_AREA_RATIO_REG.DbType = System.Data.DbType.Decimal;
                    p_GROSS_AREA_RATIO_REG.Value = GROSS_AREA_RATIO_REG.ToString();
                    p_GROSS_AREA_RATIO_REG.ParameterName = "p_GROSS_AREA_RATIO_REG";

                    p_BUILDING_COVERAGE_REG.DbType = System.Data.DbType.Decimal;
                    p_BUILDING_COVERAGE_REG.Value = BUILDING_COVERAGE_REG.ToString();
                    p_BUILDING_COVERAGE_REG.ParameterName = "p_BUILDING_COVERAGE_REG";

                    p_STOREIS_REG.DbType = System.Data.DbType.Decimal;
                    p_STOREIS_REG.Value = STOREIS_REG.ToString();
                    p_STOREIS_REG.ParameterName = "p_STOREIS_REG";

                    p_FRST_REGISTER_ID.DbType = System.Data.DbType.String;
                    p_FRST_REGISTER_ID.Value = userID;
                    p_FRST_REGISTER_ID.ParameterName = "p_FRST_REGISTER_ID";

                    comm.Parameters.Add(p_REGI_MST_NO);
                    comm.Parameters.Add(p_REGI_SUB_MST_NO);

                    comm.Parameters.Add(p_PROJECT_NAME);
                    comm.Parameters.Add(p_PLOT_ADDRESS);
                    
                    comm.Parameters.Add(p_PLOT_TYPE_CD);
                    comm.Parameters.Add(p_PLOT_BOUNDARY_VECTOR);
                    comm.Parameters.Add(p_PLOT_SURROUNDINGS_VECTOR);
                    comm.Parameters.Add(p_PLOTAREA_MANUAL);
                    comm.Parameters.Add(p_PLOTAREA_EXCLUDED);
                    comm.Parameters.Add(p_PLOTAREA_PLAN);
                    comm.Parameters.Add(p_GROSS_AREA_RATIO_REG);
                    comm.Parameters.Add(p_BUILDING_COVERAGE_REG);
                    comm.Parameters.Add(p_STOREIS_REG);
                    
                    comm.Parameters.Add(p_FRST_REGISTER_ID);

                    comm.ExecuteNonQuery();

                    comm.Dispose();
                }
                catch (OracleException ex)
                {
                    MessageBox.Show(ex.ToString());
                }
            }
        }

        public static int GetLastPlanTypeNo(List<string> idColumnName, List<string> idColumnCode, int REGI_PRE_DESIGN_NO)
        {
            List<string> idColumnNameCopy = new List<string>(idColumnName);
            List<string> idColumnCodeCopy = new List<string>(idColumnCode);

            idColumnCodeCopy.Add("REGI_PRE_DESIGN_NO");
            idColumnCodeCopy.Add(REGI_PRE_DESIGN_NO.ToString());

            const string connectionString = "Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=oracle3.ejudata.co.kr)(PORT=1521))(CONNECT_DATA=(SERVER=DEDICATED)(SID = oracle3)));User Id=whruser_sh;Password=ejudata;";

            List<int> PLAN_TYPE_NO_INDEX = new List<int>();

            string readSql = "select * FROM TD_DESIGN_AREA";

            if (idColumnName.Count() != 0)
                readSql += " WHERE";

            for (int i = 0; i < idColumnNameCopy.Count(); i++)
            {
                if (i != 0)
                    readSql += " AND";

                readSql += " " + idColumnNameCopy[i] + "=" + idColumnCodeCopy[i] + " ";
            }

            using (OracleConnection connection = new OracleConnection(connectionString))
            {
                try
                {
                    connection.Open();

                    OracleCommand readComm = new OracleCommand(readSql, connection);
                    OracleDataReader reader = readComm.ExecuteReader();

                    while (reader.Read())
                    {
                        PLAN_TYPE_NO_INDEX.Add(int.Parse(reader["REGI_PLAN_TYPE_NO"].ToString()));
                    }
                }
                catch (OracleException ex)
                {
                    MessageBox.Show(ex.ToString());
                }
            }

            PLAN_TYPE_NO_INDEX.Sort();

            if (PLAN_TYPE_NO_INDEX.Count() != 0)
                return PLAN_TYPE_NO_INDEX[PLAN_TYPE_NO_INDEX.Count() - 1];
            else
                return -1;
        }

        public static string getDirection(Vector3d yDirection)
        {
            Vector3d yDirectionCopy = new Vector3d(yDirection);
            yDirectionCopy.Reverse();

            string[] directionString = { "EE", "NE", "NN", "NW", "WW", "SW", "SS", "SE" };

            double angle = CommonFunc.vectorAngle(yDirectionCopy);

            return directionString[(int)(((angle + Math.PI / 8) - (angle + Math.PI / 8) % (Math.PI / 4)) / (Math.PI / 4))];
        }

        public static void AddTdDesignArea(List<string> idColumnName, List<string> idColumnCode, int REGI_PRE_DESIGN_NO, string userID, string typeString, double CORE_AREA,double WELFARE_AREA, double FACILITIES_AREA, double PARKINGLOT_AREA,double PLOT_SHARE_AREA, HouseholdStatistics houseHoldStatistic)
        {
            string sql = "INSERT INTO TD_DESIGN_AREA(REGI_MST_NO,REGI_SUB_MST_NO,REGI_PRE_DESIGN_NO,REGI_PLAN_TYPE_NO,TYPE_SIZE,TYPE_STRING,TYPE_DIRECTION,TYPE_COUNT,TYPE_EXCLUSIVE_AREA,TYPE_WALL_AREA,TYPE_CORE_AREA,TYPE_COMMON_USE_AREA,TYPE_SUPPLY_AREA,TYPE_WELFARE_AREA,TYPE_FACILITIES_AREA,TYPE_PARKINGLOT_AREA,TYPE_OTHER_COMMON_USE_AREA,TYPE_CONTRACT_AREA,TYPE_BALCONY_AREA,TYPE_USABLE_AREA,TYPE_PLOT_SHARE_AREA,UNIT_PLA_DRWN_VECTOR,FRST_REGIST_DT,FRST_REGISTER_ID) VALUES(:p_REGI_MST_NO,:p_REGI_SUB_MST_NO,:p_REGI_PRE_DESIGN_NO,:p_REGI_PLAN_TYPE_NO,:p_TYPE_SIZE,:p_TYPE_STRING,:p_TYPE_DIRECTION,:p_TYPE_COUNT,:p_TYPE_EXCLUSIVE_AREA,:p_TYPE_WALL_AREA,:p_TYPE_CORE_AREA,:p_TYPE_COMMON_USE_AREA,:p_TYPE_SUPPLY_AREA,:p_TYPE_WELFARE_AREA,:p_TYPE_FACILITIES_AREA,:p_TYPE_PARKINGLOT_AREA,:p_TYPE_OTHER_COMMON_USE_AREA,:p_TYPE_CONTRACT_AREA,:p_TYPE_BALCONY_AREA,:p_TYPE_USABLE_AREA,:p_TYPE_PLOT_SHARE_AREA,:p_UNIT_PLA_DRWN_VECTOR,SYSDATE,:p_FRST_REGISTER_ID)";

            using (OracleConnection connection = new OracleConnection(Consts.connectionString))
            {
                try
                {
                    connection.Open();

                    OracleCommand comm = new OracleCommand(sql, connection);

                    OracleParameter p_REGI_MST_NO = new OracleParameter();
                    OracleParameter p_REGI_SUB_MST_NO = new OracleParameter();
                    OracleParameter p_REGI_PRE_DESIGN_NO = new OracleParameter();
                    OracleParameter p_REGI_PLAN_TYPE_NO = new OracleParameter();
                    OracleParameter p_TYPE_SIZE = new OracleParameter();
                    OracleParameter p_TYPE_STRING = new OracleParameter();
                    OracleParameter p_TYPE_DIRECTION = new OracleParameter();
                    OracleParameter p_TYPE_COUNT = new OracleParameter();
                    OracleParameter p_TYPE_EXCLUSIVE_AREA = new OracleParameter();
                    OracleParameter p_TYPE_WALL_AREA = new OracleParameter();
                    OracleParameter p_TYPE_CORE_AREA = new OracleParameter();
                    OracleParameter p_TYPE_COMMON_USE_AREA = new OracleParameter();
                    OracleParameter p_TYPE_SUPPLY_AREA = new OracleParameter();
                    OracleParameter p_TYPE_WELFARE_AREA = new OracleParameter();
                    OracleParameter p_TYPE_FACILITIES_AREA = new OracleParameter();
                    OracleParameter p_TYPE_PARKINGLOT_AREA = new OracleParameter();
                    OracleParameter p_TYPE_OTHER_COMMON_USE_AREA = new OracleParameter();
                    OracleParameter p_TYPE_CONTRACT_AREA = new OracleParameter();
                    OracleParameter p_TYPE_BALCONY_AREA = new OracleParameter();
                    OracleParameter p_TYPE_USABLE_AREA = new OracleParameter();
                    OracleParameter p_TYPE_PLOT_SHARE_AREA = new OracleParameter();
                    OracleParameter p_UNIT_PLA_DRWN_VECTOR = new OracleParameter();
                    OracleParameter p_FRST_REGIST_DT = new OracleParameter();
                    OracleParameter p_FRST_REGISTER_ID = new OracleParameter();

                    p_REGI_MST_NO.DbType = System.Data.DbType.String;
                    p_REGI_MST_NO.Value = idColumnCode[0].ToString();
                    p_REGI_MST_NO.ParameterName = "p_REGI_MST_NO";

                    p_REGI_SUB_MST_NO.DbType = System.Data.DbType.String;
                    p_REGI_SUB_MST_NO.Value = idColumnCode[1].ToString();
                    p_REGI_SUB_MST_NO.ParameterName = "p_REGI_SUB_MST_NO";

                    p_REGI_PRE_DESIGN_NO.DbType = System.Data.DbType.Decimal;
                    p_REGI_PRE_DESIGN_NO.Value = REGI_PRE_DESIGN_NO.ToString();
                    p_REGI_PRE_DESIGN_NO.ParameterName = "p_REGI_PRE_DESIGN_NO";

                    p_REGI_PLAN_TYPE_NO.DbType = System.Data.DbType.Decimal;
                    p_REGI_PLAN_TYPE_NO.Value = (GetLastPlanTypeNo(idColumnName, idColumnCode, REGI_PRE_DESIGN_NO) + 1).ToString();
                    p_REGI_PLAN_TYPE_NO.ParameterName = "p_REGI_PLAN_TYPE_NO";

                    p_TYPE_SIZE.DbType = System.Data.DbType.Decimal;
                    p_TYPE_SIZE.Value = Math.Round(houseHoldStatistic.GetExclusiveArea() / 3.3 / 1000000, 0).ToString();
                    p_TYPE_SIZE.ParameterName = "p_TYPE_SIZE";

                    p_TYPE_STRING.DbType = System.Data.DbType.String;
                    p_TYPE_STRING.Value = typeString;
                    p_TYPE_STRING.ParameterName = "p_TYPE_STRING";

                    p_TYPE_DIRECTION.DbType = System.Data.DbType.String;
                    p_TYPE_DIRECTION.Value = getDirection(houseHoldStatistic.YDirection);
                    p_TYPE_DIRECTION.ParameterName = "p_TYPE_DIRECTION";

                    p_TYPE_COUNT.DbType = System.Data.DbType.Decimal;
                    p_TYPE_COUNT.Value = houseHoldStatistic.Count.ToString();
                    p_TYPE_COUNT.ParameterName = "p_TYPE_COUNT";

                    p_TYPE_EXCLUSIVE_AREA.DbType = System.Data.DbType.Decimal;
                    p_TYPE_EXCLUSIVE_AREA.Value = Math.Round(houseHoldStatistic.GetExclusiveArea() / 1000000 , 2).ToString();
                    p_TYPE_EXCLUSIVE_AREA.ParameterName = "p_TYPE_EXCLUSIVE_AREA";

                    p_TYPE_WALL_AREA.DbType = System.Data.DbType.Decimal;
                    p_TYPE_WALL_AREA.Value = Math.Round(houseHoldStatistic.GetWallArea() / 1000000, 2).ToString();
                    p_TYPE_WALL_AREA.ParameterName = "p_TYPE_WALL_AREA";

                    p_TYPE_CORE_AREA.DbType = System.Data.DbType.Decimal;
                    p_TYPE_CORE_AREA.Value = Math.Round(CORE_AREA / 1000000, 2).ToString();
                    p_TYPE_CORE_AREA.ParameterName = "p_TYPE_CORE_AREA";

                    p_TYPE_COMMON_USE_AREA.DbType = System.Data.DbType.Decimal;
                    p_TYPE_COMMON_USE_AREA.Value = Math.Round((CORE_AREA + houseHoldStatistic.GetWallArea()) / 1000000, 2).ToString();
                    p_TYPE_COMMON_USE_AREA.ParameterName = "p_TYPE_COMMON_USE_AREA";

                    p_TYPE_SUPPLY_AREA.DbType = System.Data.DbType.Decimal;
                    p_TYPE_SUPPLY_AREA.Value = Math.Round((CORE_AREA + houseHoldStatistic.GetWallArea() + houseHoldStatistic.GetExclusiveArea()) / 1000000, 2).ToString();
                    p_TYPE_SUPPLY_AREA.ParameterName = "p_TYPE_SUPPLY_AREA";

                    p_TYPE_WELFARE_AREA.DbType = System.Data.DbType.Decimal;
                    p_TYPE_WELFARE_AREA.Value = WELFARE_AREA;
                    p_TYPE_WELFARE_AREA.ParameterName = "p_TYPE_WELFARE_AREA";

                    p_TYPE_FACILITIES_AREA.DbType = System.Data.DbType.Decimal;
                    p_TYPE_FACILITIES_AREA.Value = FACILITIES_AREA;
                    p_TYPE_FACILITIES_AREA.ParameterName = "p_TYPE_FACILITIES_AREA";

                    p_TYPE_PARKINGLOT_AREA.DbType = System.Data.DbType.Decimal;
                    p_TYPE_PARKINGLOT_AREA.Value = Math.Round(PARKINGLOT_AREA /1000000, 2).ToString();
                    p_TYPE_PARKINGLOT_AREA.ParameterName = "p_TYPE_PARKINGLOT_AREA";

                    p_TYPE_OTHER_COMMON_USE_AREA.DbType = System.Data.DbType.Decimal;
                    p_TYPE_OTHER_COMMON_USE_AREA.Value = Math.Round((WELFARE_AREA + FACILITIES_AREA + PARKINGLOT_AREA) / 1000000, 2).ToString();
                    p_TYPE_OTHER_COMMON_USE_AREA.ParameterName = "p_TYPE_OTHER_COMMON_USE_AREA";

                    p_TYPE_CONTRACT_AREA.DbType = System.Data.DbType.Decimal;
                    p_TYPE_CONTRACT_AREA.Value = Math.Round((houseHoldStatistic.GetExclusiveArea() + houseHoldStatistic.GetWallArea() + CORE_AREA + WELFARE_AREA + FACILITIES_AREA + PARKINGLOT_AREA) / 1000000, 2).ToString();
                    p_TYPE_CONTRACT_AREA.ParameterName = "p_TYPE_CONTRACT_AREA";

                    p_TYPE_BALCONY_AREA.DbType = System.Data.DbType.Decimal;
                    p_TYPE_BALCONY_AREA.Value = Math.Round(houseHoldStatistic.GetBalconyArea() / 1000000, 2).ToString();
                    p_TYPE_BALCONY_AREA.ParameterName = "p_TYPE_BALCONY_AREA";

                    p_TYPE_USABLE_AREA.DbType = System.Data.DbType.Decimal;
                    p_TYPE_USABLE_AREA.Value = Math.Round((houseHoldStatistic.GetExclusiveArea() + houseHoldStatistic.GetBalconyArea()) / 1000000, 2).ToString();
                    p_TYPE_USABLE_AREA.ParameterName = "p_TYPE_USABLE_AREA";

                    p_TYPE_PLOT_SHARE_AREA.DbType = System.Data.DbType.Decimal;
                    p_TYPE_PLOT_SHARE_AREA.Value = Math.Round(PLOT_SHARE_AREA / 1000000, 2).ToString();
                    p_TYPE_PLOT_SHARE_AREA.ParameterName = "p_TYPE_PLOT_SHARE_AREA";

                    p_UNIT_PLA_DRWN_VECTOR.DbType = System.Data.DbType.String; ////////////////////////////////////
                    p_UNIT_PLA_DRWN_VECTOR.Value = "";
                    p_UNIT_PLA_DRWN_VECTOR.ParameterName = "p_UNIT_PLA_DRWN_VECTOR";

                    p_FRST_REGISTER_ID.DbType = System.Data.DbType.String;
                    p_FRST_REGISTER_ID.Value = userID;
                    p_FRST_REGISTER_ID.ParameterName = "p_FRST_REGISTER_ID";

                    comm.Parameters.Add(p_REGI_MST_NO);
                    comm.Parameters.Add(p_REGI_SUB_MST_NO);
                    comm.Parameters.Add(p_REGI_PRE_DESIGN_NO);
                    comm.Parameters.Add(p_REGI_PLAN_TYPE_NO);
                    comm.Parameters.Add(p_TYPE_SIZE);
                    comm.Parameters.Add(p_TYPE_STRING);
                    comm.Parameters.Add(p_TYPE_DIRECTION);
                    comm.Parameters.Add(p_TYPE_COUNT);
                    comm.Parameters.Add(p_TYPE_EXCLUSIVE_AREA);
                    comm.Parameters.Add(p_TYPE_WALL_AREA);
                    comm.Parameters.Add(p_TYPE_CORE_AREA);
                    comm.Parameters.Add(p_TYPE_COMMON_USE_AREA);
                    comm.Parameters.Add(p_TYPE_SUPPLY_AREA);
                    comm.Parameters.Add(p_TYPE_WELFARE_AREA);
                    comm.Parameters.Add(p_TYPE_FACILITIES_AREA);
                    comm.Parameters.Add(p_TYPE_PARKINGLOT_AREA);
                    comm.Parameters.Add(p_TYPE_OTHER_COMMON_USE_AREA);
                    comm.Parameters.Add(p_TYPE_CONTRACT_AREA);
                    comm.Parameters.Add(p_TYPE_BALCONY_AREA);
                    comm.Parameters.Add(p_TYPE_USABLE_AREA);
                    comm.Parameters.Add(p_TYPE_PLOT_SHARE_AREA);
                    comm.Parameters.Add(p_UNIT_PLA_DRWN_VECTOR);
                    comm.Parameters.Add(p_FRST_REGISTER_ID);

                    comm.ExecuteNonQuery();
                    comm.Dispose();
                }
                catch (OracleException ex)
                {
                    MessageBox.Show(ex.ToString());
                }
            }
        }

        public static string GetBuildingTypeCode(int maxStories)
        {
            if (maxStories < 5)
                return "1300";
            else
                return "2100";
        }

        public static string GetBuildingScale(int maxStories, int underGroundStories)
        {
            if (underGroundStories > 0)
                return "지하 " + underGroundStories.ToString() + "층, " + "지상 " + maxStories.ToString() + "층";
            else
                return "지상 " + maxStories.ToString() + "층";
        }

        public static void AddDesignDetail(List<string> idColumnName, List<string> idColumnCode, string userID, ApartmentGeneratorOutput agOutput, out int REGI_PRE_DESIGN_NO)
        {
            //////20160516_수정완료, 지하주차장 데이터 입력할 필요가 있음

            string sql = "INSERT INTO TD_DESIGN_DETAIL(REGI_MST_NO,REGI_SUB_MST_NO,REGI_PRE_DESIGN_NO, DESIGN_FXD_AT ,BUILDING_TYPE_CD,BUILDING_SCALE,BUILDING_STRUCTURE,BUILDING_AREA,FLOOR_AREA_UG,FLOOR_AREA_G,FLOOR_AREA_WHOLE,STORIES_UNDERGROUND,STORIES_ON_EARTH,BALCONY_AREA,PARKING_ROOFTOP_AREA,FLOOR_AREA_CONSTRUCTION,BUILDING_COVERAGE,GROSS_AREA_RATIO,HOUSEHOLD_COUNT,PARKINGLOT_COUNT,PARKINGLOT_AREA,PARKINGLOT_COUNT_LEGAL,LANDSCAPE_AREA,LANDSCAPE_AREA_LEGAL,NEIGHBOR_STORE_AREA,PUBLIC_FACILITY_AREA,FRST_REGIST_DT,FRST_REGISTER_ID) VALUES(:p_REGI_MST_NO,:p_REGI_SUB_MST_NO,:p_REGI_PRE_DESIGN_NO,:p_DESIGN_FXD_AT,:p_BUILDING_TYPE_CD,:p_BUILDING_SCALE,:p_STRUCTURE,:p_BUILDING_AREA,:p_FLOOR_AREA_UG,:p_FLOOR_AREA_G,:p_FLOOR_AREA_WHOLE,:p_STORIES_UNDERGROUND,:p_STORIES_ON_EARTH,:p_BALCONY_AREA,:p_PARKING_ROOFTOP_AREA,:p_FLOOR_AREA_CONSTRUCTION,:p_BUILDING_COVERAGE,:p_GROSS_AREA_RATIO,:p_HOUSEHOLD_COUNT,:p_PARKINGLOT_COUNT,:p_PARKINGLOT_AREA,:p_PARKINGLOT_COUNT_LEGAL,:p_LANDSCAPE_AREA,:p_LANDSCAPE_AREA_LEGAL,:p_NEIGHBOR_STORE_AREA,:p_PUBLIC_FACILITY_AREA,SYSDATE ,:p_FRST_REGISTER_ID)";

            REGI_PRE_DESIGN_NO = GetLastPreDesignNo(idColumnName, idColumnCode) + 1;

            using (OracleConnection connection = new OracleConnection(Consts.connectionString))
            {
                try
                {
                    connection.Open();

                    OracleCommand comm = new OracleCommand(sql, connection);

                    OracleParameter p_REGI_MST_NO = new OracleParameter();
                    OracleParameter p_REGI_SUB_MST_NO = new OracleParameter();
                    OracleParameter p_REGI_PRE_DESIGN_NO = new OracleParameter();
                    OracleParameter p_DESIGN_FXD_AT = new OracleParameter();
                    OracleParameter p_BUILDING_TYPE_CD = new OracleParameter();
                    OracleParameter p_BUILDING_SCALE = new OracleParameter();
                    OracleParameter p_STRUCTURE = new OracleParameter();
                    OracleParameter p_BUILDING_AREA = new OracleParameter();
                    OracleParameter p_FLOOR_AREA_UG = new OracleParameter();
                    OracleParameter p_FLOOR_AREA_G = new OracleParameter();
                    OracleParameter p_FLOOR_AREA_WHOLE = new OracleParameter();
                    OracleParameter p_STORIES_UNDERGROUND = new OracleParameter();
                    OracleParameter p_STORIES_ON_EARTH = new OracleParameter();
                    OracleParameter p_BALCONY_AREA = new OracleParameter();
                    OracleParameter p_PARKING_ROOFTOP_AREA = new OracleParameter();
                    OracleParameter p_FLOOR_AREA_CONSTRUCTION = new OracleParameter();
                    OracleParameter p_BUILDING_COVERAGE = new OracleParameter();
                    OracleParameter p_GROSS_AREA_RATIO = new OracleParameter();
                    OracleParameter p_HOUSEHOLD_COUNT = new OracleParameter();
                    OracleParameter p_PARKINGLOT_COUNT = new OracleParameter();
                    OracleParameter p_PARKINGLOT_AREA = new OracleParameter();
                    OracleParameter p_PARKINGLOT_COUNT_LEGAL = new OracleParameter();
                    OracleParameter p_LANDSCAPE_AREA = new OracleParameter();
                    OracleParameter p_LANDSCAPE_AREA_LEGAL = new OracleParameter();
                    OracleParameter p_NEIGHBOR_STORE_AREA = new OracleParameter();
                    OracleParameter p_PUBLIC_FACILITY_AREA = new OracleParameter();
                    OracleParameter p_FRST_REGIST_DT = new OracleParameter();
                    OracleParameter p_FRST_REGISTER_ID = new OracleParameter();

                    p_REGI_MST_NO.DbType = System.Data.DbType.String;
                    p_REGI_MST_NO.Value = idColumnCode[0].ToString();
                    p_REGI_MST_NO.ParameterName = "p_REGI_MST_NO";

                    p_REGI_SUB_MST_NO.DbType = System.Data.DbType.String;
                    p_REGI_SUB_MST_NO.Value = idColumnCode[1].ToString();
                    p_REGI_SUB_MST_NO.ParameterName = "p_REGI_SUB_MST_NO";

                    p_REGI_PRE_DESIGN_NO.DbType = System.Data.DbType.Decimal;
                    p_REGI_PRE_DESIGN_NO.Value = (GetLastPreDesignNo(idColumnName, idColumnCode) + 1).ToString();
                    p_REGI_PRE_DESIGN_NO.ParameterName = "p_REGI_PRE_DESIGN_NO";

                    p_DESIGN_FXD_AT.DbType = System.Data.DbType.String;
                    p_DESIGN_FXD_AT.Value = "0";
                    p_DESIGN_FXD_AT.ParameterName = "p_DESIGN_FXD_AT";

                    p_BUILDING_TYPE_CD.DbType = System.Data.DbType.String;
                    p_BUILDING_TYPE_CD.Value = GetBuildingTypeCode(Math.Max((int)agOutput.ParameterSet.Parameters[0], (int)agOutput.ParameterSet.Parameters[1]));
                    p_BUILDING_TYPE_CD.ParameterName = "p_BUILDING_TYPE_CD";

                    p_BUILDING_SCALE.DbType = System.Data.DbType.String;
                    p_BUILDING_SCALE.Value = GetBuildingScale(Math.Max((int)agOutput.ParameterSet.Parameters[0], (int)agOutput.ParameterSet.Parameters[1]), 0);  ////////////지하층 층수 입력
                    p_BUILDING_SCALE.ParameterName = "p_BUILDING_SCALE";

                    p_STRUCTURE.DbType = System.Data.DbType.String;
                    p_STRUCTURE.Value = "철근콘크리트 구조";
                    p_STRUCTURE.ParameterName = "p_STRUCTURE";

                    p_BUILDING_AREA.DbType = System.Data.DbType.String;
                    p_BUILDING_AREA.Value = Math.Round(agOutput.GetBuildingArea() / 1000000, 2).ToString();
                    p_BUILDING_AREA.ParameterName = "p_BUILDING_AREA";

                    p_FLOOR_AREA_UG.DbType = System.Data.DbType.Decimal;                                        ////////////////////////////////////// 지하층 연면적
                    p_FLOOR_AREA_UG.Value = "0";
                    p_FLOOR_AREA_UG.ParameterName = "p_FLOOR_AREA_UG";

                    p_FLOOR_AREA_G.DbType = System.Data.DbType.Decimal;
                    p_FLOOR_AREA_G.Value = Math.Round(agOutput.GetGrossArea() / 1000000, 2).ToString();
                    p_FLOOR_AREA_G.ParameterName = "p_FLOOR_AREA_G";

                    p_FLOOR_AREA_WHOLE.DbType = System.Data.DbType.Decimal;
                    p_FLOOR_AREA_WHOLE.Value = Math.Round(agOutput.GetGrossArea() / 1000000, 2).ToString();
                    p_FLOOR_AREA_WHOLE.ParameterName = "p_FLOOR_AREA_WHOLE";

                    p_STORIES_UNDERGROUND.DbType = System.Data.DbType.Decimal;                                      ////////////////////////////////////// 지하층 층수 입력
                    p_STORIES_UNDERGROUND.Value = "0";
                    p_STORIES_UNDERGROUND.ParameterName = "p_STORIES_UNDERGROUND";

                    p_STORIES_ON_EARTH.DbType = System.Data.DbType.Decimal;
                    p_STORIES_ON_EARTH.Value = (Math.Max((int)agOutput.ParameterSet.Parameters[0], (int)agOutput.ParameterSet.Parameters[1])+1).ToString();
                    p_STORIES_ON_EARTH.ParameterName = "p_STORIES_ON_EARTH";

                    p_BALCONY_AREA.DbType = System.Data.DbType.Decimal;
                    p_BALCONY_AREA.Value = Math.Round(agOutput.GetBalconyArea() / 1000000, 2).ToString();
                    p_BALCONY_AREA.ParameterName = "p_BALCONY_AREA";

                    p_PARKING_ROOFTOP_AREA.DbType = System.Data.DbType.Decimal;
                    p_PARKING_ROOFTOP_AREA.Value = Math.Round((agOutput.GetRooftopCoreArea() + agOutput.ParkingLotUnderGround.GetAreaSum()) / 1000000, 2).ToString();
                    p_PARKING_ROOFTOP_AREA.ParameterName = "p_PARKING_ROOFTOP_AREA";

                    p_FLOOR_AREA_CONSTRUCTION.DbType = System.Data.DbType.Decimal;
                    p_FLOOR_AREA_CONSTRUCTION.Value = Math.Round((agOutput.GetGrossArea() + agOutput.GetBalconyArea() + agOutput.GetRooftopCoreArea() + agOutput.ParkingLotUnderGround.GetAreaSum()) / 1000000, 2).ToString();
                    p_FLOOR_AREA_CONSTRUCTION.ParameterName = "p_FLOOR_AREA_CONSTRUCTION";

                    p_BUILDING_COVERAGE.DbType = System.Data.DbType.Decimal;
                    p_BUILDING_COVERAGE.Value = Math.Round(agOutput.GetBuildingCoverage(),2).ToString();
                    p_BUILDING_COVERAGE.ParameterName = "p_BUILDING_COVERAGE";

                    p_GROSS_AREA_RATIO.DbType = System.Data.DbType.Decimal;
                    p_GROSS_AREA_RATIO.Value = Math.Round(agOutput.GetGrossAreaRatio(),2).ToString();
                    p_GROSS_AREA_RATIO.ParameterName = "p_GROSS_AREA_RATIO";

                    p_HOUSEHOLD_COUNT.DbType = System.Data.DbType.Decimal;
                    p_HOUSEHOLD_COUNT.Value = agOutput.GetHouseholdCount().ToString();
                    p_HOUSEHOLD_COUNT.ParameterName = "p_HOUSEHOLD_COUNT";

                    p_PARKINGLOT_COUNT.DbType = System.Data.DbType.Decimal;
                    p_PARKINGLOT_COUNT.Value = (agOutput.ParkingLotOnEarth.GetCount() + agOutput.ParkingLotUnderGround.GetCount()).ToString();
                    p_PARKINGLOT_COUNT.ParameterName = "p_PARKINGLOT_COUNT";

                    p_PARKINGLOT_AREA.DbType = System.Data.DbType.Decimal;
                    p_PARKINGLOT_AREA.Value = Math.Round(agOutput.ParkingLotUnderGround.GetAreaSum()/1000000,2).ToString();
                    p_PARKINGLOT_AREA.ParameterName = "p_PARKINGLOT_AREA";

                    p_PARKINGLOT_COUNT_LEGAL.DbType = System.Data.DbType.Decimal;
                    p_PARKINGLOT_COUNT_LEGAL.Value = agOutput.GetLegalParkingLotNums().ToString();
                    p_PARKINGLOT_COUNT_LEGAL.ParameterName = "p_PARKINGLOT_COUNT_LEGAL";

                    p_LANDSCAPE_AREA.DbType = System.Data.DbType.Decimal;                             
                    p_LANDSCAPE_AREA.Value =Math.Round(agOutput.GetGreenArea() / 1000000 , 2).ToString();
                    p_LANDSCAPE_AREA.ParameterName = "p_LANDSCAPE_AREA";

                    p_LANDSCAPE_AREA_LEGAL.DbType = System.Data.DbType.Decimal;                                   
                    p_LANDSCAPE_AREA_LEGAL.Value =Math.Round( agOutput.CalculateLegalGreen() / 1000000 , 2).ToString();
                    p_LANDSCAPE_AREA_LEGAL.ParameterName = "p_LANDSCAPE_AREA_LEGAL";

                    p_NEIGHBOR_STORE_AREA.DbType = System.Data.DbType.Decimal;                                      
                    p_NEIGHBOR_STORE_AREA.Value = Math.Round(agOutput.GetCommercialArea() / 100000, 2).ToString();
                    p_NEIGHBOR_STORE_AREA.ParameterName = "p_NEIGHBOR_STORE_AREA";

                    p_PUBLIC_FACILITY_AREA.DbType = System.Data.DbType.Decimal;                                  
                    p_PUBLIC_FACILITY_AREA.Value = Math.Round(agOutput.GetPublicFacilityArea() / 100000, 2).ToString();
                    p_PUBLIC_FACILITY_AREA.ParameterName = "p_PUBLIC_FACILITY_AREA";

                    p_FRST_REGISTER_ID.DbType = System.Data.DbType.String;
                    p_FRST_REGISTER_ID.Value = userID;
                    p_FRST_REGISTER_ID.ParameterName = "p_FRST_REGISTER_ID";

                    comm.Parameters.Add(p_REGI_MST_NO);
                    comm.Parameters.Add(p_REGI_SUB_MST_NO);
                    comm.Parameters.Add(p_REGI_PRE_DESIGN_NO);
                    comm.Parameters.Add(p_DESIGN_FXD_AT);
                    comm.Parameters.Add(p_BUILDING_TYPE_CD);
                    comm.Parameters.Add(p_BUILDING_SCALE);
                    comm.Parameters.Add(p_STRUCTURE);
                    comm.Parameters.Add(p_BUILDING_AREA);
                    comm.Parameters.Add(p_FLOOR_AREA_UG);
                    comm.Parameters.Add(p_FLOOR_AREA_G);
                    comm.Parameters.Add(p_FLOOR_AREA_WHOLE);
                    comm.Parameters.Add(p_STORIES_UNDERGROUND);
                    comm.Parameters.Add(p_STORIES_ON_EARTH);
                    comm.Parameters.Add(p_BALCONY_AREA);
                    comm.Parameters.Add(p_PARKING_ROOFTOP_AREA);
                    comm.Parameters.Add(p_FLOOR_AREA_CONSTRUCTION);
                    comm.Parameters.Add(p_BUILDING_COVERAGE);
                    comm.Parameters.Add(p_GROSS_AREA_RATIO);
                    comm.Parameters.Add(p_HOUSEHOLD_COUNT);
                    comm.Parameters.Add(p_PARKINGLOT_COUNT);
                    comm.Parameters.Add(p_PARKINGLOT_AREA);
                    comm.Parameters.Add(p_PARKINGLOT_COUNT_LEGAL);
                    comm.Parameters.Add(p_LANDSCAPE_AREA);
                    comm.Parameters.Add(p_LANDSCAPE_AREA_LEGAL);
                    comm.Parameters.Add(p_NEIGHBOR_STORE_AREA);
                    comm.Parameters.Add(p_PUBLIC_FACILITY_AREA);
                    comm.Parameters.Add(p_FRST_REGISTER_ID);

                    comm.ExecuteNonQuery();
                    comm.Dispose();
                }
                catch (OracleException ex)
                {
                    MessageBox.Show(ex.ToString());
                }
            }
        }

        public static string ListToCSV(List<int> inputList)
        {
            string output = "";

            for (int i = 0; i < inputList.Count(); i++)
            {
                if (i != 0)
                    output += ",";

                output += inputList[i].ToString();
            }

            return output;
        }

        public static string ListToCSV(List<double> inputList)
        {
            string output = "";

            for (int i = 0; i < inputList.Count(); i++)
            {
                if (i != 0)
                    output += ",";

                output += inputList[i].ToString();
            }

            return output;
        }

        public static void AddDesignNonResidential(List<string> idColumnName, List<string> idColumnCode, string userID, int REGI_PRE_DESIGN_NO, string USE_CD, double NONRESI_EXCLUSIVE_AREA, double NONRESI_COMMON_USE_AREA, double NONRESI_PARKING_AREA, double NONRESI_LEGAL_PARKING, double NONRESI_PLOT_SHARE_AREA)
        {
            string sql = "INSERT INTO TD_DESIGN_NONRESIDENTIAL(REGI_MST_NO,REGI_SUB_MST_NO,REGI_PRE_DESIGN_NO,NONRESI_USE_CD,NONRESI_EXCLUSIVE_AREA,NONRESI_COMMON_USE_AREA,NONRESI_SUPPLY_AREA,NONRESI_PARKING_AREA,NONRESI_CONTRACT_AREA,NONRESI_LEGAL_PARKING,NONRESI_PLOT_SHARE_AREA,FRST_REGIST_DT,FRST_REGISTER_ID) VALUES(:p_REGI_MST_NO,:p_REGI_SUB_MST_NO,:p_REGI_PRE_DESIGN_NO,:p_NONRESI_USE_CD,:p_NONRESI_EXCLUSIVE_AREA,:p_NONRESI_COMMON_USE_AREA,:p_NONRESI_SUPPLY_AREA,:p_NONRESI_PARKING_AREA,:p_NONRESI_CONTRACT_AREA,:p_NONRESI_LEGAL_PARKING,:p_NONRESI_PLOT_SHARE_AREA,:p_FRST_REGIST_DT,:p_FRST_REGISTER_ID)";

            using (OracleConnection connection = new OracleConnection(Consts.connectionString))
            {
                try
                {
                    connection.Open();

                    OracleCommand comm = new OracleCommand(sql, connection);

                    OracleParameter p_REGI_MST_NO = new OracleParameter();
                    OracleParameter p_REGI_SUB_MST_NO = new OracleParameter();
                    OracleParameter p_REGI_PRE_DESIGN_NO = new OracleParameter();
                    OracleParameter p_NONRESI_USE_CD = new OracleParameter();
                    OracleParameter p_NONRESI_EXCLUSIVE_AREA = new OracleParameter();
                    OracleParameter p_NONRESI_COMMON_USE_AREA = new OracleParameter();
                    OracleParameter p_NONRESI_SUPPLY_AREA = new OracleParameter();
                    OracleParameter p_NONRESI_PARKING_AREA = new OracleParameter();
                    OracleParameter p_NONRESI_CONTRACT_AREA = new OracleParameter();
                    OracleParameter p_NONRESI_LEGAL_PARKING = new OracleParameter();
                    OracleParameter p_NONRESI_PLOT_SHARE_AREA = new OracleParameter();
                    OracleParameter p_FRST_REGISTER_ID = new OracleParameter();

                    p_REGI_MST_NO.DbType = System.Data.DbType.String;
                    p_REGI_MST_NO.Value = idColumnCode[0].ToString();
                    p_REGI_MST_NO.ParameterName = "p_REGI_MST_NO";

                        p_REGI_SUB_MST_NO.DbType = System.Data.DbType.String;
                    p_REGI_SUB_MST_NO.Value = idColumnCode[1].ToString();
                    p_REGI_SUB_MST_NO.ParameterName = "p_REGI_SUB_MST_NO";

                    p_REGI_PRE_DESIGN_NO.DbType = System.Data.DbType.Decimal;
                    p_REGI_PRE_DESIGN_NO.Value = REGI_PRE_DESIGN_NO.ToString();
                    p_REGI_PRE_DESIGN_NO.ParameterName = "p_REGI_PRE_DESIGN_NO";

                    p_NONRESI_USE_CD.DbType = System.Data.DbType.String;
                    p_NONRESI_USE_CD.Value = USE_CD.ToString();
                    p_NONRESI_USE_CD.ParameterName = "p_NONRESI_USE_CD";

                    p_NONRESI_EXCLUSIVE_AREA.DbType = System.Data.DbType.Decimal;
                    p_NONRESI_EXCLUSIVE_AREA.Value = Math.Round(NONRESI_EXCLUSIVE_AREA/1000000,2).ToString();
                    p_NONRESI_EXCLUSIVE_AREA.ParameterName = "p_NONRESI_EXCLUSIVE_AREA";

                    p_NONRESI_COMMON_USE_AREA.DbType = System.Data.DbType.Decimal;
                    p_NONRESI_COMMON_USE_AREA.Value = Math.Round(NONRESI_COMMON_USE_AREA/1000000, 2).ToString();
                    p_NONRESI_COMMON_USE_AREA.ParameterName = "p_NONRESI_COMMON_USE_AREA";

                    p_NONRESI_SUPPLY_AREA.DbType = System.Data.DbType.Decimal;
                    p_NONRESI_SUPPLY_AREA.Value = Math.Round(NONRESI_EXCLUSIVE_AREA + NONRESI_COMMON_USE_AREA/1000000, 2).ToString();
                    p_NONRESI_SUPPLY_AREA.ParameterName = "p_NONRESI_SUPPLY_AREA";

                    p_NONRESI_PARKING_AREA.DbType = System.Data.DbType.Decimal;
                    p_NONRESI_PARKING_AREA.Value = Math.Round(NONRESI_PARKING_AREA / 1000000, 2).ToString();
                    p_NONRESI_PARKING_AREA.ParameterName = "p_NONRESI_PARKING_AREA";

                    p_NONRESI_CONTRACT_AREA.DbType = System.Data.DbType.Decimal;
                    p_NONRESI_CONTRACT_AREA.Value = Math.Round(NONRESI_EXCLUSIVE_AREA + NONRESI_COMMON_USE_AREA + NONRESI_PARKING_AREA / 1000000, 2).ToString();
                    p_NONRESI_CONTRACT_AREA.ParameterName = "p_NONRESI_CONTRACT_AREA";

                    p_NONRESI_LEGAL_PARKING.DbType = System.Data.DbType.Decimal;
                    p_NONRESI_LEGAL_PARKING.Value = NONRESI_LEGAL_PARKING.ToString();
                    p_NONRESI_LEGAL_PARKING.ParameterName = "p_NONRESI_LEGAL_PARKING";

                    p_NONRESI_PLOT_SHARE_AREA.DbType = System.Data.DbType.Decimal;
                    p_NONRESI_PLOT_SHARE_AREA.Value = Math.Round(NONRESI_PLOT_SHARE_AREA / 1000000, 2).ToString();
                    p_NONRESI_PLOT_SHARE_AREA.ParameterName = "p_NONRESI_PLOT_SHARE_AREA";

                    p_FRST_REGISTER_ID.DbType = System.Data.DbType.String;
                    p_FRST_REGISTER_ID.Value = userID;
                    p_FRST_REGISTER_ID.ParameterName = "p_FRST_REGISTER_ID";

                    comm.Parameters.Add(p_REGI_MST_NO);
                    comm.Parameters.Add(p_REGI_SUB_MST_NO);
                    comm.Parameters.Add(p_REGI_PRE_DESIGN_NO);
                    comm.Parameters.Add(p_NONRESI_EXCLUSIVE_AREA);
                    comm.Parameters.Add(p_NONRESI_COMMON_USE_AREA);
                    comm.Parameters.Add(p_NONRESI_SUPPLY_AREA);
                    comm.Parameters.Add(p_NONRESI_PARKING_AREA);
                    comm.Parameters.Add(p_NONRESI_CONTRACT_AREA);
                    comm.Parameters.Add(p_NONRESI_LEGAL_PARKING);
                    comm.Parameters.Add(p_NONRESI_PLOT_SHARE_AREA);

                    comm.Parameters.Add(p_FRST_REGISTER_ID);

                    comm.ExecuteNonQuery();
                    comm.Dispose();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                }
            }
        }

        public static void AddDesignModel(List<string> idColumnName, List<string> idColumnCode, string userID, int REGI_PRE_DESIGN_NO, string DESIGN_PATTERN, string INPUT_PARAMETER, string CORE_TYPE, List<double> targetArea, List<double> targetTypeCount)
        {
            string sql = "INSERT INTO TD_DESIGN_MODEL(REGI_MST_NO,REGI_SUB_MST_NO,REGI_PRE_DESIGN_NO,DESIGN_PATTERN,INPUT_PARAM_VECTOR,CORE_TYPE,GOAL_PYUNG_TYPE_VECTOR,GOAL_RT_VECTOR,FRST_REGIST_DT,FRST_REGISTER_ID) VALUES(:p_REGI_MST_NO,:p_REGI_SUB_MST_NO,:p_REGI_PRE_DESIGN_NO,:p_DESIGN_PATTERN,:p_INPUT_PARAM_VECTOR,:p_CORE_TYPE,:p_GOAL_PYUNG_TYPE_VECTOR,:p_GOAL_RT_VECTOR,SYSDATE,:p_FRST_REGISTER_ID)";

            using (OracleConnection connection = new OracleConnection(Consts.connectionString))
            {
                try
                {
                    connection.Open();

                    OracleCommand comm = new OracleCommand(sql, connection);

                    OracleParameter p_REGI_MST_NO = new OracleParameter();
                    OracleParameter p_REGI_SUB_MST_NO = new OracleParameter();
                    OracleParameter p_REGI_PRE_DESIGN_NO = new OracleParameter();
                    OracleParameter p_DESIGN_PATTERN = new OracleParameter();
                    OracleParameter p_INPUT_PARAM_VECTOR = new OracleParameter();
                    OracleParameter p_CORE_TYPE = new OracleParameter();
                    OracleParameter p_GOAL_PYUNG_TYPE_VECTOR = new OracleParameter();
                    OracleParameter p_GOAL_RT_VECTOR = new OracleParameter();
                    OracleParameter p_FRST_REGIST_DT = new OracleParameter();
                    OracleParameter p_FRST_REGISTER_ID = new OracleParameter();

                    p_REGI_MST_NO.DbType = System.Data.DbType.String;
                    p_REGI_MST_NO.Value = idColumnCode[0].ToString();
                    p_REGI_MST_NO.ParameterName = "p_REGI_MST_NO";

                    p_REGI_SUB_MST_NO.DbType = System.Data.DbType.String;
                    p_REGI_SUB_MST_NO.Value = idColumnCode[1].ToString();
                    p_REGI_SUB_MST_NO.ParameterName = "p_REGI_SUB_MST_NO";

                    p_REGI_PRE_DESIGN_NO.DbType = System.Data.DbType.Decimal;
                    p_REGI_PRE_DESIGN_NO.Value = REGI_PRE_DESIGN_NO.ToString();
                    p_REGI_PRE_DESIGN_NO.ParameterName = "p_REGI_PRE_DESIGN_NO";

                    p_DESIGN_PATTERN.DbType = System.Data.DbType.String;
                    p_DESIGN_PATTERN.Value = DESIGN_PATTERN;
                    p_DESIGN_PATTERN.ParameterName = "p_DESIGN_PATTERN";

                    p_INPUT_PARAM_VECTOR.DbType = System.Data.DbType.String;
                    p_INPUT_PARAM_VECTOR.Value = INPUT_PARAMETER;
                    p_INPUT_PARAM_VECTOR.ParameterName = "p_INPUT_PARAM_VECTOR";

                    p_CORE_TYPE.DbType = System.Data.DbType.String;
                    p_CORE_TYPE.Value = CORE_TYPE;
                    p_CORE_TYPE.ParameterName = "p_CORE_TYPE";

                    p_GOAL_PYUNG_TYPE_VECTOR.DbType = System.Data.DbType.String;
                    p_GOAL_PYUNG_TYPE_VECTOR.Value = ListToCSV(targetArea);
                    p_GOAL_PYUNG_TYPE_VECTOR.ParameterName = "p_GOAL_PYUNG_TYPE_VECTOR";

                    p_GOAL_RT_VECTOR.DbType = System.Data.DbType.String;
                    p_GOAL_RT_VECTOR.Value = ListToCSV(targetTypeCount);
                    p_GOAL_RT_VECTOR.ParameterName = "p_GOAL_RT_VECTOR";

                    p_FRST_REGISTER_ID.DbType = System.Data.DbType.String;
                    p_FRST_REGISTER_ID.Value = userID;
                    p_FRST_REGISTER_ID.ParameterName = "p_FRST_REGISTER_ID";

                    comm.Parameters.Add(p_REGI_MST_NO);
                    comm.Parameters.Add(p_REGI_SUB_MST_NO);
                    comm.Parameters.Add(p_REGI_PRE_DESIGN_NO);
                    comm.Parameters.Add(p_DESIGN_PATTERN);
                    comm.Parameters.Add(p_INPUT_PARAM_VECTOR);
                    comm.Parameters.Add(p_CORE_TYPE);
                    comm.Parameters.Add(p_GOAL_PYUNG_TYPE_VECTOR);
                    comm.Parameters.Add(p_GOAL_RT_VECTOR);

                    comm.Parameters.Add(p_FRST_REGISTER_ID);

                    comm.ExecuteNonQuery();
                    comm.Dispose();
                }
                catch (OracleException ex)
                {
                    MessageBox.Show(ex.ToString());
                }
            }
        }

        public static void SaveDesignReportBlob(string sourceFilePath, string rowName, List<string> idColumnName, List<string> idColumnCode)
        {
            string sql = "UPDATAE TD_DESIGN_REPORT SET " + rowName + "@Source";

            if (idColumnName.Count() != 0)
                sql += " WHERE";

            for (int i = 0; i < idColumnName.Count(); i++)
            {
                if (i != 0)
                    sql += " AND";

                sql += " " + idColumnName[i] + "=" + idColumnCode[i] + " ";
            }

            using (OracleConnection connection = new OracleConnection(Consts.connectionString))
            {
                try
                {
                    connection.Open();

                    OracleCommand comm = new OracleCommand(sql, connection);

                    System.IO.FileStream fs = new System.IO.FileStream(sourceFilePath, System.IO.FileMode.Open, System.IO.FileAccess.Read);
                    Byte[] currentByte = new Byte[fs.Length];
                    fs.Read(currentByte, 0, currentByte.Length);
                    fs.Close();

                    OracleParameter currentParameter = new OracleParameter("@Source", OracleDbType.Blob, currentByte.Length, System.Data.ParameterDirection.Input, false, 0, 0, null, System.Data.DataRowVersion.Current, currentByte);

                    comm.Parameters.Add(currentParameter);

                    connection.Open();
                    comm.ExecuteNonQuery();
                    connection.Close();
                }
                catch(Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        public static void AddDesignReport(List<string> idColumnName, List<string> idColumnCode,int REGI_PRE_DESIGN_NO, List<string> FilePath, List<string> RowName)
        {
            List<string> idColumnNameCopy = new List<string>(idColumnName);
            List<string> idColumnCodeCopy = new List<string>(idColumnCode);

            idColumnNameCopy.Add("REGI_PRE_DESIGN_NO");
            idColumnCodeCopy.Add(REGI_PRE_DESIGN_NO.ToString());

            for(int i = 0; i < FilePath.Count(); i++)
            {
                SaveDesignReportBlob(FilePath[i], RowName[i], idColumnNameCopy, idColumnCodeCopy);
            }
        }

        public static string getStringFromRegistry(string name)
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey("Software\\SH\\HOUSE\\"))
                {
                    if (key != null)
                    {
                        Object o = key.GetValue(name);

                        if (o != null)
                        {
                            return o as string;                                                               //do what you like with version
                        }
                    }

                }
            }
            catch (Exception)  //just for demonstration...it's always best to handle specific exceptions
            {
                return "";
            }

            return "";
        }

        public static string getAddressFromServer(List<string> idColumnName, List<string> idColumnCode)
        {
            List<string> landCodeSet = new List<string>();
            string address = "";

            const string connectionString = "Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=oracle3.ejudata.co.kr)(PORT=1521))(CONNECT_DATA=(SERVER=DEDICATED)(SID = oracle3)));User Id=whruser_sh;Password=ejudata;";

            string sql = "select * FROM TN_PREV_ASSET";

            if (idColumnName.Count() != 0)
                sql += " WHERE";

            for (int i = 0; i < idColumnName.Count(); i++)
            {
                if (i != 0)
                    sql += " AND";

                sql += " " + idColumnName[i] + "=" + idColumnCode[i] + " ";
            }

            using (OracleConnection connection = new OracleConnection(connectionString))
            {
                try
                {
                    connection.Open();

                    OracleCommand comm = new OracleCommand(sql, connection);
                    OracleDataReader rs = comm.ExecuteReader();

                    while (rs.Read())
                    {
                        landCodeSet.Add(rs["LAND_CD"].ToString());
                    }

                    string sqlToGetAddress = "select * FROM TN_LAW_LAND WHERE LAND_CD=" + landCodeSet[0];

                    OracleCommand commToGetAddress = new OracleCommand(sqlToGetAddress, connection);
                    OracleDataReader readerToGetAddress = commToGetAddress.ExecuteReader();

                    while (readerToGetAddress.Read())
                    {
                        address += readerToGetAddress["LAND_SIDO_NM"].ToString();
                        address += " " + readerToGetAddress["LAND_SIGUNGU_NM"].ToString();
                        address += " " + readerToGetAddress["LAND_DONG_NM"].ToString();

                        if (readerToGetAddress["LAND_RI_NM"] != null)
                            address += " " + readerToGetAddress["LAND_RI_NM"].ToString();
                    }

                    //address += " " + rs["BNBUN"].ToString() + "-" + rs["BUBUN"].ToString() + "번지";

                    rs.Close();
                    readerToGetAddress.Close();
                }
                catch (OracleException ex)
                {
                    MessageBox.Show(ex.ToString());
                }

                connection.Close();
            }

            return address;
        }

        public static List<string> getStringFromServer(string dataToGet, string tableName, List<string> idColumnName, List<string> idColumnCode)
        {
            List<string> output = new List<string>();

            const string connectionString = "Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=oracle3.ejudata.co.kr)(PORT=1521))(CONNECT_DATA=(SERVER=DEDICATED)(SID = oracle3)));User Id=whruser_sh;Password=ejudata;";

            string sql = "select * FROM " + tableName;

            if (idColumnName.Count() != 0)
                sql += " WHERE";

            for (int i = 0; i < idColumnName.Count(); i++)
            {
                if (i != 0)
                    sql += " AND";

                sql += " " + idColumnName[i] + "=" + idColumnCode[i] + " ";
            }

            using (OracleConnection connection = new OracleConnection(connectionString))
            {
                try
                {
                    connection.Open();

                    OracleCommand comm = new OracleCommand(sql, connection);
                    OracleDataReader rs = comm.ExecuteReader();

                    while (rs.Read())
                    {
                        output.Add(rs[dataToGet].ToString());
                    }

                    rs.Close();
                }
                catch (OracleException)
                {
                }
            }

            return output;
        }

        private static double vectorAngle(Vector3d vector)
        {
            double sin = vector.Y / vector.Length;
            double cos = vector.X / vector.Length;

            if (sin > 0)
                return Math.Acos(cos);
            else
                return Math.PI - Math.Acos(cos);
        }

        public static Curve adjustOrientation(Curve crv)
        {
            if ((int)crv.ClosedCurveOrientation(new Vector3d(0, 0, 1)) == -1)
            {
                crv.Reverse();
            }
            return crv;
        }

        public static Curve joinRegulations(Curve firstCurve, Curve secondCurve)
        {
            var intersections = Rhino.Geometry.Intersect.Intersection.CurveCurve(firstCurve, secondCurve, 0, 0);

            List<double> firstShatterDomain = new List<double>();
            List<double> secondShatterDomain = new List<double>();

            foreach (Rhino.Geometry.Intersect.IntersectionEvent i in intersections)
            {
                firstShatterDomain.Add(i.ParameterA);
                secondShatterDomain.Add(i.ParameterB);
            }

            Curve[] shatteredFirst = firstCurve.Split(firstShatterDomain);
            Curve[] shatteredSecond = secondCurve.Split(secondShatterDomain);

            List<Curve> usableCrvs = new List<Curve>();

            foreach (Curve i in shatteredFirst)
            {
                if (secondCurve.Contains(i.PointAt(i.Domain.Mid)) != PointContainment.Outside)
                {
                    usableCrvs.Add(i);
                }
            }

            foreach (Curve i in shatteredSecond)
            {
                if (firstCurve.Contains(i.PointAt(i.Domain.Mid)) == PointContainment.Inside)
                {
                    usableCrvs.Add(i);
                }
            }

            Curve combinationCurve = Curve.JoinCurves(usableCrvs)[0];
            return combinationCurve;
        }

        public class colorSetting
        {
            public static System.Drawing.Color GetColorFromValue(double i)
            {
                int myInt = (int)(i * 255);
                System.Drawing.Color tempColor = System.Drawing.Color.FromArgb((int)myInt, (int)myInt, (int)myInt);

                return tempColor;
            }
        }


    }
}


