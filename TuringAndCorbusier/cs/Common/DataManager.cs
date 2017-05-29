using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rhino.Geometry;
using System.IO;
using System.Xml.Serialization;
namespace TuringAndCorbusier
{
    public class DataManager
    {
        public string savepath = @"C:\Program Files (x86)\Boundless\TuringAndCorbusier\DataBase\save";
        
        public DataManager()
        {
            DirectoryInfo dir = new DirectoryInfo(savepath);
            if (!dir.Exists)
                dir.Create();
        }

        public void SaveData(ProjectData data)
        {
            DirectoryInfo dir = new DirectoryInfo(savepath);
            var files = dir.GetFiles();
            var thisprojectname = files.Where(n => n.Name.Contains(data.projectName));

            if (thisprojectname.Count() != 0)
            {
                //해당이름의파일이이미존재
            }

            else
            {
                //해당 파일이 없으면 txt파일 생성
                
                FileStream fs = new FileStream(savepath + @"\" + data.projectName + ".txt", FileMode.CreateNew);
                StreamWriter w = new StreamWriter(fs);
                w.Write(Helper.Serialize<ProjectData>(data));
                w.Close();
                w.Dispose();
                fs.Close();
                fs.Dispose();
            }
        }

        public ProjectData LoadData(string projectName)
        {
            ProjectData result = null;
            
            FileStream fs = new FileStream(projectName,FileMode.Open);
            StreamReader r = new StreamReader(fs);
            result = Helper.Desirialize<ProjectData>(r.ReadToEnd());
            r.Close();
            r.Dispose();
            fs.Close();
            fs.Dispose();
          
            return result;
        }

    }

    [Serializable]
    public class ProjectData
    {
        public string projectName { get; set; }
        public SerializablePlot plot { get; set; }
        public Datastructure_Settings.Settings_Page1 setting1 { get; set; }
        public Datastructure_Settings.RegulationSettings regsetting { get; set; }
        //public Datastructure_Settings.Settings_Page2 setting2 { get; set; }
        public ProjectData()
        {

        }
    }

    public class SerializablePlot
    {
        //plot
        public bool ignoreNorth;
        public bool isSpecialCase;
        public int plotType;
        public List<Point3d> boundary;
        public int[] surroundings;
        public List<Point3d> outrect;
        public List<Point3d> simplifiedBoundary;
        public int[] simplifiedSurroundings;
        public List<List<Point3d>> layout = new List<List<Point3d>>();
        public List<Point3d> originalBoundary;
        public double[] originalSurroundings;

        public SerializablePlot()
        { }

        public SerializablePlot(Plot toCopy, KDGinfo kdg)
        {
            plotType = (int)toCopy.PlotType;
            boundary = toCopy.Boundary?.DuplicateSegments().Select(n => n.PointAtStart).ToList();
            surroundings = toCopy.Surroundings;
            outrect = kdg.outrect?.DuplicateSegments().Select(n => n.PointAtStart).ToList();
            simplifiedSurroundings = toCopy.SimplifiedSurroundings;
            simplifiedBoundary = toCopy.SimplifiedBoundary?.DuplicateSegments().Select(n => n.PointAtStart).ToList();
            ignoreNorth = toCopy.ignoreNorth;
            isSpecialCase = toCopy.isSpecialCase;
            var x = kdg.surrbuildings[0];
            kdg.surrbuildings?.ForEach(n => layout.Add(OpenCurveToPoint(n)));

        }

        public Plot ToPlot()
        {
            Plot plot = new Plot();
            plot.ignoreNorth = ignoreNorth;
            plot.isSpecialCase = isSpecialCase;
            plot.PlotType = (PlotType)plotType;
            Curve tempcurve = new Polyline(Closed(boundary)).ToNurbsCurve();
            if (tempcurve.ClosedCurveOrientation(Vector3d.ZAxis) == CurveOrientation.Clockwise)
                tempcurve.Reverse();
            plot.Boundary = tempcurve;

            plot.Surroundings = surroundings;
            plot.outrect = new Polyline(Closed(outrect)).ToNurbsCurve();
            plot.SimplifiedBoundary = new Polyline(Closed(simplifiedBoundary)).ToNurbsCurve();
            plot.SimplifiedSurroundings = simplifiedSurroundings;
            plot.layout = layout.Select(n => new PolylineCurve(n) as Curve).ToList();

            plot.OriginalBoundary = new Polyline(Closed(originalBoundary)).ToNurbsCurve();
            if (originalSurroundings != null)
                plot.OriginalRoadwidths = originalSurroundings.ToList();
            return plot;
        }

        public List<Point3d> OpenCurveToPoint(Curve c)
        {

            List<Point3d> r = c.DuplicateSegments().Select(n => n.PointAtStart).ToList();
            r.Add(c.PointAtEnd);

            if (r.Count < 2)
            {
                r.Clear();
                r.Add(c.PointAtStart);
                r.Add(c.PointAtEnd);
            }
            return r;
        }

        public List<Point3d> Closed(List<Point3d> p)
        {
            if (p == null)
                return new List<Point3d>();

            if (p.Count == 0)
                return new List<Point3d>();

            List<Point3d> closed = new List<Point3d>(p);
            closed.Add(p[0]);
            return closed;
        }


    }


    public static class Helper
    {
        public static string Serialize<T>(this T toSerialize)
        {
            XmlSerializer xml = new XmlSerializer(typeof(T));
            StringWriter writer = new StringWriter();
            xml.Serialize(writer, toSerialize);
            return writer.ToString();
        }

        public static T Desirialize<T>(this string toDeserialize)
        {
            XmlSerializer xml = new XmlSerializer(typeof(T));
            StringReader reader = new StringReader(toDeserialize);
            return (T)xml.Deserialize(reader);

            
        }

        public static void Add(this Rhino.Collections.ArchivableDictionary d, System.Object obj)
        {

        }
    }

}
