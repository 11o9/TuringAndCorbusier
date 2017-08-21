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
    /// reportCover.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class ReportCover : UserControl, IDisposable
    {
        string imageDirectory = "";

        public ReportCover()
        {
            //이전에 저장한 이미지 가져오기 위해 이미지 저장되어있는 경로 만들기
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
        }//reportCover

        //지정한 경로에 저장되어있는 이미지 가져와 넣기
        public bool setImage(string imageFilePath)
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

                image.Source = bitmapImage;


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

        //보고서 제목 넣기
        public void SetTitle(string projectNameStr)
        {
            string tempTitle = projectNameStr;
            title.Text = tempTitle + "\n가로주택정비사업 기획설계";
        }
        //보고서 출력 날짜 넣기
        public void SetPublishDate()
        {
            publishDate.Text = DateTime.Now.ToString("yyyy-MM-dd");
        }
        //보고서 표지 정보 넣기
        public void SetCoverBuildingInfo(List<string> infoValueList)
        {
            projectName.Text = String.Format("{0:0.00}",infoValueList[0]);
            address.Text = String.Format("{0:0.00}", infoValueList[1]);
            plotType.Text = String.Format("{0:0.00}", infoValueList[2]);
            plotArea_Usable.Text = String.Format("{0:0.00}", infoValueList[3]);
            buildingCoverage.Text = String.Format("{0:0.00}", infoValueList[4]);
            floorAreaRatio.Text = String.Format("{0:0.00}", infoValueList[5]);
            numOfHouseHolds.Text = String.Format("{0:0.00}", infoValueList[6]);

        }
        //이미지 삭제하기
        public void Dispose()
        {
            image.Source = null;
        }

        private void SetFontFamily()
        {
            FontFamily fontFamily = new FontFamily("Din Alternate");

        }
    }//class
}//namespace
