using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Rhino.Geometry;
using System.Collections.Generic;
using Rhino;
using Rhino.Commands;
using Rhino.Input.Custom;
using System.Diagnostics;
using System.Windows.Xps.Packaging;
using System.Windows.Xps;
using System.Windows.Documents;
using System;
using System.IO;
using System.Windows.Media.Imaging;
namespace TuringAndCorbusier
{
    /// <summary>
    /// UserControl1.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class Turing : System.Windows.Controls.UserControl
    {

        private Brush previousClickedButtonBrush = Brushes.White;
        private int previousClickedButtonIndex = -1;
        public int tempIndex = -1;
        private CurveConduit MainPanel_building2DPreview = new CurveConduit();
       
        private CurveConduit parkingLotPreview = new CurveConduit(System.Drawing.Color.DimGray);

        public List<List<System.Guid>> MainPanel_building3DPreview = new List<List<System.Guid>>();
        public List<Dictionary<string, string>> MainPanel_reportspaths = new List<Dictionary<string, string>>();

        List<Point3d> SlopePoints = new List<Point3d>();

        public List<Apartment> MainPanel_AGOutputList = new List<Apartment>();

        public string[] CurrentDataIdName = { "REGI_MST_NO", "REGI_SUB_MST_NO" };
        public string[] CurrentDataId = { CommonFunc.getStringFromRegistry("REGI_MST_NO"), CommonFunc.getStringFromRegistry("REGI_SUB_MST_NO") };

        public string USERID = CommonFunc.getStringFromRegistry("USERID");
        public string DBURL = CommonFunc.getStringFromRegistry("DBURL");
        
        List<FloorPlanLibrary> MainPanel_planLibraries = new List<FloorPlanLibrary>();

        /*
        public string[] CurrentDataIdName = { "REGI_MST_NO", "REGI_SUB_MST_NO" };
        public string[] CurrentDataId = { "123412341234", "123412341234" };

        public string USERID = "";
        public string DBURL = "";
        */

        public bool IsDataUploaded = false;

        public List<Window> popupwindows = new List<Window>();

        public Turing()
        {
            InitializeComponent();

            Calculate.Click += Btn_SetInputValues;


            try
            {
                this.ProjectName.Text = CommonFunc.getStringFromServer("REGI_BIZNS_NM", "TN_REGI_MASTER", CurrentDataIdName.ToList(), CurrentDataId.ToList())[0];
                
                this.ProjectAddress.Text = CommonFunc.getAddressFromServer(CurrentDataIdName.ToList(), CurrentDataId.ToList());

                
                this.ProjectArea.Text = Math.Round(CommonFunc.GetManualAreaFromServer(CurrentDataIdName.ToList(), CurrentDataId.ToList()), 2).ToString();
            }
            catch (System.Exception)
            {
                errorMessage tempError = new errorMessage("서버와 연결할 수 없습니다.");
            }

            bool is64 = System.Environment.Is64BitOperatingSystem;
            string name = "plantype";
            string dir = "";
            if (is64)
            {
                dir = "C://Program Files (x86)//Boundless//TuringAndCorbusier//DataBase//floorPlanLibrary";
                RhinoApp.WriteLine("fpldir64" + dir);
            }
            else
            {
                dir = "C://Program Files//Boundless//TuringAndCorbusier//DataBase//floorPlanLibrary";
                RhinoApp.WriteLine("fpldir32" + dir);
            }


            string[] allFiles = System.IO.Directory.GetFiles(dir);

            foreach (string file in allFiles)
            {
                if (file.Contains(name))
                {
                    try
                    {
                        FloorPlanLibrary tempFloorPlanLibrary = new FloorPlanLibrary(file);
                        MainPanel_planLibraries.Add(tempFloorPlanLibrary);
                    }
                    catch (System.Exception ex)
                    {
                        //MessageBox.Show(ex.ToString());
                        RhinoApp.WriteLine(ex.Message);
                    }
                }
            }

            TuringAndCorbusierPlugIn.InstanceClass.turing = mainWindow;
            TuringAndCorbusierPlugIn.InstanceClass.page1Settings = new Datastructure_Settings.Settings_Page1(ProjectName.Text, ProjectAddress.Text, "제 2종 일반 주거지역", double.Parse(ProjectArea.Text.Replace("m2","")), 200, 60, 7);
            List<System.Guid> dummy = new List<System.Guid>();
            Dictionary<string, string> pathdummy = new Dictionary<string, string>();
            for (int i = 0; i < 10; i++)
            {
                MainPanel_building3DPreview.Add(dummy);
                MainPanel_reportspaths.Add(pathdummy);
            }

        }

        public void ResetStackPanel(object sender, RoutedEventArgs e)
        {
            stackPanel.Children.Clear();
        }


        public void Menu_Click(object sender, RoutedEventArgs e)
        {
            if (UIManager.getInstance().menu != null)
                UIManager.getInstance().ShowWindow(TuringAndCorbusierPlugIn.InstanceClass.theOnlyMenuWindow, UIManager.WindowType.Menu);
        }

        //JHL
        private void MainPaenl_StackButton_Click(object sender, RoutedEventArgs e)
        {

            if (previousClickedButtonIndex != -1)
                (stackPanel.Children[previousClickedButtonIndex] as Button).Background = previousClickedButtonBrush;

            this.previousClickedButtonIndex = stackPanel.Children.IndexOf(sender as Button);
            this.previousClickedButtonBrush = (sender as Button).Background;

            (sender as Button).Background = new SolidColorBrush(Color.FromArgb(255, 255, 204, 0));




            tempIndex = stackPanel.Children.IndexOf(sender as System.Windows.Controls.Button);

            //법규선..땜시
            TuringAndCorbusierPlugIn.InstanceClass.plot.PlotType = MainPanel_AGOutputList[tempIndex].Plot.PlotType;

            ModelViewControl(tempIndex);

            RhinoApp.WriteLine(tempIndex.ToString());
            //JHL
            List<Curve> tempCurves = MainPanel_AGOutputList[stackPanel.Children.IndexOf(sender as System.Windows.Controls.Button)].drawEachHouse();


            tempCurves.AddRange(MainPanel_AGOutputList[stackPanel.Children.IndexOf(sender as System.Windows.Controls.Button)].drawEachCore());
            List<NurbsCurve> tempParkingLot = new List<NurbsCurve>();

            for(int i = 0; i < MainPanel_AGOutputList[tempIndex].ParkingLotOnEarth.ParkingLines.Count(); i++)
            {
                for(int j = 0; j < MainPanel_AGOutputList[tempIndex].ParkingLotOnEarth.ParkingLines[i].Count(); j++)
                {
                    tempParkingLot.Add(MainPanel_AGOutputList[tempIndex].ParkingLotOnEarth.ParkingLines[i][j].Boundary.ToNurbsCurve());
                }
            }

            if(MainPanel_AGOutputList[tempIndex].ParkingLotUnderGround.Ramp != null)
                tempParkingLot.Add(MainPanel_AGOutputList[tempIndex].ParkingLotUnderGround.Ramp.ToNurbsCurve());

            Curve[] tempParkingLotArr = Curve.JoinCurves(tempParkingLot);

            var tempoutput = MainPanel_AGOutputList[stackPanel.Children.IndexOf(sender as Button)];

            RhinoApp.WriteLine("tempoutputplottype = " + tempoutput.Plot.PlotType.ToString());

            bool using1F = tempoutput.ParameterSet.using1F;
            int stories = tempoutput.Household.Count();
            Plot tempPlot = tempoutput.Plot;

            MainPanel_building2DPreview.CurveToDisplay = tempCurves;
            MainPanel_LawPreview_North.CurveToDisplay = CommonFunc.LawLineDrawer.North(tempPlot, stories, using1F);
            MainPanel_LawPreview_NearPlot.CurveToDisplay = CommonFunc.LawLineDrawer.NearPlot(tempPlot, stories, using1F);
            MainPanel_LawPreview_Lighting.CurveToDisplay = CommonFunc.LawLineDrawer.Lighting(tempPlot, stories, tempoutput, using1F);
            MainPanel_LawPreview_Boundary.CurveToDisplay = CommonFunc.LawLineDrawer.Boundary(tempPlot, stories, using1F);
      
            List<string> widthlog;
            MainPanel_LawPreview_ApartDistance.CurveToDisplay = CommonFunc.LawLineDrawer.ApartDistance(tempoutput, out widthlog);
            MainPanel_LawPreview_ApartDistance.dimension = widthlog;
            //MainPanel_LawPreview_ApartDistance.dimPoint = MainPanel_LawPreview_ApartDistance.CurveToDisplay[0].
            // CommonFunc.JoinRegulation(tempoutput.Plot, tempoutput.Household.Count, tempoutput);
            MainPanel_building2DPreview.Enabled = true;
            parkingLotPreview.CurveToDisplay = tempParkingLotArr.ToList();
            parkingLotPreview.Enabled = true;
            RhinoDoc.ActiveDoc.Views.Redraw();
        }

        //public void UIChanged(Window window)
        //{
        //    double y = windowStartPoint.Y;

        //    if (popupwindows.Contains(window))
        //        popupwindows.Remove(window);


        //    else
        //        popupwindows.Add(window);

        //    foreach (Window w in popupwindows)
        //    {
        //        w.Left = windowStartPoint.X;
        //        w.Top = y;
        //        y += w.Height;
        //    }

        //MessageBox.Show("x : " + windowStartPoint.X + " y : " + y);

        //}
        public void Btn_SetInputValues(object sender, RoutedEventArgs e)
        {
            if (TuringAndCorbusierPlugIn.InstanceClass.currentNewProjectWindow != null)
                return;

            NewProjectWindow tempNewProject = new NewProjectWindow();
            //AddChild(tempNewProject);
            /// <summary>
            /// 0518 민호 newproject 클릭시 생성된 창 정보 정적변수에 전달
            /// </summary>
            TuringAndCorbusierPlugIn.InstanceClass.currentNewProjectWindow = tempNewProject;


            try { UIManager.getInstance().ShowWindow(tempNewProject, UIManager.WindowType.Basic); }

            catch (System.Exception)
            { }
        }
        private void Btn_GetPlot_Click(object sender, RoutedEventArgs e)
        {
            var gcc = new GetObject();
            gcc.SetCommandPrompt("select closed curve");
            gcc.GeometryFilter = Rhino.DocObjects.ObjectType.Curve;
            gcc.GeometryAttributeFilter = GeometryAttributeFilter.ClosedCurve;
            gcc.DisablePreSelect();
            //gcc.SubObjectSelect = false;
            gcc.Get();

            if (gcc.CommandResult() != Result.Success)
                return;
            if (null == gcc.Object(0).Curve())
                return;

            Curve boundary = gcc.Object(0).Curve();

            TuringAndCorbusierPlugIn.InstanceClass.plot = new TuringAndCorbusier.Plot(boundary);
        }

        //private void EditButton_Click(object sender, RoutedEventArgs e)
        //{
        //    EditProjectPropertyWindow tempNewProject = new EditProjectPropertyWindow();

        //    UIManager.getInstance().ShowWindow(tempNewProject, UIManager.WindowType.Basic);

        //}

        public void Calculate_Click(object sender, RoutedEventArgs e)
        {
            if (TuringAndCorbusierPlugIn.InstanceClass.plot == null)
            {
                errorMessage tempError = new errorMessage("Error : 대지 경계를 입력하세요.");
                tempError.ShowDialog();
                tempError.Activate();
                return;
            }

           


            /// <summary>
            /// 0518 민호 계산 시작시 newprojectwindow가 열려있으면 닫음
            /// </summary>
            if (TuringAndCorbusierPlugIn.InstanceClass.currentNewProjectWindow != null)
            {
                if (TuringAndCorbusierPlugIn.InstanceClass.currentNewProjectWindow.points.Count == 0)
                {
                    bool result = TuringAndCorbusierPlugIn.InstanceClass.currentNewProjectWindow.CloseWithoutOkClick();
                    if(!result)return;
                }
                else
                    TuringAndCorbusierPlugIn.InstanceClass.currentNewProjectWindow.CloseWithoutOkClick();
            }


            ///결과 나와있는데 다시 계산할때 모델링/라인 숨김
            ///

            if (tempIndex != -1)
            {
                HideCurrentModelView(tempIndex);
            }

            //TuringAndCorbusierPlugIn.InstanceClass.page1Settings = new Settings_Page1(projectName.Text, address.Text, double.Parse(plotArea.Text), double.Parse(maxFloorAreaRatio.Text), double.Parse(maxBuildingCoverage.Text), int.Parse(maxFloors.Text));

            MainPanel_building2DPreview.Enabled = false;
            parkingLotPreview.Enabled = false;
            Rhino.RhinoDoc.ActiveDoc.Views.Redraw();


            //TuringAndCorbusierPlugIn.InstanceClass.navigationHost.Top = TuringAndCorbusierPlugIn.InstanceClass.theOnlyMenuWindow.Top - 30;

            //TuringAndCorbusierPlugIn.InstanceClass.navigationHost.Left = TuringAndCorbusierPlugIn.InstanceClass.theOnlyMenuWindow.Left - TuringAndCorbusierPlugIn.InstanceClass.navigationHost.Width - 18;
            //TuringAndCorbusierPlugIn.InstanceClass.navigationHost.Show();



            ///AG3  / AG4 판별코드

            //ParameterSet parameterset4minimumPattern3 = new ParameterSet(new double[] { 3, 3, 7000, 0, 0 }, "PT-3", CoreType.Folded);
            //AG3 testAg3 = new AG3();
            //Target testTarget = new Target();
            //var test = testAg3.generator(TuringAndCorbusierPlugIn.InstanceClass.plot, parameterset4minimumPattern3, testTarget);

            ////if (test.Household.Count == 0)
            ////    RhinoApp.WriteLine("안.되.잖.아.요.");
            ////RhinoApp.WriteLine(test.GetBuildingCoverage().ToString());


            //if (!testAg3.isvalid)
            //{


            //    TuringAndCorbusierPlugIn.InstanceClass.page2.Btn_AG3_Click(this, null);
            //    TuringAndCorbusierPlugIn.InstanceClass.page2.Btn_AG3.Click -= TuringAndCorbusierPlugIn.InstanceClass.page2.Btn_AG3_Click;
            //    TuringAndCorbusierPlugIn.InstanceClass.page2.Toggle_AG3.Click -= TuringAndCorbusierPlugIn.InstanceClass.page2.Toggle_AG3_Click;

            //    TuringAndCorbusierPlugIn.InstanceClass.page2.ag3errorMsg.Visibility = Visibility.Visible;

            //    TuringAndCorbusierPlugIn.InstanceClass.page2.Btn_AG4_Click(this, null);
            //    TuringAndCorbusierPlugIn.InstanceClass.page2.Btn_AG4.Click -= TuringAndCorbusierPlugIn.InstanceClass.page2.Btn_AG4_Click;
            //    TuringAndCorbusierPlugIn.InstanceClass.page2.Toggle_AG4.Click -= TuringAndCorbusierPlugIn.InstanceClass.page2.Toggle_AG4_Click;

            //    TuringAndCorbusierPlugIn.InstanceClass.page2.ag4errorMsg.Visibility = Visibility.Visible;

            //    TuringAndCorbusierPlugIn.InstanceClass.page2.isAG34Valid = false;

            //    //// MessageBox.Show("부적합");
            //}


            //TuringAndCorbusierPlugIn.InstanceClass.page2.Btn_AG3.Click -= TuringAndCorbusierPlugIn.InstanceClass.page2.Btn_AG3_Click;
            //TuringAndCorbusierPlugIn.InstanceClass.page2.Btn_AG3.Click += TuringAndCorbusierPlugIn.InstanceClass.page2.Btn_AG3_Click;
            
            //TuringAndCorbusierPlugIn.InstanceClass.page2.ag3errorMsg.Visibility = Visibility.Hidden;
            //TuringAndCorbusierPlugIn.InstanceClass.page2.ag4errorMsg.Visibility = Visibility.Hidden;


                //TuringAndCorbusierPlugIn.InstanceClass.page2.Btn_AG3.Click += TuringAndCorbusierPlugIn.InstanceClass.page2.Btn_AG3_Click;

                // MessageBox.Show("적합");
            

            

            UIManager.getInstance().ShowWindow(TuringAndCorbusierPlugIn.InstanceClass.navigationHost, UIManager.WindowType.Navi);

        }

        public TextBlock createTextBlock(string text)
        {
            TextBlock tempTextBlock = new TextBlock();
            tempTextBlock.FontFamily = new FontFamily("Noto Sans CJK KR medium");
            tempTextBlock.FontSize = 10;
            tempTextBlock.TextAlignment = TextAlignment.Center;
            tempTextBlock.Padding = new Thickness(3);

            tempTextBlock.Text = text;

            return tempTextBlock;
        }

        public void AddButton(string AGName, Apartment AGOutput)
        {
            string[] stackPannelButtonStyle = { "stackPannelButtonStyle1", "stackPannelButtonStyle2" };

            System.Windows.Controls.Button btn = new System.Windows.Controls.Button();

            string tempStyle = stackPannelButtonStyle[stackPanel.Children.Count % 2];
            System.Windows.Style style = FindResource(tempStyle) as System.Windows.Style;

            Grid tempGrid = new Grid();
            tempGrid.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;
            tempGrid.Width = ((Rhino.UI.Panels.GetPanel(TuringHost.PanelId) as RhinoWindows.Controls.WpfElementHost).Width) - 34;

            for (int j = 0; j < 3; j++)
            {
                ColumnDefinition tempColDef = new ColumnDefinition();
                tempColDef.Width = new GridLength(1, GridUnitType.Star);
                tempGrid.ColumnDefinitions.Add(tempColDef);
            }
            System.Windows.Media.Brush textBrush = System.Windows.Media.Brushes.Black;

            ColumnDefinition lastColDef = new ColumnDefinition();
            lastColDef.Width = new GridLength(17, GridUnitType.Pixel);
            tempGrid.ColumnDefinitions.Add(lastColDef);

            TextBlock calculateNumsTextBlock = createTextBlock(CommonFunc.GetApartmentType(AGOutput));
            tempGrid.Children.Add(calculateNumsTextBlock);
            calculateNumsTextBlock.Foreground = textBrush;
            Grid.SetColumn(calculateNumsTextBlock, 0);

            TextBlock FloorAreaRatioTextBlock = createTextBlock((System.Math.Round(AGOutput.GetGrossAreaRatio(), 2)).ToString() + "%");
            tempGrid.Children.Add(FloorAreaRatioTextBlock);
            FloorAreaRatioTextBlock.Foreground = textBrush;
            Grid.SetColumn(FloorAreaRatioTextBlock, 1);

            TextBlock BuildingCoverageTextBlock = createTextBlock((System.Math.Round(AGOutput.GetBuildingCoverage(), 2)).ToString() + "%");
            tempGrid.Children.Add(BuildingCoverageTextBlock);
            BuildingCoverageTextBlock.Foreground = textBrush;
            Grid.SetColumn(BuildingCoverageTextBlock, 2);

            btn.FontSize = 10;
            btn.Style = style;
            btn.Content = tempGrid;
            btn.Height = 21;
            btn.Click += MainPaenl_StackButton_Click;
            ///test용
            //btn.Click += ShowOptionWindow;

            ///
            stackPanel.Children.Add(btn);
            MainPanel_AGOutputList.Add(AGOutput);

           // var makekdg = MessageBox.Show("건물이 있으면 안되는 블럭의 내부에 점을 찍으세요. 선택이 끝나면 esc", "도로 선택 완료", MessageBoxButton.YesNoCancel);
            //깍두기 최초 생성
            if (stackPanel.Children.Count == 1)
            {
                var wantset = MessageBox.Show("주변 대지모형을 생성","주변 대지 모형",MessageBoxButton.YesNo);
                if (wantset == MessageBoxResult.No)
                    return;

                var makekdg = MessageBox.Show("주변 건물을 생성합니다. 건물이 있으면 안되는 블럭(도로/공지)의 내부에 점을 찍으세요."
                    + System.Environment.NewLine + "선택이 끝나면 esc 후 완료버튼을, 잘못 입력했으면 esc 후 이전 취소를 눌러주세요.", "도로 선택 시작");

                SetKDG();

            }
        }



        public void RefreshProjectInfo()
        {
            this.ProjectName.Text = TuringAndCorbusierPlugIn.InstanceClass.page1Settings.ProjectName;
            this.ProjectAddress.Text = TuringAndCorbusierPlugIn.InstanceClass.page1Settings.Address;
            this.ProjectArea.Text = TuringAndCorbusierPlugIn.InstanceClass.page1Settings.PlotArea.ToString() + "m\xB2";
        }

        private void Btn_ExportReport_Click(object sender, RoutedEventArgs e)
        {


            try
            {
                GetBirdEye();
            }
            catch
            {
                RhinoApp.WriteLine("조감도 출력 실패");
            }

            try
            {
                List<string> pagename = new List<string>();
                List<FixedPage> fps = new List<FixedPage>();
                if (tempIndex == -1)
                {
                    errorMessage tempError = new errorMessage("설계안을 먼저 선택하세요.");
                    return;
                }

                List<System.Windows.Documents.FixedPage> FixedPageList = new List<System.Windows.Documents.FixedPage>();

                FixedDocument currentDoc = new FixedDocument();
                currentDoc.DocumentPaginator.PageSize = new Size(1240, 1753);

                List<Page> pagesToVIew = new List<Page>();

                //page1 표지

                //Reports.xmlcover cover = new Reports.xmlcover();
                string projectNameStr = TuringAndCorbusierPlugIn.InstanceClass.page1Settings.ProjectName;

                //fps.Add(cover.fixedPage);
                //pagename.Add("Cover");

                //page1.5 조감도

                //Reports.ImagePage imagepage = new Reports.ImagePage();
                //var bool1 = imagepage.setImage1("Export\\test\\test0.jpg");
                //var bool2 = imagepage.setImage2("Export\\test\\test1.jpg");
                //if (bool1 && bool2)
                //{
                //    fps.Add(imagepage.fixedPage);
                //    pagename.Add("Birdeye");
                //}

                //---------------------------------JHL---------------------//
                Reports.ReportCover reportCover = new Reports.ReportCover();
                var hasImage = reportCover.setImage("test1.jpeg");
                reportCover.SetTitle(projectNameStr);
                reportCover.SetPublishDate();
                //표지에 넣을 정보 값 리스트에 넣기
                Reports.xmlBuildingReport xmlBuildingInfo = new Reports.xmlBuildingReport(MainPanel_AGOutputList[tempIndex]);

                List<string> coverBuildingInfoList = new List<string>();

                coverBuildingInfoList.Add(xmlBuildingInfo.projectName.Text);
                coverBuildingInfoList.Add(xmlBuildingInfo.address.Text);
                coverBuildingInfoList.Add(xmlBuildingInfo.plotType.Text);
                coverBuildingInfoList.Add(xmlBuildingInfo.plotArea_Usable.Text);

                string buildingCoverage = xmlBuildingInfo.buildingCoverage.Text;
                buildingCoverage += xmlBuildingInfo.buildingCoverage_legal.Text;

                string floorAreaRatio = xmlBuildingInfo.floorAreaRatio.Text;
                floorAreaRatio += xmlBuildingInfo.floorAreaRatio_legal.Text;

                coverBuildingInfoList.Add(buildingCoverage);
                coverBuildingInfoList.Add(floorAreaRatio);
                coverBuildingInfoList.Add(xmlBuildingInfo.numOfHouseHolds.Text);

                reportCover.SetCoverBuildingInfo(coverBuildingInfoList);

                if (hasImage)
                {
                    fps.Add(reportCover.fixedPage);
                    pagename.Add("mainCover");
                }

                //page2 건축개요
                // 배치도 테스트
                BoundingBox tempBBox = TuringAndCorbusierPlugIn.InstanceClass.kdgInfoSet.outrect.GetBoundingBox(true);
                Rectangle3d tempRectangle = new Rectangle3d(Plane.WorldXY, tempBBox.Min, tempBBox.Max);
                TypicalPlan tempTypicalPlan_FL0 = TypicalPlan.DrawTypicalPlan(MainPanel_AGOutputList[tempIndex].Plot, tempRectangle, TuringAndCorbusierPlugIn.InstanceClass.kdgInfoSet.surrbuildings, MainPanel_AGOutputList[tempIndex], MainPanel_planLibraries, 2);
                List<Curve> coreOutline = MainPanel_AGOutputList[tempIndex].drawEachCore();
                List<Curve> houseOutline = MainPanel_AGOutputList[tempIndex].drawEachHouse();
                List<Curve> coreDetailOutline = MainPanel_AGOutputList[tempIndex].drawEachCoreDetail();
                xmlBuildingInfo.SetHouseOutline(coreOutline,coreDetailOutline,houseOutline, tempTypicalPlan_FL0);

                fps.Add(xmlBuildingInfo.fixedPage);
                pagename.Add("buildingReport");
                



                //page3~ 세대타입별 개요

                List<HouseholdStatistics> uniqueHouseHoldProperties = MainPanel_AGOutputList[tempIndex].HouseholdStatistics.ToList();

                List<string> typeString = MainPanel_AGOutputList[tempIndex].AreaTypeString();
                double coreAreaSum = MainPanel_AGOutputList[tempIndex].GetCoreAreaSum();
                double UGParkingLotAreaSum = MainPanel_AGOutputList[tempIndex].ParkingLotUnderGround.ParkingArea;
                double publicFacilityArea = MainPanel_AGOutputList[tempIndex].GetPublicFacilityArea();
                double serviceArea = -1000;

                //List<FloorPlan> floorPlans = (from i in uniqueHouseHoldProperties
                //                              select new FloorPlan(PlanDrawingFunction.alignHousholdProperties(i.ToHousehold()), MainPanel_planLibraries, MainPanel_AGOutputList[tempIndex].AGtype)).ToList();

                //List<Rectangle3d> boundingBoxes = (from i in floorPlans
                //                                  select new Rectangle3d(Plane.WorldXY, i.GetBoundingBox().Min, i.GetBoundingBox().Max)).ToList();

                //List<System.Windows.Point> origins = new List<System.Windows.Point>();
                //double scaleFactor = PlanDrawingFunction.calculateMultipleScaleFactor(Reports.unitPlanTemplate.GetUnitPlanRectangle(), boundingBoxes, out origins);



                //JHL
                
                    List<Reports.unitPlanTemplate> multipleUnitPlanList = new List<Reports.unitPlanTemplate>();
                for(int i = 0; i < uniqueHouseHoldProperties.Count(); i++)
                {
                    Household household = new Household(uniqueHouseHoldProperties[i].ToHousehold());
                    household.Origin = new Point3d(household.Origin.X, household.Origin.Y, 0);
                    Curve householdOutline = household.GetDefaultXYOutline();
                    Rectangle3d householdOutlineBoundingBox = new Rectangle3d(Plane.WorldXY, householdOutline.GetBoundingBox(true).Min,householdOutline.GetBoundingBox(true).Max);
                    System.Windows.Point origin = new System.Windows.Point();
                    double scaleFactor = PlanDrawingFunction.scaleToFitFactor(Reports.unitPlanTemplate.GetUnitPlanRectangle(),householdOutlineBoundingBox,out origin);

                    double exclusiveSum = MainPanel_AGOutputList[tempIndex].GetExclusiveAreaSum();
                    double exclusiveArea = household.GetExclusiveArea();
                    double tempCoreArea = coreAreaSum * exclusiveArea / exclusiveSum;
                    double tempParkingLotArea = UGParkingLotAreaSum / MainPanel_AGOutputList[tempIndex].GetExclusiveAreaSum() * household.GetExclusiveArea();
                    tempCoreArea += uniqueHouseHoldProperties[i].CorridorArea;

                    Reports.unitPlanTemplate unitPlanTemplate = new Reports.unitPlanTemplate(household, typeString[i], tempCoreArea, tempParkingLotArea, publicFacilityArea, serviceArea, uniqueHouseHoldProperties[i].Count);
                    unitPlanTemplate.SetUnitPlan(householdOutline, uniqueHouseHoldProperties[i], scaleFactor, origin, MainPanel_AGOutputList[tempIndex].AGtype);
                    multipleUnitPlanList.Add(unitPlanTemplate);
                }

                //세대 타입이 1개 일 경우
                if(multipleUnitPlanList.Count == 1)
                {
                    Reports.xmlUnitReport unitReport = new Reports.xmlUnitReport();
                    unitReport.SetFirstUnitTypePlan(multipleUnitPlanList[0]);
                    fps.Add(unitReport.fixedPage);
                    pagename.Add("newUnitReport" + 1.ToString());

                }
                else
                {
                for(int i = 0; i < multipleUnitPlanList.Count; i += 2)
                {
                        if (i == multipleUnitPlanList.Count && multipleUnitPlanList.Count%2!=0)
                        {
                            Reports.xmlUnitReport unitReport1 = new Reports.xmlUnitReport();
                            unitReport1.SetFirstUnitTypePlan(multipleUnitPlanList[i]);
                            fps.Add(unitReport1.fixedPage);
                            pagename.Add("newUnitReport" + (i + 1).ToString());
                        }
                        if (i < multipleUnitPlanList.Count - 2)
                        {
                        Reports.xmlUnitReport unitReport = new Reports.xmlUnitReport();
                        unitReport.SetUnitTypePlan(multipleUnitPlanList[i],multipleUnitPlanList[i+1]);
                        fps.Add(unitReport.fixedPage);
                        pagename.Add("newUnitReport" + (i + 1).ToString());
                        }
                }
                }

             
                //이전코드
                //for (int i = 0; i < uniqueHouseHoldProperties.Count(); i++)
                //{
                //    Household i_Copy = new Household(uniqueHouseHoldProperties[i].ToHousehold());

                //    i_Copy.Origin = new Point3d(i_Copy.Origin.X, i_Copy.Origin.Y, 0);

                //    double exclusiveSum = MainPanel_AGOutputList[tempIndex].GetExclusiveAreaSum();
                //    double exclusiveTemp = i_Copy.GetExclusiveArea();

                //    double tempCoreArea = coreAreaSum * exclusiveTemp / exclusiveSum;
                //    double tempParkingLotArea = UGParkingLotAreaSum / MainPanel_AGOutputList[tempIndex].GetExclusiveAreaSum() * i_Copy.GetExclusiveArea();
                //    tempCoreArea += uniqueHouseHoldProperties[i].CorridorArea;
                //    Reports.xmlUnitReport unitReport = new Reports.xmlUnitReport(i_Copy, typeString[i], tempCoreArea, tempParkingLotArea, publicFacilityArea, serviceArea, uniqueHouseHoldProperties[i].Count);
                //    //unitReport.setUnitPlan(uniqueHouseHoldProperties[i], floorPlans[i], scaleFactor, origins[i], MainPanel_AGOutputList[tempIndex].AGtype);

                //    fps.Add(unitReport.fixedPage);
                //    pagename.Add("unitReport" + (i + 1).ToString());
                //}

                //JHL
                for (int i = 0; i < MainPanel_AGOutputList[tempIndex].ParameterSet.Stories + 2; i++)
                {
                    try
                    {
                        if (i==0)
                        {

                            TypicalPlan typicalCorePlan = TypicalPlan.DrawTypicalPlan(MainPanel_AGOutputList[tempIndex].Plot, tempRectangle, TuringAndCorbusierPlugIn.InstanceClass.kdgInfoSet.surrbuildings, MainPanel_AGOutputList[tempIndex], MainPanel_planLibraries, i+1);
                            Reports.floorPlanDrawingPage floorPlanDrawing1 = new Reports.floorPlanDrawingPage(1);
                            floorPlanDrawing1.SetCoreOutline(coreOutline, coreDetailOutline, houseOutline, typicalCorePlan, new Interval(1, 1));
                            fps.Add(floorPlanDrawing1.fixedPage);
                            pagename.Add("floorPlanDrawingPage" + i.ToString());
                        }
                        List<HouseholdStatistics> householeStatisticsList = MainPanel_AGOutputList[tempIndex].HouseholdStatistics;
                        Reports.floorPlanDrawingPage floorPlanDrawing = new Reports.floorPlanDrawingPage(new Interval(i, i));
                        TypicalPlan testTypicalPlan = TypicalPlan.DrawTypicalPlan(MainPanel_AGOutputList[tempIndex].Plot, tempRectangle, TuringAndCorbusierPlugIn.InstanceClass.kdgInfoSet.surrbuildings, MainPanel_AGOutputList[tempIndex], MainPanel_planLibraries, i);
                        floorPlanDrawing.SetHouseOutline(coreOutline, coreDetailOutline, houseOutline, testTypicalPlan, new Interval(i,i));
                        //아파트 하우스 홀드 리스트 가져오기
                        List<List<List<Household>>> householdTripleList = MainPanel_AGOutputList[tempIndex].Household;
                        List<Household> householdList = new List<Household>();
                        foreach (List<List<Household>> householdDoubleList in householdTripleList)
                        {
                            foreach(List<Household> tempHouseholdList in householdDoubleList)
                            {
                                foreach(Household household in tempHouseholdList)
                                {
                                    householdList.Add(household);
                                }
                            }
                        }


                        floorPlanDrawing.SetHouseOutline(coreOutline, coreDetailOutline, houseOutline, testTypicalPlan,householdList,new Interval(i, i));

                        fps.Add(floorPlanDrawing.fixedPage);
                        pagename.Add("floorPlanDrawingPage" + (i+1).ToString());
                    }
                    catch (System.Exception EX)
                    {
                        continue;
                        //MessageBox.Show(EX.ToString());
                    }

                }

                    //Reports.wpfSection testSectionPage = new Reports.wpfSection();
                    //DrawSection drawsection = new DrawSection(MainPanel_AGOutputList[tempIndex]);

                //try
                //{
                //    //testSectionPage.setPlan(drawsection.Draw());
                //    fps.Add(testSectionPage.fixedPage);
                //    pagename.Add("sectionPage");
                //}
                //catch (Exception df)
                //{

                //}
                //finally
                //{

                //}

                var a = TuringAndCorbusierPlugIn.InstanceClass.showmewindow.showmeinit(fps, pagename, TuringAndCorbusierPlugIn.InstanceClass.page1Settings.ProjectName, MainPanel_AGOutputList[tempIndex], ref MainPanel_reportspaths,tempIndex);
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void Btn_Export3D_Click(object sender, RoutedEventArgs e)
        {
            if (tempIndex == -1)
            {
                errorMessage tempError = new errorMessage("설계안을 먼저 선택하세요.");
                return;
            }
            //0519 민호 창 중복 방지 , 창 위치 조정
            //if (TuringAndCorbusierPlugIn.InstanceClass.modelingComplexity != null)
            //    return;



            //var location = System.Windows.Forms.Control.MousePosition;

            //ModelingComplexity tempModelingComplexity = new ModelingComplexity(MainPanel_AGOutputList[tempIndex]);
            //TuringAndCorbusierPlugIn.InstanceClass.modelingComplexity = tempModelingComplexity;
            //tempModelingComplexity.Left = location.X - tempModelingComplexity.Width / 2;
            //tempModelingComplexity.Top = location.Y - tempModelingComplexity.Height / 2;
            //tempModelingComplexity.CaptureMouse();
            //tempModelingComplexity.Show();


            int index = tempIndex;

            try
            {
                if (TuringAndCorbusierPlugIn.InstanceClass.turing.MainPanel_building3DPreview[index] != null)
                    TuringAndCorbusierPlugIn.InstanceClass.turing.MainPanel_building3DPreview.RemoveAt(index);
            }
            catch (System.ArgumentOutOfRangeException)
            {

            }

            try
            {
                List<Brep>tempBreps = TuringAndCorbusier.MakeBuildings.makeBuildings(MainPanel_AGOutputList[index]);
                List<Guid> tempGuid = new List<Guid>();
                foreach (Brep i in tempBreps)
                {
                    tempGuid.Add(LoadManager.getInstance().DrawObjectWithSpecificLayer(i, LoadManager.NamedLayer.Model));
                }
                MainPanel_building3DPreview.Insert(index, tempGuid);
                RhinoDoc.ActiveDoc.Views.Redraw();
            }
            catch
            {
                RhinoApp.WriteLine("모델링-BREP생성실패");
            }
        }

        private void Btn_ExportElevation_Click(object sender, RoutedEventArgs e)
        {
            if (tempIndex == -1)
            {
                errorMessage tempError = new errorMessage("설계안을 먼저 선택하세요.");
                return;
            }

            //Elevation tempElevation = Elevation.drawElevation(MainPanel_AGOutputList[tempIndex].AptLines[0], MainPanel_AGOutputList[tempIndex].Household[0], MainPanel_AGOutputList[tempIndex].Core[0]);
            Section tempSection = Section.drawSection(MainPanel_AGOutputList[tempIndex].AptLines, MainPanel_AGOutputList[tempIndex].Household, MainPanel_AGOutputList[tempIndex].Core, MainPanel_AGOutputList[tempIndex].Plot);

            List<Curve> output = new List<Curve>(tempSection.Boundary);
            output.AddRange(tempSection.Room);

            foreach (Surroundinginfo i in tempSection.Surrounding)
            {
                output.Add(i.curve);
            }

            foreach (RoomNamecard i in tempSection.roomNamecard)
            {
                output.AddRange(i.Form);

                foreach (Rhino.Display.Text3d j in i.Text)
                {
                    RhinoDoc.ActiveDoc.Objects.AddText(j);
                }

            }

            foreach (Curve i in output)
            {
                RhinoDoc.ActiveDoc.Objects.AddCurve(i);
            }
        }

        public void sendDataToServer()
        {
            if (tempIndex == -1)
            {
                errorMessage tempError = new errorMessage("설계안을 먼저 선택하세요.");
                return;
            }

            try
            {
                ///출력 할 Apartment

                Apartment tempAGOutput = MainPanel_AGOutputList[tempIndex];

                ///현재 대지에 대한 DesignMaster입력 << 중복될 시 입력 안함

                if (!CommonFunc.checkDesignMasterPresence(CurrentDataIdName.ToList(), CurrentDataId.ToList()))
                {
                    List<Point3d> tempBoundaryPts = new List<Point3d>();

                    foreach (Curve i in MainPanel_AGOutputList[tempIndex].Plot.Boundary.DuplicateSegments())
                        tempBoundaryPts.Add(i.PointAt(i.Domain.T0));

                    CommonFunc.AddTdDesignMaster(CurrentDataIdName.ToList(), CurrentDataId.ToList(), USERID, (int)TuringAndCorbusierPlugIn.InstanceClass.page1Settings.MaxFloorAreaRatio, (int)TuringAndCorbusierPlugIn.InstanceClass.page1Settings.MaxBuildingCoverage, TuringAndCorbusierPlugIn.InstanceClass.page1Settings.MaxFloors, CommonFunc.GetPlotBoundaryVector(tempBoundaryPts), CommonFunc.ListToCSV(MainPanel_AGOutputList[tempIndex].Plot.Surroundings.ToList()), MainPanel_AGOutputList[tempIndex].Plot.GetArea());
                }


                ///가장 마지막 DESIGN_NO 다음 번호로 DesignDetail(설계 전체에 관한 내용) 입력


              
                int temp_REGI_PRE_DESIGN_NO;


                CommonFunc.AddDesignDetail(CurrentDataIdName.ToList(), CurrentDataId.ToList(), USERID, MainPanel_AGOutputList[tempIndex], out temp_REGI_PRE_DESIGN_NO);


                ///세대 타입 취합
                Stack<HouseholdStatistics> hhsStack = new Stack<HouseholdStatistics>();
                List<HouseholdStatistics> temphhs1 = new List<HouseholdStatistics>();

                temphhs1 = new List<HouseholdStatistics>(MainPanel_AGOutputList[tempIndex].HouseholdStatistics);

                double max = 0;
                int maxindex = -1;
                int index = temphhs1.Count;
                for (int j = 0; j < index; j++)
                {
                    //0 3      1  3  2  3   
                    max = 0;
                    for (int i = 0; i < index - j; i++)
                    {
                        if (temphhs1[i].ExclusiveArea > max)
                        {
                            max = temphhs1[i].ExclusiveArea;
                            maxindex = i;
                        }
                    }

                    hhsStack.Push(temphhs1[maxindex]);
                    temphhs1.RemoveAt(maxindex);

                }

                List<HouseholdStatistics> temphhs = new List<HouseholdStatistics>();
                List<HouseholdStatistics> tomerge = new List<HouseholdStatistics>();
                int time = hhsStack.Count;
                for (int i = 0; i < time; i++)
                {
                    if (tomerge.Count != 0)
                    {
                        if (Math.Abs(Math.Round(tomerge.Last().ExclusiveArea / 3.3 / 1000000) - Math.Round(hhsStack.First().ExclusiveArea / 3.3 / 1000000)) < 2)
                        {
                            tomerge.Add(hhsStack.Pop());
                        }

                        else
                        {
                            temphhs.Add(new HouseholdStatistics(tomerge));
                            RhinoApp.WriteLine(tomerge.Count.ToString() + " 개의 householdstatistics 가 병합됨.");
                            tomerge.Clear();
                            tomerge.Add(hhsStack.Pop());
                        }

                    }
                    else
                        tomerge.Add(hhsStack.Pop());


                    if (i == time - 1)
                    {
                        temphhs.Add(new HouseholdStatistics(tomerge));
                    }
                }


                ///각 세대 타입별 정보 입력

                //for (int i = 0; i < MainPanel_AGOutputList[tempIndex].HouseholdStatistics.Count; i++)
                //{

                //    HouseholdStatistics tempHouseholdStatistics = MainPanel_AGOutputList[tempIndex].HouseholdStatistics[i];
                //    double tempExclusiveAreaSum = tempAGOutput.GetExclusiveAreaSum();
                //    double tempExclusiveArea = tempHouseholdStatistics.GetExclusiveArea();

                //    double GroundCoreAreaPerHouse = tempAGOutput.GetCoreAreaOnEarthSum() / (tempExclusiveAreaSum + tempAGOutput.GetCommercialArea()) * tempExclusiveArea;
                //    double coreAreaPerHouse = GroundCoreAreaPerHouse + (tempAGOutput.GetCoreAreaSum() - tempAGOutput.GetCoreAreaOnEarthSum()) / tempExclusiveAreaSum * tempExclusiveArea;
                //    double parkingLotAreaPerHouse = tempAGOutput.ParkingLotUnderGround.GetAreaSum() / (tempExclusiveAreaSum + tempAGOutput.GetCommercialArea()) * tempExclusiveArea;
                //    double plotAreaPerHouse = tempAGOutput.Plot.GetArea() / (tempExclusiveAreaSum + tempAGOutput.GetCommercialArea()) * tempExclusiveArea;
                //    double welfareAreaPerHouse = tempAGOutput.GetPublicFacilityArea() / tempExclusiveAreaSum * tempExclusiveArea;
                //    double facilitiesAreaPerHouse = 0 / tempExclusiveAreaSum * tempExclusiveArea;

                //    CommonFunc.AddTdDesignArea(CurrentDataIdName.ToList(), CurrentDataId.ToList(), temp_REGI_PRE_DESIGN_NO, USERID, tempAGOutput.AreaTypeString()[i], coreAreaPerHouse, welfareAreaPerHouse, facilitiesAreaPerHouse, parkingLotAreaPerHouse, plotAreaPerHouse, tempHouseholdStatistics);

                //}

                for (int i = 0; i < temphhs.Count; i++)
                {

                    HouseholdStatistics tempHouseholdStatistics = temphhs[i];
                    double tempExclusiveAreaSum = tempAGOutput.GetExclusiveAreaSum();
                    double tempExclusiveArea = temphhs[i].GetExclusiveArea();
                    double rate = tempExclusiveArea / tempExclusiveAreaSum;
                    //double GroundCoreAreaPerHouse = tempAGOutput.GetCoreAreaOnEarthSum() / (tempExclusiveAreaSum + tempAGOutput.GetCommercialArea()) * tempExclusiveArea;

                    //ground , rooftop cores
                    double GroundCoreAreaPerHouse = tempAGOutput.Core[0].Sum(n => n.GetArea()) * 2 * rate;

                    //double coreareaSum = tempAGOutput.GetCoreAreaSum();
                    //double coreAreaPerHouse = GroundCoreAreaPerHouse + (tempAGOutput.GetCoreAreaSum() - tempAGOutput.GetCoreAreaOnEarthSum()) / tempExclusiveAreaSum * tempExclusiveArea;
                    //double floorCores = tempAGOutput.GetCoreAreaSum() - tempAGOutput.Core[0].Sum(n => n.GetArea()) * 2;
                    double coreAreaPerHouse = GroundCoreAreaPerHouse + (tempAGOutput.GetCoreAreaSum() - tempAGOutput.Core[0].Sum(n=>n.GetArea())*2)*rate;

                    double parkingLotAreaPerHouse = tempAGOutput.ParkingLotUnderGround.ParkingArea / (tempExclusiveAreaSum + tempAGOutput.GetCommercialArea()) * tempExclusiveArea;

                    double plotAreaPerHouse = tempAGOutput.Plot.GetArea() / (tempExclusiveAreaSum + tempAGOutput.GetCommercialArea()) * tempExclusiveArea;
                    double welfareAreaPerHouse = tempAGOutput.GetPublicFacilityArea() / tempExclusiveAreaSum * tempExclusiveArea;
                    double facilitiesAreaPerHouse = 0 / tempExclusiveAreaSum * tempExclusiveArea;

                    CommonFunc.AddTdDesignArea(CurrentDataIdName.ToList(), CurrentDataId.ToList(), temp_REGI_PRE_DESIGN_NO, USERID, "A", coreAreaPerHouse, welfareAreaPerHouse, facilitiesAreaPerHouse, parkingLotAreaPerHouse, plotAreaPerHouse, tempHouseholdStatistics);

                }

                CommonFunc.AddDesignModel(CurrentDataIdName.ToList(), CurrentDataId.ToList(), USERID, temp_REGI_PRE_DESIGN_NO, tempAGOutput.AGtype, CommonFunc.ListToCSV(tempAGOutput.ParameterSet.Parameters.ToList()), "Core", tempAGOutput.Target.Area, tempAGOutput.Target.Ratio);

                if (tempAGOutput.Commercial.Count != 0)
                {
                    double plotAreaOfCommercial = tempAGOutput.Plot.GetArea() / (tempAGOutput.GetExclusiveAreaSum() + tempAGOutput.GetCommercialArea()) * tempAGOutput.GetCommercialArea();
                    double coreAreaOfCommercial = tempAGOutput.GetCoreAreaOnEarthSum() / (tempAGOutput.GetExclusiveAreaSum() + tempAGOutput.GetCommercialArea()) * tempAGOutput.GetCommercialArea();
                    double parkingLotAreaOfCommercial = tempAGOutput.ParkingLotUnderGround.ParkingArea / (tempAGOutput.GetExclusiveAreaSum() + tempAGOutput.GetCommercialArea()) * tempAGOutput.GetCommercialArea();

                    CommonFunc.AddDesignNonResidential(CurrentDataIdName.ToList(), CurrentDataId.ToList(), USERID, temp_REGI_PRE_DESIGN_NO, CommonFunc.GetNonresiUseCode(CommonFunc.NonResiType.Commercial), tempAGOutput.GetCommercialArea(), 0, parkingLotAreaOfCommercial, (int)tempAGOutput.GetLegalParkingLotOfCommercial(), plotAreaOfCommercial);
                }


                RhinoApp.WriteLine("설계 보고서 업로드 시작");
                CommonFunc.AddTdDesignReport(CurrentDataIdName.ToList(), CurrentDataId.ToList(), MainPanel_reportspaths[tempIndex], temp_REGI_PRE_DESIGN_NO);



                if (tempAGOutput.PublicFacility.Count != 0)
                {
                    double plotAreaOfPublic = 0;
                    double parkingLotAreaOfPublic = 0;

                    CommonFunc.AddDesignNonResidential(CurrentDataIdName.ToList(), CurrentDataId.ToList(), USERID, temp_REGI_PRE_DESIGN_NO, CommonFunc.GetNonresiUseCode(CommonFunc.NonResiType.Commercial), tempAGOutput.GetCommercialArea(), 0, parkingLotAreaOfPublic, (int)tempAGOutput.GetLegalParkingLotOfCommercial(), plotAreaOfPublic);
                }


                //CommonFunc.AddDesignReport(CurrentDataIdName.ToList(), CurrentDataId.ToList(),temp_REGI_PRE_DESIGN_NO);


                ///임시 파일 경로 - 추가시 각 코드 밑에 넣기

                //string dummypath = "C:\\Users\\user\\Desktop\\Dummyfiles\\";

                //MainPanel_reportspaths[tempIndex].Add("ELEVATION", dummypath + "elevation.JPG");
                //MainPanel_reportspaths[tempIndex].Add("SECTION", dummypath + "section.JPG");
                //MainPanel_reportspaths[tempIndex].Add("DWG_PLANS", dummypath + "dwgplans.dwg");
                //MainPanel_reportspaths[tempIndex].Add("GROUND_PLAN", dummypath + "groundplan.JPG");
                //MainPanel_reportspaths[tempIndex].Add("TYPICAL_PLAN", dummypath + "typicalplan.JPG");
                //MainPanel_reportspaths[tempIndex].Add("BIRDEYE1", dummypath + "birdeye1.JPG");
                //MainPanel_reportspaths[tempIndex].Add("BIRDEYE2", dummypath + "birdeye2.JPG");






                RhinoApp.WriteLine("업로드 끝");

            }
            catch(Exception x)
            {
                RhinoApp.WriteLine("업로드 실패");
            }
        }

        private void Btn_SendToServer_Click(object sender, RoutedEventArgs e)
        {
            this.sendDataToServer();
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            Rhino.FileIO.FileReadOptions readoption = new Rhino.FileIO.FileReadOptions();

            readoption.ImportMode = true;

            Rhino.UI.OpenFileDialog ofd = new Rhino.UI.OpenFileDialog();

            ofd.Filter = "Rhino Files or Cad Files (*.3dm;*.dwg;)|*.3dm;*.dwg;";

            if (ofd.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                return;

            try
            {
                Rhino.FileIO.File3dm.Read(ofd.FileName);
            }
            catch (Rhino.FileIO.BinaryArchiveException f)
            {
                MessageBox.Show(f + " " + ofd.FileName + " 파일이 존재하지 않습니다");
            }

        }


        private void backGround_Loaded(object sender, RoutedEventArgs e)
        {
            TuringAndCorbusierPlugIn.InstanceClass.turing = this;
        }

        private void SetKDG()
        {

            try
            {

                var regacy = Rhino.RhinoDoc.ActiveDoc.Objects.FindByObjectType(Rhino.DocObjects.ObjectType.AnyObject);





                bool newPoint = true;
                List<Point3d> newpoints = new List<Point3d>();
                List<Rhino.DocObjects.PointObject> newpointobjects = new List<Rhino.DocObjects.PointObject>();
                while (newPoint)
                {




                    newPoint = RhinoApp.RunScript("Point", false);

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

                            return;
                        }

                        else if (result == MessageBoxResult.No)
                        {
                            System.Guid lastguid;

                            if (RhinoDoc.ActiveDoc.Objects.ElementAt(0).ObjectType == Rhino.DocObjects.ObjectType.Point)
                            {
                                lastguid = RhinoDoc.ActiveDoc.Objects.ElementAt(0).Id;

                                RhinoDoc.ActiveDoc.Objects.Delete(lastguid, true);

                                newpointobjects.RemoveAt(newpointobjects.Count - 1);

                                Rhino.RhinoDoc.ActiveDoc.Views.Redraw();

                                newPoint = true;
                            }
                            else
                            {

                                newPoint = true;
                            }

                        }

                    }

                    else
                    {

                        var guid = RhinoDoc.ActiveDoc.Objects.ElementAt(0).Id;

                        Rhino.DocObjects.PointObject pointobject = RhinoDoc.ActiveDoc.Objects.Find(guid) as Rhino.DocObjects.PointObject;

                        newpointobjects.Add(pointobject);

                        Rhino.Geometry.Point point = pointobject.PointGeometry;
                        Point3d point3d = point.Location;
                        newpoints.Add(point3d);
                    }

                }

                if (TuringAndCorbusierPlugIn.InstanceClass.kdgInfoSet == null)
                {
                    MessageBox.Show("깍두기정보없음");
                    return;
                }

                List<Curve> mg = new List<Curve>();

                mg.AddRange(TuringAndCorbusierPlugIn.InstanceClass.kdgInfoSet.surrbuildings);
                mg.Add(TuringAndCorbusierPlugIn.InstanceClass.kdgInfoSet.outrect);


                //FOR MEMORY CHECK

                //long mem1 = GC.GetTotalMemory(false);
                var kdgs = KDG.getInstance().KDGmaker(mg, TuringAndCorbusierPlugIn.InstanceClass.kdgInfoSet.boundary, newpoints, TuringAndCorbusierPlugIn.InstanceClass.kdgInfoSet.ground);
                //long mem2 = GC.GetTotalMemory(false);
                LoadManager.getInstance().DrawObjectWithSpecificLayer(kdgs, LoadManager.NamedLayer.ETC);
                //long mem3 = GC.GetTotalMemory(false);

                //Rhino.RhinoApp.WriteLine("addkdg : {0},{1},{2}", mem1, mem2, mem3);




                //Rhino.RhinoDoc.ActiveDoc.Layers.SetCurrentLayerIndex(Rhino.RhinoDoc.ActiveDoc.Layers.Find("ZC304", true), true);

                //foreach (var kdg in kdgs)
                //{
                //    RhinoDoc.ActiveDoc.Objects.AddBrep(kdg);
                //}

                //Rhino.RhinoDoc.ActiveDoc.Layers.SetCurrentLayerIndex(Rhino.RhinoDoc.ActiveDoc.Layers.Find("Default", true), true);

                foreach (var pointguid in newpointobjects)
                {
                    RhinoDoc.ActiveDoc.Objects.Hide(pointguid, true);
                }

                //RhinoDoc.ActiveDoc.Objects.AddBrep(TuringAndCorbusierPlugIn.InstanceClass.kdgInfoSet.ground);

                //TuringAndCorbusierPlugIn.InstanceClass.kdgInfoSet.HideRegacy();

                foreach (var zz in regacy)
                {
                    RhinoDoc.ActiveDoc.Objects.Hide(zz, true);
                }

            }
            catch(OutOfMemoryException e)
            {
                MessageBox.Show(e.Message);
            }
            
        }

        private void button_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (TuringAndCorbusierPlugIn.InstanceClass.theOnlyMenuWindow.IsVisible != true)     
            {
                //var location = (sender as System.Windows.Controls.Button).PointToScreen(new System.Windows.Point(0, 0));

                //TuringAndCorbusierPlugIn.InstanceClass.theOnlyMenuWindow.Left = location.X;

                //TuringAndCorbusierPlugIn.InstanceClass.theOnlyMenuWindow.Top = location.Y + (sender as System.Windows.Controls.Button).Height; //- TuringAndCorbusierPlugIn.InstanceClass.theOnlyMenuWindow.Height;

               
                //5019 민호 showdialog -> show
                UIManager.getInstance().ShowWindow(TuringAndCorbusierPlugIn.InstanceClass.theOnlyMenuWindow, UIManager.WindowType.Menu);
                //TuringAndCorbusierPlugIn.InstanceClass.theOnlyMenuWindow.Show();

            }
        }
        private void ModelViewControl(int index)
        {

            foreach (var guidlist in MainPanel_building3DPreview)
            {
                foreach (var guid in guidlist)
                {
                    RhinoDoc.ActiveDoc.Objects.Hide(guid, true);
                }
            }

            try
            {
                var asdf = MainPanel_building3DPreview[index];
            }
            catch (System.ArgumentOutOfRangeException)
            {
                //MessageBox.Show(f.Message);
                return;
            }

            foreach (var guid in MainPanel_building3DPreview[index])
            {
                RhinoDoc.ActiveDoc.Objects.Show(guid, true);
            }
        }

        private void HideCurrentModelView(int index)
        {

            foreach (var guidlist in MainPanel_building3DPreview)
            {
                foreach (var guid in guidlist)
                {
                    RhinoDoc.ActiveDoc.Objects.Hide(guid, true);
                }
            }

        }

        private void button_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            //if (TuringAndCorbusierPlugIn.InstanceClass.theOnlyMenuWindow.IsVisible == true)
               //UIManager.getInstance().HideWindow(TuringAndCorbusierPlugIn.InstanceClass.theOnlyMenuWindow, UIManager.WindowType.Menu);
        }

        private void TestButton_Click(object sender, RoutedEventArgs e)
        {
            //클릭 -> 점

            //점 다찍고 esc

            //하...하...하하.ㅎ.ㅎ.ㅎ.ㅏㅎ.하.하.


        }
   
        private void BirdEye_Click(object sender, RoutedEventArgs e)
        {
            bool is64 = Environment.Is64BitOperatingSystem;
            DirectoryInfo dirinfo = null;
            string dir64 = "C://Program Files (x86)//Boundless//TuringAndCorbusier//DataBase//temp//";
            string dir32 = "C://Program Files//Boundless//TuringAndCorbusier//DataBase//temp//";


            if (is64)
                dirinfo = new DirectoryInfo(dir64);
            else
                dirinfo = new DirectoryInfo(dir32);

            if (!dirinfo.Exists)
                dirinfo.Create();


            Point3d backupPoint = RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport.CameraLocation;
            Vector3d backupDir = RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport.CameraDirection;
            MainPanel_building2DPreview.Enabled = false;


            for (int i = 0; i < 2; i++)
            {
                Point3d center = TuringAndCorbusierPlugIn.InstanceClass.kdgInfoSet.center;
                Point3d tempcampos = TuringAndCorbusierPlugIn.InstanceClass.kdgInfoSet.campos[i] + Vector3d.ZAxis * new Vector3d(center - TuringAndCorbusierPlugIn.InstanceClass.kdgInfoSet.campos[i]).Length;
                Vector3d tempdir = new Vector3d(center - tempcampos);

              
                tempcampos = tempcampos - 2 * tempdir;
          
                RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport.SetCameraLocation(tempcampos, false);
                //RhinoDoc.ActiveDoc.Objects.AddLine(new Line(tempcampos, tempdir * 3));
                RhinoDoc.ActiveDoc.Views.ActiveView.Redraw();
                RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport.SetCameraDirection(new Vector3d(center - tempcampos), false);
                
                RhinoDoc.ActiveDoc.Views.ActiveView.Redraw();

                RhinoApp.Wait();
                //-----------JHL 조감도 ---------------//
                var bitmap = RhinoDoc.ActiveDoc.Views.ActiveView.CaptureToBitmap(new System.Drawing.Size(940, 665), Rhino.Display.DisplayModeDescription.FindByName("Rendered"));
                string path = dirinfo.FullName + "\\test" + i.ToString() + ".jpeg";
                bitmap.Save(path, System.Drawing.Imaging.ImageFormat.Jpeg);
                string key = "BIRDEYE" + (i + 1).ToString();
                MainPanel_reportspaths[tempIndex].Add(key, path);
                
            }
            MainPanel_building2DPreview.Enabled = true;
            RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport.SetCameraLocation(backupPoint, false);
            RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport.SetCameraDirection(backupDir, false);
            RhinoDoc.ActiveDoc.Views.ActiveView.Redraw();

        }

        public void GetBirdEye()
        {

            bool is64 = Environment.Is64BitOperatingSystem;
            DirectoryInfo dirinfo = null;
            string dir64 = "C://Program Files (x86)//Boundless//TuringAndCorbusier//DataBase//temp//";
            string dir32 = "C://Program Files//Boundless//TuringAndCorbusier//DataBase//temp//";


            if (is64)
                dirinfo = new DirectoryInfo(dir64);
            else
                dirinfo = new DirectoryInfo(dir32);

            if (!dirinfo.Exists)
                dirinfo.Create();

            Point3d backupPoint = RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport.CameraLocation;
            Vector3d backupDir = RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport.CameraDirection;
            MainPanel_building2DPreview.Enabled = false;

            for (int i = 0; i < 2; i++)
            {
                Point3d center = TuringAndCorbusierPlugIn.InstanceClass.kdgInfoSet.center;
                Point3d tempcampos = TuringAndCorbusierPlugIn.InstanceClass.kdgInfoSet.campos[i] + Vector3d.ZAxis * new Vector3d(center - TuringAndCorbusierPlugIn.InstanceClass.kdgInfoSet.campos[i]).Length;
                Vector3d tempdir = new Vector3d(center - tempcampos);
                tempcampos = tempcampos - 2 * tempdir;
                if (MainPanel_AGOutputList[tempIndex].Plot.PlotType == PlotType.상업지역)
                    tempcampos = tempcampos - 6 * tempdir;
                RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport.SetCameraLocation(tempcampos, false);
                //RhinoDoc.ActiveDoc.Objects.AddLine(new Line(tempcampos, tempdir * 3));
                RhinoDoc.ActiveDoc.Views.ActiveView.Redraw();
                RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport.SetCameraDirection(new Vector3d(center - tempcampos), false);

                RhinoDoc.ActiveDoc.Views.ActiveView.Redraw();

                RhinoApp.Wait();
                string path = "";
                using (var bitmap = RhinoDoc.ActiveDoc.Views.ActiveView.CaptureToBitmap(new System.Drawing.Size(1035, 1081), Rhino.Display.DisplayModeDescription.FindByName("Rendered")))
                {
                    
                    path = dirinfo.FullName + "test" + i.ToString() + ".jpeg";
                    //var files = dirinfo.GetFiles("test*.jpeg");
                    //foreach (var f in files)
                    //    f.Delete();
                    System.Drawing.Bitmap newbitmap = new System.Drawing.Bitmap(bitmap);
                    bitmap.Dispose();
                    
                    //var file = dirinfo.GetFiles("test" + i.ToString() + ".jpeg");
                    
                    

                    newbitmap.Save(path, System.Drawing.Imaging.ImageFormat.Jpeg);
                    newbitmap.Dispose();
                    
                }
                RhinoApp.Wait();


                string key = "BIRDEYE" + (i + 1).ToString();


                if (MainPanel_reportspaths[tempIndex].ContainsKey(key))
                    MainPanel_reportspaths[tempIndex].Remove(key);
                MainPanel_reportspaths[tempIndex].Add(key, path);

            }
            MainPanel_building2DPreview.Enabled = true;
            RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport.SetCameraLocation(backupPoint, false);
            RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport.SetCameraDirection(backupDir, false);
            RhinoDoc.ActiveDoc.Views.ActiveView.Redraw();
        }

        

        private void Btn_LawBoundary_Click(object sender, RoutedEventArgs e)
        {
            ChangeColor(sender as Button, 0);
        }

        private void Btn_LawNorth_Click(object sender, RoutedEventArgs e)
        {
            ChangeColor(sender as Button, 1);
        }

        private void Btn_LawNear_Click(object sender, RoutedEventArgs e)
        {
            ChangeColor(sender as Button, 2);
        }

        private void Btn_LawLighting_Click(object sender, RoutedEventArgs e)
        {
            ChangeColor(sender as Button, 3);
        }

        private void Btn_LawApart_Click(object sender, RoutedEventArgs e)
        {
            ChangeColor(sender as Button, 4);
        }

        bool[] lawlineActivated = new bool[] { false, false, false, false,false };
        SolidColorBrush[] brushes = new SolidColorBrush[] { Brushes.White, Brushes.Red, Brushes.Gold, Brushes.Green, Brushes.HotPink };
        public CurveConduit MainPanel_LawPreview_North = new CurveConduit(System.Drawing.Color.Red);
        public CurveConduit MainPanel_LawPreview_NearPlot = new CurveConduit(System.Drawing.Color.Gold);
        public CurveConduit MainPanel_LawPreview_Lighting = new CurveConduit(System.Drawing.Color.Green);
        public CurveConduit MainPanel_LawPreview_Boundary = new CurveConduit(System.Drawing.Color.White);
        public CurveConduit MainPanel_LawPreview_ApartDistance = new CurveConduit(System.Drawing.Color.HotPink);
        
        private void ChangeColor(Button sender, int index)
        {
            CurveConduit[] conduits = new CurveConduit[] { MainPanel_LawPreview_Boundary, MainPanel_LawPreview_North, MainPanel_LawPreview_NearPlot, MainPanel_LawPreview_Lighting , MainPanel_LawPreview_ApartDistance };
            lawlineActivated[index] = !lawlineActivated[index];
            if (lawlineActivated[index])
            {
                sender.Background = brushes[index];
                conduits[index].Enabled = true;
                sender.Foreground = new SolidColorBrush(Color.FromRgb(64, 64, 64));
            }
            else
            {
                conduits[index].Enabled = false;
                sender.Foreground = brushes[0];
                sender.Background = new SolidColorBrush(Color.FromRgb(64, 64, 64));
            }

            if (lawlineActivated.Where(n => n == true).Count() > 0)
                Btn_Lawline.Content = "전체 법규선 끄기";
            else if (lawlineActivated.Where(n => n == true).Count() == 0)
                Btn_Lawline.Content = "전체 법규선 보기";
            RhinoDoc.ActiveDoc.Views.ActiveView.Redraw();
        }

        private void Btn_Lawline_Click(object sender, RoutedEventArgs e)
        {
            if (tempIndex == -1)
                return;
            bool turn = true;
            if (lawlineActivated.Where(n => n == true).Count() == 0)
            {
                Btn_Lawline.Content = "전체 법규선 끄기";
                turn = true;

            }

            else
            {
                Btn_Lawline.Content = "전체 법규선 보기";
                turn = false;
            }

            Button[] buttons = new Button[] { Btn_LawBoundary, Btn_LawNorth, Btn_LawNear, Btn_LawLighting , Btn_LawApart};

            for (int i = 0; i < 5; i++)
            {
                if (lawlineActivated[i] != turn)
                {
                    ChangeColor(buttons[i], i);
                }
            }
            

            
            RhinoDoc.ActiveDoc.Views.Redraw();

        
        }

        private void Btn_Save_Click(object sender, RoutedEventArgs e)
        {

            if (TuringAndCorbusierPlugIn.InstanceClass.plot == null)
            {
                RhinoApp.WriteLine("설정된 대지가 없습니다.");
                return;
            }
            DataManager dm = new DataManager();
            string outname = "";
            Rhino.Input.RhinoGet.GetString("파일 이름 (영문,한글,숫자)", true, ref outname);

            if (outname == "")
            {
                RhinoApp.WriteLine("잘못된 이름입니다.");
                return;
            }

            ProjectData pd = new ProjectData() { projectName = outname, plot = new SerializablePlot(TuringAndCorbusierPlugIn.InstanceClass.plot,TuringAndCorbusierPlugIn.InstanceClass.kdgInfoSet), setting1=TuringAndCorbusierPlugIn.InstanceClass.page1Settings, regsetting = TuringAndCorbusierPlugIn.InstanceClass.regSettings};
            dm.SaveData(pd);
            
        }

        private void Btn_Load_Click(object sender, RoutedEventArgs e)
        {

           
           
            DataManager dm = new DataManager();

            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.InitialDirectory = dm.savepath;
            dlg.ShowDialog();
            string outname = dlg.FileName;
            //Rhino.Input.RhinoGet.GetString("파일이름이뭐요", true, ref outname);

            if (outname == "")
                return;

            ProjectData pd = dm.LoadData(outname);
            Plot plot = pd.plot.ToPlot();
            TuringAndCorbusierPlugIn.InstanceClass.plot = plot;
            Rhino.RhinoApp.WriteLine(pd.projectName + "로드됨");

            if (pd.setting1 != null)
                TuringAndCorbusierPlugIn.InstanceClass.page1Settings = pd.setting1;

            if (pd.regsetting != null)
                TuringAndCorbusierPlugIn.InstanceClass.regSettings = pd.regsetting;

            //KDGinfo tempKDGinfo = new KDGinfo(pd.plot.ToPlot().Boundary, 1, false);
            if (TuringAndCorbusierPlugIn.InstanceClass.kdgInfoSet == null)
                TuringAndCorbusierPlugIn.InstanceClass.kdgInfoSet = new KDGinfo(plot.Boundary, 1, true);
            TuringAndCorbusierPlugIn.InstanceClass.kdgInfoSet.outrect = plot.outrect;
            TuringAndCorbusierPlugIn.InstanceClass.kdgInfoSet.surrbuildings = plot.layout;
            TuringAndCorbusierPlugIn.InstanceClass.kdgInfoSet.boundary = plot.Boundary;
            //TuringAndCorbusierPlugIn.InstanceClass.kdgInfoSet. = plot.outrect;

            Curve scaledBoundary = plot.Boundary;

            var objectsToHide = Rhino.RhinoDoc.ActiveDoc.Objects.FindByObjectType(Rhino.DocObjects.ObjectType.AnyObject);

            foreach (var objectToHide in objectsToHide)
            {
                Rhino.RhinoDoc.ActiveDoc.Objects.Hide(objectToHide.Id, true);
            }



            var guid = LoadManager.getInstance().DrawObjectWithSpecificLayer(scaledBoundary, LoadManager.NamedLayer.Guide);
            LoadManager.getInstance().DrawObjectWithSpecificLayer(TuringAndCorbusierPlugIn.InstanceClass.kdgInfoSet.outrect, LoadManager.NamedLayer.Model);
            int index = TuringAndCorbusierPlugIn.InstanceClass.turing.stackPanel.Children.Count;
            LoadManager.getInstance().DrawObjectWithSpecificLayer(TuringAndCorbusierPlugIn.InstanceClass.kdgInfoSet.surrbuildings, LoadManager.NamedLayer.Model);

            RhinoDoc.ActiveDoc.Objects.Select(guid);
            RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport.ZoomExtentsSelected();

            RhinoDoc.ActiveDoc.Views.Redraw();


            Calculate.Content = "설계 시작";
            Calculate.Click -= Btn_SetInputValues;
            Calculate.Click += Calculate_Click;

            ProjectName.Text = TuringAndCorbusierPlugIn.InstanceClass.page1Settings.ProjectName;
            ProjectAddress.Text = TuringAndCorbusierPlugIn.InstanceClass.page1Settings.Address;
            ProjectArea.Text = Math.Round(TuringAndCorbusierPlugIn.InstanceClass.page1Settings.PlotArea).ToString();

            //MainPanel_building2DPreview.CurveToDisplay = tempCurves;
            //MainPanel_LawPreview_North.CurveToDisplay = CommonFunc.LawLines(plot, 6, true);
            //MainPanel_LawPreview_NearPlot.CurveToDisplay = CommonFunc.LawLines(plot, 6, false);
            //MainPanel_LawPreview_Lighting.CurveToDisplay = new List<Curve>() { new Regulation(6).RoadCenterLines(plot) };//CommonFunc.LawbyLighting(plot,6);
            //MainPanel_LawPreview_Boundary.CurveToDisplay = CommonFunc.LawLines(plot, 6);
            
           // string widthlog;
            //MainPanel_LawPreview_ApartDistance.CurveToDisplay = CommonFunc.ApartDistance(MainPanel_AGOutputList[tempIndex], out widthlog);
            //MainPanel_LawPreview_ApartDistance.dimension = widthlog;
            //MainPanel_LawPreview_ApartDistance.dimPoint = MainPanel_LawPreview_ApartDistance.CurveToDisplay[0].
            // CommonFunc.JoinRegulation(tempoutput.Plot, tempoutput.Household.Count, tempoutput);
            //MainPanel_building2DPreview.Enabled = true;
            //parkingLotPreview.CurveToDisplay = tempParkingLotArr.ToList();
            //parkingLotPreview.Enabled = true;
            RhinoDoc.ActiveDoc.Views.Redraw();

            //RhinoDoc.ActiveDoc.Layers.SetCurrentLayerIndex(Rhino.RhinoDoc.ActiveDoc.Layers.Find("Default", true), true);

        }


        //private void Btn_Export3D_Copy_Click(object sender, RoutedEventArgs e)
        //{
        //    Rhino.FileIO.FileReadOptions readoption = new Rhino.FileIO.FileReadOptions();

        //    readoption.ImportMode = true;

        //    Rhino.UI.OpenFileDialog ofd = new Rhino.UI.OpenFileDialog();

        //    ofd.InitialDirectory = "Export";



        //}
    }
}
