using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Rhino.Geometry;
using Rhino;
using System.Windows.Media;
using System;
using System.Linq;

namespace TuringAndCorbusier
{
    /// <summary>
    /// Page3.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class Page3 : Page
    {

        private List<Guid> sectionpreview = new List<Guid>();

        private CurveConduit building2DPreview = new CurveConduit();
        //private BrepConduit building3DPreview = new BrepConduit();
        private CurveConduit parkingLotPreview = new CurveConduit(System.Drawing.Color.DimGray);
        private textConduit textPreview = new textConduit();
        private Brush previousClickedButtonBrush = Brushes.White;
        private int previousClickedButtonIndex = -1;

        public double currentWorkQuantity = 0;
        public double currentProgressFactor = 0;

        public void DisableConduit()
        {
            building2DPreview.Enabled = false;
            parkingLotPreview.Enabled = false;
            //building3DPreview.Enabled = false;
            RhinoDoc.ActiveDoc.Views.Redraw();
        }

        public Page3()
        {
            InitializeComponent();
        }

        public List<Apartment> tempOutput = new List<Apartment>();
        public List<string> tempAGName = new List<string>();

        private bool checkHasSimilarPattern(Apartment agOutput, out int index)
        {
            int tempIndex = tempOutput.Count;
            index = tempIndex;
            //try
            //{

            //    for (int i = 0; i < tempOutput.Count(); i++)
            //    {
            //        if (tempOutput[i].AGtype == agOutput.AGtype)
            //        {
            //            if ((tempOutput[i].ParkingLotUnderGround.GetCount() == 0) != (agOutput.ParkingLotUnderGround.GetCount() == 0))
            //            {
            //                continue;
            //            }

            //            if((tempOutput[i].Commercial.Count == 0) != (agOutput.Commercial.Count == 0))
            //            {
            //                continue;
            //            }

            //            tempIndex = i;

            //            double GrossAreaFactor = (agOutput.GetGrossAreaRatio() - tempOutput[i].GetGrossAreaRatio()) / tempOutput[i].GetGrossAreaRatio();
            //            double BuildingCoverageFactor = Math.Abs(agOutput.GetBuildingCoverage() - tempOutput[i].GetBuildingCoverage()) / tempOutput[i].GetBuildingCoverage();
            //            double HouseholdNumsFactor = Math.Abs(agOutput.GetHouseholdCount() - tempOutput[i].GetHouseholdCount()) / tempOutput[i].GetHouseholdCount();

            //            if (GrossAreaFactor <= 0.05 && BuildingCoverageFactor <= 0.1 && HouseholdNumsFactor <= 0.1)
            //            {
            //                index = i;
            //                return true;
            //            }
            //        }
            //    }

            //    index = -1;
            //    return false;
            //}
            //catch (Exception)
            //{
            //    index = tempIndex;
            //    return true;
            //}
            bool result = false;
            return result;
        }

        private bool AddButtonMethod(Apartment agOutput)
        {
            if (agOutput.AptLines.Count == 0)
                return false;
            double grossAreaRatio = agOutput.GetGrossAreaRatio();
            double buildingCoverage = agOutput.GetBuildingCoverage();
            int buildingNums = agOutput.Household.Count;
            int householdNums = agOutput.GetHouseholdCount();
            int parkingLot = (int)(agOutput.ParkingLotOnEarth.GetCount() + agOutput.ParkingLotUnderGround.Count);
            int neededParkingLot = (int)System.Math.Round((double)agOutput.GetLegalParkingLotofHousing() + agOutput.GetLegalParkingLotOfCommercial());

            Grid tempGrid = new Grid();
            tempGrid.HorizontalAlignment = HorizontalAlignment.Stretch;
            tempGrid.Width = 560;

            ColumnDefinition firstColDef = new ColumnDefinition();
            firstColDef.Width = new GridLength(2, GridUnitType.Star);
            tempGrid.ColumnDefinitions.Add(firstColDef);

            ColumnDefinition secondColDef = new ColumnDefinition();
            secondColDef.Width = new GridLength(0, GridUnitType.Pixel);
            tempGrid.ColumnDefinitions.Add(secondColDef);

            for (int i = 0; i < 7; i++)
            {
                ColumnDefinition tempColDef = new ColumnDefinition();
                tempColDef.Width = new GridLength(1, GridUnitType.Star);
                tempGrid.ColumnDefinitions.Add(tempColDef);
            }

            ColumnDefinition lastColDef = new ColumnDefinition();
            lastColDef.Width = new GridLength(17, GridUnitType.Pixel);
            tempGrid.ColumnDefinitions.Add(lastColDef);


            TextBlock algorithmTextBlock = createTextBlock(CommonFunc.GetApartmentType(agOutput));
            tempGrid.Children.Add(algorithmTextBlock);
            Grid.SetColumn(algorithmTextBlock, 0);

            TextBlock grossAreaRatioTextBlock = createTextBlock(((int)grossAreaRatio).ToString() + "%");
            tempGrid.Children.Add(grossAreaRatioTextBlock);
            Grid.SetColumn(grossAreaRatioTextBlock, 2);

            TextBlock buildingCoverageTextBlock = createTextBlock(((int)buildingCoverage).ToString() + "%");
            tempGrid.Children.Add(buildingCoverageTextBlock);
            Grid.SetColumn(buildingCoverageTextBlock, 3);


            //parameter 를 그대로 층수로 사용 했었으나, 지역 최적화에서 층수를 낮추는 과정에서 실제 층수와 맞지 않을수 있어 수정
            //TextBlock storiesTextBlock = createTextBlock(((int)agOutput.ParameterSet.Stories + 1).ToString() + "층");
            TextBlock storiesTextBlock = createTextBlock(((int)agOutput.Household.Count+1).ToString() + "층");
            tempGrid.Children.Add(storiesTextBlock);
            Grid.SetColumn(storiesTextBlock, 4);

            TextBlock householdNumsTextBlock = createTextBlock(householdNums.ToString() + "세대");
            tempGrid.Children.Add(householdNumsTextBlock);
            Grid.SetColumn(householdNumsTextBlock, 5);

            TextBlock commerCialTextBlock = createTextBlock(Math.Round(agOutput.GetCommercialArea() / agOutput.GetGrossArea() * 100, 1).ToString() + "%");
            tempGrid.Children.Add(commerCialTextBlock);
            Grid.SetColumn(commerCialTextBlock, 6);

            TextBlock parkingLotTextBlock = createTextBlock(parkingLot.ToString() + "/" + neededParkingLot);
            tempGrid.Children.Add(parkingLotTextBlock);
            Grid.SetColumn(parkingLotTextBlock, 7);

            TextBlock parkingLotUnderGroundTextBlock = createTextBlock(agOutput.ParkingLotUnderGround.Count.ToString());
            tempGrid.Children.Add(parkingLotUnderGroundTextBlock);
            Grid.SetColumn(parkingLotUnderGroundTextBlock, 8);

            Button btn = new Button();

            string[] stackPannelButtonStyle = { "stackPannelButtonStyle1", "stackPannelButtonStyle2" };
            string tempStyle = stackPannelButtonStyle[stackPanel.Children.Count % 2];
            System.Windows.Style style = this.FindResource(tempStyle) as System.Windows.Style;

            btn.Style = style;
            btn.Content = tempGrid;
            btn.Height = 21;
            btn.Click += StackButton_Click;
            btn.MouseDoubleClick += StackButton_DoubleClick;

            stackPanel.Children.Add(btn);

            return true;
        }

        public bool AddButtonToStackPanel(Apartment agOutput)
        {
            int similarIndex;
            if (checkHasSimilarPattern(agOutput, out similarIndex))
            {
                if (tempOutput[similarIndex].GetGrossAreaRatio() < agOutput.GetGrossAreaRatio())
                {
                    return AddButtonMethod(agOutput);
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return AddButtonMethod(agOutput);
            }
        }

        private void StackButton_Click(object sender, RoutedEventArgs e)
        {
            if(previousClickedButtonIndex != -1)
                (stackPanel.Children[previousClickedButtonIndex] as Button).Background = previousClickedButtonBrush;

            this.previousClickedButtonIndex = stackPanel.Children.IndexOf(sender as Button);
            this.previousClickedButtonBrush = (sender as Button).Background;

            (sender as Button).Background = new SolidColorBrush(Color.FromArgb(255, 255, 204, 0));
            this.preview(tempOutput[stackPanel.Children.IndexOf(sender as Button)]);

            UpdateSummary(tempOutput[stackPanel.Children.IndexOf(sender as Button)]);

            RhinoDoc.ActiveDoc.Views.Redraw();
            RhinoApp.Wait();

            

            /*

            List<Brep> tempBreps = DrawBuildingOutLine.simpleBuildings(tempOutput[stackPanel.Children.IndexOf(sender as Button)]);

            building3DPreview.BrepToDisplay = tempBreps;
            building3DPreview.Enabled = true;

            RhinoDoc.ActiveDoc.Views.Redraw();*/

        }

        private void StackButton_DoubleClick(object sender, RoutedEventArgs e)
        {
            int tempIndex = stackPanel.Children.IndexOf(sender as Button);
            ((Rhino.UI.Panels.GetPanel(TuringHost.PanelId) as RhinoWindows.Controls.WpfElementHost).Child as Turing).AddButton(tempAGName[tempIndex], tempOutput[tempIndex]);
            DisableConduit();
        }

        private string GetConvertedAGName(ApartmentGeneratorBase AG)
        {
            return "AG1";
        }

        private TextBlock createTextBlock(string text)
        {
            TextBlock tempTextBlock = new TextBlock();
            tempTextBlock.FontFamily = new FontFamily("Noto Sans CJK KR medium");
            tempTextBlock.FontSize = 10;
            tempTextBlock.TextAlignment = TextAlignment.Center;
            tempTextBlock.Padding = new Thickness(1);

            tempTextBlock.Text = text;

            return tempTextBlock;
        }

        public void UpdateSummary(Apartment agOutput)
        {
            BuildingType.Text = "공동주택(" + agOutput.BuildingType.ToString() + ")";

            BuildingScale.Text = "지상 1층 ~ " + (agOutput.ParameterSet.Stories + 1).ToString() + "층";

            BuildingArea.Text = Math.Round(agOutput.GetBuildingArea() / 1000000, 2).ToString() + " m\xB2";
            BuildingArea_Py.Text = "(" + Math.Round(agOutput.GetBuildingArea() / 1000000 / 3.3, 2).ToString() + " 평)";
            BuildingCoverage.Text = ((int)agOutput.GetBuildingCoverage()).ToString() + " %";
            LegalBuildingCoverage.Text = "(법정 : " + TuringAndCorbusierPlugIn.InstanceClass.page1Settings.MaxBuildingCoverage.ToString() + " %)";

            GrossArea.Text = Math.Round(agOutput.GetGrossArea() / 1000000, 2).ToString() + " m\xB2";
            GrossArea_Py.Text = "(" + Math.Round(agOutput.GetGrossArea() / 1000000 / 3.3, 2).ToString() + " 평)";
            FloorAreaRatio.Text = ((int)agOutput.GetGrossAreaRatio()).ToString() + " %";
        
            int numberOfHousing = 0;

            foreach (List<List<Household>> i in agOutput.Household)
            {
                foreach (List<Household> j in i)
                {
                    numberOfHousing += j.Count();
                }
            }
            NumOfHousing.Text = numberOfHousing.ToString() + "세대";

            NumOfParking.Text = (agOutput.ParkingLotOnEarth.GetCount() + agOutput.ParkingLotUnderGround.Count).ToString() + "대";


            List<double> firstFloorHHArea = new List<double>();
            for (int i = 0; i < agOutput.Household[0].Count; i++)
            {
                for (int j = 0; j < agOutput.Household[0][i].Count; j++)
                {
                    double round = Math.Round(agOutput.Household[0][i][j].ExclusiveArea / 1000000);
                    if (!firstFloorHHArea.Contains(round))
                        firstFloorHHArea.Add(round);
                }
            }
            var ta = firstFloorHHArea.OrderBy(n=>n).ToList();
            int[] type = new int[ta.Count];
            type = type.Select(n => 0).ToArray();
            foreach (var hh in agOutput.Household)
            {
                foreach (var h in hh)
                {
                    foreach (var x in h)
                    {
                        for (int i = 0; i < ta.Count; i++)
                        {
                            double round = Math.Round(x.ExclusiveArea / 1000000);
                            if (round == ta[i])
                            {
                                type[i]++;
                                break;
                            }
                        }
                    }
                }

            }

            TextBlock[] title = new TextBlock[] { most1,    most2,    most3,    most4,    most5    };
            TextBlock[] count = new TextBlock[] { nummost1, nummost2, nummost3, nummost4, nummost5 };


            for (int i = 0; i < title.Length; i++)
            {
                if (i < ta.Count)
                {
                    title[i].Text = ta[i].ToString() + " m2 형";
                    count[i].Text = type[i].ToString();
                }

                else
                {
                    title[i].Text = "";
                    count[i].Text = "";
                }
            }

          


        }

        private void Btn_Calculate_Click(object sender, RoutedEventArgs e)
        {
            currentProgressFactor = 0;

            //building3DPreview.BrepToDisplay = new List<Brep>();
            Rhino.RhinoDoc.ActiveDoc.Views.Redraw();
            RhinoApp.Wait();

            GC.Collect();

            List<ApartmentGeneratorBase> agSet = new List<ApartmentGeneratorBase>();

            agSet.Add(new AG1());
            agSet.Add(new AG3());
            agSet.Add(new AG4());

            if (TuringAndCorbusierPlugIn.InstanceClass.plot == null)
            {
                return;
            }

            Plot tempPlot = TuringAndCorbusierPlugIn.InstanceClass.plot;
            Target tempTarget = TuringAndCorbusierPlugIn.InstanceClass.page2Settings.Target;

            List<ApartmentGeneratorBase> usingAGSet = new List<ApartmentGeneratorBase>();

            for (int i = 0; i < agSet.Count(); i++)
            {
                if (TuringAndCorbusierPlugIn.InstanceClass.page2Settings.WhichAGToUse[i])
                {
                    if (i > 0 && !TuringAndCorbusierPlugIn.InstanceClass.page2.isAG34Valid)
                        return;
                    usingAGSet.Add(agSet[i]);
                }
            }

            if (usingAGSet.Count() == 0)
                usingAGSet.Add(new AG1());

            this.calcWorkQuantity(usingAGSet);


            if (TuringAndCorbusierPlugIn.InstanceClass.page2Settings.WhichAGToUse.IndexOf(true) == -1)
            {
                try
                {
                    AG1 tempAG = new AG1();

                    List<Apartment> tempTempOutputs = GiantAnteater.giantAnteater(tempPlot, tempAG, tempTarget, !this.Preview_Toggle.IsChecked.Value);

                    string tempAGname = GetConvertedAGName(tempAG);

                    for (int i = 0; i < tempTempOutputs.Count; i++)
                    {
                        bool addResult = this.AddButtonToStackPanel(tempTempOutputs[i]);
                        if (addResult)
                        {
                            this.tempOutput.Add(tempTempOutputs[i]);
                            this.tempAGName.Add(tempAGname);
                        }
                    }
            }
                catch (Exception ex)
            {
                RhinoApp.WriteLine(ex.Message);
            }
        }


            for (int i = 0; i < agSet.Count(); i++)
            {
                if (TuringAndCorbusierPlugIn.InstanceClass.page2Settings.WhichAGToUse[i])
                {


                    //try
                    //{
                        List<Apartment> tempTempOutputs = GiantAnteater.giantAnteater(tempPlot, agSet[i], tempTarget, !this.Preview_Toggle.IsChecked.Value);



                        string tempAGname = GetConvertedAGName(agSet[i]);

                        if (tempTempOutputs == null)
                            continue;

                        for (int k = 0; k < tempTempOutputs.Count; k++)
                        {

                            bool addResult = this.AddButtonToStackPanel(tempTempOutputs[k]);
                            if (addResult)
                            {
                                this.tempOutput.Add(tempTempOutputs[k]);
                                this.tempAGName.Add(tempAGname);
                            }

                        }


                        Rhino.RhinoApp.Wait();
                        GC.Collect();
                    //}
                    //catch (Exception ex)
                    //{
                    //    RhinoApp.WriteLine(ex.Message);
                    //}
                }
            }
        }

        private void preview_Toggle_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as System.Windows.Controls.Primitives.ToggleButton).IsChecked.Value)
            {
                (sender as System.Windows.Controls.Primitives.ToggleButton).IsChecked = true;
                return;
            }
            else
            {
                (sender as System.Windows.Controls.Primitives.ToggleButton).IsChecked = false;
                return;
            }
        }

        private void Btn_Next_Click(object sender, RoutedEventArgs e)
        {
            currentProgressFactor = 0;
            previousClickedButtonIndex = -1;
            DisableConduit();
            this.tempOutput.Clear();
            this.stackPanel.Children.Clear();

            GC.Collect();



            NavigationService.Navigate(TuringAndCorbusierPlugIn.InstanceClass.page2);
        }

        public void preview(Apartment outputToPreview)
        {
            /*
            List<Rhino.Display.Text3d> textToShow = new List<Rhino.Display.Text3d>();

            foreach(List<List<Household>> i in outputToPreview.Household)
            {
                foreach(List<Household> j in i)
                {
                    foreach(Household k in j)
                    {
                        Rhino.Display.Text3d tempText = new Rhino.Display.Text3d(Math.Round(k.ExclusiveArea / 1000000, 0).ToString(), new Plane(k.Origin, Vector3d.ZAxis), 2000);

                        textToShow.Add(tempText);
                    }
                }
            }

            
            Point3d textDrawPoint = outputToPreview.Plot.Boundary.GetBoundingBox(false).Max;

            List<string> texts = new List<string>();

            texts.Add("층수 : " + Math.Max(outputToPreview.ParameterSet.Parameters[0], outputToPreview.ParameterSet.Parameters[1]).ToString() + "층");
            texts.Add("세대수 : " + outputToPreview.GetHouseholdCount().ToString() + "세대");
            texts.Add("건축면적 : " + Math.Round(outputToPreview.GetBuildingArea()/1000000, 2).ToString() + "제곱미터");
            texts.Add("건폐율 : " + outputToPreview.GetBuildingCoverage().ToString() + "%");
            texts.Add("연면적 : " + Math.Round(outputToPreview.GetGrossArea()/1000000,2).ToString() + "제곱미터");
            texts.Add("용적률 : " + outputToPreview.GetGrossAreaRatio().ToString() + "%");
            texts.Add("주차대수 : " + outputToPreview.GetParkingLotNums().ToString() + "대");

            foreach(string i in texts)
            {
                Rhino.Display.Text3d tempText3d = new Rhino.Display.Text3d(i, new Plane(textDrawPoint, Vector3d.ZAxis), 4000);
                textDrawPoint.Transform(Rhino.Geometry.Transform.Translation(Vector3d.YAxis * -6000));

                textToShow.Add(tempText3d);
            }

            this.textPreview.TextToDisplay = textToShow;
            */


            //List<Curve> aptCurves = outputToPreview.drawEachCore();
            //aptCurves.AddRange(outputToPreview.drawEachHouse());
            //aptCurves.AddRange(outputToPreview.drawCommercialArea());
            //aptCurves.AddRange(outputToPreview.drawPublicFacilities());

            List<Curve> aptCurves = outputToPreview.drawEachHouse();
            aptCurves.AddRange(outputToPreview.drawEachCore());
            aptCurves.AddRange(outputToPreview.AptLines);

            //aptCurves.AddRange(outputToPreview.topReg);
            List<Curve> lotCurves = new List<Curve>();

            foreach (List<ParkingLine> i in outputToPreview.ParkingLotOnEarth.ParkingLines)
            {
                foreach (ParkingLine j in i)
                {
                    lotCurves.Add(j.Boundary.ToNurbsCurve());
                }
            }

            this.building2DPreview.CurveToDisplay = aptCurves;
            this.parkingLotPreview.CurveToDisplay = lotCurves;

            List<Point3d> debugs = new List<Point3d>();
            List<Point3d> origins = new List<Point3d>();
            if (outputToPreview.Household.Count != 0)
            { 
                foreach (var hh in outputToPreview.Household.Last())
                {
                    foreach (var h in hh)
                    {
                        debugs.AddRange(h.DebugPoints);
                        origins.Add(h.Origin);
                    }
                }
                building2DPreview.debugPoints = debugs;
                building2DPreview.HouseholdOrigins = origins;
            }
            /*
            textPreview.Enabled = true;
            */

            building2DPreview.Enabled = true;
            parkingLotPreview.Enabled = true;
        }

        public void updateProGressBar(string currentConditionString)
        {
            double workQuantity = this.currentWorkQuantity;
            double progressBarFactor = this.currentProgressFactor;

            double percentage = progressBarFactor / workQuantity * 100;

            CurrentPercentage.Text = Math.Round(percentage, 0).ToString() + "%";
            CurrentProgressBar.Width = ProgressBarBase.Width * percentage / 100;

            CurrentCondition.Text = currentConditionString;
        }

        public void calcWorkQuantity(List<ApartmentGeneratorBase> agList)
        {
            double output = 0;

            foreach (ApartmentGeneratorBase i in agList)
            {
                //population * initialboost * generation
                output += i.GAParameterSet[4] * i.GAParameterSet[2] * i.GAParameterSet[3];
            }

            this.currentWorkQuantity = output;
        }

        private void Button_Close_Click(object sender, RoutedEventArgs e)
        {
            currentProgressFactor = 0;
            TuringAndCorbusierPlugIn.InstanceClass.page3.DisableConduit();
            UIManager.getInstance().HideWindow(TuringAndCorbusierPlugIn.InstanceClass.navigationHost, UIManager.WindowType.Navi);
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            if (previousClickedButtonIndex == -1)
                return;

            if (sectionpreview != null)
            { 
                foreach (var g in sectionpreview)
                {
                    RhinoDoc.ActiveDoc.Objects.Delete(g, true);
                }
            }



            //법규선체크
            //var tttt = new RegulationChecker(tempOutput[previousClickedButtonIndex]);
            //this.sectionpreview.Add(LoadManager.getInstance().DrawObjectWithSpecificLayer(tttt.ByLightingHigh, LoadManager.NamedLayer.ETC));
            //this.sectionpreview.Add(LoadManager.getInstance().DrawObjectWithSpecificLayer(tttt.FromNorthHigh, LoadManager.NamedLayer.Guide));
            //this.sectionpreview.Add(LoadManager.getInstance().DrawObjectWithSpecificLayer(tttt.FromSurroundings, LoadManager.NamedLayer.Model));

            DrawSection drawsection = new DrawSection(tempOutput[previousClickedButtonIndex]);
            
            RhinoDoc.ActiveDoc.Views.ActiveView.Redraw();

        }
        
        private void Btn_CheckReg_Click(object sender, RoutedEventArgs e)
        {
           
        }
    }
}