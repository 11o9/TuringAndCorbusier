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
    public partial class xmlBuildingReport
    {
        public xmlBuildingReport(Apartment AGoutput)
        {
            InitializeComponent();
            this.projectName.Text = TuringAndCorbusierPlugIn.InstanceClass.page1Settings.ProjectName;
            this.address.Text = TuringAndCorbusierPlugIn.InstanceClass.page1Settings.Address;
            this.plotType.Text = TuringAndCorbusierPlugIn.InstanceClass.page1Settings.PlotType;
            this.plotArea_Manual.Text = Math.Round(TuringAndCorbusierPlugIn.InstanceClass.page1Settings.PlotArea, 2).ToString() + "m\xB2";
            //this.plotArea_Manual_Py.Text = Math.Round(TuringAndCorbusierPlugIn.InstanceClass.page1Settings.PlotArea / 3.3, 2).ToString() + "평";
            this.plotArea_Unusable.Text = (0).ToString() + "m\xB2";
            //this.plotArea_Unusable_Py.Text = (0 / 3.3).ToString() + "평";
            this.plotArea_Usable.Text = Math.Round(AGoutput.Plot.GetArea() / 1000000, 2).ToString() + "m\xB2";
            //this.plotArea_Usable_Py.Text = Math.Round(AGoutput.Plot.GetArea() / 1000000 / 3.3, 2).ToString() + "평";
            this.buildingType.Text = "공동주택(" + CommonFunc.GetApartmentType(AGoutput) + ")";
            this.buildingScale.Text = buildingScaleForReport(AGoutput);
            this.buildingArea.Text = Math.Round(AGoutput.GetBuildingArea() / 1000000, 2).ToString() + "m\xB2";
            //this.buildingArea_Py.Text = Math.Round(AGoutput.GetBuildingArea() / 1000000 / 3.3, 2).ToString() + "평";
            this.grossArea_UnderGround.Text = Math.Round(AGoutput.ParkingLotUnderGround.ParkingArea / 1000000, 2).ToString() + "m\xB2"; //////////////////////////////////////////////////////////////
            //this.grossArea_UnderGround_Py.Text = Math.Round(AGoutput.ParkingLotUnderGround.ParkingArea / 1000000 / 3.3, 2).ToString() + "평"; //////////////////////////////////////////////////////////////
            this.grossArea_OverGround.Text = Math.Round(AGoutput.GetGrossArea() / 1000000, 2).ToString() + "m\xB2";
            //this.grossArea_OverGround_Py.Text = Math.Round(AGoutput.GetGrossArea() / 1000000 / 3.3, 2).ToString() + "평";
            this.grossArea.Text = Math.Round((AGoutput.GetGrossArea() + 1) / 1000000, 2).ToString() + "m\xB2";//////////////////////////////////////////////////////////////
            //this.grossArea_Py.Text = Math.Round((AGoutput.GetGrossArea() + 1) / 1000000 / 3.3, 2).ToString() + "평";//////////////////////////////////////////////////////////////
            this.BPR.Text = Math.Round((AGoutput.GetBalconyArea() + AGoutput.ParkingLotUnderGround.ParkingArea + AGoutput.GetCoreAreaOnEarthSum()) / 1000000, 2).ToString() + "m\xB2";
            //this.BPR_Py.Text = Math.Round((AGoutput.GetBalconyArea() + AGoutput.ParkingLotUnderGround.ParkingArea + AGoutput.GetCoreAreaOnEarthSum()) / 1000000 / 3.3, 2).ToString() + "평";
            //this.ConstructionArea.Text = Math.Round((AGoutput.GetGrossArea() + AGoutput.GetBalconyArea() + AGoutput.ParkingLotUnderGround.ParkingArea + AGoutput.GetCoreAreaOnEarthSum()) / 1000000, 2).ToString() + "m\xB2";
            //this.ConstructionArea_Py.Text = Math.Round((AGoutput.GetGrossArea() + AGoutput.GetBalconyArea() + AGoutput.ParkingLotUnderGround.ParkingArea + AGoutput.GetCoreAreaOnEarthSum()) / 1000000 / 3.3, 2).ToString() + "평";
            this.legalParking.Text = AGoutput.GetLegalParkingLotofHousing().ToString();
            //test
            double grossarea = AGoutput.GetGreenArea();
            double balcony = AGoutput.GetBalconyArea();
            double parking = AGoutput.ParkingLotUnderGround.ParkingArea;
            double core1f = AGoutput.GetCoreAreaOnEarthSum();


            this.buildingCoverage.Text = Math.Round(AGoutput.GetBuildingCoverage(), 2).ToString() + "%";
            this.buildingCoverage_legal.Text = "(법정 : " + Math.Round(TuringAndCorbusierPlugIn.InstanceClass.page1Settings.MaxBuildingCoverage) + "%)";
            this.floorAreaRatio.Text = Math.Round(AGoutput.GetGrossAreaRatio(), 2).ToString() + "%";
            this.floorAreaRatio_legal.Text = "(법정 : " + Math.Round(TuringAndCorbusierPlugIn.InstanceClass.page1Settings.MaxFloorAreaRatio) + "%)";
            this.numOfHouseHolds.Text = AGoutput.GetHouseholdCount().ToString() + "세대";
            this.NumOfPakringLots.Text = (AGoutput.ParkingLotOnEarth.GetCount() + AGoutput.ParkingLotUnderGround.Count).ToString() + "대";
            this.ParkingLotArea.Text = "(주차장면적 : " + (AGoutput.ParkingLotUnderGround.ParkingArea / 1000000).ToString() + "m\xB2)";
        }

        private string buildingScaleForReport(Apartment AGoutput)
        {
            return "지상 1층부터 지상 " + (AGoutput.ParameterSet.Stories + 1).ToString() + "층";
        }

        //--------JHL
        public void SetHouseOutline(List<Curve> coreOutline,List<Curve> coreDetail,List<Curve> houseOutline,TypicalPlan typicalPlan)
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
            double scaleFactor = PlanDrawingFunction.scaleToFitFactor(canvasRectangle, rectangleToFit, out initialOriginPoint);



            PlanDrawingFunction.drawPlan(rectangleToFit, surroundingSite, scaleFactor, initialOriginPoint, ref this.typicalPlanCanvas, System.Windows.Media.Brushes.Black, 0.2);
            PlanDrawingFunction.drawBoundaryPlan(rectangleToFit, boundary, scaleFactor, initialOriginPoint, ref this.typicalPlanCanvas, System.Windows.Media.Brushes.Black, 5);
            //PlanDrawingFunction.drawBackGround(rectangleToFit, boundary, scaleFactor, initialOriginPoint, ref this.typicalPlanCanvas, System.Windows.Media.Brushes.LightGray);

            foreach (Curve house in houseOutline)
            {
                PlanDrawingFunction.drawPlan(rectangleToFit, house, scaleFactor, initialOriginPoint, ref this.typicalPlanCanvas, System.Windows.Media.Brushes.Black, 1);

            }

            foreach(Curve core in corePlanList)
            {
                PlanDrawingFunction.drawPlan(rectangleToFit, core, scaleFactor, initialOriginPoint, ref this.typicalPlanCanvas, System.Windows.Media.Brushes.Black, 1);
                
            }

            foreach (FloorPlan floorPlan in floorPlanList)
            {
                PlanDrawingFunction.drawPlan(rectangleToFit, floorPlan.balconyLines, scaleFactor, initialOriginPoint, ref this.typicalPlanCanvas, System.Windows.Media.Brushes.Black, 0.075);
            }

            PlanDrawingFunction.drawPlan(rectangleToFit, typicalPlan.OutLine.ToNurbsCurve(), scaleFactor, initialOriginPoint, ref this.typicalPlanCanvas, System.Windows.Media.Brushes.Black, 0.5);
        }



        //public TypicalPlan SetTypicalPlan
        //{
        //    set
        //    {
        //        TypicalPlan tempPlan = value;

        //        Rectangle3d tempBoundingBox = new Rectangle3d(Plane.WorldXY, tempPlan.GetBoundingBox().Min, tempPlan.GetBoundingBox().Max);
        //        Rectangle canvasRectangle = new Rectangle();
        //        canvasRectangle.Width = typicalPlanCanvas.Width;
        //        canvasRectangle.Height = typicalPlanCanvas.Height;

        //        System.Windows.Point tempOrigin = new System.Windows.Point();
        //        double tempScaleFactor = PlanDrawingFunction.scaleToFitFactor(canvasRectangle, tempBoundingBox, out tempOrigin);

        //        //doc.Objects.AddCurve(Boundary);

        //        PlanDrawingFunction.drawPlan(tempBoundingBox, tempPlan.SurroundingSite, tempScaleFactor, tempOrigin, ref this.typicalPlanCanvas, System.Windows.Media.Brushes.Black, 0.2);
        //        PlanDrawingFunction.drawBackGround(tempBoundingBox, tempPlan.Boundary, tempScaleFactor, tempOrigin, ref this.typicalPlanCanvas, System.Windows.Media.Brushes.White);
        //        PlanDrawingFunction.drawPlan(tempBoundingBox, tempPlan.Boundary, tempScaleFactor, tempOrigin, ref this.typicalPlanCanvas, System.Windows.Media.Brushes.Black, 0.5);

        //        foreach (Text3d i in tempPlan.RoadWidth)
        //            PlanDrawingFunction.drawText(tempBoundingBox, i, tempScaleFactor, tempOrigin, ref this.typicalPlanCanvas, 10, System.Windows.Media.Brushes.HotPink);

        //        foreach (CorePlan i in tempPlan.CorePlans)
        //        {
        //            PlanDrawingFunction.drawPlan(tempBoundingBox, i.normals, tempScaleFactor, tempOrigin, ref this.typicalPlanCanvas, System.Windows.Media.Brushes.Black, 0.2);
        //            PlanDrawingFunction.drawPlan(tempBoundingBox, i.others, tempScaleFactor, tempOrigin, ref this.typicalPlanCanvas, System.Windows.Media.Brushes.DimGray, 0.2);
        //            PlanDrawingFunction.drawPlan(tempBoundingBox, i.walls, tempScaleFactor, tempOrigin, ref this.typicalPlanCanvas, System.Windows.Media.Brushes.Black, 0.2);
        //        }

        //        PlanDrawingFunction.drawPlan(tempBoundingBox, tempPlan.OutLine.ToNurbsCurve(), tempScaleFactor, tempOrigin, ref this.typicalPlanCanvas, System.Windows.Media.Brushes.Black, 1);

        //        //doc.Objects.AddCurve(OutLine.ToNurbsCurve());

        //        //foreach (Text3d i in RoadWidth)
        //        //    doc.Objects.AddText(i);

        //        //foreach (FloorPlan i in UnitPlans)
        //        //{
        //        //    List<Curve> floorPlans = i.caps;

        //        //    foreach (List<Curve> j in i.centerLines)
        //        //        floorPlans.AddRange(j);
        //        //    foreach (List<Curve> j in i.walls)
        //        //        floorPlans.AddRange(j);
        //        //    foreach (List<Curve> j in i.windows)
        //        //        floorPlans.AddRange(j);
        //        //    foreach (List<Curve> j in i.tilings)
        //        //        floorPlans.AddRange(j);
        //        //    foreach (List<Curve> j in i.doors)
        //        //        floorPlans.AddRange(j);

        //        //    foreach (Curve j in floorPlans)
        //        //        doc.Objects.AddCurve(j);
        //        //}

        //        //foreach (CorePlan i in CorePlans)
        //        //{
        //        //    List<Curve> corePlans = i.normals;
        //        //    corePlans.AddRange(i.others);
        //        //    corePlans.AddRange(i.walls);

        //        //    foreach (Curve j in corePlans)
        //        //        doc.Objects.AddCurve(j);
        //        //}

        //        //foreach (Curve i in SurroundingSite)
        //        //    doc.Objects.AddCurve(i);

        //    }
        //}
    }
}