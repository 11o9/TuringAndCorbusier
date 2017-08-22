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
        public floorPlanDrawingPage(Interval floorInterval, bool isUsing1F)
        {
            InitializeComponent();
            if (isUsing1F == true)
            {
                this.SetTitle2(floorInterval);
            }
            else
            {
                this.SetTitle(floorInterval);

            }
        }
        public floorPlanDrawingPage(Interval floorInterval, bool isUsing1F, bool isTopFloorDifferent, bool isTopSetBack)
        {
            InitializeComponent();

            if (isUsing1F == false && isTopFloorDifferent == true)
            {
                this.SetTitleIsTopDifferent(floorInterval);
            }
            else if (isUsing1F == true && isTopFloorDifferent == true)
            {
                if (isTopSetBack == false)
                {
                    this.SetTitleIsUsingAndTopDifferent(floorInterval);

                }
                else if (isTopSetBack == true)
                {
                    this.SetTitleIsTopDifferent(floorInterval);
                }
            }
            else if (isUsing1F == false && isTopFloorDifferent == false)
            {
                if (isTopSetBack == true)
                {
                    SetTitleIsTopDifferent(floorInterval);
                }
                else
                {
                    this.SetTitle(floorInterval);

                }
            }
            else if (isUsing1F == true && isTopFloorDifferent == false)
            {
                if (isTopSetBack == false)
                {
                    this.SetTitleIsUsingAndTopDifferent(floorInterval);
                }
                else if (isTopSetBack == true)
                {
                    this.SetTitleIsUsingAndTopDifferent(floorInterval);
                }
            }

        }

        public floorPlanDrawingPage(int number)
        {
            InitializeComponent();
            this.planPageTitle.Text = "PLAN 1F";
        }

        public floorPlanDrawingPage(double number, string lastFloor)
        {
            InitializeComponent();
            this.planPageTitle.Text = "PLAN " + (number - 1).ToString() + "F";
        }
        private void SetTitleIsTopDifferent(Interval floorInterval)
        {
            this.planPageTitle.Text = "PLAN " + 2 + "-" + (floorInterval.Max - 2).ToString() + "F";
        }
        private void SetTitleIsUsingAndTopDifferent(Interval floorInterval)
        {
            this.planPageTitle.Text = "PLAN " + 2 + "-" + (floorInterval.Max - 2).ToString() + "F";
        }
        private void SetTitle(Interval floorInterval)
        {
            this.planPageTitle.Text = "PLAN " + 2 + "-" + (floorInterval.Max - 1).ToString() + "F";

            //else
            //{
            //    this.planPageTitle.Text = "4." + floorInterval.Min.ToString() + "~" + floorInterval.Max.ToString() + "th Floor plan";
            //}
        }
        private void SetTitle2(Interval floorInterval)
        {
            this.planPageTitle.Text = "PLAN " + 1 + "-" + floorInterval.Max.ToString() + "F";

            //else
            //{
            //    this.planPageTitle.Text = "4." + floorInterval.Min.ToString() + "~" + floorInterval.Max.ToString() + "th Floor plan";
            //}
        }

        //private void SethouseAreaTypeColor(xmlUnitReport unitReport)
        //{
        //    string AreaType = unitReport.AreaType.Text;
        //}

        private static List<List<Curve>> DrawBalconyLine(Apartment apartment)
        {
            double aptWidth = apartment.ParameterSet.Parameters[2];
            List<List<Curve>> balconyLine = new List<List<Curve>>();
            List<Curve> aptLine = apartment.AptLines;
            List<List<Rhino.Geometry.Line>> lightingEdges = new List<List<Rhino.Geometry.Line>>();
            List<Household> householdLists = new List<Household>();
            foreach (var householdDoubleList in apartment.Household)
            {
                foreach (var householdList in householdDoubleList)
                {
                    foreach (var household in householdList)
                    {
                        List<Rhino.Geometry.Line> lineList = new List<Rhino.Geometry.Line>();
                        for (int i = 0; i < household.LightingEdge.Count; i++)
                        {
                            Rhino.Geometry.Line line = new Rhino.Geometry.Line(new Point3d(household.LightingEdge[i].FromX, household.LightingEdge[i].FromY, household.LightingEdge[i].FromZ), new Point3d(household.LightingEdge[i].ToX, household.LightingEdge[i].ToY, household.LightingEdge[i].ToZ));
                            lineList.Add(line);
                        }
                        householdLists.Add(household);
                        lightingEdges.Add(lineList);
                    }
                }
            }

            for (int i = 0; i < lightingEdges.Count; i++)
            {
                List<Curve> balconyLineList = new List<Curve>();
                for (int j = 0; j < lightingEdges[i].Count; j++)
                {
                    Rhino.Geometry.Line line = new Rhino.Geometry.Line(new Point3d(lightingEdges[i][j].FromX, lightingEdges[i][j].FromY, lightingEdges[i][j].FromZ), new Point3d(lightingEdges[i][j].ToX, lightingEdges[i][j].ToY, lightingEdges[i][j].ToZ));
                    Vector3d dir = line.UnitTangent;
                    dir.Rotate(Math.PI / 2, Vector3d.ZAxis);
                    line.Transform(Rhino.Geometry.Transform.Translation(dir * 10));
                    var pointContainment = householdLists[i].GetOutline().Contains(line.PointAt(0.5));
                    if (pointContainment == PointContainment.Outside)
                    {
                        dir.Rotate(Math.PI, Vector3d.ZAxis);
                        Rhino.Geometry.Line line1 = new Rhino.Geometry.Line(new Point3d(lightingEdges[i][j].FromX, lightingEdges[i][j].FromY, lightingEdges[i][j].FromZ), new Point3d(lightingEdges[i][j].ToX, lightingEdges[i][j].ToY, lightingEdges[i][j].ToZ));
                        line1.Transform(Rhino.Geometry.Transform.Translation(dir * 1500));
                        balconyLineList.Add(line1.ToNurbsCurve());
                    }
                    else
                    {
                        Rhino.Geometry.Line line1 = new Rhino.Geometry.Line(new Point3d(lightingEdges[i][j].FromX, lightingEdges[i][j].FromY, lightingEdges[i][j].FromZ), new Point3d(lightingEdges[i][j].ToX, lightingEdges[i][j].ToY, lightingEdges[i][j].ToZ));
                        line1.Transform(Rhino.Geometry.Transform.Translation(dir * 1500));
                        balconyLineList.Add(line1.ToNurbsCurve());
                    }
                }
                balconyLine.Add(balconyLineList);
            }
            return balconyLine;
        }

        private void AddAreaTypeToHouseOutline(System.Windows.Point newCentroid, string areaTypeInText)
        {
            double size = 10;
            TextBlock areaType = new TextBlock();
            areaType.Text = areaTypeInText;
            areaType.FontSize = size;
            typicalPlanCanvas.Children.Add(areaType);

            Canvas.SetLeft(areaType, newCentroid.X - (10));
            Canvas.SetTop(areaType, newCentroid.Y - (5));

        }

        private string ProcessExclusiveArea(double unRoundedExclusiveArea)
        {
            string roundedStringExclusiveArea = Math.Round(unRoundedExclusiveArea / 1000000, 0).ToString() + "m\xB2 ";
            return roundedStringExclusiveArea;
        }
        //타입마다 크기색상 지정
        //private System.Windows.Media.SolidColorBrush SetAreaTypeColor(List<double> distinctRoundedExclusiveArea, double actualRoundedExclusiveArea)
        //{
        //    System.Windows.Media.SolidColorBrush areaTypeColour = null;
        //    //set colors
        //    List<System.Windows.Media.SolidColorBrush> colorList = new List<System.Windows.Media.SolidColorBrush>();
        //    colorList.Add(System.Windows.Media.Brushes.Aquamarine);
        //    colorList.Add(System.Windows.Media.Brushes.Crimson);
        //    colorList.Add(System.Windows.Media.Brushes.LightGreen);
        //    colorList.Add(System.Windows.Media.Brushes.Snow);
        //    colorList.Add(System.Windows.Media.Brushes.Plum);
        //    colorList.Add(System.Windows.Media.Brushes.Gold);
        //    colorList.Add(System.Windows.Media.Brushes.Tomato);
        //    colorList.Add(System.Windows.Media.Brushes.Khaki);
        //    colorList.Add(System.Windows.Media.Brushes.Lavender);
        //    colorList.Add(System.Windows.Media.Brushes.LightSeaGreen);

        //        for (int j = 0; j < distinctRoundedExclusiveArea.Count; j++)
        //        {
        //            try
        //            {
        //            if(actualRoundedExclusiveArea == distinctRoundedExclusiveArea[j])
        //            {
        //                areaTypeColour = colorList[j];
        //            }
        //            }catch(Exception e){
        //            continue;
        //               // System.Windows.MessageBox.Show(e.Message);
        //            }
        //        }


        //    return areaTypeColour;
        //}


        //--------JHL
        public void SetTopHouseOutline(Apartment apartment, List<Curve> coreOutline, List<Curve> houseOutline, TypicalPlan typicalPlan, List<Household> householdList, int floor, List<double> numOfHouseInEachFloorList, List<Core> newCoreList)
        {

            List<List<Curve>> balconyLines = DrawBalconyLine(apartment);
            List<Curve> aptLineList = apartment.AptLines;
            double aptWidth = apartment.ParameterSet.Parameters[2];
            string agType = apartment.AGtype;

            Curve boundary = typicalPlan.Boundary;
            List<Curve> surroundingSite = typicalPlan.SurroundingSite;
            List<FloorPlan> housePlanList = typicalPlan.UnitPlans;
            List<Curve> corePlanList = coreOutline;
            List<FloorPlan> floorPlanList = typicalPlan.UnitPlans;
            List<Curve> houseOutlineList = houseOutline;
            List<Point3d> houseOutlinesCentroid = new List<Point3d>();
            Rectangle3d rectangleToFit = new Rectangle3d(Plane.WorldXY, typicalPlan.GetBoundingBox().Min, typicalPlan.GetBoundingBox().Max);
            Rectangle canvasRectangle = new Rectangle();
            canvasRectangle.Width = typicalPlanCanvas.Width;
            canvasRectangle.Height = typicalPlanCanvas.Height;
            System.Windows.Point initialOriginPoint = new System.Windows.Point();
            double scaleFactor = PlanDrawingFunction_90degree.scaleToFitFactor(canvasRectangle, rectangleToFit, out initialOriginPoint);

            System.Windows.Media.SolidColorBrush SCBGray = new SolidColorBrush();
            SCBGray.Color = System.Windows.Media.Color.FromRgb(225, 225, 225);

            System.Windows.Media.SolidColorBrush SCBCorridor = new SolidColorBrush();
            SCBCorridor.Color = System.Windows.Media.Color.FromRgb(242, 242, 242);

            System.Windows.Media.SolidColorBrush SCBLime = new SolidColorBrush();
            SCBLime.Color = System.Windows.Media.Color.FromRgb(200, 229, 13);

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
                if (coreType[i] == CoreType.Folded)
                {
                    if (newCoreList[i].Depth != 5800 && newCoreList[i].Width != 6060)
                    {
                        pointString = GetSimplifiedCoreString(CoreType.CourtShortEdge);
                        coreType[i] = CoreType.CourtShortEdge;
                    }
                }
                else if (coreType[i] == CoreType.CourtShortEdge)
                {
                    if (newCoreList[i].depth == 2700 && newCoreList[i].width != 7600)
                    {
                        pointString = GetSimplifiedCoreString(CoreType.CourtShortEdge2);
                        coreType[i] = CoreType.CourtShortEdge2;
                    }
                }

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

            RotateToFit(ref coreDetailDoubleList, newCoreList, coreType, coreOutline);


            PlanDrawingFunction_90degree.drawPlan(rectangleToFit, surroundingSite, scaleFactor, initialOriginPoint, ref this.typicalPlanCanvas, System.Windows.Media.Brushes.Black, 1);
            PlanDrawingFunction_90degree.drawBoundaryPlan(rectangleToFit, boundary, scaleFactor, initialOriginPoint, ref this.typicalPlanCanvas, System.Windows.Media.Brushes.Red, 5);


            if (agType == "PT-3")
            {
                Curve corridorOutline;
                Curve innerOutline;
                DrawPT3Corridor(coreType, aptLineList, agType, aptWidth, out corridorOutline, out innerOutline);
                if (corridorOutline != null && innerOutline != null)
                {

                    PlanDrawingFunction_90degree.drawBackGround(rectangleToFit, innerOutline, scaleFactor, initialOriginPoint, ref this.typicalPlanCanvas, SCBCorridor);
                    PlanDrawingFunction_90degree.drawBackGround(rectangleToFit, corridorOutline, scaleFactor, initialOriginPoint, ref this.typicalPlanCanvas, System.Windows.Media.Brushes.White);
                    PlanDrawingFunction_90degree.drawPlan(rectangleToFit, corridorOutline, scaleFactor, initialOriginPoint, ref this.typicalPlanCanvas, System.Windows.Media.Brushes.Black, 0.75);
                }
            }

            if (agType == "PT-1")
            {
                List<Curve> corridor = DrawPT1Corridor(apartment);
                for (int i = 0; i < corridor.Count; i++)
                {
                    PlanDrawingFunction_90degree.drawBackGround(rectangleToFit, corridor[i], scaleFactor, initialOriginPoint, ref this.typicalPlanCanvas, SCBCorridor);
                    PlanDrawingFunction_90degree.drawPlan(rectangleToFit, corridor[i], scaleFactor, initialOriginPoint, ref this.typicalPlanCanvas, System.Windows.Media.Brushes.Black, 0.75);
                }
            }

            //세대 타입 구하기
            List<string> roundedStringExclusiveAreaList = new List<string>();
            //List<double> distinctRoundedExclusiveArea = new List<double>();
            foreach (Household household in householdList)
            {
                roundedStringExclusiveAreaList.Add(ProcessExclusiveArea(household.GetExclusiveArea()));
                //    distinctRoundedExclusiveArea.Add(Math.Round(household.GetExclusiveArea() / 1000000, 0));
            }
            //List<double> testing = distinctRoundedExclusiveArea.Distinct().ToList();



            try
            {
                for (int i = 0; i < houseOutlineList.Count; i++)
                {
                    //double actualRoundExclusiveArea = Math.Round(householdList[i].GetExclusiveArea() / 1000000, 0);
                    //System.Windows.Media.SolidColorBrush areaTypeBackgroundColour = SetAreaTypeColor(distinctRoundedExclusiveArea, actualRoundExclusiveArea);
                    PlanDrawingFunction_90degree.drawPlan(rectangleToFit, houseOutlineList[i], scaleFactor, initialOriginPoint, ref this.typicalPlanCanvas, System.Windows.Media.Brushes.Black, 3);
                    //PlanDrawingFunction_90degree.drawBackGround(rectangleToFit, houseOutlineList[i], scaleFactor, initialOriginPoint, ref this.typicalPlanCanvas, areaTypeBackgroundColour);

                    PlanDrawingFunction_90degree.drawBackGround(rectangleToFit, houseOutlineList[i], scaleFactor, initialOriginPoint, ref this.typicalPlanCanvas, SCBGray);
                }

                for (int i = houseOutlineList.Count - 1; i > (houseOutlineList.Count - 1) - (int)numOfHouseInEachFloorList.Last(); i--)
                {
                    //double actualRoundExclusiveArea = Math.Round(householdList[i].GetExclusiveArea() / 1000000, 0);
                    //System.Windows.Media.SolidColorBrush areaTypeBackgroundColour = SetAreaTypeColor(distinctRoundedExclusiveArea, actualRoundExclusiveArea);
                    PlanDrawingFunction_90degree.drawPlan(rectangleToFit, houseOutlineList[i], scaleFactor, initialOriginPoint, ref this.typicalPlanCanvas, System.Windows.Media.Brushes.Black, 3);
                    //PlanDrawingFunction_90degree.drawBackGround(rectangleToFit, houseOutlineList[i], scaleFactor, initialOriginPoint, ref this.typicalPlanCanvas, areaTypeBackgroundColour);
                    PlanDrawingFunction_90degree.drawHouseBackGroundPlan(rectangleToFit, houseOutlineList[i], scaleFactor, initialOriginPoint, ref this.typicalPlanCanvas, SCBLime, 0);

                    if (balconyLines[i].Count > 1)
                    {
                        PlanDrawingFunction_90degree.drawDashedPlan(rectangleToFit, balconyLines[i][1], scaleFactor, initialOriginPoint, ref this.typicalPlanCanvas, System.Windows.Media.Brushes.Black, 0.075);

                    }
                    else
                    {
                        PlanDrawingFunction_90degree.drawDashedPlan(rectangleToFit, balconyLines[i][0], scaleFactor, initialOriginPoint, ref this.typicalPlanCanvas, System.Windows.Media.Brushes.Black, 0.075);
                    }
                }



                //for (int i = 0; i < balconyLines.Count; i++)
                //{
                //    if (balconyLines[i].Count > 1)
                //    {
                //        PlanDrawingFunction_90degree.drawDashedPlan(rectangleToFit, balconyLines[i][1], scaleFactor, initialOriginPoint, ref this.typicalPlanCanvas, System.Windows.Media.Brushes.Black, 0.075);

                //    }
                //    else
                //    {
                //        PlanDrawingFunction_90degree.drawDashedPlan(rectangleToFit, balconyLines[i][0], scaleFactor, initialOriginPoint, ref this.typicalPlanCanvas, System.Windows.Media.Brushes.Black, 0.075);
                //    }

                //}

                for (int i = 0; i < corePlanList.Count; i++)
                {
                    PlanDrawingFunction_90degree.drawPlan(rectangleToFit, corePlanList[i], scaleFactor, initialOriginPoint, ref this.typicalPlanCanvas, System.Windows.Media.Brushes.Black, 2);
                    PlanDrawingFunction_90degree.drawBackGround(rectangleToFit, corePlanList[i], scaleFactor, initialOriginPoint, ref this.typicalPlanCanvas, SCBGray);
                    PlanDrawingFunction_90degree.drawPlan(rectangleToFit, coreDetailDoubleList[i], scaleFactor, initialOriginPoint, ref this.typicalPlanCanvas, System.Windows.Media.Brushes.Black, 0.5);
                }


                //foreach (FloorPlan floorPlan in floorPlanList)
                //{
                //    //PlanDrawingFunction_90degree.drawPlan(rectangleToFit, floorPlan.balconyLines, scaleFactor, initialOriginPoint, ref this.typicalPlanCanvas, System.Windows.Media.Brushes.Black, 0.075);
                //}
                PlanDrawingFunction_90degree.drawPlan(rectangleToFit, typicalPlan.OutLine.ToNurbsCurve(), scaleFactor, initialOriginPoint, ref this.typicalPlanCanvas, System.Windows.Media.Brushes.Black, 1);
            }
            catch (Exception e)
            {
                System.Windows.MessageBox.Show(e.Message);
            }



            //JHL 글씨 넣기 위해 중심 점 구함 

            for (int i = 0; i < houseOutlineList.Count; i++)
            {
                houseOutlinesCentroid.Add(Rhino.Geometry.AreaMassProperties.Compute(houseOutlineList[i]).Centroid);
            }

            for (int i = houseOutlinesCentroid.Count - 1; i > (houseOutlinesCentroid.Count - 1) - (int)numOfHouseInEachFloorList.Last(); i--)
            {
                System.Windows.Point newCentroid = PlanDrawingFunction_90degree.pointConverter(rectangleToFit, houseOutlinesCentroid[i], scaleFactor, initialOriginPoint);
                try
                {
                    this.AddAreaTypeToHouseOutline(newCentroid, roundedStringExclusiveAreaList[i]);

                }
                catch (Exception e)
                {
                    continue;
                }
            }
        }

        public void SetFirstFloorHouseOutline(Apartment apartment, List<Curve> coreOutline, List<Curve> houseOutline, TypicalPlan typicalPlan, List<Household> householdList, int numOfFloors, List<double> numOfHouseList, List<Core> newCoreList)
        {
            System.Windows.Media.SolidColorBrush SCBGray = new SolidColorBrush();
            SCBGray.Color = System.Windows.Media.Color.FromRgb(225, 225, 225);
            System.Windows.Media.SolidColorBrush SCBCorridor = new SolidColorBrush();
            SCBCorridor.Color = System.Windows.Media.Color.FromRgb(242, 242, 242);
            System.Windows.Media.SolidColorBrush SCBLime = new SolidColorBrush();
            SCBLime.Color = System.Windows.Media.Color.FromRgb(200, 229, 13);

            List<List<Curve>> balconyLines = DrawBalconyLine(apartment);
            Curve boundary = typicalPlan.Boundary;
            List<Curve> surroundingSite = typicalPlan.SurroundingSite;
            List<FloorPlan> housePlanList = typicalPlan.UnitPlans;
            List<Curve> corePlanList = coreOutline;
            List<FloorPlan> floorPlanList = typicalPlan.UnitPlans;

            List<Curve> houseOutlineList = houseOutline;
            List<Point3d> houseOutlinesCentroid = new List<Point3d>();
            Rectangle3d rectangleToFit = new Rectangle3d(Plane.WorldXY, typicalPlan.GetBoundingBox().Min, typicalPlan.GetBoundingBox().Max);
            Rectangle canvasRectangle = new Rectangle();
            canvasRectangle.Width = typicalPlanCanvas.Width;
            canvasRectangle.Height = typicalPlanCanvas.Height;
            System.Windows.Point initialOriginPoint = new System.Windows.Point();
            double scaleFactor = PlanDrawingFunction_90degree.scaleToFitFactor(canvasRectangle, rectangleToFit, out initialOriginPoint);
            List<Curve> aptLineList = apartment.AptLines;
            double aptWidth = apartment.ParameterSet.Parameters[2];
            string agType = apartment.AGtype;

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
                if (coreType[i] == CoreType.Folded)
                {
                    if (newCoreList[i].Depth != 5800 && newCoreList[i].Width != 6060)
                    {
                        pointString = GetSimplifiedCoreString(CoreType.CourtShortEdge);
                        coreType[i] = CoreType.CourtShortEdge;
                    }
                }
                else if (coreType[i] == CoreType.CourtShortEdge)
                {
                    if (newCoreList[i].depth == 2700 && newCoreList[i].width != 7600)
                    {
                        pointString = GetSimplifiedCoreString(CoreType.CourtShortEdge2);
                        coreType[i] = CoreType.CourtShortEdge2;
                    }
                }

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

            RotateToFit(ref coreDetailDoubleList, newCoreList, coreType, coreOutline);


            PlanDrawingFunction_90degree.drawPlan(rectangleToFit, surroundingSite, scaleFactor, initialOriginPoint, ref this.typicalPlanCanvas, System.Windows.Media.Brushes.Black, 1);
            PlanDrawingFunction_90degree.drawBoundaryPlan(rectangleToFit, boundary, scaleFactor, initialOriginPoint, ref this.typicalPlanCanvas, System.Windows.Media.Brushes.Red, 5);


            //if (agType == "PT-3")
            //{
            //    Curve corridorOutline;
            //    Curve innerOutline;
            //    DrawPT3Corridor(coreType, aptLineList, agType, aptWidth, out corridorOutline, out innerOutline);
            //    if (corridorOutline != null && innerOutline != null)
            //    {

            //        PlanDrawingFunction_90degree.drawBackGround(rectangleToFit, innerOutline, scaleFactor, initialOriginPoint, ref this.typicalPlanCanvas, SCBCorridor);
            //        PlanDrawingFunction_90degree.drawBackGround(rectangleToFit, corridorOutline, scaleFactor, initialOriginPoint, ref this.typicalPlanCanvas, System.Windows.Media.Brushes.White);
            //        PlanDrawingFunction_90degree.drawPlan(rectangleToFit, corridorOutline, scaleFactor, initialOriginPoint, ref this.typicalPlanCanvas, System.Windows.Media.Brushes.Black, 0.75);
            //    }
            //}

            //if (agType == "PT-1")
            //{
            //    List<Curve> corridor = DrawPT1Corridor(apartment);
            //    for (int i = 0; i < corridor.Count; i++)
            //    {
            //        PlanDrawingFunction_90degree.drawBackGround(rectangleToFit, corridor[i], scaleFactor, initialOriginPoint, ref this.typicalPlanCanvas, SCBCorridor);
            //        PlanDrawingFunction_90degree.drawPlan(rectangleToFit, corridor[i], scaleFactor, initialOriginPoint, ref this.typicalPlanCanvas, System.Windows.Media.Brushes.Black, 0.75);
            //    }
            //}


            //세대 타입 구하기
            List<string> roundedStringExclusiveAreaList = new List<string>();
            //List<double> distinctRoundedExclusiveArea = new List<double>();
            foreach (Household household in householdList)
            {
                roundedStringExclusiveAreaList.Add(ProcessExclusiveArea(household.GetExclusiveArea()));
                //    distinctRoundedExclusiveArea.Add(Math.Round(household.GetExclusiveArea() / 1000000, 0));
            }
            //List<double> testing = distinctRoundedExclusiveArea.Distinct().ToList();

            try
            {

                for (int i = 0; i < numOfHouseList[0]; i++)
                {
                    //double actualRoundExclusiveArea = Math.Round(householdList[i].GetExclusiveArea() / 1000000, 0);
                    //System.Windows.Media.SolidColorBrush areaTypeBackgroundColour = SetAreaTypeColor(distinctRoundedExclusiveArea, actualRoundExclusiveArea);
                    PlanDrawingFunction_90degree.drawPlan(rectangleToFit, houseOutlineList[i], scaleFactor, initialOriginPoint, ref this.typicalPlanCanvas, System.Windows.Media.Brushes.Black, 3);
                    //PlanDrawingFunction_90degree.drawBackGround(rectangleToFit, houseOutlineList[i], scaleFactor, initialOriginPoint, ref this.typicalPlanCanvas, areaTypeBackgroundColour);
                    PlanDrawingFunction_90degree.drawHouseBackGroundPlan(rectangleToFit, houseOutlineList[i], scaleFactor, initialOriginPoint, ref this.typicalPlanCanvas, SCBLime, 0);

                }


                for (int i = 0; i < numOfHouseList[0]; i++)
                {
                    if (balconyLines[i].Count > 1)
                    {
                        PlanDrawingFunction_90degree.drawDashedPlan(rectangleToFit, balconyLines[i][1], scaleFactor, initialOriginPoint, ref this.typicalPlanCanvas, System.Windows.Media.Brushes.Black, 0.075);

                    }
                    else
                    {
                        PlanDrawingFunction_90degree.drawDashedPlan(rectangleToFit, balconyLines[i][0], scaleFactor, initialOriginPoint, ref this.typicalPlanCanvas, System.Windows.Media.Brushes.Black, 0.075);
                    }

                }

                for (int i = 0; i < corePlanList.Count; i++)
                {
                    PlanDrawingFunction_90degree.drawPlan(rectangleToFit, corePlanList[i], scaleFactor, initialOriginPoint, ref this.typicalPlanCanvas, System.Windows.Media.Brushes.Black, 2);
                    PlanDrawingFunction_90degree.drawBackGround(rectangleToFit, corePlanList[i], scaleFactor, initialOriginPoint, ref this.typicalPlanCanvas, SCBGray);
                    PlanDrawingFunction_90degree.drawPlan(rectangleToFit, coreDetailDoubleList[i], scaleFactor, initialOriginPoint, ref this.typicalPlanCanvas, System.Windows.Media.Brushes.Black, 0.5);

                }
            }
            catch (Exception e)
            {
                System.Windows.MessageBox.Show(e.Message);
            }

            //JHL 글씨 넣기 위해 중심 점 구함 
            foreach (Curve house in houseOutlineList)
            {
                houseOutlinesCentroid.Add(Rhino.Geometry.AreaMassProperties.Compute(house).Centroid);
            }
            for (int i = 0; i < numOfHouseList[numOfFloors]; i++)
            {
                System.Windows.Point newCentroid = PlanDrawingFunction_90degree.pointConverter(rectangleToFit, houseOutlinesCentroid[i], scaleFactor, initialOriginPoint);
                try
                {
                    this.AddAreaTypeToHouseOutline(newCentroid, roundedStringExclusiveAreaList[i]);

                }
                catch (Exception e)
                {
                    continue;
                }
            }

            PlanDrawingFunction_90degree.drawPlan(rectangleToFit, typicalPlan.OutLine.ToNurbsCurve(), scaleFactor, initialOriginPoint, ref this.typicalPlanCanvas, System.Windows.Media.Brushes.Black, 1);

        }

        //--------JHL
        public void SetHouseOutline(Apartment apartment, List<Curve> coreOutline, List<Curve> houseOutline, TypicalPlan typicalPlan, List<Household> householdList, int numOfFloors, List<double> numOfHouseList, List<Core> newCoreList)
        {

            System.Windows.Media.SolidColorBrush SCBGray = new SolidColorBrush();
            SCBGray.Color = System.Windows.Media.Color.FromRgb(225, 225, 225);

            System.Windows.Media.SolidColorBrush SCBCorridor = new SolidColorBrush();
            SCBCorridor.Color = System.Windows.Media.Color.FromRgb(242, 242, 242);

            System.Windows.Media.SolidColorBrush SCBLime = new SolidColorBrush();
            SCBLime.Color = System.Windows.Media.Color.FromRgb(200, 229, 13);

            List<List<Curve>> balconyLines = DrawBalconyLine(apartment);
            Curve boundary = typicalPlan.Boundary;
            List<Curve> surroundingSite = typicalPlan.SurroundingSite;
            List<FloorPlan> housePlanList = typicalPlan.UnitPlans;
            List<Curve> corePlanList = coreOutline;
            List<FloorPlan> floorPlanList = typicalPlan.UnitPlans;

            List<Curve> houseOutlineList = houseOutline;
            List<Point3d> houseOutlinesCentroid = new List<Point3d>();
            Rectangle3d rectangleToFit = new Rectangle3d(Plane.WorldXY, typicalPlan.GetBoundingBox().Min, typicalPlan.GetBoundingBox().Max);
            Rectangle canvasRectangle = new Rectangle();
            canvasRectangle.Width = typicalPlanCanvas.Width;
            canvasRectangle.Height = typicalPlanCanvas.Height;
            System.Windows.Point initialOriginPoint = new System.Windows.Point();
            double scaleFactor = PlanDrawingFunction_90degree.scaleToFitFactor(canvasRectangle, rectangleToFit, out initialOriginPoint);
            List<Curve> aptLineList = apartment.AptLines;
            double aptWidth = apartment.ParameterSet.Parameters[2];
            string agType = apartment.AGtype;

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
                if (coreType[i] == CoreType.Folded)
                {
                    if (newCoreList[i].Depth != 5800 && newCoreList[i].Width != 6060)
                    {
                        pointString = GetSimplifiedCoreString(CoreType.CourtShortEdge);
                        coreType[i] = CoreType.CourtShortEdge;
                    }
                }
                else if (coreType[i] == CoreType.CourtShortEdge)
                {
                    if (newCoreList[i].depth == 2700 && newCoreList[i].width != 7600)
                    {
                        pointString = GetSimplifiedCoreString(CoreType.CourtShortEdge2);
                        coreType[i] = CoreType.CourtShortEdge2;
                    }
                }

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

            RotateToFit(ref coreDetailDoubleList, newCoreList, coreType, coreOutline);


            PlanDrawingFunction_90degree.drawPlan(rectangleToFit, surroundingSite, scaleFactor, initialOriginPoint, ref this.typicalPlanCanvas, System.Windows.Media.Brushes.Black, 1);
            PlanDrawingFunction_90degree.drawBoundaryPlan(rectangleToFit, boundary, scaleFactor, initialOriginPoint, ref this.typicalPlanCanvas, System.Windows.Media.Brushes.Red, 5);


            if (agType == "PT-3")
            {
                Curve corridorOutline;
                Curve innerOutline;
                DrawPT3Corridor(coreType, aptLineList, agType, aptWidth, out corridorOutline, out innerOutline);
                if (corridorOutline != null && innerOutline != null)
                {

                    PlanDrawingFunction_90degree.drawBackGround(rectangleToFit, innerOutline, scaleFactor, initialOriginPoint, ref this.typicalPlanCanvas, SCBCorridor);
                    PlanDrawingFunction_90degree.drawBackGround(rectangleToFit, corridorOutline, scaleFactor, initialOriginPoint, ref this.typicalPlanCanvas, System.Windows.Media.Brushes.White);
                    PlanDrawingFunction_90degree.drawPlan(rectangleToFit, corridorOutline, scaleFactor, initialOriginPoint, ref this.typicalPlanCanvas, System.Windows.Media.Brushes.Black, 0.75);
                }
            }

            if (agType == "PT-1")
            {
                List<Curve> corridor = DrawPT1Corridor(apartment);
                for (int i = 0; i < corridor.Count; i++)
                {
                    PlanDrawingFunction_90degree.drawBackGround(rectangleToFit, corridor[i], scaleFactor, initialOriginPoint, ref this.typicalPlanCanvas, SCBCorridor);
                    PlanDrawingFunction_90degree.drawPlan(rectangleToFit, corridor[i], scaleFactor, initialOriginPoint, ref this.typicalPlanCanvas, System.Windows.Media.Brushes.Black, 0.75);
                }
            }


            //세대 타입 구하기
            List<string> roundedStringExclusiveAreaList = new List<string>();
            //List<double> distinctRoundedExclusiveArea = new List<double>();
            foreach (Household household in householdList)
            {
                roundedStringExclusiveAreaList.Add(ProcessExclusiveArea(household.GetExclusiveArea()));
                //    distinctRoundedExclusiveArea.Add(Math.Round(household.GetExclusiveArea() / 1000000, 0));
            }
            //List<double> testing = distinctRoundedExclusiveArea.Distinct().ToList();

            try
            {

                for (int i = (int)numOfHouseList[1]; i < numOfHouseList[1] + numOfHouseList[numOfFloors]; i++)
                {
                    //double actualRoundExclusiveArea = Math.Round(householdList[i].GetExclusiveArea() / 1000000, 0);
                    //System.Windows.Media.SolidColorBrush areaTypeBackgroundColour = SetAreaTypeColor(distinctRoundedExclusiveArea, actualRoundExclusiveArea);
                    PlanDrawingFunction_90degree.drawPlan(rectangleToFit, houseOutlineList[i], scaleFactor, initialOriginPoint, ref this.typicalPlanCanvas, System.Windows.Media.Brushes.Black, 3);
                    //PlanDrawingFunction_90degree.drawBackGround(rectangleToFit, houseOutlineList[i], scaleFactor, initialOriginPoint, ref this.typicalPlanCanvas, areaTypeBackgroundColour);
                    PlanDrawingFunction_90degree.drawHouseBackGroundPlan(rectangleToFit, houseOutlineList[i], scaleFactor, initialOriginPoint, ref this.typicalPlanCanvas, SCBLime, 0);

                }


                for (int i = 0; i < balconyLines.Count - (int)numOfHouseList.Last(); i++)
                {
                    if (balconyLines[i].Count > 1)
                    {
                        PlanDrawingFunction_90degree.drawDashedPlan(rectangleToFit, balconyLines[i][1], scaleFactor, initialOriginPoint, ref this.typicalPlanCanvas, System.Windows.Media.Brushes.Black, 0.075);

                    }
                    else
                    {
                        PlanDrawingFunction_90degree.drawDashedPlan(rectangleToFit, balconyLines[i][0], scaleFactor, initialOriginPoint, ref this.typicalPlanCanvas, System.Windows.Media.Brushes.Black, 0.075);
                    }

                }

                for (int i = 0; i < corePlanList.Count; i++)
                {
                    PlanDrawingFunction_90degree.drawPlan(rectangleToFit, corePlanList[i], scaleFactor, initialOriginPoint, ref this.typicalPlanCanvas, System.Windows.Media.Brushes.Black, 2);
                    PlanDrawingFunction_90degree.drawBackGround(rectangleToFit, corePlanList[i], scaleFactor, initialOriginPoint, ref this.typicalPlanCanvas, SCBGray);
                    PlanDrawingFunction_90degree.drawPlan(rectangleToFit, coreDetailDoubleList[i], scaleFactor, initialOriginPoint, ref this.typicalPlanCanvas, System.Windows.Media.Brushes.Black, 0.5);

                }


                //foreach (FloorPlan floorPlan in floorPlanList)
                //{
                //    //PlanDrawingFunction_90degree.drawPlan(rectangleToFit, floorPlan.balconyLines, scaleFactor, initialOriginPoint, ref this.typicalPlanCanvas, System.Windows.Media.Brushes.Black, 0.075);
                //}

            }
            catch (Exception e)
            {
                System.Windows.MessageBox.Show(e.Message);
            }

            //JHL 글씨 넣기 위해 중심 점 구함 
            foreach (Curve house in houseOutlineList)
            {
                houseOutlinesCentroid.Add(Rhino.Geometry.AreaMassProperties.Compute(house).Centroid);
            }
            for (int i = (int)numOfHouseList[numOfFloors]; i < houseOutlineList.Count - numOfHouseList.Last(); i++)
            {
                System.Windows.Point newCentroid = PlanDrawingFunction_90degree.pointConverter(rectangleToFit, houseOutlinesCentroid[i], scaleFactor, initialOriginPoint);
                try
                {
                    this.AddAreaTypeToHouseOutline(newCentroid, roundedStringExclusiveAreaList[i]);

                }
                catch (Exception e)
                {
                    continue;
                }
            }

            PlanDrawingFunction_90degree.drawPlan(rectangleToFit, typicalPlan.OutLine.ToNurbsCurve(), scaleFactor, initialOriginPoint, ref this.typicalPlanCanvas, System.Windows.Media.Brushes.Black, 1);
        }

        public void SetCoreOutline(List<double> numOfHouseEachFloor, Apartment apartment, List<Curve> coreOutline, List<Curve> houseOutline, TypicalPlan typicalPlan, Interval floor, List<Core> newCoreList)
        {

            List<Curve> aptLineList = apartment.AptLines;
            double aptWidth = apartment.ParameterSet.Parameters[2];
            string agType = apartment.AGtype;

            System.Windows.Media.SolidColorBrush SCBGray = new SolidColorBrush();
            SCBGray.Color = System.Windows.Media.Color.FromRgb(225, 225, 225);
            Curve boundary = typicalPlan.Boundary;
            List<Curve> parkinglineList = typicalPlan.ParkingLines;
            List<Curve> surroundingSite = typicalPlan.SurroundingSite;
            List<FloorPlan> housePlanList = typicalPlan.UnitPlans;
            List<Curve> corePlanList = coreOutline;
            List<FloorPlan> floorPlanList = typicalPlan.UnitPlans;
            List<Curve> houseOutlineList = houseOutline;
            Rectangle3d rectangleToFit = new Rectangle3d(Plane.WorldXY, typicalPlan.GetBoundingBox().Min, typicalPlan.GetBoundingBox().Max);

            Rectangle canvasRectangle = new Rectangle();
            canvasRectangle.Width = typicalPlanCanvas.Width;
            canvasRectangle.Height = typicalPlanCanvas.Height;
            System.Windows.Point initialOriginPoint = new System.Windows.Point();
            double scaleFactor = PlanDrawingFunction_90degree.scaleToFitFactor(canvasRectangle, rectangleToFit, out initialOriginPoint);


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
                if (coreType[i] == CoreType.Folded)
                {
                    if (newCoreList[i].Depth != 5800 && newCoreList[i].Width != 6060)
                    {
                        pointString = GetSimplifiedCoreString(CoreType.CourtShortEdge);
                        coreType[i] = CoreType.CourtShortEdge;
                    }
                }
                else if (coreType[i] == CoreType.CourtShortEdge)
                {
                    if (newCoreList[i].depth == 2700 && newCoreList[i].width != 7600)
                    {
                        pointString = GetSimplifiedCoreString(CoreType.CourtShortEdge2);
                        coreType[i] = CoreType.CourtShortEdge2;
                    }
                }

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

            RotateToFit(ref coreDetailDoubleList, newCoreList, coreType, coreOutline);



            PlanDrawingFunction_90degree.drawPlan(rectangleToFit, surroundingSite, scaleFactor, initialOriginPoint, ref this.typicalPlanCanvas, System.Windows.Media.Brushes.Black, 1);
            PlanDrawingFunction_90degree.drawBoundaryPlan(rectangleToFit, boundary, scaleFactor, initialOriginPoint, ref this.typicalPlanCanvas, System.Windows.Media.Brushes.Red, 5);


            if (agType == "PT-3")
            {
                Curve corridorOutline;
                Curve innerOutline;
                DrawPT3Corridor(coreType, aptLineList, agType, aptWidth, out corridorOutline, out innerOutline);
                if (corridorOutline != null && innerOutline != null)
                {

                    PlanDrawingFunction_90degree.drawDashedPlan(rectangleToFit, corridorOutline, scaleFactor, initialOriginPoint, ref this.typicalPlanCanvas, System.Windows.Media.Brushes.LightGray, 0.075);
                }
            }

            if (agType == "PT-1")
            {
                List<Curve> corridor = DrawPT1Corridor(apartment);
                for (int i = 0; i < corridor.Count; i++)
                {
                    PlanDrawingFunction_90degree.drawDashedPlan(rectangleToFit, corridor[i], scaleFactor, initialOriginPoint, ref this.typicalPlanCanvas, System.Windows.Media.Brushes.LightGray, 0.075);
                }
            }

            //draw houseoutline with dashlines
            List<Curve> validOutline = new List<Curve>();
            for (int i = (int)numOfHouseEachFloor[0]; i < numOfHouseEachFloor[0] + numOfHouseEachFloor[1]; i++)
            {
                validOutline.Add(houseOutlineList[i]);

            }
            List<Curve> validHouseOutline = Rhino.Geometry.Curve.CreateBooleanUnion(validOutline).ToList();
            for (int i = 0; i < validHouseOutline.Count; i++)
            {
                PlanDrawingFunction_90degree.drawDashedPlan(rectangleToFit, validHouseOutline[i], scaleFactor, initialOriginPoint, ref this.typicalPlanCanvas, System.Windows.Media.Brushes.LightGray, 0.075);
            }


            foreach (Curve parkingLine in parkinglineList)
            {
                PlanDrawingFunction_90degree.drawPlan(rectangleToFit, parkingLine, scaleFactor, initialOriginPoint, ref this.typicalPlanCanvas, System.Windows.Media.Brushes.Black, 0.075);
            }

            for (int i = 0; i < corePlanList.Count; i++)
            {
                PlanDrawingFunction_90degree.drawPlan(rectangleToFit, corePlanList[i], scaleFactor, initialOriginPoint, ref this.typicalPlanCanvas, System.Windows.Media.Brushes.Black, 2);
                PlanDrawingFunction_90degree.drawBackGround(rectangleToFit, corePlanList[i], scaleFactor, initialOriginPoint, ref this.typicalPlanCanvas, SCBGray);
                PlanDrawingFunction_90degree.drawPlan(rectangleToFit, coreDetailDoubleList[i], scaleFactor, initialOriginPoint, ref this.typicalPlanCanvas, System.Windows.Media.Brushes.Black, 0.5);

            }



            PlanDrawingFunction_90degree.drawPlan(rectangleToFit, typicalPlan.OutLine.ToNurbsCurve(), scaleFactor, initialOriginPoint, ref this.typicalPlanCanvas, System.Windows.Media.Brushes.Black, 1);
        }
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
                        double radian = Vector3d.VectorAngle(v2, v1, Plane.WorldXY);

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
                else if (coreType[i] == CoreType.Folded)
                {

                    Vector3d v1 = coreList[i].YDirection;
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
                        coreDetail[i][j].Transform(Rhino.Geometry.Transform.Translation(coreList[i].YDirection * coreList[i].depth));

                        //var longestCoreDetail = (from crv in coreDetail[i][j] let len = crv.GetLength() where len > 0 orderby len descending select crv).First();
                        //Point3d segmentMidPoint2 = longestCoreDetail.PointAtLength(longestCoreDetail.GetLength()/2);
                        //Vector3d v3 = new Vector3d(segmentMidPoint2 - segmentMidPoint1);


                        //coreDetail[i][j].Transform(Rhino.Geometry.Transform.Translation(v3));
                    }

                }
                else if (coreType[i] == CoreType.CourtShortEdge2)
                {

                    Vector3d v1 = coreList[i].YDirection;
                    Vector3d v2 = new Vector3d(coreDetail[i][0].PointAtStart - coreDetail[i][0].PointAtEnd);

                    for (int j = 0; j < coreDetail[i].Count; j++)
                    {
                        double radian = Vector3d.VectorAngle(v2, v1, Plane.WorldXY);
                        coreDetail[i][j].Transform(Rhino.Geometry.Transform.Rotation(radian, coreList[i].Origin));
                        coreDetail[i][j].Transform(Rhino.Geometry.Transform.Translation(coreList[i].YDirection * coreList[i].depth));

                    }

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
                return "{4860,0,0}/{4860,-5200,0}/{4860,-5200,0}/{0,-5200,0}/{0,-5200,0}/{0,0,0}/{0,0,0}/{4860,0,0}/{3540,-1300,0}/{1300,-1300,0}/{1300,-2600,0}/{3540,-2600,0}/{3540,-2600,0}/{3540,0,0}/{3260,-2600,0}/{3260,0,0}/{2980,-2600,0}/{2980,0,0}/{2700,-2600,0}/{2700,0,0}/{2420,-2600,0}/{2420,0,0}/{2140,-2600,0}/{2140,0,0}/{1860,-2600,0}/{1860,0,0}/{1580,-2600,0}/{1580,0,0}/{1300,-2600,0}/{1300,0,0}/{3540,-2600,0}/{4860,-2600,0}/{1300,-5200,0}/{1300,-2600,0}/{3900,-5200,0}/{3900,-2600,0}/{3900,-5200,0}/{4860,-2600,0}/{3900,-2600,0}/{4860,-5200,0}/{1300,-5200,0}/{3900,-2600,0}/{1300,-2600,0}/{3900,-5200,0}";
            }
            else if (coreType == CoreType.Vertical)
            {
                return "{0,0,0}/{-2500,0,0}/{-7600,-2500,0}/{-4900,-2500,0}/{-9000,-2500,0}/{-7600,-2500,0}/{0,0,0}/{-9000,0,0}/{-9000,0,0}/{-9000,-2500,0}/{-9000,-2500,0}/{-7600,-2500,0}/{-7600,-2500,0}/{-4900,-2500,0}/{-4900,-2500,0}/{-2500,-2500,0}/{-2500,-2500,0}/{0,-2500,0}/{0,-2500,0}/{0,0,0}/{-7600,-2500,0}/{-7600,0,0}/{-2500,-2500,0}/{-2500,0,0}/{-4900,-2500,0}/{-4900,0,0}/{-2500,-2500,0}/{0,0,0}/{-2500,0,0}/{0,-2500,0}/{-5200,-2500,0}/{-5200,0,0}/{-5500,-2500,0}/{-5500,0,0}/{-5800,-2500,0}/{-5800,0,0}/{-6100,-2500,0}/{-6100,0,0}/{-6400,-2500,0}/{-6400,0,0}/{-6700,-2500,0}/{-6700,0,0}/{-7000,-2500,0}/{-7000,0,0}/{-7300,-2500,0}/{-7300,0,0}/{-7600,-1250,0}/{-4900,-1250,0}";
            }
            else if (coreType == CoreType.Folded)
            {
                return "{0,0,0}/{0,-5800,0}/{0,-5800,0}/{6060,-5800,0}/{0,0,0}/{6060,0,0}/{6060,0,0}/{6060,-5800,0}/{1400,-4400,0}/{4660,-4400,0}/{4660,-4400,0}/{4660,-1400,0}/{4660,-1400,0}/{1400,-1400,0}/{1400,-1400,0}/{1400,-4400,0}/{1400,-4400,0}/{4660,-1400,0}/{1400,-1400,0}/{4660,-4400,0}/{1400,-4400,0}/{1400,-5800,0}/{1700,-4400,0}/{1700,-5800,0}/{2000,-4400,0}/{2000,-5800,0}/{2300,-4400,0}/{2300,-5800,0}/{2600,-4400,0}/{2600,-5800,0}/{2900,-4400,0}/{2900,-5800,0}/{3200,-4400,0}/{3200,-5800,0}/{3500,-4400,0}/{3500,-5800,0}/{3800,-4400,0}/{3800,-5800,0}/{4660,-3500,0}/{6060,-3500,0}/{4660,-3200,0}/{6060,-3200,0}/{4660,-2900,0}/{6060,-2900,0}/{4660,-2600,0}/{6060,-2600,0}/{4660,-2300,0}/{6060,-2300,0}/{4660,-2000,0}/{6060,-2000,0}/{4660,-1700,0}/{6060,-1700,0}/{4660,-1400,0}/{6060,-1400,0}";
            }
            else if (coreType == CoreType.Vertical_AG1)
            {
                return "{0,-2500,0}/{-2500,-2500,0}/{-6620,-2500,0}/{-3920,-2500,0}/{-7920,-2500,0}/{-6620,-2500,0}/{-7920,0,0}/{-7920,-2500,0}/{0,-2500,0}/{0,0,0}/{-7920,0,0}/{0,0,0}/{-7920,0,0}/{-7920,0,0}/{-7920,-2500,0}/{-7920,-2500,0}/{-6620,-2500,0}/{-6620,-2500,0}/{-3920,-2500,0}/{-3920,-2500,0}/{-2500,-2500,0}/{-2500,-2500,0}/{0,-2500,0}/{-6620,-2500,0}/{-6620,0,0}/{-3920,-2500,0}/{-3920,0,0}/{-2500,-2500,0}/{-2500,0,0}/{-2500,-2500,0}/{0,0,0}/{-2500,0,0}/{0,-2500,0}/{-4220,-2500,0}/{-4220,0,0}/{-4520,-2500,0}/{-4520,0,0}/{-4820,-2500,0}/{-4820,0,0}/{-5120,-2500,0}/{-5120,0,0}/{-5420,-2500,0}/{-5420,0,0}/{-5720,-2500,0}/{-5720,0,0}/{-6020,-2500,0}/{-6020,0,0}/{-6320,-2500,0}/{-6320,0,0}/{-6620,-1250,0}/{-3920,-1250,0}";
            }
            else if (coreType == CoreType.CourtShortEdge)
            {
                return "{0,0,0}/{2700,0,0}/{2700,0,0}/{2700,-1300,0}/{2700,-1300,0}/{2700,-4000,0}/{2700,-4000,0}/{2700,-5300,0}/{2700,-5300,0}/{2700,-7600,0}/{2700,-7600,0}/{0,-7600,0}/{0,-7600,0}/{0,0,0}/{2700,-1300,0}/{0,-1300,0}/{2700,-4000,0}/{0,-4000,0}/{2700,-5300,0}/{0,-5300,0}/{0,-7600,0}/{2700,-5300,0}/{2700,-7600,0}/{0,-5300,0}/{1350,-4000,0}/{1350,-1300,0}/{2700,-1600,0}/{0,-1600,0}/{2700,-1900,0}/{0,-1900,0}/{2700,-2200,0}/{0,-2200,0}/{2700,-2500,0}/{0,-2500,0}/{2700,-2800,0}/{0,-2800,0}/{2700,-3100,0}/{0,-3100,0}/{2700,-3400,0}/{0,-3400,0}/{2700,-3700,0}/{0,-3700,0}";
            }
            else if (coreType == CoreType.CourtShortEdge2)
            {
                return "{0,0,0}/{2700,0,0}/{0,0,0}/{0,-6500,0}/{2700,0,0}/{2700,-1250,0}/{2700,-1250,0}/{2700,-3490,0}/{2700,-3490,0}/{2700,-4740,0}/{2700,-4740,0}/{0,-4740,0}/{2700,-1250,0}/{0,-1250,0}/{2700,-3490,0}/{0,-3490,0}/{1350,-1250,0}/{1350,-3490,0}/{2700,-3210,0}/{0,-3210,0}/{2700,-2930,0}/{0,-2930,0}/{2700,-2650,0}/{0,-2650,0}/{2700,-2370,0}/{0,-2370,0}/{2700,-2090,0}/{0,-2090,0}/{2700,-1810,0}/{0,-1810,0}/{2700,-1530,0}/{0,-1530,0}/{0,-6700,0}/{2700,-6700,0}/{2700,-6700,0}/{2700,-4740,0}/{0,-6700,0}/{0,-6500,0}/{2700,-4740,0}/{0,-6700,0}/{0,-4740,0}/{2700,-6700,0}";
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
                if (i <= horizontalCorePointList.Count - 2)
                {
                    Rhino.Geometry.Line line = new Rhino.Geometry.Line(horizontalCorePointList[i], horizontalCorePointList[i + 1]);
                    Curve curve = line.ToNurbsCurve();
                    horizontalCoreCurve.Add(curve);
                }
            }

            return horizontalCoreCurve;
        }
        private static void DrawPT3Corridor(List<CoreType> coreTypeList, List<Curve> aptLineList, string agType, double aptWidth, out Curve corridorOutline, out Curve currentInnerLine)
        {
            Curve currentCenterLine = aptLineList[0].DuplicateCurve();
            if ((int)currentCenterLine.ClosedCurveOrientation(new Vector3d(0, 0, 1)) == -1)
                currentCenterLine.Reverse();

            currentInnerLine = currentCenterLine.Offset(Plane.WorldXY, aptWidth / 2, 1, CurveOffsetCornerStyle.Sharp)[0];

            if ((int)currentInnerLine.ClosedCurveOrientation(new Vector3d(0, 0, 1)) == -1)
                currentInnerLine.Reverse();

            corridorOutline = currentInnerLine.Offset(Plane.WorldXY, Consts.corridorWidth, 1, CurveOffsetCornerStyle.Sharp)[0];
            return;
        }

        private static List<Curve> DrawPT1Corridor(Apartment apartment)
        {

            List<Curve> corridorLines = new List<Curve>();

            foreach (var hh in apartment.Household)
            {
                foreach (var h in hh)
                {
                    foreach (var household in h)
                    {
                        if (household.isCorridorType)
                        {
                            Curve corridorLine = new LineCurve(household.Origin, household.Origin + household.XDirection * household.XLengthA);
                            corridorLines.Add(corridorLine);
                        }
                    }
                }
            }

            corridorLines = Curve.JoinCurves(corridorLines).ToList();
            List<Curve> finalCorridor = new List<Curve>();
            for (int i = 0; i < corridorLines.Count; i++)
            {
                Curve offset = corridorLines[i].DuplicateCurve();
                Vector3d dir = offset.TangentAtStart;
                dir.Rotate(Math.PI / 2, Vector3d.ZAxis);
                offset.Transform(Rhino.Geometry.Transform.Translation(dir * Consts.corridorWidth));
                PolylineCurve floor = new PolylineCurve(new Point3d[] { corridorLines[i].PointAtStart, offset.PointAtStart, offset.PointAtEnd, corridorLines[i].PointAtEnd, corridorLines[i].PointAtStart });
                finalCorridor.Add(floor.ToNurbsCurve());
            }
            return finalCorridor;
        }

    }//class
}//namespace