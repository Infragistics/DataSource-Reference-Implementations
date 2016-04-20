package com.infragistics.odatavirtualdatasource;

import com.infragistics.controls.DataSourcePage;
import com.infragistics.controls.DataSourceSchema;
import org.apache.olingo.client.api.domain.ClientEntity;
import org.apache.olingo.client.api.domain.ClientEntitySet;
import org.apache.olingo.client.api.domain.ClientProperty;
import org.apache.olingo.client.api.domain.ClientValue;

import java.util.Calendar;
import java.util.List;

public class ODataDataSourcePage
    implements DataSourcePage {

    List<ClientEntity> _actualData;
    private DataSourceSchema _schema;
    private int _pageIndex;

    public ODataDataSourcePage(ClientEntitySet sourceData, DataSourceSchema schema, int pageIndex) {
        _pageIndex = pageIndex;
        _schema = schema;
        if (sourceData == null) {
            _actualData = null;
        }
        else {
            _actualData = sourceData.getEntities();
        }
    }

    @Override
    public int count() {
        return _actualData.size();
    }

    @Override
    public Object getItemAtIndex(int i) {
        return _actualData.get(i);
    }

     @Override
    public Object getItemValueAtIndex(int i, String s) {
        ClientProperty property = _actualData.get(i).getProperty(s);
        if (property == null) {
            return null;
        }

        ClientValue value = property.getValue();
        if (!value.isPrimitive()) {
            return value;
        }
        Object val = value.asPrimitive().toValue();

        if (val instanceof java.sql.Timestamp) {
            Calendar c = Calendar.getInstance();
            c.setTimeInMillis(((java.sql.Timestamp) val).getTime());
            return c;
        }

        return val;
    }

    @Override
    public int pageIndex() {
        return _pageIndex;
    }

    @Override
    public DataSourceSchema schema() {
        return _schema;
    }
}
