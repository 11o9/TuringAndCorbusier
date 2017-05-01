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
using TuringAndCorbusier.Datastructure_Settings;
using Rhino.Geometry;
using TuringAndCorbusier.xaml;
namespace TuringAndCorbusier
{
    /// <summary>
    /// Page2.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class Page2 : Page
    {
        //double s1bak = 0;
        //double s2bak = 0;
        //double s3bak = 0;
        //double s4bak = 0;
        //double s5bak = 0;
        public bool isAG34Valid = true;
        UnitTypeSetting targetsetting = new UnitTypeSetting();
        public string Format(double x)
        {
            double min = Math.Round(x * 0.95,0);
            double max = Math.Round(x * 1.05,0);
            double k = 1.5;
            double mink = Math.Round(x * 0.95 * k, 0);
            double maxk = Math.Round(x * 1.05 * k, 0);
            string formatstring = "전용 면적 : " + min.ToString() + "m2" + " ~ " + max.ToString() + "m2" +
                "             " +
                " 공급 면적 : " + mink.ToString() + "m2" + " ~ " + maxk.ToString() + "m2";

            return formatstring;
        }

        public Page2()
        {
            InitializeComponent();
            stackpanelslot.Content = targetsetting;
 
            Disable34();
        }

        public void Disable34()
        {
            //Btn_AG3.Opacity = 0.4;
            //Toggle_AG3.IsChecked = true;

            //ag3errorMsg.Text = "현재 수정 중입니다.";
            //ag3errorMsg.Visibility = Visibility.Visible;
            //ag3errorMsg.IsEnabled = true;
            //Btn_AG3.Click -= Btn_AG3_Click;

            //Toggle_AG3.IsEnabled = false;
            //Btn_AG3.IsEnabled = false;


            //Btn_AG4.Opacity = 0.4;
            //Toggle_AG4.IsChecked = true;


            //ag4errorMsg.Text = "현재 수정 중입니다.";
            //ag4errorMsg.Visibility = Visibility.Visible;
            //ag4errorMsg.IsEnabled = true;

            //Toggle_AG4.IsEnabled = false;
            //Btn_AG4.IsEnabled = false;
        }

        private void SliderValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        { 

        }
        private void Btn_ToNext_Click(object sender, RoutedEventArgs e)
        {
            bool[] whichAgToUse = { !Toggle_AG1.IsChecked.Value, !Toggle_AG3.IsChecked.Value, !Toggle_AG4.IsChecked.Value };

            List<double> targetArea = new List<double>();
            List<double> targetRatio = new List<double>();

            //targetsetting.UnitTypeVM.SetPercentages();

            foreach (var x in targetsetting.UnitTypeVM.UnitTypes)
            {
                if (x == null)
                    continue;

                if (x.RelativeValue.Contains(" %"))
                {
                    targetArea.Add(x.MaxArea);
                    targetRatio.Add(double.Parse(x.RelativeValue.Replace(" %", "")) / 100);
                }
                else
                {
                    targetArea.Add(x.MaxArea);
                    targetRatio.Add(0);
                }
            }

            Target target;

            if (targetArea.Count != 0)
            {
                target = new Target(targetArea, targetRatio);
            }
            else
            {
                target = new Target();
            }

            //double[] directions = { Direction1.Value, Direction2.Value };
            double[] directions = { 0, 1 };
            List<double> directionList = directions.ToList();
            directionList.Sort();

            //double[] storiesArray = { Stories1.Value, Stories2.Value };
            double[] storiesArray = { 0, 1 };
            List<double> storiesList = storiesArray.ToList();
            storiesList.Sort();

            TuringAndCorbusierPlugIn.InstanceClass.page2Settings = new Settings_Page2(whichAgToUse.ToList(), target, new Interval(directionList[0], directionList[1]), new Interval(storiesList[0], storiesList[1]), !UndergroundParking_Button.IsChecked.Value);

            GC.Collect();

            NavigationService.Navigate(TuringAndCorbusierPlugIn.InstanceClass.page3);
        }

        private void UndergroundParking_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as System.Windows.Controls.Primitives.ToggleButton).IsChecked.Value)
            {
                (sender as System.Windows.Controls.Primitives.ToggleButton).IsChecked = true;
                UndergroundParking_Name.Foreground = System.Windows.Media.Brushes.DimGray;
                UndergroundParking_Text.Foreground = System.Windows.Media.Brushes.DimGray;
                return;
            }
            else
            {
                (sender as System.Windows.Controls.Primitives.ToggleButton).IsChecked = false;
                UndergroundParking_Name.Foreground = System.Windows.Media.Brushes.Black;
                UndergroundParking_Text.Foreground = System.Windows.Media.Brushes.Black;
                return;
            }
        }

        private void Btn_AG1_Click(object sender, RoutedEventArgs e)
        {
            if (Btn_AG1.Opacity == 0)
            {
                Btn_AG1.Opacity = 0.4;
                Toggle_AG1.IsChecked = true;
                return;
            }
            else
            {
                Btn_AG1.Opacity = 0;
                Toggle_AG1.IsChecked = false;
                return;
            }
        }

        private void Toggle_AG1_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as System.Windows.Controls.Primitives.ToggleButton).IsChecked.Value)
            {
                (sender as System.Windows.Controls.Primitives.ToggleButton).IsChecked = true;
                Btn_AG1.Opacity = 0.4;
                return;
            }
            else
            {
                (sender as System.Windows.Controls.Primitives.ToggleButton).IsChecked = false;
                Btn_AG1.Opacity = 0;
                return;
            }
        }

        private void Btn_AG2_Click(object sender, RoutedEventArgs e)
        {
            /*
            if (Btn_AG2.Opacity == 0)
            {
                Btn_AG2.Opacity = 0.4;
                Toggle_AG2.IsChecked = true;
                return;
            }
            else
            {
                Btn_AG2.Opacity = 0;
                Toggle_AG2.IsChecked = false;
                return;
            }
            */
        }

        private void Toggle_AG2_Click(object sender, RoutedEventArgs e)
        {
            /*
            if ((sender as System.Windows.Controls.Primitives.ToggleButton).IsChecked.Value)
            {
                (sender as System.Windows.Controls.Primitives.ToggleButton).IsChecked = true;
                Btn_AG2.Opacity = 0.4;
                return;
            }
            else
            {
                (sender as System.Windows.Controls.Primitives.ToggleButton).IsChecked = false;
                Btn_AG2.Opacity = 0;
                return;
            }
            */
        }

        public void Btn_AG3_Click(object sender, RoutedEventArgs e)
        {
            if (Btn_AG3.Opacity == 0)
            {
                Btn_AG3.Opacity = 0.4;
                Toggle_AG3.IsChecked = true;
                return;
            }
            else
            {
                Btn_AG3.Opacity = 0;
                Toggle_AG3.IsChecked = false;
                return;
            }
        }

        public void Toggle_AG3_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as System.Windows.Controls.Primitives.ToggleButton).IsChecked.Value)
            {
                (sender as System.Windows.Controls.Primitives.ToggleButton).IsChecked = true;
                Btn_AG3.Opacity = 0.4;
                return;
            }
            else
            {
                (sender as System.Windows.Controls.Primitives.ToggleButton).IsChecked = false;
                Btn_AG3.Opacity = 0;
                return;
            }
        }

        public void Btn_AG4_Click(object sender, RoutedEventArgs e)
        {
            if (Btn_AG4.Opacity == 0)
            {
                Btn_AG4.Opacity = 0.4;
                Toggle_AG4.IsChecked = true;
                return;
            }
            else
            {
                Btn_AG4.Opacity = 0;
                Toggle_AG4.IsChecked = false;
                return;
            }

        }

        public void Toggle_AG4_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as System.Windows.Controls.Primitives.ToggleButton).IsChecked.Value)
            {
                (sender as System.Windows.Controls.Primitives.ToggleButton).IsChecked = true;
                Btn_AG4.Opacity = 0.4;
                return;
            }
            else
            {
                (sender as System.Windows.Controls.Primitives.ToggleButton).IsChecked = false;
                Btn_AG4.Opacity = 0;
                return;
            }

        }

        private void Btn_AG5_Click(object sender, RoutedEventArgs e)
        {
            /*
            if (Btn_AG5.Opacity == 0)
            {
                Btn_AG5.Opacity = 0.4;
                Toggle_AG5.IsChecked = true;
                return;
            }
            else
            {
                Btn_AG5.Opacity = 0;
                Toggle_AG5.IsChecked = false;
                return;
            }
            */
        }

        private void Toggle_AG5_Click(object sender, RoutedEventArgs e)
        {
            /*
            if ((sender as System.Windows.Controls.Primitives.ToggleButton).IsChecked.Value)
            {
                (sender as System.Windows.Controls.Primitives.ToggleButton).IsChecked = true;
                Btn_AG5.Opacity = 0.4;
                return;
            }
            else
            {
                (sender as System.Windows.Controls.Primitives.ToggleButton).IsChecked = false;
                Btn_AG5.Opacity = 0;
                return;
            }
            */

        }

        //private void Toggle_30m_Click(object sender, RoutedEventArgs e)
        //{
        //    if ((sender as System.Windows.Controls.Primitives.ToggleButton).IsChecked.Value)
        //    {
        //        (sender as System.Windows.Controls.Primitives.ToggleButton).IsChecked = true;
        //        TextBlock_30m.Foreground = System.Windows.Media.Brushes.DimGray;
        //        Slider_30m.IsEnabled = false;
        //        Explanation_30m.Foreground = System.Windows.Media.Brushes.DimGray; ;
        //        s1bak = Slider_30m.Value;
        //        Slider_30m.Value = 0;
        //        return;
        //    }
        //    else
        //    {
        //        (sender as System.Windows.Controls.Primitives.ToggleButton).IsChecked = false;
        //        TextBlock_30m.Foreground = System.Windows.Media.Brushes.Black;
        //        Slider_30m.IsEnabled = true;
        //        Explanation_30m.Foreground = System.Windows.Media.Brushes.Black;
        //        Slider_30m.Value = s1bak;
        //        return;
        //    }

        //}
        //private void Toggle_50m_Click(object sender, RoutedEventArgs e)
        //{
        //    if ((sender as System.Windows.Controls.Primitives.ToggleButton).IsChecked.Value)
        //    {
        //        (sender as System.Windows.Controls.Primitives.ToggleButton).IsChecked = true;
        //        TextBlock_50m.Foreground = System.Windows.Media.Brushes.DimGray;
        //        Slider_50m.IsEnabled = false;
        //        Explanation_50m.Foreground = System.Windows.Media.Brushes.DimGray; ;
        //        s2bak = Slider_50m.Value;
        //        Slider_50m.Value = 0;
        //        return;
        //    }
        //    else
        //    {
        //        (sender as System.Windows.Controls.Primitives.ToggleButton).IsChecked = false;
        //        TextBlock_50m.Foreground = System.Windows.Media.Brushes.Black;
        //        Slider_50m.IsEnabled = true;
        //        Explanation_50m.Foreground = System.Windows.Media.Brushes.Black;
        //        Slider_50m.Value = s2bak;
        //        return;
        //    }
        //}
        //private void Toggle_70m_Click(object sender, RoutedEventArgs e)
        //{
        //    if ((sender as System.Windows.Controls.Primitives.ToggleButton).IsChecked.Value)
        //    {
        //        (sender as System.Windows.Controls.Primitives.ToggleButton).IsChecked = true;
        //        TextBlock_70m.Foreground = System.Windows.Media.Brushes.DimGray;
        //        Slider_70m.IsEnabled = false;
        //        Explanation_70m.Foreground = System.Windows.Media.Brushes.DimGray;
        //        s3bak = Slider_70m.Value;
        //        Slider_70m.Value = 0;
        //        return;
        //    }
        //    else
        //    {
        //        (sender as System.Windows.Controls.Primitives.ToggleButton).IsChecked = false;
        //        TextBlock_70m.Foreground = System.Windows.Media.Brushes.Black;
        //        Slider_70m.IsEnabled = true;
        //        Explanation_70m.Foreground = System.Windows.Media.Brushes.Black;
        //        Slider_70m.Value = s3bak;
        //        return;
        //    }

        //}
        //private void Toggle_85m_Click(object sender, RoutedEventArgs e)
        //{
        //    if ((sender as System.Windows.Controls.Primitives.ToggleButton).IsChecked.Value)
        //    {
        //        (sender as System.Windows.Controls.Primitives.ToggleButton).IsChecked = true;
        //        TextBlock_85m.Foreground = System.Windows.Media.Brushes.DimGray;
        //        Slider_85m.IsEnabled = false;
        //        Explanation_85m.Foreground = System.Windows.Media.Brushes.DimGray;
        //        s4bak = Slider_85m.Value;
        //        Slider_85m.Value = 0;
        //        return;
        //    }
        //    else
        //    {
        //        (sender as System.Windows.Controls.Primitives.ToggleButton).IsChecked = false;
        //        TextBlock_85m.Foreground = System.Windows.Media.Brushes.Black;
        //        Slider_85m.IsEnabled = true;
        //        Explanation_85m.Foreground = System.Windows.Media.Brushes.Black;
        //        Slider_85m.Value = s4bak;
        //        return;
        //    }
        //}
        //private void Toggle_103m_Click(object sender, RoutedEventArgs e)
        //{
        //    if ((sender as System.Windows.Controls.Primitives.ToggleButton).IsChecked.Value)
        //    {
        //        (sender as System.Windows.Controls.Primitives.ToggleButton).IsChecked = true;
        //        TextBlock_103m.Foreground = System.Windows.Media.Brushes.DimGray;
        //        Slider_103m.IsEnabled = false;
        //        Explanation_103m.Foreground = System.Windows.Media.Brushes.DimGray;
        //        s5bak = Slider_103m.Value;
        //        Slider_103m.Value = 0;
        //        return;
        //    }
        //    else
        //    {
        //        (sender as System.Windows.Controls.Primitives.ToggleButton).IsChecked = false;
        //        TextBlock_103m.Foreground = System.Windows.Media.Brushes.Black;
        //        Slider_103m.IsEnabled = true;
        //        Explanation_103m.Foreground = System.Windows.Media.Brushes.Black;
        //        Slider_103m.Value = s5bak;
        //        return;
        //    }
        //}

        private void Button_Close_Click(object sender, RoutedEventArgs e)
        {
            TuringAndCorbusierPlugIn.InstanceClass.page3.DisableConduit();
            TuringAndCorbusierPlugIn.InstanceClass.navigationHost.Hide();
        }

        private void Slider_30m_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {

        }

    }
}
