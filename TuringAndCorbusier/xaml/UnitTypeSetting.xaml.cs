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
    /// UnitTypeSetting.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class UnitTypeSetting : UserControl
    {
        UnitTypeViewModel VM = new UnitTypeViewModel();
        public UnitTypeViewModel UnitTypeVM { get { return VM; } }
        public UnitTypeSetting()
        {
            InitializeComponent();
            stackpanel.SetVerticalOffset(50);
            VM.setrelativefinished += delegate () { stackpanel.Children.Cast<object>().ToList().Where(n => n is UnitTypeButton).Select(n=>n as UnitTypeButton).Where(n=>n!=null && n.Visibility == Visibility.Visible).ToList().ForEach(n => n.percentage.Text = n.Unittype.RelativeValue); };
        }

        private void toadd_Click(object sender, RoutedEventArgs e)
        {

            bool cancled = false;

            UnitTypeButton tempbutton = new UnitTypeButton();
            
            tempbutton.removethisCallBack += (UnitTypeButton button) => { stackpanel.Children.Remove(button); VM.UnitTypes.Remove(button.Unittype); };
            UnitTypeInputBox typeinput = new UnitTypeInputBox();
            Window win = new Window();
            var p = System.Windows.Forms.Control.MousePosition;
            //var p2 = 
            win.Left = p.X;
            win.Top = p.Y;
            
            win.Content = typeinput;
            win.WindowStyle = WindowStyle.None;
            win.SizeToContent = SizeToContent.WidthAndHeight;
            win.BorderThickness = new Thickness(0);
            win.Topmost = true;
            typeinput.CallBack += (string n) => { if (n == "Cancle") { win.Close(); cancled = true; return n; } if (n == "") n = "59"; tempbutton.Unittype = new UnitType(double.Parse(n)); tempbutton.Unittype.ScrollValue = 1; win.Close(); return n; };
            win.ShowDialog();

            if (cancled)
                return;

            int insertindex = GetInsertIndex(tempbutton.Unittype.Area);

            stackpanel.Children.Insert(insertindex, tempbutton);

            VM.UnitTypes = stackpanel.Children.Cast<object>().ToList().Where(n=>n is UnitTypeButton).Select(n=>n as UnitTypeButton).Where(n=>n.Visibility != Visibility.Collapsed).Select(n =>( n as UnitTypeButton).Unittype).Where(n=>n!=null).ToList();
            tempbutton.valueChangedCallBack += VM.SetPercentages;
            VM.SetPercentages();
        }

        private int GetInsertIndex(double area)
        {

            VM.UnitTypes = VM.UnitTypes.OrderBy(n => n.Area).ToList();
            for (int i = 0; i < VM.UnitTypes.Count; i++)
            {
                if (area < VM.UnitTypes[i].Area)
                    return i;
            }

            return VM.UnitTypes.Count;
        }
    }


    public class UnitTypeViewModel
    {
        public delegate void SetRelativeValueFinished();
        public SetRelativeValueFinished setrelativefinished = null;

        List<UnitType> unittypes = new List<UnitType>();
        public List<UnitType> UnitTypes { get { return unittypes; } set { unittypes = value; } }
        public UnitTypeViewModel()
        {

        }

        public void SetPercentages()
        {
            //현재값 합
            double tempSum = 0; 
            foreach (var ut in unittypes)
            {
                if (ut == null)
                    continue;
                tempSum += ut.ScrollValue;
            }
            //상대비율
            foreach (var ut in unittypes)
            {
                if (ut == null)
                    continue;
                ut.RelativeValue = Math.Round((ut.ScrollValue / tempSum)*100).ToString() + " %";
            }

            setrelativefinished?.Invoke();
        }
    }

    
}
