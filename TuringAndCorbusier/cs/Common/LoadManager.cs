using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xaml;

namespace TuringAndCorbusier
{

    [
    System.Runtime.InteropServices.Guid("A3B9EC3F-5A32-4E5B-947D-46753AE65D82"),
    Rhino.Commands.CommandStyle(Rhino.Commands.Style.ScriptRunner)
    ]
    public class TestCommand : Rhino.Commands.Command
    {
        bool is64 = Environment.Is64BitOperatingSystem;

        string dir64 = "C://Program Files (x86)//Boundless//TuringAndCorbusier//DataBase//cadastrals//";
        string dir32 = "C://Program Files//Boundless//TuringAndCorbusier//DataBase//cadastrals//";
        string dir = "";
        public string url = "";
        
        public string GetFileName()
        {
            string filename = "";


            if (url == null)
                MessageBox.Show("프로젝트 주소 오류");

            char[] nameSplit = url.ToArray();

            string search = "";

            for (int i = 0; i < nameSplit.Length; i++)
            {
                if (nameSplit[i] == '구' && search.Replace("서울특별시","") != " ")

                    break;
                else
                {
                    search += nameSplit[i];
                }
            }

            search = search.Replace("서울", "");
            search = search.Replace("특별시", "");
            search = search.Replace(" ", "");
            search = search.Replace("서울특별시", "");

            Rhino.RhinoApp.WriteLine(search);

            System.IO.FileInfo[] fileinfo = new System.IO.DirectoryInfo(dir).GetFiles(search + "*최종.DWG");

            filename = fileinfo[0].Name;

            return filename;
        }
        public override string EnglishName
        {
            get { return "ImportFile"; }
        }

        protected override Rhino.Commands.Result RunCommand(Rhino.RhinoDoc doc, Rhino.Commands.RunMode mode)
        {
            ///////////////
            if (is64)
            {
                dir = dir64;
                Rhino.RhinoApp.WriteLine("64bit" + dir);
            }
            else
            {
                dir = dir32;
                Rhino.RhinoApp.WriteLine("32bit" + dir);
            }

            string[] searchpaths = Rhino.ApplicationSettings.FileSettings.GetSearchPaths();


            if (searchpaths.Length == 0 || 
                searchpaths[0] != dir)
            {     
                Rhino.ApplicationSettings.FileSettings.AddSearchPath(dir, 0);
            }

            Rhino.ApplicationSettings.FileSettings.WorkingFolder =
                Rhino.ApplicationSettings.FileSettings.GetSearchPaths()[0];

            Rhino.UI.OpenFileDialog open = new Rhino.UI.OpenFileDialog();
            open.InitialDirectory = dir;
            url = TuringAndCorbusierPlugIn.InstanceClass.turing.ProjectAddress.Text;

            if (GetFileName().Length > 0)
            {
                Rhino.RhinoApp.Wait();

                if (TuringAndCorbusierPlugIn.InstanceClass.isfirst)
                {
                    
                    Rhino.RhinoApp.RunScript("_-Open " + GetFileName() + " '_Enter" , true);
                    TuringAndCorbusierPlugIn.InstanceClass.isfirst = false;
                }
                else
                    Rhino.RhinoApp.RunScript("_-Open n " + GetFileName() + " '_Enter" , true);



                return Rhino.Commands.Result.Success;
            }
            else
            {
                return Rhino.Commands.Result.Failure;
            }

        }

        public void SendEnterKey()
        {
            System.Windows.Forms.SendKeys.SendWait("{ENTER}");
        }

    }


    class LoadManager
    {

        public enum NamedLayer
        {
            Model = 0,
            ETC,
            Guide
        }

        private static LoadManager instance = new LoadManager();

        Rhino.DocObjects.Layer modelLayer = new Rhino.DocObjects.Layer();
        Rhino.DocObjects.Layer etcLayer = new Rhino.DocObjects.Layer();
        Rhino.DocObjects.Layer guideLayer = new Rhino.DocObjects.Layer();

        Rhino.DocObjects.Layer[] layerarray = new Rhino.DocObjects.Layer[3];

        public int[] layerIndexes = new int[3];

        private LoadManager()
        {
            if (instance == null)
                instance = this;
        }
        public static LoadManager getInstance()
        { return instance; }

        public void importFileWithAdress()
        {
            Rhino.RhinoApp.RunScript("ImportFile", false);

            //Rhino.RhinoApp.Wait();

            //findFileWithAdress();
        }

        public void findFileWithAdress()
        {
            string adress = TuringAndCorbusierPlugIn.InstanceClass.turing.ProjectAddress.Text;

            char[] nameSplit = adress.ToArray();

            string search = "";

            char[] ototen = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '-' };

            char beforechar = 'a';

            int strt = 0;

            for (int i = 0; i < nameSplit.Length; i++)
            {
                int samecount = 0;
                for (int j = 0; j < ototen.Length; j++)
                {
                    if (nameSplit[i] == ototen[j])
                    {
                        search += nameSplit[i];
                        beforechar = nameSplit[i];
                        samecount++;
                        if (beforechar != 'a')
                            strt++;
                        break;
                    }
                }

                if (samecount == 0 && strt < 2)
                {
                    beforechar = 'a';
                    strt = 0;
                }
                else if (samecount == 0 && strt >= 2)
                    break;

            }




            search = search.Replace(" ", "");

            if (search == "")
            {
                Rhino.RhinoApp.WriteLine("번지 정보가 없습니다.");
                return;
            }

            MessageBox.Show(search + "번지로 검색");
            //zc303 = 지번 레이어
            List<string> layer_name = new List<string>();
            for (int i = 0; i < Rhino.RhinoDoc.ActiveDoc.Layers.Count; i++)
            {
                if (Rhino.RhinoDoc.ActiveDoc.Layers[i].Name.Contains("ZC"))
                    layer_name.Add(Rhino.RhinoDoc.ActiveDoc.Layers[i].Name);
            }

            List<Rhino.DocObjects.RhinoObject> robj = new List<Rhino.DocObjects.RhinoObject>();

            for (int i = 0; i < layer_name.Count; i++)
            {
                robj.AddRange(Rhino.RhinoDoc.ActiveDoc.Objects.FindByLayer(layer_name[i]));
            }

            List<Rhino.DocObjects.RhinoObject> searched = new List<Rhino.DocObjects.RhinoObject>();

            Rhino.RhinoApp.WriteLine(robj.Count + "개 있음");

            foreach (Rhino.DocObjects.RhinoObject i in robj)
            {

                var annotation = i as Rhino.DocObjects.AnnotationObjectBase;

                if (annotation == null)
                    continue;

                //System.InvalidCastException

                char[] zzz = ((Rhino.DocObjects.TextObject)i).DisplayText.ToArray<char>();
                string result = "";

                for (int j = 0; j < zzz.Length; j++)
                {
                    for (int k = 0; k < ototen.Length; k++)
                    {
                        if (zzz[j] == ototen[k])
                        {
                            result += zzz[j];
                            break;
                        }
                    }
                }



                if (result == search)
                    searched.Add(i);

            }
            MessageBox.Show(robj.Count.ToString() + "검색한 오브젝트");
            foreach (Rhino.DocObjects.TextObject i in searched)
            {
                Rhino.RhinoApp.WriteLine("유사한 결과 : " + i.DisplayText);
            }

            if (searched.Count == 0)
            {
                MessageBox.Show("없는 지번");
            }

            else
            {
                //xaml.FindForm findform = new xaml.FindForm(searched,Rhino.RhinoDoc.ActiveDoc);

                //for (int i = 0; i < searched.Count; i++)
                //{
                //    findform.comboBox.Items.Add(searched[i]);
                //}
                //findform.comboBox.SelectedItem = findform.comboBox.Items[0];


                //findform.Show();

                Rhino.RhinoApp.RunScript("FindText", false);





            }
            //Rhino.RhinoApp.RunScript("_-FindText s ", true);


            //MenuWindow menu = MenuWindow.GetForegroundWindow() as MenuWindow;
            //Window window = Window.GetWindow()



            //Rhino.RhinoApp.
        }

        public void SetDBFiles()
        {

            bool is64 = Environment.Is64BitOperatingSystem;

            string cadaUrlDest = "";
            string cadaUrlFrom = "";
            string floorPlanDest = "";
            string floorPlanFrom = "";


            if (is64)
            {
                cadaUrlDest = "C:\\Users\\user\\Documents\\cadastrals\\";
                cadaUrlFrom = "C:\\Program Files (x86)\\Boundless\\TuringAndCorbusierSetUp\\DataBase\\cadastrals\\";
                floorPlanDest = "C:\\Program Files (x86)\\이주데이타\\floorPlanLibrary\\";
                floorPlanFrom = "C:\\Program Files (x86)\\Boundless\\TuringAndCorbusierSetUp\\DataBase\\floorPlanLibrary\\";
            }
            else
            {
                cadaUrlDest = "C:\\Users\\user\\Documents\\cadastrals\\";
                cadaUrlFrom = "C:\\Program Files\\Boundless\\TuringAndCorbusierSetUp\\DataBase\\cadastrals\\";
                floorPlanDest = "C:\\Program Files\\이주데이타\\floorPlanLibrary\\";
                floorPlanFrom = "C:\\Program Files\\Boundless\\TuringAndCorbusierSetUp\\DataBase\\floorPlanLibrary\\";
            }


            System.IO.DirectoryInfo cadadir = new System.IO.DirectoryInfo(cadaUrlDest);
            if (!cadadir.Exists)
                cadadir.Create();
            System.IO.DirectoryInfo pfldir = new System.IO.DirectoryInfo(floorPlanDest);
            if (!pfldir.Exists)
                pfldir.Create();

            System.IO.FileInfo[] cadainfo = new System.IO.DirectoryInfo(cadaUrlDest).GetFiles();
            System.IO.FileInfo[] fplinfo = new System.IO.DirectoryInfo(floorPlanDest).GetFiles();

            //Rhino.RhinoApp.WriteLine("BEFORE");
            //Rhino.RhinoApp.WriteLine("Cadafiles = " + cadainfo.Length.ToString() + " fplFiles = " + fplinfo.Length.ToString());

            if (cadainfo.Length < 1)
            {
                System.IO.FileInfo[] cadafiles = new System.IO.DirectoryInfo(cadaUrlFrom).GetFiles();
                CopyFiles(cadaUrlDest, cadafiles);
            }

            if (fplinfo.Length < 1)
            {
                System.IO.FileInfo[] fplfiles = new System.IO.DirectoryInfo(floorPlanFrom).GetFiles();
                CopyFiles(floorPlanDest, fplfiles);
            }

            Rhino.RhinoApp.WriteLine("AFTER");
            Rhino.RhinoApp.WriteLine("Cadafiles = " + cadainfo.Length.ToString() + " fplFiles = " + fplinfo.Length.ToString());

        }

        public void CopyFiles(string destiny, System.IO.FileInfo[] files)
        {
            foreach (System.IO.FileInfo f in files)
            {
                System.IO.File.Copy(f.FullName, destiny + f.Name, true);
                System.Windows.Forms.MessageBox.Show(f.FullName + " , " + destiny + f.Name);
            }
        }

        public void LayerSetting()
        {

            foreach (var item in Rhino.RhinoDoc.ActiveDoc.Objects)
            {
                item.Attributes.ColorSource = Rhino.DocObjects.ObjectColorSource.ColorFromLayer;
                item.CommitChanges();
            }

            foreach (var item in Rhino.RhinoDoc.ActiveDoc.Layers)
            {
                item.Color = System.Drawing.Color.White;
                item.CommitChanges();
            }

            Rhino.DocObjects.Layer[] newLayers = { modelLayer, etcLayer, guideLayer };
            layerarray = newLayers;
            System.Drawing.Color[] layerColors = { System.Drawing.Color.White, System.Drawing.Color.DarkGray, System.Drawing.Color.Red };

            int layerCount;

            for (int i = 0; i < newLayers.Length; i++)
            {

                newLayers[i].Color = layerColors[i];

                newLayers[i].CommitChanges();
                layerCount = Rhino.RhinoDoc.ActiveDoc.Layers.Count;
                Rhino.RhinoDoc.ActiveDoc.Layers.Add(newLayers[i]);
                layerIndexes[i] = layerCount;

            }

        }

        public Guid DrawObjectWithSpecificLayer<T>(object o, NamedLayer layername)
        {
            Rhino.RhinoDoc.ActiveDoc.Layers.SetCurrentLayerIndex(layerIndexes[(int)layername], false);
            if (o is Rhino.Geometry.GeometryBase)
            {
                var result = Rhino.RhinoDoc.ActiveDoc.Objects.Add((Rhino.Geometry.GeometryBase)o);
                return result;
            }
            return Guid.Empty;
            
        }

        public Guid DrawObjectWithSpecificLayer(Rhino.Geometry.Curve curve,NamedLayer layername)
        {
            Rhino.RhinoDoc.ActiveDoc.Layers.SetCurrentLayerIndex(layerIndexes[(int)layername], false);

            var result =  Rhino.RhinoDoc.ActiveDoc.Objects.AddCurve(curve);

            Rhino.RhinoDoc.ActiveDoc.Layers.SetCurrentLayerIndex(0, false);

            return result;
        }

        public List<Guid> DrawObjectWithSpecificLayer(List<Rhino.Geometry.Curve> curves, NamedLayer layername)
        {
            Rhino.RhinoDoc.ActiveDoc.Layers.SetCurrentLayerIndex(layerIndexes[(int)layername], false);
            var result = new List<Guid>();
            foreach (var c in curves)
            {
                result.Add(Rhino.RhinoDoc.ActiveDoc.Objects.AddCurve(c));
            }

            Rhino.RhinoDoc.ActiveDoc.Layers.SetCurrentLayerIndex(0, false);
            return result;
        }

        public Guid DrawObjectWithSpecificLayer(Rhino.Geometry.Point3d point, NamedLayer layername)
        {
            Rhino.RhinoDoc.ActiveDoc.Layers.SetCurrentLayerIndex(layerIndexes[(int)layername], false);

            var result = Rhino.RhinoDoc.ActiveDoc.Objects.AddPoint(point);

            Rhino.RhinoDoc.ActiveDoc.Layers.SetCurrentLayerIndex(0, false);
            return result;
        }

        public List<Guid> DrawObjectWithSpecificLayer(List<Rhino.Geometry.Point3d> points, NamedLayer layername)
        {
            Rhino.RhinoDoc.ActiveDoc.Layers.SetCurrentLayerIndex(layerIndexes[(int)layername], false);
            var result = new List<Guid>();
            foreach (var p in points)
            {
                result.Add(Rhino.RhinoDoc.ActiveDoc.Objects.AddPoint(p));
            }

            Rhino.RhinoDoc.ActiveDoc.Layers.SetCurrentLayerIndex(0, false);
            return result;
        }

        public Guid DrawObjectWithSpecificLayer(Rhino.Geometry.Brep brep, NamedLayer layername)
        {
            Rhino.RhinoDoc.ActiveDoc.Layers.SetCurrentLayerIndex(layerIndexes[(int)layername], false);

            var result = Rhino.RhinoDoc.ActiveDoc.Objects.AddBrep(brep);

            Rhino.RhinoDoc.ActiveDoc.Layers.SetCurrentLayerIndex(0, false);
            return result;
        }

        public Guid DrawObjectWithSpecificLayer(Rhino.Geometry.Mesh mesh, NamedLayer layername)
        {
            Rhino.RhinoDoc.ActiveDoc.Layers.SetCurrentLayerIndex(layerIndexes[(int)layername], false);

            var result = Rhino.RhinoDoc.ActiveDoc.Objects.AddMesh(mesh);

            Rhino.RhinoDoc.ActiveDoc.Layers.SetCurrentLayerIndex(0, false);
            return result;
        }



        public List<Guid> DrawObjectWithSpecificLayer(List<Rhino.Geometry.Brep> breps, NamedLayer layername)
        {
            Rhino.RhinoDoc.ActiveDoc.Layers.SetCurrentLayerIndex(layerIndexes[(int)layername], false);
            var result = new List<Guid>();
            foreach (var p in breps)
            {
                result.Add(Rhino.RhinoDoc.ActiveDoc.Objects.AddBrep(p));
            }

            Rhino.RhinoDoc.ActiveDoc.Layers.SetCurrentLayerIndex(0, false);
            return result;
        }

        public Guid DrawObjectWithSpecificLayer(Rhino.Display.Text3d text, NamedLayer layername)
        {
            Rhino.RhinoDoc.ActiveDoc.Layers.SetCurrentLayerIndex(layerIndexes[(int)layername], false);
            Rhino.DocObjects.ObjectAttributes attribute = new Rhino.DocObjects.ObjectAttributes();

            var result = Rhino.RhinoDoc.ActiveDoc.Objects.AddText(text.Text, text.TextPlane, 200, "Arial", false, false, Rhino.Geometry.TextJustification.Center);

            Rhino.RhinoDoc.ActiveDoc.Layers.SetCurrentLayerIndex(0, false);
            return result;
        }



        public List<Guid> DrawObjectWithSpecificLayer(List<Rhino.Display.Text3d> texts, NamedLayer layername)
        {
            Rhino.RhinoDoc.ActiveDoc.Layers.SetCurrentLayerIndex(layerIndexes[(int)layername], false);
            var result = new List<Guid>();
            foreach (var t in texts)
            {
                result.Add(Rhino.RhinoDoc.ActiveDoc.Objects.AddText(t.Text, t.TextPlane, 200, "Arial", false, false, Rhino.Geometry.TextJustification.Center));
            }

            Rhino.RhinoDoc.ActiveDoc.Layers.SetCurrentLayerIndex(0, false);
            return result;
        }

        public List<Guid> DrawObjectWithSpecificLayer(List<Rhino.Display.Text3d> texts, NamedLayer layername, Rhino.Geometry.TextJustification justification)
        {
            Rhino.RhinoDoc.ActiveDoc.Layers.SetCurrentLayerIndex(layerIndexes[(int)layername], false);
            var result = new List<Guid>();
            foreach (var t in texts)
            {
                result.Add(Rhino.RhinoDoc.ActiveDoc.Objects.AddText(t.Text, t.TextPlane, 200, "Arial", false, false, justification));
            }

            Rhino.RhinoDoc.ActiveDoc.Layers.SetCurrentLayerIndex(0, false);
            return result;
        }



    }
}

