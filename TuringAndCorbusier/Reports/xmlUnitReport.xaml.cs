using System;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Shapes;
using Rhino.Geometry;
using TuringAndCorbusier;
using System.Linq;

namespace Reports
{
    /// <summary>
    /// Window1.xaml에 대한 상호 작용 논리
    /// </summary>\
    /// 

    public partial class xmlUnitReport
    {

        public xmlUnitReport()
        {
            InitializeComponent();
            
            
        }

        public void SetFirstUnitTypePlan(Reports.unitPlanTemplate singleUnitTemplate)
        {
            unitPlanCanvasControl1.Content = singleUnitTemplate;
        }
        public void SetUnitTypePlan(Reports.unitPlanTemplate planForCanvas1, Reports.unitPlanTemplate planForCanvas2)
        {

            unitPlanCanvasControl1.Content = planForCanvas1;

            unitPlanCanvasControl2.Content = planForCanvas2;
        }

        public static System.Windows.Shapes.Rectangle GetCanvasRectangle()
        {
            System.Windows.Shapes.Rectangle tempRectangle = new System.Windows.Shapes.Rectangle();
            tempRectangle.Width = 940;
            tempRectangle.Height = 750;
            return tempRectangle;
        }
    }
}
