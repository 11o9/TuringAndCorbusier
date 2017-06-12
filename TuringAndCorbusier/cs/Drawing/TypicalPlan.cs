using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rhino.Geometry;
using Rhino.Display;
using Rhino.Collections;

namespace TuringAndCorbusier
{
    public class typicalPlan
    {
        private typicalPlan(Curve boundary, Rectangle3d outLine, List<Text3d> roadWidthText, List<FloorPlan> unitPlans, List<CorePlan> corePlans, List<Curve> surroundingSite, int floor)
        {
            this.Boundary = boundary;
            this.OutLine = outLine;
            this.RoadWidth = roadWidthText;
            this.UnitPlans = unitPlans;
            this.CorePlans = corePlans;
            this.SurroundingSite = surroundingSite;
            this.Floor = floor;
        }

        private typicalPlan(Curve boundary, Rectangle3d outLine, List<Text3d> roadWidthText, List<FloorPlan> unitPlans, List<CorePlan> corePlans,List<List<Curve>> nonresidentials, List<Curve> parkingLines, List<Curve> surroundingSite, int floor)
        {
            this.Boundary = boundary;
            this.OutLine = outLine;
            this.RoadWidth = roadWidthText;
            this.UnitPlans = unitPlans;
            this.CorePlans = corePlans;
            this.SurroundingSite = surroundingSite;
            this.Nonresidentials = nonresidentials;
            this.ParkingLines = parkingLines;
            this.Floor = floor;
        }

        public Curve Boundary { get; private set; }
        public Rectangle3d OutLine { get; private set; }
        public List<Text3d> RoadWidth { get; private set; }
        public List<FloorPlan> UnitPlans { get; private set; }
        public List<CorePlan> CorePlans { get; private set; }
        public List<List<Curve>> Nonresidentials { get; private set; }
        public List<Curve> ParkingLines { get; private set; }
        public List<Curve> SurroundingSite { get; private set; }
        public int Floor { get; private set; }

        Rhino.DocObjects.ObjectAttributes CreateColorAttribute(int R, int G, int B)
        {
            Rhino.DocObjects.ObjectAttributes output = new Rhino.DocObjects.ObjectAttributes();
            output.ObjectColor = System.Drawing.Color.FromArgb(255, R, G, B);

            return output;

        }

        public BoundingBox GetBoundingBox()
        {
            return this.OutLine.BoundingBox;
        }

        public static typicalPlan DrawTypicalPlan(Plot plot, Rectangle3d outLine, List<Curve> surroundingSite, Apartment agOutput, List<FloorPlanLibrary> fpls, int targetFloor , bool drawParking)
        {
            List<CorePlan> tempCorePlans = new List<CorePlan>();
            List<FloorPlan> tempUnitPlans = new List<FloorPlan>();
            List<Curve> tempParkingLines = new List<Curve>();

            if (targetFloor == 0)
            {
                tempCorePlans = agOutput.Core.First().Select(n => new CorePlan(n)).ToList();
                tempUnitPlans = new List<FloorPlan>();
            }

            else
            {
                foreach (List<Core> i in agOutput.Core)
                {
                    foreach (Core j in i)
                    {
                        if (j.Stories + 2 > targetFloor)
                            tempCorePlans.Add(new CorePlan(j));
                    }
                }

                for (int i = 0; i < agOutput.Household[targetFloor - 1].Count(); i++)
                {
                    try
                    {
                        if (agOutput.Household[targetFloor - 1][i] != null)
                        {
                            for (int j = 0; j < agOutput.Household[targetFloor - 1][i].Count; j++)
                                tempUnitPlans.Add(new FloorPlan(agOutput.Household[targetFloor - 1][i][j], fpls, agOutput.AGtype));
                        }

                    }
                    catch (Exception)
                    {
                        continue;
                    }

                }
            }

            List<List<Curve>> nonResidentials = new List<List<Curve>>();

            foreach(NonResidential i in agOutput.Commercial)
            {

                if (i == null)
                    continue;

                List<Curve> tempCurves = new List<Curve>();

                Curve OutLine = i.ToNurbsCurve();

                

                if (OutLine.ClosedCurveOrientation(Vector3d.ZAxis) != CurveOrientation.CounterClockwise)
                    OutLine.Reverse();
                //임시수정
                if (OutLine.Offset(Plane.WorldXY, Consts.exWallThickness, 0, CurveOffsetCornerStyle.None).Count() == 0)
                    break;
                Curve InnerCurve = OutLine.Offset(Plane.WorldXY, Consts.exWallThickness, 0, CurveOffsetCornerStyle.None)[0];

                tempCurves.Add(OutLine);
                tempCurves.Add(InnerCurve);

                nonResidentials.Add(tempCurves);
            }

            foreach (NonResidential i in agOutput.PublicFacility)
            {
                if (i == null)
                    continue;


                List<Curve> tempCurves = new List<Curve>();

                Curve OutLine = i.ToNurbsCurve();

                if (OutLine.ClosedCurveOrientation(Vector3d.ZAxis) != CurveOrientation.CounterClockwise)
                    OutLine.Reverse();

                Curve InnerCurve = OutLine.Offset(Plane.WorldXY, Consts.exWallThickness, 0, CurveOffsetCornerStyle.None)[0];

                tempCurves.Add(OutLine);
                tempCurves.Add(InnerCurve);

                nonResidentials.Add(tempCurves);
            }


            List<Text3d> roadWithText = GetRoadWidthFromPlot(plot);

            if (drawParking)
            {
                for (int i = 0; i < agOutput.ParkingLotOnEarth.ParkingLines.Count(); i++)
                    for (int j = 0; j < agOutput.ParkingLotOnEarth.ParkingLines[i].Count(); j++)
                        tempParkingLines.Add(agOutput.ParkingLotOnEarth.ParkingLines[i][j].Boundary.ToNurbsCurve());

                return new typicalPlan(plot.Boundary, outLine, roadWithText, tempUnitPlans, tempCorePlans, nonResidentials, tempParkingLines, surroundingSite, targetFloor);
            }
            else
            {
                return new typicalPlan(plot.Boundary, outLine, roadWithText, tempUnitPlans, tempCorePlans, surroundingSite, targetFloor);
            }

        }

        private static List<Text3d> GetRoadWidthFromPlot(Plot plot)
        {
            Curve[] segments = plot.Boundary.DuplicateSegments();

            List<Text3d> output = new List<Text3d>();

            for (int i = 0; i < plot.Surroundings.Count(); i++)
            {
                if (plot.Surroundings[i] > 0)
                {
                    Curve tempCurve = segments[i].DuplicateCurve();
                    RhinoList<Curve> tempOffsettedCurve = new RhinoList<Curve>(tempCurve.Offset(Plane.WorldXY, -plot.Surroundings[i] * 1000, 0, CurveOffsetCornerStyle.None));

                    double[] offsettedCurveLength = (from curve in tempOffsettedCurve
                                                     select curve.GetLength()).ToArray();

                    tempOffsettedCurve.Sort(offsettedCurveLength);

                    Point3d tempPoint = tempOffsettedCurve[tempOffsettedCurve.Count - 1].PointAt(tempOffsettedCurve[tempOffsettedCurve.Count - 1].Domain.Mid);
                    Vector3d tempCurveVector = new Vector3d(tempCurve.PointAt(tempCurve.Domain.T1) - tempCurve.PointAt(tempCurve.Domain.T0));
                    double angle = getAngle(tempCurveVector);

                    if (angle > Math.PI / 2)
                        angle += Math.PI;

                    Plane tempPlane = new Plane();
                    tempPlane.Transform(Transform.Rotation(angle, Point3d.Origin));
                    tempPlane.Transform(Transform.Translation(new Vector3d(tempPoint)));

                    output.Add(new Text3d(plot.Surroundings[i].ToString() + "m 도로", tempPlane, 1000));
                }
            }

            return output;
        }

        private static double getAngle(Vector3d targetVec)
        {
            double angle = Vector3d.VectorAngle(Vector3d.XAxis, targetVec);
            angle *= Math.Sign(Vector3d.CrossProduct(Vector3d.XAxis, targetVec).Z);
            return angle;
        }

    }
}
