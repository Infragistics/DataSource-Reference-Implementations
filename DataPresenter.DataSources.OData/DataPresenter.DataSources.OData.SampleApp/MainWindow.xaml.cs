using DataPresenter.DataSources.OData;
using Infragistics.Controls.Interactions;
using Infragistics.Windows.Controls;
using Infragistics.Windows.DataPresenter;
using Infragistics.Windows.Themes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
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

			this.numPageSizeRequested.Value						= 200;
			this.numMaxCachedPages.Value						= 200;
			this.cboOdataSources.SelectedIndex					= 0;
			this.cboDataPresenterView.SelectedIndex				= 0;
			this.cboRecordFilterLogicalOperator.SelectedIndex	= 0;

			// Listen to the XamDataPresenter's ThemeChanged event so we can initialize the DataPendingOverlayBrush color picker when the Theme changes.
			this.dataPresenter1.ThemeChanged += (s,e) => { this.Dispatcher.BeginInvoke(new Action(() => this.InitializeColorPicker()), System.Windows.Threading.DispatcherPriority.ApplicationIdle); };

			// Initialize the list of themes and select 'Office2013'.
			this.cboThemes.ItemsSource		= Infragistics.Windows.Themes.ThemeManager.GetThemes();
			this.cboThemes.SelectedValue	= "Office2013";
			
			// Initialize the list of XamBusyIndicator Animations.
			this.cboBusyIndicatorAnimations.ItemsSource		= typeof(BusyAnimations).GetFields(BindingFlags.Public | BindingFlags.Static).Select((prop) => prop.Name).ToArray();
			this.cboBusyIndicatorAnimations.SelectedValue	= "Gears";
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
				if (null != this.dataPresenter1)
					return this.dataPresenter1.DataSource as ODataDataSource;

				return null;
			}
		}
		#endregion //CurrentDataSource

		#region PageSizeRequested
		private int PageSizeRequested
		{
			get { return Convert.ToInt32((double)this.numPageSizeRequested.Value); }
		}
		#endregion //PageSizeRequested

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

		#region MaxCachedPages
		private int MaxCachedPages
		{
			get { return Convert.ToInt32((double)this.numMaxCachedPages.Value); }
		}
		#endregion //MaxCachedPages

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

		#region Private Methods

		#region InitializeColorPicker
		private void InitializeColorPicker()
		{
			// Initialize the color picker with the current DataPendingOverlayBrush.
			if (this.dataPresenter1.Resources.Contains(DataPresenterBrushKeys.DataPendingOverlayBrushKey))
			{
				SolidColorBrush overlayBrush = this.dataPresenter1.Resources[DataPresenterBrushKeys.DataPendingOverlayBrushKey] as SolidColorBrush;
				if (null != overlayBrush && overlayBrush.Color != this.colorPicker.SelectedColor)
					this.colorPicker.SelectedColor = overlayBrush.Color;
			}
		}
		#endregion //InitializeColorPicker

		#endregion //Private Methods

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
			// Establish the DataSourceConfigurationInfo combobox item that was selected.  Refer to the MainWindow.xaml file
			// to see how these combobox items were defined.
			DataSourceConfigurationInfo dataSourceConfigInfo = this.cboOdataSources.SelectedItem as DataSourceConfigurationInfo;
			if (null != dataSourceConfigInfo)
			{
				// If requested, null out the grid's DataSource before setting the new one.
				if (this.chkNullOutDatasource.IsChecked.HasValue && this.chkNullOutDatasource.IsChecked.Value == true)
					this.dataPresenter1.DataSource = null;

				// Reset some FieldLayout related fields and settings.
				this.dataPresenter1.FieldLayouts.Clear();
				this.dataPresenter1.DefaultFieldLayout = null;
				this.dataPresenter1.FieldLayoutSettings.AutoGenerateFields = true;

				// For demo purposes, define a subset of fields to display in the grid.
				//
				// For the Northwind Orders table let's limit the number of fields by defining a DefaultFieldLayout
				// in the XamDataPresenter that includes a subset of all the fields in the Orders table.  Not only will this
				// limit the number of fields that are displayed by the grid, but it will also improve response time by
				// limiting which columns of data are requested from the backend and transmitted over the connection.
				if (dataSourceConfigInfo.BaseUri == @"http://services.odata.org/V4/Northwind/Northwind.svc" && dataSourceConfigInfo.EntitySet == "Orders")
				{
					this.dataPresenter1.FieldLayoutSettings.AutoGenerateFields = false;
					FieldLayout fieldLayout = new FieldLayout();
					fieldLayout.Fields.Add(new Field("CustomerID", typeof(string)));
					fieldLayout.Fields.Add(new Field("EmployeeID", typeof(int)));
					fieldLayout.Fields.Add(new Field("ShipName", typeof(string)));
					fieldLayout.Fields.Add(new Field("ShipAddress", typeof(string)));
					fieldLayout.Fields.Add(new Field("ShipCity", typeof(string)));
					fieldLayout.Fields.Add(new Field("ShipRegion", typeof(string)));
					fieldLayout.Fields.Add(new Field("ShipPostalCode", typeof(string)));

					this.dataPresenter1.FieldLayouts.Add(fieldLayout);
					fieldLayout.IsDefault = true;
				}


				// ======================================================================================================
				// Another way to limit the number of fields that are fetched from the backend to improve response time is to set 
				// the DesiredFields property on the ODataDataSource. Refer to the MainWindow.xaml file to see how we have limited
				// the number of fields requested and returned by the data source by setting the DesiredFields property of the 
				// DataSourceConfigurationInfo combobox item.  We will use this configuration info below when we create the
				// ODataDataSource instance.
				// ======================================================================================================


				// Set the grid's DataSource to an instance of an ODataDataSource created using the settings from the
				// selected DataSourceConfigurationInfo.
				this.dataPresenter1.DataSource = 
					new ODataDataSource {	BaseUri = dataSourceConfigInfo.BaseUri,
											EntitySet = dataSourceConfigInfo.EntitySet,
											FieldsRequested = dataSourceConfigInfo.FieldsRequested,
											PageSizeRequested = this.PageSizeRequested,
											MaxCachedPages = this.MaxCachedPages };
			}
		}
		#endregion //cboOdataSources_SelectionChanged

		#region cboRecordFilterLogicalOperator_SelectionChanged
		private void cboRecordFilterLogicalOperator_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (null == this.dataPresenter1)
				return;

			string logicalOperator = (string)this.cboRecordFilterLogicalOperator.SelectedValue;
			switch (logicalOperator)
			{
				case "And":
					this.dataPresenter1.FieldLayoutSettings.RecordFiltersLogicalOperator = LogicalOperator.And;
					break;
				case "Or":
					this.dataPresenter1.FieldLayoutSettings.RecordFiltersLogicalOperator = LogicalOperator.Or;
					break;
			}
		}
		#endregion //cboRecordFilterLogicalOperator_SelectionChanged

		#region cboThemes_SelectionChanged
		private void cboThemes_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			// Remove the existing DataPendingOverlayBrush resource (if any) from the XamDataPresenter Resources dictionary.
			if (this.dataPresenter1.Resources.Contains(DataPresenterBrushKeys.DataPendingOverlayBrushKey))
				this.dataPresenter1.Resources.Remove(DataPresenterBrushKeys.DataPendingOverlayBrushKey);
		}
		#endregion //cboThemes_SelectionChanged

		#region colorPicker_SelectedColorChanged
		private void colorPicker_SelectedColorChanged(object sender, Infragistics.Controls.Editors.SelectedColorChangedEventArgs e)
		{
			if (e.NewColor.HasValue)
			{
				// Remove the existing DataPendingOverlayBrush resource (if any) from the XamDataPresenter Resources dictionary.
				if (this.dataPresenter1.Resources.Contains(DataPresenterBrushKeys.DataPendingOverlayBrushKey))
				{
					SolidColorBrush brush = this.dataPresenter1.Resources[DataPresenterBrushKeys.DataPendingOverlayBrushKey] as SolidColorBrush;
					if (brush.Color != e.NewColor.Value)
					{
						this.dataPresenter1.Resources.Remove(DataPresenterBrushKeys.DataPendingOverlayBrushKey);

						// Add a new DataPendingOverlayBrush resource for the selected color to the XamDataPresenter Resources dictionary.
						this.dataPresenter1.Resources.Add(DataPresenterBrushKeys.DataPendingOverlayBrushKey, new SolidColorBrush(e.NewColor.Value));
					}
				}
				else
					// Add a new DataPendingOverlayBrush resource for the selected color to the XamDataPresenter Resources dictionary.
					this.dataPresenter1.Resources.Add(DataPresenterBrushKeys.DataPendingOverlayBrushKey, new SolidColorBrush(e.NewColor.Value));
			}
		}
		#endregion //colorPicker_SelectedColorChanged

		#region numPageSizeRequested_EditModeEnded
		private void numPageSizeRequested_EditModeEnded(object sender, Infragistics.Windows.Editors.Events.EditModeEndedEventArgs e)
		{
			if (null != this.CurrentDataSource)
				this.CurrentDataSource.PageSizeRequested = this.PageSizeRequested;
		}
		#endregion //numPageSizeRequested_EditModeEnded

		#region numMaxCachedPages_EditModeEnded
		private void numMaxCachedPages_EditModeEnded(object sender, Infragistics.Windows.Editors.Events.EditModeEndedEventArgs e)
		{
			if (null != this.CurrentDataSource)
				this.CurrentDataSource.MaxCachedPages = this.MaxCachedPages;
		}
		#endregion //numMaxCachedPages_EditModeEnded

		#endregion //Event Handlers
	}

	#region DataSourceConfigurationInfo Class
	public class DataSourceConfigurationInfo
	{
		public override string ToString() { return Description; }

		public string BaseUri { get; set; }
		public string Description { get; set; }
		public string EntitySet{ get; set; }
		public string[] FieldsRequested { get; set; }
	}
	#endregion //DataSourceConfigurationInfo Class
}
