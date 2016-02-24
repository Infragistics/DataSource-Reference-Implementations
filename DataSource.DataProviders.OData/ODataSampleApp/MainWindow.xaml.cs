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
