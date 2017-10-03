using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AcademicSupport
{
    class PandocStarter
    {
        private DirectoryInfo _sysFolder;

        internal PandocStarter(DirectoryInfo sysFolder)
        {
            _sysFolder = sysFolder;
        }

        public string citationStyle = "elsevier-harvard.csl";

        private string bibLibrary = "biblatex.bib";

        private string CSL => Path.Combine(_sysFolder.FullName, citationStyle);
        internal string BIB => Path.Combine(_sysFolder.FullName, bibLibrary);
        public bool PlaceTable { get; set; }

        public PandocConversionResult Convert(FileInfo sourcefile)
        {
            // prepare pngs
            var d = new DirectoryInfo(Path.Combine(sourcefile.DirectoryName, "Charts"));
            Svg.ConvertVectorGraphics(d);

            var dst = new FileInfo(
                Path.Combine(
                    Path.Combine(_sysFolder.FullName, "pandoc-out"),
                    sourcefile.Name + ".docx"));

            var lockingProcesses = FileUtil.WhoIsLocking(dst.FullName);
            foreach (var lockingProcess in lockingProcesses)
            {
                lockingProcess.CloseMainWindow();
                lockingProcess.WaitForExit(); // todo, this will have to change to a fixed amount of time
            }

            var FilterList = new List<string>();

            if (PlaceTable)
                FilterList.Add("--filter pandoc-placetable");

            FilterList.Add($"--filter pandoc-citeproc --csl \"{CSL}\" --bibliography \"{BIB}\"");

            var Filters = string.Join(" ", FilterList.ToArray());

            // todo: --number-sections can be added when working with html (it's ignored in docx anyway)
            //
            const string command = @"pandoc.exe";
            // var args = $"\"{sourcefile.FullName}\" {Filters} -f markdown -t docx -s -o \"{dst.FullName}\"";
            var args = $"\"{sourcefile.FullName}\" {Filters} -s -o \"{dst.FullName}\"";

            var cmdline = command + " " + args;

            // instantiate process
            var process = new Process
            {
                StartInfo =
                {
                    FileName = command,
                    Arguments = args,
                    WorkingDirectory = sourcefile.DirectoryName,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };

            // launch and start stopwatch
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            process.Start();
            process.WaitForExit();

            var retT = process.StandardError.ReadToEnd();

            // once finished
            stopWatch.Stop();
            var res = new PandocConversionResult
            {
                ConvertedFile = dst,
                Milliseconds = stopWatch.ElapsedMilliseconds,
                Report = Uri.UnescapeDataString(retT), 
                ExitCode = process.ExitCode
            };
            return res;
        }
    }
}
