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



namespace TuringAndCorbusier
{
    /// <summary>
    /// UserControl1.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class Turing : System.Windows.Controls.UserControl
    {
        public int tempIndex = -1;
        private CurveConduit MainPanel_building2DPreview = new CurveConduit();
        private CurveConduit parkingLotPreview = new CurveConduit(System.Drawing.Color.DimGray);
        public List<ApartmentGeneratorOutput> MainPanel_AGOutputList = new List<ApartmentGeneratorOutput>();

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

        public Turing()
        {
            InitializeComponent();

            try
            {
                this.ProjectName.Text = CommonFunc.getStringFromServer("REGI_BIZNS_NM", "TN_REGI_MASTER", CurrentDataIdName.ToList(), CurrentDataId.ToList())[0];

                this.ProjectAddress.Text = CommonFunc.getAddressFromServer(CurrentDataIdName.ToList(), CurrentDataId.ToList());
            }
            catch (System.Exception)
            {
                errorMessage tempError = new errorMessage("서버와 연결할 수 없습니다.");
            }


            string name = "plantype";
            string[] allFiles = System.IO.Directory.GetFiles("C://Program Files (x86)//이주데이타//floorPlanLibrary//");

            foreach (string file in allFiles)
            {
                if (file.Contains(name))
                {
                    try
                    {
                        FloorPlanLibrary tempFloorPlanLibrary = new FloorPlanLibrary(file);
                        MainPanel_planLibraries.Add(tempFloorPlanLibrary);
                    }
                    catch(System.Exception)
                    {
                        
                    }
                }
            }

            /// 0519 민호 생성시 mainWindow 값 보냄.
            TuringAndCorbusierPlugIn.InstanceClass.turing = mainWindow;

        }


        public void Menu_Click(object sender, RoutedEventArgs e)
        {
            if (TuringAndCorbusierPlugIn.InstanceClass.theOnlyMenuWindow.IsVisible == true)
            {
                TuringAndCorbusierPlugIn.InstanceClass.theOnlyMenuWindow.Hide();
            }
            else
            {
                var location = (sender as System.Windows.Controls.Button).PointToScreen(new System.Windows.Point(0, 0));

                TuringAndCorbusierPlugIn.InstanceClass.theOnlyMenuWindow.Left = location.X;

                TuringAndCorbusierPlugIn.InstanceClass.theOnlyMenuWindow.Top = location.Y + (sender as System.Windows.Controls.Button).Height; //- TuringAndCorbusierPlugIn.InstanceClass.theOnlyMenuWindow.Height;

                //5019 민호 showdialog -> show

                TuringAndCorbusierPlugIn.InstanceClass.theOnlyMenuWindow.Show();

            }
        }

        private void MainPaenl_StackButton_Click(object sender, RoutedEventArgs e)
        {
            tempIndex = stackPanel.Children.IndexOf(sender as System.Windows.Controls.Button);

            List<Curve> tempCurves = MainPanel_AGOutputList[stackPanel.Children.IndexOf(sender as System.Windows.Controls.Button)].drawEachHouse();
            tempCurves.AddRange(MainPanel_AGOutputList[stackPanel.Children.IndexOf(sender as System.Windows.Controls.Button)].drawEachCore());

            List<NurbsCurve> tempParkingLot = MainPanel_AGOutputList[tempIndex].ParkingLotOnEarth.ParkingLines.Select(n => n.Boundary.ToNurbsCurve()).ToList();

            Curve[] tempParkingLotArr = Curve.JoinCurves(tempParkingLot);

            MainPanel_building2DPreview.CurveToDisplay = tempCurves;
            MainPanel_building2DPreview.Enabled = true;
            RhinoDoc.ActiveDoc.Views.Redraw();

            parkingLotPreview.CurveToDisplay = tempParkingLotArr.ToList();
            parkingLotPreview.Enabled = true;
            RhinoDoc.ActiveDoc.Views.Redraw();
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

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            EditProjectPropertyWindow tempNewProject = new EditProjectPropertyWindow();
            tempNewProject.ShowDialog();
           
        }

        private void Calculate_Click(object sender, RoutedEventArgs e)
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
                TuringAndCorbusierPlugIn.InstanceClass.currentNewProjectWindow.CloseWithoutOkClick();
                
            }

            //TuringAndCorbusierPlugIn.InstanceClass.page1Settings = new Settings_Page1(projectName.Text, address.Text, double.Parse(plotArea.Text), double.Parse(maxFloorAreaRatio.Text), double.Parse(maxBuildingCoverage.Text), int.Parse(maxFloors.Text));

            MainPanel_building2DPreview.Enabled = false;
            parkingLotPreview.Enabled = false;
            Rhino.RhinoDoc.ActiveDoc.Views.Redraw();


            TuringAndCorbusierPlugIn.InstanceClass.navigationHost.Top = TuringAndCorbusierPlugIn.InstanceClass.theOnlyMenuWindow.Top-30;

            TuringAndCorbusierPlugIn.InstanceClass.navigationHost.Left = TuringAndCorbusierPlugIn.InstanceClass.theOnlyMenuWindow.Left - TuringAndCorbusierPlugIn.InstanceClass.navigationHost.Width - 18;
            TuringAndCorbusierPlugIn.InstanceClass.navigationHost.Show();

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

        public void AddButton(string AGName, ApartmentGeneratorOutput AGOutput)
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

            ColumnDefinition lastColDef = new ColumnDefinition();
            lastColDef.Width = new GridLength(17, GridUnitType.Pixel);
            tempGrid.ColumnDefinitions.Add(lastColDef);

            TextBlock calculateNumsTextBlock = createTextBlock(CommonFunc.GetApartmentType(AGOutput));
            tempGrid.Children.Add(calculateNumsTextBlock);
            Grid.SetColumn(calculateNumsTextBlock, 0);

            TextBlock FloorAreaRatioTextBlock = createTextBlock((System.Math.Round( AGOutput.GetGrossAreaRatio(), 2)).ToString() + "%");
            tempGrid.Children.Add(FloorAreaRatioTextBlock);
            Grid.SetColumn(FloorAreaRatioTextBlock, 1);

            TextBlock BuildingCoverageTextBlock = createTextBlock((System.Math.Round(AGOutput.GetBuildingCoverage(), 2)).ToString() + "%");
            tempGrid.Children.Add(BuildingCoverageTextBlock);
            Grid.SetColumn(BuildingCoverageTextBlock, 2);

            btn.FontSize = 10;
            btn.Style = style;
            btn.Content = tempGrid;
            btn.Height = 21;
            btn.Click += MainPaenl_StackButton_Click;

            stackPanel.Children.Add(btn);
            MainPanel_AGOutputList.Add(AGOutput);
        }

        public void RefreshProjectInfo()
        {
            this.ProjectName.Text = TuringAndCorbusierPlugIn.InstanceClass.page1Settings.ProjectName;
            this.ProjectAddress.Text = TuringAndCorbusierPlugIn.InstanceClass.page1Settings.Address;
            this.ProjectArea.Text = TuringAndCorbusierPlugIn.InstanceClass.page1Settings.PlotArea.ToString() + "m\xB2";
        }

        private void Btn_ExportReport_Click(object sender, RoutedEventArgs e)
        {
            List<string> pagename = new List<string>();
            List<FixedPage> fps = new List<FixedPage>();
            if (tempIndex == -1)
            {
                errorMessage tempError = new errorMessage("설계안을 먼저 선택하세요.");
                return;
            }

            // 배치도 테스트

            List<CorePlan> tempCorePlans = new List<CorePlan>();
            List<FloorPlan> tempFloorPlans = new List<FloorPlan>();

            foreach(List<CoreProperties> i in MainPanel_AGOutputList[tempIndex].CoreProperties)
            {
                foreach(CoreProperties j in i)
                {
                    tempCorePlans.Add(new CorePlan(j));
                }
            }

            for (int i = 0; i < MainPanel_AGOutputList[tempIndex].HouseholdProperties.Count(); i++)
            {
                for (int j = 0; j < MainPanel_AGOutputList[tempIndex].HouseholdProperties[i][0].Count(); j++)
                {
                    tempFloorPlans.Add(new FloorPlan(MainPanel_AGOutputList[tempIndex].HouseholdProperties[i][0][j], MainPanel_planLibraries));
                }
            }

            foreach(CorePlan i in tempCorePlans)
            {
                List<Curve> tempCrvs = i.normals;
                tempCrvs.AddRange(i.others);
                tempCrvs.AddRange(i.walls);
                
                foreach(Curve j in tempCrvs)
                    RhinoDoc.ActiveDoc.Objects.AddCurve(j);
            }


            foreach (FloorPlan i in tempFloorPlans)
            {
                List<Curve> tempCrvs = i.all;

                foreach (Curve j in tempCrvs)
                    RhinoDoc.ActiveDoc.Objects.AddCurve(j);
            }

            // 배치도 테스트 끝

            List<System.Windows.Documents.FixedPage> FixedPageList = new List<System.Windows.Documents.FixedPage>();

            FixedDocument currentDoc = new FixedDocument();
            currentDoc.DocumentPaginator.PageSize = new Size(1240, 1753);

            List<Page> pagesToVIew = new List<Page>();

            //page1 표지

            Reports.xmlcover cover = new Reports.xmlcover();       
            cover.SetTitle = TuringAndCorbusierPlugIn.InstanceClass.page1Settings.ProjectName;

            fps.Add(cover.fixedPage);
            pagename.Add("Cover");

            //page2 건축개요

            Reports.xmlBuildingReport buildingReport = new Reports.xmlBuildingReport(MainPanel_AGOutputList[tempIndex]);

            fps.Add(buildingReport.fixedPage);
            pagename.Add("buildingReport");

            //page3~ 세대타입별 개요

            List<HouseholdStatistics> uniqueHouseHoldProperties = MainPanel_AGOutputList[tempIndex].HouseholdStatistics.ToList();

            List<string> typeString = MainPanel_AGOutputList[tempIndex].AreaTypeString();
            double coreAreaSum = MainPanel_AGOutputList[tempIndex].GetCoreAreaSum();
            double UGParkingLotAreaSum = MainPanel_AGOutputList[tempIndex].ParkingLotUnderGround.GetAreaSum();
            double publicFacilityArea = MainPanel_AGOutputList[tempIndex].GetPublicFacilityArea();
            double serviceArea = -1000; //*****

            for(int i = 0; i < uniqueHouseHoldProperties.Count(); i++)
            {
                HouseholdProperties i_Copy = new HouseholdProperties(uniqueHouseHoldProperties[i].ToHouseholdProperties());

                i_Copy.Origin = new Point3d(i_Copy.Origin.X, i_Copy.Origin.Y, 0);

                double tempCoreArea = coreAreaSum / MainPanel_AGOutputList[tempIndex].GetExclusiveAreaSum() * i_Copy.GetExclusiveArea();
                double tempParkingLotArea = UGParkingLotAreaSum / MainPanel_AGOutputList[tempIndex].GetExclusiveAreaSum() * i_Copy.GetExclusiveArea();

                Reports.xmlUnitReport unitReport = new Reports.xmlUnitReport(i_Copy,typeString[i], tempCoreArea, tempParkingLotArea,publicFacilityArea, serviceArea, uniqueHouseHoldProperties[i].Count);
                unitReport.setUnitPlan(uniqueHouseHoldProperties[i], MainPanel_planLibraries);

                fps.Add(unitReport.fixedPage);
                pagename.Add("unitReport" + (i + 1).ToString());
            }            

            //page4
            /*
            //아직 평면 드로잉 안끝남 (20160504);

            ImageFilePath = generateImageFileName(imageFileName, tempImageNumber);
            Report.BasicPage plans = new Report.BasicPage("배치도");
            plans.SetTypicalPlan = typicalPlan.drawTipicalPlan(MainPanel_AGOutputList[tempIndex].HouseHoldProperties, MainPanel_AGOutputList[tempIndex].CoreProperties, MainPanel_AGOutputList[tempIndex].buildingOutline, MainPanel_AGOutputList[tempIndex].Plot, 0);

            List<Rectangle3d> tempParkingLotBoundary = new List<Rectangle3d>();

            foreach(ParkingLot i in MainPanel_AGOutputList[tempIndex].ParkingLot)
            {
                tempParkingLotBoundary.Add(i.Boundary);
            }

            FixedPageList.Add(GeneratePDF.CreateFixedPage(plans));
            /*

            //page5

            Report.BasicPage section = new Report.BasicPage("단면도");

            FixedPageList.Add(GeneratePDF.CreateFixedPage(section));
            */

            //page6

            /*

            Reports.xmlRegulationCheck regCheck = new Reports.xmlRegulationCheck(MainPanel_AGOutputList[tempIndex]);

            fps.Add(regCheck.fixedPage);
            pagename.Add("regCheck");
            */

            TuringAndCorbusierPlugIn.InstanceClass.showmewindow.showmeinit(fps, pagename,TuringAndCorbusierPlugIn.InstanceClass.page1Settings.ProjectName);


            RhinoApp.WriteLine(pagename.ToArray().Length.ToString());

            TuringAndCorbusierPlugIn.InstanceClass.showmewindow.Show();
            //TuringAndCorbusierPlugIn.InstanceClass.showmewindow

            //pdf 생성

            //GeneratePDF.SaveFixedDocument(FixedPageList);
            //GeneratePDF.CreatePortableFile(userControlList, "C://Program Files (x86)//이주데이타//가로주택정비//" + "testFile.xps");
            /*
            GeneratePDF.CreaterPortableFile(FixedPageList, "C://Program Files (x86)//이주데이타//가로주택정비//" + "testFile.xps");
            GeneratePDF.SavePdfFromXps();
            */
        }

        private void Btn_Export3D_Click(object sender, RoutedEventArgs e)
        {
            if (tempIndex == -1)
            {
                errorMessage tempError = new errorMessage("설계안을 먼저 선택하세요.");
                return;
            }
            //0519 민호 창 중복 방지 , 창 위치 조정
            if (TuringAndCorbusierPlugIn.InstanceClass.modelingComplexity != null)
                return;



            var location = System.Windows.Forms.Control.MousePosition;

            ModelingComplexity tempModelingComplexity = new ModelingComplexity(MainPanel_AGOutputList[tempIndex]);
            TuringAndCorbusierPlugIn.InstanceClass.modelingComplexity = tempModelingComplexity;
            tempModelingComplexity.Left = location.X - tempModelingComplexity.Width/2;
            tempModelingComplexity.Top = location.Y - tempModelingComplexity.Height / 2;
            tempModelingComplexity.CaptureMouse();
            tempModelingComplexity.Show();
        }

        private void Btn_ExportElevation_Click(object sender, RoutedEventArgs e)
        {
            if (tempIndex == -1)
            {
                errorMessage tempError = new errorMessage("설계안을 먼저 선택하세요.");
                return;
            }

            //Elevation tempElevation = Elevation.drawElevation(MainPanel_AGOutputList[tempIndex].AptLines[0], MainPanel_AGOutputList[tempIndex].HouseholdProperties[0], MainPanel_AGOutputList[tempIndex].CoreProperties[0]);
            Section tempSection = Section.drawSection(MainPanel_AGOutputList[tempIndex].AptLines, MainPanel_AGOutputList[tempIndex].HouseholdProperties, MainPanel_AGOutputList[tempIndex].CoreProperties, MainPanel_AGOutputList[tempIndex].Plot);

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
            
            ///출력 할 ApartmentGeneratorOutput

            ApartmentGeneratorOutput tempAGOutput = MainPanel_AGOutputList[tempIndex];

            ///현재 대지에 대한 DesignMaster입력 << 중복될 시 입력 안함
            
            if(!CommonFunc.checkDesignMasterPresence(CurrentDataIdName.ToList(), CurrentDataId.ToList()))
            {
                List<Point3d> tempBoundaryPts = new List<Point3d>();

                foreach (Curve i in MainPanel_AGOutputList[tempIndex].Plot.Boundary.DuplicateSegments())
                    tempBoundaryPts.Add(i.PointAt(i.Domain.T0));

                CommonFunc.AddTdDesignMaster(CurrentDataIdName.ToList(), CurrentDataId.ToList(), USERID, (int)TuringAndCorbusierPlugIn.InstanceClass.page1Settings.MaxFloorAreaRatio, (int)TuringAndCorbusierPlugIn.InstanceClass.page1Settings.MaxBuildingCoverage, TuringAndCorbusierPlugIn.InstanceClass.page1Settings.MaxFloors, CommonFunc.GetPlotBoundaryVector(tempBoundaryPts), CommonFunc.ListToCSV(MainPanel_AGOutputList[tempIndex].Plot.Surroundings.ToList()), MainPanel_AGOutputList[tempIndex].Plot.GetArea());
            }
            

            ///가장 마지막 DESIGN_NO 다음 번호로 DesignDetail(설계 전체에 관한 내용) 입력
            
            int temp_REGI_PRE_DESIGN_NO;
            CommonFunc.AddDesignDetail(CurrentDataIdName.ToList(), CurrentDataId.ToList(), USERID, MainPanel_AGOutputList[tempIndex], out temp_REGI_PRE_DESIGN_NO);

            ///각 세대 타입별 정보 입력

            for (int i = 0; i < MainPanel_AGOutputList[tempIndex].HouseholdStatistics.Count; i++)
            {

                HouseholdStatistics tempHouseholdStatistics = MainPanel_AGOutputList[tempIndex].HouseholdStatistics[i];
                double tempExclusiveAreaSum = tempAGOutput.GetExclusiveAreaSum();
                double tempExclusiveArea = tempHouseholdStatistics.GetExclusiveArea();

                double GroundCoreAreaPerHouse = tempAGOutput.GetCoreAreaOnEarthSum() / (tempExclusiveAreaSum + tempAGOutput.GetCommercialArea()) * tempExclusiveArea;
                double coreAreaPerHouse = GroundCoreAreaPerHouse + (tempAGOutput.GetCoreAreaSum() - tempAGOutput.GetCoreAreaOnEarthSum()) / tempExclusiveAreaSum * tempExclusiveArea;
                double parkingLotAreaPerHouse = tempAGOutput.ParkingLotUnderGround.GetAreaSum() / (tempExclusiveAreaSum + tempAGOutput.GetCommercialArea()) * tempExclusiveArea;
                double plotAreaPerHouse = tempAGOutput.Plot.GetArea() / (tempExclusiveAreaSum + tempAGOutput.GetCommercialArea()) * tempExclusiveArea;
                double welfareAreaPerHouse = tempAGOutput.GetPublicFacilityArea() / tempExclusiveAreaSum * tempExclusiveArea;
                double facilitiesAreaPerHouse = 0 / tempExclusiveAreaSum * tempExclusiveArea;

                CommonFunc.AddTdDesignArea(CurrentDataIdName.ToList(), CurrentDataId.ToList(), temp_REGI_PRE_DESIGN_NO, USERID, tempAGOutput.AreaTypeString()[i], coreAreaPerHouse,welfareAreaPerHouse, facilitiesAreaPerHouse, parkingLotAreaPerHouse, plotAreaPerHouse, tempHouseholdStatistics);

            }

            CommonFunc.AddDesignModel(CurrentDataIdName.ToList(), CurrentDataId.ToList(), USERID, temp_REGI_PRE_DESIGN_NO, tempAGOutput.AGtype, CommonFunc.ListToCSV(tempAGOutput.ParameterSet.Parameters.ToList()), tempAGOutput.ParameterSet.CoreType.ToString(), tempAGOutput.Target.TargetArea, tempAGOutput.Target.TargetRatio);
            //CommonFunc.AddDesignReport(CurrentDataIdName.ToList(), CurrentDataId.ToList(),temp_REGI_PRE_DESIGN_NO);

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
            catch(Rhino.FileIO.BinaryArchiveException f)
            {
                MessageBox.Show(f + " " + ofd.FileName + " 파일이 존재하지 않습니다");
            }

        }


        private void backGround_Loaded(object sender, RoutedEventArgs e)
        {
            TuringAndCorbusierPlugIn.InstanceClass.turing = this;
        }

        private void mainWindow_Initialized(object sender, System.EventArgs e)
        {

          
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
