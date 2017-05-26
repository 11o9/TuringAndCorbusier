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

        private TypicalPlan typicalPlanValue;
        private double scaleFactor = 0;
        private System.Windows.Point origin = new System.Windows.Point();

        private void SetTitle(Interval floorInterval)
        {
            if (floorInterval.Min == floorInterval.Max)
            {
                if(floorInterval.Min == 1)
                {
                    this.planPageTitle.Text = floorInterval.Min.ToString() + "st Floor plan";
                }else if(floorInterval.Min == 2){
                    this.planPageTitle.Text = floorInterval.Min.ToString() + "nd Floor plan";
                }else if(floorInterval.Min == 3)
                {
                    this.planPageTitle.Text = floorInterval.Min.ToString() + "rd Floor plan";
                }else
                {
                    this.planPageTitle.Text = floorInterval.Min.ToString() + "th Floor plan";
                }
            }
                
            else
            {
                this.planPageTitle.Text = "4." + floorInterval.Min.ToString() + "~" + floorInterval.Max.ToString() + "th Floor plan";
            }
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

            if(floor.Min == 1)
            {
                System.Windows.Point initialOriginPoint = new System.Windows.Point();
                double scaleFactor = PlanDrawingFunction_90degree.scaleToFitFactor(canvasRectangle, rectangleToFit, out initialOriginPoint);

                PlanDrawingFunction_90degree.drawPlan(rectangleToFit, surroundingSite, scaleFactor, initialOriginPoint, ref this.typicalPlanCanvas, System.Windows.Media.Brushes.LightGray, 0.2);
                PlanDrawingFunction_90degree.drawPlan(rectangleToFit, boundary, scaleFactor, initialOriginPoint, ref this.typicalPlanCanvas, System.Windows.Media.Brushes.Red, 2);
                PlanDrawingFunction_90degree.drawBackGround(rectangleToFit, boundary, scaleFactor, initialOriginPoint, ref this.typicalPlanCanvas, System.Windows.Media.Brushes.White);

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

            }
            else
            {
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
        }
        
        //public TypicalPlan SetTypicalPlan
        //{
        //    set
        //    {
        //        typicalPlanValue = value;

        //        TypicalPlan tempPlan = value;

        //        Rectangle3d tempBoundingBox = new Rectangle3d(Plane.WorldXY, tempPlan.GetBoundingBox().Min, tempPlan.GetBoundingBox().Max);
        //        Rectangle canvasRectangle = new Rectangle();
        //        canvasRectangle.Width = typicalPlan.Width;
        //        canvasRectangle.Height = typicalPlan.Height;

        //        System.Windows.Point tempOrigin = new System.Windows.Point();
        //        double tempScaleFactor = PlanDrawingFunction_90degree.scaleToFitFactor(canvasRectangle, tempBoundingBox, out tempOrigin);
        //        scaleFactor = tempScaleFactor;
        //        origin = tempOrigin;

        //        //doc.Objects.AddCurve(Boundary);
        //        //주변 대지 그리기
        //        PlanDrawingFunction_90degree.drawPlan(tempBoundingBox, tempPlan.SurroundingSite, tempScaleFactor, tempOrigin, ref this.typicalPlan, System.Windows.Media.Brushes.Black, 0.2);
        //        //대지 내부 배경 색상
        //        PlanDrawingFunction_90degree.drawBackGround(tempBoundingBox, tempPlan.Boundary, tempScaleFactor, tempOrigin, ref this.typicalPlan, new System.Windows.Media.SolidColorBrush(Color.FromRgb(255 , 255, 255)));
        //        //대지선 빨간색으로 그리기
        //        PlanDrawingFunction_90degree.drawPlan(tempBoundingBox, tempPlan.Boundary, tempScaleFactor, tempOrigin, ref this.typicalPlan, new System.Windows.Media.SolidColorBrush(Color.FromRgb(102, 204, 0)), 5);

        //        foreach (Text3d i in tempPlan.RoadWidth)
        //        {
        //            PlanDrawingFunction_90degree.drawText(tempBoundingBox, i, tempScaleFactor, tempOrigin, ref this.typicalPlan, 10, System.Windows.Media.Brushes.HotPink);
        //        }

        //        foreach (FloorPlan i in tempPlan.UnitPlans)
        //        {
        //             //방 평면도에 문그리기
        //            //PlanDrawingFunction_90degree.drawPlan(tempBoundingBox, i.doors, tempScaleFactor, tempOrigin, ref this.typicalPlan, System.Windows.Media.Brushes.Black, 0.2);
        //            //방 창문그리기
        //            //PlanDrawingFunction_90degree.drawPlan(tempBoundingBox, i.windows, tempScaleFactor, tempOrigin, ref this.typicalPlan, System.Windows.Media.Brushes.Black, 0.2);
        //            //방 화장실 타일링 그리기
        //            //PlanDrawingFunction_90degree.drawPlan(tempBoundingBox, i.tilings, tempScaleFactor, tempOrigin, ref this.typicalPlan, System.Windows.Media.Brushes.Black, 0.1);
        //            //방 벽그리기
        //            //PlanDrawingFunction_90degree.drawPlan(tempBoundingBox, i.walls, tempScaleFactor, tempOrigin, ref this.typicalPlan, System.Windows.Media.Brushes.Black, 0.2);
        //            //방 과 방 분리벽
        //            //PlanDrawingFunction_90degree.drawPlan(tempBoundingBox, i.caps, tempScaleFactor, tempOrigin, ref this.typicalPlan, System.Windows.Media.Brushes.Black, 0.2);
        //        }


        //        foreach (CorePlan i in tempPlan.CorePlans)
        //        {
        //            //코어 계단과 엘리베이터 상세도면
        //            PlanDrawingFunction_90degree.drawPlan(tempBoundingBox, i.normals, tempScaleFactor, tempOrigin, ref this.typicalPlan, System.Windows.Media.Brushes.Black, 0.075);
        //            //코어 보이드 공간
        //            PlanDrawingFunction_90degree.drawPlan(tempBoundingBox, i.others, tempScaleFactor, tempOrigin, ref this.typicalPlan, System.Windows.Media.Brushes.Black, 0.075);
        //            //코어 벽
        //            PlanDrawingFunction_90degree.drawPlan(tempBoundingBox, i.walls, tempScaleFactor, tempOrigin, ref this.typicalPlan, System.Windows.Media.Brushes.Black, 0.075);
        //            //코어 복도
        //            PlanDrawingFunction_90degree.drawPlan(tempBoundingBox, i.groundFloor, tempScaleFactor, tempOrigin, ref this.typicalPlan, System.Windows.Media.Brushes.Black, 0.075);
        //        }

        //        if (typicalPlanValue.Floor == 1)
        //        {
        //            PlanDrawingFunction_90degree.drawPlan(tempBoundingBox, typicalPlanValue.ParkingLines, tempScaleFactor, tempOrigin, ref this.typicalPlan, System.Windows.Media.Brushes.Black, 0.075);
        //            PlanDrawingFunction_90degree.drawPlan(tempBoundingBox, typicalPlanValue.Nonresidentials, tempScaleFactor, tempOrigin, ref this.typicalPlan, System.Windows.Media.Brushes.Black, 0.2);
        //        }

        //        PlanDrawingFunction_90degree.drawPlan(tempBoundingBox, tempPlan.OutLine.ToNurbsCurve(), tempScaleFactor, tempOrigin, ref this.typicalPlan, System.Windows.Media.Brushes.Black, 1);

        //    }
        //}
        //public RegulationChecker SetRegulationChecker
        //{
        //    set
        //    {
        //        RegulationChecker tempRegChecker = value;

        //        Rectangle3d tempBoundingBox = new Rectangle3d(Plane.WorldXY, typicalPlanValue.GetBoundingBox().Min, typicalPlanValue.GetBoundingBox().Max);
        //        Rectangle canvasRectangle = new Rectangle();
        //        canvasRectangle.Width = typicalPlan.Width;
        //        canvasRectangle.Height = typicalPlan.Height;

        //        System.Windows.Point tempOrigin = new System.Windows.Point();
        //        double tempScaleFactor = PlanDrawingFunction_90degree.scaleToFitFactor(canvasRectangle, tempBoundingBox, out tempOrigin);


        //        PlanDrawingFunction_90degree.drawPlan(tempBoundingBox, tempRegChecker.ByLightingLow, this.scaleFactor, origin, ref this.typicalPlan, System.Windows.Media.Brushes.Blue, 1);
        //        PlanDrawingFunction_90degree.drawPlan(tempBoundingBox, tempRegChecker.FromNorthLow, this.scaleFactor, origin, ref this.typicalPlan, System.Windows.Media.Brushes.Orange, 1);
        //        PlanDrawingFunction_90degree.drawPlan(tempBoundingBox, tempRegChecker.FromSurroundings, this.scaleFactor, origin, ref this.typicalPlan, System.Windows.Media.Brushes.PaleGreen, 1);
        //    }
        //}
    }
}