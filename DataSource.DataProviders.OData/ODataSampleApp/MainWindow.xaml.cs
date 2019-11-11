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
                // The Northwind OData service doesn't support aggregation so grouping and summaries are not supported. Because of this
                // IsAggregationSupportedByServer is set to false.
                BaseUri = "https://services.odata.org/V4/Northwind/Northwind.svc",
                EntitySet = "Orders",
                PageSizeRequested = 200,
                IsAggregationSupportedByServer = false

                // This is for the provided sample OData service. You may need to change the port number
                // depending on what it chose when you run the service. This sample service supports aggregation
                // but currently there is an issue in the Simple.OData.Client package that we're using in our ODataVirtualDataSource
                // so aggregation is turned off.
                // https://github.com/simple-odata-client/Simple.OData.Client/issues/688
                //
                // Once this is resolved you should be able to set IsAggregationSupportedByServer to true and add GroupDescriptions to
                // the grid.
                //BaseUri = "http://localhost:1284/",
                //EntitySet = "Orders",
                //PageSizeRequested = 200,
                //IsAggregationSupportedByServer = false
            };
            //source.DeferAutoRefresh = true;
            source.SchemaChanged += Source_SchemaChanged;

            source.SortDescriptions.Add(new SortDescription("ShipName", ListSortDirection.Descending));

            grid1.SelectionMode = GridSelectionMode.SingleRow;

            grid1.ItemsSource = source;
            

            //grid1.SelectedItemsChanged += Grid1_SelectedItemsChanged;

            //Task.Delay(10000).ContinueWith((t) =>
            //{
            //    Dispatcher.BeginInvoke(new Action(() =>
            //    {
            //        source.DeferAutoRefresh = false;
            //    }));
            //});
        }

        private void Grid1_SelectedItemsChanged(object sender, GridSelectedItemsChangedEventArgs args)
        {
            if (grid1.FilterExpressions.Count == 0)
            {
                grid1.FilterExpressions.Add(
                FilterFactory.Build((f) =>
                {
                    return f.Property("ShipName").ToUpper().StartsWith("ALF");
                }));
            }
            else
            {
                grid1.FilterExpressions.Clear();
            }
        }

        private void Source_SchemaChanged(object sender, DataSourceSchemaChangedEventArgs args)
        {
            System.Diagnostics.Debug.WriteLine("schema fetched: " + args.Count);
        }
    }
}
