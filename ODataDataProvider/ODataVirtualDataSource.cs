using Infragistics.Controls;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Simple.OData.Client;
using System.Diagnostics;
using System.Windows;
using Infragistics.Controls.DataSource;
#if PCL
using Infragistics.Core.Controls.DataSource;
#endif

#if WINDOWS_UWP
using Windows.UI;
using Windows.UI.Xaml;
#endif

#if DATA_PRESENTER
namespace Reference.DataSources.OData
#else
namespace Infragistics.Controls.DataSource
#endif
{

    /// <summary>
    /// Represents a virtual data source that gets data from a remote server using the OData API.
    /// </summary>
    /// <remarks>
    /// Access to this class from threads other than the UI thread should be syncrhonized using the ExecutionContext.
    /// </remarks>
    public class ODataVirtualDataSource
            : VirtualDataSource
    {
        protected override IDataSourceVirtualDataProvider ResolveDataProviderOverride()
        {
            return new ODataVirtualDataSourceDataProvider();
        }

#if PCL
		private void OnBaseUriChanged(string oldValue, string newValue)
		{
			if (ActualDataProvider is ODataVirtualDataSourceDataProvider)
			{
				((ODataVirtualDataSourceDataProvider)ActualDataProvider).BaseUri = BaseUri;
			}
			QueueAutoRefresh();
		}

		private string _baseUri = null;
		/// <summary>
		/// Gets or sets the root uri of the OData API that data should be fetched from.
		/// </summary>
		public string BaseUri
		{
			get
			{
				return _baseUri;
			}
			set
			{
				var oldValue = _baseUri;
				_baseUri = value;
				if (oldValue != _baseUri)
				{
					OnBaseUriChanged(oldValue, _baseUri);
				}
			}
		}

		private void OnEntitySetChanged(string oldValue, string newValue)
		{
			if (ActualDataProvider is ODataVirtualDataSourceDataProvider)
			{
				((ODataVirtualDataSourceDataProvider)ActualDataProvider).EntitySet = EntitySet;
			}
			QueueAutoRefresh();
		}

		private string _entitySet = null;
		/// <summary>
		/// Gets or sets the desired entity set within the root OData API from which to retrieve data.
		/// </summary>
		public string EntitySet
		{
			get
			{
				return _entitySet;
			}
			set
			{
				var oldValue = _entitySet;
				_entitySet = value;
				if (_entitySet != oldValue)
				{
					OnEntitySetChanged(oldValue, _entitySet);
				}
			}
		}

		private void OnTimeoutMillisecondsChanged(int oldValue, int newValue)
		{
			if (ActualDataProvider is ODataVirtualDataSourceDataProvider)
			{
				((ODataVirtualDataSourceDataProvider)ActualDataProvider).TimeoutMilliseconds = TimeoutMilliseconds;
			}
		}

		private int _timeoutMilliseconds = 10000;
		/// <summary>
		/// Gets or sets the desired timeout to use for requests of the OData API.
		/// </summary>
		public int TimeoutMilliseconds
		{
			get
			{
				return _timeoutMilliseconds;
			}
			set
			{
				var oldValue = _timeoutMilliseconds;
				_timeoutMilliseconds = value;
				if (oldValue != _timeoutMilliseconds)
				{
					OnTimeoutMillisecondsChanged(oldValue, _timeoutMilliseconds);
				}
			}
		}
#else
		public static readonly DependencyProperty BaseUriProperty = DependencyProperty.Register("BaseUri",
        typeof(string), typeof(ODataVirtualDataSource), new PropertyMetadata(default(string), (sender, e) =>
        {
            ((ODataVirtualDataSource)sender).OnBaseUriChanged((string)e.OldValue, (string)e.NewValue);
        }));

        private void OnBaseUriChanged(string oldValue, string newValue)
        {
            if (ActualDataProvider is ODataVirtualDataSourceDataProvider)
            {
                ((ODataVirtualDataSourceDataProvider)ActualDataProvider).BaseUri = BaseUri;
            }
            QueueAutoRefresh();
        }

        /// <summary>
        /// Gets or sets the root uri of the OData API that data should be fetched from.
        /// </summary>
        public string BaseUri
        {
            get
            {
                return (string)GetValue(BaseUriProperty);
            }
            set
            {
                SetValue(BaseUriProperty, value);
            }
        }

        public static readonly DependencyProperty EntitySetProperty = DependencyProperty.Register("EntitySet",
        typeof(string), typeof(ODataVirtualDataSource), new PropertyMetadata(default(string), (sender, e) =>
        {
            ((ODataVirtualDataSource)sender).OnEntitySetChanged((string)e.OldValue, (string)e.NewValue);
        }));

        private void OnEntitySetChanged(string oldValue, string newValue)
        {
            if (ActualDataProvider is ODataVirtualDataSourceDataProvider)
            {
                ((ODataVirtualDataSourceDataProvider)ActualDataProvider).EntitySet = EntitySet;
            }
            QueueAutoRefresh();
        }

        /// <summary>
        /// Gets or sets the desired entity set within the root OData API from which to retrieve data.
        /// </summary>
        public string EntitySet
        {
            get
            {
                return (string)GetValue(EntitySetProperty);
            }
            set
            {
                SetValue(EntitySetProperty, value);
            }
        }

        public static readonly DependencyProperty TimeoutMillisecondsProperty = DependencyProperty.Register("TimeoutMilliseconds",
        typeof(int), typeof(ODataVirtualDataSource), new PropertyMetadata(10000, (sender, e) =>
        {
            ((ODataVirtualDataSource)sender).OnTimeoutMillisecondsChanged((string)e.OldValue, (string)e.NewValue);
        }));

        private void OnTimeoutMillisecondsChanged(string oldValue, string newValue)
        {
            if (ActualDataProvider is ODataVirtualDataSourceDataProvider)
            {
                ((ODataVirtualDataSourceDataProvider)ActualDataProvider).TimeoutMilliseconds = TimeoutMilliseconds;
            }
        }

        /// <summary>
        /// Gets or sets the desired timeout to use for requests of the OData API.
        /// </summary>
        public int TimeoutMilliseconds
        {
            get
            {
                return (int)GetValue(TimeoutMillisecondsProperty);
            }
            set
            {
                SetValue(TimeoutMillisecondsProperty, value);
            }
        }
#endif
	}

}