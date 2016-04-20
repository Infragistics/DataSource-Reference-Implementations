package com.infragistics.odatavirtualdatasource;

import com.fasterxml.jackson.databind.util.LinkedNode;
import com.infragistics.controls.*;
import org.apache.olingo.client.api.domain.ClientEntity;
import org.apache.olingo.client.api.domain.ClientValue;

import java.util.*;

public class ODataVirtualDataSourceDataProvider
    implements DataSourceVirtualDataProvider {

    private ODataVirtualDataSourceDataProviderWorker _worker;
    private LinkedList<Integer> _requests = new LinkedList<Integer>();
    private DataSourcePageLoadedCallback _callback;
    private SortDescriptionCollection _sortDescriptions = new SortDescriptionCollection();
    private FilterExpressionCollection _filterExpressions = new FilterExpressionCollection();

    public ODataVirtualDataSourceDataProvider() {
        _sortDescriptions.addChangedListener(new SyncableObservableCollectionChangedListener() {
            @Override
            public void onChanged(Object dataSourceSortDescriptionCollection) {
                onSortDescriptionsChanged();
            }
        });
    }

    public boolean getIsSortingSupported() {
        return true;
    }

    public SortDescriptionCollection getSortDescriptions() {
        return _sortDescriptions;
    }

    public boolean getIsFilteringSupported() {
        return true;
    }

    public FilterExpressionCollection getFilterExpressions() {
        return _filterExpressions;
    }

    private String[] _propertiesRequested;
    @Override
    public String[] setPropertiesRequested(String[] strings) {
        _propertiesRequested = strings;

        queueAutoRefresh();
        return _propertiesRequested;
    }

    @Override
    public String[] getPropertiesRequested() {
        return _propertiesRequested;
    }

    private void onSortDescriptionsChanged() {
        queueAutoRefresh();
    }

    @Override
    public void addPageRequest(int pageIndex, DataSourcePageRequestPriority dataSourcePageRequestPriority) {
        if (getDeferAutoRefresh()) {
            return;
        }
        if (_worker != null && _worker.getIsShutdown()) {
            _worker = null;
            _callback = null;
        }

        if (_worker == null)
        {
            createWorker();
        }

        if (dataSourcePageRequestPriority == DataSourcePageRequestPriority.HIGH)
        {
            _requests.addFirst(pageIndex);
        }
        else
        {
            _requests.addLast(pageIndex);
        }
        if (!_worker.addPageRequest(pageIndex, dataSourcePageRequestPriority)) {
            _worker = null;
            _callback = null;
            addPageRequest(pageIndex, dataSourcePageRequestPriority);
        }
    }

    private void createWorker() {
        _callback = new DataSourcePageLoadedCallback() {
            @Override
            public void invoke(DataSourcePage dataSourcePage, int fullCount, int actualPageSize) {
                raisePageLoaded(dataSourcePage, fullCount, actualPageSize);
            }
        };

        ODataVirtualDataSourceDataProviderWorkerSettings settings = new ODataVirtualDataSourceDataProviderWorkerSettings();
        settings.setBsaeUri(_baseURI);
        settings.setEntitySet(_entitySet);
        settings.setPageSizeRequested(_pageSizeRequested);
        settings.setTimeoutMilliseconds(_timeoutMilliseconds);
        settings.setMetadataType(_metadataType);
        settings.setPageLoaded(_callback);
        settings.setExecutionContext(_executionContext);
        settings.setSortDescriptions(_sortDescriptions);
        settings.setFilterExpressions(_filterExpressions);
        settings.setDesiredProperties(_propertiesRequested);

        _worker = new ODataVirtualDataSourceDataProviderWorker(settings);
    }

    @Override
    public void removePageRequest(int pageIndex) {
        _requests.removeFirstOccurrence(pageIndex);
        if (_worker == null)
        {
            return;
        }
        _worker.removePageRequest(pageIndex);
    }

    @Override
    public void removeAllPageRequests() {
        _requests.clear();
        if (_worker == null)
        {
            return;
        }
        _worker.removeAllPageRequests();
    }

    @Override
    public void close() {
        if (_worker != null)
        {
            _worker.shutdown();
            _worker = null;
            _callback = null;
        }
    }

    private DataSourcePageLoadedCallback _pageLoaded;
    @Override
    public DataSourcePageLoadedCallback setPageLoaded(DataSourcePageLoadedCallback pageLoaded) {
        _pageLoaded = pageLoaded;
        killWorker();
        queueAutoRefresh();
        return _pageLoaded;
    }

    private boolean valid()
    {
        return _baseURI != null &&
                _entitySet != null;
    }

    @Override
    public DataSourcePageLoadedCallback getPageLoaded() {
        return _pageLoaded;
    }

    private void raisePageLoaded(DataSourcePage page, int fullCount, int actualPageSize)
    {
        if (_pageLoaded != null)
        {
            _currentFullCount = fullCount;

            if (_currentSchema == null)
            {
                if (page != null)
                {
                    _currentSchema = page.schema();
                }
                for (int i = 0; i < _initializedHandlers.size(); i++) {
                    OnDataSourceDataProviderSchemaChangedListener handler = _initializedHandlers.get(i);
                    if (handler != null) {
                        handler.onSchemaChanged(this, new DataSourceDataProviderSchemaChangedEvent(_currentSchema, _currentFullCount));
                    }
                }
            }
            if (page.pageIndex() != ODataVirtualDataSourceDataProviderWorker.SCHEMA_REQUEST_INDEX) {
                _pageLoaded.invoke(page, fullCount, actualPageSize);
            }
        }
    }

    private void killWorker()
    {
        if (_worker != null) {
            _worker.shutdown();
            _worker = null;
            _callback = null;
        }
    }

    private int _pageSizeRequested = 50;
    @Override
    public int setPageSizeRequested(int value) {
        _pageSizeRequested = value;
        killWorker();
        queueAutoRefresh();
        return _pageSizeRequested;
    }

    @Override
    public int getPageSizeRequested() {
        return _pageSizeRequested;
    }

    private int _timeoutMilliseconds = 10000;
    public int setTimeoutMilliseconds(int value) {
        _timeoutMilliseconds = value;
        killWorker();
        queueAutoRefresh();
        return _timeoutMilliseconds;
    }

    public int getTimeoutMilliseconds() {
        return _timeoutMilliseconds;
    }

    private String _baseURI = null;
    public String setBaseURI(String value) {
        String oldValue = _baseURI;
        _baseURI = value;
        if (oldValue != _baseURI) {
            queueAutoRefresh();
            if (valid() && getDeferAutoRefresh()) {
                queueSchemaFetch();
            }
        }
        return value;
    }

    private ODataVirtualDataSourceMetadataType _metadataType = ODataVirtualDataSourceMetadataType.MINIMAL;
    public ODataVirtualDataSourceMetadataType setMetadataType(ODataVirtualDataSourceMetadataType value) {
        _metadataType = value;
        killWorker();
        queueAutoRefresh();
        return _metadataType;
    }

    public String getBaseURI() {
        return _baseURI;
    }

    private String _entitySet = null;
    public String setEntitySet(String value) {
        String oldValue = _entitySet;
        _entitySet = value;
        if (oldValue != _entitySet) {
            queueAutoRefresh();
            if (valid() && getDeferAutoRefresh()) {
                queueSchemaFetch();
            }
        }
        return value;
    }

    public String getEntitySet() {
        return _entitySet;
    }

    @Override
    public Object getItemValue(Object o, String s) {
        ClientEntity entity = (ClientEntity)o;
        ClientValue value = entity.getProperty(s).getValue();
        if (!value.isPrimitive()) {
            return value;
        }
        return value.asPrimitive().toValue();
    }

    private List<OnDataSourceDataProviderSchemaChangedListener> _initializedHandlers = new ArrayList<OnDataSourceDataProviderSchemaChangedListener>();
    @Override
    public void addOnSchemaChangedListener(OnDataSourceDataProviderSchemaChangedListener onDataSourceDataProviderSchemaChangedListener) {
        _initializedHandlers.add(onDataSourceDataProviderSchemaChangedListener);
    }

    @Override
    public void removeOnSchemaChangedListener(OnDataSourceDataProviderSchemaChangedListener onDataSourceDataProviderSchemaChangedListener) {
        _initializedHandlers.remove(onDataSourceDataProviderSchemaChangedListener);
    }

    private int _currentFullCount = 0;
    private DataSourceSchema _currentSchema;

    @Override
    public int getActualCount() {
        return _currentFullCount;
    }

    @Override
    public DataSourceSchema getActualSchema() {
        return _currentSchema;
    }

    private DataSourceExecutionContext _executionContext;
    @Override
    public DataSourceExecutionContext setExecutionContext(DataSourceExecutionContext value) {
        _executionContext = value;
        killWorker();
        queueAutoRefresh();
        return value;
    }

    @Override
    public DataSourceExecutionContext getExecutionContext() {
        return _executionContext;
    }

    private DataSourceDataProviderUpdateNotifier _updateNotifier;
    @Override
    public DataSourceDataProviderUpdateNotifier setUpdateNotifier(DataSourceDataProviderUpdateNotifier dataSourceDataProviderUpdateNotifier) {
        _updateNotifier = dataSourceDataProviderUpdateNotifier;
        return _updateNotifier;
    }

    @Override
    public DataSourceDataProviderUpdateNotifier getUpdateNotifier() {
        return _updateNotifier;
    }

    @Override
    public void notifySetItem(int i, Object oldItem, Object newItem) {
        if (_updateNotifier != null) {
            _updateNotifier.notifySetItem(i, oldItem, newItem);
        }
    }

    @Override
    public void notifyClearItems() {
        if (_updateNotifier != null) {
            _updateNotifier.notifyClearItems();
        }
    }

    @Override
    public void notifyInsertItem(int i, Object newItem) {
        if (_updateNotifier != null) {
            _updateNotifier.notifyInsertItem(i, newItem);
        }
    }

    @Override
    public void notifyRemoveItem(int i, Object oldItem) {
        if (_updateNotifier != null) {
            _updateNotifier.notifyRemoveItem(i, oldItem);
        }
    }

    public boolean _autoRefreshQueued = false;


    public void queueAutoRefresh() {
        if (getDeferAutoRefresh()) {
            return;
        }

       if (_autoRefreshQueued) {
            return;
        }

        if (getExecutionContext() != null) {
            _autoRefreshQueued = true;
            getExecutionContext().enqueueAction(new DataSourceExecutionContextExecuteCallback(this, "Infragistics.Controls.DataSource.Implementation.BaseDataSource.DoRefreshInternal") {
                public void invoke() {
                    doRefreshInternal();
                }
            });
        }

    }


    public void doRefreshInternal() {
        if (getDeferAutoRefresh()) {
            _autoRefreshQueued = false;
            return;
        }

        if (!_autoRefreshQueued) {
            return;
        }

        _autoRefreshQueued = false;
        refreshInternal();
    }


    public void refreshInternal() {
       refreshInternalOverride();
    }


    protected void refreshInternalOverride() {
        removeAllPageRequests();
        killWorker();
        createWorker();
        //TODO: should we have this here? prob need to readd request for current page instead.
        _worker.addPageRequest(0, DataSourcePageRequestPriority.NORMAL);
    }


    public void flushAutoRefresh() {
        doRefreshInternal();
    }


    public void refresh() {
        refreshInternal();
    }

    private boolean _shouldDeferUpdates = false;


    @Override
    public boolean setDeferAutoRefresh(boolean value) {

        boolean oldValue = _shouldDeferUpdates;
        _shouldDeferUpdates = value;
        if (_shouldDeferUpdates != oldValue) {
            onShouldDeferAutoRefreshChanged(oldValue, value);
        }

        return value;
    }

    private void onShouldDeferAutoRefreshChanged(boolean oldValue, boolean value) {
        queueAutoRefresh();
        if (_shouldDeferUpdates && valid() && _currentSchema == null) {
            queueSchemaFetch();
        }
    }

    @Override
    public boolean getDeferAutoRefresh() {

        return _shouldDeferUpdates;

    }

    private boolean _schemaFetchQueued = false;
    public void queueSchemaFetch()
    {
        if (_schemaFetchQueued)
            {
                        return;
        }

        if (getExecutionContext() != null) {
            _schemaFetchQueued = true;
            getExecutionContext().enqueueAction(new DataSourceExecutionContextExecuteCallback() {
                @Override
                public void invoke() {
                    doSchemaFetchInternal();
                }
            });
        }
    }

    void doSchemaFetchInternal() {
        if (!_schemaFetchQueued) {
            return;
        }
        _schemaFetchQueued = false;
        schemaFetchInternal();
    }

    void schemaFetchInternal()
    {
        schemaFetchInternalOverride();
    }

    protected void schemaFetchInternalOverride() {
        if (!getDeferAutoRefresh()) {
            return;
        }
        removeAllPageRequests();
        killWorker();
        createWorker();

        addSchemaRequest();
    }

    private void addSchemaRequest() {
        _worker.addPageRequest(
                ODataVirtualDataSourceDataProviderWorker.SCHEMA_REQUEST_INDEX,
                DataSourcePageRequestPriority.HIGH
        );
    }
    

    @Override
    public boolean getNotifyUsingSourceIndexes() {
        return false;
    }

    @Override
    public int indexOfItem(Object o) {
        return -1;
    }

    @Override
    public int indexOfKey(Object[] objects) {
        return -1;
    }

    @Override
    public boolean getIsItemIndexLookupSupported() {
        return false;
    }

    @Override
    public boolean getIsKeyIndexLookupSupported() {
        return false;
    }
}
