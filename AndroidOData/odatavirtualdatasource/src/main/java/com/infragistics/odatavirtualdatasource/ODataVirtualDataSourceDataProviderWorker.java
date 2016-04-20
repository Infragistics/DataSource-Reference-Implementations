package com.infragistics.odatavirtualdatasource;

import android.util.Log;
import com.infragistics.controls.*;
import org.apache.http.HttpResponse;
import org.apache.http.HttpVersion;
import org.apache.http.client.HttpClient;
import org.apache.http.client.methods.HttpGet;
import org.apache.http.client.methods.HttpUriRequest;
import org.apache.http.impl.client.DefaultHttpClient;
import org.apache.http.params.CoreProtocolPNames;
import org.apache.http.params.HttpConnectionParams;
import org.apache.olingo.client.api.ODataClient;
import org.apache.olingo.client.api.communication.request.retrieve.*;
import org.apache.olingo.client.api.communication.response.ODataRetrieveResponse;
import org.apache.olingo.client.api.domain.ClientEntity;
import org.apache.olingo.client.api.domain.ClientEntitySet;
import org.apache.olingo.client.api.domain.ClientPrimitiveValue;
import org.apache.olingo.client.api.domain.ClientServiceDocument;
import org.apache.olingo.client.api.edm.xml.XMLMetadata;
import org.apache.olingo.client.api.uri.URIBuilder;
import org.apache.olingo.client.api.uri.URIFilter;
import org.apache.olingo.client.core.ODataClientFactory;
import org.apache.olingo.client.core.http.DefaultHttpUriRequestFactory;
import org.apache.olingo.commons.api.edm.Edm;
import org.apache.olingo.commons.api.edm.EdmSchema;
import org.apache.olingo.commons.api.edm.provider.CsdlSchema;
import org.apache.olingo.commons.api.format.ContentType;
import org.apache.olingo.commons.api.http.HttpMethod;

import java.io.BufferedReader;
import java.io.IOException;
import java.io.InputStreamReader;
import java.io.StringReader;
import java.lang.ref.WeakReference;
import java.net.URI;
import java.util.ArrayList;
import java.util.LinkedList;
import java.util.List;
import java.util.concurrent.*;

public class ODataVirtualDataSourceDataProviderWorker
    extends AsyncVirtualDataSourceProviderWorker {

    private final ODataVirtualDataSourceMetadataType _metadataType;
    private final SortDescriptionCollection _sortDescriptions;
    private final FilterExpressionCollection _filterExpressions;
    private final String[] _desiredProperties;
    private ODataClient _client;
    private String _baseUri;
    private String _entitySet;
    private DataSourceSchema _actualSchema = null;
    private Thread _thread;

    public ODataVirtualDataSourceDataProviderWorker(ODataVirtualDataSourceDataProviderWorkerSettings settings)
    {
        super(settings);
        _baseUri = settings.getBaseUri();
        _entitySet = settings.getEntitySet();
        _metadataType = settings.getMetadataType();
        _sortDescriptions = settings.getSortDescriptions();
        _filterExpressions = settings.getFilterExpressions();
        _desiredProperties = settings.getDesiredProperties();


        Thread thread = new Thread(new Runnable() {
            @Override
            public void run() {
                doWork();
            }
        });
        thread.setPriority(Thread.MIN_PRIORITY);
        _thread = thread;
        thread.start();
    }

    class LowPriorityThreadFactory implements ThreadFactory {
        public Thread newThread(Runnable r) {
            Thread t = new Thread(r);
            t.setPriority(Thread.MIN_PRIORITY);
            return t;
        }
    }

    private class ParametersHttpUriRequestFactory extends DefaultHttpUriRequestFactory {

        @Override
        public HttpUriRequest create(final HttpMethod method, final URI uri) {
            final HttpUriRequest request = super.create(method, uri);

            final int timeout = 8000;
            HttpConnectionParams.setConnectionTimeout(request.getParams(), timeout);
            HttpConnectionParams.setSoTimeout(request.getParams(), timeout);

            return request;
        }

    }

    @Override
    protected void initialize() {
        super.initialize();

        synchronized(syncLock) {
            _client = ODataClientFactory.getClient();
            _client.getConfiguration().setDefaultPubFormat(ContentType.JSON);
            if (_metadataType == ODataVirtualDataSourceMetadataType.FULL) {
                _client.getConfiguration().setDefaultPubFormat(ContentType.JSON_FULL_METADATA);
            }
            _client.getConfiguration().setHttpUriRequestFactory(new ParametersHttpUriRequestFactory());
            _client.getConfiguration().setExecutor(Executors.newFixedThreadPool(10, new LowPriorityThreadFactory()));
        }
    }

    @Override
    protected void processCompletedTask(AsyncDataSourcePageTaskHolder taskHolder, int currentDelay, int pageIndex, AsyncVirtualDataSourceProviderTaskDataHolder taskDataHolder)
    {
        Future task = (Future)taskHolder.getTask();
        DataSourceSchema schema = null;
        ClientEntitySet result = null;
        int schemaFetchCount = -1;

        try
        {
            if (pageIndex == SCHEMA_REQUEST_INDEX) {
                Future<Integer> scalarTask = (Future<Integer>)task;
                schemaFetchCount = scalarTask.get();
            } else {
                Future<ClientEntitySet> entityTask = (Future<ClientEntitySet>)task;
                result = entityTask.get();
            }
        } catch (InterruptedException e) {
            Log.i("ODataWorker", "task interrupted");

            retryIndex(pageIndex, currentDelay);
            //TODO: other exceptions? Is there a way to skip this state for canceled stuff?
            return;
        } catch (ExecutionException e) {
            Log.i("ODataWorker", "task execution error");

            retryIndex(pageIndex, currentDelay);
            e.printStackTrace();
            //TODO: other exceptions? Is there a way to skip this state for canceled stuff?
            return;
        }
        synchronized (syncLock) {
            if (schemaFetchCount >= 0) {
                setActualCount(schemaFetchCount);
            } else {
                if (result.getCount() != null) {
                    setActualCount((int) result.getCount());
                }
            }

            schema = _actualSchema;
        }

        if (schema == null)
        {
            schema = resolveSchema(_client);
        }

        synchronized (syncLock) {
            _actualSchema = schema;
            if (_actualSchema == null) {
                retryIndex(pageIndex, currentDelay);
            }
        }

        final ODataDataSourcePage page = new ODataDataSourcePage(result, _actualSchema, pageIndex);
        if (result != null) {
            synchronized (syncLock) {
                if (!isLastPage(pageIndex) && page.count() > 0 && !getPopulatedActualPageSize()) {
                    setPopulatedActualPageSize(true);
                    setActualPageSize(page.count());
                }
            }
        }

        if (getPageLoaded() != null)
        {
            if (getExecutionContext() != null)
            {
                final DataSourceExecutionContext context = getExecutionContext();
                final DataSourcePageLoadedCallback pageLoaded = getPageLoaded();
                if (context == null || pageLoaded == null) {
                    shutdown();
                    return;
                }
                context.execute(new DataSourceExecutionContextExecuteCallback() {
                    @Override
                    public void invoke() {
                        pageLoaded.invoke(page, getActualCount(), getActualPageSize());
                    }
                });
            }
            else
            {
                final DataSourcePageLoadedCallback pageLoaded = getPageLoaded();
                if (pageLoaded == null) {
                    shutdown();
                    return;
                }
                pageLoaded.invoke(page, getActualCount(), getActualPageSize());
            }
        }
    }

    private DataSourceSchema resolveSchema(ODataClient client)
    {
        HttpClient httpclient = new DefaultHttpClient();

        HttpGet request = new HttpGet();
        URI website = client.newURIBuilder(_baseUri).appendMetadataSegment().build();
        request.setURI(website);
        HttpResponse response = null;
        try {
            response = httpclient.execute(request);
        } catch (IOException e) {
            e.printStackTrace();
            return null;
        }
        try {
            BufferedReader reader = new BufferedReader(new InputStreamReader(
                    response.getEntity().getContent()));
            StringBuilder str = new StringBuilder();
            String line = null;
            while ((line = reader.readLine()) != null) {
                str.append(line);
            }
            ODataSchemaProvider provider = new ODataSchemaProvider(str.toString());
            return provider.getODataDataSourceSchema(_entitySet);
        } catch (IOException e) {
            e.printStackTrace();
            return null;
        }

    }

    private String _orderByString = null;
    private String _filterString = null;
    public static final int SCHEMA_REQUEST_INDEX = -1;

    @Override
    protected void makeTaskForRequest(final AsyncDataSourcePageRequest request, int retryDelay) {
        int actualPageSize = 0;
        synchronized (syncLock) {
            actualPageSize = getActualPageSize();
        }

        URIBuilder uriBuilder = _client.newURIBuilder(_baseUri).appendEntitySetSegment(_entitySet);

        synchronized (syncLock) {
            if (_filterExpressions != null &&
                    _filterExpressions.size() > 0 &&
                    _filterString == null)
            {
                StringBuilder sb = new StringBuilder();
                boolean first = true;
                for (int i = 0; i < _filterExpressions.size(); i++)
                {
                    FilterExpression expr = _filterExpressions.get(i);
                    if (first)
                    {
                        first = false;
                    }
                    else
                    {
                        sb.append(" AND ");
                    }

                    ODataDataSourceFilterExpressionVisitor visitor = new ODataDataSourceFilterExpressionVisitor();

                    visitor.visit(expr);

                    String txt = visitor.toString();
                    if (_filterExpressions.size() > 1)
                    {
                        txt = "(" + txt + ")";
                    }
                    sb.append(txt);
                }
                _filterString = sb.toString();
            }

            if (_filterString != null)
            {
                uriBuilder = uriBuilder.filter(_filterString);
            }

            if (_sortDescriptions.size() > 0) {
                if (_orderByString == null) {
                    _orderByString = "";
                    boolean first = true;
                    for (int i = 0; i < _sortDescriptions.size(); i++) {
                        if (first) {
                            first = false;
                        } else {
                            _orderByString += ",";
                        }
                        _orderByString += _sortDescriptions.get(i).getPropertyName();
                        if (_sortDescriptions.get(i).getDirection() == SortDirection.DESCENDING) {
                            _orderByString += " desc";
                        }
                    }
                }
                uriBuilder = uriBuilder.orderBy(_orderByString);
            }

            if (_desiredProperties != null && _desiredProperties.length > 0) {
                uriBuilder = uriBuilder.select(_desiredProperties);
            }
        }

        //uriBuilder = uriBuilder.filter(getFilter(_filterExpressions));

        URI uri = null;
        if (request.getIndex() == SCHEMA_REQUEST_INDEX) {
            uri = uriBuilder.count().build();
        } else {
            uri = uriBuilder.skip(request.getIndex() * actualPageSize)
                    .top(actualPageSize)
                    .count(true)
                    .build();
        }

        Future t = null;
        if (request.getIndex() == SCHEMA_REQUEST_INDEX) {
            final ODataValueRequest req = _client.getRetrieveRequestFactory().getValueRequest(uri);

            Future<Integer> task = _client.getConfiguration().getExecutor().submit(new Callable<Integer>() {
                @Override
                public Integer call() throws Exception {
                    ODataRetrieveResponse<ClientPrimitiveValue> res = req.execute();

                    ClientPrimitiveValue body = res.getBody();

                    synchronized (syncLock) {
                        request.setIsDone(true);
                        signalChangesOcurred();

                        Object bodyValue = body.toValue();
                        if (bodyValue instanceof String) {
                            bodyValue = Integer.parseInt((String)bodyValue);
                        }
                        Integer value =  (Integer)bodyValue;
                        return value;
                    }
                }
            });
            t = task;
        } else {
            final ODataEntitySetRequest<ClientEntitySet> req = _client.getRetrieveRequestFactory().getEntitySetRequest(uri);

            Future<ClientEntitySet> task = _client.getConfiguration().getExecutor().submit(new Callable<ClientEntitySet>() {
                @Override
                public ClientEntitySet call() throws Exception {
                    ODataRetrieveResponse<ClientEntitySet> res = req.execute();

                    ClientEntitySet body = res.getBody();

                    synchronized (syncLock) {
                        request.setIsDone(true);
                        signalChangesOcurred();

                        return body;
                    }
                }
            });
            t = task;
        }
        AsyncDataSourcePageTaskHolder taskHolder = new AsyncDataSourcePageTaskHolder();
        taskHolder.setTask(t);
        request.setTaskHolder(taskHolder);

        getTasks().add(request);
    }
}
