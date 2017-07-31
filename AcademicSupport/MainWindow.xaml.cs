using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        private List<string> _ignoredFiles = new List<string>();

        public MainWindow()
        {
            InitializeComponent();
            TxtFolder.Text = Settings.Default.InspectionFolder;
            if (TxtFolder.Text == "")
            {
                var d = new DirectoryInfo(".");
                TxtFolder.Text = d.FullName;
            }
            if (!TxtFolder.Text.EndsWith(".system"))
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
            UpdateFileCountCurve();
            UpdateDailyCountBars();
        }

        private IEnumerable<TrackedFile> FileToChart
        {
            get
            {
                foreach (var trackedFile in _trackedFiles)
                {
                    if (_ignoredFiles.Contains(trackedFile.Key))
                        continue;
                    yield return trackedFile.Value;
                }
            }
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
                foreach (var file in fld.GetFiles("*.md"))
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
            _ignoredFiles = new List<string>();
            _trackedFiles = new Dictionary<string, TrackedFile>();
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
                        var file = line.Substring(7).Trim();
                        if (!string.IsNullOrWhiteSpace(file))
                        {
                            _ignoredFiles.Add(file);
                        }
                    }
                    else if (TrackedFile.ReadLog(line, out bName, out stat))
                    {
                        if (!_trackedFiles.ContainsKey(bName))
                        {
                            _trackedFiles.Add(bName, new TrackedFile(GetFolder(), bName));
                        }
                        _trackedFiles[bName].AddStat(stat);
                    }
                }
            }
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
    }
}
