using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rhino;
using System.IO;
namespace TuringAndCorbusier.Utility
{
    class TimeCheck // logger
    {
        System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
        string funcName = "";
        double lastCheck = 0;
        bool logOn = false;
        string url = @"C://Users//user//Desktop//test//AG1Timelog.txt";
       

        public TimeCheck(string funcName, bool logMode)
        {
            this.funcName = funcName;
            logOn = logMode;
            if (logOn == true)
            {
                //for test 
                //
                try
                {
                    DirectoryInfo dinfo = new DirectoryInfo(url);
                    string filename = url;
                    System.IO.FileStream fs = new FileStream(filename, System.IO.FileMode.Append);
                    fs.Close();
                    fs.Dispose();
                }
                catch (Exception e)
                {
                    RhinoApp.WriteLine(e.Message);
                    logOn = false;
                }
                //w.WriteLine(column);
            }

        }

        public void Write(string txt)
        {
            using (FileStream fs = new FileStream(url, FileMode.Append))
            {
                using (StreamWriter writer = new StreamWriter(fs))
                {
                    writer.Write(txt);
                    writer.WriteLine();
                }
            }
        }

        public void Start()
        {
            string start = string.Format("{0} 시작 : {1}", funcName, System.DateTime.Now.ToShortTimeString());
            if (logOn)
            {
                Write(start);
            }
            else
            {
                RhinoApp.WriteLine(start);
            }
            watch.Start();
        }

        public void Check(string checkPoint)
        {
            double elapsedFromLastCheck = watch.ElapsedMilliseconds - lastCheck;
            lastCheck = watch.ElapsedMilliseconds;

            string checkString = string.Format("{0} 체크 : {1} ms 경과", checkPoint, elapsedFromLastCheck);

            if (logOn)
            {
                Write(checkString);
            }
            else
            {
                RhinoApp.WriteLine(checkString);
            }
        }

        public void End()
        {
            watch.Stop();
            string wholeTime = string.Format("{0} 총 걸린 시간 : {1}", funcName, watch.ElapsedMilliseconds);
            string finish = string.Format("{0} 완료 : {1}", funcName, System.DateTime.Now.ToShortTimeString());
            
            if (logOn)
            {
                Write(wholeTime);
                Write(finish);
            }
            else
            {
                RhinoApp.WriteLine(wholeTime);
                RhinoApp.WriteLine(finish);
            }

            watch.Reset();
        }
    }
}
