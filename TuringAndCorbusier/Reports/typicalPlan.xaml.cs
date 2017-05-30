using System;
using System.Linq;
using System.Windows.Controls;
using TuringAndCorbusier;
using Rhino.Geometry;
using System.Collections.Generic;
using System.Windows.Media;
using System.Windows.Shapes;
using Rhino.Display;

namespace Reports
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class floorPlanDrawingPage
    {
        public floorPlanDrawingPage(Interval floorInterval,bool isUsing1F)
        {
            InitializeComponent();
            if (isUsing1F == true)
            {
                this.SetTitle2(floorInterval);
            }else
            {
            this.SetTitle(floorInterval);

            }
        }
        public floorPlanDrawingPage(int number)
        {
            InitializeComponent();
            this.planPageTitle.Text = "PLAN 1F";
        }
        public floorPlanDrawingPage(int number,string lastFloor)
        {
            InitializeComponent();
            this.planPageTitle.Text = "PLAN "+number.ToString()+"F";
        }

        private void SetTitle(Interval floorInterval)
        {
            this.planPageTitle.Text = "PLAN " + (floorInterval.Min+1).ToString() + "-" + floorInterval.Max.ToString() + "F";

            //else
            //{
            //    this.planPageTitle.Text = "4." + floorInterval.Min.ToString() + "~" + floorInterval.Max.ToString() + "th Floor plan";
            //}
        }
        private void SetTitle2(Interval floorInterval)
        {
            this.planPageTitle.Text = "PLAN " + floorInterval.Min.ToString() + "-" + floorInterval.Max.ToString() + "F";

            //else
            //{
            //    this.planPageTitle.Text = "4." + floorInterval.Min.ToString() + "~" + floorInterval.Max.ToString() + "th Floor plan";
            //}
        }

        //private void SethouseAreaTypeColor(xmlUnitReport unitReport)
        //{
        //    string AreaType = unitReport.AreaType.Text;
        //}


        private void AddAreaTypeToHouseOutline(System.Windows.Point newCentroid, string areaTypeInText)
        {

            
            TextBlock areaType = new TextBlock();
            areaType.Text = areaTypeInText;
            areaType.FontSize = 20;
            typicalPlanCanvas.Children.Add(areaType);

            Canvas.SetLeft(areaType, newCentroid.X - (20));
            Canvas.SetTop(areaType, newCentroid.Y - (10));

        }

        private string ProcessExclusiveArea(double unRoundedExclusiveArea)
        {
            string roundedStringExclusiveArea = Math.Round(unRoundedExclusiveArea / 1000000, 0).ToString() + "m\xB2 ";
            return roundedStringExclusiveArea;
        }
        //타입마다 크기색상 지정
        //private System.Windows.Media.SolidColorBrush SetAreaTypeColor(List<double> distinctRoundedExclusiveArea, double actualRoundedExclusiveArea)
        //{
        //    System.Windows.Media.SolidColorBrush areaTypeColour = null;
        //    //set colors
        //    List<System.Windows.Media.SolidColorBrush> colorList = new List<System.Windows.Media.SolidColorBrush>();
        //    colorList.Add(System.Windows.Media.Brushes.Aquamarine);
        //    colorList.Add(System.Windows.Media.Brushes.Crimson);
        //    colorList.Add(System.Windows.Media.Brushes.LightGreen);
        //    colorList.Add(System.Windows.Media.Brushes.Snow);
        //    colorList.Add(System.Windows.Media.Brushes.Plum);
        //    colorList.Add(System.Windows.Media.Brushes.Gold);
        //    colorList.Add(System.Windows.Media.Brushes.Tomato);
        //    colorList.Add(System.Windows.Media.Brushes.Khaki);
        //    colorList.Add(System.Windows.Media.Brushes.Lavender);
        //    colorList.Add(System.Windows.Media.Brushes.LightSeaGreen);

        //        for (int j = 0; j < distinctRoundedExclusiveArea.Count; j++)
        //        {
        //            try
        //            {
        //            if(actualRoundedExclusiveArea == distinctRoundedExclusiveArea[j])
        //            {
        //                areaTypeColour = colorList[j];
        //            }
        //            }catch(Exception e){
        //            continue;
        //               // System.Windows.MessageBox.Show(e.Message);
        //            }
        //        }

        
        //    return areaTypeColour;
        //}


        //--------JHL
        public void SetHouseOutline(List<Curve> coreOutline, List<Curve> coreDetail, List<Curve> houseOutline, TypicalPlan typicalPlan,List<Household> householdList,Interval floor)
        {

            Curve boundary = typicalPlan.Boundary;
            List<Curve> surroundingSite = typicalPlan.SurroundingSite;
            List<FloorPlan> housePlanList = typicalPlan.UnitPlans;
            List<Curve> corePlanList = coreOutline;
            List<FloorPlan> floorPlanList = typicalPlan.UnitPlans;
            List<Curve> coreDetailList = coreDetail;
            List<Curve> houseOutlineList = houseOutline;
            List<Point3d> houseOutlinesCentroid = new List<Point3d>();
            Rectangle3d rectangleToFit = new Rectangle3d(Plane.WorldXY, typicalPlan.GetBoundingBox().Min, typicalPlan.GetBoundingBox().Max);
            Rectangle canvasRectangle = new Rectangle();
            canvasRectangle.Width = typicalPlanCanvas.Width;
            canvasRectangle.Height = typicalPlanCanvas.Height;
            System.Windows.Point initialOriginPoint = new System.Windows.Point();
            double scaleFactor = PlanDrawingFunction_90degree.scaleToFitFactor(canvasRectangle, rectangleToFit, out initialOriginPoint);


            PlanDrawingFunction_90degree.drawPlan(rectangleToFit, surroundingSite, scaleFactor, initialOriginPoint, ref this.typicalPlanCanvas, System.Windows.Media.Brushes.Black, 1);
            PlanDrawingFunction_90degree.drawBoundaryPlan(rectangleToFit, boundary, scaleFactor, initialOriginPoint, ref this.typicalPlanCanvas, System.Windows.Media.Brushes.Red, 5);

            //세대 타입 구하기
            List<string> roundedStringExclusiveAreaList = new List<string>();
            //List<double> distinctRoundedExclusiveArea = new List<double>();
           foreach (Household household in householdList)
            {
                roundedStringExclusiveAreaList.Add(ProcessExclusiveArea(household.GetExclusiveArea()));
            //    distinctRoundedExclusiveArea.Add(Math.Round(household.GetExclusiveArea() / 1000000, 0));
            }
            //List<double> testing = distinctRoundedExclusiveArea.Distinct().ToList();

            try
            {

            for(int i = 0; i < houseOutlineList.Count; i++)
                {
                    //double actualRoundExclusiveArea = Math.Round(householdList[i].GetExclusiveArea() / 1000000, 0);
                    //System.Windows.Media.SolidColorBrush areaTypeBackgroundColour = SetAreaTypeColor(distinctRoundedExclusiveArea, actualRoundExclusiveArea);
                    PlanDrawingFunction_90degree.drawPlan(rectangleToFit, houseOutlineList[i], scaleFactor, initialOriginPoint, ref this.typicalPlanCanvas, System.Windows.Media.Brushes.Black, 2);
                    //PlanDrawingFunction_90degree.drawBackGround(rectangleToFit, houseOutlineList[i], scaleFactor, initialOriginPoint, ref this.typicalPlanCanvas, areaTypeBackgroundColour);
                    PlanDrawingFunction_90degree.drawBackGround(rectangleToFit, houseOutlineList[i], scaleFactor, initialOriginPoint, ref this.typicalPlanCanvas, System.Windows.Media.Brushes.LightGreen);
                }

            foreach (Curve core in corePlanList)
            {
                PlanDrawingFunction_90degree.drawPlan(rectangleToFit, core, scaleFactor, initialOriginPoint, ref this.typicalPlanCanvas, System.Windows.Media.Brushes.Black, 2);
                PlanDrawingFunction_90degree.drawBackGround(rectangleToFit, core, scaleFactor, initialOriginPoint, ref this.typicalPlanCanvas, System.Windows.Media.Brushes.LightGray);
            }
            foreach (Curve detail in coreDetailList)
            {
                PlanDrawingFunction_90degree.drawPlan(rectangleToFit, detail, scaleFactor, initialOriginPoint, ref this.typicalPlanCanvas, System.Windows.Media.Brushes.Black, 0.075);
            }
            foreach (FloorPlan floorPlan in floorPlanList)
            {
                PlanDrawingFunction_90degree.drawPlan(rectangleToFit, floorPlan.balconyLines, scaleFactor, initialOriginPoint, ref this.typicalPlanCanvas, System.Windows.Media.Brushes.Black, 0.075);
            }

            }catch(Exception e)
            {
                System.Windows.MessageBox.Show(e.Message);
            }



            //JHL 글씨 넣기 위해 중심 점 구함 
            foreach (Curve house in houseOutlineList)
            {
                houseOutlinesCentroid.Add(Rhino.Geometry.AreaMassProperties.Compute(house).Centroid);
            }
            for (int i = 0; i < houseOutlinesCentroid.Count; i++)
            {
                System.Windows.Point newCentroid = PlanDrawingFunction_90degree.pointConverter(rectangleToFit, houseOutlinesCentroid[i], scaleFactor, initialOriginPoint);
                try
                {
                this.AddAreaTypeToHouseOutline(newCentroid, roundedStringExclusiveAreaList[i]);

                }catch(Exception e)
                {
                    continue;
                }
            }



            PlanDrawingFunction_90degree.drawPlan(rectangleToFit, typicalPlan.OutLine.ToNurbsCurve(), scaleFactor, initialOriginPoint, ref this.typicalPlanCanvas, System.Windows.Media.Brushes.Black, 3);
        }

        public void SetCoreOutline(List<Curve> coreOutline, List<Curve> coreDetail, List<Curve> houseOutline, TypicalPlan typicalPlan, Interval floor)
        {

            Curve boundary = typicalPlan.Boundary;
            List<Curve> parkinglineList = typicalPlan.ParkingLines;
            List<Curve> surroundingSite = typicalPlan.SurroundingSite;
            List<FloorPlan> housePlanList = typicalPlan.UnitPlans;
            List<Curve> corePlanList = coreOutline;
            List<FloorPlan> floorPlanList = typicalPlan.UnitPlans;
            List<Curve> coreDetailList = coreDetail;
            List<Curve> houseOutlineList = houseOutline;
            Rectangle3d rectangleToFit = new Rectangle3d(Plane.WorldXY, typicalPlan.GetBoundingBox().Min, typicalPlan.GetBoundingBox().Max);

            Rectangle canvasRectangle = new Rectangle();
            canvasRectangle.Width = typicalPlanCanvas.Width;
            canvasRectangle.Height = typicalPlanCanvas.Height;
            System.Windows.Point initialOriginPoint = new System.Windows.Point();
            double scaleFactor = PlanDrawingFunction_90degree.scaleToFitFactor(canvasRectangle, rectangleToFit, out initialOriginPoint);

            PlanDrawingFunction_90degree.drawPlan(rectangleToFit, surroundingSite, scaleFactor, initialOriginPoint, ref this.typicalPlanCanvas, System.Windows.Media.Brushes.Black, 1);
            PlanDrawingFunction_90degree.drawBoundaryPlan(rectangleToFit, boundary, scaleFactor, initialOriginPoint, ref this.typicalPlanCanvas, System.Windows.Media.Brushes.Red, 5);
           


            //draw the core
            //foreach (Curve detail in coreDetailList)
            //{
            //    PlanDrawingFunction_90degree.drawPlan(rectangleToFit, detail, scaleFactor, initialOriginPoint, ref this.typicalPlanCanvas, System.Windows.Media.Brushes.Black, 0.075);
            //}
            foreach (Curve core in corePlanList)
            {
                PlanDrawingFunction_90degree.drawPlan(rectangleToFit, core, scaleFactor, initialOriginPoint, ref this.typicalPlanCanvas, System.Windows.Media.Brushes.Black, 2);
                PlanDrawingFunction_90degree.drawBackGround(rectangleToFit, core, scaleFactor, initialOriginPoint, ref this.typicalPlanCanvas, System.Windows.Media.Brushes.LightGray);
            }
            PlanDrawingFunction_90degree.drawPlan(rectangleToFit, typicalPlan.OutLine.ToNurbsCurve(), scaleFactor, initialOriginPoint, ref this.typicalPlanCanvas, System.Windows.Media.Brushes.Black, 1);
            //draw houseoutline with dashlines
            foreach (Curve house in houseOutline)
            {
                PlanDrawingFunction_90degree.drawDashedPlan(rectangleToFit, house, scaleFactor, initialOriginPoint, ref this.typicalPlanCanvas, System.Windows.Media.Brushes.Black, 0.075);
            }
            foreach (Curve parkingLine in parkinglineList)
            {
                PlanDrawingFunction_90degree.drawPlan(rectangleToFit, parkingLine, scaleFactor, initialOriginPoint, ref this.typicalPlanCanvas, System.Windows.Media.Brushes.Black, 0.5);
            }

            PlanDrawingFunction_90degree.drawPlan(rectangleToFit, typicalPlan.OutLine.ToNurbsCurve(), scaleFactor, initialOriginPoint, ref this.typicalPlanCanvas, System.Windows.Media.Brushes.Black, 1);
        }
    }
}