using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Collections.Generic;
using System.Windows.Xps.Packaging;
using System.Windows.Xps;
using System.Windows.Documents;
using System.Linq;
using System.Windows.Controls;


namespace TuringAndCorbusier
{
    public class GeneratePDF
    {

        public static void WriteToXps(string path, FixedDocument fixedDoc)
        {
            XpsDocument xpsDoc = new XpsDocument(path, FileAccess.Write);
            XpsDocumentWriter xWriter = XpsDocument.CreateXpsDocumentWriter(xpsDoc);
            xWriter.Write(fixedDoc);
            xpsDoc.Close();
        }


        public static void SaveFixedDocument(FixedDocument fixedDoc)
        {
            string xpsPath = "C://Program Files (x86)//이주데이타//가로주택정비//" + "testFile.xps";

            using (XpsDocument currentXpsDoc = new XpsDocument(xpsPath, FileAccess.ReadWrite))
            {

                XpsDocumentWriter xw = XpsDocument.CreateXpsDocumentWriter(currentXpsDoc);

                xw.Write(fixedDoc);

            }

            return;
        }

        public static void saveFIxedPageAsPdf(string basicFilePath, FixedPage currentPageToContain, int i)
        {
            FixedDocument currentDoc = new FixedDocument();
            currentDoc.DocumentPaginator.PageSize = new Size(1240, 1753);

            PageContent tempPageContent = new PageContent();

            ((System.Windows.Markup.IAddChild)tempPageContent).AddChild(currentPageToContain);

            currentDoc.Pages.Add(tempPageContent);

            using (XpsDocument currentXpsDoc = new XpsDocument(basicFilePath + i.ToString() + ".xps", FileAccess.ReadWrite))
            {
                try
                {
                    XpsDocumentWriter xw = XpsDocument.CreateXpsDocumentWriter(currentXpsDoc);

                    xw.Write(currentDoc);
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                }
            }

            using (PdfSharp.Xps.XpsModel.XpsDocument pdfXpsDoc = PdfSharp.Xps.XpsModel.XpsDocument.Open(basicFilePath + i.ToString() + ".xps"))
            {
                PdfSharp.Xps.XpsConverter.Convert(pdfXpsDoc, basicFilePath + i.ToString() + ".pdf", 0);
            }
        }

        public static void SaveFixedDocument(List<FixedPage> pageToContain)
        {
            string xpsPath = "C://Program Files (x86)//이주데이타//가로주택정비//" + "testFile.xps";

            System.IO.File.Delete(xpsPath);

            FixedDocument currentDoc = new FixedDocument();
            currentDoc.DocumentPaginator.PageSize = new Size(1240, 1753);

            for (int i = 0; i < pageToContain.Count(); i++)
            {
                currentDoc.Pages.Add(pageBinding(pageToContain[i]));
            }

            using (XpsDocument currentXpsDoc = new XpsDocument(xpsPath, FileAccess.ReadWrite))
            {

                XpsDocumentWriter xw = XpsDocument.CreateXpsDocumentWriter(currentXpsDoc);

                xw.Write(currentDoc);

            }

            return;
        }

        private static PageContent pageBinding(FixedPage page)
        {
            PageContent pageContent = new PageContent();
            ((System.Windows.Markup.IAddChild)pageContent).AddChild(page);

            return pageContent;
        }


        public static void SavePdfFromXps()
        {
            string xpsPath = "C://Program Files (x86)//이주데이타//가로주택정비//" + "testFile.xps";
            string pdfPath = "C://Program Files (x86)//이주데이타//가로주택정비//" + "pdfTestFile.pdf";

            using (PdfSharp.Xps.XpsModel.XpsDocument pdfXpsDoc = PdfSharp.Xps.XpsModel.XpsDocument.Open(xpsPath))
            {
                PdfSharp.Xps.XpsConverter.Convert(pdfXpsDoc, pdfPath, 0);
            }
        }


        public static FixedPage CreateFixedPage(ReportBase FixedPageBase)
        {
            System.Windows.Controls.Grid tempGrid = new Grid();
            tempGrid.Width = 1240;
            tempGrid.Height = 1753;

            tempGrid.Children.Add(FixedPageBase.ExportContent());

            FixedPage currentPage = new FixedPage();
            currentPage.Background = Brushes.White;
            currentPage.Width = 1240;
            currentPage.Height = 1753;

            currentPage.Children.Add(tempGrid);

            return currentPage;
        }


        void SaveToBmp(FrameworkElement visual, string fileName)
        {
            var encoder = new BmpBitmapEncoder();
            SaveUsingEncoder(visual, fileName, encoder);
        }

        public static void SaveToPng(FrameworkElement visual, string fileName)
        {
            var encoder = new PngBitmapEncoder();
            SaveUsingEncoder(visual, fileName, encoder);
        }

        // and so on for other encoders (if you want)

        static void SaveUsingEncoder(FrameworkElement visual, string fileName, BitmapEncoder encoder)
        {
            RenderTargetBitmap bitmap = new RenderTargetBitmap(1240, 1753, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(visual);
            BitmapFrame frame = BitmapFrame.Create(bitmap);
            encoder.Frames.Add(frame);

            using (var stream = File.Create(fileName))
            {
                encoder.Save(stream);
            }
        }

        public static void SaveUsingEncoder(string fileName, FrameworkElement UIElement, BitmapEncoder encoder)
        {
            int height = (int)UIElement.ActualHeight;
            int width = (int)UIElement.ActualWidth;

            // These two line of code make sure that you get completed visual bitmap.
            // In case your Framework Element is inside the scroll viewer then some part which is not
            // visible gets clip.  
            UIElement.Measure(new System.Windows.Size(width, height));
            UIElement.Arrange(new Rect(new System.Windows.Point(), new Point(width, height)));

            RenderTargetBitmap bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(UIElement);

            SaveUsingBitmapTargetRenderer(fileName, bitmap, encoder);
        }


        private static void SaveUsingBitmapTargetRenderer(string fileName, RenderTargetBitmap renderTargetBitmap, BitmapEncoder bitmapEncoder)
        {
            BitmapFrame frame = BitmapFrame.Create(renderTargetBitmap);
            bitmapEncoder.Frames.Add(frame);
            // Save file .
            using (var stream = File.Create(fileName))
            {
                bitmapEncoder.Save(stream);
            }
        }


        public static RenderTargetBitmap GetImage(UIElement view)
        {
            Size size = new Size(1240, 1753);

            RenderTargetBitmap result = new RenderTargetBitmap((int)size.Width, (int)size.Height, 96, 96, PixelFormats.Pbgra32);

            DrawingVisual drawingvisual = new DrawingVisual();

            using (DrawingContext context = drawingvisual.RenderOpen())
            {
                context.DrawRectangle(new VisualBrush(view), null, new Rect(0, 0, (int)size.Width, (int)size.Height));
                context.Close();
            }

            result.Render(view);
            return result;
        }
        public static void SaveAsPng(RenderTargetBitmap bitmapImage, string targetFile)
        {
            PngBitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmapImage));

            using (var system = System.IO.File.Create(targetFile))
            {
                encoder.Save(system);
            }

        }

    }
}