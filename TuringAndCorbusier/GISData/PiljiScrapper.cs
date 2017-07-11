using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rhino.Geometry;
using GISData.DataStruct;
using Rhino;
using TuringAndCorbusier.Utility;
using System.Data.SqlTypes;
using System.Data.SqlClient;
using System.Threading;
using Microsoft.SqlServer.Types;
using System.Text.RegularExpressions;

namespace GISData.Extract
{

    //testing

    public delegate void OnFirstPiljiScrapped();
    public delegate void OnPiljiScrapped(Pilji p);
    public delegate void OnScrapFinished();

    public class PiljiScrapper
    {
        public OnPiljiScrapped scrapEvent = null;
        public OnScrapFinished finishEvent = null;
        public OnFirstPiljiScrapped firstEvent = null;

        string conStr = ConnectionStringBuilder.GetConnectionString(SERVER.Azure);
        string queryStr = "";


        private object lockObj = new object();
        Queue<string[]> works = new Queue<string[]>();
        //List<Pilji> piljis = new List<Pilji>();

        //외부 개입 가능성
        public bool ForcedFinish = false;

        bool finished = false;

        public PiljiScrapper(string key)
        {
            queryStr = QueryBuilder.GetPiljiDataWithDongCode(key);
        }

        public void Run()
        {
            Thread scrapper = new Thread(new ThreadStart(Scrap));
            Thread packer = new Thread(new ThreadStart(Pack));
            scrapper.Start();
            packer.Start();
        }

        private void Scrap()
        {
            using (SqlConnection conn = new SqlConnection(conStr))
            {
                conn.Open();
                using (SqlCommand comm = new SqlCommand(queryStr, conn))
                {
                    using (var reader = comm.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            if (ForcedFinish)
                                break;
                            try
                            {
                                //enqueue here
                                lock (lockObj)
                                {
                                    string[] data = { reader["A5"].ToString(), reader["A0"].ToString(), reader["A7"].ToString(), reader["geom"].ToString() };
                                    works.Enqueue(data);
                                }
                            }
                            catch (Exception e)
                            {
                                System.Windows.MessageBox.Show(e.Message);
                            }
                        }

                    }
                }

                finished = true;
            }

        }

        private void Pack()
        {
            while (true)
            {
                lock (lockObj)
                {
                    if (works.Count > 0)
                    {
                        

                        var data = works.Dequeue();
                        var pilji = ConvertToGaroPilji(data);
                        //piljis.Add(pilji);
                        scrapEvent?.Invoke(pilji);

                        //한번 실행하고 맘
                        firstEvent?.Invoke();
                        if (firstEvent != null)
                            firstEvent = null;
                    }

                    if ((works.Count == 0 && finished)||ForcedFinish)
                    {
                        break;
                    }
                }
            }

            Thread.Sleep(1000);
            finishEvent?.Invoke();
        }

        private Pilji ConvertToGaroPilji(string[] data)
        {
            string s = data[0];
            string code = data[1];
            string jimok = data[2];
            string usage = "알수없음";
            //SqlGeometry geo = rdr["geom"] as SqlGeometry;   // this is null.
            //var check = rdr["geom"];    // type is geodata...??
            string checkString = data[3];    // geometry expressed as string, as expected. use this one.
                                                            //RhinoApp.WriteLine(s);
            var outlineParsed = ParseTextToGeometry(checkString);
            var piljimult = new MultiPolygon(outlineParsed);
            Pilji temp = new Pilji(piljimult, code, s);
            temp.Jimok = jimok;
            temp.Usage = usage;

            //RhinoApp.WriteLine(s);

            return temp;
        }

        private List<Polygon> ParseTextToGeometry(string sqlShapeString)
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
