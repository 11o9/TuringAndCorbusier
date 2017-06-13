﻿using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using Rhino.Geometry;
using Microsoft.Win32;

namespace TuringAndCorbusier.SHServer
{
    class SHServer
    {
        public enum NonResiType { Commercial, PublicFacility };

        public static string GetNonresiUseCode(NonResiType type)
        {
            if (type == NonResiType.Commercial)
                return "1000";
            else if (type == NonResiType.PublicFacility)
                return "2000";
            else
                return "1000";

        }


        public static void sendDataToServer(Apartment tempAGOutput, Dictionary<string,string> reportPath)
        {
            string[] CurrentDataIdName = { "REGI_MST_NO", "REGI_SUB_MST_NO" };
            string[] CurrentDataId = { getStringFromRegistry("REGI_MST_NO"), getStringFromRegistry("REGI_SUB_MST_NO") };

            string USERID = getStringFromRegistry("USERID");
            string DBURL = getStringFromRegistry("DBURL");

            try
            {
                ///출력 할 Apartment
                ///
                ///현재 대지에 대한 DesignMaster입력 << 중복될 시 입력 안함

                if (!checkDesignMasterPresence(CurrentDataIdName.ToList(), CurrentDataId.ToList()))
                {
                    List<Point3d> tempBoundaryPts = new List<Point3d>();

                    foreach (Curve i in tempAGOutput.Plot.Boundary.DuplicateSegments())
                        tempBoundaryPts.Add(i.PointAt(i.Domain.T0));

                    AddTdDesignMaster(CurrentDataIdName.ToList(), CurrentDataId.ToList(), USERID, (int)TuringAndCorbusierPlugIn.InstanceClass.page1Settings.MaxFloorAreaRatio, (int)TuringAndCorbusierPlugIn.InstanceClass.page1Settings.MaxBuildingCoverage, TuringAndCorbusierPlugIn.InstanceClass.page1Settings.MaxFloors, GetPlotBoundaryVector(tempBoundaryPts), ListToCSV(tempAGOutput.Plot.Surroundings.ToList()), tempAGOutput.Plot.GetArea());
                }


                ///가장 마지막 DESIGN_NO 다음 번호로 DesignDetail(설계 전체에 관한 내용) 입력



                int temp_REGI_PRE_DESIGN_NO;


                AddDesignDetail(CurrentDataIdName.ToList(), CurrentDataId.ToList(), USERID, tempAGOutput, out temp_REGI_PRE_DESIGN_NO);


                ///세대 타입 취합
                Stack<HouseholdStatistics> hhsStack = new Stack<HouseholdStatistics>();
                List<HouseholdStatistics> temphhs1 = new List<HouseholdStatistics>();

                temphhs1 = new List<HouseholdStatistics>(tempAGOutput.HouseholdStatistics);

                double max = 0;
                int maxindex = -1;
                int index = temphhs1.Count;
                for (int j = 0; j < index; j++)
                {
                    //0 3      1  3  2  3   
                    max = 0;
                    for (int i = 0; i < index - j; i++)
                    {
                        if (temphhs1[i].ExclusiveArea > max)
                        {
                            max = temphhs1[i].ExclusiveArea;
                            maxindex = i;
                        }
                    }

                    hhsStack.Push(temphhs1[maxindex]);
                    temphhs1.RemoveAt(maxindex);

                }

                List<HouseholdStatistics> temphhs = new List<HouseholdStatistics>();
                List<HouseholdStatistics> tomerge = new List<HouseholdStatistics>();
                int time = hhsStack.Count;
                for (int i = 0; i < time; i++)
                {
                    if (tomerge.Count != 0)
                    {
                        if (Math.Abs(Math.Round(tomerge.Last().ExclusiveArea / 3.3 / 1000000) - Math.Round(hhsStack.First().ExclusiveArea / 3.3 / 1000000)) < 2)
                        {
                            tomerge.Add(hhsStack.Pop());
                        }

                        else
                        {
                            temphhs.Add(new HouseholdStatistics(tomerge));
                            //RhinoApp.WriteLine(tomerge.Count.ToString() + " 개의 householdstatistics 가 병합됨.");
                            tomerge.Clear();
                            tomerge.Add(hhsStack.Pop());
                        }

                    }
                    else
                        tomerge.Add(hhsStack.Pop());


                    if (i == time - 1)
                    {
                        temphhs.Add(new HouseholdStatistics(tomerge));
                    }
                }


                ///각 세대 타입별 정보 입력

                //for (int i = 0; i < tempAGOutput.HouseholdStatistics.Count; i++)
                //{

                //    HouseholdStatistics tempHouseholdStatistics = tempAGOutput.HouseholdStatistics[i];
                //    double tempExclusiveAreaSum = tempAGOutput.GetExclusiveAreaSum();
                //    double tempExclusiveArea = tempHouseholdStatistics.GetExclusiveArea();

                //    double GroundCoreAreaPerHouse = tempAGOutput.GetCoreAreaOnEarthSum() / (tempExclusiveAreaSum + tempAGOutput.GetCommercialArea()) * tempExclusiveArea;
                //    double coreAreaPerHouse = GroundCoreAreaPerHouse + (tempAGOutput.GetCoreAreaSum() - tempAGOutput.GetCoreAreaOnEarthSum()) / tempExclusiveAreaSum * tempExclusiveArea;
                //    double parkingLotAreaPerHouse = tempAGOutput.ParkingLotUnderGround.GetAreaSum() / (tempExclusiveAreaSum + tempAGOutput.GetCommercialArea()) * tempExclusiveArea;
                //    double plotAreaPerHouse = tempAGOutput.Plot.GetArea() / (tempExclusiveAreaSum + tempAGOutput.GetCommercialArea()) * tempExclusiveArea;
                //    double welfareAreaPerHouse = tempAGOutput.GetPublicFacilityArea() / tempExclusiveAreaSum * tempExclusiveArea;
                //    double facilitiesAreaPerHouse = 0 / tempExclusiveAreaSum * tempExclusiveArea;

                //    CommonFunc.AddTdDesignArea(CurrentDataIdName.ToList(), CurrentDataId.ToList(), temp_REGI_PRE_DESIGN_NO, USERID, tempAGOutput.AreaTypeString()[i], coreAreaPerHouse, welfareAreaPerHouse, facilitiesAreaPerHouse, parkingLotAreaPerHouse, plotAreaPerHouse, tempHouseholdStatistics);

                //}

                for (int i = 0; i < temphhs.Count; i++)
                {

                    HouseholdStatistics tempHouseholdStatistics = temphhs[i];
                    double tempExclusiveAreaSum = tempAGOutput.GetExclusiveAreaSum();
                    double tempExclusiveArea = temphhs[i].GetExclusiveArea();
                    double rate = tempExclusiveArea / tempExclusiveAreaSum;
                    //double GroundCoreAreaPerHouse = tempAGOutput.GetCoreAreaOnEarthSum() / (tempExclusiveAreaSum + tempAGOutput.GetCommercialArea()) * tempExclusiveArea;

                    //ground , rooftop cores
                    double GroundCoreAreaPerHouse = tempAGOutput.Core[0].Sum(n => n.GetArea()) * 2 * rate;

                    //double coreareaSum = tempAGOutput.GetCoreAreaSum();
                    //double coreAreaPerHouse = GroundCoreAreaPerHouse + (tempAGOutput.GetCoreAreaSum() - tempAGOutput.GetCoreAreaOnEarthSum()) / tempExclusiveAreaSum * tempExclusiveArea;
                    //double floorCores = tempAGOutput.GetCoreAreaSum() - tempAGOutput.Core[0].Sum(n => n.GetArea()) * 2;
                    double coreAreaPerHouse = GroundCoreAreaPerHouse + (tempAGOutput.GetCoreAreaSum() - tempAGOutput.Core[0].Sum(n => n.GetArea()) * 2) * rate;

                    double parkingLotAreaPerHouse = tempAGOutput.ParkingLotUnderGround.ParkingArea / (tempExclusiveAreaSum + tempAGOutput.GetCommercialArea()) * tempExclusiveArea;

                    double plotAreaPerHouse = tempAGOutput.Plot.GetArea() / (tempExclusiveAreaSum + tempAGOutput.GetCommercialArea()) * tempExclusiveArea;
                    double welfareAreaPerHouse = tempAGOutput.GetPublicFacilityArea() / tempExclusiveAreaSum * tempExclusiveArea;
                    double facilitiesAreaPerHouse = 0 / tempExclusiveAreaSum * tempExclusiveArea;

                    AddTdDesignArea(CurrentDataIdName.ToList(), CurrentDataId.ToList(), temp_REGI_PRE_DESIGN_NO, USERID, "A", coreAreaPerHouse, welfareAreaPerHouse, facilitiesAreaPerHouse, parkingLotAreaPerHouse, plotAreaPerHouse, tempHouseholdStatistics);

                }

                AddDesignModel(CurrentDataIdName.ToList(), CurrentDataId.ToList(), USERID, temp_REGI_PRE_DESIGN_NO, tempAGOutput.AGtype, ListToCSV(tempAGOutput.ParameterSet.Parameters.ToList()), "Core", tempAGOutput.Target.Area, tempAGOutput.Target.Ratio);

                if (tempAGOutput.Commercial.Count != 0)
                {
                    double plotAreaOfCommercial = tempAGOutput.Plot.GetArea() / (tempAGOutput.GetExclusiveAreaSum() + tempAGOutput.GetCommercialArea()) * tempAGOutput.GetCommercialArea();
                    double coreAreaOfCommercial = tempAGOutput.GetCoreAreaOnEarthSum() / (tempAGOutput.GetExclusiveAreaSum() + tempAGOutput.GetCommercialArea()) * tempAGOutput.GetCommercialArea();
                    double parkingLotAreaOfCommercial = tempAGOutput.ParkingLotUnderGround.ParkingArea / (tempAGOutput.GetExclusiveAreaSum() + tempAGOutput.GetCommercialArea()) * tempAGOutput.GetCommercialArea();

                    AddDesignNonResidential(CurrentDataIdName.ToList(), CurrentDataId.ToList(), USERID, temp_REGI_PRE_DESIGN_NO, GetNonresiUseCode(NonResiType.Commercial), tempAGOutput.GetCommercialArea(), 0, parkingLotAreaOfCommercial, (int)tempAGOutput.GetLegalParkingLotOfCommercial(), plotAreaOfCommercial);
                }


                //RhinoApp.WriteLine("설계 보고서 업로드 시작");
                AddTdDesignReport(CurrentDataIdName.ToList(), CurrentDataId.ToList(), reportPath, temp_REGI_PRE_DESIGN_NO);



                if (tempAGOutput.PublicFacility.Count != 0)
                {
                    double plotAreaOfPublic = 0;
                    double parkingLotAreaOfPublic = 0;

                    AddDesignNonResidential(CurrentDataIdName.ToList(), CurrentDataId.ToList(), USERID, temp_REGI_PRE_DESIGN_NO, GetNonresiUseCode(NonResiType.Commercial), tempAGOutput.GetCommercialArea(), 0, parkingLotAreaOfPublic, (int)tempAGOutput.GetLegalParkingLotOfCommercial(), plotAreaOfPublic);
                }


                //CommonFunc.AddDesignReport(CurrentDataIdName.ToList(), CurrentDataId.ToList(),temp_REGI_PRE_DESIGN_NO);


                ///임시 파일 경로 - 추가시 각 코드 밑에 넣기

                //string dummypath = "C:\\Users\\user\\Desktop\\Dummyfiles\\";

                //MainPanel_reportspaths[tempIndex].Add("ELEVATION", dummypath + "elevation.JPG");
                //MainPanel_reportspaths[tempIndex].Add("SECTION", dummypath + "section.JPG");
                //MainPanel_reportspaths[tempIndex].Add("DWG_PLANS", dummypath + "dwgplans.dwg");
                //MainPanel_reportspaths[tempIndex].Add("GROUND_PLAN", dummypath + "groundplan.JPG");
                //MainPanel_reportspaths[tempIndex].Add("TYPICAL_PLAN", dummypath + "typicalplan.JPG");
                //MainPanel_reportspaths[tempIndex].Add("BIRDEYE1", dummypath + "birdeye1.JPG");
                //MainPanel_reportspaths[tempIndex].Add("BIRDEYE2", dummypath + "birdeye2.JPG");






                //RhinoApp.WriteLine("업로드 끝");

            }
            catch (Exception x)
            {
                //RhinoApp.WriteLine("업로드 실패");
            }
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

                    while (reader.Read())
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

                    while (reader.Read())
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

            foreach (string i in areaList)
            {
                areaMassSum += double.Parse(i);
            }

            return areaMassSum;
        }

        public static string GetAddressFromServer()
        {

            List<string> idColumnName = new List<string>{ "REGI_MST_NO", "REGI_SUB_MST_NO" };
            List<string> idColumnCode = new List<string> { getStringFromRegistry("REGI_MST_NO"), getStringFromRegistry("REGI_SUB_MST_NO") };

            

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

            bool IsFirst = true;

            using (OracleConnection connection = new OracleConnection(Consts.connectionString))
            {
                try
                {
                    connection.Open();

                    OracleCommand readComm = new OracleCommand(readSql, connection);
                    OracleDataReader reader = readComm.ExecuteReader();

                    while (reader.Read() && IsFirst)
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

                        addressList.Add(reader["BNBUN"].ToString() + "-" + reader["BUBUN"].ToString());

                        IsFirst = false;
                    }
                }
                catch (OracleException ex)
                {
                    MessageBox.Show(ex.ToString());
                }
            }

            string address = "";

            foreach (string i in addressList)
            {
                address += i;
                address += " ";
            }

            if (IsFirst == false)
            {
                address += " 일원";
            }

            return address;
        }

        public static bool checkDesignMasterPresence(List<string> idColumnName, List<string> idColumnCode)
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

                    bool output = tempReader.Read();

                    tempCommand.Dispose();

                    return output;
                }
                catch (Exception ex)
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

                    ///추가사항
                    ///




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
                    p_PLOT_ADDRESS.Value = GetAddressFromServer();
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
                    p_PLOTAREA_PLAN.Value = Math.Round(PLOTAREA_PLAN / 1000000, 2).ToString();
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

        private static Oracle.ManagedDataAccess.Types.OracleBlob GetBlobDataFromFile(string path, OracleConnection conn)
        {
            Oracle.ManagedDataAccess.Types.OracleBlob blob = new Oracle.ManagedDataAccess.Types.OracleBlob(conn);

            System.IO.FileInfo fi = new System.IO.FileInfo(path);
            string filename = fi.Name;
            int filesize = (int)fi.Length;
            byte[] file = new byte[fi.Length - 1];
            System.IO.FileStream fs = new System.IO.FileStream(path, System.IO.FileMode.Open, System.IO.FileAccess.Read);
            System.IO.BinaryReader br = new System.IO.BinaryReader(fs);
            int bytes;

            try
            {
                while ((bytes = br.Read(file, 0, file.Length)) > 0)
                {
                    blob.Write(file, 0, bytes);

                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally { br.Close(); fs.Close(); }

            return blob;
        }

        public static void AddTdDesignReport(List<string> idColumnName, List<string> idColumnCode, Dictionary<string, string> path, int temp_REGI_PRE_DESIGN_NO)
        {

            string REPORT_path = "";
            //string ELEVATION_path = "";
            string SECTION_path = "";
            //string DWG_PLANS_path = "";
            string GROUND_PLAN_path = "";
            string TYPICAL_PLAN_path = "";
            string BIRDEYE1_path = "";
            string BIRDEYE2_path = "";

            path.TryGetValue("REPORT", out REPORT_path);
            //path.TryGetValue("ELEVATION", out ELEVATION_path);
            path.TryGetValue("SECTION", out SECTION_path);
            //path.TryGetValue("DWG_PLANS", out DWG_PLANS_path);
            path.TryGetValue("GROUND_PLAN", out GROUND_PLAN_path);
            path.TryGetValue("TYPICAL_PLAN", out TYPICAL_PLAN_path);
            path.TryGetValue("BIRDEYE1", out BIRDEYE1_path);
            path.TryGetValue("BIRDEYE2", out BIRDEYE2_path);

            //MessageBox.Show("report path = " + REPORT_path); //+ Environment.NewLine + "ELEVATION path = " + ELEVATION_path + Environment.NewLine + "SECTION path = " + SECTION_path + Environment.NewLine
            //    + "DWG_PLANS path = " + DWG_PLANS_path + Environment.NewLine + "GROUND_PLAN path = " + GROUND_PLAN_path + Environment.NewLine + "TYPICAL_PLAN path = " + TYPICAL_PLAN_path + Environment.NewLine +
            //    "BIRDEYE1 path = " + BIRDEYE1_path + Environment.NewLine + "BIRDEYE2 path = " + BIRDEYE2_path );



            //string sql = "INSERT INTO TD_DESIGN_REPORT("+
            //    "REGI_MST_NO,REGI_SUB_MST_NO,REGI_PRE_DESIGN_NO,PDF_REPORT,PDF_REPORT_SIZE,IMG_BIRD_EYE_1,IMG_BIRD_EYE_1_SIZE,IMG_BIRD_EYE_2,IMG_BIRD_EYE_2_SIZE,IMG_GROUND_FLOOR_PLAN,IMG_GROUND_FLOOR_PLAN_SIZE,"+
            //    "IMG_TYPICAL_FLOOR_PLAN,IMG_TYPICAL_FLOOR_PLAN_SIZE,IMG_ELEVATION,IMG_ELEVATION_SIZE,IMG_SECTION,IMG_SECTION_SIZE,DWG_PLANS,DWG_PLANS_SIZE,FRST_REGIST_DT,FRST_REGISTER_ID)"
            //    + "VALUES("+
            //    ":p_REGI_MST_NO,:p_REGI_SUB_MST_NO,:p_REGI_PRE_DESIGN_NO,:p_PDF_REPORT,:p_PDF_REPORT_SIZE,:p_IMG_BIRD_EYE_1,:p_IMG_BIRD_EYE_1_SIZE,:p_IMG_BIRD_EYE_2,:p_IMG_BIRD_EYE_2_SIZE,:p_IMG_GROUND_FLOOR_PLAN,:p_IMG_GROUND_FLOOR_PLAN_SIZE,"+
            //    ":p_IMG_TYPICAL_FLOOR_PLAN,:p_IMG_TYPICAL_FLOOR_PLAN_SIZE,:p_IMG_ELEVATION,:p_IMG_ELEVATION_SIZE,:p_IMG_SECTION,:p_IMG_SECTION_SIZE,:p_DWG_PLANS,:p_DWG_PLANS_SIZE,SYSDATE,:p_FRST_REGISTER_ID)";



            string sql = "INSERT INTO TD_DESIGN_REPORT (REGI_MST_NO,REGI_SUB_MST_NO,REGI_PRE_DESIGN_NO,PDF_REPORT,PDF_REPORT_SIZE,IMG_BIRD_EYE_1,IMG_BIRD_EYE_1_SIZE,IMG_BIRD_EYE_2,IMG_BIRD_EYE_2_SIZE,IMG_GROUND_FLOOR_PLAN,IMG_GROUND_FLOOR_PLAN_SIZE,IMG_TYPICAL_FLOOR_PLAN,IMG_TYPICAL_FLOOR_PLAN_SIZE,IMG_SECTION,IMG_SECTION_SIZE,FRST_REGIST_DT,FRST_REGISTER_ID)"
                + "VALUES(:p_REGI_MST_NO,:p_REGI_SUB_MST_NO,:p_REGI_PRE_DESIGN_NO,:p_PDF_REPORT,:p_PDF_REPORT_SIZE,:p_IMG_BIRD_EYE_1,:p_IMG_BIRD_EYE_1_SIZE,:p_IMG_BIRD_EYE_2,:p_IMG_BIRD_EYE_2_SIZE,:p_IMG_GROUND_FLOOR_PLAN,:p_IMG_GROUND_FLOOR_PLAN_SIZE,:p_IMG_TYPICAL_FLOOR_PLAN,:p_IMG_TYPICAL_FLOOR_PLAN_SIZE,:p_IMG_SECTION,:p_IMG_SECTION_SIZE,SYSDATE,:p_FRST_REGISTER_ID)";


            using (OracleConnection connection = new OracleConnection(Consts.connectionString))
            {
                try
                {
                    connection.Open();


                    Oracle.ManagedDataAccess.Types.OracleBlob REPORT_blob = GetBlobDataFromFile(REPORT_path, connection);
                    //Oracle.ManagedDataAccess.Types.OracleBlob ELEVATION_blob = GetBlobDataFromFile(ELEVATION_path, connection);
                    Oracle.ManagedDataAccess.Types.OracleBlob SECTION_blob = GetBlobDataFromFile(SECTION_path, connection);
                    //Oracle.ManagedDataAccess.Types.OracleBlob DWG_PLANS_blob = GetBlobDataFromFile(DWG_PLANS_path, connection);
                    Oracle.ManagedDataAccess.Types.OracleBlob GROUND_PLAN = GetBlobDataFromFile(GROUND_PLAN_path, connection);
                    Oracle.ManagedDataAccess.Types.OracleBlob TYPICAL_PLAN = GetBlobDataFromFile(TYPICAL_PLAN_path, connection);
                    Oracle.ManagedDataAccess.Types.OracleBlob BIRDEYE1 = GetBlobDataFromFile(BIRDEYE1_path, connection);
                    Oracle.ManagedDataAccess.Types.OracleBlob BIRDEYE2 = GetBlobDataFromFile(BIRDEYE2_path, connection);


                    OracleCommand comm = new OracleCommand(sql, connection);

                    OracleParameter p_REGI_MST_NO = new OracleParameter();
                    OracleParameter p_REGI_SUB_MST_NO = new OracleParameter();
                    OracleParameter p_REGI_PRE_DESIGN_NO = new OracleParameter();
                    OracleParameter p_PDF_REPORT = new OracleParameter();
                    OracleParameter p_PDF_REPORT_SIZE = new OracleParameter();
                    OracleParameter p_IMG_BIRD_EYE_1 = new OracleParameter();
                    OracleParameter p_IMG_BIRD_EYE_1_SIZE = new OracleParameter();
                    OracleParameter p_IMG_BIRD_EYE_2 = new OracleParameter();
                    OracleParameter p_IMG_BIRD_EYE_2_SIZE = new OracleParameter();
                    OracleParameter p_IMG_GROUND_FLOOR_PLAN = new OracleParameter();
                    OracleParameter p_IMG_GROUND_FLOOR_PLAN_SIZE = new OracleParameter();
                    OracleParameter p_IMG_TYPICAL_FLOOR_PLAN = new OracleParameter();
                    OracleParameter p_IMG_TYPICAL_FLOOR_PLAN_SIZE = new OracleParameter();
                    //OracleParameter p_IMG_ELEVATION = new OracleParameter();
                    //OracleParameter p_IMG_ELEVATION_SIZE = new OracleParameter();
                    OracleParameter p_IMG_SECTION = new OracleParameter();
                    OracleParameter p_IMG_SECTION_SIZE = new OracleParameter();
                    //OracleParameter p_DWG_PLANS = new OracleParameter();
                    //OracleParameter p_DWG_PLANS_SIZE = new OracleParameter();
                    OracleParameter p_FRST_REGISTER_ID = new OracleParameter();


                    p_REGI_MST_NO.OracleDbType = OracleDbType.Char;
                    p_REGI_MST_NO.Value = idColumnCode[0];
                    p_REGI_MST_NO.ParameterName = "p_REGI_MST_NO";

                    p_REGI_SUB_MST_NO.OracleDbType = OracleDbType.Char;
                    p_REGI_SUB_MST_NO.Value = idColumnCode[1];
                    p_REGI_SUB_MST_NO.ParameterName = "p_REGI_SUB_MST_NO";

                    p_REGI_PRE_DESIGN_NO.OracleDbType = OracleDbType.Decimal;
                    p_REGI_PRE_DESIGN_NO.Value = temp_REGI_PRE_DESIGN_NO;
                    p_REGI_PRE_DESIGN_NO.ParameterName = "p_REGI_PRE_DESIGN_NO";

                    p_PDF_REPORT.OracleDbType = OracleDbType.Blob;
                    p_PDF_REPORT.Value = REPORT_blob;
                    p_PDF_REPORT.ParameterName = "p_PDF_REPORT";

                    p_PDF_REPORT_SIZE.OracleDbType = OracleDbType.Decimal;
                    p_PDF_REPORT_SIZE.Value = REPORT_blob.Length;
                    p_PDF_REPORT_SIZE.ParameterName = "p_PDF_REPORT_SIZE";

                    p_IMG_BIRD_EYE_1.OracleDbType = OracleDbType.Blob;
                    p_IMG_BIRD_EYE_1.Value = BIRDEYE1;
                    p_IMG_BIRD_EYE_1.ParameterName = "p_IMG_BIRD_EYE_1";

                    p_IMG_BIRD_EYE_1_SIZE.OracleDbType = OracleDbType.Decimal;
                    p_IMG_BIRD_EYE_1_SIZE.Value = BIRDEYE1.Length;
                    p_IMG_BIRD_EYE_1_SIZE.ParameterName = "p_IMG_BIRD_EYE_1_SIZE";

                    p_IMG_BIRD_EYE_2.OracleDbType = OracleDbType.Blob;
                    p_IMG_BIRD_EYE_2.Value = BIRDEYE2;
                    p_IMG_BIRD_EYE_2.ParameterName = "p_IMG_BIRD_EYE_2";

                    p_IMG_BIRD_EYE_2_SIZE.OracleDbType = OracleDbType.Decimal;
                    p_IMG_BIRD_EYE_2_SIZE.Value = BIRDEYE2.Length;
                    p_IMG_BIRD_EYE_2_SIZE.ParameterName = "p_IMG_BIRD_EYE_2_SIZE";

                    p_IMG_GROUND_FLOOR_PLAN.OracleDbType = OracleDbType.Blob;
                    p_IMG_GROUND_FLOOR_PLAN.Value = GROUND_PLAN;
                    p_IMG_GROUND_FLOOR_PLAN.ParameterName = "p_IMG_GROUND_FLOOR_PLAN";

                    p_IMG_GROUND_FLOOR_PLAN_SIZE.OracleDbType = OracleDbType.Decimal;
                    p_IMG_GROUND_FLOOR_PLAN_SIZE.Value = GROUND_PLAN.Length;
                    p_IMG_GROUND_FLOOR_PLAN_SIZE.ParameterName = "p_IMG_GROUND_FLOOR_PLAN_SIZE";

                    p_IMG_TYPICAL_FLOOR_PLAN.OracleDbType = OracleDbType.Blob;
                    p_IMG_TYPICAL_FLOOR_PLAN.Value = TYPICAL_PLAN;
                    p_IMG_TYPICAL_FLOOR_PLAN.ParameterName = "p_IMG_TYPICAL_FLOOR_PLAN";

                    p_IMG_TYPICAL_FLOOR_PLAN_SIZE.OracleDbType = OracleDbType.Decimal;
                    p_IMG_TYPICAL_FLOOR_PLAN_SIZE.Value = TYPICAL_PLAN.Length;
                    p_IMG_TYPICAL_FLOOR_PLAN_SIZE.ParameterName = "p_IMG_TYPICAL_FLOOR_PLAN_SIZE";

                    //p_IMG_ELEVATION.OracleDbType = OracleDbType.Blob;
                    //p_IMG_ELEVATION.Value = ELEVATION_blob;  ///?
                    //p_IMG_ELEVATION.ParameterName = "p_IMG_ELEVATION";

                    //p_IMG_ELEVATION_SIZE.OracleDbType = OracleDbType.Decimal;
                    //p_IMG_ELEVATION_SIZE.Value = ELEVATION_blob.Length;
                    //p_IMG_ELEVATION_SIZE.ParameterName = "p_IMG_ELEVATION_SIZE";

                    p_IMG_SECTION.OracleDbType = OracleDbType.Blob;
                    p_IMG_SECTION.Value = SECTION_blob;  ///?
                    p_IMG_SECTION.ParameterName = "p_IMG_SECTION";

                    p_IMG_SECTION_SIZE.OracleDbType = OracleDbType.Decimal;
                    p_IMG_SECTION_SIZE.Value = SECTION_blob.Length;
                    p_IMG_SECTION_SIZE.ParameterName = "p_IMG_SECTION_SIZE";

                    //p_DWG_PLANS.OracleDbType = OracleDbType.Blob;
                    //p_DWG_PLANS.Value = DWG_PLANS_blob;  ///?
                    //p_DWG_PLANS.ParameterName = "p_DWG_PLANS";

                    //p_DWG_PLANS_SIZE.OracleDbType = OracleDbType.Decimal;
                    //p_DWG_PLANS_SIZE.Value = DWG_PLANS_blob.Length;
                    //p_DWG_PLANS_SIZE.ParameterName = "p_DWG_PLANS_SIZE";



                    p_FRST_REGISTER_ID.OracleDbType = OracleDbType.Varchar2;
                    p_FRST_REGISTER_ID.Value = getStringFromRegistry("USERID");//?
                    p_FRST_REGISTER_ID.ParameterName = "p_FRST_REGISTER_ID";


                    //얘는 추후 수정 코드에.
                    //p_LAST_UPDUSR_ID.OracleDbType = OracleDbType.Varchar2;
                    //p_LAST_UPDUSR_ID.Value = REPORT_blob;  ///?
                    //p_LAST_UPDUSR_ID.ParameterName = "p_LAST_UPDUSR_ID";

                    comm.Parameters.Add(p_REGI_MST_NO);
                    comm.Parameters.Add(p_REGI_SUB_MST_NO);
                    comm.Parameters.Add(p_REGI_PRE_DESIGN_NO);
                    comm.Parameters.Add(p_PDF_REPORT);
                    comm.Parameters.Add(p_PDF_REPORT_SIZE);
                    comm.Parameters.Add(p_IMG_BIRD_EYE_1);
                    comm.Parameters.Add(p_IMG_BIRD_EYE_1_SIZE);
                    comm.Parameters.Add(p_IMG_BIRD_EYE_2);
                    comm.Parameters.Add(p_IMG_BIRD_EYE_2_SIZE);
                    comm.Parameters.Add(p_IMG_GROUND_FLOOR_PLAN);
                    comm.Parameters.Add(p_IMG_GROUND_FLOOR_PLAN_SIZE);
                    comm.Parameters.Add(p_IMG_TYPICAL_FLOOR_PLAN);
                    comm.Parameters.Add(p_IMG_TYPICAL_FLOOR_PLAN_SIZE);
                    //comm.Parameters.Add(p_IMG_ELEVATION);
                    //comm.Parameters.Add(p_IMG_ELEVATION_SIZE);
                    comm.Parameters.Add(p_IMG_SECTION);
                    comm.Parameters.Add(p_IMG_SECTION_SIZE);
                    //comm.Parameters.Add(p_DWG_PLANS);
                    //comm.Parameters.Add(p_DWG_PLANS_SIZE);
                    comm.Parameters.Add(p_FRST_REGISTER_ID);


                    int cntResult = comm.ExecuteNonQuery();
                    if (cntResult > 0)
                        Rhino.RhinoApp.WriteLine("설계보고서 업로드 완료");
                    else
                    {
                        MessageBox.Show(cntResult.ToString());

                    }



                }
                catch (OracleException ex)
                {
                    Rhino.RhinoApp.WriteLine("설계보고서 업로드 실패");
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

        public static string GetPlotBoundaryVector(List<Point3d> plotBoundary)
        {
            string output = "";

            for (int i = 0; i < plotBoundary.Count(); i++)
            {
                if (i != 0)
                    output += "/";

                output += plotBoundary[i].X.ToString() + "," + plotBoundary[i].Y.ToString() + "," + plotBoundary[i].Z.ToString();
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

        public static string getDirection(Vector3d yDirection)
        {
            Vector3d yDirectionCopy = new Vector3d(yDirection);
            yDirectionCopy.Reverse();

            string[] directionString = { "EE", "NE", "NN", "NW", "WW", "SW", "SS", "SE" };

            double angle = vectorAngle(yDirectionCopy);

            return directionString[(int)(((angle + Math.PI / 8) - (angle + Math.PI / 8) % (Math.PI / 4)) / (Math.PI / 4))];
        }

        public static void AddTdDesignArea(List<string> idColumnName, List<string> idColumnCode, int REGI_PRE_DESIGN_NO, string userID, string typeString, double CORE_AREA, double WELFARE_AREA, double FACILITIES_AREA, double PARKINGLOT_AREA, double PLOT_SHARE_AREA, HouseholdStatistics houseHoldStatistic)
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

                    double sup = Math.Round((CORE_AREA + houseHoldStatistic.GetWallArea() + houseHoldStatistic.GetExclusiveArea()) / 1000000, 2);

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
                    p_TYPE_SIZE.Value = Math.Round(sup * 0.3025, 0).ToString();
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
                    p_TYPE_EXCLUSIVE_AREA.Value = Math.Round(houseHoldStatistic.GetExclusiveArea() / 1000000, 2).ToString();
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
                    p_TYPE_SUPPLY_AREA.Value = sup.ToString();
                    p_TYPE_SUPPLY_AREA.ParameterName = "p_TYPE_SUPPLY_AREA";

                    p_TYPE_WELFARE_AREA.DbType = System.Data.DbType.Decimal;
                    p_TYPE_WELFARE_AREA.Value = WELFARE_AREA;
                    p_TYPE_WELFARE_AREA.ParameterName = "p_TYPE_WELFARE_AREA";

                    p_TYPE_FACILITIES_AREA.DbType = System.Data.DbType.Decimal;
                    p_TYPE_FACILITIES_AREA.Value = FACILITIES_AREA;
                    p_TYPE_FACILITIES_AREA.ParameterName = "p_TYPE_FACILITIES_AREA";

                    p_TYPE_PARKINGLOT_AREA.DbType = System.Data.DbType.Decimal;
                    p_TYPE_PARKINGLOT_AREA.Value = Math.Round(PARKINGLOT_AREA / 1000000, 2).ToString();
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

        public static void AddDesignDetail(List<string> idColumnName, List<string> idColumnCode, string userID, Apartment agOutput, out int REGI_PRE_DESIGN_NO)
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
                    p_BUILDING_SCALE.Value = GetBuildingScale(Math.Max((int)agOutput.ParameterSet.Parameters[0], (int)agOutput.ParameterSet.Parameters[1]), agOutput.ParkingLotUnderGround.Floors);
                    p_BUILDING_SCALE.ParameterName = "p_BUILDING_SCALE";

                    p_STRUCTURE.DbType = System.Data.DbType.String;
                    p_STRUCTURE.Value = "철근콘크리트 구조";
                    p_STRUCTURE.ParameterName = "p_STRUCTURE";

                    p_BUILDING_AREA.DbType = System.Data.DbType.String;
                    p_BUILDING_AREA.Value = Math.Round(agOutput.GetBuildingArea() / 1000000, 2).ToString();
                    p_BUILDING_AREA.ParameterName = "p_BUILDING_AREA";

                    p_FLOOR_AREA_UG.DbType = System.Data.DbType.Decimal;
                    p_FLOOR_AREA_UG.Value = Math.Round(agOutput.ParkingLotUnderGround.ParkingArea / 1000000, 2).ToString();
                    p_FLOOR_AREA_UG.ParameterName = "p_FLOOR_AREA_UG";

                    p_FLOOR_AREA_G.DbType = System.Data.DbType.Decimal;
                    p_FLOOR_AREA_G.Value = Math.Round(agOutput.GetGrossArea() / 1000000, 2).ToString();
                    p_FLOOR_AREA_G.ParameterName = "p_FLOOR_AREA_G";

                    p_FLOOR_AREA_WHOLE.DbType = System.Data.DbType.Decimal;
                    p_FLOOR_AREA_WHOLE.Value = Math.Round(agOutput.GetGrossArea() / 1000000, 2).ToString();
                    p_FLOOR_AREA_WHOLE.ParameterName = "p_FLOOR_AREA_WHOLE";

                    p_STORIES_UNDERGROUND.DbType = System.Data.DbType.Decimal;
                    p_STORIES_UNDERGROUND.Value = agOutput.ParkingLotUnderGround.Floors.ToString();
                    p_STORIES_UNDERGROUND.ParameterName = "p_STORIES_UNDERGROUND";

                    p_STORIES_ON_EARTH.DbType = System.Data.DbType.Decimal;
                    p_STORIES_ON_EARTH.Value = (Math.Max((int)agOutput.ParameterSet.Parameters[0], (int)agOutput.ParameterSet.Parameters[1]) + 1).ToString();
                    p_STORIES_ON_EARTH.ParameterName = "p_STORIES_ON_EARTH";

                    p_BALCONY_AREA.DbType = System.Data.DbType.Decimal;
                    p_BALCONY_AREA.Value = Math.Round(agOutput.GetBalconyArea() / 1000000, 2).ToString();
                    p_BALCONY_AREA.ParameterName = "p_BALCONY_AREA";

                    p_PARKING_ROOFTOP_AREA.DbType = System.Data.DbType.Decimal;
                    p_PARKING_ROOFTOP_AREA.Value = Math.Round((agOutput.GetCoreAreaOnEarthSum() + agOutput.ParkingLotUnderGround.ParkingArea) / 1000000, 2).ToString();
                    p_PARKING_ROOFTOP_AREA.ParameterName = "p_PARKING_ROOFTOP_AREA";

                    p_FLOOR_AREA_CONSTRUCTION.DbType = System.Data.DbType.Decimal;
                    p_FLOOR_AREA_CONSTRUCTION.Value = Math.Round((agOutput.GetGrossArea() + agOutput.GetBalconyArea() + agOutput.GetCoreAreaOnEarthSum() + agOutput.ParkingLotUnderGround.ParkingArea) / 1000000, 2).ToString();
                    p_FLOOR_AREA_CONSTRUCTION.ParameterName = "p_FLOOR_AREA_CONSTRUCTION";

                    p_BUILDING_COVERAGE.DbType = System.Data.DbType.Decimal;
                    p_BUILDING_COVERAGE.Value = Math.Round(agOutput.GetBuildingCoverage(), 2).ToString();
                    p_BUILDING_COVERAGE.ParameterName = "p_BUILDING_COVERAGE";

                    p_GROSS_AREA_RATIO.DbType = System.Data.DbType.Decimal;
                    p_GROSS_AREA_RATIO.Value = Math.Round(agOutput.GetGrossAreaRatio(), 2).ToString();
                    p_GROSS_AREA_RATIO.ParameterName = "p_GROSS_AREA_RATIO";

                    p_HOUSEHOLD_COUNT.DbType = System.Data.DbType.Decimal;
                    p_HOUSEHOLD_COUNT.Value = agOutput.GetHouseholdCount().ToString();
                    p_HOUSEHOLD_COUNT.ParameterName = "p_HOUSEHOLD_COUNT";

                    p_PARKINGLOT_COUNT.DbType = System.Data.DbType.Decimal;
                    p_PARKINGLOT_COUNT.Value = (agOutput.ParkingLotOnEarth.GetCount() + agOutput.ParkingLotUnderGround.Count).ToString();
                    p_PARKINGLOT_COUNT.ParameterName = "p_PARKINGLOT_COUNT";

                    p_PARKINGLOT_AREA.DbType = System.Data.DbType.Decimal;
                    p_PARKINGLOT_AREA.Value = Math.Round(agOutput.ParkingLotUnderGround.ParkingArea / 1000000, 2).ToString();
                    p_PARKINGLOT_AREA.ParameterName = "p_PARKINGLOT_AREA";

                    p_PARKINGLOT_COUNT_LEGAL.DbType = System.Data.DbType.Decimal;
                    p_PARKINGLOT_COUNT_LEGAL.Value = (agOutput.GetLegalParkingLotOfCommercial() + agOutput.GetLegalParkingLotofHousing()).ToString();
                    p_PARKINGLOT_COUNT_LEGAL.ParameterName = "p_PARKINGLOT_COUNT_LEGAL";

                    p_LANDSCAPE_AREA.DbType = System.Data.DbType.Decimal;
                    p_LANDSCAPE_AREA.Value = Math.Round(agOutput.GetGreenArea() / 1000000, 2).ToString();
                    p_LANDSCAPE_AREA.ParameterName = "p_LANDSCAPE_AREA";

                    p_LANDSCAPE_AREA_LEGAL.DbType = System.Data.DbType.Decimal;
                    p_LANDSCAPE_AREA_LEGAL.Value = Math.Round(agOutput.CalculateLegalGreen() / 1000000, 2).ToString();
                    p_LANDSCAPE_AREA_LEGAL.ParameterName = "p_LANDSCAPE_AREA_LEGAL";

                    p_NEIGHBOR_STORE_AREA.DbType = System.Data.DbType.Decimal;
                    p_NEIGHBOR_STORE_AREA.Value = Math.Round(agOutput.GetCommercialArea() / 1000000, 2).ToString();
                    p_NEIGHBOR_STORE_AREA.ParameterName = "p_NEIGHBOR_STORE_AREA";

                    p_PUBLIC_FACILITY_AREA.DbType = System.Data.DbType.Decimal;
                    p_PUBLIC_FACILITY_AREA.Value = Math.Round(agOutput.GetPublicFacilityArea() / 1000000, 2).ToString();
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

        public static void AddDesignNonResidential(List<string> idColumnName, List<string> idColumnCode, string userID, int REGI_PRE_DESIGN_NO, string USE_CD, double NONRESI_EXCLUSIVE_AREA, double NONRESI_COMMON_USE_AREA, double NONRESI_PARKING_AREA, int NONRESI_LEGAL_PARKING, double NONRESI_PLOT_SHARE_AREA)
        {
            string sql = "INSERT INTO TD_DESIGN_NONRESIDENTIAL(REGI_MST_NO,REGI_SUB_MST_NO,REGI_PRE_DESIGN_NO,NONRESI_USE_CD,NONRESI_EXCLUSIVE_AREA,NONRESI_COMMON_USE_AREA,NONRESI_SUPPLY_AREA,NONRESI_PARKING_AREA,NONRESI_CONTRACT_AREA,NONRESI_LEAGAL_PARKING,NONRESI_PLOT_SHARE_AREA,FRST_REGIST_DT,FRST_REGISTER_ID) VALUES(:p_REGI_MST_NO,:p_REGI_SUB_MST_NO,:p_REGI_PRE_DESIGN_NO,:p_NONRESI_USE_CD,:p_NONRESI_EXCLUSIVE_AREA,:p_NONRESI_COMMON_USE_AREA,:p_NONRESI_SUPPLY_AREA,:p_NONRESI_PARKING_AREA,:p_NONRESI_CONTRACT_AREA,:p_NONRESI_LEAGAL_PARKING,:p_NONRESI_PLOT_SHARE_AREA,SYSDATE ,:p_FRST_REGISTER_ID)";

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
                    p_NONRESI_EXCLUSIVE_AREA.Value = Math.Round(NONRESI_EXCLUSIVE_AREA / 1000000, 2).ToString();
                    p_NONRESI_EXCLUSIVE_AREA.ParameterName = "p_NONRESI_EXCLUSIVE_AREA";

                    p_NONRESI_COMMON_USE_AREA.DbType = System.Data.DbType.Decimal;
                    p_NONRESI_COMMON_USE_AREA.Value = Math.Round(NONRESI_COMMON_USE_AREA / 1000000, 2).ToString();
                    p_NONRESI_COMMON_USE_AREA.ParameterName = "p_NONRESI_COMMON_USE_AREA";

                    p_NONRESI_SUPPLY_AREA.DbType = System.Data.DbType.Decimal;
                    p_NONRESI_SUPPLY_AREA.Value = Math.Round((NONRESI_EXCLUSIVE_AREA + NONRESI_COMMON_USE_AREA) / 1000000, 2).ToString();
                    p_NONRESI_SUPPLY_AREA.ParameterName = "p_NONRESI_SUPPLY_AREA";

                    p_NONRESI_PARKING_AREA.DbType = System.Data.DbType.Decimal;
                    p_NONRESI_PARKING_AREA.Value = Math.Round(NONRESI_PARKING_AREA / 1000000, 2).ToString();
                    p_NONRESI_PARKING_AREA.ParameterName = "p_NONRESI_PARKING_AREA";

                    p_NONRESI_CONTRACT_AREA.DbType = System.Data.DbType.Decimal;
                    p_NONRESI_CONTRACT_AREA.Value = Math.Round((NONRESI_EXCLUSIVE_AREA + NONRESI_COMMON_USE_AREA + NONRESI_PARKING_AREA) / 1000000, 2).ToString();
                    p_NONRESI_CONTRACT_AREA.ParameterName = "p_NONRESI_CONTRACT_AREA";

                    p_NONRESI_LEGAL_PARKING.DbType = System.Data.DbType.Decimal;
                    p_NONRESI_LEGAL_PARKING.Value = NONRESI_LEGAL_PARKING.ToString();
                    p_NONRESI_LEGAL_PARKING.ParameterName = "p_NONRESI_LEAGAL_PARKING";

                    p_NONRESI_PLOT_SHARE_AREA.DbType = System.Data.DbType.Decimal;
                    p_NONRESI_PLOT_SHARE_AREA.Value = Math.Round(NONRESI_PLOT_SHARE_AREA / 1000000, 2).ToString();
                    p_NONRESI_PLOT_SHARE_AREA.ParameterName = "p_NONRESI_PLOT_SHARE_AREA";

                    p_FRST_REGISTER_ID.DbType = System.Data.DbType.String;
                    p_FRST_REGISTER_ID.Value = userID;
                    p_FRST_REGISTER_ID.ParameterName = "p_FRST_REGISTER_ID";

                    comm.Parameters.Add(p_REGI_MST_NO);
                    comm.Parameters.Add(p_REGI_SUB_MST_NO);
                    comm.Parameters.Add(p_REGI_PRE_DESIGN_NO);
                    comm.Parameters.Add(p_NONRESI_USE_CD);
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
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        public static void AddDesignReport(List<string> idColumnName, List<string> idColumnCode, int REGI_PRE_DESIGN_NO, List<string> FilePath, List<string> RowName)
        {
            List<string> idColumnNameCopy = new List<string>(idColumnName);
            List<string> idColumnCodeCopy = new List<string>(idColumnCode);

            idColumnNameCopy.Add("REGI_PRE_DESIGN_NO");
            idColumnCodeCopy.Add(REGI_PRE_DESIGN_NO.ToString());

            for (int i = 0; i < FilePath.Count(); i++)
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
    }
}
