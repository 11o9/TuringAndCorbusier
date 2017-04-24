using System.Collections.Generic;
using Rhino.Input.Custom;
using System;
using System.Linq;
using System.Collections;
using TuringAndCorbusier;

namespace TuringAndCorbusier
{
    public class ConduitLine
    {
        public ConduitLine(Rhino.Geometry.Line inputline)
        {
            RoadWidth = 4;
            Color = CommonFunc.colorSetting.GetColorFromValue((double)1 - (4 / 20));
            line = inputline;
        }

        public ConduitLine(Rhino.Geometry.Line inputline, int roadWidth)
        {
            this.RoadWidth = roadWidth;
            Color = CommonFunc.colorSetting.GetColorFromValue((double)1 - (roadWidth / 20));
            line = inputline;
        }

        public System.Drawing.Color Color { get; set; }
        public int RoadWidth { get; set; }
        public Rhino.Geometry.Line line { get; set; }
    }

    public class GetConduitLine : GetPoint
    {
        private List<ConduitLine> tempLines;

        private int[] roadWidth = { 0, 4, 8, 10 };

        public GetConduitLine(List<ConduitLine> conduitLines)
        {
            tempLines = conduitLines;
        }

        protected override void OnMouseDown(GetPointMouseEventArgs e)
        {
            base.OnMouseDown(e);
            var picker = new PickContext();
            picker.View = e.Viewport.ParentView;

            picker.PickStyle = PickStyle.PointPick;

            var xform = e.Viewport.GetPickTransform(e.WindowPoint);
            picker.SetPickTransform(xform);

            foreach (var cl in tempLines)
            {
                double depth;
                double distance;
                double t;
                if (picker.PickFrustumTest(cl.line, out t, out depth, out distance))
                {
                    int index = this.roadWidth.ToList().IndexOf(cl.RoadWidth);
                    int index_Next = (index + 1 + roadWidth.Count()) % roadWidth.Count();

                    cl.RoadWidth = this.roadWidth[index_Next];
                    cl.Color = CommonFunc.colorSetting.GetColorFromValue((double)1 - (this.roadWidth[index_Next] / 20));
                    Corbusier.RoadWidth[tempLines.IndexOf(cl)] = roadWidth[index_Next];
                }
            }
        }
    }

    public class LinesConduit : Rhino.Display.DisplayConduit
    {
        private List<ConduitLine> tempLines;

        public LinesConduit(List<ConduitLine> conduitLines)
        {
            tempLines = conduitLines;
        }

        protected override void DrawForeground(Rhino.Display.DrawEventArgs e)
        {
            if (tempLines != null)
                foreach (var cl in tempLines)
                {
                    e.Display.DrawLine(cl.line, cl.Color, cl.RoadWidth + 1);
                    e.Display.Draw2dText(cl.RoadWidth.ToString() + "m 도로", System.Drawing.Color.White, cl.line.PointAt(0.5) + new Rhino.Geometry.Point3d(1000, 1000, 0), true, 15);
                }
        }
    }
}
