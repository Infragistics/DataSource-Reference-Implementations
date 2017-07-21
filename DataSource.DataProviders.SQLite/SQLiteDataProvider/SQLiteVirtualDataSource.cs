using Infragistics.Controls;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Windows;
using SQLite;

#if !PCL
using Infragistics.Controls.DataSource;
#endif
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
    /// Represents a virtual data source that gets data from a SQLite database.
    /// </summary>
    /// <remarks>
    /// Access to this class from threads other than the UI thread should be syncrhonized using the ExecutionContext.
    /// </remarks>
    public class SQLiteVirtualDataSource
            : VirtualDataSource
    {
        protected override IDataSourceVirtualDataProvider ResolveDataProviderOverride()
        {
            return new SQLiteVirtualDataSourceDataProvider();
        }

		private void OnTableExpressionChanged(string oldValue, string newValue)
		{
			if (ActualDataProvider is SQLiteVirtualDataSourceDataProvider)
			{
				((SQLiteVirtualDataSourceDataProvider)ActualDataProvider).TableExpression = TableExpression;
			}
			QueueAutoRefresh();
		}

        protected override bool IsSortingSupportedOverride
        {
            get
            {
                return true;
            }
        }

        protected override bool IsFilteringSupportedOverride
        {
            get
            {
                return true;
            }
        }

        protected override bool IsGroupingSupportedOverride
        {
            get
            {
                return true;
            }
        }

        private string _tableExpression = null;
		/// <summary>
		/// Gets or sets the table expression to pull data from.
		/// </summary>
		public string TableExpression
		{
			get
			{
				return _tableExpression;
			}
			set
			{
				var oldValue = _tableExpression;
				_tableExpression = value;
				if (oldValue != _tableExpression)
				{
					OnTableExpressionChanged(oldValue, _tableExpression);
				}
			}
		}

        private void OnGroupingColumnChanged(string oldValue, string newValue)
        {
            if (ActualDataProvider is SQLiteVirtualDataSourceDataProvider)
            {
                ((SQLiteVirtualDataSourceDataProvider)ActualDataProvider).GroupingColumn = GroupingColumn;
            }
            QueueAutoRefresh();
        }

        private string _groupingColumn = null;
        /// <summary>
        /// Gets or sets column to use for storing the count when counting group sizes.
        /// </summary>
        public string GroupingColumn
        {
            get
            {
                return _groupingColumn;
            }
            set
            {
                var oldValue = _groupingColumn;
                _groupingColumn = value;
                if (oldValue != _groupingColumn)
                {
                    OnGroupingColumnChanged(oldValue, _groupingColumn);
                }
            }
        }

        private void OnConnectionChanged(SQLiteAsyncConnection oldValue, SQLiteAsyncConnection newValue)
        {
            if (ActualDataProvider is SQLiteVirtualDataSourceDataProvider)
            {
                ((SQLiteVirtualDataSourceDataProvider)ActualDataProvider).Connection = Connection;
            }
            QueueAutoRefresh();
        }


        private SQLiteAsyncConnection _connection = null;
        /// <summary>
        /// Gets or sets a connection to use.
        /// </summary>
        public SQLiteAsyncConnection Connection
        {
            get
            {
                return _connection;
            }
            set
            {
                var oldValue = _connection;
                _connection = value;
                if (oldValue != _connection)
                {
                    OnConnectionChanged(oldValue, _connection);
                }
            }
        }

        private void OnProjectionTypeChanged(Type oldValue, Type newValue)
        {
            if (ActualDataProvider is SQLiteVirtualDataSourceDataProvider)
            {
                ((SQLiteVirtualDataSourceDataProvider)ActualDataProvider).ProjectionType = ProjectionType;
            }
            QueueAutoRefresh();
        }


        private Type _projectionType = null;
        /// <summary>
        /// Gets or sets a Type to project results into when using the SQLite.NET API.
        /// </summary>
        public Type ProjectionType
        {
            get
            {
                return _projectionType;
            }
            set
            {
                var oldValue = _projectionType;
                _projectionType = value;
                if (oldValue != _projectionType)
                {
                    OnProjectionTypeChanged(oldValue, _projectionType);
                }
            }
        }

        private void OnSelectExpressionOverrideChanged(string oldValue, string newValue)
		{
			if (ActualDataProvider is SQLiteVirtualDataSourceDataProvider)
			{
				((SQLiteVirtualDataSourceDataProvider)ActualDataProvider).SelectExpressionOverride = SelectExpressionOverride;
			}
			QueueAutoRefresh();
		}

		private string _selectExpressionOverride = null;
		/// <summary>
		/// Gets or sets a select expression to override the automatic select expression.
		/// </summary>
		public string SelectExpressionOverride
		{
			get
			{
				return _selectExpressionOverride;
			}
			set
			{
				var oldValue = _selectExpressionOverride;
				_selectExpressionOverride = value;
				if (_selectExpressionOverride != oldValue)
				{
					OnSelectExpressionOverrideChanged(oldValue, _selectExpressionOverride);
				}
			}
		}

		private void OnTimeoutMillisecondsChanged(int oldValue, int newValue)
		{
			if (ActualDataProvider is SQLiteVirtualDataSourceDataProvider)
			{
				((SQLiteVirtualDataSourceDataProvider)ActualDataProvider).TimeoutMilliseconds = TimeoutMilliseconds;
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
	}

}