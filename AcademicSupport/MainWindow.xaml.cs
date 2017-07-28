using System.Collections.Generic;
using System.IO;
using System.Windows;
using AcademicSupport.Properties;

namespace AcademicSupport
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            TxtFolder.Text = Settings.Default.InspectionFolder;
            if (TxtFolder.Text == "")
            {
                var d = new DirectoryInfo(".");
                TxtFolder.Text = d.FullName;
            }
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            if (!SaveConfig())
                return;
            
            UpdateWordCount();
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
            _trackedFiles = new Dictionary<string, TrackedFile>();
            var log = LogFile;
            if (log == null)
                return;

            using (var logF = log.OpenText())
            {
                string line;
                while ((line = logF.ReadLine()) != null)
                {
                    string bName;
                    TrackedFileStat stat;
                    if (TrackedFile.ReadLog(line, out bName, out stat))
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
    }
}
