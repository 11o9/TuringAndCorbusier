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
using System.ComponentModel;
namespace TuringAndCorbusier.xaml
{
    /// <summary>
    /// UnitTypeButton.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class UnitTypeButton : UserControl
    {
        public delegate void ValueChangedCallBack();
        public delegate void RemoveThisCallBack(UnitTypeButton button);
         
        public ValueChangedCallBack valueChangedCallBack = null;
        public RemoveThisCallBack removethisCallBack = null;

        Binding rateBinding = new Binding("RelativeValue");
        Binding minBinding = new Binding("MinArea");
        Binding maxBinding = new Binding("MaxArea");
        Binding mandatoryBinding = new Binding("Mandatory");

        UnitType unittype;

        public UnitType Unittype { get { return unittype; } set { unittype = value; SetBinding(); name.Text = unittype.Name; } }

        public UnitTypeButton()
        {
            InitializeComponent();

            Input.Text = "1";

        }

        public void SetBinding()
        {
            rateBinding.Source = Unittype;
            rateBinding.Mode = BindingMode.TwoWay;
            percentage.SetBinding(TextBlock.TextProperty, rateBinding);

            minBinding.Source = Unittype;
            minBinding.Mode = BindingMode.TwoWay;
            minimum_Input.SetBinding(TextBox.TextProperty, minBinding);

            maxBinding.Source = Unittype;
            maxBinding.Mode = BindingMode.TwoWay;
            maximum_Input.SetBinding(TextBox.TextProperty, maxBinding);

            mandatoryBinding.Source = Unittype;
            mandatoryBinding.Mode = BindingMode.TwoWay;
            mandatory_Input.SetBinding(TextBox.TextProperty, mandatoryBinding);
        }

        private void mainButton_Click(object sender, RoutedEventArgs e)
        {
            this.IsEnabled = false;
            this.Visibility = Visibility.Collapsed;
            removethisCallBack?.Invoke(this);
            valueChangedCallBack?.Invoke();
        }

        private void Input_TextChanged(object sender, TextChangedEventArgs e)
        {
            double d;
            if (!double.TryParse(Input.Text, out d))
                Input.Text = "0";
            else
                if(unittype != null)
                    unittype.ScrollValue = d;

            valueChangedCallBack?.Invoke();
        }

        private void minimum_Input_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void maximum_Input_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void mandatory_Input_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        //private void valueslider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        //{
        //    if (unittype == null)
        //        return;
        //    unittype.ScrollValue = valueslider.Value;
        //    valueChangedCallBack?.Invoke();
        //}
    }

    public class UnitType : INotifyPropertyChanged
    {
        string name;
        
        double scrollvalue;

        double area;
        double maxarea;
        double minarea;
        int mandatory;


        string rvalue;


        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string info)
        {
            //scrollvalue = 1;
            Rhino.RhinoApp.WriteLine(info + "has been changed");
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(info));
        }

        public double Area { get { return area; } set { area = value; OnPropertyChanged("Area"); } }
        public double MinArea { get { return minarea; } set { minarea = value; OnPropertyChanged("MinArea"); } }
        public double MaxArea { get { return maxarea; } set { maxarea = value; OnPropertyChanged("MaxArea"); } }
        public int Mandatory { get { return mandatory; } set { mandatory = value; OnPropertyChanged("Mandatory"); } }
        public string Name { get{ return name; } }
        public double ScrollValue { get { return scrollvalue; } set { scrollvalue = value; } }
        public string RelativeValue { get { return rvalue; } set { rvalue = value; OnPropertyChanged("RelativeValue"); } }
        public UnitType(double area)
        {
            name = area.ToString() + " m2 형";
            //평 -> m2
            this.area = area;

            maxarea = area;
            minarea = area;
            mandatory = 1;
        }

      
    }

}
