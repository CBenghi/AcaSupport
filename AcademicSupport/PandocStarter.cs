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
        public string outFolder;

        public FileInfo sysFolder;

        public string citationStyle = "elsevier-harvard.csl";

        private string bibLibrary = "biblatex.bib";

        private string CSL => Path.Combine(sysFolder.FullName, citationStyle);
        private string BIB => Path.Combine(sysFolder.FullName, bibLibrary);

        public FileInfo Convert(FileInfo sourcefile)
        {
            var dst = new FileInfo(
                Path.Combine(
                    Path.Combine(sysFolder.FullName, "pandoc-out"),
                    sourcefile.Name + ".docx"));


            const string command = @"pandoc.exe";
            var args = $"\"{sourcefile.FullName}\" --filter pandoc-citeproc --csl \"{CSL}\" --bibliography \"{BIB}\" -f markdown -t docx -s -o \"{dst.FullName}\"";

            var stopWatch = new Stopwatch();
            stopWatch.Start();

            // instantiate and launch process
            var process = new Process
            {
                StartInfo =
                {
                    FileName = command,
                    Arguments = args,
                    WorkingDirectory = sourcefile.DirectoryName
                }
            };
            process.Start();
            process.WaitForExit();

            // once stopped 
            stopWatch.Stop();
            return dst;
        }
    }
}
