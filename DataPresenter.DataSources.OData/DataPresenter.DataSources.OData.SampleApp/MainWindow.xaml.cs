using DataPresenter.DataSources.OData;
using Infragistics.Windows.DataPresenter;
using Infragistics.Windows.Themes;
using System;
using System.Collections;
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

namespace DataPresenter.DataSources.OData.SampleApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

			this.cboOdataSources.SelectedIndex = 0;
        }

		private void cboOdataSources_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			ODataSourceListItem ods = this.cboOdataSources.SelectedItem as ODataSourceListItem;
			if (null != ods)
			{
				if (this.chkNullOutDatasource.IsChecked.HasValue && this.chkNullOutDatasource.IsChecked.Value == true)
					this.dataGrid1.DataSource = null;

				this.dataGrid1.DataSource = new ODataDataSource { BaseUri = ods.BaseUri, EntitySet = ods.EntitySet };
			}
		}

		private void txtPendingMessage_TextChanged(object sender, TextChangedEventArgs e)
		{
			Infragistics.Windows.DataPresenter.Resources.Customizer.SetCustomizedString("DynamicDataPendingMessage", ((TextBox)sender).Text);
		}
	}

	public class ODataSourceListItem
	{
		public override string ToString()
		{
			return Description;
		}

		public string BaseUri { get; set; }
		public string Description { get; set; }
		public string EntitySet{ get; set; }
	}
}
