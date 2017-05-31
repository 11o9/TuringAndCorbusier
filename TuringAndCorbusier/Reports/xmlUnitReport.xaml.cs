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
            InitializeComponent();
            
            
        }

        public void SetFirstUnitTypePlan(Reports.unitPlanTemplate singleUnitTemplate)
        {
            unitPlanCanvasControl1.Content = singleUnitTemplate;
        }
        public void SetUnitTypePlan(Reports.unitPlanTemplate planForCanvas1, Reports.unitPlanTemplate planForCanvas2)
        {
            unitPlanCanvasControl1.Content = planForCanvas1;
            unitPlanCanvasControl2.Content = planForCanvas2;
        }

        public static System.Windows.Shapes.Rectangle GetCanvasRectangle()
        {
            System.Windows.Shapes.Rectangle tempRectangle = new System.Windows.Shapes.Rectangle();
            tempRectangle.Width = 940;
            tempRectangle.Height = 750;
            return tempRectangle;
        }

        //public void setUnitPlan(HouseholdStatistics value, FloorPlan floorPlan, double scaleFactor, System.Windows.Point origin, string agType)
        //{
        //    try
        //    {
        //        Household tempHouseHoldProperties = value.ToHousehold();
        //        tempHouseHoldProperties.XDirection = Vector3d.XAxis;
        //        tempHouseHoldProperties.YDirection = Vector3d.YAxis;

        //        ////////////////////////////////////////////////////////

        //        Rectangle3d tempBoundingBox = new Rectangle3d(Plane.WorldXY, floorPlan.GetBoundingBox().Min, floorPlan.GetBoundingBox().Max);

        //        Rectangle canvasRectangle = new Rectangle();
        //        canvasRectangle.Width = UnitPlanCanvas.Width / 1000;
        //        canvasRectangle.Height = UnitPlanCanvas.Height / 1000;
        //        Curve boundary = tempHouseHoldProperties.GetOutline();

        //        //LineCurveDrawing

        //        PlanDrawingFunction.drawBackGround(tempBoundingBox, boundary, scaleFactor, origin, ref this.UnitPlanCanvas, new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(245, 245, 245)));
        //        foreach (Curve i in floorPlan.caps)
        //            PlanDrawingFunction.drawBackGround(tempBoundingBox, i, scaleFactor, origin, ref this.UnitPlanCanvas, new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(245, 245, 245)));

        //        PlanDrawingFunction.drawPlan(tempBoundingBox, floorPlan.walls, scaleFactor, origin, ref this.UnitPlanCanvas, System.Windows.Media.Brushes.Black, 2);
        //        PlanDrawingFunction.drawPlan(tempBoundingBox, floorPlan.tilings, scaleFactor, origin, ref this.UnitPlanCanvas, System.Windows.Media.Brushes.Gray, 1);
        //        PlanDrawingFunction.drawPlan(tempBoundingBox, floorPlan.doors, scaleFactor, origin, ref this.UnitPlanCanvas, System.Windows.Media.Brushes.Black, 0.5);
        //        PlanDrawingFunction.drawPlan(tempBoundingBox, floorPlan.windows, scaleFactor, origin, ref this.UnitPlanCanvas, System.Windows.Media.Brushes.Black, 0.5);
        //        PlanDrawingFunction.drawPlan(tempBoundingBox, floorPlan.centerLines, scaleFactor, origin, ref this.UnitPlanCanvas, System.Windows.Media.Brushes.Gold, 1);
        //        PlanDrawingFunction.drawPlan(tempBoundingBox, floorPlan.caps, scaleFactor, origin, ref this.UnitPlanCanvas, System.Windows.Media.Brushes.Black, 2);
        //        PlanDrawingFunction.drawDimension(tempBoundingBox, floorPlan.dimensions, scaleFactor, origin, ref this.UnitPlanCanvas);

        //        Rhino.Display.Text3d CenterText = new Rhino.Display.Text3d("");///Math.Round(tempHouseHoldProperties.GetArea() / 1000000, 0) + "m\xB2", new Plane((floorPlan.GetBoundingBox().Min + floorPlan.GetBoundingBox().Max) / 2, Vector3d.ZAxis), 3);
        //        PlanDrawingFunction.drawText(tempBoundingBox, CenterText, scaleFactor, origin, ref this.UnitPlanCanvas, 50, System.Windows.Media.Brushes.Gold);

        //        foreach (Rhino.Display.Text3d i in floorPlan.roomTags)
        //        {
        //            PlanDrawingFunction.drawText(tempBoundingBox, i, scaleFactor, origin, ref this.UnitPlanCanvas, 20, System.Windows.Media.Brushes.Black);
        //        }

        //        //PlanDrawingFunction.drawPlan(tempBoundingBox, tempPlan.balconyLines, tempScaleFactor, tempOrigin, ref this.UnitPlanCanvas, System.Windows.Media.Brushes.Black, 0.5);
        //    }
        //    catch (System.Exception ex)
        //    {
        //        System.Windows.MessageBox.Show(ex.ToString());
        //    }
        //}
    }
}
