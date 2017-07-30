using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace AcademicSupport
{
    internal partial class TrackedFile
    {
        public FileInfo File;
        private List<TrackedFileStat> _stats = new List<TrackedFileStat>();

        public IEnumerable<DateTime> Dates
        {
            get { return _stats.Select(x => x.TimeStamp); }
        }

        public DateTime MinDate()
        {
            return Dates.Min();
        }

        public TrackedFile(FileInfo file)
        {
            File = file;
        }

        public TrackedFile(DirectoryInfo directoryInfo, string bName)
        {
            var fullName = Path.Combine(
                directoryInfo.FullName,
                bName
            );
            File = new FileInfo(fullName);
        }

        public void AddStat(TrackedFileStat stat)
        {
            stat.SetParent(this);
            if (LatestStat == null)
                LatestStat = stat;
            else if (stat.IsNewerThan(LatestStat))
                LatestStat = stat;
            _stats.Add(stat);
            
            _sorted = false;
        }

        public TrackedFileStat LatestStat { get; private set; }

        public static string BareName(FileInfo file, DirectoryInfo folder)
        {
            var tmp = file.FullName.Substring(folder.FullName.Length).TrimStart('\\');
            return tmp;
        }

        public bool Evaluate()
        {
            using (var f = File.OpenText())
            {
                var all = f.ReadToEnd();
                var wordCount = WordCount(all);
                var needUpdate = LatestStat == null || wordCount != LatestStat.WordCount;

                if (!needUpdate)
                    return false;
                var t = new TrackedFileStat
                {
                    WordCount = wordCount,
                    TimeStamp = File.LastWriteTime
                };
                AddStat(t);
            }
            return true;
        }

        private static int WordCount(string text)
        {
            var eval = text;
            eval = eval.Replace('\'', ' ');
            eval = eval.Replace('#', ' ');
            eval = eval.Replace('`', ' ');
            var sb = new StringBuilder();

            foreach (char c in eval)
            {
                if (!char.IsPunctuation(c))
                    sb.Append(c);
            }

            eval = sb.ToString();
            var regex = new Regex("\\s+", RegexOptions.IgnoreCase);
            return regex.Matches(eval).Count;
        }

        internal static bool ReadLog(string persisted, out string barename, out TrackedFileStat stat)
        {
            barename = "";
            stat = new TrackedFileStat();

            var pos = persisted.IndexOf("\",", StringComparison.Ordinal);
            if (pos == 0)
                return false;

            barename = persisted.Substring(1, pos - 1);
            var rest = persisted.Substring(pos + 2);
            stat = TrackedFileStat.Unpersist(rest);

            return true;
        }
        
        public string LatestLog(DirectoryInfo fld)
        {
            var bname = BareName(File, fld);
            return $"\"{bname}\",{LatestStat.Persist()}";
        }

        bool _sorted = true;

        public void EnsureSorted()
        {
            if (_sorted)
                return;
            _stats.Sort((x, y) => DateTime.Compare(x.TimeStamp, y.TimeStamp));

            int prev = 0;
            foreach (var trackedFileStat in _stats)
            {
                trackedFileStat.SetPreviousCount(prev);
                prev = trackedFileStat.WordCount;
            }
            _sorted = true;
        }

        Dictionary<DateTime, int> DailyProduction()
        {
            var ret = new Dictionary<DateTime, int>();
            foreach (var trackedFileStat in _stats)
            {
                var justdate = trackedFileStat.TimeStamp.Date;
                if (!ret.ContainsKey(justdate))
                {
                    ret.Add(justdate, 0);
                }
                ret[justdate] += trackedFileStat.Delta;
            }
            return ret;
        }
    }
}