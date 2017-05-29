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
            string conn = ConnectionStringBuilder.GetConnectionString(SERVER.Azure);
            string comm = QueryBuilder.GetSiDoCode();
            try
            {
                Sis = ServerConnection.MetaCon<NameCode>(conn, comm);
                si.ItemsSource = Sis.Select(n => n.name);
            }
            catch (Exception e)
            {
                MessageBox.Show("서버 연결 에러");
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

            string conn = ConnectionStringBuilder.GetConnectionString(SERVER.Azure);
            string comm = QueryBuilder.GetGuDataWithSiCode(SiDoCode, SERVER.Azure);
            Gus = ServerConnection.MetaCon<NameCode>(conn, comm);
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

            string conn = ConnectionStringBuilder.GetConnectionString(SERVER.Azure);
            string comm = QueryBuilder.GetDongDataWithGuCode(GuCode, SERVER.Azure);
            Dongs = ServerConnection.MetaCon<NameCode>(conn, comm);
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

            string conn = ConnectionStringBuilder.GetConnectionString(SERVER.Azure);
            string comm = QueryBuilder.GetPiljiDataWithDongCode(DongCode);
            Piljis = ServerConnection.MetaCon<Pilji>(conn, comm);

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

        private void DrawPilji(object sender, RoutedEventArgs e)
        {
            drawer.Draw(Piljis);
            if (Bonbun == "")
            {
                //동 전체 그리고 중심에 카메라 셋팅
                SetCam(RhinoDoc.ActiveDoc.Objects.BoundingBox);
            }

            else
            {
                //동 전체 그리고 주소 찾아서 카메라 셋팅, 검색결과 없을시 중심에 셋팅
                string jibun = bonbun.Text + "-" + bubun.Text;
                var result = drawer.Find(jibun);
                if (result == null)
                    SetCam(RhinoDoc.ActiveDoc.Objects.BoundingBox);
                else
                {
                    List<Point3d> points = new List<Point3d>();
                    result.Outbound.ForEach(n => points.AddRange(n.DuplicateSegments().Select(m => m.PointAtStart)));
                    BoundingBox bb = new BoundingBox(points);
                    SetCam(bb);
                }
                    
            }

        }
        private void SetCam(BoundingBox bb)
        {
            
            Rhino.RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport.SetCameraLocation(new Point3d(bb.Center.X, bb.Center.Y, bb.Diagonal.Length * 1.5), false);
            Rhino.RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport.SetCameraDirection(new Vector3d(0, 0, -1), false);
            Rhino.RhinoDoc.ActiveDoc.Views.Redraw();
        }



        public void OnSelectPilji(object sender, Rhino.DocObjects.RhinoObjectSelectionEventArgs e)
        {
            foreach (var obj in e.RhinoObjects)
            {
                Pilji p = drawer.Find(obj.Id);
                if (p == null)
                    continue;
                else
                    preview.Update(p);
            }
            e.Document.Objects.UnselectAll();
        }
    }
}
