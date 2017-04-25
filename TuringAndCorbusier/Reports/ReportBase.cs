using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;

namespace TuringAndCorbusier
{
    public class ReportBase : System.Windows.Window
    {
        protected ReportBase()
        {

        }


        public virtual Grid ExportContent()
        {
            object content = this.Content;
            RemoveLogicalChild(this.Content);

            Grid output = new Grid();
            output.Width = 1240;
            output.Height = 1750;

            output.Children.Add((System.Windows.UIElement)content);

            return output;
        }
    }
}
