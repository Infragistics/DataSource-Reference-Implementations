using Infragistics.Controls.Interactions;
using Infragistics.Windows.Controls;
using Infragistics.Windows.DataPresenter;
using System;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace DataPresenter.DataSources.OData.SampleApp
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
    {
		#region Constructor
		public MainWindow()
        {
            InitializeComponent();

			#region UI Setup
			// Listen to the XamDataPresenter's ThemeChanged event so we can initialize the DataPendingOverlayBrush color picker when the Theme changes.
			this.dataPresenter1.ThemeChanged += (s,e) => { this.Dispatcher.BeginInvoke(new Action(() => this.InitializeColorPicker()), System.Windows.Threading.DispatcherPriority.ApplicationIdle); };

			// Initialize the list of themes and select 'Office2013'.
			this.cboThemes.ItemsSource		= Infragistics.Windows.Themes.ThemeManager.GetThemes();
			this.cboThemes.SelectedValue	= "RoyalDark";
			
			// Initialize the list of XamBusyIndicator Animations.
			this.cboBusyIndicatorAnimations.ItemsSource		= typeof(BusyAnimations).GetFields(BindingFlags.Public | BindingFlags.Static).Select((prop) => prop.Name).ToArray();
			this.cboBusyIndicatorAnimations.SelectedValue	= "Gears";
			#endregion //UI Setup
		}
		#endregion //Constructor

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

		#endregion //Event Handlers
	}
}
