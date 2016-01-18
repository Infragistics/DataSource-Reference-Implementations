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
		#region Member Variables
		private Infragistics.Windows.DataPresenter.GridView			_gridView;
		private Infragistics.Windows.DataPresenter.CardView			_cardView;
		private Infragistics.Windows.DataPresenter.CarouselView		_carouselView;
		private Infragistics.Windows.DataPresenter.TreeView			_treeView;
		#endregion //Member Variables

		#region Constructor
		public MainWindow()
        {
            InitializeComponent();

			this.cboOdataSources.SelectedIndex = 0;
			this.cboDataPresenterView.SelectedIndex = 0;
			this.numDesiredPageSize.Value = 200;
			this.numMaximumCachedPages.Value = 200;
        }
		#endregion //Constructor

		#region Private Properties

		#region CardView
		private Infragistics.Windows.DataPresenter.CardView CardView
		{
			get
			{
				if (null == this._cardView)
					this._cardView = new Infragistics.Windows.DataPresenter.CardView();

				return this._cardView;
			}
		}
		#endregion //CardView

		#region CarouselView
		private Infragistics.Windows.DataPresenter.CarouselView CarouselView
		{
			get
			{
				if (null == this._carouselView)
					this._carouselView = new Infragistics.Windows.DataPresenter.CarouselView();

				return this._carouselView;
			}
		}
		#endregion //CarouselView

		#region CurrentDataSource
		private ODataDataSource CurrentDataSource
		{
			get
			{
				if (null != this._cardView)
					return this.dataPresenter1.DataSource as ODataDataSource;

				return null;
			}
		}
		#endregion //CurrentDataSource

		#region GridView
		private Infragistics.Windows.DataPresenter.GridView GridView
		{
			get
			{
				if (null == this._gridView)
					this._gridView = new Infragistics.Windows.DataPresenter.GridView();

				return this._gridView;
			}
		}
		#endregion //GridView

		#region TreeView
		private Infragistics.Windows.DataPresenter.TreeView TreeView
		{
			get
			{
				if (null == this._treeView)
					this._treeView = new Infragistics.Windows.DataPresenter.TreeView();

				return this._treeView;
			}
		}
		#endregion //TreeView

		#endregion //Private Properties

		#region Event Handlers

		#region cboDataPresenterView_SelectionChanged
		private void cboDataPresenterView_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (null == this.dataPresenter1)
				return;

			string selectedView = (string)this.cboDataPresenterView.SelectedValue;
			switch (selectedView)
			{
				case "CardView":
					this.dataPresenter1.View = this.CardView;
					break;
				case "CarouselView":
					this.dataPresenter1.View = this.CarouselView;
					break;
				case "GridView":
					this.dataPresenter1.View = this.GridView;
					break;
				case "TreeView":
					this.dataPresenter1.View = this.TreeView;
					break;
			}
		}
		#endregion //cboDataPresenterView_SelectionChanged

		#region cboOdataSources_SelectionChanged
		private void cboOdataSources_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			ODataSourceListItem ods = this.cboOdataSources.SelectedItem as ODataSourceListItem;
			if (null != ods)
			{
				// If requested, null out the grid's DataSource before setting the new one.
				if (this.chkNullOutDatasource.IsChecked.HasValue && this.chkNullOutDatasource.IsChecked.Value == true)
					this.dataPresenter1.DataSource = null;

				// Set the grid's DataSource to an instance of an ODataDataSource for the selected Uri and EntitySet..
				this.dataPresenter1.DataSource = new ODataDataSource { BaseUri = ods.BaseUri, EntitySet = ods.EntitySet };
			}
		}
		#endregion //cboOdataSources_SelectionChanged

		#region numDesiredPageSize_ValueChanged
		private void numDesiredPageSize_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
		{
			if (null != this.CurrentDataSource)
				this.CurrentDataSource.DesiredPageSize = (int)e.NewValue;
		}
		#endregion //numDesiredPageSize_ValueChanged

		#region numMaximumCachedPages_ValueChanged
		private void numMaximumCachedPages_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
		{
			if (null != this.CurrentDataSource)
				this.CurrentDataSource.MaximumCachedPages = (int)e.NewValue;
		}
		#endregion //numMaximumCachedPages_ValueChanged

		#region txtPendingMessage_TextChanged
		private void txtPendingMessage_TextChanged(object sender, TextChangedEventArgs e)
		{
			// Update the dynamic resource string for the 'dynamic data pending' message.
			Infragistics.Windows.DataPresenter.Resources.Customizer.SetCustomizedString("DynamicDataPendingMessage", ((TextBox)sender).Text);
		}
		#endregion //txtPendingMessage_TextChanged

		#endregion //Event Handlers
	}

	public class ODataSourceListItem
	{
		public override string ToString() { return Description; }

		public string BaseUri { get; set; }
		public string Description { get; set; }
		public string EntitySet{ get; set; }
	}
}
