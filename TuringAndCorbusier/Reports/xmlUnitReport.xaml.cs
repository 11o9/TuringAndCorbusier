using System;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Shapes;
using Rhino.Geometry;
using TuringAndCorbusier;
using System.Linq;

namespace Reports
{
    /// <summary>
    /// Window1.xaml에 대한 상호 작용 논리
    /// </summary>\
    /// 

    public partial class xmlUnitReport
    {

        public xmlUnitReport()
        {

        }

        public xmlUnitReport(Household houseHoldProperty, string typeString, double coreArea, double parkingLotArea, double publicFacilityArea, double serviceArea, int numOfThisType)
        {
            InitializeComponent();

            this.AreaType.Text = Math.Round(houseHoldProperty.ExclusiveArea / 1000000, 0).ToString() + "m\xB2 " + typeString + "타입";
            this.NumberOfThisType.Text = numOfThisType.ToString() + "세대";
            this.exclusiveArea.Text = String.Format("{0:0.00}",Math.Round(houseHoldProperty.GetExclusiveArea() / 1000000, 2)).ToString() + "m\xB2";
            this.exclusiveArea_Py.Text = Math.Round(houseHoldProperty.GetExclusiveArea() / 1000000 / 3.3, 2).ToString() + "평";
            this.wallArea.Text = String.Format("{0:0.00}",Math.Round(houseHoldProperty.GetWallArea() / 1000000, 2)).ToString() + "m\xB2";
            this.wallArea_Py.Text = Math.Round(houseHoldProperty.GetWallArea() / 1000000 / 3.3, 2).ToString() + "평";
            this.coreArea.Text = String.Format("{0:0.00}",Math.Round(coreArea / 1000000, 2)).ToString() + "m\xB2";
            this.coreArea_Py.Text = Math.Round(coreArea / 1000000 / 3.3, 2).ToString() + "평";
            this.commonLivingArea.Text = String.Format("{0:0.00}",Math.Round((houseHoldProperty.GetWallArea() + coreArea) / 1000000, 2)).ToString() + "m\xB2";
            this.commonLivingArea_Py.Text = Math.Round((houseHoldProperty.GetWallArea() + coreArea) / 1000000 / 3.3, 2).ToString() + "평";
            this.providedArea.Text = String.Format("{0:0.00}",Math.Round((houseHoldProperty.GetExclusiveArea() + houseHoldProperty.GetWallArea() + coreArea) / 1000000, 2)).ToString() + "m\xB2";
            this.providedArea_Py.Text = String.Format("{0:0.00}",Math.Round((houseHoldProperty.GetExclusiveArea() + houseHoldProperty.GetWallArea() + coreArea) / 1000000 / 3.3, 2)).ToString() + "평";
            this.publicFacilityArea.Text = String.Format("{0:0.00}",Math.Round(publicFacilityArea / 1000000, 2)) + "m\xB2";
            this.publicFacilityArea_Py.Text = Math.Round(publicFacilityArea / 1000000 / 3.3, 2) + "평";
            this.ServiceArea.Text = String.Format("{0:0.00}", Math.Round(serviceArea / 1000000, 2)) + "m\xB2";
            this.ServiceArea_Py.Text = Math.Round(serviceArea / 1000000 / 3.3, 2) + "평";
            this.ParkingLotArea.Text = String.Format("{0:0.00}", Math.Round(parkingLotArea / 1000000, 2)).ToString() + "m\xB2";
            this.ParkingLotArea_Py.Text = Math.Round(parkingLotArea / 1000000 / 3.3, 2).ToString() + "평";
            this.ContractArea.Text = String.Format("{0:0.00}", Math.Round((houseHoldProperty.GetExclusiveArea() + houseHoldProperty.GetWallArea() + coreArea + publicFacilityArea + serviceArea + parkingLotArea) / 1000000)).ToString() + "m\xB2";
            this.ContractArea_Py.Text = Math.Round((houseHoldProperty.GetExclusiveArea() + houseHoldProperty.GetWallArea() + coreArea + publicFacilityArea + serviceArea + parkingLotArea) / 1000000 / 3.3).ToString() + "평";
            this.balconyArea.Text = String.Format("{0:0.00}", Math.Round(houseHoldProperty.GetBalconyArea() / 1000000, 2)).ToString() + "m\xB2";
            this.balconyArea_Py.Text = Math.Round(houseHoldProperty.GetBalconyArea() / 1000000 / 3.3, 2).ToString() + "평";
            this.usableArea.Text = String.Format("{0:0.00}", Math.Round((houseHoldProperty.GetExclusiveArea() + houseHoldProperty.GetBalconyArea() + houseHoldProperty.GetWallArea()) / 1000000, 2)).ToString() + "m\xB2";
            this.usableArea_Py.Text = Math.Round((houseHoldProperty.GetExclusiveArea() + houseHoldProperty.GetBalconyArea() + houseHoldProperty.GetWallArea()) / 1000000 / 3.3, 2).ToString() + "평";
        }

        public static System.Windows.Shapes.Rectangle GetCanvasRectangle()
        {
            System.Windows.Shapes.Rectangle tempRectangle = new System.Windows.Shapes.Rectangle();
            tempRectangle.Width = 940;
            tempRectangle.Height = 750;

            return tempRectangle;
        }

        public void setUnitPlan(HouseholdStatistics value, FloorPlan floorPlan, double scaleFactor, System.Windows.Point origin, string agType)
        {
            try
            {
                Household tempHouseHoldProperties = value.ToHousehold();
                tempHouseHoldProperties.XDirection = Vector3d.XAxis;
                tempHouseHoldProperties.YDirection = Vector3d.YAxis;

                ////////////////////////////////////////////////////////

                Rectangle3d tempBoundingBox = new Rectangle3d(Plane.WorldXY, floorPlan.GetBoundingBox().Min, floorPlan.GetBoundingBox().Max);

                Rectangle canvasRectangle = new Rectangle();
                canvasRectangle.Width = UnitPlanCanvas.Width/1000;
                canvasRectangle.Height = UnitPlanCanvas.Height/1000;
                Curve boundary = tempHouseHoldProperties.GetOutline();

                //LineCurveDrawing

                PlanDrawingFunction.drawBackGround(tempBoundingBox, boundary, scaleFactor, origin, ref this.UnitPlanCanvas, new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(245, 245, 245)));
                foreach (Curve i in floorPlan.caps)
                    PlanDrawingFunction.drawBackGround(tempBoundingBox, i, scaleFactor, origin, ref this.UnitPlanCanvas, new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(245, 245, 245)));

                PlanDrawingFunction.drawPlan(tempBoundingBox, floorPlan.walls, scaleFactor, origin, ref this.UnitPlanCanvas, System.Windows.Media.Brushes.Black, 2);
                PlanDrawingFunction.drawPlan(tempBoundingBox, floorPlan.tilings, scaleFactor, origin, ref this.UnitPlanCanvas, System.Windows.Media.Brushes.Gray, 1);
                PlanDrawingFunction.drawPlan(tempBoundingBox, floorPlan.doors, scaleFactor, origin, ref this.UnitPlanCanvas, System.Windows.Media.Brushes.Black, 0.5);
                PlanDrawingFunction.drawPlan(tempBoundingBox, floorPlan.windows, scaleFactor, origin, ref this.UnitPlanCanvas, System.Windows.Media.Brushes.Black, 0.5);
                PlanDrawingFunction.drawPlan(tempBoundingBox, floorPlan.centerLines, scaleFactor, origin, ref this.UnitPlanCanvas, System.Windows.Media.Brushes.Gold, 1);
                PlanDrawingFunction.drawPlan(tempBoundingBox, floorPlan.caps, scaleFactor, origin, ref this.UnitPlanCanvas, System.Windows.Media.Brushes.Black, 2);
                PlanDrawingFunction.drawDimension(tempBoundingBox, floorPlan.dimensions, scaleFactor, origin, ref this.UnitPlanCanvas);

                Rhino.Display.Text3d CenterText = new Rhino.Display.Text3d("");///Math.Round(tempHouseHoldProperties.GetArea() / 1000000, 0) + "m\xB2", new Plane((floorPlan.GetBoundingBox().Min + floorPlan.GetBoundingBox().Max) / 2, Vector3d.ZAxis), 3);
                PlanDrawingFunction.drawText(tempBoundingBox, CenterText, scaleFactor, origin, ref this.UnitPlanCanvas, 50, System.Windows.Media.Brushes.Gold);

                foreach (Rhino.Display.Text3d i in floorPlan.roomTags)
                {
                    PlanDrawingFunction.drawText(tempBoundingBox, i, scaleFactor, origin, ref this.UnitPlanCanvas, 20, System.Windows.Media.Brushes.Black);
                }

                //PlanDrawingFunction.drawPlan(tempBoundingBox, tempPlan.balconyLines, tempScaleFactor, tempOrigin, ref this.UnitPlanCanvas, System.Windows.Media.Brushes.Black, 0.5);
            }
            catch (System.Exception ex)
            {
                System.Windows.MessageBox.Show(ex.ToString());
            }
        }
    }
}
