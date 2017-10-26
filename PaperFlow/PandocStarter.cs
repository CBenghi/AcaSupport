using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using AcademicSupport;
using PaperFlow;

namespace AcademicSupport
{
    public class PandocStarter
    {
        private DirectoryInfo _sysFolder;

        public PandocStarter(DirectoryInfo sysFolder)
        {
            _sysFolder = sysFolder;
        }

        public string citationStyle = "elsevier-harvard.csl";

        private string bibLibrary = "biblatex.bib";

        private string CSL => Path.Combine(_sysFolder.FullName, citationStyle);
        public string BIB => Path.Combine(_sysFolder.FullName, bibLibrary);
        public bool PlaceTable { get; set; }
        public bool Numbering { get; set; } = true;
        public bool FilterFigno { get; set; } = true;
        public bool FilterTabno { get; set; } = true;
        public bool SectionNumbering { get; set; } = true;
        
        public PandocConversionResult Convert(FileInfo sourcefile, FileUnlocker unlocker = null)
        {
            // prepare pngs
            var d = new DirectoryInfo(Path.Combine(sourcefile.DirectoryName, "Charts"));
            Svg.ConvertVectorGraphics(d);

            var dst = new FileInfo(
                Path.Combine(
                    Path.Combine(_sysFolder.FullName, "pandoc-out"),
                    sourcefile.Name + ".docx"));

            // only if not null.
            unlocker?.RequestUnlock(dst.FullName);


            
            var FilterList = new List<string>();


            if (FilterTabno)
                FilterList.Add("--filter pandoc-tablenos");

            if (Numbering)
                FilterList.Add("--filter pandoc-numbering");

            if (FilterFigno)
                FilterList.Add("--filter pandoc-fignos");

            

            if (PlaceTable)
                FilterList.Add("--filter pandoc-placetable");

            // todo: not sure if this works in docx format
            if (SectionNumbering)
                FilterList.Add("--number-sections");



            FilterList.Add($"--filter pandoc-citeproc --csl \"{CSL}\" --bibliography \"{BIB}\"");

            var Filters = string.Join(" ", FilterList.ToArray());
            
            // template logic
            string template = "";
            var templateFileName = Path.ChangeExtension(sourcefile.FullName, "template.docx");
            if (File.Exists(templateFileName))
            {
                template = $"--reference-docx \"{templateFileName}\"";
            }
            
            // todo: --number-sections can be added when working with html (it's ignored in docx anyway)
            //
            const string command = @"pandoc.exe";
            // var args = $"\"{sourcefile.FullName}\" {Filters} -f markdown -t docx -s -o \"{dst.FullName}\"";
            var args = $"\"{sourcefile.FullName}\" {Filters} {template} -s -o \"{dst.FullName}\"";

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
