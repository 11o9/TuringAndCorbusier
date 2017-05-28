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
        public floorPlanDrawingPage(Interval floorInterval)
        {
            InitializeComponent();
            this.SetTitle(floorInterval);
        }
        public floorPlanDrawingPage(int number)
        {
            InitializeComponent();
            this.planPageTitle.Text = "1st Floor plan";
        }

        private void SetTitle(Interval floorInterval)
        {
            if (floorInterval.Min == floorInterval.Max)
            {
                if(floorInterval.Min == 1)
                {
                    this.planPageTitle.Text = (floorInterval.Min+1).ToString() + "nd Floor plan";
                }else if(floorInterval.Min == 2){
                    this.planPageTitle.Text = (floorInterval.Min+1).ToString() + "rd Floor plan";
                }else
                {
                    this.planPageTitle.Text = (floorInterval.Min+1).ToString() + "th Floor plan";
                }
            }
                
            //else
            //{
            //    this.planPageTitle.Text = "4." + floorInterval.Min.ToString() + "~" + floorInterval.Max.ToString() + "th Floor plan";
            //}
        }

        private void SethouseAreaTypeColor(xmlUnitReport unitReport)
        {
            string AreaType = unitReport.AreaType.Text;
        }

        //--------JHL
        public void SetHouseOutline(List<Curve> coreOutline, List<Curve> coreDetail, List<Curve> houseOutline, TypicalPlan typicalPlan, Interval floor)
        {

            Curve boundary = typicalPlan.Boundary;
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

                PlanDrawingFunction_90degree.drawPlan(rectangleToFit, surroundingSite, scaleFactor, initialOriginPoint, ref this.typicalPlanCanvas, System.Windows.Media.Brushes.LightGray, 0.2);
                PlanDrawingFunction_90degree.drawPlan(rectangleToFit, boundary, scaleFactor, initialOriginPoint, ref this.typicalPlanCanvas, System.Windows.Media.Brushes.Red, 2);
                PlanDrawingFunction_90degree.drawBackGround(rectangleToFit, boundary, scaleFactor, initialOriginPoint, ref this.typicalPlanCanvas, System.Windows.Media.Brushes.White);


                foreach (Curve house in houseOutline)
                {
                    PlanDrawingFunction_90degree.drawPlan(rectangleToFit, house, scaleFactor, initialOriginPoint, ref this.typicalPlanCanvas, System.Windows.Media.Brushes.Black, 0.5);
                    PlanDrawingFunction_90degree.drawBackGround(rectangleToFit, house, scaleFactor, initialOriginPoint, ref this.typicalPlanCanvas, System.Windows.Media.Brushes.LightSeaGreen);
                }

                foreach (Curve core in corePlanList)
                {
                    PlanDrawingFunction_90degree.drawPlan(rectangleToFit, core, scaleFactor, initialOriginPoint, ref this.typicalPlanCanvas, System.Windows.Media.Brushes.Black, 0.5);
                    PlanDrawingFunction_90degree.drawBackGround(rectangleToFit, core, scaleFactor, initialOriginPoint, ref this.typicalPlanCanvas, System.Windows.Media.Brushes.LightGray);
                }
                foreach (Curve detail in coreDetailList)
                {
                    PlanDrawingFunction_90degree.drawPlan(rectangleToFit, detail, scaleFactor, initialOriginPoint, ref this.typicalPlanCanvas, System.Windows.Media.Brushes.Black, 0.075);
                }
                foreach (FloorPlan floorPlan in floorPlanList)
                {
                    PlanDrawingFunction_90degree.drawPlan(rectangleToFit, floorPlan.balconyLines, scaleFactor, initialOriginPoint, ref this.typicalPlanCanvas, System.Windows.Media.Brushes.Black, 0.5);
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

                PlanDrawingFunction_90degree.drawPlan(rectangleToFit, surroundingSite, scaleFactor, initialOriginPoint, ref this.typicalPlanCanvas, System.Windows.Media.Brushes.LightGray, 0.2);
                PlanDrawingFunction_90degree.drawPlan(rectangleToFit, boundary, scaleFactor, initialOriginPoint, ref this.typicalPlanCanvas, System.Windows.Media.Brushes.Red, 2);
                PlanDrawingFunction_90degree.drawBackGround(rectangleToFit, boundary, scaleFactor, initialOriginPoint, ref this.typicalPlanCanvas, System.Windows.Media.Brushes.White);


            //draw the core
                foreach (Curve core in corePlanList)
                {
                    PlanDrawingFunction_90degree.drawPlan(rectangleToFit, core, scaleFactor, initialOriginPoint, ref this.typicalPlanCanvas, System.Windows.Media.Brushes.Black, 0.5);
                    PlanDrawingFunction_90degree.drawBackGround(rectangleToFit, core, scaleFactor, initialOriginPoint, ref this.typicalPlanCanvas, System.Windows.Media.Brushes.LightGray);
                }
                foreach (Curve detail in coreDetailList)
                {
                    PlanDrawingFunction_90degree.drawPlan(rectangleToFit, detail, scaleFactor, initialOriginPoint, ref this.typicalPlanCanvas, System.Windows.Media.Brushes.Black, 0.075);
                }
                PlanDrawingFunction_90degree.drawPlan(rectangleToFit, typicalPlan.OutLine.ToNurbsCurve(), scaleFactor, initialOriginPoint, ref this.typicalPlanCanvas, System.Windows.Media.Brushes.Black, 3);
            //draw houseoutline with dashlines
            foreach (Curve house in houseOutline)
            {
                PlanDrawingFunction_90degree.drawPlan(rectangleToFit, house, scaleFactor, initialOriginPoint, ref this.typicalPlanCanvas, System.Windows.Media.Brushes.LightGray, 0.025);
            }
            foreach(Curve parkingLine in parkinglineList)
            {
                PlanDrawingFunction_90degree.drawPlan(rectangleToFit, parkingLine, scaleFactor, initialOriginPoint, ref this.typicalPlanCanvas, System.Windows.Media.Brushes.Black, 0.075);
            }

            PlanDrawingFunction_90degree.drawPlan(rectangleToFit, typicalPlan.OutLine.ToNurbsCurve(), scaleFactor, initialOriginPoint, ref this.typicalPlanCanvas, System.Windows.Media.Brushes.Black, 3);
        }
    }
}