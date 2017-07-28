using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace AcademicSupport
{
    internal class TrackedFile
    {
        public FileInfo File;
        private List<TrackedFileStat> _stats = new List<TrackedFileStat>();
        
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
            if (LatestStat == null)
                LatestStat = stat;
            else if (stat.IsNewerThan(LatestStat))
                LatestStat = stat;

            _stats.Add(stat);    
        }

        public TrackedFileStat LatestStat { get; private set; }

        public static string BareName(FileInfo file, DirectoryInfo folder)
        {
            var tmp = file.FullName.Substring(folder.FullName.Length).TrimStart('\\');
            return tmp;
        }

        public bool Evaluate()
        {
            var needUpdate = false;
            using (var f = File.OpenText())
            {
                var wordCount = 0;

                string all = f.ReadToEnd();
                wordCount = WordCount(all);

                string line;
                

                needUpdate = LatestStat == null || wordCount != LatestStat.WordCount;

                if (needUpdate)
                {
                    var t = new TrackedFileStat
                    {
                        WordCount = wordCount,
                        TimeStamp = DateTime.Now
                    };
                    AddStat(t);
                }
            }
            return needUpdate;
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
    }
}