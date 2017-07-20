using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Simple.OData.Client;
using System.Diagnostics;
using Infragistics.Controls;

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
    /// <summary>
    /// Represents a single page of data retreived from the ODataVirtualDataSource
    /// </summary>
    public class ODataDataSourcePage
        : IDataSourcePage
    {
        IDictionary<string, object>[] _actualData;
        private IDataSourceSchema _schema;
        private int _pageIndex;
        private IGroupInformation[] _groupInfromation;

        public ODataDataSourcePage(IEnumerable<IDictionary<string, object>> sourceData, IDataSourceSchema schema, IGroupInformation[] groupInformation, int pageIndex)
        {
            if (sourceData == null)
            {
                _actualData = null;
            }
            else
            {
                _actualData = sourceData.ToArray();
            }
            _schema = schema;
            _groupInfromation = groupInformation;
            _pageIndex = pageIndex;
        }

        /// <summary>
        /// Gets the actual number of records in the current page.
        /// </summary>
        /// <returns>The number of records in the page.</returns>
        public int Count()
        {
            return _actualData.Length;
        }

        /// <summary>
        /// Gets the item at the specified index within the page.
        /// </summary>
        /// <param name="index">The index, within the page, from which to get the desired item.</param>
        /// <returns>The requested item from the page.</returns>
        public object GetItemAtIndex(int index)
        {
            return _actualData[index];
        }

        /// <summary>
        /// Gets the desired item value, by name, from the item at the provided index, within the page.
        /// </summary>
        /// <param name="index">The index, within the page, for the item from which to get values.</param>
        /// <param name="valueName">The name of the value to read from the desired item.</param>
        /// <returns>The desired value from the requested item.</returns>
        public object GetItemValueAtIndex(int index, string valueName)
        {
            var item = _actualData[index];
            if (!item.ContainsKey(valueName))
            {
                return null;
            }
            return item[valueName];
        }

        /// <summary>
        /// Gets the absolute index of the current page within the data source.
        /// </summary>
        /// <returns></returns>
        public int PageIndex()
        {
            return _pageIndex;
        }

        /// <summary>
        /// Gets the schema associated with the items of the current page.
        /// </summary>
        /// <returns></returns>
        public IDataSourceSchema Schema()
        {
            return _schema;
        }

        /// <summary>
        /// Information about group boundaries, if available. Not required if unchanged or not yet available.
        /// </summary>
        /// <returns>An array of information about the group boundaries, in order, if available, otherwise null.</returns>
        public IGroupInformation[] GetGroupInformation()
        {
            return _groupInfromation;
        }
    }
}