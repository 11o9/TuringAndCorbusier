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
    /// FindForm.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class FindForm : Window
    {

        List<Rhino.DocObjects.RhinoObject> objects = new List<Rhino.DocObjects.RhinoObject>();
        Rhino.RhinoDoc doc = null;
        int index = 0;
        
        public FindForm(List<Rhino.DocObjects.RhinoObject> input, Rhino.RhinoDoc _doc)
        {
            InitializeComponent();
            objects = input;
            doc = _doc;
            setfocustoitem(index);
        }

        public void setfocustoitem(int _index)
        {

            Guid selectedId = objects[_index].Id;



            doc.Objects.Select(selectedId);
            
            doc.Views.ActiveView.ActiveViewport.ZoomExtentsSelected();
            doc.Views.ActiveView.Redraw();
            MessageBox.Show("좀되거라" + _index.ToString() + selectedId);

        }



        private void button_Click(object sender, RoutedEventArgs e)
        {
            index = (index+ objects.Count - 1) % objects.Count;
            setfocustoitem(index);
        }

        private void button_Copy_Click(object sender, RoutedEventArgs e)
        {
            index = (index + 1) % objects.Count;
            setfocustoitem(index);
        }

        private void comboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            index = comboBox.Items.IndexOf(comboBox.SelectedItem);
            setfocustoitem(index);
        }

        private void button_Copy1_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
