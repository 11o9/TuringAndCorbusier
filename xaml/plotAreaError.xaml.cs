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
    public partial class plotAreaError : Window
    {
        int dialogReturnValue = 0;

        public plotAreaError(string message)
        {
            InitializeComponent();
            this.plotAreaErrorMessage.Text = message;
            this.Left = TuringAndCorbusierPlugIn.InstanceClass.navigationHost.Left + TuringAndCorbusierPlugIn.InstanceClass.navigationHost.Width / 2 - this.Width / 2;
            this.Top = TuringAndCorbusierPlugIn.InstanceClass.navigationHost.Top + TuringAndCorbusierPlugIn.InstanceClass.navigationHost.Height / 2 - this.Height / 2;
        }

        public int showDialogReturnValue()
        {
            this.ShowDialog();

            return dialogReturnValue;
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.dialogReturnValue = 1;
            this.Close();
        }
        private void ScaleToFit_Click(object sender, RoutedEventArgs e)
        {
            this.dialogReturnValue = 2;
            this.Close();
        }

        private void Continue_Click(object sender, RoutedEventArgs e)
        {
            this.dialogReturnValue = 0;
            this.Close();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }
    }
}
