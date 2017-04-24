using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using Rhino.Geometry;

using System.Runtime.InteropServices;
namespace TuringAndCorbusier
{
    /// <summary>
    /// MenuWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    /// 


    public partial class MenuWindow : Window
    {

        //[DllImport("user32.dll")]
        //static extern int GetForegrondWindow();
        //[DllImport("user32.dll")]
        //static extern int GetWindowText(int hWnd, StringBuilder text, int count);
        //[DllImport("user32.dll")]
        //static extern void SetWindowPos(int hWnd, int hWndInsertAfter, int x, int y, int cx, int cy);
        //[DllImport("user32.dll")]
        //public static extern IntPtr GetWindow(IntPtr hWnd, int uCmd);

        public NewProjectWindow current = null;

        public MenuWindow()
        {
            InitializeComponent();
        }

        private void Load_Click(object sender, RoutedEventArgs e)
        {
            errorMessage tempError = new errorMessage("아직 구현되지 않은 기능입니다");
            tempError.ShowDialog();
        }
       
        private void NewProject_Click(object sender, RoutedEventArgs e)
        {

            if (TuringAndCorbusierPlugIn.InstanceClass.currentNewProjectWindow != null)
                return;

            
            NewProjectWindow tempNewProject = new NewProjectWindow();
            //AddChild(tempNewProject);
            /// <summary>
            /// 0518 민호 newproject 클릭시 생성된 창 정보 정적변수에 전달
            /// </summary>
            TuringAndCorbusierPlugIn.InstanceClass.currentNewProjectWindow = tempNewProject;


            
            UIManager.getInstance().HideWindow(this, UIManager.WindowType.Menu);
            try { UIManager.getInstance().ShowWindow(tempNewProject,UIManager.WindowType.Basic); }

            catch(System.Exception)
            {  }
            
        }


        
        private void Upload_Data_Click(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < 1; i++)
            {
                try
                {
                    ((Rhino.UI.Panels.GetPanel(TuringHost.PanelId) as RhinoWindows.Controls.WpfElementHost).Child as Turing).sendDataToServer();
                }
                catch (Exception x)
                {

                }
            }


            UIManager.getInstance().HideWindow(this, UIManager.WindowType.Menu);
        }

        //public void GetWindow()
        //{
        //    IntPtr currentwindow = GetWindow(Rhino.RhinoApp.MainWindowHandle(), 0);

        //    System.Windows.Forms.Control cwc = System.Windows.Forms.Control.FromHandle(currentwindow);

        //    Rhino.RhinoApp.WriteLine(cwc.Name);

        //}


        private void RhinoUI_Show_Click(object sender, RoutedEventArgs e)
        {
            //if (!RhinoWindowSetUp.isvisible)
            //    RhinoWindowSetUp.SetUIForTC(Rhino.RhinoDoc.ActiveDoc);
            //else
            //    RhinoWindowSetUp.ResetUI(Rhino.RhinoDoc.ActiveDoc);
            Rhino.UI.OpenFileDialog ofd = new Rhino.UI.OpenFileDialog();
            ofd.Filter = "RhinoFiles (*.3dm)|*.3dm";
            var result = ofd.ShowDialog();

            if (result == System.Windows.Forms.DialogResult.OK)
            {
                Rhino.RhinoApp.RunScript("_-Open n " + ofd.FileName, true);
            }
       
           


            UIManager.getInstance().HideWindow(this, UIManager.WindowType.Menu);
        }

        private void Window_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            TuringAndCorbusierPlugIn.InstanceClass.turing.Focus();
            UIManager.getInstance().HideWindow(this, UIManager.WindowType.Menu);
        }

        


        //public void SendEnterKey()
        //{
        //    System.Windows.Forms.SendKeys.SendWait("{ENTER}");
        //    Rhino.RhinoApp.Write("Enter");
        //}
        //public void SendEnterKey(object sender, System.EventArgs e)
        //{
        //    System.Windows.Forms.SendKeys.SendWait("{ENTER}");
        //    Rhino.RhinoApp.Write("Enter");
        //}
        public void LoDDDDd_Click(object sender, RoutedEventArgs e)
        {

            var yorn = MessageBox.Show("지적도를 다시 불러옵니다. 작업 내용은 저장되지 않습니다. 계속 하시겠습니까?", "지적도 재 설정", MessageBoxButton.YesNo);

            if (yorn == MessageBoxResult.No)
                return;

            LoadManager.getInstance().
            importFileWithAdress();

            //GetWindow();

            UIManager.getInstance().HideWindow(this, UIManager.WindowType.Menu);
        }
    }
}
