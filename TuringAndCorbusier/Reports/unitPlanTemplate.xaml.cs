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
           // List<Curve> horizontal = DimensionLines(householdOutline, tempBoundingBox);

          //  PlanDrawingFunction.drawUnitPlan(tempBoundingBox, householdOutline, scaleFactor - 0.01, origin, ref this.UnitCanvas, System.Windows.Media.Brushes.Black, 1);
            PlanDrawingFunction.drawUnitBackGround(tempBoundingBox, householdOutline, scaleFactor - 0.01, origin, ref this.UnitCanvas, System.Windows.Media.Brushes.LightGreen);

            PlanDrawingFunction.DrawUnitPlanDimension(householdOutline, dimensionLocationPointList, houseOutlineLength, tempBoundingBox, scaleFactor - 0.01, origin, ref this.UnitCanvas);

            //for(int i = 0; i < horizontal.Count; i++)
            //{
            //    PlanDrawingFunction.drawUnitPlan(tempBoundingBox, horizontal[i], scaleFactor - 0.01, origin, ref this.UnitCanvas, System.Windows.Media.Brushes.Black, 1);
            //}
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
                    p.X = tempBoundingBox.Corner(0).X - 1500;
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

        //private static List<Curve> DimensionLines(Curve householdeOutline, Rectangle3d boundingBox)
        //{
        //    Curve[] segment = householdeOutline.DuplicateSegments();
        //    List<Curve> horizontal = new List<Curve>();
        //    List<Curve> vertical = new List<Curve>();
        //    List<double> horizontalLength = new List<double>();

        //    for (int i = 0; i < segment.Length; i++)
        //    {
        //        bool isHorizontal = IsHorizontal(segment[i]);
        //        if (isHorizontal == true)
        //        {
        //            horizontal.Add(segment[i]);
        //        } else
        //        {
        //            vertical.Add(segment[i]);
        //        }
        //    }
        //    for(int i = 0; i < horizontal.Count; i++)
        //    {
        //        Curve curve = horizontal[i].DuplicateCurve();
        //        Vector3d dir = curve.TangentAt(0.5);
        //        dir.Rotate(-Math.PI / 2, Vector3d.ZAxis);
        //        curve.Transform(Rhino.Geometry.Transform.Translation(dir*500));
        //        horizontal[i] = curve;
        //        horizontalLength.Add(horizontal[i].GetLength());
        //    }
        //    //int longestIndex = GetLargestValueIndex(horizontalLength);
        //    //Vector3d dir1 = horizontal[longestIndex].TangentAtStart;
        //    //dir1.Rotate(-Math.PI / 2, Vector3d.ZAxis);
        //    //horizontal[longestIndex].Transform(Rhino.Geometry.Transform.Translation(dir1 * 1000));
        //    return horizontal;

        //}


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
