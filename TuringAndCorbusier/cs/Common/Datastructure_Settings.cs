using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rhino.Geometry;

namespace TuringAndCorbusier
{
    namespace Datastructure_Settings
    {
        public class Settings_Page1
        {
            //Construct, 생성자
            public Settings_Page1()
            {
            }

            public Settings_Page1(string projectName, string address, string plotType, double plotArea, double maxfloorAreaRatio, double maxbuildingCoverage, int maxfloors)
            {
                this.ProjectName = projectName.Replace('/', ',');
                this.Address = address;
                this.PlotArea = plotArea;
                this.PlotType = plotType;
                this.MaxFloorAreaRatio = maxfloorAreaRatio;
                this.MaxBuildingCoverage = maxbuildingCoverage;
                this.MaxFloors = maxfloors;
            }

            //Properties. 속성

            public string ProjectName { get; set; }
            public string Address { get; set; }
            public string PlotType { get; set; }
            public double PlotArea { get; set; }
            public double MaxFloorAreaRatio { get; set; }
            public double MaxBuildingCoverage { get; set; }
            public int MaxFloors { get; set; }
        }
        public class Settings_Page2
        {
            //Construct, 생성자
            public Settings_Page2(List<bool> whichAGtoUse, Target target, Interval direction, Interval targetFloor, bool makeUndergroundParking)
            {
                this.WhichAGToUse = whichAGtoUse;
                this.Target = target;
                this.Direction = direction;
                this.TargetFloor = targetFloor;
                this.MakeUnderGroundParking = makeUndergroundParking;
            }

            //Properties. 속성

            public List<bool> WhichAGToUse { get; private set; }
            public Target Target { get; private set; }
            public Interval Direction { get; private set; }
            public Interval TargetFloor { get; private set; }
            public bool MakeUnderGroundParking { get; private set; }
        }

        public class RegulationSettings
        {
            // 초기값
            // {road, plot}
            public double[] DistanceApartment = new double[] { 3000, 3000 };
            public double[] DistanceEase = new double[] { 1500, 1500 };
            public double[] DistanceRow = new double[] { 1000, 750 };
            public double DistanceLighting = 0.25;
            public double DistanceIndentation = 0.8;
            public double EaseFloor = 7;
        }
    }
}
