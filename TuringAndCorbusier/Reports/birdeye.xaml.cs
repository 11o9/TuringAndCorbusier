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
using System.IO;
namespace Reports
{
    /// <summary>
    /// xmlcover.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class ImagePage : UserControl,IDisposable
    {
        string imagedir = "";


        public ImagePage()
        {
            bool is64 = Environment.Is64BitOperatingSystem;
            DirectoryInfo dirinfo = null;
            string dir64 = "C://Program Files (x86)//Boundless//TuringAndCorbusier//DataBase//temp//";
            string dir32 = "C://Program Files//Boundless//TuringAndCorbusier//DataBase//temp//";
            
            if (is64)
                dirinfo = new DirectoryInfo(dir64);
            else
                dirinfo = new DirectoryInfo(dir32);

            if (!dirinfo.Exists)
                dirinfo.Create();

            imagedir = dirinfo.FullName;

            InitializeComponent();
            
        }

        public void Dispose()
        {
            
            image.Source = null;
            image1.Source = null;
        }

        public bool setImage1(string imageFilePath)
        {
            try
            {

                FileStream fs = new FileStream(imagedir + "test0.jpeg",FileMode.Open);
                System.Drawing.Bitmap newimg = new System.Drawing.Bitmap(fs);

              
                MemoryStream ms = new MemoryStream();
                newimg.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);

                BitmapImage bimg = new BitmapImage();
                bimg.BeginInit();
                bimg.StreamSource = new MemoryStream(ms.ToArray());
                bimg.EndInit();

                image.Source = bimg;


                newimg.Dispose();
                fs.Dispose();
                fs.Close();
                ms.Close();
                ms.Dispose();

                Rhino.RhinoApp.WriteLine("birdeye1 done");

            }
            catch(Exception e)
            {
                MessageBox.Show(e.ToString());
                return false;
            }

            return true;
        }

        public bool setImage2(string imageFilePath)
        {
            try
            {

                //BitmapImage img = new BitmapImage(new Uri(imagedir + "test1.jpeg"));
                //image1.Source = img;

                //Rhino.RhinoApp.WriteLine("birdeye2 done");
                FileStream fs = new FileStream(imagedir + "test1.jpeg", FileMode.Open);
                System.Drawing.Bitmap newimg = new System.Drawing.Bitmap(fs);

                MemoryStream ms = new MemoryStream();
                newimg.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);

                BitmapImage bimg = new BitmapImage();
                bimg.BeginInit();
                bimg.StreamSource = new MemoryStream(ms.ToArray());
                bimg.EndInit();

                image1.Source = bimg;

                newimg.Dispose();
                fs.Dispose();
                fs.Close();
                ms.Close();
                ms.Dispose();
                Rhino.RhinoApp.WriteLine("birdeye2 done");


            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
                return false;
            }

            return true;
        }

    }
}
