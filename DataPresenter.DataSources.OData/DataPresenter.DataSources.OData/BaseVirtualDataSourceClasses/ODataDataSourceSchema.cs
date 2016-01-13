using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Simple.OData.Client;
using System.Diagnostics;
using Infragistics.Controls;

namespace DataPresenter.DataSources.OData
{
    /// <summary>
    /// Represents the schema of the items within a page of the virtual data source.
    /// </summary>
    public class ODataDataSourceSchema
        : IDataSourceSchema
    {
        private string _primaryKey = null;
        private string[] _valueNames;
        private DataSourceSchemaValueType[] _valueTypes;

        public ODataDataSourceSchema(string[] valueNames, DataSourceSchemaValueType[] valueTypes, string primaryKey)
        {
            _valueNames = valueNames;
            _valueTypes = valueTypes;
            _primaryKey = primaryKey;
        }

        /// <summary>
        /// Returns the names of the values contained on the items.
        /// </summary>
        public string[] ValueNames
        {
            get { return _valueNames; }
        }

        /// <summary>
        /// Returns the names of the values the represent the primary key or composite primary key of the items as a comma seperated string. This may be null or empty if there is no primary key defined.
        /// </summary>
        public string PrimaryKey
        {
            get { return _primaryKey; }
        }

        /// <summary>
        /// Returns the data types of the values contained on the items.
        /// </summary>
        public DataSourceSchemaValueType[] ValueTypes
        {
            get { return _valueTypes; }
        }
    }
}