using System;
using System.Windows;
using System.Data;
using System.Net;
using System.IO;
using System.ComponentModel;
using System.Reflection;
using TuringAndCorbusier.Utility;
using TuringAndCorbusier.AutoUpdateModule;

namespace SPWK_AutoUpdateClient
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : Window
    {
        //changedsomething <<
        int updatedFileCount = 0;
        string tempFileName = "";
        string defaultLocal = "";
        //string defaultDownload = "";
        private string defaultFTPUrl = @"ftp://spwk-cloud-ftp.koreacentral.cloudapp.azure.com/";
        public string company = "LH";
        WebClient webClient = new WebClient();

        string localV = "";
        string remoteV = "";

        public MainWindow()
        {
            
            InitializeComponent();


            System.Drawing.Bitmap backGround = TuringAndCorbusier.Properties.Resources.shVersion;
            background.Source = XamlTools.CreateBitmapSourceFromGdiBitmap(backGround);
            background.UpdateLayout();
           
            //bool isLatest = CheckVersion();
            webClient.DownloadProgressChanged += webClient_DownloadProgressChanged;
            webClient.DownloadFileCompleted += webClient_DownloadFileCompleted;
            //if (!isLatest)
        }

        public void Run(string companyName)
        {
            company = companyName;
            defaultFTPUrl += companyName + @"/";

            InitializeDirectory();

            if (defaultLocal == "")
                return;

            DownloadServerXml();
            
            System.Threading.Thread t1 = new System.Threading.Thread(new System.Threading.ThreadStart(Update));
            t1.Start();
        }
      
        private void InitializeDirectory()
        {
            if (Environment.Is64BitProcess)
            {
                var rhps = new DirectoryInfo(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)).GetFiles("TuringAndCorbusier.rhp", SearchOption.AllDirectories);
                for (int i = 0; i < rhps.Length; i++)
                {
                    if (rhps[i].FullName.Contains("Boundless")|| rhps[i].FullName.Contains("boundless"))
                        defaultLocal = rhps[i].Directory.FullName;
                }
            }

            else
            {

                var rhps = new DirectoryInfo(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)).GetFiles("TuringAndCorbusier.rhp", SearchOption.AllDirectories);
                for (int i = 0; i < rhps.Length; i++)
                {
                    if (rhps[i].FullName.Contains("Boundless") || rhps[i].FullName.Contains("boundless"))
                        defaultLocal = rhps[i].Directory.FullName;
                }
            }

            if (defaultLocal == "")
            {
                TitleChanged("파일이 올바른 경로에 있지 않습니다.");
                return;
            }

            Rhino.RhinoApp.WriteLine(defaultLocal.ToString());

            if (!Directory.Exists(defaultLocal + @"\xmls"))
                Directory.CreateDirectory(defaultLocal + @"\xmls");
            
            if (!Directory.Exists(defaultLocal + @"\Common"))
                Directory.CreateDirectory(defaultLocal + @"\Common");

            if (!Directory.Exists(defaultLocal + @"\ko"))
                Directory.CreateDirectory(defaultLocal + @"\ko");

            localVersion.Text = localV = AssemblyName.GetAssemblyName(defaultLocal + @"\TuringAndCorbusier.rhp").Version.ToString();
        }

        private void DownloadServerXml()
        {
            string downloadPath = defaultLocal + @"\xmls\serverxml.xml";
            string debug = defaultFTPUrl + "xmls/textxml.xml";
            try
            {
                webClient.DownloadFile(debug, downloadPath);

                UpdateListDataSet serverSet = new UpdateListDataSet();
                try
                {
                    serverSet.ReadXml(defaultLocal + @"\xmls\serverxml.xml");
                    foreach (var row in serverSet.Tables[0].Rows)
                    {
                        DataRow r = row as DataRow;
                        if (r[0].ToString().Contains(".rhp"))
                        {
                            if (r.ItemArray.Length > 3)
                            {
                                remoteVersion.Text = remoteV = r[3].ToString();
                                break;
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    //remoteVersion.Text = "최신 버전 : 알수없음";
                }
            }
            catch (Exception e)
            {
                TitleChanged("서버 연결 실패, " + e.Message);
            }
        }


        private void Update()
        {

            bool islatest = true;
            bool fileCountEqual = true;


            //read local xml
            UpdateListDataSet localSet = new UpdateListDataSet();
            try
            {
                localSet.ReadXml(defaultLocal + @"\xmls\localxml.xml");
            }
            catch (Exception e)
            {
                fileCountEqual = false;
            }
            //read server xml
            UpdateListDataSet serverSet = new UpdateListDataSet();
            try
            {
                serverSet.ReadXml(defaultLocal + @"\xmls\serverxml.xml");
            }
            catch (Exception e)
            {
               //remoteVersion.Text = "최신 버전 : 알수없음";
            }
          
            if (localV != remoteV)
            {
                islatest = false;
            }

            //if(!islatest || !fileCountEqual)
            //{
            //    for (int i = 0; i < localSet.Tables[0].Rows.Count; i++)
            //    {
            //        var localr = localSet.Tables[0].Rows[i];
            //        var serverr = serverSet.Tables[0].Rows[i];
            //        if (localr[0].ToString() != serverr[0].ToString()
            //            || localr[1].ToString() != serverr[1].ToString()
            //            || localr[2].ToString() != serverr[2].ToString()
            //            )
            //        {
            //            islatest = false;
            //            break;
            //        }
            //    }
            //}

            if (islatest && fileCountEqual)
            {
                string lastMessage = "최신 버전 입니다.";
                TitleChanged(lastMessage);
                return;
            }
            else
            {
                serverSet.WriteXml(defaultLocal + @"\\xmls\\localxml.xml");
            }
            var localTb = localSet.Tables[0];
            var serverTb = serverSet.Tables[0];

            foreach (DataRow row in serverTb.Rows)
            {
                try
                {
                    string urlpath = row["Path"].ToString();
                    string length = row["Length"].ToString();
                    string write = row["LastWriteTime"].ToString();

                    string downloadPath = defaultLocal + @"\" + urlpath;
                    bool needUpdate = false;


                    FileInfo info = new FileInfo(downloadPath);


                    //bool a = !info.Length.ToString().Equals(write);
                    //bool b = !info.LastWriteTime.ToString().Equals(length);
                    //bool c = (a || b);

                    if (info.Exists)
                    {
                        string size = info.Length.ToString();
                        string time = info.LastWriteTime.ToString();
                        if (!time.Equals(write) || !size.Equals(length))
                            needUpdate = true;
                    }
                    else
                    {
                        needUpdate = true;
                    }

                    if (!needUpdate)
                        continue;


                    if (urlpath.Contains("TuringAndCorbusier"))
                    {
                        //실행파일 이름변경 처리
                        string newFile = defaultLocal + @"\temp.userdata";
                        if (File.Exists(newFile))
                            File.Delete(newFile);

                        if (File.Exists(downloadPath))
                            File.Move(downloadPath, newFile);
                    }

                    updatedFileCount++;
                    Uri uri = new Uri(defaultFTPUrl + urlpath);
                    string fileName = urlpath;
                    long fileSize = long.Parse(length);


                    DownloadAsync(uri, downloadPath);

                    System.Threading.Thread.Sleep(1000);
                }
                catch (Exception e)
                {

                }
            }

        }

        private void DownloadAsync(Uri uri, string downloadPathAndFileName)
        {
            try
            {
                tempFileName = downloadPathAndFileName;
                webClient.DownloadFileAsync(uri, downloadPathAndFileName);
            }
            catch (Exception e)
            {

            }
        }

        private void webClient_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            //this.Text = (string)e.UserState + "완료";
            //labelTitle.Text = "Complete!!";

            string lastMessage = "";

            if (tempFileName.Contains(".xml"))
            {
                lastMessage = "업데이트 완료, 프로그램을 재시작 해주세요.";
                TitleChanged(lastMessage);
            }
          
            

            else
            {
                string text = tempFileName + " Complete";
                TitleChanged(text);
            }
        }

        private void TitleChanged(string title)
        {
            if (fileName.Dispatcher.CheckAccess())
            {
                fileName.Text = title;
            }
            else
            {
                fileName.Dispatcher.BeginInvoke(new Action<string>(TitleChanged), new object[] { title });
            }

        }

        private void ProgresstxtChanged(string txt)
        {
            if (fileName.Dispatcher.CheckAccess())
            {
                progress.Text = txt;
            }
            else
            {
                progress.Dispatcher.BeginInvoke(new Action<string>(TitleChanged), new object[] { txt });
            }

        }

        private void webClient_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            ProgressBarChanged(e.ProgressPercentage, e.BytesReceived, e.TotalBytesToReceive);
            //isDownload = true;
        }

        private void ProgressBarChanged(double value, double bytes, double total)
        {
            if (pb.Dispatcher.CheckAccess())
            {
                pb.Value = value;

                TitleChanged(tempFileName);
                //fileName.Text = tempFileName;
                ProgresstxtChanged(string.Format("download {0}byte/{1} bytes   {2}%",
                    bytes,
                    total,
                    value
                    ));
            }
            else
            {
                this.Dispatcher.BeginInvoke(new Action<double,double,double>(ProgressBarChanged), new object[]{ value,bytes,total });
            }

        }

    } 
}
