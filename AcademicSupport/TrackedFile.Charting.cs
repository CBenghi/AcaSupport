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
                var rounded = Math.Round(ts.TotalDays * 20) /20 ;
                pts.Add(new ObservablePoint(rounded, trackedFileStat.WordCount));
            }
            
            var v = new LineSeries
            {
                Values = pts,
                PointGeometrySize = 15
            };

            return v;
        }

        private ChartValues<double> DailyValues(List<DateTime> requiredDays)
        {
            var dailyDictionary = DailyProduction();

            var ret = new ChartValues<double>();
            foreach (var requiredDay in requiredDays)
            {
                if (dailyDictionary.ContainsKey(requiredDay))
                {
                    ret.Add(dailyDictionary[requiredDay]);
                }
                else
                {
                    ret.Add(0);
                }
            }
            return ret;
        }

        public StackedColumnSeries DailySeries(List<DateTime> requiredDays)
        {
            var v = new ChartValues<double> {1, 0, 3, 4};
            return new StackedColumnSeries
                {
                    Values = DailyValues(requiredDays),
                    StackMode = StackMode.Values,
                    DataLabels = true
                };
        }
    }
}
