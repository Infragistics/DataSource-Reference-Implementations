#An OData DataSource Reference Implementation
###for XamDataPresenter
------------------




####This open source solution contains several projects: 

1. A project that contains a reference implementation of an OData data source built on the functionality provided in the **AsyncPagingDataSourceBase** class shipped as part of **Infragistics NetAdvantage 16.1**.  Note that this project references the source code in the _Shared Project_ at [**..\ODataDataProvider**](https://github.com/Infragistics/DataSource-Reference-Implementations/tree/master/ODataDataProvider)
2. A sample application project that demonstrates how to use the OData data source with the XamDataPresenter _(shipped as part of Infragistics NetAdvantage 16.1.)_  The sample app presents a UI that lets you explore the capability of the OData data source as well as the XamDataPresenter's handling of the asynchronous/paged data fetching implemented in the data source.  Several public accessible OData service Uris are built-in to the sample to enable browsing of data containing different data types and hosted on servers with varying performance characteristics.

####AsyncPagingDataSourceBase Class Notes
*	This library is shipped as part of NA 16.1.
*	This **AsyncPagingDataSourceBase** (APDSB) component is an _abstract base class_ that contains functionality which is designed to work with the XamDataPresenter (XDP) family of controls only.  It does not support being used as a datasource for other controls that accept data sources which implement IEnumerable.
*	The APDSB relies on the _Infragistics.Controls.DataSource_.**VirtualDataSource** component, which is delivered as part of the Infragistics DataVisualization library in **InfragisticsWPF4.DataVisualization.v16.1.dll**.  The **VirtualDataSource** class _(and derived classes)_ contain the bulk of the async, paging and threaded data fetching logic.
*	Concrete implementations that derive from APDSB must override the **CreateUnderlyingDataSource** protected virtual method to provide an instance of a VirtualDataSource-derived class.
*	This APDSB is implemented as an abstract base class and its primary function is to serve as a bridge between the XDP and an underlying data source implementation derived from **VirtualDataSource** _(described above)_.  The APDSB exposes the data fetching, paging, filtering, sorting and schema capabilities of the **VirtualDataSource** to the XDP primarily via the _ICollectionView_ interface.  This allows the XDP to delegate time consuming tasks like sorting and filtering to the backend store for improved performance and reduced data flow over network connections.
*	The APDSB exposes 3 public properties:
  +	**DesiredFields** - Returns/sets the list of fields in the data schema to include when fetching data from a remote data source.  By setting this property to a subset of the fields in the backend schema you can improve the performance of the data source since less data will be requested and returned over the server connection.

  _**NOTE:** You can also limit the amount of data requested and returned by the APDSB by  defining a FieldLayout in the XDP with a subset of the schema fields.  This will automatically set the APDSB’s DesiredFields property behind the scenes._
  +	**DesiredPageSize** – Returns/sets the desired logical page size _(i.e., the number of rows to fetch)_ when issuing a request to the backend to return a ‘page’ of data.
  +	**MaximumCachedPages** – Returns/sets the maximum number of fetched data pages that the APDSB should retain in memory to optimize subsequent fetches.  When the APDSB receives a request for a data row, it first checks the page cache to see if it has already fetched the row.  If not found there, a request for the page containing the row is sent to the backend store.

These properties can be used to tune the fetching and caching behavior of the APDSB to optimize performance for specific server scenarios.

####XamDataPresenter Notes
The XamDataPresenter (XDP) has undergone some minor modifications for 16.1 to support using the APDSB as a data source.  For example, a  new property on the XDP's **DataRecordCellArea** class and a new **DataPresenterBrushKey** let the developer control what the XDP displays when data fetches are pending.  Here are some highlights:
*	The developer uses the APDSB with the XDP the same way that any other data source is used – i.e., by setting the XDP’s DataSource property to an instance of an APDSB-derived data source.  No other property settings are required to use the APDSB with the XDP.
*	XDP performance can be improved when using the APDSB if you know the contents of the OData schema for the EntitySet whose data you are retrieving.  By defining a FieldLayout in the XDP with a subset of the schema fields, the APDSB will request and return less data over the server connection.   

  _**NOTE:** You can also limit the amount of data requested and returned by the APDSB by setting the APDSB’s DesiredFields property to a subset of the fields in the backend schema._
  
*	The end user will notice some differences in XDP functionality and behavior when an APDSB is used as a data source:
  +	Arguably the biggest change in XDP behavior that is noticeable by the end user is the display of _‘overlay elements’_ when the XDP is waiting for the APDSB to respond to a request for more data.  While the XDP is waiting for requested data to arrive, an overlay element is displayed in the cell area of XDP records that have not yet received their data from the APDSB.  Once the data has arrived, the XDP will automatically refresh the display, remove the overlay elements and display the data as it normally does.  Depending on server performance and connection speed, the overlay elements may remain displayed for a fraction of a second or several seconds.  

  Note that since the APDSB requests data in pages _(which by default contain 200 records but that value can be customized)_ not every vertical scroll activity results in overlay elements.  For example, if the XDP is currently displaying 20 records per screen, the user can page through 10 screens worth of data before _(possibly)_ seeing overlay elements again.  I say ‘possibly’ because the end user may be able to page thru more than 10 screens in this scenario if they rapidly scroll thru screens.  This is because the APDSB uses a predictive paging algorithm _(principally based on the number of data pages explicitly requested over a defined sampling period)_ to automatically request additional pages that it _‘predicts’_ the user will ask for, before they are actually requested.

_**NOTE:** The XDP can use the APDSP in all its views (GridView, CardView etc.) and appropriate overlay elements are displayed as described above._

*	The XDP delegates all sorting and filtering operations to the backend _(via the APDSB)_ for maximum efficiency.  No sorting or filtering is performed directly by the XDP.
* **LIMITATIONS** While the bulk of XDP functionality is supported when using the APDSB as a data source, there are some limitations:
  * Hierarchical and non-homogenous data are not supported.  Only flat homogeneous data is supported
  * Editing is not supported in the XamDataPresenter when using APDSB – the grid is read-only in this scenario.  Adding/removing records is also not supported.
  * The XDP’s filter dropdown does not include a list of unique data values from the associated column since the APDSB does not support the ability to query the backend for those unique values.  
  * A **FieldLayoutSettings.FilterUIType** of **LabelIcons** is not supported when the XDP is using an APDSB as its data source.  The **FilterUIType** will always resolve to **FilterUIType.FilterRecord** in this scenario since the unique data values that would normally be displayed in the filter dropdown when the Label icon is clicked are not available as described above.
  * The following ComparisonOperators are not supported when filtering:
    +	Match
    +	DoesNotMatch
    + Like
    +	NotLike
    +	Top
    +	TopPercentile
    + Bottom
    + BottomPercentile
  * The following SpecialFilterOperands are not supported:
    + BlanksOperand 
    + AverageOperand
  * Grouping is not currently supported
  * Since the XDP optimizes filtering and sorting by delegating to the backend store via the APDSB, sorting and filtering is only supported for actual data values. Displayed values and unbound column values cannot be used to sort or filter.
  * New properties added to the XDP:
    + **DataPresenterBrushKeys.DataPendingOverlayBrushKey** – Defines a _ResourceKey_ used to identify a brush resource that is referenced by XDP templates as a _DynamicResource_ and  used to color the cell area background when the data for the cell is pending – i.e., it has been requested but has not yet been delivered. 
    + **DataRecordCellArea.IsDynamicDataPending** - Returns true when the data for a **DataRecord** has been requested but has not yet been delivered.
