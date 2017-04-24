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

namespace TuringAndCorbusier.xaml
{
    /// <summary>
    /// UnitTypeInputBox.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class UnitTypeInputBox : UserControl
    {
        public Func<string, string> CallBack = null;

        public UnitTypeInputBox()
        {
            InitializeComponent();
            result.Focus();
        }

        private void button_ok_Click(object sender, RoutedEventArgs e)
        {
            CallBack?.Invoke(result.Text);
        }

        private void result_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                CallBack?.Invoke(result.Text);
        }
    }
}
