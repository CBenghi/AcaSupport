using System;
using System.Windows.Controls;
using LiveCharts;
using LiveCharts.Wpf;

namespace AcademicSupport
{
    /// <summary>
    /// Interaction logic for StackedColumnExample.xaml
    /// </summary>
    public partial class StackedColumn : UserControl
    {
        public StackedColumn()
        {
            InitializeComponent();

            SeriesCollection = new SeriesCollection();
            //{
            //    new StackedColumnSeries
            //    {
            //        Values = new ChartValues<double> {1, 2, 3, 4},
            //        StackMode = StackMode.Values, // this is not necessary, values is the default stack mode
            //        DataLabels = true
            //    },
            //    new StackedColumnSeries
            //    {
            //        Values = new ChartValues<double> {4, 4, 4, 4},
            //        StackMode = StackMode.Values,
            //        DataLabels = true
            //    }
            //};          

            //Labels = new[] { "Chrome", "Mozilla", "Opera", "IE" };
            
            DataContext = this;
        }

        public string Title { get; set; }

        public string[] Labels { get; set; }

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
                    
                }
                catch (Exception)
                {
                    // ignored
                }
            }
        }

    }
}