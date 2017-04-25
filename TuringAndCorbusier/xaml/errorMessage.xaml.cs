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
    /// errorMessage.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class errorMessage : Window
    {
        public errorMessage(string message)
        {
            InitializeComponent();
            this.ErrorMessage.Text = message;
            this.Left = TuringAndCorbusierPlugIn.InstanceClass.navigationHost.Left + TuringAndCorbusierPlugIn.InstanceClass.navigationHost.Width / 2 - this.Width / 2;
            this.Top = TuringAndCorbusierPlugIn.InstanceClass.navigationHost.Top + TuringAndCorbusierPlugIn.InstanceClass.navigationHost.Height / 2 - this.Height / 2;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }
    }
}
