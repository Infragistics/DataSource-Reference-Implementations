using Infragistics.Controls;
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
using SQLite;
using System.Collections;
using System.Reflection;
#endif

#if DATA_PRESENTER
namespace Reference.DataSources.OData
#else
namespace Infragistics.Controls.DataSource
#endif
{
    internal class SQLiteVirtualDataSourceDataProviderWorkerSettings
        : AsyncVirtualDataSourceDataProviderWorkerSettings
    {
        public string TableExpression { get; set; }
        public Type ProjectionType { get; set; }
        public string SelectExpressionOverride { get; set; }
        public SQLiteAsyncConnection Connection { get; set; }

        public SortDescriptionCollection SortDescriptions { get; set; }

        public FilterExpressionCollection FilterExpressions { get; set; }

        public string[] PropertiesRequested { get; set; }
    }

    


    internal class SQLiteVirtualDataSourceProviderTaskDataHolder
        : AsyncVirtualDataSourceProviderTaskDataHolder
    {
        public int FullCount { get; set; }
    }

    
    internal class SQLiteVirtualDataSourceDataProviderWorker
        : AsyncVirtualDataSourceProviderWorker
    {
        private string _tableExpression;
        private string _selectExpressionOverride;
        private Type _projectionType;
        private SQLiteAsyncConnection _connection;
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

        protected override void Initialize()
        {
            base.Initialize();

        }

        protected override AsyncVirtualDataSourceProviderTaskDataHolder GetTaskDataHolder()
        {
            SQLiteVirtualDataSourceProviderTaskDataHolder holder = new SQLiteVirtualDataSourceProviderTaskDataHolder();
            return holder;
        }

        protected override void GetCompletedTaskData(AsyncVirtualDataSourceProviderTaskDataHolder holder, int completed)
        {
            base.GetCompletedTaskData(holder, completed);

            var h = (SQLiteVirtualDataSourceProviderTaskDataHolder)holder;
        }

        protected override void RemoveCompletedTaskData(AsyncVirtualDataSourceProviderTaskDataHolder holder, int completed)
        {
            base.RemoveCompletedTaskData(holder, completed);

            var h = (SQLiteVirtualDataSourceProviderTaskDataHolder)holder;
        }

        protected override void GetTasksData(AsyncVirtualDataSourceProviderTaskDataHolder holder)
        {
            base.GetTasksData(holder);
        }

        public SQLiteVirtualDataSourceDataProviderWorker(SQLiteVirtualDataSourceDataProviderWorkerSettings settings)
            :base(settings)
        {
            _tableExpression = settings.TableExpression;
            _projectionType = settings.ProjectionType;
            _selectExpressionOverride = settings.SelectExpressionOverride;
            _connection = settings.Connection;
            _sortDescriptions = settings.SortDescriptions;
            _filterExpressions = settings.FilterExpressions;
            _desiredPropeties = settings.PropertiesRequested;
            Task.Factory.StartNew(() => DoWork(), TaskCreationOptions.LongRunning);
        }        
      
        protected override void ProcessCompletedTask(AsyncDataSourcePageTaskHolder completedTask, int currentDelay, int pageIndex, AsyncVirtualDataSourceProviderTaskDataHolder taskDataHolder)
        {
            SQLiteVirtualDataSourceProviderTaskDataHolder h = (SQLiteVirtualDataSourceProviderTaskDataHolder)taskDataHolder;
            IDataSourceSchema schema = null;
            SQLiteDataSourceQueryResult result = null;
            int schemaFetchCount = -1;

            try
            {
                if (pageIndex == SchemaRequestIndex)
                {
                    Task<int> task = (Task<int>)completedTask.Task;

                    schemaFetchCount = task.Result;
                }
                else
                {
                    Task<SQLiteDataSourceQueryResult> task = (Task<SQLiteDataSourceQueryResult>)completedTask.Task;

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
                    ActualCount = result.FullCount;
                }

                schema = ActualSchema;
            }

            if (schema == null)
            {
                schema = ResolveSchema();
            }

            lock (SyncLock)
            {                
                ActualSchema = schema;
                executionContext = ExecutionContext;
                pageLoaded = PageLoaded;
            }

            SQLiteDataSourcePage page = null;

            if (result != null)
            {
                page = new SQLiteDataSourcePage(result, schema, pageIndex);
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
                page = new SQLiteDataSourcePage(null, schema, pageIndex);
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
            var listT = typeof(List<>);
            var listType = listT.MakeGenericType(_projectionType);
            var newList = (IList)Activator.CreateInstance(listType);
            var sampleObject = Activator.CreateInstance(_projectionType);
            newList.Add(sampleObject);
            var lds = new LocalDataSource();
            lds.DeferAutoRefresh = true;
            lds.ItemsSource = newList;
            var schema = lds.ActualSchema;
            lds.ItemsSource = null;


            var actualPrimaryKey = schema.PrimaryKey;
            //TODO: get primary key;

            var actualSchema = new DefaultDataSourceSchema(
                schema.PropertyNames, schema.PropertyTypes, actualPrimaryKey, schema.PropertyDataIntents);

            return actualSchema;
        }

        private string _filterString = null;
        private string _selectedString = null;
        public const int SchemaRequestIndex = -1;
        private MethodInfo _specific;

        protected override void MakeTaskForRequest(AsyncDataSourcePageRequest request, int retryDelay)
        {
            int actualPageSize = 0;
            SortDescriptionCollection sortDescriptions = null;
            lock (SyncLock)
            {
                actualPageSize = ActualPageSize;
                sortDescriptions = SortDescriptions;
            }

            StringBuilder sb = new StringBuilder();

            sb.Append("SELECT ");

            lock (SyncLock)
            {
                
                if (_selectedString == null)
                {
                    StringBuilder selectBuilder = new StringBuilder();

                    if (_selectExpressionOverride != null)
                    {
                        selectBuilder.Append(_selectExpressionOverride);
                    }
                    else
                    {
                        if (DesiredProperties != null && DesiredProperties.Length > 0)
                        {
                            for (var i = 0; i < DesiredProperties.Length; i++)
                            {
                                if (i > 0)
                                {
                                    selectBuilder.Append(", ");
                                }
                                selectBuilder.Append(DesiredProperties[i]);
                            }
                        }
                        else
                        {
                            selectBuilder.Append("*");
                        }
                    }
                    _selectedString = selectBuilder.ToString();
                }

                sb.Append(_selectedString);

                sb.Append(" FROM ");
                sb.Append(_tableExpression);
                sb.Append(" ");

                if (FilterExpressions != null &&
                    FilterExpressions.Count > 0 &&
                    _filterString == null)
                {
                    StringBuilder filterBuilder = new StringBuilder();

                    bool first = true;
                    foreach (var expr in FilterExpressions)
                    {
                        if (first)
                        {
                            first = false;
                        }
                        else
                        {
                            filterBuilder.Append(" AND ");
                        }

                        SQLiteDataSourceFilterExpressionVisitor visitor = new SQLiteDataSourceFilterExpressionVisitor();

                        visitor.Visit(expr);

                        var txt = visitor.ToString();
                        if (FilterExpressions.Count > 1)
                        {
                            txt = "(" + txt + ")";
                        }
                        filterBuilder.Append(txt);
                    }
                    _filterString = filterBuilder.ToString();
                }

                if (_filterString != null)
                {
                    sb.Append(" WHERE ");
                    sb.Append(_filterString);
                }

                if (SortDescriptions != null && SortDescriptions.Count > 0)
                {
                    sb.Append(" ORDER BY ");
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
                            sb.Append(sort.PropertyName + " DESC");
                        }
                        else
                        {
                            sb.Append(sort.PropertyName + " ASC");
                        }
                    }
                }
            }

            Task task = null;
            if (request.Index == SchemaRequestIndex)
            {
                var query = "SELECT count(*) FROM " + _tableExpression;

                task = _connection.ExecuteScalarAsync<int>(query);
            }
            else
            {
                sb.Append(" LIMIT " + actualPageSize + " OFFSET " + request.Index * actualPageSize);

                var query = sb.ToString();

                if (_specific == null)
                {
                    var queryAsync = _connection.GetType().GetTypeInfo().GetDeclaredMethod("QueryAsync");
                    
                    var specific = queryAsync.MakeGenericMethod(_projectionType);
                    _specific = specific;
                }

                task = GetResult(query, _specific);
            }

            request.TaskHolder = new AsyncDataSourcePageTaskHolder();
            request.TaskHolder.Task = task;

            lock (SyncLock)
            {
                Tasks.Add(request);
            }
        }

        private async Task<SQLiteDataSourceQueryResult> GetResult(string query, MethodInfo specific)
        {
            var t = (Task)specific.Invoke(_connection, new object[] { query, new object[] { } });
            return await Task.Run(async () =>
            {
                SQLiteDataSourceQueryResult res = new SQLiteDataSourceQueryResult();
                
                var count = await _connection.ExecuteScalarAsync<int>("SELECT count(*) FROM " + _tableExpression);
                res.FullCount = count;

                t.Wait();
                var resultProp = t.GetType().GetTypeInfo().GetDeclaredProperty("Result");
                var data = (IList)resultProp.GetValue(t);
                res.Data = data;

                return res;
            });
        }
    }

    internal class SQLiteDataSourceQueryResult
    {
        public IList Data { get; set; }
        public int FullCount { get; set; }
    }
}