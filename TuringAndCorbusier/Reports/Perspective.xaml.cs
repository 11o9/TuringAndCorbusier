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
using Rhino;

namespace Reports
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class Perspective
    {
        string imageDirectory = "";
        public Perspective()
        {
            bool is64 = Environment.Is64BitOperatingSystem;
            DirectoryInfo dirInfo = null;
            string dir64 = "C://Program Files (x86)//Boundless//TuringAndCorbusier//DataBase//temp//";
            string dir32 = "C://Program Files//Boundless//TuringAndCorbusier//DataBase//temp//";

            if (is64)
                dirInfo = new DirectoryInfo(dir64);
            else
                dirInfo = new DirectoryInfo(dir32);

            if (!dirInfo.Exists)
                dirInfo.Create();

            imageDirectory = dirInfo.FullName;
            RhinoApp.WriteLine(imageDirectory.ToString());
            InitializeComponent();

        }
        public bool setImage1(string imageFilePath)
        {
            try
            {
                FileStream fileStream = new FileStream(imageDirectory + imageFilePath, FileMode.Open);
                System.Drawing.Bitmap newImage = new System.Drawing.Bitmap(fileStream);


                MemoryStream memoryStream = new MemoryStream();
                newImage.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Tiff);

                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = new MemoryStream(memoryStream.ToArray());
                bitmapImage.EndInit();

                perspective1.Source = bitmapImage;


                newImage.Dispose();
                fileStream.Dispose();
                fileStream.Close();
                memoryStream.Close();
                memoryStream.Dispose();

                Rhino.RhinoApp.WriteLine("perspectiveView done");

            }
            catch (Exception e)
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
                FileStream fileStream = new FileStream(imageDirectory + imageFilePath, FileMode.Open);
                System.Drawing.Bitmap newImage = new System.Drawing.Bitmap(fileStream);


                MemoryStream memoryStream = new MemoryStream();
                newImage.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Png);

                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = new MemoryStream(memoryStream.ToArray());
                bitmapImage.EndInit();

                perspective2.Source = bitmapImage;


                newImage.Dispose();
                fileStream.Dispose();
                fileStream.Close();
                memoryStream.Close();
                memoryStream.Dispose();

                Rhino.RhinoApp.WriteLine("perspectiveView done");

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