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
using Rhino.Geometry;
using Rhino;

namespace TuringAndCorbusier
{
    /// <summary>
    /// UserControl1.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class ModelingComplexity : Window
    {
        public ModelingComplexity(Apartment tempOutput)
        {
            InitializeComponent();
            this.tempOutput = tempOutput;

            var location = System.Windows.Forms.Control.MousePosition;
            Left = location.X - Width / 2;
            Top = location.Y - Height / 2;


        }

        private Apartment tempOutput;

        private void Button_Simple_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();

            int index = TuringAndCorbusierPlugIn.InstanceClass.turing.tempIndex;


            try
            {
                if (TuringAndCorbusierPlugIn.InstanceClass.turing.MainPanel_building3DPreview[index] != null)
                    TuringAndCorbusierPlugIn.InstanceClass.turing.MainPanel_building3DPreview.RemoveAt(index);
            }
            catch (System.ArgumentOutOfRangeException)
            {

            }

            //Rhino.RhinoDoc.ActiveDoc.Layers.SetCurrentLayerIndex(Rhino.RhinoDoc.ActiveDoc.Layers.Find("ZC304", true), true);

            List<Brep> tempBreps = TuringAndCorbusier.MakeBuildings.makeBuildings(tempOutput);
            List<Guid> tempGuid = new List<Guid>();
            foreach (Brep i in tempBreps)
            {

                tempGuid.Add(LoadManager.getInstance().DrawObjectWithSpecificLayer(i, LoadManager.NamedLayer.Model));

                //foreach (Brep j in i)
                //{
                //    var guid = RhinoDoc.ActiveDoc.Objects.AddBrep(j);
                //    tempGuid.Add(guid);
                //    RhinoApp.Wait();
                //}
            }

            //Rhino.RhinoDoc.ActiveDoc.Layers.SetCurrentLayerIndex(Rhino.RhinoDoc.ActiveDoc.Layers.Find("Default", true), true);


            

            TuringAndCorbusierPlugIn.InstanceClass.turing.MainPanel_building3DPreview.Insert(index, tempGuid);
            //MessageBox.Show(index + " 번 결과에 모델링 저장");

            RhinoDoc.ActiveDoc.Views.Redraw();

            CloseWindow();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private void CloseWindow()
        {
            //TuringAndCorbusierPlugIn.InstanceClass.modelingComplexity = null;
            Close();
        }

        private void complexityWindow_MouseLeave(object sender, MouseEventArgs e)
        {
            CloseWindow();
        }
    }
}
