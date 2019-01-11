using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using AcademicSupport;
using PaperFlow;
using PaperFlow.Markdown;
using System.Linq;

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

        // private string bibLibrary = "biblatex.bib";
        private string bibLibrary = "bib.json";

        private string CSL => Path.Combine(_sysFolder.FullName, citationStyle);
        public string BIB(FileInfo sourceFile)
        {
            // PreferPaperBib
            if (PreferPaperBib && sourceFile!= null)
            {
                var localfile = Path.ChangeExtension(sourceFile.FullName, "json");
                if (File.Exists(localfile))
                    return localfile;

            }
            return Path.Combine(_sysFolder.FullName, bibLibrary);
        }
            
            
        public bool PlaceTable { get; set; }
        public bool Numbering { get; set; } = false;
        public bool FilterFigno { get; set; } = true;
        public bool FilterTabno { get; set; } = true;
        public bool SectionNumbering { get; set; } = true;
        public bool WrapPreserve { get; set; } = false;
        public Svg ImageConverter { get; set; }

        /// <summary>
        /// This bit determines if a local bib file has to be preferred to the main repository, when available.
        /// </summary>
        public bool PreferPaperBib { get; set; } = true;

        public PandocConversionResult ToLatex(FileInfo sourcefile, FileInfo destFile = null, FileUnlocker unlocker = null)
        {
            // if no destination specified then write to system folder
            if (destFile == null)
            {
                destFile = new FileInfo(
                    Path.Combine(
                        Path.Combine(_sysFolder.FullName, "pandoc-out"),
                        sourcefile.Name + ".tex")
                    );
            }

            // only if not null.
            unlocker?.RequestUnlock(destFile.FullName);

            var args = $"";
            if (WrapPreserve)
                args = "--wrap=preserve";
            PandocConversionResult res = RunPandoc(sourcefile, destFile, args);
            return res;
        }

        public PandocConversionResult ToMarkDown(FileInfo sourcefile, FileInfo destFile = null, FileUnlocker unlocker = null)
        {
            // if no destination specified then write to system folder
            if (destFile == null)
            {
                destFile = new FileInfo(
                    Path.Combine(
                        Path.Combine(_sysFolder.FullName, "pandoc-out"),
                        sourcefile.Name + ".md")
                    );
            }

            // only if not null.
            unlocker?.RequestUnlock(destFile.FullName);
            
            var args = $"";
            if (WrapPreserve)
                args = "--wrap=preserve";
            PandocConversionResult res = RunPandoc(sourcefile, destFile, args);
            return res;
        }

        public PandocConversionResult ToJson(FileInfo sourcefile, FileInfo destFile = null, FileUnlocker unlocker = null)
        {
            // if no destination specified then write to system folder
            if (destFile == null)
            {
                destFile = new FileInfo(
                    Path.Combine(
                        Path.Combine(_sysFolder.FullName, "pandoc-out"),
                        sourcefile.Name + ".json")
                    );
            }

            // only if not null.
            unlocker?.RequestUnlock(destFile.FullName);

            var args = $"-t json";
            PandocConversionResult res = RunPandoc(sourcefile, destFile, args);
            return res;
        }


        public PandocConversionResult ToWord(FileInfo sourcefile, FileInfo destFile = null, FileUnlocker unlocker = null)
        {

            if (ImageConverter != null)
            {
                var errorImages = new List<string>();
                // prepare pngs
                
                foreach (var imageFileInfo in PandocMarkDownImages.GetImages(sourcefile))
                {
                    if (imageFileInfo.Name.EndsWith(".svg.png"))
                    {
                        var svgFileInfo = new FileInfo(imageFileInfo.FullName.Substring(0, imageFileInfo.FullName.Length - 4));
                        ImageConverter.ConvertVectorGraphics(svgFileInfo);
                    }
                    if (!imageFileInfo.Exists)
                    {
                        errorImages.Add(imageFileInfo.FullName);
                        continue;
                    }
                }
                if (errorImages.Any())
                {
                    var result = new PandocConversionResult()
                    {
                        Report = @"Images missing:\r\n- " + string.Join("\r\n- ", errorImages)
                    };
                    return result;
                }

                //// directory approach
                //var d = new DirectoryInfo(Path.Combine(sourcefile.DirectoryName, "Charts"));
                //ImageConverter.ConvertVectorGraphics(d);
            }
            

            // if no destination specified then write to system folder
            if (destFile == null)
            {
                destFile = new FileInfo(
                    Path.Combine(
                        Path.Combine(_sysFolder.FullName, "pandoc-out"),
                        sourcefile.Name + ".docx")
                    );
            }
            var isLatex = false;
            if (destFile.FullName.EndsWith(".tex"))
                isLatex = true;


            // only if not null.
            unlocker?.RequestUnlock(destFile.FullName);

            var FilterList = new List<string>();

            if (FilterTabno)
                FilterList.Add("--filter pandoc-tablenos");

            if (Numbering)
                FilterList.Add("--filter pandoc-numbering");

            if (FilterFigno)
                FilterList.Add("--filter pandoc-fignos");

            if (PlaceTable)
                FilterList.Add(@"--filter ""C:\Users\sgmk2\AppData\Roaming\cabal\bin\pandoc-placetable""");

            // --number-sections can be added when working with html
            // it's ignored in docx anyway
            if (SectionNumbering)
                FilterList.Add("--number-sections");
            
            if (isLatex)
                FilterList.Add($"--bibliography \"{BIB(sourcefile)}\"");
            else
                FilterList.Add($"--filter pandoc-citeproc --csl \"{CSL}\" --bibliography \"{BIB(sourcefile)}\"");

            var Filters = string.Join(" ", FilterList.ToArray());

            string template = "";
            // template logic
            if (!isLatex)
            {
                var templateFileName = Path.ChangeExtension(sourcefile.FullName, "template.docx");
                if (File.Exists(templateFileName))
                {
                    template = $"--reference-doc \"{templateFileName}\"";
                }
            }
            
            // -s is for standalone, it produces comprehensive files, rather than fragments
            //
            var args = $"{Filters} {template} -s";
            PandocConversionResult res = RunPandoc(sourcefile, destFile, args);
            return res;
        }

        private static PandocConversionResult RunPandoc(FileInfo sourcefile, FileInfo destFile, string args)
        {
            args = $"\"{sourcefile.FullName}\" {args} -o \"{destFile.FullName}\"";
            const string command = @"""C:\Program Files\Pandoc\pandoc.exe""";
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
                ConvertedFile = destFile,
                Milliseconds = stopWatch.ElapsedMilliseconds,
                Report = Uri.UnescapeDataString(retT),
                ExitCode = process.ExitCode
            };
            return res;
        }
    }
}
