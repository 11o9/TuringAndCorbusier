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

            public Settings_Page1(string projectName, string address, double plotArea, double maxfloorAreaRatio, double maxbuildingCoverage, int maxfloors)
            {
                this.ProjectName = projectName;
                this.Address = address;
                this.PlotArea = plotArea;
                this.MaxFloorAreaRatio = maxfloorAreaRatio;
                this.MaxBuildingCoverage = maxbuildingCoverage;
                this.MaxFloors = maxfloors;
            }

            //Properties. 속성

            public string ProjectName { get; private set; }
            public string Address { get; private set; }
            public double PlotArea { get; private set; }
            public double MaxFloorAreaRatio { get; private set; }
            public double MaxBuildingCoverage { get; private set; }
            public int MaxFloors { get; private set; }
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
    }
}
