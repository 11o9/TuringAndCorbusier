using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Shapes;
using Rhino.Geometry;
using Rhino.Display;
using System.Windows.Media;
using System.Windows;

namespace TuringAndCorbusier
{
    public class PlanDrawingFunction
    {


        public static Household alignHousholdProperties(Household household)
        {
            Household tempHousehold = new Household(household);

            tempHousehold.XDirection = Vector3d.XAxis;
            tempHousehold.YDirection = Vector3d.YAxis;

            return tempHousehold;
        }

        public static void drawDimension(Rectangle3d tempBoundingBox, List<FloorPlan.Dimension> dimensions, double tempScaleFactor, System.Windows.Point tempOrigin, ref Canvas UnitPlanCanvas)
        {
            for (int i = 0; i < dimensions.Count; i++)
            {
                FloorPlan.Dimension tempDimension = dimensions[i];
                drawText(tempBoundingBox, tempDimension.NumberText, tempScaleFactor, tempOrigin, ref UnitPlanCanvas, 20, Brushes.Black);
                List<Curve> dimensionCurves = tempDimension.ExtensionLine;
                dimensionCurves.Add(tempDimension.DimensionLine);
                drawPlan(tempBoundingBox, dimensionCurves, tempScaleFactor, tempOrigin, ref UnitPlanCanvas, Brushes.Black, 1);
            }
        }

        public static void drawText(Rectangle3d tempBoundingBox, Text3d textToDraw, double tempScaleFactor, System.Windows.Point tempOrigin, ref Canvas UnitPlanCanvas, double fontSize, Brush foreGround)
        {
            System.Windows.Point tempTransformedPoint = pointConverter(tempBoundingBox, textToDraw.TextPlane.Origin, tempScaleFactor, tempOrigin);

            Border tempBorder = new Border();
            tempBorder.Height = 200;
            tempBorder.Width = 200;
            Canvas.SetLeft(tempBorder, pointConverter(tempBoundingBox, textToDraw.TextPlane.Origin, tempScaleFactor, tempOrigin).X - 200 / 2);
            Canvas.SetTop(tempBorder, pointConverter(tempBoundingBox, textToDraw.TextPlane.Origin, tempScaleFactor, tempOrigin).Y - 200 / 2);

            TextBlock textBlockToDraw = new TextBlock();
            textBlockToDraw.Text = textToDraw.Text;
            textBlockToDraw.Foreground = foreGround;
            textBlockToDraw.FontSize = fontSize;
            textBlockToDraw.VerticalAlignment = VerticalAlignment.Center;
            textBlockToDraw.HorizontalAlignment = HorizontalAlignment.Center;

            RotateTransform tempRotater = new RotateTransform(getAngle(textToDraw.TextPlane.XAxis) * 180 / Math.PI);
            textBlockToDraw.RenderTransform = tempRotater;
            textBlockToDraw.RenderTransformOrigin = new System.Windows.Point(.5, .5);

            tempBorder.Child = textBlockToDraw;

            UnitPlanCanvas.Children.Add(tempBorder);
        }

        public static void drawBackGround(Rectangle3d tempBoundingBox, Curve curveToDraw, double tempScaleFactor, System.Windows.Point tempOrigin, ref Canvas UnitPlanCanvas, Brush fillBrush)
        {
            List<System.Windows.Point> pointSet = new List<System.Windows.Point>();
            Curve duplicatedCurve = curveToDraw.DuplicateCurve();
            duplicatedCurve.MakeClosed(0);
            Curve[] segments = duplicatedCurve.DuplicateSegments();

            for (int i = 0; i <= segments.Length; i++)
            {
                int tempIndex = i % segments.Length;

                System.Windows.Point tempTransformedPoint = pointConverter(tempBoundingBox, segments[tempIndex].PointAt(segments[tempIndex].Domain.T0), tempScaleFactor, tempOrigin);
                pointSet.Add(tempTransformedPoint);
            }

            Polygon polygonToDraw = new Polygon();
            polygonToDraw.Points = new PointCollection(pointSet);
            polygonToDraw.Fill = fillBrush;

            UnitPlanCanvas.Children.Add(polygonToDraw);
        }
        public static void drawUnitBackGround(Rectangle3d tempBoundingBox, Curve curveToDraw, double tempScaleFactor, System.Windows.Point tempOrigin, ref Canvas UnitPlanCanvas, Brush fillBrush)
        {
            List<System.Windows.Point> pointSet = new List<System.Windows.Point>();
            Curve duplicatedCurve = curveToDraw.DuplicateCurve();
            duplicatedCurve.MakeClosed(0);
            Curve[] segments = duplicatedCurve.DuplicateSegments();

            for (int i = 0; i <= segments.Length; i++)
            {
                int tempIndex = i % segments.Length;

                System.Windows.Point tempTransformedPoint = pointConverter(tempBoundingBox, segments[tempIndex].PointAt(segments[tempIndex].Domain.T0), tempScaleFactor, tempOrigin);
                pointSet.Add(tempTransformedPoint);
            }

            Polygon polygonToDraw = new Polygon();
            polygonToDraw.Points = new PointCollection(pointSet);
            SolidColorBrush LHGreen = new SolidColorBrush();
            LHGreen.Color = Color.FromRgb(200, 229, 13);
            polygonToDraw.Fill = LHGreen;

            UnitPlanCanvas.Children.Add(polygonToDraw);
            double width = ((tempBoundingBox.Width / 2) * tempScaleFactor);
            double height = ((tempBoundingBox.Height / 2) * tempScaleFactor);
            Canvas.SetLeft(polygonToDraw, -width);
            Canvas.SetTop(polygonToDraw, -height);
        }

        public static void drawPlan(Rectangle3d tempBoundingBox, List<List<Curve>> curveToDraw, double tempScaleFactor, System.Windows.Point tempOrigin, ref Canvas UnitPlanCanvas, Brush strokeBrush, double strokeThickness)
        {

            foreach (List<Curve> h in curveToDraw)
            {
                foreach (Curve i in h)
                {
                    Curve[] shatteredCurves = i.DuplicateSegments();

                    if (shatteredCurves.Length > 1)
                    {
                        foreach (Curve j in shatteredCurves)
                        {
                            System.Windows.Point Start = pointConverter(tempBoundingBox, j.PointAt(j.Domain.T0), tempScaleFactor, tempOrigin);
                            System.Windows.Point End = pointConverter(tempBoundingBox, j.PointAt(j.Domain.T1), tempScaleFactor, tempOrigin);

                            System.Windows.Shapes.Line line = new System.Windows.Shapes.Line();
                            line.Stroke = strokeBrush;

                            line.X1 = Math.Abs(Start.X);
                            line.X2 = Math.Abs(End.X);
                            line.Y1 = Math.Abs(Start.Y);
                            line.Y2 = Math.Abs(End.Y);

                            line.StrokeThickness = strokeThickness;

                            UnitPlanCanvas.Children.Add(line);

                        }
                    }
                    else
                    {

                        if (i.PointAt(i.Domain.Mid) != (i.PointAtStart + i.PointAtEnd) / 2)
                        {
                            List<Curve> shatteredArc = shatterArc(i);

                            foreach (Curve j in shatteredArc)
                            {
                                System.Windows.Point Start = pointConverter(tempBoundingBox, j.PointAt(j.Domain.T0), tempScaleFactor, tempOrigin);
                                System.Windows.Point End = pointConverter(tempBoundingBox, j.PointAt(j.Domain.T1), tempScaleFactor, tempOrigin);

                                System.Windows.Shapes.Line line = new System.Windows.Shapes.Line();
                                line.Stroke = strokeBrush;

                                line.X1 = Math.Abs(Start.X);
                                line.X2 = Math.Abs(End.X);
                                line.Y1 = Math.Abs(Start.Y);
                                line.Y2 = Math.Abs(End.Y);

                                line.StrokeThickness = strokeThickness;

                                UnitPlanCanvas.Children.Add(line);

                            }
                        }
                        else
                        {
                            System.Windows.Point Start = pointConverter(tempBoundingBox, i.PointAt(i.Domain.T0), tempScaleFactor, tempOrigin);
                            System.Windows.Point End = pointConverter(tempBoundingBox, i.PointAt(i.Domain.T1), tempScaleFactor, tempOrigin);

                            System.Windows.Shapes.Line line = new System.Windows.Shapes.Line();
                            line.Stroke = strokeBrush;

                            line.X1 = Math.Abs(Start.X);
                            line.X2 = Math.Abs(End.X);
                            line.Y1 = Math.Abs(Start.Y);
                            line.Y2 = Math.Abs(End.Y);

                            line.StrokeThickness = strokeThickness;

                            UnitPlanCanvas.Children.Add(line);

                        }
                    }
                }
            }
        }
        //JHL
        public static void drawBoundaryPlan(Rectangle3d tempBoundingBox, Curve curveToDraw, double tempScaleFactor, System.Windows.Point tempOrigin, ref Canvas UnitPlanCanvas, Brush strokeBrush, double strokeThickness)
        {
            Curve[] shatteredCurves = curveToDraw.DuplicateSegments();
            List<System.Windows.Point> polygonPointList = new List<System.Windows.Point>();
            System.Windows.Point point = new System.Windows.Point();
            if (shatteredCurves.Length > 1)
            {
                for (int i = 0; i < shatteredCurves.Length; i++)
                {
                    System.Windows.Point Start = pointConverter(tempBoundingBox, shatteredCurves[i].PointAt(shatteredCurves[i].Domain.T0), tempScaleFactor, tempOrigin);
                    System.Windows.Point End = pointConverter(tempBoundingBox, shatteredCurves[i].PointAt(shatteredCurves[i].Domain.T1), tempScaleFactor, tempOrigin);

                    System.Windows.Shapes.Line line = new System.Windows.Shapes.Line();
                    line.Stroke = strokeBrush;

                    line.X1 = Math.Abs(Start.X);
                    line.X2 = Math.Abs(End.X);
                    line.Y1 = Math.Abs(Start.Y);
                    line.Y2 = Math.Abs(End.Y);

                    //line.Stroke = new SolidColorBrush(Color.FromRgb(196, 215, 0));
                    // line.StrokeThickness = strokeThickness;
                    if (i == 0)
                    {
                        point = new System.Windows.Point(line.X1, line.Y1);
                    }
                    polygonPointList.Add(new System.Windows.Point(line.X2, line.Y2));
                }

                polygonPointList.Add(point);
                Polygon polygon = new Polygon();
                polygon.Stroke = new SolidColorBrush(Color.FromRgb(200, 229, 13));
                polygon.StrokeThickness = strokeThickness;
                PointCollection pointCollection = new PointCollection();
                for (int i = 0; i < polygonPointList.Count; i++)
                {
                    pointCollection.Add(polygonPointList[i]);
                }
                polygon.Points = pointCollection;
                SolidColorBrush fillColor = new SolidColorBrush();
                fillColor.Color = Colors.LightGray;
                polygon.Fill = fillColor;
                UnitPlanCanvas.Children.Add(polygon);
            }
            else
            {

                if (curveToDraw.PointAt(curveToDraw.Domain.Mid) != (curveToDraw.PointAtStart + curveToDraw.PointAtEnd) / 2)
                {
                    List<Curve> shatteredArc = shatterArc(curveToDraw);

                    foreach (Curve j in shatteredArc)
                    {
                        System.Windows.Point Start = pointConverter(tempBoundingBox, j.PointAt(j.Domain.T0), tempScaleFactor, tempOrigin);
                        System.Windows.Point End = pointConverter(tempBoundingBox, j.PointAt(j.Domain.T1), tempScaleFactor, tempOrigin);

                        System.Windows.Shapes.Line line = new System.Windows.Shapes.Line();
                        line.Stroke = strokeBrush;

                        line.X1 = Math.Abs(Start.X);
                        line.X2 = Math.Abs(End.X);
                        line.Y1 = Math.Abs(Start.Y);
                        line.Y2 = Math.Abs(End.Y);

                        line.StrokeThickness = strokeThickness;

                        UnitPlanCanvas.Children.Add(line);

                    }
                }
                else
                {
                    System.Windows.Point Start = pointConverter(tempBoundingBox, curveToDraw.PointAt(curveToDraw.Domain.T0), tempScaleFactor, tempOrigin);
                    System.Windows.Point End = pointConverter(tempBoundingBox, curveToDraw.PointAt(curveToDraw.Domain.T1), tempScaleFactor, tempOrigin);

                    System.Windows.Shapes.Line line = new System.Windows.Shapes.Line();
                    line.Stroke = strokeBrush;

                    line.X1 = Math.Abs(Start.X);
                    line.X2 = Math.Abs(End.X);
                    line.Y1 = Math.Abs(Start.Y);
                    line.Y2 = Math.Abs(End.Y);

                    line.StrokeThickness = strokeThickness;

                    UnitPlanCanvas.Children.Add(line);

                }
            }
        }


        public static void drawPlan(Rectangle3d tempBoundingBox, List<Curve> curveToDraw, double tempScaleFactor, System.Windows.Point tempOrigin, ref Canvas UnitPlanCanvas, Brush strokeBrush, double strokeThickness)
        {
            foreach (Curve i in curveToDraw)
            {
                Curve[] shatteredCurves = i.DuplicateSegments();


                if (shatteredCurves.Length > 1)
                {
                    foreach (Curve j in shatteredCurves)
                    {
                        System.Windows.Point Start = pointConverter(tempBoundingBox, j.PointAt(j.Domain.T0), tempScaleFactor, tempOrigin);
                        System.Windows.Point End = pointConverter(tempBoundingBox, j.PointAt(j.Domain.T1), tempScaleFactor, tempOrigin);

                        System.Windows.Shapes.Line line = new System.Windows.Shapes.Line();
                        line.Stroke = strokeBrush;

                        line.X1 = Math.Abs(Start.X);
                        line.X2 = Math.Abs(End.X);
                        line.Y1 = Math.Abs(Start.Y);
                        line.Y2 = Math.Abs(End.Y);

                        line.StrokeThickness = strokeThickness;

                        UnitPlanCanvas.Children.Add(line);

                    }
                }
                else
                {
                    if (i.PointAt(i.Domain.Mid) != (i.PointAtStart + i.PointAtEnd) / 2)
                    {
                        List<Curve> shatteredArc = shatterArc(i);

                        foreach (Curve j in shatteredArc)
                        {
                            System.Windows.Point Start = pointConverter(tempBoundingBox, j.PointAt(j.Domain.T0), tempScaleFactor, tempOrigin);
                            System.Windows.Point End = pointConverter(tempBoundingBox, j.PointAt(j.Domain.T1), tempScaleFactor, tempOrigin);

                            System.Windows.Shapes.Line line = new System.Windows.Shapes.Line();
                            line.Stroke = Brushes.Black;

                            line.X1 = Math.Abs(Start.X);
                            line.X2 = Math.Abs(End.X);
                            line.Y1 = Math.Abs(Start.Y);
                            line.Y2 = Math.Abs(End.Y);

                            line.StrokeThickness = strokeThickness;

                            UnitPlanCanvas.Children.Add(line);
                        }
                    }
                    else
                    {
                        System.Windows.Point Start = pointConverter(tempBoundingBox, i.PointAt(i.Domain.T0), tempScaleFactor, tempOrigin);
                        System.Windows.Point End = pointConverter(tempBoundingBox, i.PointAt(i.Domain.T1), tempScaleFactor, tempOrigin);

                        System.Windows.Shapes.Line line = new System.Windows.Shapes.Line();
                        line.Stroke = Brushes.Black;

                        line.X1 = Math.Abs(Start.X);
                        line.X2 = Math.Abs(End.X);
                        line.Y1 = Math.Abs(Start.Y);
                        line.Y2 = Math.Abs(End.Y);

                        line.StrokeThickness = strokeThickness;

                        UnitPlanCanvas.Children.Add(line);
                    }
                }
            }
        }


        public static void drawDashedPlan(Rectangle3d tempBoundingBox, Curve curveToDraw, double tempScaleFactor, System.Windows.Point tempOrigin, ref Canvas UnitPlanCanvas, System.Windows.Media.Brush strokeBrush, double strokeThickness)
        {
            if (curveToDraw != null)

            {
                Curve[] shatteredCurves = curveToDraw.DuplicateSegments();

                if (shatteredCurves.Length > 1)
                {
                    foreach (Curve j in shatteredCurves)
                    {
                        System.Windows.Point Start = pointConverter(tempBoundingBox, j.PointAt(j.Domain.T0), tempScaleFactor, tempOrigin);
                        System.Windows.Point End = pointConverter(tempBoundingBox, j.PointAt(j.Domain.T1), tempScaleFactor, tempOrigin);

                        System.Windows.Shapes.Line line = new System.Windows.Shapes.Line();
                        line.Stroke = strokeBrush;

                        line.X1 = Math.Abs(Start.X);
                        line.X2 = Math.Abs(End.X);
                        line.Y1 = Math.Abs(Start.Y);
                        line.Y2 = Math.Abs(End.Y);

                        line.StrokeThickness = strokeThickness;
                        DoubleCollection dashes = new DoubleCollection();
                        dashes.Add(35);
                        dashes.Add(30);
                        line.StrokeDashArray = dashes;
                        UnitPlanCanvas.Children.Add(line);
                    }
                }
                else
                {

                    if (curveToDraw.PointAt(curveToDraw.Domain.Mid) != (curveToDraw.PointAtStart + curveToDraw.PointAtEnd) / 2)
                    {
                        List<Curve> shatteredArc = shatterArc(curveToDraw);

                        foreach (Curve j in shatteredArc)
                        {
                            System.Windows.Point Start = pointConverter(tempBoundingBox, j.PointAt(j.Domain.T0), tempScaleFactor, tempOrigin);
                            System.Windows.Point End = pointConverter(tempBoundingBox, j.PointAt(j.Domain.T1), tempScaleFactor, tempOrigin);

                            System.Windows.Shapes.Line line = new System.Windows.Shapes.Line();
                            line.Stroke = strokeBrush;

                            line.X1 = Math.Abs(Start.X);
                            line.X2 = Math.Abs(End.X);
                            line.Y1 = Math.Abs(Start.Y);
                            line.Y2 = Math.Abs(End.Y);

                            line.StrokeThickness = strokeThickness;
                            DoubleCollection dashes = new DoubleCollection();
                            dashes.Add(35);
                            dashes.Add(30);
                            line.StrokeDashArray = dashes;
                            UnitPlanCanvas.Children.Add(line);

                        }
                    }
                    else
                    {
                        System.Windows.Point Start = pointConverter(tempBoundingBox, curveToDraw.PointAt(curveToDraw.Domain.T0), tempScaleFactor, tempOrigin);
                        System.Windows.Point End = pointConverter(tempBoundingBox, curveToDraw.PointAt(curveToDraw.Domain.T1), tempScaleFactor, tempOrigin);

                        System.Windows.Shapes.Line line = new System.Windows.Shapes.Line();
                        line.Stroke = strokeBrush;

                        line.X1 = Math.Abs(Start.X);
                        line.X2 = Math.Abs(End.X);
                        line.Y1 = Math.Abs(Start.Y);
                        line.Y2 = Math.Abs(End.Y);

                        line.StrokeThickness = strokeThickness;
                        DoubleCollection dashes = new DoubleCollection();
                        dashes.Add(35);
                        dashes.Add(30);
                        line.StrokeDashArray = dashes;
                        UnitPlanCanvas.Children.Add(line);

                    }
                }
            }

        }


        public static void drawPlan(Rectangle3d tempBoundingBox, Curve curveToDraw, double tempScaleFactor, System.Windows.Point tempOrigin, ref Canvas UnitPlanCanvas, Brush strokeBrush, double strokeThickness)
        {
            Curve[] shatteredCurves = curveToDraw.DuplicateSegments();

            if (shatteredCurves.Length > 1)
            {
                foreach (Curve j in shatteredCurves)
                {
                    System.Windows.Point Start = pointConverter(tempBoundingBox, j.PointAt(j.Domain.T0), tempScaleFactor, tempOrigin);
                    System.Windows.Point End = pointConverter(tempBoundingBox, j.PointAt(j.Domain.T1), tempScaleFactor, tempOrigin);

                    System.Windows.Shapes.Line line = new System.Windows.Shapes.Line();
                    line.Stroke = strokeBrush;

                    line.X1 = Math.Abs(Start.X);
                    line.X2 = Math.Abs(End.X);
                    line.Y1 = Math.Abs(Start.Y);
                    line.Y2 = Math.Abs(End.Y);

                    line.StrokeThickness = strokeThickness;

                    UnitPlanCanvas.Children.Add(line);

                }
            }
            else
            {

                if (curveToDraw.PointAt(curveToDraw.Domain.Mid) != (curveToDraw.PointAtStart + curveToDraw.PointAtEnd) / 2)
                {
                    List<Curve> shatteredArc = shatterArc(curveToDraw);

                    foreach (Curve j in shatteredArc)
                    {
                        System.Windows.Point Start = pointConverter(tempBoundingBox, j.PointAt(j.Domain.T0), tempScaleFactor, tempOrigin);
                        System.Windows.Point End = pointConverter(tempBoundingBox, j.PointAt(j.Domain.T1), tempScaleFactor, tempOrigin);

                        System.Windows.Shapes.Line line = new System.Windows.Shapes.Line();
                        line.Stroke = strokeBrush;

                        line.X1 = Math.Abs(Start.X);
                        line.X2 = Math.Abs(End.X);
                        line.Y1 = Math.Abs(Start.Y);
                        line.Y2 = Math.Abs(End.Y);

                        line.StrokeThickness = strokeThickness;

                        UnitPlanCanvas.Children.Add(line);

                    }
                }
                else
                {
                    System.Windows.Point Start = pointConverter(tempBoundingBox, curveToDraw.PointAt(curveToDraw.Domain.T0), tempScaleFactor, tempOrigin);
                    System.Windows.Point End = pointConverter(tempBoundingBox, curveToDraw.PointAt(curveToDraw.Domain.T1), tempScaleFactor, tempOrigin);

                    System.Windows.Shapes.Line line = new System.Windows.Shapes.Line();
                    line.Stroke = strokeBrush;

                    line.X1 = Math.Abs(Start.X);
                    line.X2 = Math.Abs(End.X);
                    line.Y1 = Math.Abs(Start.Y);
                    line.Y2 = Math.Abs(End.Y);

                    line.StrokeThickness = strokeThickness;

                    UnitPlanCanvas.Children.Add(line);

                }
            }
        }
        //JHL unitplan drawing
        public static void drawUnitPlan(Rectangle3d tempBoundingBox, Curve curveToDraw, double tempScaleFactor, System.Windows.Point tempOrigin, ref Canvas UnitPlanCanvas, Brush strokeBrush, double strokeThickness)
        {
            Curve[] shatteredCurves = curveToDraw.DuplicateSegments();
            List<System.Windows.Shapes.Line> WPFLineList = new List<System.Windows.Shapes.Line>();
            double width, height;
            if (shatteredCurves.Length > 1)
            {
                foreach (Curve j in shatteredCurves)
                {
                    System.Windows.Point Start = pointConverter(tempBoundingBox, j.PointAt(j.Domain.T0), tempScaleFactor, tempOrigin);
                    System.Windows.Point End = pointConverter(tempBoundingBox, j.PointAt(j.Domain.T1), tempScaleFactor, tempOrigin);

                    System.Windows.Shapes.Line line = new System.Windows.Shapes.Line();
                    line.Stroke = strokeBrush;

                    line.X1 = Math.Abs(Start.X);
                    line.X2 = Math.Abs(End.X);
                    line.Y1 = Math.Abs(Start.Y);
                    line.Y2 = Math.Abs(End.Y);

                    line.StrokeThickness = strokeThickness;
                    WPFLineList.Add(line);
                    UnitPlanCanvas.Children.Add(line);
                    width = ((tempBoundingBox.Width / 2) * tempScaleFactor);
                    height = ((tempBoundingBox.Height / 2) * tempScaleFactor);
                    Canvas.SetLeft(line, -width);
                    Canvas.SetTop(line, -height);
                }
            }
            else
            {

                if (curveToDraw.PointAt(curveToDraw.Domain.Mid) != (curveToDraw.PointAtStart + curveToDraw.PointAtEnd) / 2)
                {
                    List<Curve> shatteredArc = shatterArc(curveToDraw);

                    foreach (Curve j in shatteredArc)
                    {
                        System.Windows.Point Start = pointConverter(tempBoundingBox, j.PointAt(j.Domain.T0), tempScaleFactor, tempOrigin);
                        System.Windows.Point End = pointConverter(tempBoundingBox, j.PointAt(j.Domain.T1), tempScaleFactor, tempOrigin);

                        System.Windows.Shapes.Line line = new System.Windows.Shapes.Line();
                        line.Stroke = strokeBrush;

                        line.X1 = Math.Abs(Start.X);
                        line.X2 = Math.Abs(End.X);
                        line.Y1 = Math.Abs(Start.Y);
                        line.Y2 = Math.Abs(End.Y);

                        line.StrokeThickness = strokeThickness;
                        WPFLineList.Add(line);
                        UnitPlanCanvas.Children.Add(line);
                        //aligned the drawing to center
                        width = ((tempBoundingBox.Width / 2) * tempScaleFactor);
                        height = ((tempBoundingBox.Height / 2) * tempScaleFactor);
                        Canvas.SetLeft(line, -width);
                        Canvas.SetTop(line, -height);


                    }
                }
                else
                {
                    System.Windows.Point Start = pointConverter(tempBoundingBox, curveToDraw.PointAt(curveToDraw.Domain.T0), tempScaleFactor, tempOrigin);
                    System.Windows.Point End = pointConverter(tempBoundingBox, curveToDraw.PointAt(curveToDraw.Domain.T1), tempScaleFactor, tempOrigin);

                    System.Windows.Shapes.Line line = new System.Windows.Shapes.Line();
                    line.Stroke = strokeBrush;
                    WPFLineList.Add(line);
                    line.X1 = Math.Abs(Start.X);
                    line.X2 = Math.Abs(End.X);
                    line.Y1 = Math.Abs(Start.Y);
                    line.Y2 = Math.Abs(End.Y);

                    line.StrokeThickness = strokeThickness;

                    UnitPlanCanvas.Children.Add(line);
                    width = ((tempBoundingBox.Width / 2) * tempScaleFactor);
                    height = ((tempBoundingBox.Height / 2) * tempScaleFactor);
                    Canvas.SetLeft(line, -width);
                    Canvas.SetTop(line, -height);
                }
            }
        }


        private static bool IsLineHorizontal(System.Windows.Shapes.Line WPFLineList)
        {

            if (WPFLineList.X1 == WPFLineList.X2)
            {
                return true;
            }
            else
            {
                return false;
            }

        }
        public static void DrawUnitPlanDimension(Curve householdOutline, List<Point3d> dimensionLocationPointList, List<double> houseOutlineLength, Rectangle3d tempBoundingBox, double scaleFactor, System.Windows.Point origin, ref Canvas unitPlanCanvas)
        {
            List<TextBlock> dimensionTextBlockList = GetDimensionList(houseOutlineLength);
            List<System.Windows.Point> convertedPointList = new List<System.Windows.Point>();

            foreach (Point3d point in dimensionLocationPointList)
            {
                convertedPointList.Add(pointConverter(tempBoundingBox, point, scaleFactor, origin));
            }
            List<TextBlock> horizontal = new List<TextBlock>();
            List<TextBlock> vertical = new List<TextBlock>();
            List<System.Windows.Point> horizontalPoints = new List<System.Windows.Point>();
            List<System.Windows.Point> verticalPoints = new List<System.Windows.Point>();
            DrawDimension(origin, dimensionTextBlockList, convertedPointList, unitPlanCanvas, tempBoundingBox, scaleFactor, householdOutline, out horizontal, out vertical, out horizontalPoints, out verticalPoints);
            FinalRepositioning(scaleFactor, tempBoundingBox, convertedPointList, horizontal, vertical, unitPlanCanvas, horizontalPoints, verticalPoints);

        }

        private static void FinalRepositioning(double tempScaleFactor, Rectangle3d tempBoundingBox, List<System.Windows.Point> pointList, List<TextBlock> horizontal, List<TextBlock> vertical, Canvas unitPlanCanvas, List<System.Windows.Point> horizontalPoints, List<System.Windows.Point> verticalPoints)
        {
            List<int> horizontalInt = new List<int>();
            for (int i = 0; i < horizontal.Count; i++)
            {
                string s = horizontal[i].Text.Replace(",", "");
                horizontalInt.Add(int.Parse(s));
            }
            int largestValueIndex = GetLargestValueIndex(horizontalInt);
            double width = ((tempBoundingBox.Width / 2) * tempScaleFactor);
            double height = ((tempBoundingBox.Height / 2) * tempScaleFactor);

            if (horizontal.Count > 2)
            {
                Canvas.SetLeft(horizontal[largestValueIndex], (horizontalPoints[largestValueIndex].X - (width + 15)));
            }
            else
            {
                horizontal.RemoveAt(largestValueIndex);
            }

            List<int> verticalInt = new List<int>();
            for (int i = 0; i < vertical.Count; i++)
            {
                string s = vertical[i].Text.Replace(",", "");
                verticalInt.Add(int.Parse(s));
            }
            int largestValueIndex1 = GetLargestValueIndex(verticalInt);

            if (vertical.Count > 2)
            {
                Canvas.SetTop(vertical[largestValueIndex1], (verticalPoints[largestValueIndex1].Y - (height + 20)));
            }
            else
            {
                vertical.RemoveAt(largestValueIndex1);
            }
        }





        private static int GetLargestValueIndex(List<int> textBlockList)
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

        private static bool IsHorizontal(Curve segment)
        {
            bool isHorizontal = false;
            if (segment.PointAtStart.X == segment.PointAtEnd.X)
            {
                isHorizontal = true;
            }
            return isHorizontal;
        }



        private static List<TextBlock> GetDimensionList(List<double> lineLengthList)
        {
            List<TextBlock> dimensionList = new List<TextBlock>();

            foreach (double lineLength in lineLengthList)
            {
                string formattedLength = lineLength.ToString("N0");
                TextBlock dimension = new TextBlock();
                dimension.Text = formattedLength;
                dimension.FontSize = 10;
                dimensionList.Add(dimension);
            }
            return dimensionList;
        }

        private static System.Windows.Shapes.Line DrawWPFLine(Point3d p1, Point3d p2)
        {

            System.Windows.Shapes.Line line = new System.Windows.Shapes.Line();
            line.X1 = p1.X;
            line.Y1 = p1.Y;
            line.X2 = p2.X;
            line.Y2 = p2.Y;
            return line;
        }

        private static void DrawDimension(System.Windows.Point tempOrigin, List<TextBlock> dimensionList, List<System.Windows.Point> pointList, Canvas UnitPlanCanvas, Rectangle3d tempBoundingBox, double tempScaleFactor, Curve householdOutline, out List<TextBlock> horizontal, out List<TextBlock> vertical, out List<System.Windows.Point> horizontalPoints, out List<System.Windows.Point> verticalPoints)
        {
            Curve[] segments = householdOutline.DuplicateSegments();
            List<bool> isHorizontal = new List<bool>();
            horizontal = new List<TextBlock>();
            vertical = new List<TextBlock>();
            horizontalPoints = new List<System.Windows.Point>();
            verticalPoints = new List<System.Windows.Point>();


            for (int i = 0; i < segments.Length; i++)
            {
                bool ih = IsHorizontal(segments[i]);
                isHorizontal.Add(ih);
            }

            for (int i = 0; i < dimensionList.Count; i++)
            {

                UnitPlanCanvas.Children.Add(dimensionList[i]);

                double width = ((tempBoundingBox.Width / 2) * tempScaleFactor);
                double height = ((tempBoundingBox.Height / 2) * tempScaleFactor);

                Canvas.SetLeft(dimensionList[i], pointList[i].X - width);
                Canvas.SetTop(dimensionList[i], pointList[i].Y - height);


                if (isHorizontal[i] == true)
                {
                    RotateTransform rt = new RotateTransform(-90);
                    TransformGroup tg = new TransformGroup();
                    tg.Children.Add(rt);
                    dimensionList[i].RenderTransform = tg;
                    Canvas.SetTop(dimensionList[i], (pointList[i].Y - height) + 10);
                    horizontal.Add(dimensionList[i]);
                    horizontalPoints.Add(pointList[i]);

                }
                else
                {
                    Canvas.SetLeft(dimensionList[i], (pointList[i].X - width) - 15);
                    Canvas.SetTop(dimensionList[i], (pointList[i].Y - height) - 5);
                    vertical.Add(dimensionList[i]);
                    verticalPoints.Add(pointList[i]);

                }
            }

        }

        private static double getAngle(Vector3d baseVec, Vector3d targetVec)
        {
            double angle = Vector3d.VectorAngle(baseVec, targetVec);
            angle *= Math.Sign(Vector3d.CrossProduct(baseVec, targetVec).Z);
            return angle;
        }

        private static double getAngle(Vector3d targetVec)
        {
            double angle = Vector3d.VectorAngle(Vector3d.XAxis, targetVec);
            angle *= Math.Sign(Vector3d.CrossProduct(Vector3d.XAxis, targetVec).Z);
            return angle;
        }

        public static List<Curve> shatterArc(Curve arcCurve)
        {
            List<Curve> output = new List<Curve>();

            double start = arcCurve.Domain.T0;
            double end = arcCurve.Domain.T1;

            for (int i = 0; i < 16; i++)
            {
                double tempStart = (end - start) / 16 * i + start;
                double tempEnd = (end - start) / 16 * (i + 1) + start;

                Curve tempCurve = new LineCurve(arcCurve.PointAt(tempStart), arcCurve.PointAt(tempEnd));

                output.Add(tempCurve);
            }

            return output;
        }

        public static double scaleToFitFactor(Rectangle targetRectangle, Rectangle3d baseRectangle, out System.Windows.Point origin)
        {
            double heightFactor = targetRectangle.Height / baseRectangle.Height;
            double widthFactor = targetRectangle.Width / baseRectangle.Width;

            double tempFactor = heightFactor;
            origin = new System.Windows.Point((targetRectangle.Width - baseRectangle.Width * tempFactor) / 2, 0);

            if (heightFactor > widthFactor)
            {
                tempFactor = widthFactor;
                //origin = new System.Windows.Point(0, targetRectangle.Height -(targetRectangle.Height - baseRectangle.Height * tempFactor)/2);
                origin = new System.Windows.Point(0, (targetRectangle.Height - baseRectangle.Height * tempFactor) / 2);
            }
            return tempFactor;
        }

        public static double calculateMultipleScaleFactor(Rectangle targetRectangle, List<Rectangle3d> BaseRectangleList, out List<System.Windows.Point> origin)
        {
            List<double> heightFactor = new List<double>();
            List<double> widthFactor = new List<double>();

            for (int i = 0; i < BaseRectangleList.Count; i++)
            {
                heightFactor.Add(targetRectangle.Height / BaseRectangleList[i].Height);
                widthFactor.Add(targetRectangle.Width / BaseRectangleList[i].Width);
            }

            double tempFactor = heightFactor.Min();
            int tempIndex = heightFactor.IndexOf(heightFactor.Min());
            System.Windows.Point tempOrigin = new System.Windows.Point((targetRectangle.Width - BaseRectangleList[tempIndex].Width * tempFactor) / 2, 0);

            if (heightFactor.Min() > widthFactor.Min())
            {
                tempFactor = widthFactor.Min();
                tempIndex = widthFactor.IndexOf(widthFactor.Min());
                tempOrigin = new System.Windows.Point(0, (targetRectangle.Height - BaseRectangleList[tempIndex].Height * tempFactor) / 2);
            }

            List<System.Windows.Point> originOut = new List<System.Windows.Point>();

            for (int i = 0; i < BaseRectangleList.Count; i++)
            {
                double originMoveFactorX = (BaseRectangleList[tempIndex].Width - BaseRectangleList[i].Width) * tempFactor / 2;
                double originMoveFactorY = (BaseRectangleList[tempIndex].Height - BaseRectangleList[i].Height) * tempFactor / 2;

                originOut.Add(new System.Windows.Point(tempOrigin.X + originMoveFactorX, tempOrigin.Y + originMoveFactorY));
            }

            origin = originOut;

            return tempFactor;
        }

        public static Rectangle GetCanvasRectangle(Canvas canvas)
        {
            Rectangle output = new Rectangle();
            output.Width = canvas.Width;
            output.Height = canvas.Height;

            return output;
        }

        public static System.Windows.Point pointConverter(Rectangle3d boundingBox, Point3d pointToConvert, double scaleFactor, System.Windows.Point origin)
        {
            System.Windows.Point point = new System.Windows.Point();

            point.X = (pointToConvert.X - boundingBox.X.Min) * scaleFactor + origin.X;
            point.Y = (boundingBox.Height - (pointToConvert.Y - boundingBox.Y.Min)) * scaleFactor + origin.Y;

            return point;
        }
    }
}