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

namespace TuringAndCorbusier
{
    /// <summary>
    /// NavigationHost.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class Corbusier : NavigationWindow
    {
        public static List<Curve> inputCrvs { get; set; }
        public static List<ConduitLine> conduitLines = new List<ConduitLine>();
        public static List<Curve> outputCrvs = new List<Curve>();
        public static List<int> RoadWidth { get { return roadWidth; } set { roadWidth = value; } }
        public static LinesConduit conduitLineDisplay {get;  set; }

        private static List<int> roadWidth = new List<int>();

        int frommousex = 0;
        int frommousey = 0;
        int tomousex = 0;
        int tomousey = 0;

        public Corbusier()
        {
            InitializeComponent();

            Page2 newPage = TuringAndCorbusierPlugIn.InstanceClass.page2;
            
            //this.NavigationService.Navigate(TuringAndCorbusierPlugIn.InstanceClass.page2);
            this.NavigationService.Navigate(newPage);
        }


        

        private void NavigationWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = false;
            UIManager.getInstance().HideWindow(this, UIManager.WindowType.Navi);

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// 

        bool clicked = false;
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {

            
            if (e.ChangedButton == MouseButton.Left)
            {
                clicked = true;
                frommousex = System.Windows.Forms.Cursor.Position.X;
                frommousey = System.Windows.Forms.Cursor.Position.Y;

 
                this.DragMove();
            }


        }

        private void NavigationWindow_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left && clicked)
            {

                tomousex = System.Windows.Forms.Cursor.Position.X;
                tomousey = System.Windows.Forms.Cursor.Position.Y;


                GetMouseMove();
            }
        }
        private void GetMouseMove()
        {
            int x = tomousex - frommousex;
            int y = tomousey - frommousey;

            UIManager.getInstance().navi_marginx += x;
            UIManager.getInstance().navi_marginy += y;
            clicked = false;    
        }
    }
}
