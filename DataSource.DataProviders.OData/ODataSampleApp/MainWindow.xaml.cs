using Infragistics.Controls;
using Infragistics.Controls.DataSource;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ODataSampleApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            var source = new ODataVirtualDataSource()
            {
                BaseUri = "http://services.odata.org/V4/Northwind/Northwind.svc",
                EntitySet = "Orders",
                PageSizeRequested = 200
            };
            //source.DeferAutoRefresh = true;
            source.SchemaChanged += Source_SchemaChanged;

            source.SortDescriptions.Add(new SortDescription("ShipName", ListSortDirection.Descending));

            source.FilterExpressions.Add(
                FilterFactory.Build((f) =>
                {
                    return f.Property("ShipName").ToUpper().IsLessThan("G")
                    .Or(f.Property("ShipName").ToUpper().IsGreaterThan("U"));
                }));

            grid1.ItemsSource = source;

            //Task.Delay(10000).ContinueWith((t) =>
            //{
            //    Dispatcher.BeginInvoke(new Action(() =>
            //    {
            //        source.DeferAutoRefresh = false;
            //    }));
            //});
        }

        private void Source_SchemaChanged(object sender, DataSourceSchemaChangedEventArgs args)
        {
            System.Diagnostics.Debug.WriteLine("schema fetched: " + args.Count);
        }
    }
}
