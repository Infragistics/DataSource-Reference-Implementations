using Infragistics.Controls;
using Simple.OData.Client;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using GridODataTest;
using System.Xml;

namespace DataPresenter.DataSources.OData
{
    internal class ODataVirtualDataSourceDataProviderWorkerSettings
        : AsyncVirtualDataSourceDataProviderWorkerSettings
    {
        public string BaseUri { get; set; }
        public string EntitySet { get; set; }

        public DataSourceSortDescriptionCollection SortDescriptions { get; set; }
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

        protected DataSourceSortDescriptionCollection SortDescriptions
        {
            get
            {
                return _sortDescriptions;
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

			SchemaProvider sp = new SchemaProvider(this._client);
			return sp.GetODataDataSourceSchema(this._entitySet);
        }

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
            if (sortDescriptions != null)
            {
                foreach (var sort in sortDescriptions)
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

	#region SchemaProvider
	internal class SchemaProvider
	{
		#region Member Variables
		private ODataClient				_client;
		private string					_entityTypeSchemaNamespace;
		private string					_entitySetSchemaNamespace;
		#endregion //Member Variables

		#region Constructor
		internal SchemaProvider(ODataClient client)
		{
			this._client			= client;
            //TODO: I had to change this. Which version of the client are you using Joe?
            var t = this._client.GetMetadataDocumentAsync();
            t.Wait();
            var metadataDocument = t.Result;

			if (null == metadataDocument)
				return;

			XmlDocument xmlDoc		= new XmlDocument();
			xmlDoc.LoadXml(metadataDocument);

			XmlNodeList schemaElements = xmlDoc.GetElementsByTagName("Schema");
			if (null == schemaElements)
				return;

			XmlNodeList entitySetElements		= null;
			XmlNodeList entityTypeElements		= null;
			foreach (XmlElement schemaElement in schemaElements)
			{
				if (null == entitySetElements)
				{
					XmlNodeList nodes = schemaElement.GetElementsByTagName("EntityContainer");
					if (null != nodes && nodes.Count > 0)
					{
						entitySetElements = (nodes[0] as XmlElement).GetElementsByTagName("EntitySet");
						if (null != entitySetElements)
							this._entitySetSchemaNamespace = schemaElement.Attributes["Namespace"].Value;
					}
				}

				if (null == entityTypeElements)
				{
					entityTypeElements = schemaElement.GetElementsByTagName("EntityType");
					if (null != entityTypeElements)
						this._entityTypeSchemaNamespace = schemaElement.Attributes["Namespace"].Value;
				}
			}
			
			if (null == entitySetElements || null == entityTypeElements)
				return;

			this.Schema = new Schema(this._entityTypeSchemaNamespace, entityTypeElements, entitySetElements);		
		}
		#endregion //Constructor

		#region Properties

		#region Schema
		private Schema Schema { get; set; }
		#endregion //Schema

		#endregion //Properties

		#region Methods

		#region GetODataDataSourceSchema
		internal ODataDataSourceSchema GetODataDataSourceSchema(string entitySet)
		{
			List<string>					valueNames = new List<string>();
			List<DataSourceSchemaValueType> valueTypes = new List<DataSourceSchemaValueType>();
            List<string> primaryKey = new List<string>();

			EntitySet es = this.Schema.EntitySets[entitySet];
			if (null != es)
			{
				Entity entity = this.Schema.Entities[es.EntityName];
				if (null != entity)
				{
					foreach (EntityProperty property in entity.Properties.Values)
					{
						valueNames.Add(property.Name);

						if (property.Type == typeof(string))
							valueTypes.Add(DataSourceSchemaValueType.StringValue);
						else
						if (property.Type == typeof(int) ||
							property.Type == typeof(Int16) ||
							property.Type == typeof(Int32) ||
							property.Type == typeof(Int64))
							valueTypes.Add(DataSourceSchemaValueType.IntValue);
						else
						if (property.Type == typeof(bool))
							valueTypes.Add(DataSourceSchemaValueType.BooleanValue); 
						else
						if (property.Type == typeof(byte))
							valueTypes.Add(DataSourceSchemaValueType.ShortValue);  //TODO: No byte type in DataSourceSchemaValueType???  Use short for now
						else
						if (property.Type == typeof(DateTime))
							valueTypes.Add(DataSourceSchemaValueType.DateTimeValue);
						else
						if (property.Type == typeof(long))
							valueTypes.Add(DataSourceSchemaValueType.LongValue);
						else
						if (property.Type == typeof(decimal))
							valueTypes.Add(DataSourceSchemaValueType.FloatValue);  //TODO: No decimal type in DataSourceSchemaValueType???  Use float for now
						else
						if (property.Type == typeof(sbyte))
							valueTypes.Add(DataSourceSchemaValueType.ShortValue);  //TODO: No sbyte type in DataSourceSchemaValueType???  Use short for now
						else
							valueTypes.Add(DataSourceSchemaValueType.ObjectValue);
					}

                    primaryKey.AddRange(entity.PrimaryKey);
				}
			}

            return new ODataDataSourceSchema(valueNames.ToArray(), valueTypes.ToArray(), String.Join(",", primaryKey.ToArray()));
		}
		#endregion //GetODataDataSourceSchema

		#endregion //Methods
	}
	#endregion //SchemaProvider

	#region Schema
	internal class Schema
	{
		#region Member Variables
		private Dictionary<string, Entity>			_entities;
		private Dictionary<string, EntitySet>		_entitySets;
		#endregion //Member Variables

		#region Constructor
		internal Schema(string @namespace, XmlNodeList entityTypeElements, XmlNodeList entitySetElements)
		{
			this.Namespace = @namespace;
			this.LoadEntities(entityTypeElements);
			this.LoadEntitySets(entitySetElements);
		}
		#endregion //Constructor

		#region Properties

		#region Entities
		internal Dictionary<string, Entity> Entities
		{
			get
			{
				if (null == this._entities)
					this._entities = new Dictionary<string, Entity>();

				return this._entities;
			}
		}
		#endregion //Entities

		#region EntitySets
		internal Dictionary<string, EntitySet> EntitySets
		{
			get
			{
				if (null == this._entitySets)
					this._entitySets = new Dictionary<string, EntitySet>();

				return this._entitySets;
			}
		}
		#endregion //EntitySets

		#region Namespace
		internal string Namespace { get; private set; }
		#endregion //Namespace

		#endregion //Properties

		#region Methods

		#region LoadEntities
		private void LoadEntities(XmlNodeList entityTypeElements)
		{
			foreach (XmlNode node in entityTypeElements)
			{
				Entity entity = new Entity(node.Attributes["Name"].Value, node);
				this.Entities.Add(entity.Name, entity);
			};

		}
		#endregion //LoadEntities

		#region LoadEntitySets
		private void LoadEntitySets(XmlNodeList entitySetElements)
		{
			foreach (XmlNode node in entitySetElements)
			{
				EntitySet entitySet = new EntitySet(node.Attributes["Name"].Value, node.Attributes["EntityType"].Value);
				this.EntitySets.Add(entitySet.Name, entitySet);
			};

		}
		#endregion //LoadEntitySets

		#endregion //Methods
	}
	#endregion //Schema

	#region Entity
	internal class Entity
	{
		#region Member Variables
		private Dictionary<string,EntityProperty>	_properties;
        private List<string> _primaryKey;
		#endregion //Member Variables

		#region Constructor
		internal Entity(string name, XmlNode entityNode)
		{
			this.Name = name;
			this.LoadProperties(entityNode);
            this.LoadPrimaryKey(entityNode);
		}
        #endregion //Constructor

        #region Properties

        #region Name
        internal string Name { get; private set; }
		#endregion //Name

		#region Properties
		internal Dictionary<string, EntityProperty> Properties
		{
			get
			{
				if (null == this._properties)
					this._properties = new Dictionary<string, EntityProperty>();

				return this._properties;
			}
		}

        internal List<string> PrimaryKey
        {
            get
            {
                if (null == this._primaryKey)
                {
                    this._primaryKey = new List<string>();
                }

                return this._primaryKey;
            }
        }
		#endregion //Properties

		#endregion //Properties

		#region Methods

		#region LoadProperties
		private void LoadProperties(XmlNode entityNode)
		{
			foreach (XmlNode node in entityNode.ChildNodes)
			{
				if (node.Name == "Property")
				{
					string	name	= node.Attributes["Name"].Value;
					string	type	= node.Attributes["Type"].Value;

					this.Properties.Add(name, new EntityProperty(name, type));
				}
			};

		}

        private void LoadPrimaryKey(XmlNode entityNode)
        {
            foreach (XmlNode node in entityNode.ChildNodes)
            {
                if (node.Name == "Key")
                {
                    foreach (XmlNode keyNode in node.ChildNodes)
                    {
                        if (keyNode.Name == "PropertyRef")
                        {
                            PrimaryKey.Add(keyNode.Attributes["Name"].Value);
                        }
                    }
                }
            };
        }
        #endregion //LoadProperties

        #endregion //Methods
    }
	#endregion //Entity

	#region EntityProperty
	internal class EntityProperty
	{
		internal EntityProperty(string name, string schemaType)
		{
			this.Name	= name;
			this.Type	= this.GetTypeFromSchemaType(schemaType);
		}

		internal string Name { get; private set; }
		internal bool IsNullable { get; private set; }
		internal Type Type { get; private set; }

		private Type GetTypeFromSchemaType(string schemaType)
		{
			switch (schemaType)
			{
				case "Edm.Binary":
					return typeof(object);
				case "Edm.Boolean":
					return typeof(bool);
				case "Edm.Byte":
					return typeof(byte);
				case "Edm.Date":
					return typeof(DateTime);
				case "Edm.DateTimeOffset":
					return typeof(long);
				case "Edm.Decimal":
					return typeof(decimal);
				case "Edm.Double":
					return typeof(double);
				case "Edm.Float":
					return typeof(float);
				case "Edm.Guid":
					return typeof(Guid);
				case "Edm.Int16":
					return typeof(Int16);
				case "Edm.Int32":
					return typeof(Int32);
				case "Edm.Int64":
					return typeof(Int64);
				case "Edm.SByte":
					return typeof(sbyte);
				case "Edm.String":
					return typeof(string);
				case "Edm.Time":
					return typeof(DateTime);
				default:
					return typeof(string);
			}
		}
	}
	#endregion //EntityProperty

	#region EntitySet
	internal class EntitySet
	{
		#region Member Variables
		private Dictionary<string, EntityProperty> _properties;
		#endregion //Member Variables

		#region Constructor
		internal EntitySet(string name, string entityType)
		{
			this.Name		= name;
			this.EntityType	= entityType;

			if (entityType.Contains("."))
			{
				string [] parts = entityType.Split('.');
				if (parts.Length == 2)
				{
					this.EntityNamespace = parts[0];
					this.EntityName = parts[1];
				}
				else
				{
					// Consider everything up to the last dot as the namespace, and the string after the dot as the name.
					int i					= entityType.LastIndexOf(".");
					this.EntityNamespace	= entityType.Substring(0, i);
					this.EntityName			= entityType.Substring(i + 1);
				}
			}
			else
			{
				//TODO: Review this - not sure what to do in this case.
				this.EntityNamespace	= entityType;
				this.EntityName			= entityType;
			}
		}
		#endregion //Constructor

		#region //Properties

		#region EntityName
		internal string EntityName { get; private set; }
		#endregion //EntityName

		#region EntityNamespace
		internal string EntityNamespace { get; private set; }
		#endregion //EntityNameSpace

		#region EntityType
		internal string EntityType { get; private set; }
		#endregion //EntityType

		#region Name
		internal string Name { get; private set; }
		#endregion //Name

		#endregion //Properties
	}
	#endregion //EntitySet
}