using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using GISData.DataStruct;
using GISData;
using Rhino.Geometry;
using Rhino;
using System.Collections;
using GISData.Extract;
namespace TuringAndCorbusier
{
    /// <summary>
    /// ServerUI.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class ServerUI : UserControl
    {
        public PiljiDrawer drawer = new PiljiDrawer();
        public PlotPreview preview = new PlotPreview();

        List<NameCode> Sis = new List<NameCode>();
        List<NameCode> Gus = new List<NameCode>();
        List<NameCode> Dongs = new List<NameCode>();
        public List<Pilji> Piljis = new List<Pilji>();
        //address
        string SiDo = "";
        string Gu = "";
        string Dong = "";
        string Bonbun = "";
        string Bubun = "0000";
        public string Address = "";

        //code
        string SiDoCode = "";
        string GuCode = "";
        string DongCode = "";
        string Code = "";

        public ServerUI()
        {
            InitializeComponent();

            bool test = ServerConnection.TestConnection(SERVER.Azure);

            if (!test)
            {
                //MessageBox.Show("서버 연결 에러");
                return;
            }
            try
            {
                Sis = new List<NameCode>() { new NameCode("서울특별시", "11") };
                si.ItemsSource = Sis.Select(n => n.name);
            }
            catch (Exception e)
            {
                MessageBox.Show("서버 연결 실패");
            }
        }
        private void SiChanged(object sender, SelectionChangedEventArgs e)
        {
            //si changed


            Gus.Clear();
            Dongs.Clear();
            gu.ItemsSource = Gus;
            dong.ItemsSource = Dongs;
            preview = new PlotPreview();
            preview.Enabled = true;
            bonbun.Text = "";
            bubun.Text = "";


            if (si.SelectedIndex < 0)
                return;

            SiDo = Sis[si.SelectedIndex].name;
            SiDoCode = Sis[si.SelectedIndex].code;

            Gus = ServerConnection.MetaCon<NameCode>(ServerConnection.DATATYPE.GU, SiDoCode);
            gu.ItemsSource = Gus.Select(n => n.name);

            Refresh();
        }
        private void GuChanged(object sender, SelectionChangedEventArgs e)
        {
            //si changed
            preview = new PlotPreview();
            preview.Enabled = true;
            bonbun.Text = "";
            bubun.Text = "";
            if (gu.SelectedIndex < 0)
                return;

            Gu = Gus[gu.SelectedIndex].name;
            GuCode = Gus[gu.SelectedIndex].code;

            
            Dongs = ServerConnection.MetaCon<NameCode>(ServerConnection.DATATYPE.DONG, GuCode);
            dong.ItemsSource = Dongs.Select(n => n.name);

            Refresh();
        }
        private void DongChanged(object sender, SelectionChangedEventArgs e)
        {
            //dong changed
            bonbun.Text = "";
            bubun.Text = "";
            preview = new PlotPreview();
            preview.Enabled = true;
            if (dong.SelectedIndex < 0)
                return;

            Dong = Dongs[dong.SelectedIndex].name;
            DongCode = Dongs[dong.SelectedIndex].code;

           // string q = QueryBuilder.GetPiljiDataWithDongCode(DongCode);


            string servername = QueryBuilder.GetServerName(AZURE_Mssql_GIS_Tables_shp.busan_shp);


            Piljis = ServerConnection.MetaCon<Pilji>(ServerConnection.DATATYPE.PILJI, DongCode);

            

            Piljis.ForEach(n => n.Scale(0.001));


            Refresh();

        }
        private void BonbunChanged(object sender, TextChangedEventArgs e)
        {
            string bonbunBase = "0000";
            string bon = bonbun.Text;
            bonbunBase = bonbunBase.Remove(0, bon.Length);
            Bonbun = bonbunBase + bon;

            Refresh();
        }
        private void BubunChanged(object sender, TextChangedEventArgs e)
        {
            string bubunBase = "0000";
            string bu = bubun.Text;
            bubunBase = bubunBase.Remove(0, bu.Length);
            Bubun = bubunBase + bu;

            Refresh();
        }
        private void Refresh()
        {
            Address = SiDo + " " + Gu + " " + Dong + " ";
            Code = DongCode+"%"+ Bonbun + Bubun;
            //codeTest.Text = Code;
        }

        string lastbonbun = "";
        string lastbubun = "";
        string lastdong = "";
        private void DrawPilji(object sender, RoutedEventArgs e)
        {
            //동 같으면 카메라만 맞춤
            if (Dong == lastdong)
            {
                SetCam();
            }
            else
            {
                //RhinoApp.RunScript("Show", false);
                drawer.Draw(Piljis);
                lastdong = Dong;
                SetCam();
            }

            if (Bonbun == "")
            {
                //동 전체 그리고 중심에 카메라 셋팅
                //SetCam(RhinoDoc.ActiveDoc.Objects.BoundingBox);
                SetCam();
             
            }

            else
            {
                //동 전체 그리고 주소 찾아서 카메라 셋팅, 검색결과 없을시 중심에 셋팅
                string jibun = bonbun.Text + "-" + bubun.Text;
                if (bubun.Text == "")
                    jibun = bonbun.Text;
                var result = drawer.FindDrawn(jibun);
                if (result == null)
                {
                    RhinoApp.WriteLine("존재하지 않는 지번 주소입니다.");
                    SetCam();
                }
                    
                else
                {
                    List<Point3d> points = new List<Point3d>();
                    //RhinoApp.RunScript("SelAll", false);
                    SetCam(result.drawnObj);
                    //result.Outbound.ForEach(n => points.AddRange(n.DuplicateSegments().Select(m => m.PointAtStart)));
                    //BoundingBox bb = new BoundingBox(points);
                    //SetCam(bb);
                }

            }



            lastbonbun = Bonbun;
            lastbubun = Bubun;

        }
        private void SetCam(BoundingBox bb)
        {
            SetView();
            Rhino.RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport.SetCameraLocation(new Point3d(bb.Center.X, bb.Center.Y, bb.Diagonal.Length * 1.5), false);
            Rhino.RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport.SetCameraDirection(new Vector3d(0, 0, -1), false);
            Rhino.RhinoDoc.ActiveDoc.Views.Redraw();
        }
        private void SetCam(IEnumerable<Guid> guid)
        {
            SetView();
            RhinoDoc.ActiveDoc.Objects.Select(guid);
            RhinoApp.RunScript("ZSA", false);
            RhinoDoc.ActiveDoc.Objects.UnselectAll();
        }
        private void SetCam()
        {
            SetView();
            RhinoApp.RunScript("SelAll", false);
            RhinoApp.RunScript("ZSA", false);
            RhinoDoc.ActiveDoc.Objects.UnselectAll();
        }

        private void SetView()
        {
            Rhino.DocObjects.Tables.ViewTable viewinfo = RhinoDoc.ActiveDoc.Views;
            foreach (Rhino.Display.RhinoView i in viewinfo)
            {
                Rhino.Display.RhinoViewport vp = i.ActiveViewport;
                Guid dpmguid = Guid.Empty;

                if (vp.Name == "Top")
                {
                    //currentview.Add(i)

                    vp.WorldAxesVisible = false;
                    vp.ParentView.Maximized = true;
                    Rhino.Display.DisplayModeDescription dm = vp.DisplayMode;
                    if (dm.EnglishName != "Shaded")
                    {
                        Rhino.Display.DisplayModeDescription[] dms = Rhino.Display.DisplayModeDescription.GetDisplayModes();

                        for (int j = 0; j < dms.Length; j++)
                        {
                            string english_name = dms[j].EnglishName;
                            english_name = english_name.Replace("_", "");
                            english_name = english_name.Replace(" ", "");
                            english_name = english_name.Replace("-", "");
                            english_name = english_name.Replace(",", "");
                            english_name = english_name.Replace(".", "");

                            if (english_name == "Shaded")
                            {
                                vp.DisplayMode = Rhino.Display.DisplayModeDescription.FindByName(dms[j].EnglishName);
                                vp.DisplayMode.DisplayAttributes.ShowCurves = true;
                            }

                        }
                    }
                }
                else
                {
                    //   i.Close();
                }
            }
        }
     
        public void OnSelectPilji(object sender, Rhino.DocObjects.RhinoObjectSelectionEventArgs e)
        {
            foreach (var obj in e.RhinoObjects)
            {
                Pilji p = drawer.Find(obj.Id);
                if (p == null)
                    continue;
                else
                {
                    preview.Update(p);
                    break;
                }
            }
            e.Document.Objects.UnselectAll();
        }
    }
}
