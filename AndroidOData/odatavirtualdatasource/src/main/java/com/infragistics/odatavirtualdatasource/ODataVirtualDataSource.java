package com.infragistics.odatavirtualdatasource;

import android.content.Context;
import com.infragistics.controls.DataSourceVirtualDataProvider;
import com.infragistics.controls.VirtualDataSource;

public class ODataVirtualDataSource
    extends VirtualDataSource {

    public ODataVirtualDataSource(Context context) {
        super(context);
    }

    @Override
    protected DataSourceVirtualDataProvider resolveDataProviderOverride() {
        return new ODataVirtualDataSourceDataProvider();
    }

    private String _baseURI = null;
    public String getBaseUri() {
        return _baseURI;
    }
    public void setBaseURI(String value) {
        _baseURI = value;
        if (getActualDataProvider() instanceof ODataVirtualDataSourceDataProvider)
        {
            ((ODataVirtualDataSourceDataProvider)getActualDataProvider()).setBaseURI(_baseURI);
        }
        queueAutoRefresh();
    }

    private String _entitySet = null;
    public String getEntitySet() {
        return _entitySet;
    }
    public void setEntitySet(String value) {
        _entitySet = value;
        if (getActualDataProvider() instanceof ODataVirtualDataSourceDataProvider)
        {
            ((ODataVirtualDataSourceDataProvider)getActualDataProvider()).setEntitySet(_entitySet);
        }
        queueAutoRefresh();
    }

    private ODataVirtualDataSourceMetadataType _metadataType = null;
    public ODataVirtualDataSourceMetadataType getMetadataType() {
        return _metadataType;
    }
    public void setMetadataType(ODataVirtualDataSourceMetadataType value) {
        _metadataType = value;
        if (getActualDataProvider() instanceof ODataVirtualDataSourceDataProvider)
        {
            ((ODataVirtualDataSourceDataProvider)getActualDataProvider()).setMetadataType(_metadataType);
        }
        queueAutoRefresh();
    }

    private int _timeoutMilliseconds = 10000;
    public int getTimeoutMilliseconds() {
        return _timeoutMilliseconds;
    }
    public void setTimeoutMilliseconds(int value) {
        _timeoutMilliseconds = value;
        if (getActualDataProvider() instanceof ODataVirtualDataSourceDataProvider)
        {
            ((ODataVirtualDataSourceDataProvider)getActualDataProvider()).setTimeoutMilliseconds(_timeoutMilliseconds);
        }
        queueAutoRefresh();
    }
}
