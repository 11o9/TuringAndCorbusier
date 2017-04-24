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
    public partial class wpfTypicalPlan
    {
        public wpfTypicalPlan(Interval floorInterval)
        {
            InitializeComponent();

            this.SetTitle(floorInterval);
        }

        private typicalPlan typicalPlanValue;
        private double scaleFactor = 0;
        private System.Windows.Point origin = new System.Windows.Point();

        private void SetTitle(Interval floorInterval)
        {
            if (floorInterval.Min == floorInterval.Max)
            {
                this.Title.Text = "4." + floorInterval.Min.ToString() + "층 평면도";
            }
            else
            {
                this.Title.Text = "4." + floorInterval.Min.ToString() + "~" + floorInterval.Max.ToString() + "층 평면도";
            }
        }

        public typicalPlan SetTypicalPlan
        {
            set
            {
                typicalPlanValue = value;

                typicalPlan tempPlan = value;

                Rectangle3d tempBoundingBox = new Rectangle3d(Plane.WorldXY, tempPlan.GetBoundingBox().Min, tempPlan.GetBoundingBox().Max);
                Rectangle canvasRectangle = new Rectangle();
                canvasRectangle.Width = typicalPlan.Width;
                canvasRectangle.Height = typicalPlan.Height;

                System.Windows.Point tempOrigin = new System.Windows.Point();
                double tempScaleFactor = PlanDrawingFunction_90degree.scaleToFitFactor(canvasRectangle, tempBoundingBox, out tempOrigin);
                scaleFactor = tempScaleFactor;
                origin = tempOrigin;

                //doc.Objects.AddCurve(Boundary);

                PlanDrawingFunction_90degree.drawPlan(tempBoundingBox, tempPlan.SurroundingSite, tempScaleFactor, tempOrigin, ref this.typicalPlan, System.Windows.Media.Brushes.Black, 0.2);
                PlanDrawingFunction_90degree.drawBackGround(tempBoundingBox, tempPlan.Boundary, tempScaleFactor, tempOrigin, ref this.typicalPlan, new System.Windows.Media.SolidColorBrush(Color.FromRgb(240, 240, 240)));
                PlanDrawingFunction_90degree.drawPlan(tempBoundingBox, tempPlan.Boundary, tempScaleFactor, tempOrigin, ref this.typicalPlan, new System.Windows.Media.SolidColorBrush(Color.FromRgb(255, 0, 0)), 0.2);

                foreach (Text3d i in tempPlan.RoadWidth)
                {
                    PlanDrawingFunction_90degree.drawText(tempBoundingBox, i, tempScaleFactor, tempOrigin, ref this.typicalPlan, 10, System.Windows.Media.Brushes.HotPink);
                }

                foreach (FloorPlan i in tempPlan.UnitPlans)
                {
                    PlanDrawingFunction_90degree.drawPlan(tempBoundingBox, i.doors, tempScaleFactor, tempOrigin, ref this.typicalPlan, System.Windows.Media.Brushes.Black, 0.2);
                    PlanDrawingFunction_90degree.drawPlan(tempBoundingBox, i.windows, tempScaleFactor, tempOrigin, ref this.typicalPlan, System.Windows.Media.Brushes.Black, 0.2);
                    PlanDrawingFunction_90degree.drawPlan(tempBoundingBox, i.tilings, tempScaleFactor, tempOrigin, ref this.typicalPlan, System.Windows.Media.Brushes.Black, 0.1);
                    PlanDrawingFunction_90degree.drawPlan(tempBoundingBox, i.walls, tempScaleFactor, tempOrigin, ref this.typicalPlan, System.Windows.Media.Brushes.Black, 0.2);
                    PlanDrawingFunction_90degree.drawPlan(tempBoundingBox, i.caps, tempScaleFactor, tempOrigin, ref this.typicalPlan, System.Windows.Media.Brushes.Black, 0.2);
                }

                foreach (CorePlan i in tempPlan.CorePlans)
                {
                    PlanDrawingFunction_90degree.drawPlan(tempBoundingBox, i.normals, tempScaleFactor, tempOrigin, ref this.typicalPlan, System.Windows.Media.Brushes.Black, 0.1);
                    PlanDrawingFunction_90degree.drawPlan(tempBoundingBox, i.others, tempScaleFactor, tempOrigin, ref this.typicalPlan, System.Windows.Media.Brushes.Black, 0.075);
                    PlanDrawingFunction_90degree.drawPlan(tempBoundingBox, i.walls, tempScaleFactor, tempOrigin, ref this.typicalPlan, System.Windows.Media.Brushes.Black, 0.2);

                    PlanDrawingFunction_90degree.drawPlan(tempBoundingBox, i.groundFloor, tempScaleFactor, tempOrigin, ref this.typicalPlan, System.Windows.Media.Brushes.Black, 0.2);


                }

                if (typicalPlanValue.Floor == 1)
                {
                    PlanDrawingFunction_90degree.drawPlan(tempBoundingBox, typicalPlanValue.ParkingLines, tempScaleFactor, tempOrigin, ref this.typicalPlan, System.Windows.Media.Brushes.Black, 0.075);
                    PlanDrawingFunction_90degree.drawPlan(tempBoundingBox, typicalPlanValue.Nonresidentials, tempScaleFactor, tempOrigin, ref this.typicalPlan, System.Windows.Media.Brushes.Black, 0.2);
                }

                PlanDrawingFunction_90degree.drawPlan(tempBoundingBox, tempPlan.OutLine.ToNurbsCurve(), tempScaleFactor, tempOrigin, ref this.typicalPlan, System.Windows.Media.Brushes.Black, 1);

            }
        }
        public RegulationChecker SetRegulationChecker
        {
            set
            {
                RegulationChecker tempRegChecker = value;

                Rectangle3d tempBoundingBox = new Rectangle3d(Plane.WorldXY, typicalPlanValue.GetBoundingBox().Min, typicalPlanValue.GetBoundingBox().Max);
                Rectangle canvasRectangle = new Rectangle();
                canvasRectangle.Width = typicalPlan.Width;
                canvasRectangle.Height = typicalPlan.Height;

                System.Windows.Point tempOrigin = new System.Windows.Point();
                double tempScaleFactor = PlanDrawingFunction_90degree.scaleToFitFactor(canvasRectangle, tempBoundingBox, out tempOrigin);


                PlanDrawingFunction_90degree.drawPlan(tempBoundingBox, tempRegChecker.ByLightingLow, this.scaleFactor, origin, ref this.typicalPlan, System.Windows.Media.Brushes.Blue, 1);
                PlanDrawingFunction_90degree.drawPlan(tempBoundingBox, tempRegChecker.FromNorthLow, this.scaleFactor, origin, ref this.typicalPlan, System.Windows.Media.Brushes.Orange, 1);
                PlanDrawingFunction_90degree.drawPlan(tempBoundingBox, tempRegChecker.FromSurroundings, this.scaleFactor, origin, ref this.typicalPlan, System.Windows.Media.Brushes.PaleGreen, 1);
            }
        }
    }
}