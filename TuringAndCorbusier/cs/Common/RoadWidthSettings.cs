using System.Collections.Generic;
using Rhino.Input.Custom;
using System;
using System.Linq;
using System.Collections;
using TuringAndCorbusier;
using Rhino.Display;

namespace TuringAndCorbusier
{
    public class ConduitLine
    {
        public ConduitLine(Rhino.Geometry.Line inputline)
        {
            RoadWidth = 4;
            Color = CommonFunc.colorSetting.GetColorFromValue(RoadWidth);
            line = inputline;
        }

        public ConduitLine(Rhino.Geometry.Line inputline, int roadWidth)
        {
            this.RoadWidth = roadWidth/1000;
            Color = CommonFunc.colorSetting.GetColorFromValue(RoadWidth);
            line = inputline;
        }
        public System.Drawing.Color Color { get; set; }
        public int RoadWidth { get; set; }
        public Rhino.Geometry.Line line { get; set; }
    }

    public class GetConduitLine : GetPoint
    {
        private List<ConduitLine> tempLines;
        private int[] roadWidth = { 0, 3, 4, 6, 8, 10, 12, 16 , 20 ,21};
 
        public GetConduitLine(List<ConduitLine> conduitLines)
        {
            tempLines = conduitLines;


            ///ctrl 누르고 클릭 시 z축 elevation 막음
            this.PermitElevatorMode(0);
        }

        

        protected override void OnMouseDown(GetPointMouseEventArgs e)
        {

            //control 키 누른채로 입력시 감소
            int direction = 1;
            if (e.ControlKeyDown)
                direction = -1;

            var picker = new PickContext();
            picker.View = e.Viewport.ParentView;

            picker.PickStyle = PickStyle.PointPick;

            var xform = e.Viewport.GetPickTransform(e.WindowPoint);
            picker.SetPickTransform(xform);

            for (int i = 0; i < tempLines.Count; i++)
            {
                double depth;
                double distance;
                double t;
                

                //shift 키 누른채로 입력 시 모든 선에 적용
                if (e.ShiftKeyDown)
                {
                    int index = this.roadWidth.ToList().IndexOf(tempLines[i].RoadWidth);
                    int index_Next = (index + direction + roadWidth.Count()) % roadWidth.Count();

                    tempLines[i].RoadWidth = this.roadWidth[index_Next];
                    tempLines[i].Color = CommonFunc.colorSetting.GetColorFromValue(this.roadWidth[index_Next]);
                    TuringAndCorbusierPlugIn.InstanceClass.plot.Surroundings[tempLines.IndexOf(tempLines[i])] = roadWidth[index_Next];
                }
                else
                {

                    //가까운 선에만 적용
                    if (picker.PickFrustumTest(tempLines[i].line, out t, out depth, out distance))
                    {
                        int index = this.roadWidth.ToList().IndexOf(tempLines[i].RoadWidth);
                        int index_Next = (index + direction + roadWidth.Count()) % roadWidth.Count();

                        tempLines[i].RoadWidth = this.roadWidth[index_Next];
                        tempLines[i].Color = CommonFunc.colorSetting.GetColorFromValue(this.roadWidth[index_Next]);
                        TuringAndCorbusierPlugIn.InstanceClass.plot.Surroundings[tempLines.IndexOf(tempLines[i])] = roadWidth[index_Next];

                        
                    }
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
            e.Display.DepthMode = DepthMode.AlwaysInBack;
            if (tempLines != null)
                for(int i = 0; i < tempLines.Count;i++)
                {
                    e.Display.DrawLine(tempLines[i].line, tempLines[i].Color, tempLines[i].RoadWidth + 1);
                }
        }

        protected override void PostDrawObjects(DrawEventArgs e)
        {
            e.Display.DepthMode = DepthMode.AlwaysInFront;
            if (tempLines != null)
                for (int i = 0; i < tempLines.Count; i++)
                {
                    var v = tempLines[i].line.UnitTangent;
                    v.Rotate(Math.PI / 2, Rhino.Geometry.Vector3d.ZAxis);
                    v.Unitize();
                    v *= 2000 + 2000 * tempLines[i].RoadWidth/20;
                    string msg = tempLines[i].RoadWidth.ToString() + "m";
                    if (tempLines[i].RoadWidth == 21)
                        msg = "일조 미적용 경계";
                    e.Display.Draw2dText(msg, tempLines[i].Color, tempLines[i].line.PointAt(0.5) + v, true, 15);
                }
        }
    }
    public class GetSlopePoint : GetPoint
    {
        public GetSlopePoint()
        {

        }

        protected override void OnMouseDown(GetPointMouseEventArgs e)
        {
            base.OnMouseDown(e);
            var picker = new PickContext();
            picker.View = e.Viewport.ParentView;

            picker.PickStyle = PickStyle.PointPick;

            var xform = e.Viewport.GetPickTransform(e.WindowPoint);
            picker.SetPickTransform(xform);



        }
    }
}
