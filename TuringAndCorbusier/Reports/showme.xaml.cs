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
using System.Windows.Xps;
using System.Windows.Xps.Packaging;
using System.Printing;
using PdfSharp.Pdf;
using PdfSharp.Xps;
using TuringAndCorbusier;
using System.IO;

namespace Reports
{
    /// <summary>
    /// showme.xaml에 대한 상호 작용 논리
    /// </summary>

    public partial class showme : System.Windows.Window
    {


        public string[] CurrentDataIdName = { "REGI_MST_NO", "REGI_SUB_MST_NO" };
        public string[] CurrentDataId = { CommonFunc.getStringFromRegistry("REGI_MST_NO"), CommonFunc.getStringFromRegistry("REGI_SUB_MST_NO") };

        List<FixedPage> tempPagesToShow = new List<FixedPage>();
        


        int index = 0;/// 

        int projectcount = 0;

        string agtype = "";
        string mst_no = CommonFunc.getStringFromRegistry("REGI_MST_NO");
        string mst_sub_no = "_"+CommonFunc.getStringFromRegistry("REGI_SUB_MST_NO");

        string projectname = "_Project";
        string pdfpath = "pdf\\";
        string pdfoutname = "_Final";
        string xpspath = "xps\\";
        string root = "Export";
        string projectpath = "";
        List<string> _pagename = new List<string>();
        List<string> _pageurls = new List<string>();
        List<string> _pdfurls = new List<string>();
        

        bool saveas = false;
        


        public showme ()
        {
            InitializeComponent();    
        }

        

        public Dictionary<string,string> showmeinit(List<FixedPage> pages, List<string> pagename , string _projectname, Apartment agoutput, ref List<Dictionary<string,string>>paths , int index)
        {

            Dictionary<string, string> result = new Dictionary<string, string>();
            agtype = agoutput.AGtype;

            projectcount = 0;
            projectname = _projectname;
            pdfpath = "pdf\\";
            pdfoutname = "_Final";
            xpspath = "xps\\";

            
            tempPagesToShow.Clear();

            _pdfurls.Clear();
            _pagename.Clear();
            _pageurls.Clear();

            if (!Directory.Exists(root))
                Directory.CreateDirectory(root);


            ///Export\\mst_no+mst_sub_no+projectname+agtype 경로 생성

            pdfoutname = mst_no + mst_sub_no + "_" + projectname + "_" + agtype;
            bool exist = Directory.Exists(root + "\\" + pdfoutname + "*");

            if (exist)
            {
                DirectoryInfo[] directories = new DirectoryInfo(root).GetDirectories(pdfoutname + "*", SearchOption.TopDirectoryOnly);

                projectcount = directories.Length / 2;
            }
           


            //같은 경로/타입 폴더 이미 존재?!
            if (exist)
            {
                var msgBoxResult = MessageBox.Show("등록된 같은 타입의 보고서가 있습니다. 덮어 쓰시겠습니까?", "보고서 덮어쓰기", MessageBoxButton.OKCancel);
                if (msgBoxResult == MessageBoxResult.Cancel)
                {
                    saveas = true;
                }
                else
                {
                    saveas = false;
                    
                }
            }

                projectpath = root + "\\" + pdfoutname;


            //projectpath = 폴더 경로.




            this.Activate();


            pdfpath = projectpath + "_pdf\\";
            xpspath = projectpath + "_xps\\";

            if (!System.IO.Directory.Exists(pdfpath))
                System.IO.Directory.CreateDirectory(pdfpath);
            
            
            if (!System.IO.Directory.Exists(xpspath))
                System.IO.Directory.CreateDirectory(xpspath);


            string[] files = System.IO.Directory.GetFiles(xpspath);

            files.ToList().AddRange(System.IO.Directory.GetFiles(pdfpath));

            foreach (var file in files)
            {
                try
                {
                    System.IO.File.Delete(file);
                }
                catch (Exception ex)
                {
                    throw ex;
                }

            }

            tempPagesToShow.Clear();
            foreach (var temppage in pages)
            {


                var temp = new FixedPage();
                
                temp = temppage;
                temp.BeginInit();
                temp.EndInit();

                tempPagesToShow.Add(temp);
                
            }
            
            this._pagename = pagename;

            for (int i = 0; i < pages.Count(); i++)
            {
                if (pages[i] != null)
                {
                    if (_pagename[i] == "sectionPage")
                    {
                        if (paths[index].ContainsKey("SECTION"))
                            paths[index].Remove("SECTION");
                        paths[index].Add("SECTION", SaveDocumentPagesToImages(CreateXps(i)));
                        Rhino.RhinoApp.WriteLine(paths[index]["SECTION"]);
                    }
                    else if (_pagename[i] == "typicalPlanPage1")
                    {
                        if (paths[index].ContainsKey("GROUND_PLAN"))
                            paths[index].Remove("GROUND_PLAN");
                        paths[index].Add("GROUND_PLAN", SaveDocumentPagesToImages(CreateXps(i)));
                        Rhino.RhinoApp.WriteLine(paths[index]["GROUND_PLAN"]);
                    }
                    else if (_pagename[i] == "typicalPlanPage3")
                    {
                        if (paths[index].ContainsKey("TYPICAL_PLAN"))
                            paths[index].Remove("TYPICAL_PLAN");
                        paths[index].Add("TYPICAL_PLAN", SaveDocumentPagesToImages(CreateXps(i)));
                        Rhino.RhinoApp.WriteLine(paths[index]["TYPICAL_PLAN"]);
                    }
                    
                    else
                        CreateXps(i);

                }
            }

            PrintPDF();

            return result;
        }


        //public void openthisinviewer(int index)
        //{
        //    XpsDocument xpsdoc = new XpsDocument(_pageurls[index], System.IO.FileAccess.Read);
        //    this.documentViewer.Document = xpsdoc.GetFixedDocumentSequence();

        //}

        public string SaveDocumentPagesToImages(string dirPath)
        {

            XpsDocument doc = new XpsDocument(dirPath, FileAccess.Read);
            FixedDocumentSequence document = doc.GetFixedDocumentSequence();
            


            string newpath = "";
            MemoryStream[] streams = null;
            try
            {
                int pageCount = document.DocumentPaginator.PageCount ;
                DocumentPage[] pages = new DocumentPage[pageCount];
                for (int i = 0; i < pageCount; i++)
                    pages[i] = document.DocumentPaginator.GetPage(i);

                streams = new MemoryStream[pages.Count()];

                for (int i = 0; i < pages.Count(); i++)
                {
                    DocumentPage source = pages[i];
                    streams[i] = new MemoryStream();

                    RenderTargetBitmap renderTarget =
                       new RenderTargetBitmap((int)source.Size.Width,
                                               (int)source.Size.Height,
                                               96, // WPF (Avalon) units are 96dpi based
                                               96,
                                               System.Windows.Media.PixelFormats.Default);

                    renderTarget.Render(source.Visual);

                    JpegBitmapEncoder encoder = new JpegBitmapEncoder();  // Choose type here ie: JpegBitmapEncoder, etc
                    encoder.Rotation = Rotation.Rotate270;
                    encoder.QualityLevel = 100;
                    encoder.Frames.Add(BitmapFrame.Create(renderTarget));

                    encoder.Save(streams[i]);
                    newpath = dirPath.Replace(".xps", ".jpg");
                    FileStream file = new FileStream(newpath, FileMode.CreateNew);
                    file.Write(streams[i].GetBuffer(), 0, (int)streams[i].Length);
                    file.Close();
                    
                    streams[i].Position = 0;

                }
            }
            catch (Exception e1)
            {
                throw e1;
               
            }
            finally
            {
                if (streams != null)
                {
                    foreach (MemoryStream stream in streams)
                    {
                        stream.Close();
                        stream.Dispose();
                        doc.Close();
                        
                    }
                }
            }

            return newpath;
        }

        public string CreateXps(int i)
        {
            if (i < 0 && i >= tempPagesToShow.ToArray().Length)
                return null;

            FileStream fs = new FileStream(xpspath + _pagename[i] + ".xps", FileMode.OpenOrCreate);

            FixedPage page = tempPagesToShow[i];



            System.IO.Packaging.Package np = System.IO.Packaging.Package.Open(fs, FileMode.OpenOrCreate, FileAccess.ReadWrite);



            XpsDocument xpsdoc = new XpsDocument(np);

      
       
            _pageurls.Add(xpspath + _pagename[i] + ".xps");

            


           

           XpsDocumentWriter docWriter = XpsDocument.CreateXpsDocumentWriter(xpsdoc);
            docWriter.Write(page);

            
            xpsdoc.Close();
            np.Close();
            fs.Dispose();
            fs.Close();
            

            return xpspath + _pagename[i] + ".xps";
        }

        //enum Page
        //{
        //    Next = 0,
        //    Back = 1
        //}
        //private void otherPage(Page page)
        //{
        //    if (page == Page.Back)
        //    {
        //        if (index > 0)
        //            index--;
        //        else
        //            index = tempPagesToShow.ToArray().Length - 1;
        //    }
        //    else if (page == Page.Next)
        //    {
        //        if (index < tempPagesToShow.ToArray().Length - 1)
        //            index++;
        //        else
        //            index = 0;
        //    }
        //    openthisinviewer(index);

        //}

        //private void button_Left_Click(object sender, RoutedEventArgs e)
        //{
        //    otherPage(Page.Back);
        //}

        //private void button_Right_Click(object sender, RoutedEventArgs e)
        //{
        //    otherPage(Page.Next);

        //}

        public void PrintPDF()
        {

            PdfDocument output = new PdfDocument();

            for (int i = 0; i < tempPagesToShow.Count(); i++)
            {

                FileStream fs = new FileStream(_pageurls[i],FileMode.Open);

                var pdfxpsdoc = PdfSharp.Xps.XpsModel.XpsDocument.Open(fs);

                XpsConverter.Convert(pdfxpsdoc, pdfpath + _pagename[i] + ".pdf", 0);

                _pdfurls.Add(pdfpath + _pagename[i] + ".pdf");



                //File.Copy(_pageurls[i], xpspath + "topdf" + (i + 1).ToString() + ".xps", true);

                //var pdfxpsdoc = PdfSharp.Xps.XpsModel.XpsDocument.Open(xpspath + "topdf" + (i + 1).ToString() + ".xps");

                //XpsConverter.Convert(pdfxpsdoc, pdfpath + _pagename[i] + ".pdf", 0);

                //_pdfurls.Add(pdfpath + _pagename[i] + ".pdf");


                pdfxpsdoc.Close();
                fs.Close();
                
            }

            foreach (string _pdfurl in _pdfurls)
            {
                using (PdfDocument input = PdfSharp.Pdf.IO.PdfReader.Open(_pdfurl, PdfSharp.Pdf.IO.PdfDocumentOpenMode.Import))
                {
                    output.Version = input.Version;
                    foreach (PdfPage page in input.Pages)
                    {
                        output.AddPage(page);  
                    }
                }

                System.IO.FileInfo oldpdf = new System.IO.FileInfo(_pdfurl);
                oldpdf.Delete();

            }

            System.IO.FileInfo[] files = new System.IO.DirectoryInfo(pdfpath).GetFiles("*" + pdfoutname + "*.pdf");

            string outputpath = "";

            if (files.Length > 0 && saveas)
            {

                outputpath = pdfpath + pdfoutname + "_" + files.Length + ".pdf";

            }
            else
            {
                outputpath = pdfpath + pdfoutname + ".pdf";
            }

            output.Save(outputpath);
            output.Dispose();




            //System.IO.FileInfo info = new System.IO.FileInfo(outputpath);

            /////pdfFileSize << 요놈
            //long pdfFileSize = info.Length;
            //Rhino.RhinoApp.WriteLine(pdfoutname + "size = " + pdfFileSize.ToString() + "bytes");




            ///서버업로드용 path 등록
            ///
            var dictionaryTempIndex = TuringAndCorbusierPlugIn.InstanceClass.turing.MainPanel_reportspaths[TuringAndCorbusierPlugIn.InstanceClass.turing.tempIndex];
            if (dictionaryTempIndex.ContainsKey("REPORT") == true)
            {
                var result = MessageBox.Show("이미 등록된 설계 보고서가 있습니다. 새 보고서를 서버에 저장하시겠습니까?", "설계 보고서 덮어쓰기", MessageBoxButton.OKCancel);

                if (result == MessageBoxResult.OK)
                {
                    dictionaryTempIndex.Remove("REPORT");
                    dictionaryTempIndex.Add("REPORT", outputpath);
                    Rhino.RhinoApp.WriteLine("덮어쓰기 완료" + Environment.NewLine + "파일 경로 = " + outputpath);
                }
            }
            else
            { 
                dictionaryTempIndex.Add("REPORT", outputpath);
                Rhino.RhinoApp.WriteLine("등록 완료" + Environment.NewLine + "파일 경로 = " + outputpath);
            }

            //var result = MessageBox.Show("출력 끝  파일열기 / 경로열기 / 닫기 ", "PDF로 보고서 출력", MessageBoxButton.YesNoCancel);

            //if (result == MessageBoxResult.Yes)
            //{
            //    System.Diagnostics.Process ps = new System.Diagnostics.Process();
            //    ps.StartInfo.FileName = pdfoutname + ".pdf";
            //    ps.StartInfo.WorkingDirectory = pdfpath;
            //    ps.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Normal;

            //    ps.Start();
            //}
            //else if (result == MessageBoxResult.No)
            //{
            //    System.Diagnostics.Process.Start("explorer.exe", pdfpath);

            //}

            this.documentViewer.Document = null;

            //System.IO.File.OpenRead(outputpath);

            //foreach (string xpss in _pageurls)
            //{
            //    System.IO.File.Delete(xpss);
            //}

            //foreach (string pdfs in _pdfurls)
            //{
            //    System.IO.File.Delete(pdfs);
            //}





            //UIManager.getInstance().HideWindow(TuringAndCorbusierPlugIn.InstanceClass.showmewindow, UIManager.WindowType.Print);
            string filename = "";
            dictionaryTempIndex.TryGetValue("REPORT",out filename);

            System.Diagnostics.Process ps = new System.Diagnostics.Process();
            
            ps.StartInfo.WorkingDirectory = pdfpath;

            ps.StartInfo.FileName = filename.Replace(pdfpath, "");

            //MessageBox.Show(ps.StartInfo.FileName);


            ps.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Normal;
            ps.EnableRaisingEvents = true;
            ps.Start();

            return;

            Close();
            //MessageBox.Show(tempPagesToShow[index].ActualHeight.ToString() + " , " + tempPagesToShow[index].Height.ToString());

            
        }

        private void Ps_Exited(object sender, EventArgs e)
        {
            Rhino.RhinoApp.WriteLine("프로세스종료됨");
           
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            
        }

        private void documentViewer_KeyDown(object sender, KeyEventArgs e)
        {
  
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            
        }

        private void Window_SizeChanged_1(object sender, SizeChangedEventArgs e)
        {

        }

        private void maximize_MouseWheel(object sender, MouseWheelEventArgs e)
        {

        }

        private void Window_StateChanged(object sender, EventArgs e)
        {

        }

        private void documentViewer_KeyDown_1(object sender, KeyEventArgs e)
        {
            
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            UIManager.getInstance().HideWindow(TuringAndCorbusierPlugIn.InstanceClass.showmewindow, UIManager.WindowType.Navi);
        }

        private void Window_KeyDown_1(object sender, KeyEventArgs e)
        {

        }
    }
}
