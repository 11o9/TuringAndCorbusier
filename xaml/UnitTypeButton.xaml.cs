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
        Binding binding = new Binding("RelativeValue");
        UnitType unittype;
        public UnitType Unittype { get { return unittype; } set { unittype = value; name.Text = unittype.Name; } }

        public UnitTypeButton()
        {
            InitializeComponent();
            Input.Text = "1";
        }

        public void SetUnitType(string value)
        {
            unittype = new WeakReference(new UnitType((double)int.Parse(value))).Target as UnitType;
            binding.Source = Unittype;
            percentage.SetBinding(TextBlock.TextProperty, binding);
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
        double maxarea;
        double scrollvalue;
        string rvalue;

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string info)
        {
            //scrollvalue = 1;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(info));
        }
  
        public double MaxArea { get { return maxarea; } }
        public string Name { get{ return name; } }
        public double ScrollValue { get { return scrollvalue; } set { scrollvalue = value; } }
        public string RelativeValue { get { return rvalue; } set { rvalue = value; OnPropertyChanged("RelativeValue"); } }
        public UnitType(double area)
        {
            name = area.ToString() + " m2 형";
            //평 -> m2
            maxarea = area;
        }

      
    }

}
