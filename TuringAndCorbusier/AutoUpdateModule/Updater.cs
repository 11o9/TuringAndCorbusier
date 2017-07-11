using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPWK_AutoUpdateClient
{
    public class Updater
    {
        public Updater()
        {
            MainWindow mainWindow = new MainWindow();
            mainWindow.Topmost = true;
            mainWindow.Show();
            //mainWindow.Run();
        }
    }
}
