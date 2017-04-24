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

namespace TuringAndCorbusier
{
    /// <summary>
    /// ReportNavigation.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class ReportNavigation : NavigationWindow
    {
        List<Page> tempPagesToShow = new List<Page>();
        int index = 0;

        public ReportNavigation(List<Page> pagesToShow)
        {
            InitializeComponent();
            this.tempPagesToShow = pagesToShow;
            this.NavigationService.Navigate(tempPagesToShow[index]);
        }

        public ReportNavigation()
        {
        }

        public void nextPage()
        {
            index = (index + 1) % tempPagesToShow.Count();
            this.NavigationService.Navigate(tempPagesToShow[index]);
        }

        public void previousPage()
        {
            index = (tempPagesToShow.Count() + index - 1) % tempPagesToShow.Count();
            this.NavigationService.Navigate(tempPagesToShow[index]);
        }
    }
}
