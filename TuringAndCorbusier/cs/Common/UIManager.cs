using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Runtime.InteropServices;
namespace TuringAndCorbusier
{
    class UIManager
    {

        Rhino.ApplicationSettings.ModelAidSettingsState current = Rhino.ApplicationSettings.ModelAidSettings.GetDefaultState();
        Rhino.ApplicationSettings.SmartTrackSettingsState smtcurrent = Rhino.ApplicationSettings.SmartTrackSettings.GetDefaultState();
        //사투의 흔적
        //[DllImport("User32", EntryPoint = "FindWindow")]
        //private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        //[DllImport("user32.dll")]
        //public static extern void SetForegroundWindow(IntPtr hWnd);

        //[DllImport("user32.dll")]
        //public static extern IntPtr GetForegroundWindow();

        //[DllImport("user32.dll")]
        //public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        //private const int SW_SHOWNORMAL = 1;

        //[DllImport("user32.dll")]
        //private static extern int GetWindowText(int hWnd, StringBuilder title, int size);
        private static UIManager instance = new UIManager();

        public enum SnapMode
        {
            Default = 0,
            EndOnly,
            OffAll,
            Current
        }

        public void SnapSetter(SnapMode mode)
        {

            Rhino.ApplicationSettings.ModelAidSettingsState setting = Rhino.ApplicationSettings.ModelAidSettings.GetDefaultState();
            Rhino.ApplicationSettings.SmartTrackSettingsState smtsetting = Rhino.ApplicationSettings.SmartTrackSettings.GetDefaultState();

            if (mode != SnapMode.Current)
            {
                current = Rhino.ApplicationSettings.ModelAidSettings.GetCurrentState();
                smtcurrent = Rhino.ApplicationSettings.SmartTrackSettings.GetCurrentState();
            }
            else
            {
                setting = current;
                smtsetting = smtcurrent;
            }
            switch (mode)
            {
                case SnapMode.Default:
                   
                    //Rhino.RhinoApp.WriteLine("Default");
                    break;
                case SnapMode.EndOnly:
                   
                    smtsetting.UseSmartTrack = false;
                    setting.OsnapModes = Rhino.ApplicationSettings.OsnapModes.None;
                    setting.OsnapModes = Rhino.ApplicationSettings.OsnapModes.End;
        
                    //Rhino.RhinoApp.WriteLine("EndOnly");
                    break;
                case SnapMode.OffAll:
                  
                    smtsetting.UseSmartTrack = false;
                    setting.OsnapModes = Rhino.ApplicationSettings.OsnapModes.None;

                    //Rhino.RhinoApp.WriteLine("OffAll");
                    break;
                case SnapMode.Current:
                   
                    //Rhino.RhinoApp.WriteLine("Current");
                    break;
                default:
                    break;

            }

            Rhino.ApplicationSettings.ModelAidSettings.UpdateFromState(setting);
            Rhino.ApplicationSettings.SmartTrackSettings.UpdateFromState(smtsetting);

        }
        public enum WindowType
        {
            Basic = 0,
            Menu,
            Print,
            Navi 
        }

        public struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;


            public static bool operator == (RECT rect1, RECT rect2)
            {
                if (rect1.top == rect2.top && rect1.left == rect2.left && rect1.right == rect2.right && rect1.bottom == rect2.bottom)
                    return true;
                else
                    return false;

            }

            public static bool operator !=(RECT rect1, RECT rect2)
            {
                if (rect1.top == rect2.top && rect1.left == rect2.left && rect1.right == rect2.right && rect1.bottom == rect2.bottom)
                    return false;
                else
                    return true;
            }

        }
        [DllImport("user32.dll")]
        public static extern IntPtr GetWindow(IntPtr hWnd, int uCmd);

        [DllImport("user32")]
        public static extern int GetWindowRect(int hwnd, ref RECT lpRect);
        
        public Point MENUstartpoint = new Point(0 ,0);
        public Point BASICstartpoint = new Point(0, 0);
        public Point PRINTstartpoint = new Point(0, 0);
        public Point NAVIstartpoint = new Point(0, 0);

        public RECT mainRect = new RECT();
        public RECT screenRect = new RECT();
        public RECT panelRect = new RECT();

        IntPtr mainWindowHandle = IntPtr.Zero;
        IntPtr panelHandle = IntPtr.Zero;


        public MenuWindow menu = null;
        public Reports.showme showme = null;
        public List<Window> basicwindows = new List<Window>();
        public Corbusier navi = null;

        public bool isRun = false;

        public int[] startNavimargines = { -585, 35 };


        public int navi_marginx = -585;
        public int navi_marginy = 35;
        





        /// <summary>
        /// 각 창 시작포인트 정해주고
        /// </summary>


        private UIManager()
        {
            //howcani dele = new howcani(changedsign);
            
            
            if (instance == null)
                instance = this;
            // Rhino.RhinoApp.RendererChanged += ;

            //Rhino.Runtime.HostUtils.InvokeOnMainUiThread(dele);
        }

        public static UIManager getInstance()
        { return instance; }

        public void init()
        {
            mainWindowHandle = Rhino.RhinoApp.MainWindowHandle();
            panelHandle = GetWindow(GetWindow(GetWindow(GetWindow(GetWindow(GetWindow(mainWindowHandle, 5), 2), 2), 2), 2), 2);


            PointsUpdate();

            CurrentWindowUpdate();

            CheckStart();
        }



        public Point Menu(RECT rect)
        {
            double x = rect.left;
            double y = rect.top;

            double marginx = 22;
            double marginy = 61;

            return new Point(x + marginx, y + marginy);
        }

        public Point Basic(RECT rect)
        {
            double x = rect.left;
            double y = rect.top;

            double marginx = -340;
            double marginy = 31;

            return new Point(x + marginx, y + marginy);
        }

        public Point Print(RECT rect)
        {
            double x = rect.left;
            double y = rect.top;

            double marginx = 50;
            double marginy = 50;

            return new Point(x + marginx, y + marginy);
        }

        public Point Navi(RECT rect)
        {
            double x = rect.left;
            double y = rect.top;

            double marginx = navi_marginx;
            double marginy = navi_marginy;

            return new Point(x + marginx, y + marginy);
        }


        //public void UpdateWindowPoints()
        //{
        //    BASICstartpoint = Basic(panelRect);
        //    MENUstartpoint = Menu(panelRect);
        //    PRINTstartpoint = Print(mainRect);
        //    NAVIstartpoint = Navi(panelRect);
        //}

        public RECT GetRectFromHandle(IntPtr handle)
        {
            RECT stRect = default(RECT);
            GetWindowRect((int)handle, ref stRect);
            //System.Windows.Forms.MessageBox.Show("핸들 rect 값 좌상우하 = " + stRect.left + "," +
            //    +stRect.top + "," + stRect.right + "," + stRect.bottom + ",");

            return stRect;
        }

        public RECT GetScreenRect()
        {
            RECT temp = new RECT();
            temp.top = 0;
            temp.left = 0;
            temp.right = System.Windows.Forms.SystemInformation.VirtualScreen.Width;
            temp.bottom = System.Windows.Forms.SystemInformation.VirtualScreen.Height;

            return temp;
        }



        public void RefreshUI()
        {
            CheckEnd();

            PointsUpdate();

            CurrentWindowUpdate();

            CheckStart();
        }





        public void PointsUpdate()
        {

            PRINTstartpoint = Print(GetRectFromHandle(mainWindowHandle));
            BASICstartpoint = Basic(GetRectFromHandle(panelHandle));
            MENUstartpoint = Menu(GetRectFromHandle(panelHandle));
            NAVIstartpoint = Navi(GetRectFromHandle(panelHandle));
        }

        //public void MinimizeCurrentWindows()
        //{
        //    if (navi != null)
        //    {
        //        navi.WindowState = WindowState.Minimized;
        //    }

        //    if (menu != null)
        //    {
        //        menu.WindowState = WindowState.Minimized;
        //    }

        //    if (showme != null)
        //    {
        //        showme.WindowState = WindowState.Minimized;
        //    }

        //    if (basicwindows.Count > 0)
        //    {
                
        //        foreach (Window win in basicwindows)
        //        {
        //            win.WindowState = WindowState.Minimized;
        //        }

        //    }

        //}

        public void CurrentWindowUpdate()
        {

            if(navi != null)
            {
                navi.Top = NAVIstartpoint.Y;
                navi.Left = NAVIstartpoint.X;
            }

            if (menu != null)
            {
                menu.Top = MENUstartpoint.Y;
                menu.Left = MENUstartpoint.X;
            }

            if (showme != null)
            {
                showme.Top = PRINTstartpoint.Y;
                showme.Left = PRINTstartpoint.X;
            }

            if (basicwindows.Count > 0)
            {
                double sumheight = 0;
                double sumwidth = 0;
                foreach (Window win in basicwindows)
                {
                    win.Top = BASICstartpoint.Y + sumheight;
                    win.Left = BASICstartpoint.X + sumwidth;

                    sumheight += win.Height;

                    if (basicwindows.IndexOf(win) % 3 == 2)
                    {
                        sumwidth -= win.Width;
                        sumheight = 0;
                    }
                }

            }


        
        }

        public void CheckRects(object sender, System.EventArgs e)
        {
            RECT newmain = GetRectFromHandle(mainWindowHandle);
            RECT newpanel = GetRectFromHandle(panelHandle);

            if (mainRect == newmain && panelRect == newpanel)
                return;

            RefreshUI();

        }

        public void CheckStart()
        {
            if (isRun)
                return;
            Rhino.RhinoApp.Idle += CheckRects;
            isRun = true;
        }

        public void CheckEnd()
        {
            if (!isRun)
                return;
            Rhino.RhinoApp.Idle -= CheckRects;
            isRun = false;
        }

        public void HideWindow(Window window, WindowType type)
        {
           

            if (type == WindowType.Basic)
            {
                basicwindows.Remove(window);
                window.Close();
            }

            else if (type == WindowType.Menu)
            {
                menu = null;
                window.Hide();
            }

            else if (type == WindowType.Print)
            {
                showme = null;
                window.Hide();
            }

            else if (type == WindowType.Navi)
            {
                navi = null;
                window.Hide();
            }


            CurrentWindowUpdate();
            
        }
        public void ShowWindow(Window window, WindowType type)
        {
            
           

            if (type == WindowType.Basic)
            {
                basicwindows.Add(window);
                
            }

            else if (type == WindowType.Menu)
            {
                menu = window as MenuWindow;
                
            }

            else if (type == WindowType.Print)
            {
                showme = window as Reports.showme;
                
            }
            else if (type == WindowType.Navi)
            {
                navi = window as Corbusier;
                
            }
            
            
            CurrentWindowUpdate();
            window.Show();
        }



        ///for test
        //public void getnumbers()
        //{
        //    ///스크린 너비/높이
        //    double width = System.Windows.Forms.SystemInformation.VirtualScreen.Width;
        //    double height = System.Windows.Forms.SystemInformation.VirtualScreen.Height;

        //    ///mainwindow 의 child핸들의 rect 구하기.

        //    IntPtr mainwindowhandle = Rhino.RhinoApp.MainWindowHandle();
        //    //IntPtr panelhandle = Rhino.UI.Panels.
        //    ///뷰포트영역?
        //    IntPtr main5 = GetWindow(mainwindowhandle, 5);

        //    ///뷰포트와 비슷함
        //    IntPtr main50 = GetWindow(main5, 0);

        //    ///뷰포트탭있는곳
        //    IntPtr main51 = GetWindow(main5, 1);
        //    ///스테이터스바
        //    IntPtr main52 = GetWindow(main5, 2);

        //    ///뷰포트 뒤쪽? 뭐지이건
        //    ///
        //    IntPtr main55 = GetWindow(main5, 5);



        //    ///패널윈도우
        //    ///첫째자식 부터 다섯번째 창
        //    ///

        //    IntPtr panel = GetWindow(GetWindow(GetWindow(GetWindow(GetWindow(GetWindow(mainwindowhandle, 5), 2), 2), 2), 2),2);



        //    RECT stRect = default(RECT);
        //    GetWindowRect((int)panel, ref stRect);
        //    System.Windows.Forms.MessageBox.Show("핸들 rect 값 좌상우하 = " + stRect.left + "," +
        //        +stRect.top + "," + stRect.right + "," + stRect.bottom + ",");




        //    System.Drawing.Rectangle rectangle = new System.Drawing.Rectangle(stRect.left, stRect.top, stRect.right - stRect.left, stRect.bottom - stRect.top);

        //    DrawRect(width, height, rectangle);


        //List<System.Drawing.Rectangle> rectangles = new List<System.Drawing.Rectangle>();
        //for (int i = 0; i < 6; i++)
        //{



        //    IntPtr hwndchild2 = GetWindow(main5, i);


        //    RECT stRect = default(RECT);
        //    GetWindowRect((int)hwndchild2, ref stRect);
        //    System.Windows.Forms.MessageBox.Show("핸들 rect 값 좌상우하 = " + stRect.left + "," +
        //        +stRect.top + "," + stRect.right + "," + stRect.bottom + ",");


        //    System.Drawing.Rectangle rectangle = new System.Drawing.Rectangle(stRect.left, stRect.top, stRect.right - stRect.left, stRect.bottom - stRect.top);

        //    rectangles.Add(rectangle);

        //}

        //DrawRect(width, height, rectangles);




        //}

        //private void DrawRect(double w, double h, System.Drawing.Rectangle r)
        //{

        //    xaml.UserControl2 rects = new xaml.UserControl2();


        //    rects.Width = r.Width;
        //    rects.Height = r.Height;
        //    rects.Opacity = 100;

        //    rects.RenderTransformOrigin = new Point(r.Left, r.Top);
        //    rects.Margin = new Thickness(r.Left, r.Top, 0, 0);


        //    xaml.UserControl1 uc1 = new xaml.UserControl1
        //    {
        //        Content = rects
        //        ///여기서 숫ㅅ자바꾸면서 실험중이었음

        //     };


        //    uc1.Width = w;
        //    uc1.Height = h;
        //    uc1.HorizontalContentAlignment = HorizontalAlignment.Left;
        //    uc1.VerticalContentAlignment = VerticalAlignment.Top;




        //    Window window = new Window
        //    {
        //        Title = "dfasdf",
        //        Content = uc1
        //    };





        //    window.SizeToContent = SizeToContent.WidthAndHeight;
        //    window.WindowStyle = WindowStyle.None;
        //    window.Top = 0;
        //    window.Left = 0;


        //    window.AllowsTransparency = true;
        //    window.Opacity = 0.1;


        //    window.Show();




        //}

        /// <summary>
        /// 판넬!!!!!!!찾앋야됨!
        /// \
        /// </summary>
        /// <returns></returns>

        //public void ChangeStartPoint(Point a)
        //{
        //    startpoint = a;
        //    RefreshWindow();
        //}


        //double revx = -15;
        //double revy = -25;

        //public void HideWindow(Window window)
        //{
        //    opened.Remove(window);
        //    window.Hide();
        //    RefreshWindow();
        //}
        //public void ShowWindow(Window window)
        //{



        //    opened.Add(window);
        //    RefreshWindow();

        //    window.Show();

        //}

        //public void RefreshWindow()
        //{
        //    double sumheight = 0;
        //    double sumwidth = 0;
        //    foreach (Window win in opened)
        //    {
        //        win.Top = startpoint.Y + sumheight+revy;
        //        win.Left = startpoint.X - win.Width + sumwidth+revx;

        //        sumheight += win.Height;

        //        if (opened.IndexOf(win) % 3 == 2)
        //        {
        //            sumwidth -= win.Width;
        //            sumheight = 0;
        //        }
        //    }
        //}

        private void regacy()
        {
            //System.Diagnostics.Process[] pro = System.Diagnostics.Process.GetProcesses();
            //for (int i = 0; i < pro.Length; i++)
            //{
            //    if (pro[i].MainWindowHandle != System.IntPtr.Zero)
            //    {
            //        if (pro[i].MainWindowTitle == "")
            //            continue;
            //        if (pro[i].ProcessName == "Rhino")
            //        {
            //            //var threds = pro[i].Threads;
            //            //System.Windows.Forms.MessageBox.Show(threds.Count.ToString());
            //            IntPtr rhinohandle = RhinoApp.MainWindowHandle();
            //            IntPtr promainhandle = pro[i].MainWindowHandle;
            //            IntPtr prohandle = pro[i].Handle;
            //            IntPtr fghandle = GetForegroundWindow();
            //            System.Windows.Forms.MessageBox.Show("메인 핸들 = " + rhinohandle + " 프로세스 핸들 = " + prohandle + " 열린창핸들 = " + fghandle
            //                );
            //            IntPtr hwndfirst = GetWindow(fghandle, 0);
            //            IntPtr hwndlast = GetWindow(fghandle, 1);
            //            IntPtr hwndnext = GetWindow(fghandle, 2);
            //            IntPtr hwndprev = GetWindow(fghandle, 3);
            //            IntPtr hwndowner = GetWindow(fghandle, 4);
            //            IntPtr hwndchild = GetWindow(fghandle, 5);

            //            System.Windows.Forms.MessageBox.Show("메인 핸들 = " + promainhandle + " hwndfirst = " + hwndfirst +
            //                " hwndlast = " + hwndlast + " hwndnext = " + hwndnext + " hwndprev = " + hwndprev + " hwndowner = " + hwndowner
            //                 + " hwndchild = " + hwndchild);


            //            List<IntPtr> handles = new List<IntPtr>();
            //            for (int j = 0; j < 6; j++)
            //            {
            //                IntPtr temp = GetWindow(fghandle, j);

            //                System.Windows.Forms.Control ctrl = System.Windows.Forms.Control.FromHandle(temp);

            //                if (ctrl == null)
            //                {
            //                    System.Windows.Forms.MessageBox.Show(j.ToString() + " = null");

            //                }
            //                else
            //                {
            //                    System.Windows.Forms.MessageBox.Show(ctrl.Top.ToString() + " , " + ctrl.Left.ToString());
            //                }

            //                RECT stRect = default(RECT);
            //                GetWindowRect((int)temp, ref stRect);
            //                System.Windows.Forms.MessageBox.Show(j.ToString() + "번 핸들 rect 값 좌상우하 = " + stRect.left + "," +
            //                    +stRect.top + "," + stRect.right + "," + stRect.bottom + ",");
            //            }
            //        }
            //    }
            //}
            //WindowPos mainwindow = new WindowPos();

            //int[] ltrb;
            //mainwindow.GetMainWindowPos(out ltrb);

            //for (int i = 0; i < ltrb.Length; i++)
            //{
            //    Rhino.RhinoApp.WriteLine(ltrb[i].ToString());
            //}
        }


    }

   



}
