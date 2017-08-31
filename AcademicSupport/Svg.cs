using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AcademicSupport
{
    internal class Svg
    {
        internal static void ConvertVectorGraphics(DirectoryInfo d)
        {
            Console.WriteLine($"Converting SVG images in {d.FullName}");
            if (!d.Exists)
                return;

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
                // Console.WriteLine($"{stopWatch.Elapsed.Milliseconds} ms.");
            }
        }

    }
}
