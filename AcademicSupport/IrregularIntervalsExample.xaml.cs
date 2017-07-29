using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows.Controls;
using LiveCharts;
using LiveCharts.Defaults;
using LiveCharts.Wpf;

namespace Wpf.CartesianChart.Irregular_Intervals
{
    public partial class IrregularIntervalsExample : UserControl
    {
        private SeriesCollection _seriesCollection;

        public IrregularIntervalsExample()
        {
            InitializeComponent();
            DataContext = this;
        }
        
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

                    Display.AxisY.FirstOrDefault().MinValue = 0;
                }
                catch (Exception)
                {
                    
                }
            }
        }
    }
}