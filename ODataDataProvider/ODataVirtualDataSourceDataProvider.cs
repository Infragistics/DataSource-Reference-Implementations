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
#if !PCL
using Infragistics.Controls.DataSource;
#endif

#if PCL
using Infragistics.Core.Controls.DataSource;
#endif

#if DATA_PRESENTER
namespace Reference.DataSources.OData
#else
namespace Infragistics.Controls.DataSource
#endif
{
    public class ODataVirtualDataSourceDataProvider
        : IDataSourceVirtualDataProvider
    {

        private ODataVirtualDataSourceDataProviderWorker _worker;
        private LinkedList<int> _requests = new LinkedList<int>();
        private DataSourcePageLoadedCallback _callback;

        public ODataVirtualDataSourceDataProvider()
        {
            _sortDescriptions = new SortDescriptionCollection();
            _groupDescriptions = new SortDescriptionCollection();
            _sortDescriptions.CollectionChanged += SortDescriptions_CollectionChanged;
            _groupDescriptions.CollectionChanged += GroupDescriptions_CollectionChanged;
            _filterExpressions = new FilterExpressionCollection();
            _filterExpressions.CollectionChanged += FilterExpressions_CollectionChanged;
            _summaryDescriptions = new SummaryDescriptionCollection();
            _summaryDescriptions.CollectionChanged += SummaryDescriptions_CollectionChanged;
        }

        private void FilterExpressions_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            QueueAutoRefresh();
        }

        private void SortDescriptions_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            QueueAutoRefresh();
        }

        private void GroupDescriptions_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            QueueAutoRefresh();
        }

        private void SummaryDescriptions_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            QueueAutoRefresh();
        }

        public void AddPageRequest(int pageIndex, DataSourcePageRequestPriority priority)
        {
            if (DeferAutoRefresh)
            {
                return;
            }

            if (_worker != null && _worker.IsShutdown)
            {
                _worker = null;
                _callback = null;
            }

            if (_worker == null)
            {
                CreateWorker();
            }

            if (priority == DataSourcePageRequestPriority.High)
            {
                _requests.AddFirst(pageIndex);
            }
            else
            {
                _requests.AddLast(pageIndex);
            }
            if (!_worker.AddPageRequest(pageIndex, priority))
            {
                _worker = null;
                _callback = null;
                AddPageRequest(pageIndex, priority);
            }
        }

        private void CreateWorker()
        {
            if (!Valid())
            {
                return;
            }

            _callback = RaisePageLoaded;

            var settings = GetWorkerSettings();

            _worker = new ODataVirtualDataSourceDataProviderWorker(
                settings);
        }

        private bool Valid()
        {
            return EntitySet != null &&
                BaseUri != null;
        }

        private ODataVirtualDataSourceDataProviderWorkerSettings GetWorkerSettings()
        {
            return new ODataVirtualDataSourceDataProviderWorkerSettings()
            {
                BaseUri = _baseUri,
                EntitySet = _entitySet,
                PageSizeRequested = _pageSizeRequested,
                TimeoutMilliseconds = _timeoutMilliseconds,
                PageLoaded = _callback,
                ExecutionContext = _executionContext,
                SortDescriptions = _sortDescriptions,
                FilterExpressions = _filterExpressions,
                PropertiesRequested = _propertiesRequested,
                GroupDescriptions = _groupDescriptions,
                IsAggregationSupportedByServer = _isAggregationSupportedByServer,
                SummaryDescriptions = _summaryDescriptions,
                SummaryScope = _summaryScope
            };
        }

        public void RemovePageRequest(int pageIndex)
        {
            _requests.Remove(pageIndex);
            if (_worker == null)
            {
                return;
            }
            _worker.RemovePageRequest(pageIndex);
        }

        public void RemoveAllPageRequests()
        {
            _requests.Clear();
            if (_worker == null)
            {
                return;
            }
            _worker.RemoveAllPageRequests();
        }

        public void Close()
        {
            if (_worker != null)
            {
                _worker.Shutdown();
                _worker = null;
                _callback = null;
            }
        }

        private DataSourcePageLoadedCallback _pageLoaded;
        public DataSourcePageLoadedCallback PageLoaded
        {
            get
            {
                return _pageLoaded;
            }
            set
            {
                _pageLoaded = value;
                QueueAutoRefresh();
            }
        }

        private void RaisePageLoaded(IDataSourcePage page, int fullCount, int actualPageSize)
        {
            if (_pageLoaded != null)
            {
                _currentFullCount = fullCount;
                if (_currentSchema == null)
                {
                    IDataSourceSchema currentSchema = null;
                    if (page != null)
                    {
                        currentSchema = page.Schema();
                    }

                    _currentSchema = currentSchema;
                    if (SchemaChanged != null)
                    {
                        SchemaChanged(this, new DataSourceDataProviderSchemaChangedEventArgs(_currentSchema, _currentFullCount));
                    }
                }

                if (page.PageIndex() != ODataVirtualDataSourceDataProviderWorker.SchemaRequestIndex)
                {
                    _pageLoaded(page, fullCount, actualPageSize);
                }
            }
        }

        private void KillWorker()
        {
            if (_worker != null)
            {
                _worker.Shutdown();
                _worker = null;
                _callback = null;
            }
        }

        private int _pageSizeRequested = 50;
        public int PageSizeRequested
        {
            get
            {
                return _pageSizeRequested;
            }
            set
            {
                _pageSizeRequested = value;
                QueueAutoRefresh();
            }
        }

        private string _baseUri = null;
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
                    QueueAutoRefresh();
                    if (Valid() && DeferAutoRefresh)
                    {
                        QueueSchemaFetch();
                    }
                }
            }
        }

        private string _entitySet = null;
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
                if (oldValue != _entitySet)
                {
                    QueueAutoRefresh();
                    if (Valid() && DeferAutoRefresh)
                    {
                        QueueSchemaFetch();
                    }
                }
            }
        }

        private int _timeoutMilliseconds = 10000;
        public int TimeoutMilliseconds
        {
            get
            {
                return _timeoutMilliseconds;
            }
            set
            {
                _timeoutMilliseconds = value;
                QueueAutoRefresh();
            }
        }

        private bool _isAggregationSupportedByServer = false;
        /// <summary>
        /// Gets or sets whether the server supports aggregation query options required for grouping.
        /// </summary>
        public bool IsAggregationSupportedByServer
        {
            get
            {
                return _isAggregationSupportedByServer;
            }
            set
            {
                var oldValue = _isAggregationSupportedByServer;
                _isAggregationSupportedByServer = value;
                QueueAutoRefresh();
            }
        }

        public object GetItemValue(object item, string valueName)
        {
            var dic = (IDictionary<string, object>)item;
            if (dic.ContainsKey(valueName))
            {
                return dic[valueName];
            }
            else
            {
                return null; 
            }
        }

        public event DataSourceDataProviderSchemaChangedHandler SchemaChanged;
        private int _currentFullCount = 0;
        private IDataSourceSchema _currentSchema;

        public int ActualCount
        {
            get
            {
                return _currentFullCount;
            }
        }

        public IDataSourceSchema ActualSchema
        {
            get
            {
                return _currentSchema;
            }
        }

        private IDataSourceExecutionContext _executionContext;
        public IDataSourceExecutionContext ExecutionContext
        {
            get
            {
                return _executionContext;
            }
            set
            {
                _executionContext = value;
                QueueAutoRefresh();
            }
        }

        private IDataSourceDataProviderUpdateNotifier _updateNotifier;
        public IDataSourceDataProviderUpdateNotifier UpdateNotifier
        {
            get
            {
                return _updateNotifier;
            }
            set
            {
                _updateNotifier = value;
            }
        }

        private bool _deferAutoRefresh = false;
        public bool DeferAutoRefresh
        {
            get
            {
                return _deferAutoRefresh;
            }

            set
            {
                _deferAutoRefresh = value;
                if (!_deferAutoRefresh)
                {
                    QueueAutoRefresh();
                }
                if (_deferAutoRefresh && Valid() && _currentSchema == null)
                {
                    QueueSchemaFetch();
                }
            }
        }

        public bool IsSortingSupported
        {
            get
            {
                return true;
            }
        }

        public bool IsFilteringSupported
        {
            get
            {
                return true;
            }
        }

        private SortDescriptionCollection _sortDescriptions;
        public SortDescriptionCollection SortDescriptions
        {
            get
            {
                return _sortDescriptions;
            }
        }

        private string[] _propertiesRequested;
        public string[] PropertiesRequested
        {
            get
            {
                return _propertiesRequested;
            }
            set
            {
                _propertiesRequested = value;
                QueueAutoRefresh();
            }
        }

        private FilterExpressionCollection _filterExpressions;
        public FilterExpressionCollection FilterExpressions
        {
            get
            {
                return _filterExpressions;
            }
        }

        public bool NotifyUsingSourceIndexes
        {
            get
            {
                return true;
            }
        }

        public bool IsItemIndexLookupSupported
        {
            get
            {
                return false;
            }
        }

        public bool IsKeyIndexLookupSupported
        {
            get
            {
                return false;
            }
        }

        public bool IsGroupingSupported
        {
            get
            {
                return false;
            }
        }

        private SortDescriptionCollection _groupDescriptions;
        public SortDescriptionCollection GroupDescriptions
        {
            get
            {
                return _groupDescriptions;
            }
        }

        private SummaryDescriptionCollection _summaryDescriptions;
        public SummaryDescriptionCollection SummaryDescriptions
        {
            get
            {
                return _summaryDescriptions;
            }
        }

        private DataSourceSummaryScope _summaryScope;
        public DataSourceSummaryScope SummaryScope
        {
            get
            {
                return _summaryScope;
            }
            set
            {
                _summaryScope = value;
            }
        }

        public void NotifySetItem(int index, object oldItem, object newItem)
        {
            if (UpdateNotifier != null)
            {
                UpdateNotifier.NotifySetItem(index, oldItem, newItem);
            }
        }

        public void NotifyClearItems()
        {
            if (UpdateNotifier != null)
            {
                UpdateNotifier.NotifyClearItems();
            }
        }

        public void NotifyInsertItem(int index, object newItem)
        {
            if (UpdateNotifier != null)
            {
                UpdateNotifier.NotifyInsertItem(index, newItem);
            }
        }

        public void NotifyRemoveItem(int index, object oldItem)
        {
            if (UpdateNotifier != null)
            {
                UpdateNotifier.NotifyRemoveItem(index, oldItem);
            }
        }

        internal bool _schemaFetchQueued = false;
        public void QueueSchemaFetch()
        {
            if (_schemaFetchQueued)
            {
                return;
            }

            if (ExecutionContext != null)
            {
                _schemaFetchQueued = true;
                ExecutionContext.EnqueueAction(DoSchemaFetchInternal);
            }
        }

        internal void DoSchemaFetchInternal()
        {
            if (!_schemaFetchQueued)
            {
                return;
            }
            _schemaFetchQueued = false;
            SchemaFetchInternal();
        }

        internal void SchemaFetchInternal()
        {
            SchemaFetchInternalOverride();
        }

        protected virtual void SchemaFetchInternalOverride()
        {
            if (!DeferAutoRefresh)
            {
                return;
            }
            RemoveAllPageRequests();
            KillWorker();
            CreateWorker();
        
            AddSchemaRequest();
        }

        private void AddSchemaRequest()
        {
            _worker.AddPageRequest(
                ODataVirtualDataSourceDataProviderWorker.SchemaRequestIndex,
                DataSourcePageRequestPriority.High
                );
        }

        internal bool _autoRefreshQueued = false;
        public void QueueAutoRefresh()
        {
            if (DeferAutoRefresh)
            {
                return;
            }
            if (_autoRefreshQueued)
            {
                return;
            }

            if (ExecutionContext != null)
            {
                _autoRefreshQueued = true;
                ExecutionContext.EnqueueAction(DoRefreshInternal);
            }
        }

        internal void DoRefreshInternal()
        {
            if (DeferAutoRefresh)
            {
                _autoRefreshQueued = false;
                return;
            }
            if (!_autoRefreshQueued)
            {
                return;
            }
            _autoRefreshQueued = false;
            RefreshInternal();
        }

        internal void RefreshInternal()
        {
            RefreshInternalOverride();
        }

        protected virtual void RefreshInternalOverride()
        {
            RemoveAllPageRequests();
            KillWorker();
            CreateWorker();
            //TODO: should we have this here? prob need to readd request for current page instead.
            _worker.AddPageRequest(0, DataSourcePageRequestPriority.Normal);
        }

        public void FlushAutoRefresh()
        {
            DoRefreshInternal();
        }

        public void Refresh()
        {
            RefreshInternal();
        }

        public int IndexOfItem(object item)
        {
            return -1;
        }

        public int IndexOfKey(object[] key)
        {
            return -1;
        }
        
        public DataSourceSchemaPropertyType ResolveSchemaPropertyType(string propertyPath)
		{
			var propertiesInPath = propertyPath.Split('.');
			if (propertiesInPath.Length == 1 && this.ActualSchema != null)
			{
				for (int i = 0; i < this.ActualSchema.PropertyNames.Length; i++)
				{
					if (this.ActualSchema.PropertyNames[i] == propertiesInPath[0])
					{
						return this.ActualSchema.PropertyTypes[i];
					}
				}
			}

			return DataSourceSchemaPropertyType.ObjectValue;
		}

        public IDataSourceDataProvider Clone()
        {
            var provider = new ODataVirtualDataSourceDataProvider();
            provider.ExecutionContext = ExecutionContext;
            provider.EntitySet = EntitySet;
            provider.PageSizeRequested = PageSizeRequested;
            provider.PropertiesRequested = PropertiesRequested;
            provider.SummaryScope = SummaryScope;
            provider.TimeoutMilliseconds = TimeoutMilliseconds;

            for (var i = 0; i < FilterExpressions.Count; i++)
                provider.FilterExpressions.Add(FilterExpressions[i]);

            for (var i = 0; i < SortDescriptions.Count; i++)
                provider.SortDescriptions.Add(SortDescriptions[i]);

            for (var i = 0; i < GroupDescriptions.Count; i++)
                provider.GroupDescriptions.Add(GroupDescriptions[i]);

            for (var i = 0; i < SummaryDescriptions.Count; i++)
                provider.SummaryDescriptions.Add(SummaryDescriptions[i]);

            return provider;
        }
    }

   
}
