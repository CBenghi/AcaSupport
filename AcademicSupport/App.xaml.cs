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
                Console.WriteLine($"Converting SVG images in {d.FullName}");

                var svgs = d.GetFiles("*.svg", SearchOption.AllDirectories);
                foreach (var svg in svgs)
                {
                    // if png exists and is newer than source
                    var png = new FileInfo(svg.FullName + ".png");
                    if (png.Exists && png.LastWriteTime > svg.LastWriteTime)
                        continue; // exit

                    // otherwise convert
                    Console.Write($" - \"{png.FullName}\"... ");

                    const string command = @"C:\Program Files\Inkscape\inkscape.exe";
                    var args = $"--file=\"{svg.FullName}\" " +
                               $"--export-png=\"{png.FullName}\" " +
                               "--export-dpi=150 " +
                               "--export-area-page";

                    var stopWatch = new Stopwatch();
                    stopWatch.Start();

                    // instantiate and launch process
                    var process = new Process
                    {
                        StartInfo =
                        {
                            FileName = command,
                            Arguments = args
                        }
                    };
                    process.Start();
                    process.WaitForExit();

                    // once stopped 
                    stopWatch.Stop();
                    
                    // report
                    Console.WriteLine($"{stopWatch.Elapsed.Milliseconds} ms.");
                }
                Current.Shutdown();
            }
        }
    }
}
