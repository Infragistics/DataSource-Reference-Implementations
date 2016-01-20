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
            _sortDescriptions = new DataSourceSortDescriptionCollection();
            _sortDescriptions.CollectionChanged += SortDescriptions_CollectionChanged;
            _filterExpressions = new DataSourceFilterExpressionCollection();
            _filterExpressions.CollectionChanged += FilterExpressions_CollectionChanged;
        }

        private void FilterExpressions_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            QueueAutoRefresh();
        }

        private void SortDescriptions_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            QueueAutoRefresh();
        }

        public void AddPageRequest(int pageIndex, DataSourcePageRequestPriority priority)
        {
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
            _worker.AddPageRequest(0, DataSourcePageRequestPriority.Normal);
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
                DesiredPageSize = _desiredPageSize,
                TimeoutMilliseconds = _timeoutMilliseconds,
                PageLoaded = _callback,
                ExecutionContext = _executionContext,
                SortDescriptions = _sortDescriptions,
                FilterExpressions = _filterExpressions
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
                _pageLoaded(page, fullCount, actualPageSize);
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

        private int _desiredPageSize = 200;
        public int DesiredPageSize
        {
            get
            {
                return _desiredPageSize;
            }
            set
            {
                _desiredPageSize = value;
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
                _baseUri = value;
                QueueAutoRefresh();
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
                _entitySet = value;
                QueueAutoRefresh();
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

        public int GetCount()
        {
            return _currentFullCount;
        }

        public IDataSourceSchema GetSchema()
        {
            return _currentSchema;
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

        private bool _shouldDeferAutoRefresh = false;
        public bool ShouldDeferAutoRefresh
        {
            get
            {
                return _shouldDeferAutoRefresh;
            }

            set
            {
                _shouldDeferAutoRefresh = value;
                if (!_shouldDeferAutoRefresh)
                {
                    QueueAutoRefresh();
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

        private DataSourceSortDescriptionCollection _sortDescriptions;
        public DataSourceSortDescriptionCollection SortDescriptions
        {
            get
            {
                return _sortDescriptions;
            }
        }

        private DataSourceFilterExpressionCollection _filterExpressions;
        public DataSourceFilterExpressionCollection FilterExpressions
        {
            get
            {
                return _filterExpressions;
            }
        }

        public bool AreSortingAndFilteringExternal
        {
            get
            {
                return true;
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

        internal bool _autoRefreshQueued = false;
        public void QueueAutoRefresh()
        {
            if (ShouldDeferAutoRefresh)
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
            if (ShouldDeferAutoRefresh)
            {
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
        }

        public void FlushAutoRefresh()
        {
            DoRefreshInternal();
        }

        public void Refresh()
        {
            RefreshInternal();
        }
    }

   
}
