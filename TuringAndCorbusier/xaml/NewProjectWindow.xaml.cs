using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Input.Custom;
using TuringAndCorbusier;

using Rhino;
using System.Linq;
using System.Collections.Generic;
using System.Windows.Input;
using Reports;
using TuringAndCorbusier.Datastructure_Settings;

namespace TuringAndCorbusier
{
    /// <summary>
    /// UserControl1.xaml에 대한 상호 작용 논리
    /// </summary>
    


    public partial class NewProjectWindow : Window
    {
        enum ButtonState
        {
            None = 0,
            GetWidth,
            GetHeight
        }

        public List<Point3d> points = new List<Point3d>();
        List<PointUnited> slopes = new List<PointUnited>();
        List<xaml.SlopeSetting> slopesettings = new List<xaml.SlopeSetting>();
        List<System.Guid> pointsguids = new List<System.Guid>();
        bool endWhileLoop = false;
        List<System.Guid> spguids = new List<System.Guid>();
        ButtonState buttonstate = ButtonState.None;
        public string plotType = "제 2종 일반 주거지역";
        public PlotType plotType2 = PlotType.제2종일반주거지역;
        
        /// <summary>
        /// 규현수정분
        /// </summary>
        private double LastClickedPlotType = -1;

        private void resetPlotTypeButton()
        {
            
            if (LastClickedPlotType == 1)
            {
                this.PlotType_1.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 158, 158, 158));
                this.PlotType_1.Foreground = System.Windows.Media.Brushes.White;
            }
            else if (LastClickedPlotType == 2)
            {
                this.PlotType_2.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 158, 158, 158));
                this.PlotType_2.Foreground = System.Windows.Media.Brushes.White;
            }
            else if (LastClickedPlotType == 3)
            {
                this.PlotType_3.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 158, 158, 158));
                this.PlotType_3.Foreground = System.Windows.Media.Brushes.White;
            }
            else if (LastClickedPlotType == 4)
            {
                this.Commercial.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 158, 158, 158));
                this.Commercial.Foreground = System.Windows.Media.Brushes.White;
            }
        }

        private void PlotType_1_Click(object sender, RoutedEventArgs e)
        {
            resetPlotTypeButton();
            LastClickedPlotType = 1;

            (sender as Button).Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 255, 204, 17));
            (sender as Button).Foreground = System.Windows.Media.Brushes.Black;
            this.plotType = "제 1종 일반 주거지역";
            plotType2 = PlotType.제1종일반주거지역;
            this.maxFloorAreaRatio.Text = "150";
            this.maxBuildingCoverage.Text = "60";
            this.maxFloors.Text = "4";
            Rhino.RhinoApp.WriteLine(plotType2.ToString());
        }

        private void PlotType_2_Click(object sender, RoutedEventArgs e)
        {
            resetPlotTypeButton();
            LastClickedPlotType = 2;

            (sender as Button).Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 255, 204, 17));
            (sender as Button).Foreground = System.Windows.Media.Brushes.Black;
            this.plotType = "제 2종 일반 주거지역";
            plotType2 = PlotType.제2종일반주거지역;
            this.maxFloorAreaRatio.Text = "200";
            this.maxBuildingCoverage.Text = "60";
            this.maxFloors.Text = "7";
            RhinoApp.WriteLine(plotType2.ToString());
        }

        private void PlotType_3_Click(object sender, RoutedEventArgs e)
        {
            resetPlotTypeButton();
            LastClickedPlotType = 3;

            (sender as Button).Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 255, 204, 17));
            (sender as Button).Foreground = System.Windows.Media.Brushes.Black;
            this.plotType = "제 3종 일반 주거지역";
            plotType2 = PlotType.제3종일반주거지역;
            this.maxFloorAreaRatio.Text = "250";
            this.maxBuildingCoverage.Text = "50";
            this.maxFloors.Text = "10";
            RhinoApp.WriteLine(plotType2.ToString());
            SpecialCase.IsChecked = false;
        }
        private void Commercial_Click(object sender, RoutedEventArgs e)
        {
            resetPlotTypeButton();
            LastClickedPlotType = 4;

            (sender as Button).Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 255, 204, 17));
            (sender as Button).Foreground = System.Windows.Media.Brushes.Black;
            this.plotType = "상업지역";
            plotType2 = PlotType.상업지역;
            this.maxFloorAreaRatio.Text = "1300";
            this.maxBuildingCoverage.Text = "80";
            this.maxFloors.Text = "30";
            RhinoApp.WriteLine(plotType2.ToString());
            SpecialCase.IsChecked = true;
        }
        private void EasterEgg_Click(object sender, RoutedEventArgs e)
        {
            resetPlotTypeButton();

            LastClickedPlotType = -1;
        }

        /// <summary>
        /// /규현수정분
        /// </summary>


        public NewProjectWindow()
        {
            InitializeComponent();


            


            if (TuringAndCorbusierPlugIn.InstanceClass.page1Settings != null)
            {
                projectName.Text = TuringAndCorbusierPlugIn.InstanceClass.page1Settings.ProjectName;
                address.Text = TuringAndCorbusierPlugIn.InstanceClass.page1Settings.Address;
                manualPlotArea.Text = TuringAndCorbusierPlugIn.InstanceClass.page1Settings.PlotArea.ToString();
                

                switch (TuringAndCorbusierPlugIn.InstanceClass.page1Settings.PlotType)
                {
                    case "제 1종 일반 주거지역":
                        PlotType_1_Click(PlotType_1, null);
                        break;
                    case "제 2종 일반 주거지역":
                        PlotType_2_Click(PlotType_2, null);
                        break;
                    case "제 3종 일반 주거지역":
                        PlotType_3_Click(PlotType_3, null);
                        break;
                    case "상업지역":
                        Commercial_Click(Commercial, null);
                        break;
                    default:
                        break;
                }

                maxBuildingCoverage.Text = TuringAndCorbusierPlugIn.InstanceClass.page1Settings.MaxBuildingCoverage.ToString();
                maxFloorAreaRatio.Text = TuringAndCorbusierPlugIn.InstanceClass.page1Settings.MaxFloorAreaRatio.ToString();
                maxFloors.Text = TuringAndCorbusierPlugIn.InstanceClass.page1Settings.MaxFloors.ToString();

               

            }

            


            else
            {
                LastClickedPlotType = -1;
                WindowStartupLocation = WindowStartupLocation.Manual;
            }

            if (TuringAndCorbusierPlugIn.InstanceClass.plot != null)
            {
                SpecialCase.IsChecked = TuringAndCorbusierPlugIn.InstanceClass.plot.isSpecialCase;
                NorthCheck.IsChecked = TuringAndCorbusierPlugIn.InstanceClass.plot.ignoreNorth;
            }
            else
            {
                SpecialCase.IsChecked = true;
            }
        }

        private void ButtonStateCheck(ButtonState after)
        {
            string before = buttonstate.ToString();
            string afters = after.ToString();
            switch (buttonstate)
            {
                case ButtonState.GetWidth:
                case ButtonState.GetHeight:
                    RhinoApp.SendKeystrokes("!Cancel", true);
                    buttonstate = after;
                    //RhinoApp.WriteLine(before + "에서" + afters);
                    break;
                case ButtonState.None:
                default:
                    buttonstate = after;
                    //RhinoApp.WriteLine(before + "에서" + afters);
                    break;
            }

        }

        private void Btn_Ok_Click(object sender, RoutedEventArgs e)
        {
            ButtonStateCheck(ButtonState.None);
            CloseWithoutOkClick();
        }
        /// <summary>
        /// 0518 민호 클릭하지 않아도 닫는 함수 생성. 종료 전 currentnewprojectwindow 변수 초기화
        /// </summary>
        public bool CloseWithoutOkClick()
        {
            ButtonStateCheck(ButtonState.None);

            if (TuringAndCorbusierPlugIn.InstanceClass.kdgInfoSet == null)
            {
                return false;
            }


            if (points.Count == 0)
            {
                var result = MessageBox.Show("대지의 경사 정보가 설정되지 않았습니다. 평평한 대지로 계속 하시겠습니까?", "경사 정보 없음", MessageBoxButton.YesNo);

                if (result == MessageBoxResult.No)
                    return false;
            }
            else
            {
                var result = MessageBox.Show("정보 입력을 마치고 설계를 시작합니다.", "창 닫기", MessageBoxButton.YesNo);

                if (result == MessageBoxResult.No)
                    return false;
            }
            RhinoDoc.SelectObjects -= SelectSlopePoint;

            endWhileLoop = true;

            TuringAndCorbusierPlugIn.InstanceClass.kdgInfoSet.Getdir(points);

            foreach (var gggg in spguids)
            {
                RhinoDoc.ActiveDoc.Objects.Hide(gggg, true);
            }

            if (NorthCheck.IsChecked == true)
                TuringAndCorbusierPlugIn.InstanceClass.plot.ignoreNorth = true;
            else
                TuringAndCorbusierPlugIn.InstanceClass.plot.ignoreNorth = false;

            if (SpecialCase.IsChecked == true)
                TuringAndCorbusierPlugIn.InstanceClass.plot.isSpecialCase = true;
            else
                TuringAndCorbusierPlugIn.InstanceClass.plot.isSpecialCase = false;

            RhinoApp.SetFocusToMainWindow();
            //System.Windows.Forms.SendKeys.SendWait("{ESC}");
            string manualPlotArea = this.manualPlotArea.Text;
            List<char> manualPlotAreaList = manualPlotArea.ToList();
            string manualPlotAreaValue = manualPlotArea;

            TuringAndCorbusierPlugIn.InstanceClass.page1Settings = new Settings_Page1(this.projectName.Text, this.address.Text, this.plotType, double.Parse(manualPlotAreaValue), double.Parse(this.maxFloorAreaRatio.Text), double.Parse(this.maxBuildingCoverage.Text), int.Parse(this.maxFloors.Text));
            if(TuringAndCorbusierPlugIn.InstanceClass.plot!=null)
                TuringAndCorbusierPlugIn.InstanceClass.plot.PlotType = this.plotType2;
            ((Rhino.UI.Panels.GetPanel(TuringHost.PanelId) as RhinoWindows.Controls.WpfElementHost).Child as Turing).RefreshProjectInfo();
            TuringAndCorbusierPlugIn.InstanceClass.currentNewProjectWindow = null;
            TuringAndCorbusierPlugIn.InstanceClass.turing.Calculate.Content = "설계 시작";
            TuringAndCorbusierPlugIn.InstanceClass.turing.Calculate.Click -= TuringAndCorbusierPlugIn.InstanceClass.turing.Btn_SetInputValues;
            TuringAndCorbusierPlugIn.InstanceClass.turing.Calculate.Click += TuringAndCorbusierPlugIn.InstanceClass.turing.Calculate_Click;

            


            UIManager.getInstance().HideWindow(this, UIManager.WindowType.Basic);

            return true;
        }

        /// <summary>
        /// get plot 과 set boundary 분리함.
        /// 재활용
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>

        private void Btn_GetPlot_Click(object sender, RoutedEventArgs e)
        {
            Hide();

            ButtonStateCheck(ButtonState.None);

            var gcc = new GetObject();

            gcc.SetCommandPrompt("select closed curve");
            gcc.GeometryFilter = Rhino.DocObjects.ObjectType.Curve;
            gcc.GeometryAttributeFilter = GeometryAttributeFilter.ClosedCurve;
            gcc.SubObjectSelect = false;

            gcc.Get();

            if (gcc.CommandResult() != Result.Success)
            {
                Show();
                return;
            }

            if (null == gcc.Object(0).Curve())
            {
                Show();
                return;
            }


            Curve boundary = gcc.Object(0).Curve();

            SetCurve(boundary);

        }

        private void SetCurve2(Curve boundary, double scalefactor)
        {
            KDGinfo tempKDGinfo = new KDGinfo(boundary, scalefactor, false);
            TuringAndCorbusierPlugIn.InstanceClass.kdgInfoSet = tempKDGinfo;

            Curve scaledBoundary = tempKDGinfo.boundary;

            var objectsToHide = Rhino.RhinoDoc.ActiveDoc.Objects.FindByObjectType(Rhino.DocObjects.ObjectType.AnyObject);

            foreach (var objectToHide in objectsToHide)
            {
                Rhino.RhinoDoc.ActiveDoc.Objects.Hide(objectToHide.Id, true);
            }


            var guid = LoadManager.getInstance().DrawObjectWithSpecificLayer(scaledBoundary, LoadManager.NamedLayer.Guide);
            //Rhino.RhinoDoc.ActiveDoc.Layers.SetCurrentLayerIndex(Rhino.RhinoDoc.ActiveDoc.Layers.Find("ZC304", true), true);
            //var guid = RhinoDoc.ActiveDoc.Objects.AddCurve(scaledBoundary);
            LoadManager.getInstance().DrawObjectWithSpecificLayer(tempKDGinfo.outrect, LoadManager.NamedLayer.Model);
            //var rectguid = RhinoDoc.ActiveDoc.Objects.AddCurve(tempKDGinfo.outrect);
            int index = TuringAndCorbusierPlugIn.InstanceClass.turing.stackPanel.Children.Count;


            /// checkpoint 테스트용
            /// 

            LoadManager.getInstance().DrawObjectWithSpecificLayer(tempKDGinfo.surrbuildings, LoadManager.NamedLayer.Model);

            //foreach (var surrcurve in TuringAndCorbusierPlugIn.InstanceClass.kdgInfoSet.surrbuildings)
            //{

            //    RhinoDoc.ActiveDoc.Objects.AddCurve(surrcurve);
            //}
            ///
            RhinoDoc.ActiveDoc.Objects.Select(guid);
            RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport.ZoomExtentsSelected();

            RhinoDoc.ActiveDoc.Views.Redraw();
            //RhinoDoc.ActiveDoc.Layers.SetCurrentLayerIndex(Rhino.RhinoDoc.ActiveDoc.Layers.Find("Default", true), true);
            TuringAndCorbusierPlugIn.InstanceClass.plot = new TuringAndCorbusier.Plot(scaledBoundary);
            TuringAndCorbusierPlugIn.InstanceClass.plot.PlotType = this.plotType2;
        }

        private void SetCurve(Curve boundary)
        {
            double plotArea_Manual = double.Parse(manualPlotArea.Text);

            double plotArea_CAD = CommonFunc.getArea(boundary);


            if (plotArea_Manual == 0)
            {
                manualPlotArea.Text = System.Math.Round(plotArea_CAD).ToString();
                plotArea_Manual = plotArea_CAD;
            }
            if (System.Math.Abs(plotArea_Manual - plotArea_CAD / 1000000) / plotArea_Manual >= 0.03)
            {
                var location = System.Windows.Forms.Control.MousePosition;
                plotAreaError tempPlotAreaError = new plotAreaError("(공부상면적 : " + System.Math.Round(plotArea_Manual, 0).ToString() + ", 캐드파일 면적 : " + System.Math.Round(plotArea_CAD , 0).ToString() + ")");

                tempPlotAreaError.Left = location.X - tempPlotAreaError.Width / 2;
                tempPlotAreaError.Top = location.Y - tempPlotAreaError.Height / 2;
                int tempDialogReturn = tempPlotAreaError.showDialogReturnValue();

                if (tempDialogReturn == 1)
                {
                    Show();
                    return;
                }
                else if (tempDialogReturn == 2)
                {

                    SetCurve2(boundary, System.Math.Pow(plotArea_Manual / (plotArea_CAD / 1000000), 0.5));

                    //TuringAndCorbusierPlugIn.InstanceClass.kdgInfoSet = tempKDGinfo;

                    //Curve scaledBoundary = tempKDGinfo.boundary;

                    //var objectsToHide = Rhino.RhinoDoc.ActiveDoc.Objects.FindByObjectType(Rhino.DocObjects.ObjectType.AnyObject);

                    //foreach (var objectToHide in objectsToHide)
                    //{
                    //    Rhino.RhinoDoc.ActiveDoc.Objects.Hide(objectToHide.Id, true);
                    //}


                    

                    //var guid = RhinoDoc.ActiveDoc.Objects.AddCurve(scaledBoundary);

                    //int index = TuringAndCorbusierPlugIn.InstanceClass.turing.stackPanel.Children.Count;


                    ///// checkpoint 테스트용
                    //foreach (var surrcurve in TuringAndCorbusierPlugIn.InstanceClass.kdgInfoSet.surrbuildings)
                    //{
                    //    RhinoDoc.ActiveDoc.Objects.AddCurve(surrcurve);
                    //}
                    /////
                    //RhinoDoc.ActiveDoc.Objects.Select(guid);
                    //RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport.ZoomExtentsSelected();

                    //RhinoDoc.ActiveDoc.Views.Redraw();

                    //TuringAndCorbusierPlugIn.InstanceClass.plot = new TuringAndCorbusier.Plot(scaledBoundary);
                }
                else
                {

                    SetCurve2(boundary, 1000);

                    //KDGinfo tempKDGinfo = new KDGinfo(boundary, 1000, false);
                    //TuringAndCorbusierPlugIn.InstanceClass.kdgInfoSet = tempKDGinfo;

                    //Curve scaledBoundary = tempKDGinfo.boundary;

                    //var objectsToHide = Rhino.RhinoDoc.ActiveDoc.Objects.FindByObjectType(Rhino.DocObjects.ObjectType.AnyObject);

                    //foreach (var objectToHide in objectsToHide)
                    //{
                    //    Rhino.RhinoDoc.ActiveDoc.Objects.Hide(objectToHide.Id, true);
                    //}




                    //var guid = RhinoDoc.ActiveDoc.Objects.AddCurve(scaledBoundary);

                    //int index = TuringAndCorbusierPlugIn.InstanceClass.turing.stackPanel.Children.Count;


                    ///// checkpoint 테스트용
                    //foreach (var surrcurve in TuringAndCorbusierPlugIn.InstanceClass.kdgInfoSet.surrbuildings)
                    //{
                    //    RhinoDoc.ActiveDoc.Objects.AddCurve(surrcurve);
                    //}
                    /////
                    //RhinoDoc.ActiveDoc.Objects.Select(guid);
                    //RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport.ZoomExtentsSelected();

                    //RhinoDoc.ActiveDoc.Views.Redraw();

                    //TuringAndCorbusierPlugIn.InstanceClass.plot = new TuringAndCorbusier.Plot(scaledBoundary);
                }
            }
            else
            {

                

                KDGinfo tempKDGinfo = new KDGinfo(boundary, System.Math.Pow(plotArea_Manual / (plotArea_CAD / 1000000), 1), false);
                TuringAndCorbusierPlugIn.InstanceClass.kdgInfoSet = tempKDGinfo;

                Curve scaledBoundary = tempKDGinfo.boundary;

                var objectsToHide = Rhino.RhinoDoc.ActiveDoc.Objects.FindByObjectType(Rhino.DocObjects.ObjectType.AnyObject);

                foreach (var objectToHide in objectsToHide)
                {
                    Rhino.RhinoDoc.ActiveDoc.Objects.Hide(objectToHide.Id, true);
                }

                var guid = RhinoDoc.ActiveDoc.Objects.AddCurve(scaledBoundary);
                RhinoDoc.ActiveDoc.Objects.Select(guid);
                RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport.ZoomExtentsSelected();

                RhinoDoc.ActiveDoc.Views.Redraw();

                TuringAndCorbusierPlugIn.InstanceClass.plot = new TuringAndCorbusier.Plot(boundary);
            }


            Show();
        }


        private void Button_Close_Click(object sender, RoutedEventArgs e)
        {
            ButtonStateCheck(ButtonState.None);
            endWhileLoop = true;
            RhinoApp.SetFocusToMainWindow();

            string manualPlotArea = this.manualPlotArea.Text;
            List<char> manualPlotAreaList = manualPlotArea.ToList();
            string manualPlotAreaValue = manualPlotArea;

            TuringAndCorbusierPlugIn.InstanceClass.page1Settings = new Settings_Page1(this.projectName.Text, this.address.Text, this.plotType, double.Parse(manualPlotAreaValue), double.Parse(this.maxFloorAreaRatio.Text), double.Parse(this.maxBuildingCoverage.Text), int.Parse(this.maxFloors.Text));
            ((Rhino.UI.Panels.GetPanel(TuringHost.PanelId) as RhinoWindows.Controls.WpfElementHost).Child as Turing).RefreshProjectInfo();
            /// <summary>
            /// 0518 민호 close 클릭시 currnetnewprojectwindow 초기화
            /// </summary>
            TuringAndCorbusierPlugIn.InstanceClass.currentNewProjectWindow = null;
            UIManager.getInstance().HideWindow(this, UIManager.WindowType.Basic);
        }

        private void Btn_SetWidth_Click(object sender, RoutedEventArgs e)
        {

            ButtonStateCheck(ButtonState.GetWidth);
            RhinoApp.SendKeystrokes("Cancel", true);

            RhinoApp.Wait();

            UIManager.getInstance().SnapSetter(UIManager.SnapMode.OffAll);



            if (TuringAndCorbusierPlugIn.InstanceClass.plot == null)
            {
                MessageBox.Show("먼저 커브를 선택하세요");
                
                return;
            }

            if (TuringAndCorbusierPlugIn.InstanceClass.plot.Boundary.ClosedCurveOrientation(Plane.WorldXY) == CurveOrientation.Clockwise)
                TuringAndCorbusierPlugIn.InstanceClass.plot.Boundary.Reverse();
      
            List<Curve> inputList = TuringAndCorbusierPlugIn.InstanceClass.plot.Boundary.DuplicateSegments().ToList();
            List<ConduitLine> tempConduitLine = new List<ConduitLine>();

            if (inputList.Count == TuringAndCorbusierPlugIn.InstanceClass.plot.Surroundings.Length)
            {
                foreach (Curve i in inputList)
                {
                    Rhino.Geometry.Line tempLine = new Rhino.Geometry.Line(i.PointAt(i.Domain.T0), i.PointAt(i.Domain.T1));
                    ConduitLine tempTempConduitLine = new ConduitLine(tempLine, TuringAndCorbusierPlugIn.InstanceClass.plot.Surroundings[inputList.IndexOf(i)]);

                    tempConduitLine.Add(tempTempConduitLine);
                }
            }
            else
            {
                TuringAndCorbusierPlugIn.InstanceClass.plot.Surroundings = new int[inputList.Count];

                foreach (Curve i in inputList)
                {
                    Rhino.Geometry.Line tempLine = new Rhino.Geometry.Line(i.PointAt(i.Domain.T0), i.PointAt(i.Domain.T1));
                    ConduitLine tempTempConduitLine = new ConduitLine(tempLine);
                    TuringAndCorbusierPlugIn.InstanceClass.plot.Surroundings[inputList.IndexOf(i)] = 4;
             
                    tempConduitLine.Add(tempTempConduitLine);
                }
            } 

            Corbusier.conduitLines.Clear();
            Corbusier.conduitLines = tempConduitLine;

            Corbusier.conduitLineDisplay = new LinesConduit(Corbusier.conduitLines);
            Corbusier.conduitLineDisplay.Enabled = true;
            RhinoDoc.ActiveDoc.Views.Redraw();

            var gcl = new GetConduitLine(Corbusier.conduitLines);

            RhinoApp.WriteLine("늘리거나 줄일 도로 선택 ( 클릭 = 늘리기 , Ctrl + 클릭 = 줄이기 , Shift + 클릭 = 전체 늘리기 , Shift + Ctrl + 클릭 = 전체 줄이기 , ESC = 마침");

            while (!endWhileLoop)
            {
                gcl.AcceptNothing(true);
                gcl.Get(true);
                RhinoDoc.ActiveDoc.Views.Redraw();

                if (gcl.CommandResult() != Rhino.Commands.Result.Success)
                    break;
            }


            if (TuringAndCorbusierPlugIn.InstanceClass.kdgInfoSet.boundary.ClosedCurveOrientation(Plane.WorldXY) == CurveOrientation.CounterClockwise)
                TuringAndCorbusierPlugIn.InstanceClass.kdgInfoSet.boundary.Reverse();

            Curve[] boundarysegs = TuringAndCorbusierPlugIn.InstanceClass.kdgInfoSet.boundary.DuplicateSegments();

     
            /////대지경계선후퇴적용
            //for (int i = 0; i < Corbusier.RoadWidth.Count; i++)
            //{

            //    if (Corbusier.RoadWidth[i] < 4 && Corbusier.RoadWidth[i] >= 0)
            //    {

            //        Vector3d curvev = boundarysegs[i].PointAtEnd - boundarysegs[i].PointAtStart;
            //        curvev.Rotate(RhinoMath.ToRadians(-90), Vector3d.ZAxis);
            //        curvev.Unitize();

            //        boundarysegs[i].Translate(curvev * ((Corbusier.RoadWidth[i] - 4) * 500));
            //        boundarysegs[i].Extend(CurveEnd.Both, 1000, CurveExtensionStyle.Line);

            //        LoadManager.getInstance().DrawObjectWithSpecificLayer(boundarysegs[i], LoadManager.NamedLayer.Guide);


            //    }       
            //}

            //for (int i = 0; i < boundarysegs.Length; i++)
            //{
            //    int i2 = (i + 1) % boundarysegs.Length;
            //    var intersection = Rhino.Geometry.Intersect.Intersection.CurveCurve(boundarysegs[i], boundarysegs[i2], 0, 0);
            //    foreach (var x in intersection)
            //    {
            //        if (x.ParameterA != 1)
            //            boundarysegs[i] = boundarysegs[i].Split(x.ParameterA)[0];

            //        if (x.ParameterB != 1)
            //            boundarysegs[i2] = boundarysegs[i2].Split(x.ParameterB)[1];
            //    }

            //}


            //List<Curve> tojoin = new List<Curve>();

            //for (int i = 0; i < boundarysegs.Length; i++)
            //{


            //}

            //Curve[] joined = Curve.JoinCurves(boundarysegs);

           

            //TuringAndCorbusierPlugIn.InstanceClass.kdgInfoSet.setbackBoundary = joined[0];
            //대지경계선후퇴 끝
            //kdginfoset 의 setbackBoundary에 저장.
            
            //LoadManager.getInstance().DrawObjectWithSpecificLayer(TuringAndCorbusierPlugIn.InstanceClass.kdgInfoSet.setbackBoundary, LoadManager.NamedLayer.Model);

            Corbusier.conduitLineDisplay.Enabled = false;
            TuringAndCorbusierPlugIn.InstanceClass.plot.Surroundings = Corbusier.conduitLines.Select(n => n.RoadWidth * 1000).ToArray();      //    .RoadWidth.Select(n=>n*1000).ToArray();
            TuringAndCorbusierPlugIn.InstanceClass.plot.UpdateSimplifiedSurroundings();
            UIManager.getInstance().SnapSetter(UIManager.SnapMode.Current);
            ButtonStateCheck(ButtonState.None);
            RhinoDoc.ActiveDoc.Views.Redraw();
            
       
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        public void RefreshWindow()
        {
            this.maxFloorAreaRatio.Text = "200";
            this.maxBuildingCoverage.Text = "60";
            this.maxFloors.Text = "7";
        }


        /// <summary>
        /// 새로운 경계 입력 함수
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>

            
        


        private void Btn_GetPlot_Copy1_Click(object sender, RoutedEventArgs e)
        {
            Btn_GetPlot_Copy1.Click -= Btn_GetPlot_Copy1_Click;
            //var gcc = new GetObject();
            //gcc.SetCommandPrompt("select closed curve");
            //gcc.GeometryFilter = Rhino.DocObjects.ObjectType.Curve;
            //gcc.GeometryAttributeFilter = GeometryAttributeFilter.ClosedCurve;
            //gcc.SubObjectSelect = false;
            //RhinoApp.RunScript("Polyline", true);
            //RhinoApp.RunScript("SelLast", true);



            ButtonStateCheck(ButtonState.None);
            Rhino.ApplicationSettings.ModelAidSettings.OsnapModes = Rhino.ApplicationSettings.OsnapModes.None;
            Rhino.ApplicationSettings.ModelAidSettings.OsnapModes = Rhino.ApplicationSettings.OsnapModes.End;


            var result = RhinoApp.RunScript("Polyline", true);

            Rhino.ApplicationSettings.ModelAidSettings.OsnapModes = Rhino.ApplicationSettings.OsnapModes.None;

            System.Guid guid = System.Guid.Empty;

            if (result)
            {
                guid = RhinoDoc.ActiveDoc.Objects.ElementAt(0).Id;
                var c = RhinoDoc.ActiveDoc.Objects.Find(guid);
                Rhino.DocObjects.CurveObject d = c as Rhino.DocObjects.CurveObject;
                Curve f = d.CurveGeometry;
                SetCurve(f);
            }
            else
            {
                Show();
                Btn_GetPlot_Copy1.Click += Btn_GetPlot_Copy1_Click;
                return;
            }


            //if (gcc.CommandResult() != Result.Success)
            //    return;
            //if (null == gcc.Object(0).Curve())
            //    return;

            //Curve boundary = gcc.Object(0).Curve();

            //SetCurve(boundary);








            //////자체구현 = 쓰레기 ////
            //    List<Point3d> points = new List<Point3d>();
            //    Point3d temp = Point3d.Unset;
            //    List<LineCurve> lines = new List<LineCurve>();
            //    Rhino.ApplicationSettings.ModelAidSettings.Osnap = true;
            //    List<System.Guid> guids = new List<System.Guid>();
            //    System.Guid mp = System.Guid.Empty;
            //    while(true)
            //    {

            //        var get = Rhino.Input.RhinoGet.GetPoint("select points", true, out temp);

            //        if (get != Result.Cancel)
            //        {
            //            points.Add(temp);

            //            if (points.Count >= 2)
            //            {
            //                LineCurve newline = new LineCurve(points[points.Count - 2], temp);
            //                lines.Add(newline);
            //                System.Guid newguid = RhinoDoc.ActiveDoc.Objects.AddCurve(newline);
            //                guids.Add(newguid);

            //            }

            //            if (points.Count > 2)
            //                if (points[0] == temp)
            //                {

            //                    break;

            //                }

            //        }


            //        else if(get == Result.Cancel) { break; }

            //        else if(get == Result.Nothing) { RhinoApp.WriteLine("zzz"); }
            //    }


            //    if (points.Count < 2)
            //        return;
            //    Curve[] joined = Curve.JoinCurves(lines);

            //    Rhino.ApplicationSettings.ModelAidSettings.Osnap = false;

            //    if (joined[0].IsClosed)
            //    {

            //        RhinoDoc.ActiveDoc.Objects.AddCurve(joined[0]);

            //        RhinoApp.Wait();

            //        SetCurve(joined[0]);
            //    }

            //    foreach (var c in guids)
            //    {
            //        RhinoDoc.ActiveDoc.Objects.Delete(c, true);  
            //    }

        }


        public List<Point3d> GetRoadPoints()
        {

            ButtonStateCheck(ButtonState.None);
            RhinoApp.SendKeystrokes("Cancel", true);
            RhinoApp.Wait();
            bool newPoint = true;
            List<Point3d> newpoints = new List<Point3d>();
            List<Rhino.DocObjects.PointObject> newpointobjects = new List<Rhino.DocObjects.PointObject>();
            while (newPoint)
            {

                newPoint = RhinoApp.RunScript("Point", false);
                var guid = RhinoDoc.ActiveDoc.Objects.ElementAt(0).Id;

                Rhino.DocObjects.PointObject pointobject = RhinoDoc.ActiveDoc.Objects.Find(guid) as Rhino.DocObjects.PointObject;

                newpointobjects.Add(pointobject);

                Rhino.Geometry.Point point = pointobject.PointGeometry;
                Point3d point3d = point.Location;
                newpoints.Add(point3d);



                if (!newPoint)
                {
                    var result = MessageBox.Show("Yes = 끝내기  No = 이전 취소  Cancle = 취소", "도로 선택 완료", MessageBoxButton.YesNoCancel);

                    if (result == MessageBoxResult.Cancel)
                    {
                        foreach (var pobj in newpointobjects)
                        {
                            Rhino.RhinoDoc.ActiveDoc.Objects.Delete(pobj, true);

                        }

                        Rhino.RhinoDoc.ActiveDoc.Views.Redraw();
            
                    }

                    else if (result == MessageBoxResult.No)
                    {

                        var lastguid = RhinoDoc.ActiveDoc.Objects.ElementAt(0).Id;
                        RhinoDoc.ActiveDoc.Objects.Delete(lastguid, true);

                        Rhino.RhinoDoc.ActiveDoc.Views.Redraw();

                        newPoint = true;
                    }

                }

                ////show/hide작업
            }

            return newpoints;

        }


        public List<System.Guid> ClickAbleSphere = new List<System.Guid>();
        public void SelectSlopePoint(object sender, Rhino.DocObjects.RhinoObjectSelectionEventArgs e)
        {
            if (e.RhinoObjects[0].ObjectType == Rhino.DocObjects.ObjectType.Brep)
            {
                for (int i = 0; i < ClickAbleSphere.Count; i++)
                {
                    if (ClickAbleSphere[i] == (e.RhinoObjects[0].Id))
                    {
                        slopesettings[i].Hide();
                        slopesettings[i].Update();
                        slopesettings[i].Show();
                    }
                    else
                        slopesettings[i].Hide();
                }
            }
        }

        private void Btn_GetSlope_Copy_Click(object sender, RoutedEventArgs e)
        {
            ButtonStateCheck(ButtonState.GetHeight);

            // !_Polyline !_ESC

            if (TuringAndCorbusierPlugIn.InstanceClass.plot == null)
            {
                MessageBox.Show("먼저 커브를 선택하세요");
                return;
            }
           


            //Curve c = TuringAndCorbusierPlugIn.InstanceClass.kdgInfoSet.outrect;
            //List<Point3d> segPoints = new List<Point3d>();
            //foreach (Curve d in c.DuplicateSegments())
            //{
            //    segPoints.Add(d.PointAtEnd);
            //    segPoints.Add(d.PointAtStart);
            //}

            //RhinoDoc.ActiveDoc.Layers.Add("temp", System.Drawing.Color.AliceBlue);
            //var layerindex = RhinoDoc.ActiveDoc.Layers.Find("temp", true);
            //var layer = RhinoDoc.ActiveDoc.Layers[layerindex];
            //RhinoDoc.ActiveDoc.Layers.SetCurrentLayerIndex(layerindex,true);

            ////layer.SetPersistentLocking(true);

            //BoundingBox boundary = new BoundingBox(segPoints);
            //PlaneSurface sfc = PlaneSurface.CreateThroughBox(Plane.WorldXY, boundary);

            //RhinoDoc.ActiveDoc.Objects.AddSurface(sfc);

            //RhinoDoc.ActiveDoc.Layers.SetCurrentLayerIndex(0, true);

            //layer.IsLocked = true;
            //layer.CommitChanges();

            bool loop = true;
            while (loop)
            {
                if (slopes.Count == 0)
                    RhinoDoc.SelectObjects += SelectSlopePoint;


               
                
                var newPoint = RhinoApp.RunScript("Point", false);
               

                if (newPoint == true)
                {
                    var guid = RhinoDoc.ActiveDoc.Objects.ElementAt(0).Id;
                   

                    var guidelement = RhinoDoc.ActiveDoc.Objects.Find(guid);
                    if (guidelement.ObjectType != Rhino.DocObjects.ObjectType.Point)
                        break;
                    pointsguids.Add(guid);
                    Point3d rPoint = (RhinoDoc.ActiveDoc.Objects.Find(guid) as Rhino.DocObjects.PointObject).PointGeometry.Location;
                    System.Guid SphereGuid = RhinoDoc.ActiveDoc.Objects.AddBrep(new Sphere(rPoint, 2000).ToBrep());
                    ClickAbleSphere.Add(SphereGuid);
                    PointUnited temp = new PointUnited(rPoint);
                    points.Add(rPoint);
                    slopes.Add(temp);
                    xaml.SlopeSetting tempsetting = new xaml.SlopeSetting(guid, SphereGuid,this, points.Count-1);
                    tempsetting.title.Text = "점 " + (slopesettings.Count + 1).ToString();
                    slopesettings.Add(tempsetting);
                }
                else
                {
                    loop = false;
                    ButtonStateCheck(ButtonState.None);
                    //Show();

                }
            }

        }

        public class PointUnited
        {
            public System.Guid SphereGuid;
            public Point3d rPoint;
            System.Windows.Point wPoint;
            

            public PointUnited(Point3d rpoint)
            {
                float mousex = System.Windows.Forms.Cursor.Position.X;
                float mousey = System.Windows.Forms.Cursor.Position.Y;
                
                rPoint = rpoint;
                wPoint = new System.Windows.Point(mousex,mousey);

                Rhino.Display.Text3d text3d = new Rhino.Display.Text3d(rpoint.Z.ToString());

                Rhino.RhinoDoc.ActiveDoc.Objects.AddText(text3d);
            }

        }

      
    }


    /// <summary>
    /// 경사 입력에 사용되는 점 클래스
    /// </summary>

    //public class SlopePoint : Window
    //{
    //    Point3d rPoint;
    //    System.Windows.Point wPoint;
    //    TextBox NumberInput = new TextBox();
    //    Rhino.Display.Text3d text3d;

    //    public SlopePoint(System.Windows.Point wpoint, Point3d rpoint)
    //    {

    //        Width = 1000;
    //        Height = 1000;
    //        rPoint = rpoint;
    //        wPoint = wpoint;
    //        //NumberInput = new TextBox();
    //        NumberInput.Text = "여기요";
    //        //NumberInput.ApplyTemplate


    //        MessageBox.Show(wpoint.X.ToString() + "," + wpoint.Y.ToString());

    //        //NumberInput.show
    //        text3d = new Rhino.Display.Text3d("dmdfkjhsdf");
    //        text3d.Text = "으아아아";

    //        Rhino.DocObjects.ObjectAttributes attributes = new Rhino.DocObjects.ObjectAttributes();

    //        var txt = RhinoDoc.ActiveDoc.Objects.AddText(text3d, attributes);



    //        NumberInput.Visibility = Visibility.Visible;
    //    }

    //    protected override void OnMouseEnter(MouseEventArgs e)
    //    {
    //        base.OnMouseEnter(e);
            
    //    }

   // }
}
