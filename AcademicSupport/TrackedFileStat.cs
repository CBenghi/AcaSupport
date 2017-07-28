using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AcademicSupport
{
    internal class TrackedFileStat
    {
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

        public DateTime TimeStamp;
        public int WordCount;

        public bool IsNewerThan(TrackedFileStat otherStat)
        {
            return TimeStamp > otherStat.TimeStamp;
        }

        public string Persist()
        {
            return $"{TimeStamp:O},{WordCount}";
        }
    }
}
