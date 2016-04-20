package com.infragistics.odatavirtualdatasource;

import com.infragistics.controls.AsyncVirtualDataSourceDataProviderWorkerSettings;
import com.infragistics.controls.FilterExpressionCollection;
import com.infragistics.controls.SortDescriptionCollection;

public class ODataVirtualDataSourceDataProviderWorkerSettings
    extends AsyncVirtualDataSourceDataProviderWorkerSettings {

    private String _baseUri;
    public String getBaseUri() {
        return _baseUri;
    }
    public void setBsaeUri(String value) {
        _baseUri = value;
    }
    private String _entitySet;
    public String getEntitySet() {
        return _entitySet;
    }
    public void setEntitySet(String value) {
        _entitySet = value;
    }

    private ODataVirtualDataSourceMetadataType _metadataType;
    public ODataVirtualDataSourceMetadataType getMetadataType() {
        return _metadataType;
    }
    public void setMetadataType(ODataVirtualDataSourceMetadataType value) {
        _metadataType = value;
    }

    private SortDescriptionCollection _sortDescriptions;
    public SortDescriptionCollection getSortDescriptions() {
        return _sortDescriptions;
    }
    public void setSortDescriptions(SortDescriptionCollection value) {
        _sortDescriptions = value;
    }

    private String[] _desiredProperties;
    public String[] getDesiredProperties() {
        return _desiredProperties;
    }
    public void setDesiredProperties(String[] value) {
        _desiredProperties = value;
    }


    private FilterExpressionCollection _filterExpressions;
    public FilterExpressionCollection getFilterExpressions() {
        return _filterExpressions;
    }
    public void setFilterExpressions(FilterExpressionCollection value) {
        _filterExpressions = value;
    }
}
