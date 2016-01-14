using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using Infragistics.Windows.DataPresenter.DataSources;
using Infragistics.Controls;
using Reference.DataSources.OData;

namespace DataPresenter.DataSources.OData
{
	/// <summary>
	/// Represents an async paging data source (designed to work with the XamDataPresenter family of controls) that gets data from a remote server using the OData API.
	/// </summary>
	public sealed class ODataDataSource : AsyncPagingDataSourceBase 
    {
        #region Constructor
        /// <summary>
        /// Constructor
        /// </summary>
        public ODataDataSource()
        {
        }
		#endregion //Constructor

		#region Base Class Overrides

		#region CanFilter
		/// <summary>
		/// Gets a value that indicates whether this data source supports filtering via the <see cref="ICollectionView.Filter"/> property.
		/// </summary>
		/// <returns>True if this view support filtering; otherwise, false.</returns>
		protected override bool CanFilter
		{
			get
			{
				// TODO: Return the appropriate CanFilter value
				return false;
			}
		}
		#endregion //CanFilter

		#region CanGroup
		/// <summary>
		/// Gets a value that indicates whether this data source supports grouping via the <see cref="ICollectionView.GroupDescriptions"/> property.
		/// </summary>
		/// <returns>True if this view supports grouping; otherwise, false.</returns>
		protected override bool CanGroup
		{
			get
			{
				// TODO: Return the appropriate CanGroup value
				return false;
			}
		}
		#endregion //CanGroup

		#region CanSort
		/// <summary>
		/// Gets a value that indicates whether this data source supports sorting via the <see cref="ICollectionView.SortDescriptions"/> property.
		/// </summary>
		/// <returns>True if this view supports sorting; otherwise, false.</returns>
		protected override bool CanSort
		{
			get { return true; }
		}
		#endregion //CanSort

		#region CreateInstanceCore
		/// <summary>
		/// Creates a new instance of the System.Windows.Freezable derived class.
		/// </summary>
		/// <returns></returns>
		protected override Freezable CreateInstanceCore()
        {
            return new ODataDataSource();
        }
        #endregion //CreateInstanceCore

        #region CreateUnderlyingDataSource
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        protected override VirtualDataSource CreateUnderlyingDataSource()
        {
            return new ODataVirtualDataSource();
        }
        #endregion //CreateUnderlyingDataSource

        #endregion //Base Class Overrides

        #region //Properties

        #region BaseUri
		/// <summary>
		/// Returns the BaseUri DependencyProperty.
		/// </summary>
        public static readonly DependencyProperty BaseUriProperty = DependencyProperty.Register("BaseUri",
        typeof(string), typeof(ODataDataSource), new PropertyMetadata(default(string), (sender, e) =>
        {
            ((ODataDataSource)sender).OnBaseUriChanged((string)e.OldValue, (string)e.NewValue);
        }));

        private void OnBaseUriChanged(string oldValue, string newValue)
        {
            this.UnderlyingVirtualDataSource.ResetCache();
            if (this.UnderlyingVirtualDataSource.ActualDataProvider is ODataVirtualDataSourceDataProvider)
            {
                ((ODataVirtualDataSourceDataProvider)this.UnderlyingVirtualDataSource.ActualDataProvider).BaseUri = BaseUri;
            }
        }

        /// <summary>
        /// Returns/sets the root uri of the OData service endpoint that data should be fetched from.
        /// </summary>
        public string BaseUri
        {
            get { return (string)GetValue(BaseUriProperty); }
            set { SetValue(BaseUriProperty, value); }
        }
		#endregion //BaseUri

		#region EntitySet
		/// <summary>
		/// Returns the EntitySet DependencyProperty.
		/// </summary>
		public static readonly DependencyProperty EntitySetProperty = DependencyProperty.Register("EntitySet",
        typeof(string), typeof(ODataDataSource), new PropertyMetadata(default(string), (sender, e) =>
        {
            ((ODataDataSource)sender).OnEntitySetChanged((string)e.OldValue, (string)e.NewValue);
        }));

        private void OnEntitySetChanged(string oldValue, string newValue)
        {
            this.UnderlyingVirtualDataSource.ResetCache();
            if (this.UnderlyingVirtualDataSource.ActualDataProvider is ODataVirtualDataSourceDataProvider)
            {
                ((ODataVirtualDataSourceDataProvider)this.UnderlyingVirtualDataSource.ActualDataProvider).EntitySet = EntitySet;
            }
        }

        /// <summary>
        /// Gets or sets the desired entity set within the root OData API from which to retrieve data.
        /// </summary>
        public string EntitySet
        {
            get { return (string)GetValue(EntitySetProperty); }
            set { SetValue(EntitySetProperty, value); }
        }
		#endregion //EntitySet

		#region TimeoutMilliseconds
		/// <summary>
		/// Returns the TimeoutMilliseconds DependencyProperty.
		/// </summary>
		public static readonly DependencyProperty TimeoutMillisecondsProperty = DependencyProperty.Register("TimeoutMilliseconds",
        typeof(int), typeof(ODataDataSource), new PropertyMetadata(10000, (sender, e) =>
        {
            ((ODataDataSource)sender).OnTimeoutMillisecondsChanged((string)e.OldValue, (string)e.NewValue);
        }));

        private void OnTimeoutMillisecondsChanged(string oldValue, string newValue)
        {
            if (this.UnderlyingVirtualDataSource.ActualDataProvider is ODataVirtualDataSourceDataProvider)
            {
                ((ODataVirtualDataSourceDataProvider)this.UnderlyingVirtualDataSource.ActualDataProvider).TimeoutMilliseconds = TimeoutMilliseconds;
            }
        }

        /// <summary>
        /// Gets or sets the desired timeout to use for requests of the OData API.
        /// </summary>
        public int TimeoutMilliseconds
        {
            get { return (int)GetValue(TimeoutMillisecondsProperty); }
            set { SetValue(TimeoutMillisecondsProperty, value); }
        }
        #endregion //TimeoutMilliseconds

        #endregion //Properties
    }
}
