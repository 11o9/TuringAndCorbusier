using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using GISData.DataStruct;
using Microsoft.SqlServer.Types;
using System.Text.RegularExpressions;
using Rhino;
using Rhino.Geometry;
namespace GISData
{
    public class ServerConnection
    {
        public static void WriteLog(string code, string date)
        {
            using (SqlConnection con = new SqlConnection(ConnectionStringBuilder.GetConnectionString(SERVER.Azure)))
            {
                con.Open();
            
                using (SqlCommand comm = new SqlCommand("use mssql_System insert into dbo.PiljiLog(Code,UseDate) values ('" + code + "', '" + date + "')",con))
                {
                    comm.ExecuteNonQuery();
                }
            }
       
        }

        public static bool TestConnection(SERVER server)
        {
            SqlConnection con = new SqlConnection(ConnectionStringBuilder.GetConnectionString(server));
            try
            {
                con.Open();
            }
            catch (Exception e)
            {
                return false;
            }
            con.Close();
            con.Dispose();
            return true;
        }

        private static T ParseData<T>(SqlDataReader rdr)
        {
            dynamic myclass = null;
           
        
            if (typeof(T) == typeof(NameCode))
            {
                myclass = new NameCode(rdr[1].ToString(), rdr[0].ToString().Replace(" ",""));
                return myclass;
            }

            else if (typeof(T) == typeof(Building))
            {
                myclass = ConverToBuilding(rdr);
            }

            //ㅠㅠ임시로 column 숫자로 체크
            else if (typeof(T) == typeof(Pilji))
            {
                if (rdr.FieldCount == 12)
                {
                    myclass = ConvertToGaroPilji(rdr);
                    return myclass;
                }
                else
                {
                    myclass = ConvertToPilji(rdr);
                    return myclass;
                }
            }
            else if (typeof(T) == typeof(MultiPolygon))
            {
                var mp = ParseTextToGeometry(rdr["geom"].ToString());
                myclass = new MultiPolygon(mp);
            }
            return myclass;
        }


        public enum DATATYPE
        {
            SI,
            GU,
            DONG,
            PILJI

        }

        public static List<T> MetaCon<T>(DATATYPE type,string key)
        {
            string con, query;
            if (type == DATATYPE.GU)
            {
                con = ConnectionStringBuilder.GetConnectionString(SERVER.Azure);
                query = QueryBuilder.GetGuDataWithSiCode(key, SERVER.Azure);
            }
            else if (type == DATATYPE.DONG)
            {
                con = ConnectionStringBuilder.GetConnectionString(SERVER.Azure);
                query = QueryBuilder.GetDongDataWithGuCode(key, SERVER.Azure);
            }
            else if (type == DATATYPE.PILJI)
            {
                con = ConnectionStringBuilder.GetConnectionString(SERVER.Azure);
                query = QueryBuilder.GetPiljiDataWithDongCode(key);
            }
            else if (type == DATATYPE.SI)
            {
                con = ConnectionStringBuilder.GetConnectionString(SERVER.Azure);
                query = QueryBuilder.GetSiDoCode();
            }
            else
            {
                return new List<T>();
            }

            try
            {

                List<T> result = new List<T>();
                using (SqlConnection conn = new SqlConnection(con))
                {
                    conn.Open();
                    SqlCommand cmd = new SqlCommand();
                    cmd.Connection = conn;
                    cmd.CommandText = query;
                    cmd.CommandTimeout = 30;
                    using (SqlDataReader rdr = cmd.ExecuteReader())
                    {
                        try
                        {
                            while (rdr.Read())
                            {
                                try
                                {
                                    T x = ParseData<T>(rdr);
                                    result.Add(x);
                                }
                                catch (Exception e)
                                {
                                    //RhinoApp.WriteLine(e.Message);
                                }
                            }
                        }
                        catch (Exception e)
                        {

                        }
                    }
                }
                return result;
            }
            catch (Exception e)
            {
                Rhino.RhinoApp.WriteLine(query);
                return new List<T>();
            }

            
        }

        private static Pilji ConvertToPilji(SqlDataReader rdr)
        {
            string s = rdr["JIBUN"] as string;
            string code = rdr["PNU"] as string;
            string jimok = rdr["JIMOK"] as string;
            string usage = rdr["SPFC1"] as string;
            SqlGeometry geo = rdr["geom"] as SqlGeometry;   // this is null.
            var check = rdr["geom"];    // type is geodata...??
            string checkString = rdr["geom"].ToString();    // geometry expressed as string, as expected. use this one.
                                                            //RhinoApp.WriteLine(s);
            var outlineParsed = ParseTextToGeometry(checkString);
            var piljimult = new MultiPolygon(outlineParsed);
            Pilji temp = new Pilji(piljimult, code, s);
            temp.Jimok = jimok;
            temp.Usage = usage;

            //RhinoApp.WriteLine(s);

            return temp;
        }

        private static Pilji ConvertToGaroPilji(SqlDataReader rdr)
        {
            try
            {
                string s = rdr["StreetNum"] as string;
                string code = rdr["UniqueNum"] as string;
                string jimok = rdr["LotCategory"] as string;
                jimok = jimok.Substring(jimok.Length - 1, 1);
                string usage = "알수없음";
                SqlGeometry geo = rdr["geom"] as SqlGeometry;   // this is null.
                var check = rdr["geom"];    // type is geodata...??
                string checkString = rdr["geom"].ToString();    // geometry expressed as string, as expected. use this one.
                //RhinoApp.WriteLine(s);
                var outlineParsed = ParseTextToGeometry(checkString);
                var piljimult = new MultiPolygon(outlineParsed);
                Pilji temp = new Pilji(piljimult, code, s);
                temp.Jimok = jimok;
                temp.Usage = usage;

                //RhinoApp.WriteLine(s);

                return temp;
            }
            catch (Exception e)
            {
                RhinoApp.WriteLine(e.Message);
            }

            return null;

        }

        private static Building ConverToBuilding(SqlDataReader rdr)
        {
            string code = rdr["PNU"] as string;
            string day = rdr["USEAPR_DAY"] as string;
            string bcheckString = rdr["Bgeom"].ToString();    // geometry expressed as string, as expected. use this one.
            var archarea = double.Parse(rdr["ARCHAREA"].ToString());
            var totalarea = double.Parse(rdr["TOTALAREA"].ToString());
            var group = int.Parse(rdr["Grp"].ToString());
            //RhinoApp.WriteLine(s);
            var boutlineParsed = ParseTextToGeometry(bcheckString);
            var bmult = new MultiPolygon(boutlineParsed);

            Building b = new Building(day, bmult, archarea, totalarea, group);
            b.Code = code;
            b.Address = rdr["JIBUN"] as string;

            return b;
        }




        private static List<Polygon> ParseTextToGeometry(string sqlShapeString)
        {
            List<Polygon> outputGeometries = new List<Polygon>();
            char shapeImply = sqlShapeString.First();


            Regex PolygonSeivePattern = new Regex(@"\({2}\d.*?\d\){2}");
            Regex PartSeivePattern = new Regex(@"\d[^\(]*\d");
            MatchCollection PolygonCollection = PolygonSeivePattern.Matches(sqlShapeString);

            for (int i = 0; i < PolygonCollection.Count; i++)
            {
                Polygon currentPolygon = new Polygon();

                string currentPolygonString = PolygonCollection[i].Value;
                MatchCollection PartCollection = PartSeivePattern.Matches(currentPolygonString);

                for (int j = 0; j < PartCollection.Count; j++)
                {
                    List<Point3d> currentPartVertices = new List<Point3d>();

                    string currentPartString = PartCollection[j].Value;
                    string[] pointString = currentPartString.Split(',');

                    foreach (string k in pointString)
                    {
                        string[] xAndY = k.Split(' ');
                        double x, y;
                        double.TryParse(xAndY[xAndY.Length - 2], out x);
                        double.TryParse(xAndY[xAndY.Length - 1], out y);
                        currentPartVertices.Add(new Point3d(x * 1000, y * 1000, 0));
                    }

                    if (j == 0)
                        currentPolygon.OutBound = new Polyline(currentPartVertices);
                    else
                        currentPolygon.Subtractors.Add(new Polyline(currentPartVertices));
                }

                outputGeometries.Add(currentPolygon);
            }

            return outputGeometries;
        }
    }
}
