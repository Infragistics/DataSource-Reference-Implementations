using Infragistics.Controls;
using Simple.OData.Client;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using GridODataTest;
using System.Xml;
using Infragistics.Controls.DataSource;
using System.Text;

#if DATA_PRESENTER
namespace Reference.DataSources.OData
#else
namespace Infragistics.Controls.DataSource
#endif
{
    internal class ODataVirtualDataSourceDataProviderWorkerSettings
        : AsyncVirtualDataSourceDataProviderWorkerSettings
    {
        public string BaseUri { get; set; }
        public string EntitySet { get; set; }

        public DataSourceSortDescriptionCollection SortDescriptions { get; set; }

        public DataSourceFilterExpressionCollection FilterExpressions { get; set; }
    }

    


    internal class ODataVirtualDataSourceProviderTaskDataHolder
        : AsyncVirtualDataSourceProviderTaskDataHolder
    {
        public ODataFeedAnnotations CompletedAnnotation { get; internal set; }
        public ODataFeedAnnotations[] CurrentAnnotations { get; set; }
    }

    
    internal class ODataVirtualDataSourceDataProviderWorker
        : AsyncVirtualDataSourceProviderWorker
    {
        private ODataClient _client;
        private string _baseUri;
        private string _entitySet;
        private DataSourceSortDescriptionCollection _sortDescriptions;
        private DataSourceFilterExpressionCollection _filterExpressions;

        protected DataSourceSortDescriptionCollection SortDescriptions
        {
            get
            {
                return _sortDescriptions;
            }
        }

        protected DataSourceFilterExpressionCollection FilterExpressions
        {
            get
            {
                return _filterExpressions;
            }
        }

        private List<ODataFeedAnnotations> _annotations =
            new List<ODataFeedAnnotations>();

        protected override void Initialize()
        {
            base.Initialize();

            lock (SyncLock)
            {
                _client = new ODataClient(_baseUri);
            }
        }

        protected override AsyncVirtualDataSourceProviderTaskDataHolder GetTaskDataHolder()
        {
            ODataVirtualDataSourceProviderTaskDataHolder holder = new ODataVirtualDataSourceProviderTaskDataHolder();
            return holder;
        }

        protected override void GetCompletedTaskData(AsyncVirtualDataSourceProviderTaskDataHolder holder, int completed)
        {
            base.GetCompletedTaskData(holder, completed);

            var h = (ODataVirtualDataSourceProviderTaskDataHolder)holder;
            h.CompletedAnnotation = h.CurrentAnnotations[completed];
        }

        protected override void RemoveCompletedTaskData(AsyncVirtualDataSourceProviderTaskDataHolder holder, int completed)
        {
            base.RemoveCompletedTaskData(holder, completed);

            var h = (ODataVirtualDataSourceProviderTaskDataHolder)holder;
            _annotations.Remove(h.CompletedAnnotation);
        }

        protected override void GetTasksData(AsyncVirtualDataSourceProviderTaskDataHolder holder)
        {
            base.GetTasksData(holder);

            var h = (ODataVirtualDataSourceProviderTaskDataHolder)holder;
            h.CurrentAnnotations = _annotations.ToArray();
        }

        public ODataVirtualDataSourceDataProviderWorker(ODataVirtualDataSourceDataProviderWorkerSettings settings)
            :base(settings)
        {
            _baseUri = settings.BaseUri;
            _entitySet = settings.EntitySet;
            _sortDescriptions = settings.SortDescriptions;
            _filterExpressions = settings.FilterExpressions;
            Task.Factory.StartNew(() => DoWork(), TaskCreationOptions.LongRunning);
        }        
      
        protected override void ProcessCompletedTask(AsyncDataSourcePageTaskHolder completedTask, int currentDelay, int pageIndex, AsyncVirtualDataSourceProviderTaskDataHolder taskDataHolder)
        {
            ODataVirtualDataSourceProviderTaskDataHolder h = (ODataVirtualDataSourceProviderTaskDataHolder)taskDataHolder;
            IDataSourceSchema schema = null;
            IEnumerable<IDictionary<string, object>> result;
            Task<IEnumerable<IDictionary<string, object>>> task = (Task<IEnumerable<IDictionary<string, object>>>)completedTask.Task;
            try
            {
                result = task.Result;
            }
            catch (AggregateException e)
            {
                bool allCancels = true;
                foreach (var ex in e.Flatten().InnerExceptions)
                {
                    if (!(ex is TaskCanceledException))
                    {
                        allCancels = false;
                    }
                }
                if (!allCancels)
                {
                    RetryIndex(pageIndex, currentDelay);
                    return;
                }
                else
                {
                    RetryIndex(pageIndex, currentDelay);
                    return;
                }
            }
            catch (TaskCanceledException e)
            {
                RetryIndex(pageIndex, currentDelay);
                //TODO: other exceptions? Is there a way to skip this state for canceled stuff?
                return;
            }

            IDataSourceExecutionContext executionContext;
            DataSourcePageLoadedCallback pageLoaded;
            lock (SyncLock)
            {
                var completedAnnotation = h.CompletedAnnotation;
                if (completedAnnotation.Count.HasValue)
                {
                    ActualCount = (int)completedAnnotation.Count.Value;
                }

                schema = ActualSchema;
                if (schema == null)
                {
                    schema = ResolveSchema(result.FirstOrDefault());
                }
                ActualSchema = schema;
                executionContext = ExecutionContext;
                pageLoaded = PageLoaded;
            }

            ODataDataSourcePage page = new ODataDataSourcePage(result, schema, pageIndex);
            lock (SyncLock)
            {
                if (!IsLastPage(pageIndex) && page.Count() > 0 && !PopulatedActualPageSize)
                {
                    PopulatedActualPageSize = true;
                    ActualPageSize = page.Count();
                }
            }

            if (PageLoaded != null)
            {
                if (ExecutionContext != null)
                {
                    if (executionContext == null || pageLoaded == null)
                    {
                        Shutdown();
                        return;
                    }

                    executionContext.Execute(() =>
                        {
                            pageLoaded(page, ActualCount, ActualPageSize);
                        });              
                }
                else
                {
                    if (pageLoaded == null)
                    {
                        Shutdown();
                        return;
                    }

                    pageLoaded(page, ActualCount, ActualPageSize);
                }
            }
        }

        

        private IDataSourceSchema ResolveSchema(IDictionary<string, object> item)
        {
            if (item == null)
            {
                return null;
            }

            var t = this._client.GetMetadataDocumentAsync();
            t.Wait();
            var metadataDocument = t.Result;
            SchemaProvider sp = new SchemaProvider(metadataDocument);
			return sp.GetODataDataSourceSchema(this._entitySet);
        }

        private string _filterString = null;

        protected override void MakeTaskForRequest(AsyncDataSourcePageRequest request, int retryDelay)
        {
            int actualPageSize = 0;
            DataSourceSortDescriptionCollection sortDescriptions = null;
            lock (SyncLock)
            {
                actualPageSize = ActualPageSize;
                sortDescriptions = SortDescriptions;
            }

            ODataFeedAnnotations annotations = new ODataFeedAnnotations();

            var client = _client.For(_entitySet);

            lock (SyncLock)
            {
                if (FilterExpressions != null &&
                    FilterExpressions.Count > 0 &&
                    _filterString == null)
                {
                    StringBuilder sb = new StringBuilder();
                    bool first = true;
                    foreach (var expr in FilterExpressions)
                    {
                        if (first)
                        {
                            first = false;
                        }
                        else
                        {
                            sb.Append(" AND ");
                        }

                        ODataDataSourceFilterExpressionVisitor visitor = new ODataDataSourceFilterExpressionVisitor();

                        visitor.Visit(expr);

                        var txt = visitor.ToString();
                        if (FilterExpressions.Count > 1)
                        {
                            txt = "(" + txt + ")";
                        }
                        sb.Append(txt);
                    }
                    _filterString = sb.ToString();
                }

                if (_filterString != null)
                {
                    client = client.Filter(_filterString);
                }

                if (SortDescriptions != null)
                {
                    foreach (var sort in SortDescriptions)
                    {
                        if (sort.IsDescending)
                        {
                            client = client.OrderByDescending(sort.PropertyName);
                        }
                        else
                        {
                            client = client.OrderBy(sort.PropertyName);
                        }
                    }
                }
            }

            Task<IEnumerable<IDictionary<string, object>>> task = client
                .Skip(request.Index * actualPageSize)
                .Top(actualPageSize)
                .FindEntriesAsync(annotations);

            request.TaskHolder = new AsyncDataSourcePageTaskHolder();
            request.TaskHolder.Task = task;

            lock (SyncLock)
            {
                Tasks.Add(request);
                _annotations.Add(annotations);
            }
        }
    }

}