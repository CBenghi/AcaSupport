using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Runtime.InteropServices;

namespace AcademicSupport
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        [DllImport("kernel32.dll")]
        static extern bool FreeConsole();

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            if (e.Args.Length == 0)
            {
                FreeConsole();
                var wnd = new MainWindow();
                wnd.Show();
                return;
            }

            if (e.Args[0] == "-svg")
            {
                var folder = ".";
                if (e.Args.Length > 1)
                {
                    folder = e.Args[1];
                }
                var d = new DirectoryInfo(folder);
                var svg = new Svg();
                svg.ConvertVectorGraphics(d);
                Current.Shutdown();
            }
        }
    }
}
