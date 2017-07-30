using System;
using System.Globalization;
using System.Linq;

namespace AcademicSupport
{
    internal partial class TrackedFileStat
    {
        private TrackedFile _parent;

        internal static TrackedFileStat Unpersist(string persistedString)
        {
            var ret = new TrackedFileStat();
            var vals = persistedString.Split(new[] {","}, StringSplitOptions.RemoveEmptyEntries);
            var cnt = vals.Count();
            if (cnt > 0) 
                ret.TimeStamp = DateTime.ParseExact(vals[0], "O", CultureInfo.InvariantCulture);
            if (cnt > 1)
                ret.WordCount = Convert.ToInt32(vals[1]);
            return ret;
        }

        private int _delta;
        public int Delta
        {
            get
            {
                _parent.EnsureSorted();
                return _delta;
            }
        }

        public DateTime TimeStamp;
        public int WordCount;

        public bool IsNewerThan(TrackedFileStat otherStat)
        {
            return TimeStamp >= otherStat.TimeStamp;
        }

        public string Persist()
        {
            return $"{TimeStamp:O},{WordCount}";
        }

        public void SetPreviousCount(int prev)
        {
            _delta = WordCount - prev;
        }

        public void SetParent(TrackedFile trackedFile)
        {
            _parent = trackedFile;
        }
    }
}
