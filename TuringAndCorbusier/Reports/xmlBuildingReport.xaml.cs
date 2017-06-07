using System;
using System.Linq;
using System.Windows.Controls;
using TuringAndCorbusier;
using Rhino.Geometry;
using System.Collections.Generic;
using System.Windows.Media;
using System.Windows.Shapes;
using Rhino.Display;
using System.IO;

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
            //세대구성
            List<HouseholdStatistics> statistics = AGoutput.HouseholdStatistics;
            List<double> area = new List<double>();
            for (int i = 0; i < statistics.Count; i++)
            {
              double exclusiveArea = statistics[i].GetExclusiveArea();
                area.Add(exclusiveArea);
            }
            CreateAreaTypeTextBlock(area);
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
            //this.legalParking.Text = AGoutput.GetLegalParkingLotofHousing().ToString();
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
        public void SetHouseOutline(List<Curve> coreOutline, List<Curve> houseOutline, TypicalPlan typicalPlan, List<Core> newCoreList, double numberOfHouses, List<HouseholdStatistics> uniqueHouseStatistics)
        {
            List<double> exclusivArea = new List<double>();
          
            Curve boundary = typicalPlan.Boundary;
            List<Curve> surroundingSite = typicalPlan.SurroundingSite;
            List<FloorPlan> housePlanList = typicalPlan.UnitPlans;

            List<Curve> corePlanList = coreOutline;
            List<FloorPlan> floorPlanList = typicalPlan.UnitPlans;
            List<Curve> houseOutlineList = houseOutline;

            List<CoreType> coreType = new List<CoreType>();
            List<Point3d> coreOriginPointList = new List<Point3d>();
            foreach (Core c in newCoreList)
            {
                coreType.Add(c.coreType);
                coreOriginPointList.Add(c.origin);
            }

            List<List<Curve>> coreDetailDoubleList = new List<List<Curve>>();
            for (int i = 0; i < coreType.Count; i++)
            {
                string pointString = GetSimplifiedCoreString(coreType[i]);
                List<Curve> coreDetail = GetCoreDetail(pointString);
                coreDetailDoubleList.Add(coreDetail);
            }

            for (int i = 0; i < coreDetailDoubleList.Count; i++)
            {
                Vector3d v = coreOriginPointList[i] - coreDetailDoubleList[i][0].PointAtStart;
                for (int j = 0; j < coreDetailDoubleList[i].Count; j++)
                {
                    coreDetailDoubleList[i][j].Transform(Rhino.Geometry.Transform.Translation(v));
                }

            }

           RotateToFit(ref coreDetailDoubleList,newCoreList,coreType,coreOutline);

            List<Point3d> houseOutlinesCentroid = new List<Point3d>();
            Rectangle3d rectangleToFit = new Rectangle3d(Plane.WorldXY, typicalPlan.GetBoundingBox().Min, typicalPlan.GetBoundingBox().Max);

            Rectangle canvasRectangle = new Rectangle();
            canvasRectangle.Width = typicalPlanCanvas.Width;
            canvasRectangle.Height = typicalPlanCanvas.Height;
            System.Windows.Point initialOriginPoint = new System.Windows.Point();
            double scaleFactor = PlanDrawingFunction.scaleToFitFactor(canvasRectangle, rectangleToFit, out initialOriginPoint);


            PlanDrawingFunction.drawPlan(rectangleToFit, surroundingSite, scaleFactor, initialOriginPoint, ref this.typicalPlanCanvas, System.Windows.Media.Brushes.Black, 0.2);
            PlanDrawingFunction.drawBoundaryPlan(rectangleToFit, boundary, scaleFactor, initialOriginPoint, ref this.typicalPlanCanvas, System.Windows.Media.Brushes.Black, 5);
            //for(int i = 0; i < coreType.Count; i++)
            //{
            //    if (coreType[i] == CoreType.CourtShortEdge)
            //    {
            //        List<Curve> corridor = CreateCorridor(newCoreList);
            //        PlanDrawingFunction.drawPlan(rectangleToFit, corridor, scaleFactor, initialOriginPoint, ref this.typicalPlanCanvas, System.Windows.Media.Brushes.Black, 1);

            //    }

            //}
           
            //PlanDrawingFunction.drawBackGround(rectangleToFit, boundary, scaleFactor, initialOriginPoint, ref this.typicalPlanCanvas, System.Windows.Media.Brushes.LightGray);
            for (int i = 0; i < houseOutline.Count; i++)
            {
     
                {
                    PlanDrawingFunction.drawPlan(rectangleToFit, houseOutlineList[i], scaleFactor, initialOriginPoint, ref this.typicalPlanCanvas, System.Windows.Media.Brushes.Black, 1);
                   // PlanDrawingFunction.drawPlan(rectangleToFit, floorPlanList[i].balconyLines, scaleFactor, initialOriginPoint, ref this.typicalPlanCanvas, System.Windows.Media.Brushes.Black, 0.075);
                }

            }

            for (int i = 0; i < corePlanList.Count ; i++)
            {
     
                    PlanDrawingFunction.drawPlan(rectangleToFit, corePlanList[i], scaleFactor, initialOriginPoint, ref this.typicalPlanCanvas, System.Windows.Media.Brushes.Black, 1);
                    PlanDrawingFunction.drawPlan(rectangleToFit, coreDetailDoubleList[i], scaleFactor, initialOriginPoint, ref this.typicalPlanCanvas, System.Windows.Media.Brushes.Black, 0.5);
               
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
        //JHL

        private void RotateToFit(ref List<List<Curve>> coreDetail, List<Core> coreList, List<CoreType> coreType, List<Curve> coreOutline)
        {

            for (int i = 0; i < coreDetail.Count; i++)
            {
                if (coreType[i] == CoreType.Vertical)
                {

                    Vector3d v1 = coreList[i].YDirection;
                    Vector3d v2 = new Vector3d(coreDetail[i][0].PointAtEnd - coreDetail[i][0].PointAtStart);

                    for (int j = 0; j < coreDetail[i].Count; j++)
                    {
                        double radian = Vector3d.VectorAngle(v2, v1,Plane.WorldXY);

                        coreDetail[i][j].Transform(Rhino.Geometry.Transform.Rotation(radian, coreList[i].Origin));
                    }
                }
                else if (coreType[i] == CoreType.Parallel)
                {
      
                    Vector3d v1 = coreList[i].XDirection;
                    Vector3d v2 = new Vector3d(coreDetail[i][0].PointAtEnd - coreDetail[i][0].PointAtStart);

                    for (int j = 0; j < coreDetail[i].Count; j++)
                    {
                        double radian = Vector3d.VectorAngle(v2, v1, Plane.WorldXY);

                        coreDetail[i][j].Transform(Rhino.Geometry.Transform.Rotation(radian, coreList[i].Origin));
                    }
                }
                else if (coreType[i] == CoreType.Horizontal)
                {

                    Vector3d v1 = coreList[i].XDirection;
                    Vector3d v2 = new Vector3d(coreDetail[i][0].PointAtEnd - coreDetail[i][0].PointAtStart);

                    for (int j = 0; j < coreDetail[i].Count; j++)
                    {
                        double radian = Vector3d.VectorAngle(v2, v1, Plane.WorldXY);

                        coreDetail[i][j].Transform(Rhino.Geometry.Transform.Rotation(radian, coreList[i].Origin));
                    }
                }
                else if (coreType[i] == CoreType.Vertical_AG1)
                {

                    Vector3d v1 = coreList[i].XDirection;
                    Vector3d v2 = new Vector3d(coreDetail[i][0].PointAtEnd - coreDetail[i][0].PointAtStart);

                    for (int j = 0; j < coreDetail[i].Count; j++)
                    {
                        double radian = Vector3d.VectorAngle(v2, v1, Plane.WorldXY);

                        coreDetail[i][j].Transform(Rhino.Geometry.Transform.Rotation(radian, coreList[i].Origin));
                    }
                }
                else if (coreType[i] == CoreType.CourtShortEdge)
                {

                    Vector3d v1 = coreList[i].YDirection;
                    Vector3d v2 = new Vector3d(coreDetail[i][0].PointAtStart - coreDetail[i][0].PointAtEnd);

                        //Curve[] coreOutlineSegment = coreOutline[i].DuplicateSegments();
                        //var longestCoreOutline = (from crv in coreOutlineSegment let len = crv.GetLength() where len > 0 orderby len descending select crv).First();
                        //Point3d segmentMidPoint1 = longestCoreOutline.PointAtLength(longestCoreOutline.GetLength()/2);

                    for (int j = 0; j < coreDetail[i].Count; j++)
                    {
                        double radian = Vector3d.VectorAngle(v2, v1, Plane.WorldXY);
                        coreDetail[i][j].Transform(Rhino.Geometry.Transform.Rotation(radian, coreList[i].Origin));
                        coreDetail[i][j].Transform(Rhino.Geometry.Transform.Translation(coreList[i].YDirection*coreList[i].depth));

                        //var longestCoreDetail = (from crv in coreDetail[i][j] let len = crv.GetLength() where len > 0 orderby len descending select crv).First();
                        //Point3d segmentMidPoint2 = longestCoreDetail.PointAtLength(longestCoreDetail.GetLength()/2);
                        //Vector3d v3 = new Vector3d(segmentMidPoint2 - segmentMidPoint1);


                        //coreDetail[i][j].Transform(Rhino.Geometry.Transform.Translation(v3));
                    }

                }
            }
        }

        private void ExportRadianToText(List<double> radians,List<Core> coreList)
        {
            StreamWriter sw = new StreamWriter("C:\\Users\\user\\Desktop\\radians\\Radians.txt");
            for(int i = 0; i < radians.Count; i++)
            {
                sw.Write(Math.Round(radians[i],3).ToString()+",");
                if (i>0&&i % coreList.Count == 0)
                {
                    sw.Write("\n[]");
                }
            }
        }

        private string GetSimplifiedCoreString(CoreType coreType)
        {
            if (coreType == CoreType.Horizontal)
            {
                return "{0,0,0}/{0,-7660,0}/{0,-7660,0}/{-4400,-7660,0}/{-4400,-7660,0}/{-4400,0,0}/{-4400,0,0}/{0,0,0}/{-1900,0,0}/{-1900,-1300,0}/{-1900,-1300,0}/{-1900,-4000,0}/{-1900,-4000,0}/{-1900,-5300,0}/{-1900,-5300,0}/{-1900,-7660,0}/{-1900,-5300,0}/{-4400,-5300,0}/{-1900,-4000,0}/{-4400,-4000,0}/{-1900,-1300,0}/{-4400,-1300,0}/{-1900,-1600,0}/{-4400,-1600,0}/{-1900,-1900,0}/{-4400,-1900,0}/{-1900,-2200,0}/{-4400,-2200,0}/{-1900,-2500,0}/{-4400,-2500,0}/{-1900,-2800,0}/{-4400,-2800,0}/{-1900,-3100,0}/{-4400,-3100,0}/{-1900,-3400,0}/{-4400,-3400,0}/{-1900,-3700,0}/{-4400,-3700,0}/{-3150,-4000,0}/{-3150,-1300,0}/{-1900,-5300,0}/{-4400,-7660,0}/{-1900,-7660,0}/{-4400,-5300,0}";
            }
            else if (coreType == CoreType.Parallel)
            {
                return "{4860,0,0}/{4860,-5200,0}/{4860,-5200,0}/{0,-5200,0}/{0,-5200,0}/{0,0,0}/{0,0,0}/{4860,0,0}/{0,-2600,0}/{1250,-2600,0}/{1250,-2600,0}/{1250,0,0}/{1530,-2600,0}/{1530,0,0}/{1810,-2600,0}/{1810,0,0}/{2090,-2600,0}/{2090,0,0}/{2370,-2600,0}/{2370,0,0}/{2650,-2600,0}/{2650,0,0}/{2930,-2600,0}/{2930,0,0}/{3210,-2600,0}/{3210,0,0}/{3490,-2600,0}/{3490,0,0}/{1250,-2600,0}/{3490,-2600,0}/{3490,-1300,0}/{1250,-1300,0}/{3490,-2600,0}/{3490,-5200,0}/{890,-5200,0}/{890,-2600,0}/{890,-5200,0}/{3490,-2600,0}/{890,-2600,0}/{3490,-5200,0}/{0,-5200,0}/{890,-2600,0}/{0,-2600,0}/{890,-5200,0}";
            }
            else if (coreType == CoreType.Vertical)
            {
                return "{0,0,0}/{-2500,0,0}/{-7600,-2500,0}/{-4900,-2500,0}/{-9000,-2500,0}/{-7600,-2500,0}/{0,0,0}/{-9000,0,0}/{-9000,0,0}/{-9000,-2500,0}/{-9000,-2500,0}/{-7600,-2500,0}/{-7600,-2500,0}/{-4900,-2500,0}/{-4900,-2500,0}/{-2500,-2500,0}/{-2500,-2500,0}/{0,-2500,0}/{0,-2500,0}/{0,0,0}/{-7600,-2500,0}/{-7600,0,0}/{-2500,-2500,0}/{-2500,0,0}/{-4900,-2500,0}/{-4900,0,0}/{-2500,-2500,0}/{0,0,0}/{-2500,0,0}/{0,-2500,0}/{-5200,-2500,0}/{-5200,0,0}/{-5500,-2500,0}/{-5500,0,0}/{-5800,-2500,0}/{-5800,0,0}/{-6100,-2500,0}/{-6100,0,0}/{-6400,-2500,0}/{-6400,0,0}/{-6700,-2500,0}/{-6700,0,0}/{-7000,-2500,0}/{-7000,0,0}/{-7300,-2500,0}/{-7300,0,0}/{-7600,-1250,0}/{-4900,-1250,0}";
            }
            else if (coreType == CoreType.Folded)
            {
                return "{-6060,0,0}/{0,0,0}/{0,-5800,0}/{-6060,-5800,0}/{-6060,0,0}/{0,0,0}/{-2260,0,0}/{-2260,0,0}/{-4660,0,0}/{-4660,0,0}/{-6060,0,0}/{-6060,0,0}/{-6060,-5800,0}/{-6060,-5800,0}/{0,-5800,0}/{0,-5800,0}/{0,-4400,0}/{0,-4400,0}/{0,-2300,0}/{0,-2300,0}/{0,0,0}/{-1400,-1400,0}/{-1400,-4400,0}/{-1400,-1400,0}/{-4660,-1400,0}/{-4660,-4400,0}/{-4660,-1400,0}/{-4660,-1400,0}/{-4660,0,0}/{-4660,-4400,0}/{-1400,-4400,0}/{-1400,-4400,0}/{0,-4400,0}/{-1400,-4100,0}/{0,-4100,0}/{-1400,-3800,0}/{0,-3800,0}/{-1400,-3500,0}/{0,-3500,0}/{-1400,-3200,0}/{0,-3200,0}/{-1400,-2900,0}/{0,-2900,0}/{-1400,-2600,0}/{0,-2600,0}/{-1400,-2300,0}/{0,-2300,0}/{-4360,-1400,0}/{-4360,0,0}/{-4060,-1400,0}/{-4060,0,0}/{-3760,-1400,0}/{-3760,0,0}/{-3460,-1400,0}/{-3460,0,0}/{-3160,-1400,0}/{-3160,0,0}/{-2860,-1400,0}/{-2860,0,0}/{-2560,-1400,0}/{-2560,0,0}/{-2260,-1400,0}/{-2260,0,0}/{-4660,-4400,0}/{-1400,-1400,0}/{-4660,-1400,0}/{-1400,-4400,0}";
            }
            else if (coreType == CoreType.Vertical_AG1)
            {
                return "{0,-2500,0}/{-2500,-2500,0}/{-6620,-2500,0}/{-3920,-2500,0}/{-7920,-2500,0}/{-6620,-2500,0}/{-7920,0,0}/{-7920,-2500,0}/{0,-2500,0}/{0,0,0}/{-7920,0,0}/{0,0,0}/{-7920,0,0}/{-7920,0,0}/{-7920,-2500,0}/{-7920,-2500,0}/{-6620,-2500,0}/{-6620,-2500,0}/{-3920,-2500,0}/{-3920,-2500,0}/{-2500,-2500,0}/{-2500,-2500,0}/{0,-2500,0}/{-6620,-2500,0}/{-6620,0,0}/{-3920,-2500,0}/{-3920,0,0}/{-2500,-2500,0}/{-2500,0,0}/{-2500,-2500,0}/{0,0,0}/{-2500,0,0}/{0,-2500,0}/{-4220,-2500,0}/{-4220,0,0}/{-4520,-2500,0}/{-4520,0,0}/{-4820,-2500,0}/{-4820,0,0}/{-5120,-2500,0}/{-5120,0,0}/{-5420,-2500,0}/{-5420,0,0}/{-5720,-2500,0}/{-5720,0,0}/{-6020,-2500,0}/{-6020,0,0}/{-6320,-2500,0}/{-6320,0,0}/{-6620,-1250,0}/{-3920,-1250,0}";
            }

            else if (coreType == CoreType.CourtShortEdge)
            {
                return "{0,0,0}/{2700,0,0}/{2700,0,0}/{2700,-1300,0}/{2700,-1300,0}/{2700,-4000,0}/{2700,-4000,0}/{2700,-5300,0}/{2700,-5300,0}/{2700,-7600,0}/{2700,-7600,0}/{0,-7600,0}/{0,-7600,0}/{0,0,0}/{2700,-1300,0}/{0,-1300,0}/{2700,-4000,0}/{0,-4000,0}/{2700,-5300,0}/{0,-5300,0}/{0,-7600,0}/{2700,-5300,0}/{2700,-7600,0}/{0,-5300,0}/{1350,-4000,0}/{1350,-1300,0}/{2700,-1600,0}/{0,-1600,0}/{2700,-1900,0}/{0,-1900,0}/{2700,-2200,0}/{0,-2200,0}/{2700,-2500,0}/{0,-2500,0}/{2700,-2800,0}/{0,-2800,0}/{2700,-3100,0}/{0,-3100,0}/{2700,-3400,0}/{0,-3400,0}/{2700,-3700,0}/{0,-3700,0}";
            }
            else
            {
                return "&";
            }
        }

        private List<Curve> GetCoreDetail(string coreTypeString)
        {

            char[] separator = new char[] { '/' };
            string[] pointStringArray = coreTypeString.Split(separator);
            var secondSeparator = new string[] { "{", "}" };
            for (int i = 0; i < pointStringArray.Length; i++)
            {
                foreach (var x in secondSeparator)
                {
                    pointStringArray[i] = pointStringArray[i].Replace(x, string.Empty);
                }
            }

            List<Point3d> horizontalCorePointList = new List<Point3d>();
            for (int i = 0; i < pointStringArray.Length; i++)
            {
                string[] xyz = pointStringArray[i].Split(',');
                double x = Convert.ToDouble(xyz[0]);
                double y = Convert.ToDouble(xyz[1]);
                double z = Convert.ToDouble(xyz[2]);
                Point3d point = new Point3d(x, y, z);
                horizontalCorePointList.Add(point);
            }

            List<Curve> horizontalCoreCurve = new List<Curve>();
            for (int i = 0; i < horizontalCorePointList.Count; i += 2)
            {
                if (i <= horizontalCorePointList.Count-2)
                {
                Rhino.Geometry.Line line = new Rhino.Geometry.Line(horizontalCorePointList[i], horizontalCorePointList[i + 1]);
                Curve curve = line.ToNurbsCurve();
                horizontalCoreCurve.Add(curve);
                }
            }

            return horizontalCoreCurve;
        }
        private void CreateAreaTypeTextBlock(List<double> exclusiveArea)
        {
            int count = 0;

            int previousTop = 780;
            int previousLeft = 640;
            System.Windows.Media.FontFamily family = new System.Windows.Media.FontFamily("NanumSquareOTF");
            for(int i = 0; i < exclusiveArea.Count; i++)
            {
                TextBlock textBlock = new TextBlock();
                textBlock.Text = Math.Round(exclusiveArea[i]/1000000,0).ToString() + "m\xB2";
                textBlock.FontSize = 18.667;
                textBlock.FontFamily = family;
                buildingCanvas.Children.Add(textBlock);
                Canvas.SetTop(textBlock, previousTop);
                Canvas.SetLeft(textBlock, previousLeft);
                count++;
                if (count > i)
                {
                    previousLeft += 60;
                }

            }
        }

        private List<Curve> CreateCorridor(List<Core> coreList)
        {
            Rectangle3d innerCorridor = new Rectangle3d(Plane.WorldXY ,coreList[0].Origin,coreList[1].Origin);
            List<Curve> curveRectangle = new List<Curve>();

     
            return curveRectangle;
        }
    }
}