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
    public partial class wpfSection
    {
        public wpfSection()
        {
            InitializeComponent();

            SetTitle();
        }

        private TypicalPlan typicalPlanValue;
        private double scaleFactor = 0;
        private System.Windows.Point origin = new System.Windows.Point();

        private void SetTitle()
        {
            this.Title.Text = "4.단면도";
        }

        public void setPlan(SectionBase planToDraw)
        {
            Rectangle3d tempBoundingBox = new Rectangle3d(Plane.WorldXY, planToDraw.boundingBox.Min, planToDraw.boundingBox.Max);
            Rectangle canvasRectangle = new Rectangle();
            canvasRectangle.Width = typicalPlan.Width;
            canvasRectangle.Height = typicalPlan.Height;

            System.Windows.Point tempOrigin = new System.Windows.Point();
            double tempScaleFactor = PlanDrawingFunction_90degree.scaleToFitFactor(canvasRectangle, tempBoundingBox, out tempOrigin);
            scaleFactor = tempScaleFactor;
            origin = tempOrigin;


            foreach (Hatch i in planToDraw.Hatchs)
                PlanDrawingFunction_90degree.drawHatch(tempBoundingBox, i, tempScaleFactor, tempOrigin, System.Windows.Media.Brushes.DimGray, ref this.typicalPlan);

            PlanDrawingFunction_90degree.drawPlan(tempBoundingBox, planToDraw.CoreOutLines, tempScaleFactor, tempOrigin, ref this.typicalPlan, System.Windows.Media.Brushes.Black, 0.1);
            PlanDrawingFunction_90degree.drawPlan(tempBoundingBox, planToDraw.SectionLines, tempScaleFactor, tempOrigin, ref this.typicalPlan, System.Windows.Media.Brushes.Black, 0.2);

            foreach (Text3d i in planToDraw.texts)
                PlanDrawingFunction_90degree.drawText_section(tempBoundingBox, i, tempScaleFactor, tempOrigin, ref this.typicalPlan, 5, System.Windows.Media.Brushes.Black);

            PlanDrawingFunction_90degree.drawDimension(tempBoundingBox, planToDraw.Dimensions, tempScaleFactor, tempOrigin, ref this.typicalPlan);
            
            foreach(UnitInfo i in planToDraw.Unitinfo)
            {
                PlanDrawingFunction_90degree.drawPlan(tempBoundingBox, i.curves, tempScaleFactor, tempOrigin, ref this.typicalPlan, System.Windows.Media.Brushes.Black, 0.1);

                foreach(Text3d j in i.text)
                    PlanDrawingFunction_90degree.drawText_section(tempBoundingBox, j, tempScaleFactor, tempOrigin, ref this.typicalPlan, 5, System.Windows.Media.Brushes.Black);
            }

            PlanDrawingFunction_90degree.drawPlan(tempBoundingBox, planToDraw.RegsOutput, tempScaleFactor, tempOrigin, ref this.typicalPlan, System.Windows.Media.Brushes.HotPink, 0.4);
        }

    }
}