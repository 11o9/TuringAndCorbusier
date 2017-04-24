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

namespace TuringAndCorbusier.xaml
{
    /// <summary>
    /// SlopeSetting.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class SlopeSetting : Window
    {
        public Rhino.Geometry.Point3d Point;
        public NewProjectWindow nw = null;
        public Guid guid;
        public Guid sphere;
        int index;



        public SlopeSetting(Guid guid ,Guid sphereGuid, NewProjectWindow nw, int index)
        {
            this.guid = guid;
            InitializeComponent();
            this.nw = nw;
            Point = (Rhino.RhinoDoc.ActiveDoc.Objects.Find(guid) as Rhino.DocObjects.PointObject).PointGeometry.Location;
            textBox.Focus();
            this.sphere = sphereGuid;
            this.index = index;
        }
        public void Update()
        {

            textBox.Text =(Point.Z / 1000).ToString();
            float mousex = System.Windows.Forms.Cursor.Position.X;
            float mousey = System.Windows.Forms.Cursor.Position.Y;
            Top = mousey;
            Left = mousex;
            textBox.SelectionStart = textBox.Text.Length;
            Rhino.RhinoDoc.ActiveDoc.Views.ActiveView.Redraw();
        }

        private void button_Click(object sender, RoutedEventArgs e)
        { 
        }

        private void textBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {

                string textboxValue = textBox.Text;
                int result = 0;
                bool parsable = int.TryParse(textboxValue, out result);
                if (!parsable)
                {
                    MessageBox.Show("정수값입력");
                }
                else
                {
                    Point.Z = result * 1000;

                    Guid newp = Rhino.RhinoDoc.ActiveDoc.Objects.AddPoint(Point);
                    Guid newsphere = Rhino.RhinoDoc.ActiveDoc.Objects.AddBrep((new Rhino.Geometry.Sphere(Point, 2000)).ToBrep());

                    Rhino.RhinoDoc.ActiveDoc.Objects.Hide(sphere, true);
                    Rhino.RhinoDoc.ActiveDoc.Objects.Hide(guid,true);

                    guid = newp;
                    sphere = newsphere;

                    Point = (Rhino.RhinoDoc.ActiveDoc.Objects.Find(newp) as Rhino.DocObjects.PointObject).PointGeometry.Location;

                    nw.points[index] = Point;
                    nw.ClickAbleSphere[index] = sphere;
                    Hide();
                }
            }
        }
    }
}
