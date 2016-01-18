using Infragistics.Controls;
using Infragistics.Controls.DataSource;
using System;
using System.Collections.Generic;
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
                DesiredPageSize = 200
            };

            source.SortDescriptions.Add(new DataSourceSortDescription("ShipName", true));

            grid1.ItemsSource = source;
        }
    }
}
