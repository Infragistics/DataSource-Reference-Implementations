using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DataPresenter.DataSources.OData.SampleApp
{
	/// <summary>
	/// A class that defines a ViewModel implementation for the OData Sample App.
	/// </summary>
	public class SampleViewModel : INotifyPropertyChanged
	{
		#region Member Variables
		private int								_pageSizeRequested = 200;
		private int								_maxCachedPages = 200;
		private ODataDataSource					_currentDataSource;
		private DataSourceConfigurationInfo		_currentDataSourceConfigurationInfo;
		#endregion //Member Variables

		#region Properties

		#region CurrentDataSource
		/// <summary>
		/// Returns the current ODataDataSource based on the CurrentDataSourceConfigurationInfo property. (read only)
		/// </summary>
		public ODataDataSource CurrentDataSource
		{
			get
			{
				if (null == this._currentDataSource)
					this.CurrentDataSource = new ODataDataSource();

				return this._currentDataSource;
			}
			private set
			{
				if (this._currentDataSource != value)
				{
					this._currentDataSource = value;
					this.RaisePropertyChanged();
				}
			}
		}
		#endregion //CurrentDataSource

		#region CurrentDataSourceConfigurationInfo
		/// <summary>
		/// Returns/sets the DataSourceConfigurationInfo to use when creating the ODataDataSource that we 
		/// expose via the CurrentDataSource property.
		/// </summary>
		public DataSourceConfigurationInfo CurrentDataSourceConfigurationInfo
		{
			get { return this._currentDataSourceConfigurationInfo; }
			set
			{
				if (this._currentDataSourceConfigurationInfo != value)
				{
					this._currentDataSourceConfigurationInfo = value;
					this.RaisePropertyChanged();

					// ===============================================================================================================
					// We could define a subset of fields to retrieve and display in the grid by defining a DefaultFieldLayout
					// in the XamDataPresenter that includes a subset of all the fields in the Entity.  Not only will this
					// limit the number of fields that are displayed by the grid, but it will also improve response time by
					// limiting which columns of data are requested from the backend and transmitted over the connection.
					// ===============================================================================================================

					// ===============================================================================================================
					// Another way to limit the number of fields that are fetched from the backend to improve response time is to set 
					// the DesiredFields property on the ODataDataSource. Refer to the MainWindow.xaml file to see how we have limited
					// the number of fields requested and returned by the data source by setting the DesiredFields property of the 
					// DataSourceConfigurationInfo for the 'BuildingPermits' combobox item.  We will use this configuration info 
					// below when we create the ODataDataSource instance.
					// ===============================================================================================================


					// Create and return an instance of an ODataDataSource using the settings from the
					// current DataSourceConfigurationInfo.
					this.CurrentDataSource =
						new ODataDataSource
						{
							BaseUri				= this._currentDataSourceConfigurationInfo.BaseUri,
							EntitySet			= this._currentDataSourceConfigurationInfo.EntitySet,
							FieldsRequested		= this._currentDataSourceConfigurationInfo.FieldsRequested,
							PageSizeRequested	= this.PageSizeRequested,
							MaxCachedPages		= this.MaxCachedPages
						};
				}
			}
		}
		#endregion //CurrentDataSourceConfigurationInfo

		#region MaxCachedPages
		/// <summary>
		/// Returns/sets the maximum number of fetched pages that the CurrentDataSource should cache.
		/// </summary>
		public int MaxCachedPages
		{
			get { return this._maxCachedPages; }
			set
			{
				if (this._maxCachedPages!= value)
				{
					this._maxCachedPages= value;
					this.RaisePropertyChanged();

					this.CurrentDataSource.MaxCachedPages = value;
				}
			}
		}
		#endregion //MaxCachedPages

		#region PageSizeRequested
		/// <summary>
		/// Returns/sets the logical page size (i.e., the number of records requested by each fetch request) that the CurrentDataSource 
		/// should use when fetching data from a remote data source.
		/// </summary>
		public int PageSizeRequested
		{
			get { return this._pageSizeRequested; }
			set
			{
				if (this._pageSizeRequested != value)
				{
					this._pageSizeRequested = value;
					this.RaisePropertyChanged();

					this.CurrentDataSource.PageSizeRequested = value;
				}
			}
		}
		#endregion //PageSizeRequested

		#endregion //Properties

		#region INotifyPropertyChanged Implementation
		public event PropertyChangedEventHandler PropertyChanged;

		private void RaisePropertyChanged([CallerMemberName] string caller = "")
		{
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(caller));
		}
		#endregion //INotifyPropertyChanged Implementation
	}

	#region DataSourceConfigurationInfo Class
	/// <summary>
	/// Class that contains configuration info used by the SampleViewModel to create an instance
	/// of an ODataDataSource.
	/// </summary>
	public class DataSourceConfigurationInfo
	{
		/// <summary>
		/// Returns a string representation of the instance.
		/// </summary>
		/// <returns></returns>
		public override string ToString() { return Description; }

		/// <summary>
		/// Returns/sets the Uri of the OData service endpoint.
		/// </summary>
		public string BaseUri { get; set; }

		/// <summary>
		/// Returns/sets a description for the configuration information that can be displayed in the sample app UI.
		/// </summary>
		public string Description { get; set; }

		/// <summary>
		/// Returns/sets the EntitySet (i.e., 'table') within the OData service endpoint defined by the BaseUri
		/// from which data should be fetched.
		/// </summary>
		public string EntitySet { get; set; }

		/// <summary>
		/// Returns/sets an array of field names for which data should be fetched.  This field names specified must
		/// exist in the schema for the desired EntitySet.
		/// </summary>
		public string[] FieldsRequested { get; set; }
	}
	#endregion //DataSourceConfigurationInfo Class
}
