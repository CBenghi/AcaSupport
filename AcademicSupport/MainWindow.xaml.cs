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
                UpdateDisplay();
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
                return  new DirectoryInfo(pth);
            }
        }

        private void PandocLaunch(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var f = SelectedMarkDown;

            var s = new PandocStarter(SysFolder);
            var conversion = s.Convert(f);
            var ret = MessageBoxResult.Yes;
            if (!string.IsNullOrWhiteSpace(conversion.Report))
            {
                ret = MessageBox.Show(this,
                    $"Error in conversion:\r\n\r\n{conversion.Report}\r\nshall I open the file?", "Error",
                    MessageBoxButton.YesNo);
            }
            if (ret == MessageBoxResult.Yes)
            {
                System.Diagnostics.Process.Start(conversion.ConvertedFile.FullName);
            }
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
            var s = new PandocStarter(SysFolder);
            var fullBib = new FileInfo(s.BIB);

            var mdSource = SelectedMarkDown;
            var mdBibName = Path.ChangeExtension(mdSource.FullName, "bib");
            var mdBib = new FileInfo(mdBibName);



            var reRefKey = new Regex("@[a-zA-Z0-9:_]+", RegexOptions.Compiled);
            var reNewBib = new Regex("^@[a-z]+{(.+),$", RegexOptions.Compiled);

            var avails = new Dictionary<string, string>();
            using (var mdSourceS = fullBib.OpenText())
            {
                string key = "";
                string val = "";
                string line;
                while ((line = mdSourceS.ReadLine()) != null)
                {
                    
                    var testNewBib = reNewBib.Match(line);
                    if (testNewBib.Success)
                    {
                        key = "@" + testNewBib.Groups[1].Value;
                        val = line + "\r\n";
                    }
                    else if (line == "}")
                    {
                        val += line + "\r\n";
                        if (key != "")
                        {
                            avails.Add(key, val);
                        }
                        key = "";
                        val = "";
                    }
                    else
                    {
                        val += line + "\r\n";
                    }
                }
            }

            var doneMatches = new List<string>();
            using (var mdSourceS = mdSource.OpenText())
            using (var mdBibS = mdBib.CreateText())
            {
                var markDown = mdSourceS.ReadToEnd();
                foreach (Match match in reRefKey.Matches(markDown))
                {
                    var key = match.Value;
                    if (doneMatches.Contains(key))
                        continue;

                    string bib;
                    var found = avails.TryGetValue(key, out bib);
                    if (!found)
                        continue;
                    mdBibS.WriteLine(bib);
                    doneMatches.Add(key);
                }
            }
        }
    }
}
