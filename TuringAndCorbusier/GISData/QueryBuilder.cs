using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;

namespace GISData
{
    public enum SERVER
    {
        seoulGIS = 0,
        GIS_170125 = 1,
        JOSH_LOCAL = 2,
        Azure = 3
    }
    
    public enum AZURE_Mssql_GIS_Tables_shp
    {
        //특별시
        seoul_shp = 11,

        //광역시
        busan_shp = 26,
        daegu_shp = 27,
        incheon_shp = 28,
        kwangju_shp = 29,
        daejun_shp = 30,
        ulsan_shp = 31,

        //특별자치시
        sejong_shp = 36,

        //도
        kyeongki_shp = 41,
        kangwon_shp = 42,
        n_chungcheong_shp = 43,
        s_chungcheong_shp = 44,
        n_junla_shp = 45,
        s_junla_shp = 46,
        n_kyeongsang_shp = 47,
        s_kyeongsang_shp = 48,

        //특별자치도
        jeju_shp = 50,
       
        Count = 17
    }

    public enum AZURE_Mssql_GIS_Tables_use
    {
        //특별시
        seoul_landuse = 11,

        //광역시
        busan_landuse = 26,
        daegu_landuse = 27,
        incheon_landuse = 28,
        kwangju_landuse = 29,
        daejun_landuse = 30,
        ulsan_landuse = 31,

        //특별자치시
        sejong_landuse = 36,

        //도
        kyeongki_landuse = 41,
        kangwon_landuse = 42,
        n_chungcheong_landuse = 43,
        s_chungcheong_landuse = 44,
        n_junla_landuse = 45,
        s_junla_landuse = 46,
        n_kyeongsang_landuse = 47,
        s_kyeongsang_landuse = 48,

        //특별자치도
        jeju_landuse = 50,

        Count = 17
    }

    //enum tostring 빙구라서
    


    public class ConnectionStringBuilder
    {
        public static string GetConnectionString(SERVER server)
        {
            string con = "";
            if ((int)server == 0)
            {
                con = @"Data Source=demo.crevisse.com,10801;Initial Catalog=seoulGIS;USER ID=sa;PASSWORD=building39!";
            }
            else if ((int)server == 1)
            {
                con = @"Data Source=demo.crevisse.com,10801;Initial Catalog=GIS_170125;USER ID=sa;PASSWORD=building39!";
            }
            else if ((int)server == 2)
            {
                con = @"Data Source = DESKTOP-TV5U6PH\SQLEXPRESS;Initial Catalog=testDB;Integrated Security=True";
            }
            else if ((int)server == 3)
            {
                con = @"Data Source=spwk-vm.koreacentral.cloudapp.azure.com,1433;Initial Catalog=mssql_RoK_GIS_05302017;USER ID=spwk_dvlp;PASSWORD=building39!";
            }
            return con;
        }
    }


    public class QueryBuilder
    {
        public static string GetServerName(AZURE_Mssql_GIS_Tables_shp e)
        {
            switch ((int)e)
            {
                case 11:
                    return "seoul_shp";
                case 26:
                    return "busan_shp";
                case 27:
                    return "daegu_shp";
                case 28:
                    return "incheon_shp";
                case 29:
                    return "kwangju_shp";
                case 30:
                    return "daejun_shp";
                case 31:
                    return "ulsan_shp";

                case 36:
                    return "sejong_shp";

                case 41:
                    return "kyeongki_shp";
                case 42:
                    return "kangwon_shp";
                case 43:
                    return "n_chungcheong_shp";
                case 44:
                    return "s_chungcheong_shp";
                case 45:
                    return "n_junla_shp";
                case 46:
                    return "s_junla_shp";
                case 47:
                    return "n_kyeongsang_shp";
                case 48:
                    return "s_kyeongsang_shp";


                case 50:
                    return "jeju_shp";

                default:
                    return "";
            }

        }



        public static string GetSiDoCode()
        {
            return "select * from mssql_GIS.dbo.sido_20170528";
        }
        public static string GetGuDataWithSiCode(string sicode, SERVER server)
        {
            string DBname = server == SERVER.JOSH_LOCAL ? "testDB" : "seoulGIS";
            return "select * from " + DBname + ".dbo.F00_ADM_SIGUNGU where ADM_SECT_C like '" + sicode + "%'";
        }

        public static string GetDongDataWithGuCode(string gucode, SERVER server)
        {
            string DBname = server == SERVER.JOSH_LOCAL ? "testDB" : "seoulGIS";
            return "select * from " + DBname + ".dbo.F00_ADM_LEGALEMD where EMD_CD like '" + gucode + "%'";
        }

        public static string GetPiljiDataWithGuCode(string gucode, SERVER server)
        {
            string DBname = server == SERVER.JOSH_LOCAL ? "testDB" : "seoulGIS";
            string si = gucode.Substring(0, 2);
            return "SELECT * " +
           "FROM " + DBname + ".dbo.APMM_NV_LAND_2016_" + si + "_01 INNER JOIN " + DBname + ".dbo.LSMD_CONT_LDREG_" + si + "_201701 " +
           "ON " + DBname + ".dbo.APMM_NV_LAND_2016_" + si + "_01.PNU = " + DBname + ".dbo.LSMD_CONT_LDREG_" + si + "_201701.PNU " +
           "WHERE " + DBname + ".dbo.APMM_NV_LAND_2016_" + si + "_01.PNU LIKE '" + gucode + "%'";

        }

        public static string GetPiljiDataWithDongCode(string dongcode)
        {
            string query = "";

            string sido = dongcode.Substring(0, 2);
            AZURE_Mssql_GIS_Tables_shp db = (AZURE_Mssql_GIS_Tables_shp)int.Parse(sido);

            query = "SELECT * FROM mssql_RoK_GIS_05302017.dbo." + GetServerName(db) + " WHERE A0 LIKE '" + dongcode + "%' " ;

            return query;
        }

        public static string GetBuildingDataWidthGuCode(string gucode, SERVER server)
        {
            string DBname = server == SERVER.JOSH_LOCAL ? "testDB" : "seoulGIS";
            string sicode = gucode.Substring(0, 2);
            string groupname = (sicode == "11") ? "Seoul" : "Gyeonggi";

            return
                "select d.PNU, d.[Group] as Grp, a.JIMOK,a.SPFC1,b.Bgeom,b.USEAPR_DAY,b.ARCHAREA,b.TOTALAREA,c.JIBUN,c.Pgeom from dbo.apmms" + sicode + " as a, dbo.bldgs" + sicode + " as b,dbo.lsmds" + sicode + " as c,dbo." + groupname + "PiljiGroup as d where d.PNU = a.PNU and d.PNU = b.PNU and d.PNU = c.PNU and d.PNU like '" + gucode + "%'";

        }

        public static string GetGuOutline(string gucode, SERVER server)
        {
            string DBname = server == SERVER.JOSH_LOCAL ? "testDB" : "seoulGIS";
            return "select a.geom from " + DBname + ".dbo.F00_ADM_SIGUNGU as a where a.ADM_SECT_C like '" + gucode + "%'";
        }
    }
}
