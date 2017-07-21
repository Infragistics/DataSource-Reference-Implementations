using Infragistics.Controls;
using Simple.OData.Client;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
#if !PCL
using GridODataTest;
#endif

using System.Xml;
using Infragistics.Controls.DataSource;
using System.Text;
#if PCL
using Infragistics.Core.Controls.DataSource;
#endif

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

        public SortDescriptionCollection SortDescriptions { get; set; }

        public FilterExpressionCollection FilterExpressions { get; set; }

        public string[] PropertiesRequested { get; set; }
        public SortDescriptionCollection GroupDescriptions { get; internal set; }
        public bool IsAggregationSupportedByServer { get; internal set; }
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
        private SortDescriptionCollection _sortDescriptions;
        private FilterExpressionCollection _filterExpressions;
        private string[] _desiredPropeties;

        protected SortDescriptionCollection SortDescriptions
        {
            get
            {
                return _sortDescriptions;
            }
        }

        protected FilterExpressionCollection FilterExpressions
        {
            get
            {
                return _filterExpressions;
            }
        }

        protected string[] DesiredProperties
        {
            get
            {
                return _desiredPropeties;
            }
        }

        private List<ODataFeedAnnotations> _annotations =
            new List<ODataFeedAnnotations>();

        protected override void Initialize()
        {
            base.Initialize();

            lock (SyncLock)
            {
                _client = new ODataClient(new ODataClientSettings(_baseUri)
                {
                    IgnoreUnmappedProperties = true
                });
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
            _desiredPropeties = settings.PropertiesRequested;
            _groupDescriptions = settings.GroupDescriptions;
            if (_groupDescriptions != null && _groupDescriptions.Count > 0)
            {
                _sortDescriptions = new SortDescriptionCollection();
                foreach (var sd in settings.SortDescriptions)
                {
                    _sortDescriptions.Add(sd);
                }

                for (var i = 0; i < _groupDescriptions.Count; i++)
                {
                    _sortDescriptions.Insert(i, _groupDescriptions[i]);
                }
            }
            _isAggregationSupportedByServer = settings.IsAggregationSupportedByServer;
            Task.Factory.StartNew(() => DoWork(), TaskCreationOptions.LongRunning);
        }        
      
        protected override void ProcessCompletedTask(AsyncDataSourcePageTaskHolder completedTask, int currentDelay, int pageIndex, AsyncVirtualDataSourceProviderTaskDataHolder taskDataHolder)
        {
            ODataVirtualDataSourceProviderTaskDataHolder h = (ODataVirtualDataSourceProviderTaskDataHolder)taskDataHolder;
            IDataSourceSchema schema = null;
            IEnumerable<IDictionary<string, object>> result = null;
            ISectionInformation[] groupInformation = null;
            int schemaFetchCount = -1;
            bool isAggregationSupportedByServer = false;
            bool isGrouping = false;

            try
            {
                if (pageIndex == SchemaRequestIndex)
                {
                    Task<int> task = (Task<int>)completedTask.Task;

                    schemaFetchCount = task.Result;
                }
                else
                {
                    Task<IEnumerable<IDictionary<string, object>>> task = (Task<IEnumerable<IDictionary<string, object>>>)completedTask.Task;

                    result = task.Result;
                }                
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
                if (schemaFetchCount >= 0)
                {
                    ActualCount = schemaFetchCount;
                }
                else
                {
                    var completedAnnotation = h.CompletedAnnotation;
                    if (completedAnnotation.Count.HasValue)
                    {
                        ActualCount = (int)completedAnnotation.Count.Value;
                    }
                }

                groupInformation = _groupInformation;
                isAggregationSupportedByServer = _isAggregationSupportedByServer;
                schema = ActualSchema;
                isGrouping = _groupDescriptions != null && _groupDescriptions.Count > 0;
            }

            if (schema == null)
            {
                schema = ResolveSchema();
            }
            if (isGrouping && isAggregationSupportedByServer &&
                groupInformation == null)
            {
                groupInformation = ResolveGroupInformation();
            }

            lock (SyncLock)
            {                
                ActualSchema = schema;
                _groupInformation = groupInformation;
                executionContext = ExecutionContext;
                pageLoaded = PageLoaded;
            }

            ODataDataSourcePage page = null;

            if (result != null)
            {
                page = new ODataDataSourcePage(result, schema, groupInformation, pageIndex);
                lock (SyncLock)
                {
                    if (!IsLastPage(pageIndex) && page.Count() > 0 && !PopulatedActualPageSize)
                    {
                        PopulatedActualPageSize = true;
                        ActualPageSize = page.Count();
                    }
                }
            }
            else
            {
                page = new ODataDataSourcePage(null, schema, groupInformation, pageIndex);
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

        

        private IDataSourceSchema ResolveSchema()
        {
            var t = this._client.GetMetadataDocumentAsync();
            t.Wait();
            var metadataDocument = t.Result;
            ODataSchemaProvider sp = new ODataSchemaProvider(metadataDocument);
			return sp.GetODataDataSourceSchema(this._entitySet);
        }
        private ISectionInformation[] ResolveGroupInformation()
        {
            //TODO: both this and the schema fetch should 
            //be async, and we may just hold up page delivery until present.
            //Rather than doing a sync wait.
            string orderBy = null;
            string groupBy = null;
            string filter = null;
            lock (SyncLock)
            {
                if (_groupDescriptions == null ||
                    _groupDescriptions.Count == 0)
                {
                    return null;
                }
                filter = _filterString;
                UpdateFilterString();

                StringBuilder sb = new StringBuilder();
                if (SortDescriptions != null)
                {
                    bool first = true;
                    foreach (var sort in SortDescriptions)
                    {
                        if (first)
                        {
                            first = false;
                        }
                        else
                        {
                            sb.Append(", ");
                        }
                        if (sort.Direction ==
#if PCL
							ListSortDirection.Descending
#else
                            System.ComponentModel.ListSortDirection.Descending
#endif
                            )
                        {
                            sb.Append(sort.PropertyName + " desc");
                        }
                        else
                        {
                            sb.Append(sort.PropertyName + " asc");
                        }
                    }
                }

                orderBy = sb.ToString();

                StringBuilder gsb = new StringBuilder();
                if (_groupDescriptions != null)
                {
                    bool first = true;
                    foreach (var group in _groupDescriptions)
                    {
                        if (first)
                        {
                            first = false;
                        }
                        else
                        {
                            sb.Append(", ");
                        }
                        gsb.Append(group.PropertyName);
                    }
                }

                groupBy = gsb.ToString();

            }

            var commandText = _entitySet + "?$orderby=" + orderBy + "&$apply=";
            if (!String.IsNullOrEmpty(filter))
            {
                commandText += "filter(" + filter + ")/";
            }
            commandText += "groupby((" + groupBy + "), aggregate($count as $__count))";

            lock (SyncLock)
            {
                try
                {
                    var annotations = new ODataFeedAnnotations();
                    var t = _client.FindEntriesAsync(commandText, annotations);
                    t.Wait();
                    var res = t.Result;
                    List<ISectionInformation> groupInformation = new List<ISectionInformation>();
                    List<string> groupNames = new List<string>();

                    foreach (var group in _groupDescriptions)
                    {
                        groupNames.Add(group.PropertyName);
                    }
                    var groupNamesArray = groupNames.ToArray();
                    int currentIndex = 0;
                    foreach (var group in res)
                    {
                        AddGroup(groupInformation, groupNames, 
                            groupNamesArray, currentIndex, 
                            group);
                    }
                    while (annotations.NextPageLink != null)
                    {
                        var link = _entitySet + "?" + annotations.NextPageLink.Query;
                        var t2 = _client.FindEntriesAsync(link, annotations);
                        t2.Wait();
                        var res2 = t2.Result;
                        foreach (var group in res2)
                        {
                            AddGroup(groupInformation, groupNames,
                                groupNamesArray, currentIndex,
                                group);
                        }
                    }

                    return groupInformation.ToArray();
                }
                catch (Exception e)
                {
                    return null;
                }
            }
        }

        private static void AddGroup(List<ISectionInformation> groupInformation, List<string> groupNames, string[] groupNamesArray, int currentIndex, IDictionary<string, object> group)
        {
            List<object> groupValues = new List<object>();
            foreach (var name in groupNames)
            {
                if (group.ContainsKey(name))
                {
                    groupValues.Add(group[name]);
                }
            }
            var groupCount = 0;
            if (group.ContainsKey("$__count"))
            {
                groupCount = Convert.ToInt32(group["$__count"]);
            }
            DefaultSectionInformation groupInfo = new DefaultSectionInformation(
                currentIndex,
                currentIndex + (groupCount - 1),
                groupNamesArray,
                groupValues.ToArray());
            groupInformation.Add(groupInfo);
        }

        private string _filterString = null;
        private string _selectedString = null;
        public const int SchemaRequestIndex = -1;
        private SortDescriptionCollection _groupDescriptions;
        private bool _isAggregationSupportedByServer = false;
        private ISectionInformation[] _groupInformation = null;

        protected override void MakeTaskForRequest(AsyncDataSourcePageRequest request, int retryDelay)
        {
            int actualPageSize = 0;
            SortDescriptionCollection sortDescriptions = null;
            lock (SyncLock)
            {
                actualPageSize = ActualPageSize;
                sortDescriptions = SortDescriptions;
            }

            ODataFeedAnnotations annotations = new ODataFeedAnnotations();

            var client = _client.For(_entitySet);

            lock (SyncLock)
            {
                UpdateFilterString();

                if (_filterString != null)
                {
                    client = client.Filter(_filterString);
                }

                if (SortDescriptions != null)
                {
                    foreach (var sort in SortDescriptions)
                    {
                        if (sort.Direction ==
#if PCL
							ListSortDirection.Descending
#else
                            System.ComponentModel.ListSortDirection.Descending
#endif
                            )
                        {
                            client = client.OrderByDescending(sort.PropertyName);
                        }
                        else
                        {
                            client = client.OrderBy(sort.PropertyName);
                        }
                    }
                }

                if (DesiredProperties != null && DesiredProperties.Length > 0)
                {
                    client = client.Select(DesiredProperties);
                }
            }

            Task task;
            if (request.Index == SchemaRequestIndex)
            {
                task = client
                    .Count()
                    .FindScalarAsync<int>();
            }
            else
            {
                task = client
                    .Skip(request.Index * actualPageSize)
                    .Top(actualPageSize)
                    .FindEntriesAsync(annotations);
            }

            request.TaskHolder = new AsyncDataSourcePageTaskHolder();
            request.TaskHolder.Task = task;

            lock (SyncLock)
            {
                Tasks.Add(request);
                _annotations.Add(annotations);
            }
        }

        private void UpdateFilterString()
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
        }
    }

}