using System;
using System.Linq;
using System.Windows.Controls;
using LiveCharts;

namespace AcademicSupport
{
    public partial class IrregularIntervals : UserControl
    {
        public IrregularIntervals()
        {
            InitializeComponent();
            DataContext = this;
        }

        private SeriesCollection _seriesCollection;

        public SeriesCollection SeriesCollection
        {
            get { return _seriesCollection; }
            set
            {
                if (_seriesCollection == null)
                {
                    _seriesCollection = value;
                    return;
                }
                try
                {
                    _seriesCollection.Clear();
                    _seriesCollection.AddRange(value);

                    var f = Display.AxisY.FirstOrDefault();
                    if (f != null)
                        f.MinValue = 0;
                }
                catch (Exception)
                {
                    // ignored
                }
            }
        }
    }
}