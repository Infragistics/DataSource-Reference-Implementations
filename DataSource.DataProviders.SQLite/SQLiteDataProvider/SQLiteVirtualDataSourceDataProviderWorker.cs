using Infragistics.Controls;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
#if !PCL
using GridODataTest;
#endif

using SQLite;
using System.Xml;
using Infragistics.Controls.DataSource;
using System.Text;
using System.Collections;
using System.Reflection;
using System.Collections.Concurrent;

#if PCL
using Infragistics.Core.Controls.DataSource;
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
        public SortDescriptionCollection GroupDescriptions { get; internal set; }
        public string GroupingColumn { get; internal set; }
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

        private TableMapping _propertyMappings = null;

        public SQLiteVirtualDataSourceDataProviderWorker(SQLiteVirtualDataSourceDataProviderWorkerSettings settings)
            : base(settings)
        {
            _tableExpression = settings.TableExpression;
            _projectionType = settings.ProjectionType;
            _selectExpressionOverride = settings.SelectExpressionOverride;
            _connection = settings.Connection;
            _sortDescriptions = settings.SortDescriptions;
            _groupDescriptions = settings.GroupDescriptions;
            _groupingField = settings.GroupingColumn;

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
            _filterExpressions = settings.FilterExpressions;
            _desiredPropeties = settings.PropertiesRequested;
            _propertyMappings = ResolvePropertyMappings();
            ActualSchema = ResolveSchema();
            if (_groupDescriptions != null && _groupDescriptions.Count > 0)
            {
                _groupInformation = ResolveGroupInformation();
            }

            Task.Factory.StartNew(() => DoWork(), TaskCreationOptions.LongRunning);
        }

        private ISectionInformation[] ResolveGroupInformation()
        {
            UpdateSelect();
            UpdateFilterClause();

            StringBuilder sb = new StringBuilder();
            sb.Append("SELECT ");
            for (var i = 0; i < _groupDescriptions.Count; i++)
            {
                if (i > 0)
                {
                    sb.Append(", ");
                }
                var propertyName = _groupDescriptions[i].PropertyName;
                var col = _propertyMappings.FindColumnWithPropertyName(propertyName);
                if (col != null)
                {
                    propertyName = col.Name;
                }
                sb.Append(propertyName);
            }

            var groupingField = _groupingField;
            if (groupingField == null)
            {
                if (ActualSchema != null &&
                    ActualSchema.PrimaryKey != null &&
                    ActualSchema.PrimaryKey.Length > 0)
                {
                    groupingField = ActualSchema.PrimaryKey[0];

                    int index = -1;
                    for (var i = 0; i < ActualSchema.PropertyNames.Length; i++)
                    {
                        if (ActualSchema.PropertyNames[i] == groupingField)
                        {
                            index = i;
                            break;
                        }
                    }

                    if (index == -1 || !IsIntegral(ActualSchema.PropertyTypes[index]))
                    {
                        groupingField = null;
                    }
                }
            }

            if (groupingField == null)
            {
                throw new NotSupportedException("You must specify a valid grouping column to use to accumulate the count of groups for the target table.");
            }

            sb.Append(", COUNT(" + groupingField + ") as " + groupingField);
            AddTableExpression(sb);
            AddFilterClause(sb);           

            sb.Append(" GROUP BY ");
            for (var i = 0; i < _groupDescriptions.Count; i++)
            {
                if (i > 0)
                {
                    sb.Append(", ");
                }
                var propertyName = _groupDescriptions[i].PropertyName;
                var col = _propertyMappings.FindColumnWithPropertyName(propertyName);
                if (col != null)
                {
                    propertyName = col.Name;
                }
                sb.Append(propertyName);
            }

            AddOrderByClause(sb);

            EnsureSpecific();
            var t = (Task)_specific.Invoke(_connection, new object[] { sb.ToString(), new object[] { } });
            t.Wait();
            var resultProp = t.GetType().GetTypeInfo().GetDeclaredProperty("Result");
            var data = (IList)resultProp.GetValue(t);

            
            List<ISectionInformation> groupInformation = new List<ISectionInformation>();
            List<string> groupNames = new List<string>();

            foreach (var group in _groupDescriptions)
            {
                groupNames.Add(group.PropertyName);
            }
            
            var groupNamesArray = groupNames.ToArray();
            int currentIndex = 0;
            foreach (var group in data)
            {
                AddGroup(groupInformation, groupNames,
                    groupNamesArray, currentIndex,
                    group, groupingField);
            }

            return groupInformation.ToArray();
        }

        private void AddGroup(
            List<ISectionInformation> groupInformation, 
            List<string> groupNames, string[] groupNamesArray, 
            int currentIndex, object group, string groupingField)
        {
            List<object> groupValues = new List<object>();
            foreach (var name in groupNames)
            {
                var val = group.GetPropertyValue(name);
                groupValues.Add(val);
            }
            var groupCount = 0;

            var countVal = group.GetPropertyValue(groupingField);
            if (countVal != null)
            {
                groupCount = Convert.ToInt32(countVal);
            }
            DefaultSectionInformation groupInfo = new DefaultSectionInformation(
                currentIndex,
                currentIndex + (groupCount - 1),
                groupNamesArray,
                groupValues.ToArray());
            groupInformation.Add(groupInfo);
        }

        private bool IsIntegral(DataSourceSchemaPropertyType type)
        {
            return type == DataSourceSchemaPropertyType.IntValue ||
                type == DataSourceSchemaPropertyType.LongValue;
        }

        private TableMapping ResolvePropertyMappings()
        {
            return _connection.GetConnection().GetMapping(_projectionType);
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
            ISectionInformation[] groupInformation = null;

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
                groupInformation = _groupInformation;
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
                page = new SQLiteDataSourcePage(result, schema, groupInformation, pageIndex);
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
                page = new SQLiteDataSourcePage(null, schema, groupInformation, pageIndex);
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

            var pkCol = _propertyMappings.PK;
            if (pkCol != null)
            {
                actualPrimaryKey = new string[1] { pkCol.Name };
            }

            var actualSchema = new DefaultDataSourceSchema(
                schema.PropertyNames, schema.PropertyTypes, actualPrimaryKey, schema.PropertyDataIntents);

            return actualSchema;
        }

        private string _filterString = null;
        private string _selectedString = null;
        public const int SchemaRequestIndex = -1;
        private MethodInfo _specific;
        private SortDescriptionCollection _groupDescriptions;
        private ISectionInformation[] _groupInformation;
        private string _groupingField;

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
                UpdateSelect();
                AddSelect(sb);
                AddTableExpression(sb);
                UpdateFilterClause();
                AddFilterClause(sb);
                AddOrderByClause(sb);
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
                EnsureSpecific();

                task = GetResult(query, _specific, true);
            }

            request.TaskHolder = new AsyncDataSourcePageTaskHolder();
            request.TaskHolder.Task = task;

            lock (SyncLock)
            {
                Tasks.Add(request);
            }
        }

        private void EnsureSpecific()
        {
            if (_specific == null)
            {
                var queryAsync = _connection.GetType().GetTypeInfo().GetDeclaredMethod("QueryAsync");

                var specific = queryAsync.MakeGenericMethod(_projectionType);
                _specific = specific;
            }
        }

        private void AddSelect(StringBuilder sb)
        {
            sb.Append(_selectedString);
        }

        private void AddTableExpression(StringBuilder sb)
        {
            sb.Append(" FROM ");
            sb.Append(_tableExpression);
            sb.Append(" ");
        }

        private void AddFilterClause(StringBuilder sb)
        {
            if (_filterString != null)
            {
                sb.Append(" WHERE ");
                sb.Append(_filterString);
            }
        }

        private void AddOrderByClause(StringBuilder sb)
        {
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

                    var propertyName = sort.PropertyName;
                    var col = _propertyMappings.FindColumnWithPropertyName(propertyName);
                    if (col != null)
                    {
                        propertyName = col.Name;
                    }

                    if (sort.Direction ==
#if PCL
                            ListSortDirection.Descending
#else
                            System.ComponentModel.ListSortDirection.Descending
#endif
                            )
                    {
                        sb.Append(propertyName + " DESC");
                    }
                    else
                    {
                        sb.Append(propertyName + " ASC");
                    }
                }
            }
        }

        private void UpdateFilterClause()
        {
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

                    SQLiteDataSourceFilterExpressionVisitor visitor = new SQLiteDataSourceFilterExpressionVisitor(_propertyMappings);

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
        }

        private void UpdateSelect()
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
                    var actualProperties = new List<string>();
                    var propertySet = new HashSet<string>();
                    if (DesiredProperties != null)
                    {
                        for (var i = 0; i < DesiredProperties.Length; i++)
                        {
                            actualProperties.Add(DesiredProperties[i]);
                            if (!propertySet.Contains(DesiredProperties[i]))
                            {
                                propertySet.Add(DesiredProperties[i]);
                            }
                        }
                        string[] pk = null;
                        if (ActualSchema != null && ActualSchema.PrimaryKey != null &&
                            ActualSchema.PrimaryKey.Length > 0)
                        {
                            pk = ActualSchema.PrimaryKey;
                        }
                        if (pk != null)
                        {
                            for (var i = 0; i < pk.Length; i++)
                            {
                                if (!propertySet.Contains(pk[i]))
                                {
                                    propertySet.Add(pk[i]);
                                    actualProperties.Add(pk[i]);
                                }
                            }
                        }
                    }

                    if (actualProperties.Count > 0)
                    {
                        for (var i = 0; i < actualProperties.Count; i++)
                        {
                            if (i > 0)
                            {
                                selectBuilder.Append(", ");
                            }
                            var col = _propertyMappings.FindColumnWithPropertyName(actualProperties[i]);
                            if (col != null)
                            {
                                if (col.Name.Contains("__"))
                                {
                                    selectBuilder.Append(col.Name.Replace("__", ".") + " AS " + col.Name);
                                }
                                else
                                {
                                    selectBuilder.Append(col.Name);
                                }
                            }
                            else
                            {
                                selectBuilder.Append(actualProperties[i]);
                            }
                        }
                    }
                    else
                    {
                        selectBuilder.Append("*");
                    }
                }
                _selectedString = selectBuilder.ToString();
            }
        }

        private async Task<SQLiteDataSourceQueryResult> GetResult(string query, MethodInfo specific, bool getCount)
        {
            var t = (Task)specific.Invoke(_connection, new object[] { query, new object[] { } });
            return await Task.Run(async () =>
            {
                SQLiteDataSourceQueryResult res = new SQLiteDataSourceQueryResult();

                if (getCount)
                {
                    var filter = _filterString;
                    if (filter == null)
                    {
                        filter = "";
                    }
                    else
                    {
                        filter = " WHERE " + filter;
                    }
                    var count = await _connection.ExecuteScalarAsync<int>("SELECT count(*) FROM " + _tableExpression + filter);
                    res.FullCount = count;
                }

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
