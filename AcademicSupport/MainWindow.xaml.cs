using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using AcademicSupport.Properties;
using LiveCharts;
using LiveCharts.Wpf;
using Microsoft.Win32;
using System.Text;
using System.Windows.Controls;
using PaperFlow.Markdown;
using PaperFlow.MarkDown;

namespace AcademicSupport
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private List<Regex> _ignoredFiles = new List<Regex>();

        public MainWindow()
        {
            InitializeComponent();
            TxtFolder.Text = Settings.Default.InspectionFolder;
            if (TxtFolder.Text == "")
            {
                var d = new DirectoryInfo(".");
                TxtFolder.Text = d.FullName;
            }
            var dir = new DirectoryInfo(TxtFolder.Text);
            if (!dir.Exists)
                return;
            if (dir.GetDirectories(".system").FirstOrDefault() != null)
            {
                UpdateDisplay();
                UpdateSystem();
            }
        }

        private void UpdateSystem()
        {
            var fld = GetFolder();
            var sysfolder = fld.GetDirectories(".system").FirstOrDefault();
            UpdateCitationStyles(sysfolder);
        }

        private void UpdateCitationStyles(DirectoryInfo sysfolder)
        {
            CitationStyle.Items.Clear();
            var styles = sysfolder.GetFiles("*.csl");
            foreach (var fileInfo in styles)
            {
                CitationStyle.Items.Add(fileInfo.Name);
            }
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            if (!SaveConfig())
                return;
            UpdateWordCount();
            UpdateDisplay();
        }

        private void UpdateDisplay()
        {
            LoadTrackedFiles();
            UpdateMarkdownList();
            UpdateFileCountCurve();
            UpdateDailyCountBars();
        }

        private IEnumerable<FileInfo> MarkdownFiles
        {
            get
            {
                var fld = GetFolder();
                if (!fld.Exists)
                    yield break;
                foreach (var file in fld.GetFiles("*.md", SearchOption.AllDirectories))
                {
                    yield return file;
                }
            }
        }

        private void UpdateMarkdownList()
        {
            var bareNames = new List<string>();
            var fld = GetFolder();
            foreach (var markdownFile in MarkdownFiles)
            {
                var bare = TrackedFile.BareName(markdownFile, fld);
                bareNames.Add(bare);
            }
            MarkDownList.ItemsSource = bareNames;
        }

        private IEnumerable<TrackedFile> FileToChart
        {
            get
            {
                foreach (var trackedFile in _trackedFiles)
                {
                    if (IsIgnoredFile(trackedFile.Key))
                        continue;
                    yield return trackedFile.Value;
                }
            }
        }

        private bool IsIgnoredFile(string trackedFileKey)
        {
            foreach (var ignoredFile in _ignoredFiles)
            {
                if (ignoredFile.IsMatch(trackedFileKey))
                    return true;
            }
            return false;
        }

        private void UpdateDailyCountBars()
        {
            var sc = new SeriesCollection();
            var runDate = MinDate().Date; // start from date only;
            var oneWeekDate = DateTime.Now.Date.AddDays(-7);

            runDate = (runDate > oneWeekDate ? runDate : oneWeekDate);

            var required = new List<DateTime>();

            var tonight = DateTime.Now.Date.AddDays(1);
            var labels = new List<string>();
            while (runDate < tonight)
            {
                labels.Add(runDate.ToShortDateString());
                required.Add(runDate);
                runDate = runDate.AddDays(1);
            }

            var tot = 0;
            var activeDays = 0;

            foreach (var trackedFilesValue in FileToChart)
            {
                var ser = trackedFilesValue.DailySeries(required);
                var thisday = CountWords(ser);
                if (thisday > 0)
                    activeDays++;
                tot += thisday;
                var bareName = TrackedFile.BareName(trackedFilesValue.File, GetFolder());
                ser.Title = bareName;
                sc.Add(ser);
            }

            var title = $"{tot / labels.Count} words per day";
            if (activeDays > 0)
                title += $" ({tot / activeDays} words per active day)";

            DailyCountBars.Title = title;
            DailyCountBars.Labels = labels.ToArray();
            DailyCountBars.SeriesCollection = sc;
        }

        private int CountWords(StackedColumnSeries ser)
        {
            var tot = 0;
            foreach (var chartValue in ser.Values.OfType<double>())
            {
                tot += (int)chartValue;
            }
            return tot;
        }

        private void UpdateFileCountCurve()
        {
            var sc = new SeriesCollection();
            var minDate = MinDate().Date;
            foreach (var trackedFilesValue in FileToChart)
            {
                var ser = trackedFilesValue.ToSeries(minDate);
                var bareName = TrackedFile.BareName(trackedFilesValue.File, GetFolder());
                ser.Title = bareName;
                sc.Add(ser);
            }
            FileCountCurve.SeriesCollection = sc;
        }

        private DateTime MinDate()
        {
            return _trackedFiles.Values.Min(x => x.MinDate());
        }

        private bool SaveConfig()
        {
            var f = GetFolder();
            if (!f.Exists)
                return false;
            Settings.Default.InspectionFolder = TxtFolder.Text;
            Settings.Default.Save();
            return true;
        }

        Dictionary<string, TrackedFile> _trackedFiles;
        Dictionary<string, string> _mappedFiles;

        private string _logfileName = ".system\\ChangeLog.txt";

        private FileInfo LogFile
        {
            get
            {
                var f = GetFolder();
                if (f == null)
                    return null;

                var fname = Path.Combine(f.FullName, _logfileName);

                var ret = new FileInfo(fname);
                return ret;
            }
        }

        private void UpdateWordCount()
        {
            var fld = GetFolder();
            if (!fld.Exists)
                return;

            LoadTrackedFiles();

            var log = LogFile;
            if (log == null)
                return;

            using (var logF = log.AppendText())
            {
                foreach (var file in MarkdownFiles)
                {
                    var bareName = TrackedFile.BareName(file, fld);
                    TrackedFile f;
                    if (!_trackedFiles.TryGetValue(bareName, out f))
                    {
                        f = new TrackedFile(file);
                        _trackedFiles.Add(bareName, f);
                    }
                    var needUpdate = f.Evaluate();
                    if (needUpdate)
                        logF.WriteLine(f.LatestLog(fld));
                }
            }
        }

        private void LoadTrackedFiles()
        {

            var mapRe = new Regex(@"#map\s+(?<from>.+)\s+=>\s+(?<to>.+)\s*");

            _ignoredFiles = new List<Regex>();
            _trackedFiles = new Dictionary<string, TrackedFile>();
            _mappedFiles = new Dictionary<string, string>();
            var log = LogFile;
            if (log == null)
                return;
            if (!log.Exists)
                return;
            using (var logF = log.OpenText())
            {
                string line;
                while ((line = logF.ReadLine()) != null)
                {
                    string bName;
                    TrackedFileStat stat;
                    if (line.StartsWith("#ignore"))
                    {
                        var pattern = line.Substring(7).Trim();
                        if (!string.IsNullOrWhiteSpace(pattern))
                        {
                            _ignoredFiles.Add(new Regex(pattern));
                        }
                    }
                    else if (line.StartsWith("#map"))
                    {

                        var m = mapRe.Match(line);
                        _mappedFiles.Add(
                            m.Groups["from"].Value,
                            m.Groups["to"].Value
                            );
                    }
                    else if (TrackedFile.ReadLog(line, out bName, out stat))
                    {
                        bName = GetMap(bName);
                        if (!_trackedFiles.ContainsKey(bName))
                        {
                            _trackedFiles.Add(bName, new TrackedFile(GetFolder(), bName));
                        }
                        _trackedFiles[bName].AddStat(stat);
                    }
                }
            }
            MarkDownList.ItemsSource = TrackedFileNames;
        }

        private string GetMap(string bName)
        {
            return _mappedFiles.ContainsKey(bName)
                ? _mappedFiles[bName]
                : bName;
        }

        DirectoryInfo GetFolder()
        {
            return new DirectoryInfo(TxtFolder.Text);
        }

        private void button2_Click(object sender, RoutedEventArgs e)
        {
            var SeriesCollection = new SeriesCollection
            {
                new StackedColumnSeries
                {
                    Values = new ChartValues<double> {1, 2, 3, 4},
                    StackMode = StackMode.Values, // this is not necessary, values is the default stack mode
                    DataLabels = true
                },
                new StackedColumnSeries
                {
                    Values = new ChartValues<double> {4, 4, 4, 4},
                    StackMode = StackMode.Values,
                    DataLabels = true
                }
            };

            DailyCountBars.SeriesCollection = SeriesCollection;
        }

        public IEnumerable<string> TrackedFileNames => _trackedFiles.Keys;

        private DirectoryInfo SysFolder
        {
            get
            {
                var f = GetFolder();
                if (f == null)
                    return null;
                var pth = Path.Combine(GetFolder().FullName, ".system");
                return new DirectoryInfo(pth);
            }
        }

        WindowsFileUlocker _fileUnlocker = new WindowsFileUlocker();

        private void PandocLaunch(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var f = SelectedMarkDown;

            Svg svg = new Svg()
            {
                ForceRefresh = (bool)InkscapeRefresh.IsChecked,
                ResolutionDPI = Convert.ToInt32(InkscapeResolution.Text),
                TimeOutSeconds = Convert.ToInt32(InkscapeTimeout.Text)
            };

            var converter = new PandocStarter(SysFolder);
            converter.ImageConverter = svg;
            
            converter.WrapPreserve = GetBool(WrapPreserve);

            PandocConversionResult conversion = null;

            switch(FormatOut.Text)
            {
                case "word":
                    if (!string.IsNullOrEmpty(CitationStyle.Text))
                    {
                        converter.citationStyle = CitationStyle.Text;
                    }
                    converter.PlaceTable = GetBool(FilterPlacetable);
                    // s.Numbering = GetBool(FilterNumbering);
                    converter.FilterFigno = GetBool(FilterFigno);
                    converter.FilterTabno = GetBool(FilterTabno);
                    converter.SectionNumbering = GetBool(SectionNumbering);
                    conversion = converter.ToWord(f, null, _fileUnlocker);
                    break;
                case "json":
                    conversion = converter.ToJson(f, null, _fileUnlocker);
                    break;
                case "markdown":
                    conversion = converter.ToMarkDown(f, null, _fileUnlocker);
                    break;

            }
            if (GetBool(OpenWhenDone))
            {
                var ret = MessageBoxResult.Yes;
                if (!string.IsNullOrWhiteSpace(conversion.Report))
                {
                    ret = MessageBox.Show(this,
                        $"Error in conversion:\r\n\r\n{conversion.Report}\r\nShall I open the file?\r\nChoosing No copies error to the clipboard.", "Error",
                        MessageBoxButton.YesNoCancel);
                }
                if (ret == MessageBoxResult.Yes)
                {
                    Process.Start(conversion.ConvertedFile.FullName);
                }
                else if (ret == MessageBoxResult.No)
                {
                    Clipboard.SetText(conversion.Report);
                }
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(conversion.Report))
                {
                    var ret = MessageBox.Show(this,
                        $"Error in conversion:\r\n\r\n{conversion.Report}\r\nShall I copy the error to the clipboard.", "Error",
                        MessageBoxButton.YesNoCancel);
                    if (ret == MessageBoxResult.Yes)
                    {
                        Clipboard.SetText(conversion.Report);
                    }
                }
            }
        }

        private bool GetBool(CheckBox checkBox)
        {
            return checkBox.IsChecked.HasValue && checkBox.IsChecked.Value;
        }

        private FileInfo SelectedMarkDown
        {
            get
            {
                var fName = MarkDownList.SelectedItem.ToString();
                var f = new FileInfo(
                    Path.Combine(
                        GetFolder().FullName,
                        fName
                    ));
                return f;
            }
        }

        private void BibExtract_Click(object sender, RoutedEventArgs e)
        {
            // get available bib keys
            //
            var s = new PandocStarter(SysFolder);
            var systemBib = new FileInfo(s.BIB(null));
            var avails = BibliographyManagement.BibliographyAsDictionary(systemBib);
            
            // get the matching references
            //
            var mdSource = SelectedMarkDown;
            var usages = BibliographyManagement.GetUsage(mdSource, avails.Keys);
            


            // produce new file
            //
            var mdBibName = Path.ChangeExtension(mdSource.FullName, "bib");
            var mdBib = new FileInfo(mdBibName);
            var ret = MessageBox.Show($"{usages.Count} references used. Write to {mdBib.FullName} file?", "", MessageBoxButton.YesNoCancel);
            if (ret != MessageBoxResult.Yes)
                return;
            
            using (var mdBibS = mdBib.CreateText())
            {
                foreach (var key in usages.Keys)
                {
                    var found = avails.TryGetValue(key, out string bib);
                    if (!found)
                        continue;
                    mdBibS.WriteLine(bib);
                }
            }
        }

        private void Other_File(object sender, RoutedEventArgs e)
        {
            var d = new OpenFileDialog
            {
                Multiselect = false,
                DefaultExt = ".md",
            };
            d.ShowDialog(this);
            if (string.IsNullOrWhiteSpace(d.FileName))
                return;
            var fn = d.FileName;

            var f = new FileInfo(fn);

            var s = new PandocStarter(SysFolder);
            if (!string.IsNullOrEmpty(CitationStyle.Text))
            {
                s.citationStyle = CitationStyle.Text;
            }
            var conversion = s.ToWord(f, null, _fileUnlocker);
            var ret = MessageBoxResult.Yes;
            if (!string.IsNullOrWhiteSpace(conversion.Report))
            {
                ret = MessageBox.Show(this,
                    $"Error in conversion:\r\n\r\n{conversion.Report}\r\nshall I open the file?\r\nChoosing No copies error to the clipboard.", "Error",
                    MessageBoxButton.YesNoCancel);
            }
            if (ret == MessageBoxResult.Yes)
            {
                Process.Start(conversion.ConvertedFile.FullName);
            }
            else if (ret == MessageBoxResult.No)
            {
                Clipboard.SetText(conversion.Report);
            }
        }

        private void EmphasisExtract_Click(object sender, RoutedEventArgs e)
        {
            var reRefKey = new Regex(@"\*\w+( \w+){0,3}\*");

            var mdSource = SelectedMarkDown;
            var doneMatches = new List<string>();
            using (var mdSourceS = mdSource.OpenText())
            {
                var markDown = mdSourceS.ReadToEnd();
                foreach (Match match in reRefKey.Matches(markDown))
                {
                    var key = match.Value;
                    if (doneMatches.Contains(key))
                        continue;
                    doneMatches.Add(key);
                }
            }
            if (!doneMatches.Any())
            {
                MessageBox.Show("No matches found.");
            }
            doneMatches.Sort();
            var allmatches = string.Join("\r\n", doneMatches.ToArray());
            Clipboard.SetText(allmatches);
            MessageBox.Show($"{doneMatches.Count} matches copied to clipboard.");
        }

        private void BibExtractTwo_Click(object sender, RoutedEventArgs e)
        {
            // get available bib keys
            //
            var fullBib4 = new FileInfo(@"E:\Dev\PaperRepository\zot4.bib");
            var fullBib5 = new FileInfo(@"E:\Dev\PaperRepository\zot5.bib");
            var avails4 = BibliographyManagement.BibliographyAsDictionary(fullBib4);
            var avails5 = BibliographyManagement.BibliographyAsDictionary(fullBib5);

            if (avails4.Count != avails5.Count)
            {
                return;
            }

            var keys4 = avails4.Keys.ToArray();
            var keys5 = avails5.Keys.ToArray();

            var titRe = new Regex(@"title = *([^\n])*");



            var replaceIds = new Dictionary<string, string>();
            for (int i = 0; i < avails4.Count; i++)
            {

                var cont4 = avails4[keys4[i]];
                var cont5 = avails5[keys5[i]];
                var title4 = titRe.Match(cont4);
                var title5 = titRe.Match(cont5);

                if (title4.Groups[1].Value == title5.Groups[1].Value)
                {
                    // same title
                    if (keys4[i] != keys5[i])
                    {
                        replaceIds.Add(keys4[i], keys5[i]);
                    }
                }
            }


            // produce new file
            //
            var mdSource = SelectedMarkDown;
            var mdBibName = Path.ChangeExtension(mdSource.FullName, ".2.md");
            var mdBib = new FileInfo(mdBibName);

            using (var mdSourceS = mdSource.OpenText())
            using (var mdBibS = mdBib.CreateText())
            {
                var markDown = mdSourceS.ReadToEnd();
                var mdCopy = markDown;

                foreach (var item in replaceIds)
                {
                    mdCopy = mdCopy.Replace(item.Key, item.Value);
                }
                mdBibS.Write(mdCopy);
            }
        }


        private void CheckAcronyms(object sender, RoutedEventArgs e)
        {
            var mdSource = SelectedMarkDown;
            var doneMatches = new Dictionary<string, AcronymDesc>();
            using (var mdSourceS = mdSource.OpenText())
            {
                var markDown = mdSourceS.ReadToEnd();
                foreach (Match match in AcronymDesc.Find(markDown))
                {
                    var key = match.Value;
                    if (doneMatches.ContainsKey(key))
                        continue;
                    AcronymDesc d = new AcronymDesc(key, markDown);
                    doneMatches.Add(key, d);
                }
            }
            if (!doneMatches.Any())
            {
                MessageBox.Show("No matches found.");
            }

            AcronymDesc.RemovePluralForms(doneMatches);


            var sorted = doneMatches.OrderBy(x => x.Key);
            var sb = new StringBuilder();
            var cnt = 0;
            foreach (var item in sorted)
            {
                if (item.Value.Ignore)
                    continue;
                cnt++;
                sb.AppendLine(item.Key + "\t" + item.Value.ToS());
            }

            Clipboard.SetText(sb.ToString());
            MessageBox.Show($"{doneMatches.Count} matches found, {cnt} copied to clipboard, excluding ignores.");

        }

        private void SplitPars(object sender, RoutedEventArgs e)
        {
            var mdSource = SelectedMarkDown;
            using (var p = new PandocMarkDownReader(mdSource))
            {
                var broken = PandocMarkdown.BreakSentences(p);
                Clipboard.SetText(broken);
            }
        }
    }
}
