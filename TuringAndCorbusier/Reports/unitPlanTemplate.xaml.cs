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
            this.wallArea.Text = Math.Round(houseHoldProperty.GetWallArea() / 1000000, 2).ToString() + "m\xB2";
            this.coreArea.Text = Math.Round(coreArea / 1000000, 2).ToString() + "m\xB2";
            this.commonLivingArea.Text = Math.Round((houseHoldProperty.GetWallArea() + coreArea) / 1000000, 2).ToString() + "m\xB2";
            this.providedArea.Text = Math.Round((houseHoldProperty.GetExclusiveArea() + houseHoldProperty.GetWallArea() + coreArea) / 1000000, 2).ToString() + "m\xB2";
            this.ParkingLotArea.Text = Math.Round(parkingLotArea / 1000000, 2).ToString() + "m\xB2";
            this.ContractArea.Text = Math.Round((houseHoldProperty.GetExclusiveArea() + houseHoldProperty.GetWallArea() + coreArea + publicFacilityArea + serviceArea + parkingLotArea) / 1000000).ToString() + "m\xB2";
            this.balconyArea.Text = Math.Round(houseHoldProperty.GetBalconyArea() / 1000000, 2).ToString() + "m\xB2";
        }


        public void SetUnitPlan(Curve householdOutline, HouseholdStatistics value, double scaleFactor, System.Windows.Point origin, string agType)
        {
            Rectangle3d tempBoundingBox = new Rectangle3d(Plane.WorldXY, householdOutline.GetBoundingBox(true).Min, householdOutline.GetBoundingBox(true).Max);
            //Point3d householdOutlineCentroid = AreaMassProperties.Compute(householdOutline).Centroid;
            System.Windows.Shapes.Rectangle canvas = new Rectangle();
            canvas.Height = UnitCanvas.Height;
            canvas.Width = UnitCanvas.Width;
            origin.X = canvas.Width / 2;
            origin.Y = canvas.Height / 2;
            List<Point3d> dimensionLocationPointList = new List<Point3d>();
            List<double> houseOutlineLength = GetLineLength(householdOutline, out dimensionLocationPointList, tempBoundingBox);
            List<Curve> horizontal = new List<Curve>();
            List<Curve> vertical = new List<Curve>();
           DimensionLines(householdOutline, tempBoundingBox,out horizontal,out vertical);
            horizontal = HorizontalDimensionLineDetail(horizontal,tempBoundingBox);
            vertical = VerticalDimensionLineDetail(vertical, tempBoundingBox);
            PlanDrawingFunction.drawUnitBackGround(tempBoundingBox, householdOutline, scaleFactor - 0.01, origin, ref this.UnitCanvas, System.Windows.Media.Brushes.LightGreen);
          //  PlanDrawingFunction.drawUnitPlan(tempBoundingBox, householdOutline, scaleFactor - 0.01, origin, ref this.UnitCanvas, System.Windows.Media.Brushes.Black, 3);

            PlanDrawingFunction.DrawUnitPlanDimension(householdOutline, dimensionLocationPointList, houseOutlineLength, tempBoundingBox, scaleFactor - 0.01, origin, ref this.UnitCanvas);

            for (int i = 0; i < horizontal.Count; i++)
            {
                PlanDrawingFunction.drawUnitPlan(tempBoundingBox, horizontal[i], scaleFactor - 0.01, origin, ref this.UnitCanvas, System.Windows.Media.Brushes.Black, 0.075);
            }

            for (int i = 0; i < vertical.Count; i++)
            {
                PlanDrawingFunction.drawUnitPlan(tempBoundingBox, vertical[i], scaleFactor - 0.01, origin, ref this.UnitCanvas, System.Windows.Media.Brushes.Black, 0.075);
            }
        }
        private static List<Curve> HorizontalDimensionLineDetail(List<Curve> dimensionLines,Rectangle3d boundingBox)
        {
            List<Curve> horizontal = new List<Curve>();
            for(int i = 0; i < dimensionLines.Count; i++)
            {
                Rhino.Geometry.Line line1 = new Rhino.Geometry.Line(dimensionLines[i].PointAtStart, new Point3d(boundingBox.Corner(0).X, dimensionLines[i].PointAtStart.Y, dimensionLines[i].PointAtStart.Z));
                Rhino.Geometry.Line line2 = new Rhino.Geometry.Line(dimensionLines[i].PointAtEnd, new Point3d(boundingBox.Corner(0).X, dimensionLines[i].PointAtEnd.Y, dimensionLines[i].PointAtEnd.Z));
                horizontal.Add(line1.ToNurbsCurve());
                horizontal.Add(line2.ToNurbsCurve());
                horizontal.Add(dimensionLines[i]);
            }
            return horizontal;
        }

        private static List<Curve> VerticalDimensionLineDetail(List<Curve> dimensionLines, Rectangle3d boundingBox)
        {
            List<Curve> vertical = new List<Curve>();
            for (int i = 0; i < dimensionLines.Count; i++)
            {
                Rhino.Geometry.Line line1 = new Rhino.Geometry.Line(dimensionLines[i].PointAtStart, new Point3d(dimensionLines[i].PointAtStart.X, boundingBox.Corner(2).Y, dimensionLines[i].PointAtStart.Z));
                Rhino.Geometry.Line line2 = new Rhino.Geometry.Line(dimensionLines[i].PointAtEnd, new Point3d(dimensionLines[i].PointAtEnd.X, boundingBox.Corner(2).Y, dimensionLines[i].PointAtEnd.Z));
                vertical.Add(line1.ToNurbsCurve());
                vertical.Add(line2.ToNurbsCurve());
                vertical.Add(dimensionLines[i]);
            }
            return vertical;
        }
        private List<double> GetLineLength(Curve houseOutline, out List<Point3d> dimensionLocationPointList, Rectangle3d tempBoundingBox)
        {
            List<double> houseOutlineLength = new List<double>();
            Curve[] houseOutlineSegment = houseOutline.DuplicateSegments();
            dimensionLocationPointList = new List<Point3d>();
            foreach (Curve segment in houseOutlineSegment)
            {
                houseOutlineLength.Add(Math.Round(segment.GetLength()));
                Point3d p = new Point3d(segment.PointAtNormalizedLength(0.5));
                bool isHorizontal = IsHorizontal(segment);
                if (isHorizontal == true)
                {
                    p.X = tempBoundingBox.Corner(0).X - 1200;
                } else
                {
                    p.Y = tempBoundingBox.Corner(2).Y + 900;
                }
                dimensionLocationPointList.Add(p);
            }
            return houseOutlineLength;
        }


        private static bool IsHorizontal(Curve segment)
        {
            bool isHorizontal = false;
            if (segment.PointAtStart.X == segment.PointAtEnd.X)
            {
                isHorizontal = true;
            }
            return isHorizontal;
        }

        private static void DimensionLines(Curve householdeOutline, Rectangle3d boundingBox,out List<Curve>finalHorizontal,out List<Curve>finalVertical)
        {
            Curve[] segment = householdeOutline.DuplicateSegments();
            List<Curve> horizontal = new List<Curve>();
            List<Curve> vertical = new List<Curve>();
            List<double> horizontalLength = new List<double>();

            for (int i = 0; i < segment.Length; i++)
            {
                bool isHorizontal = IsHorizontal(segment[i]);
                if (isHorizontal == true)
                {
                    horizontal.Add(segment[i]);
                }
                else
                {
                    vertical.Add(segment[i]);
                }
            }
            finalHorizontal = new List<Curve>();
            finalVertical = new List<Curve>();
            finalHorizontal = HorizontalDimensionLinePlacement(horizontal,boundingBox);
            finalVertical = VerticalDimensionLinePlacement(vertical, boundingBox);
  
        }

        private static List<Curve> HorizontalDimensionLinePlacement(List<Curve> dimensionLine, Rectangle3d boundingBox)
        {
            List<double> lengthList = new List<double>();
            for (int i = 0; i < dimensionLine.Count; i++)
            {
                Curve curve = dimensionLine[i].DuplicateCurve();
                double t = 0;
                Rhino.Geometry.Line line = new Rhino.Geometry.Line(curve.PointAtStart, curve.PointAtEnd);
                Point3d point = line.ClosestPoint(boundingBox.Corner(0), false);
                Vector3d dir = boundingBox.Corner(0) - point;
                curve.Transform(Rhino.Geometry.Transform.Translation(dir));
                dimensionLine[i] = curve;
                lengthList.Add(dimensionLine[i].GetLength());
            }
            for (int i = 0; i < dimensionLine.Count; i++)
            {
                Vector3d direction = dimensionLine[i].TangentAtStart;
                if (direction.Y == -1)
                {
                    direction.Rotate(-Math.PI / 2, Vector3d.ZAxis);
                }
                else
                {
                    direction.Rotate(Math.PI / 2, Vector3d.ZAxis);
                }
                dimensionLine[i].Transform(Rhino.Geometry.Transform.Translation(direction * 500));
            }
            if (dimensionLine.Count > 2)
            {
                int longestLength = GetLargestValueIndex(lengthList);
                Vector3d dir1 = dimensionLine[longestLength].TangentAtStart;
                if (dir1.Y == -1)
                {
                    dir1.Rotate(-Math.PI / 2, Vector3d.ZAxis);
                }
                else
                {
                    dir1.Rotate(Math.PI / 2, Vector3d.ZAxis);
                }
                dimensionLine[longestLength].Transform(Rhino.Geometry.Transform.Translation(dir1 * 800));

            }
            return dimensionLine;
        }

        private static List<Curve> VerticalDimensionLinePlacement(List<Curve> dimensionLine, Rectangle3d boundingBox)
        {
            List<double> lengthList = new List<double>();
            for (int i = 0; i < dimensionLine.Count; i++)
            {
                Curve curve = dimensionLine[i].DuplicateCurve();
                double t = 0;
                Rhino.Geometry.Line line = new Rhino.Geometry.Line(curve.PointAtStart, curve.PointAtEnd);
                Point3d point = line.ClosestPoint(boundingBox.Corner(2), false);
                Vector3d dir = boundingBox.Corner(2) - point;
                curve.Transform(Rhino.Geometry.Transform.Translation(dir));
                dimensionLine[i] = curve;
                lengthList.Add(dimensionLine[i].GetLength());
            }
            for (int i = 0; i < dimensionLine.Count; i++)
            {
                Vector3d direction = dimensionLine[i].TangentAtStart;
                if (direction.X == -1)
                {
                    direction.Rotate(-Math.PI / 2, Vector3d.ZAxis);
                }
                else
                {
                    direction.Rotate(Math.PI / 2, Vector3d.ZAxis);
                }
                dimensionLine[i].Transform(Rhino.Geometry.Transform.Translation(direction * 500));
            }
            if (dimensionLine.Count > 2)
            {
                int longestLength = GetLargestValueIndex(lengthList);
                Vector3d dir1 = dimensionLine[longestLength].TangentAtStart;
                if (dir1.X == -1)
                {
                    dir1.Rotate(-Math.PI / 2, Vector3d.ZAxis);
                }
                else
                {
                    dir1.Rotate(Math.PI / 2, Vector3d.ZAxis);
                }
                dimensionLine[longestLength].Transform(Rhino.Geometry.Transform.Translation(dir1 * 800));

            }
            return dimensionLine;
        }

        public static System.Windows.Shapes.Rectangle GetUnitPlanRectangle()
        {
            System.Windows.Shapes.Rectangle unitPlanRectangle = new System.Windows.Shapes.Rectangle();
            unitPlanRectangle.Width = 913;
            unitPlanRectangle.Height = 298;
            return unitPlanRectangle;
        }

        private static int GetLargestValueIndex(List<double> textBlockList)
        {
            int index = 0;
            for (int i = 0; i < textBlockList.Count; i++)
            {

                if (textBlockList[index] < textBlockList[i])
                {
                    index = i;
                }

            }
            return index;
        }

       
    }
}
