using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using TuringAndCorbusier;
using Rhino.Geometry;

namespace Reports
{
    /// <summary>
    /// unitPlanTemplate.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class unitPlanTemplate : UserControl
    {
        public unitPlanTemplate()
        {

        }

        public unitPlanTemplate(Household houseHoldProperty, string typeString, double coreArea, double parkingLotArea, double publicFacilityArea, double serviceArea, int numOfThisType)
        {
            InitializeComponent();
            this.AreaType.Text = Math.Round(houseHoldProperty.ExclusiveArea / 1000000, 0).ToString() + "m\xB2 " + typeString + "타입";
            this.NumberOfThisType.Text = numOfThisType.ToString() + "세대";
            this.exclusiveArea.Text = Math.Round(houseHoldProperty.GetExclusiveArea() / 1000000, 2).ToString() + "m\xB2";
            //this.exclusiveArea_Py.Text = Math.Round(houseHoldProperty.GetExclusiveArea() / 1000000 / 3.3, 2).ToString() + "평";
            this.wallArea.Text = Math.Round(houseHoldProperty.GetWallArea() / 1000000, 2).ToString() + "m\xB2";
            //this.wallArea_Py.Text = Math.Round(houseHoldProperty.GetWallArea() / 1000000 / 3.3, 2).ToString() + "평";
            this.coreArea.Text = Math.Round(coreArea / 1000000, 2).ToString() + "m\xB2";
            //this.coreArea_Py.Text = Math.Round(coreArea / 1000000 / 3.3, 2).ToString() + "평";
            this.commonLivingArea.Text = Math.Round((houseHoldProperty.GetWallArea() + coreArea) / 1000000, 2).ToString() + "m\xB2";
            //this.commonLivingArea_Py.Text = Math.Round((houseHoldProperty.GetWallArea() + coreArea) / 1000000 / 3.3, 2).ToString() + "평";
            this.providedArea.Text = Math.Round((houseHoldProperty.GetExclusiveArea() + houseHoldProperty.GetWallArea() + coreArea) / 1000000, 2).ToString() + "m\xB2";
            //this.providedArea_Py.Text = Math.Round((houseHoldProperty.GetExclusiveArea() + houseHoldProperty.GetWallArea() + coreArea) / 1000000 / 3.3, 2).ToString() + "평";
            //this.publicFacilityArea.Text = Math.Round(publicFacilityArea / 1000000, 2) + "m\xB2";
            //this.publicFacilityArea_Py.Text = Math.Round(publicFacilityArea / 1000000 / 3.3, 2) + "평";
            //this.ServiceArea.Text = Math.Round(serviceArea / 1000000, 2) + "m\xB2";
            //this.ServiceArea_Py.Text = Math.Round(serviceArea / 1000000 / 3.3, 2) + "평";
            this.ParkingLotArea.Text = Math.Round(parkingLotArea / 1000000, 2).ToString() + "m\xB2";
            //this.ParkingLotArea_Py.Text = Math.Round(parkingLotArea / 1000000 / 3.3, 2).ToString() + "평";
            this.ContractArea.Text = Math.Round((houseHoldProperty.GetExclusiveArea() + houseHoldProperty.GetWallArea() + coreArea + publicFacilityArea + serviceArea + parkingLotArea) / 1000000).ToString() + "m\xB2";
            // this.ContractArea_Py.Text = Math.Round((houseHoldProperty.GetExclusiveArea() + houseHoldProperty.GetWallArea() + coreArea + publicFacilityArea + serviceArea + parkingLotArea) / 1000000 / 3.3).ToString() + "평";
            this.balconyArea.Text = Math.Round(houseHoldProperty.GetBalconyArea() / 1000000, 2).ToString() + "m\xB2";
            //this.balconyArea_Py.Text = Math.Round(houseHoldProperty.GetBalconyArea() / 1000000 / 3.3, 2).ToString() + "평";
            //this.usableArea.Text = Math.Round((houseHoldProperty.GetExclusiveArea() + houseHoldProperty.GetBalconyArea() + houseHoldProperty.GetWallArea()) / 1000000, 2).ToString() + "m\xB2";
            //this.usableArea_Py.Text = Math.Round((houseHoldProperty.GetExclusiveArea() + houseHoldProperty.GetBalconyArea() + houseHoldProperty.GetWallArea()) / 1000000 / 3.3, 2).ToString() + "평";
        }

        private List<double> GetLineLength(Curve houseOutline, out List<Point3d> dimensionLocationPointList)
        {
            List<double> houseOutlineLength = new List<double>();
            Curve[] houseOutlineSegment = houseOutline.DuplicateSegments();
            dimensionLocationPointList = new List<Point3d>();
            foreach(Curve segment in houseOutlineSegment)
            {
                houseOutlineLength.Add(Math.Round(segment.GetLength()));
                dimensionLocationPointList.Add(segment.PointAtNormalizedLength(0.5));
            }
            return houseOutlineLength;
        }


        public void SetUnitPlan(Curve householdOutline, HouseholdStatistics value, double scaleFactor, System.Windows.Point origin, string agType)
        {
            Rectangle3d tempBoundingBox = new Rectangle3d(Plane.WorldXY, householdOutline.GetBoundingBox(true).Min, householdOutline.GetBoundingBox(true).Max);
            Point3d householdOutlineCentroid = AreaMassProperties.Compute(householdOutline).Centroid;

            System.Windows.Shapes.Rectangle canvas = new Rectangle();
            canvas.Height = UnitCanvas.Height;
            canvas.Width = UnitCanvas.Width;
            origin.X = canvas.Width / 2;
            origin.Y = canvas.Height / 2;
            List<Point3d> dimensionLocationPointList = new List<Point3d>();
            List<double> houseOutlineLength = GetLineLength(householdOutline,out dimensionLocationPointList);


            //PlanDrawingFunction.drawUnitPlan(tempBoundingBox, householdOutline, scaleFactor-0.01, origin, ref this.UnitCanvas, System.Windows.Media.Brushes.Black, 1);
            PlanDrawingFunction.drawUnitBackGround(tempBoundingBox, householdOutline, scaleFactor - 0.01, origin, ref this.UnitCanvas, System.Windows.Media.Brushes.LightGreen);

            PlanDrawingFunction.DrawUnitPlanDimension(householdOutline, dimensionLocationPointList, houseOutlineLength, tempBoundingBox, scaleFactor - 0.01, origin, ref this.UnitCanvas);

            //PlanDrawingFunction.drawDimension(tempBoundingBox, floorPlan.dimensions, scaleFactor, origin, ref this.UnitPlanCanvas);
        }

 

        public static System.Windows.Shapes.Rectangle GetUnitPlanRectangle()
        {
            System.Windows.Shapes.Rectangle unitPlanRectangle = new System.Windows.Shapes.Rectangle();
            unitPlanRectangle.Width = 913;
            unitPlanRectangle.Height = 298;
            return unitPlanRectangle;
        }
    }
}
