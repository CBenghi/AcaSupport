using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LiveCharts;
using LiveCharts.Defaults;
using LiveCharts.Wpf;

namespace AcademicSupport
{
    internal partial class TrackedFile
    {
        public LineSeries ToSeries(DateTime referenceDate)
        {
            var pts = new ChartValues<ObservablePoint>();

            foreach (var trackedFileStat in _stats)
            {
                var ts = trackedFileStat.TimeStamp - referenceDate;
                double rounded = Math.Round(ts.TotalDays * 20) /20 ;
                pts.Add(new ObservablePoint(rounded, trackedFileStat.WordCount));
            }
            
            var v = new LineSeries
            {
                Values = pts,
                PointGeometrySize = 15
            };

            return v;
        }
    }
}
