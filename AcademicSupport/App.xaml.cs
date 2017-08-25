using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace AcademicSupport
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            if (e.Args.Length == 0)
            {
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
                var svgs = d.GetFiles("*.svg", SearchOption.AllDirectories);
                foreach (var svg in svgs)
                {
                    var png = new FileInfo(svg.FullName + ".png");
                    if (png.Exists && png.LastWriteTime > svg.LastWriteTime)
                            continue;
                    // otherwise convert

                    var command = @"C:\Program Files\Inkscape\inkscape.exe";
                    var args = $"--file=\"{svg.FullName}\" " +
                               $"--export-png=\"{png.FullName}\" " +
                               "--export-dpi=150 " +
                               "--export-area-page";
                    
                    var process = new Process
                    {
                        StartInfo =
                        {
                            FileName = command,
                            Arguments = args
                        }
                    };
                    process.Start();
                }
                Current.Shutdown();
            }
        }
    }
}
