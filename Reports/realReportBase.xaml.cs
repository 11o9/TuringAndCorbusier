using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace TuringAndCorbusier
{
    /// <summary>
    /// realReportBase.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class realReportBase : Window
    {

        public Grid ExportContent()
        {
            RemoveLogicalChild(this.Content);

            Grid output = new Grid();
            output.Width = 1240;
            output.Height = 1750;

            output.Children.Add((System.Windows.UIElement)this.Content);

            return output;
        }
    }
}
