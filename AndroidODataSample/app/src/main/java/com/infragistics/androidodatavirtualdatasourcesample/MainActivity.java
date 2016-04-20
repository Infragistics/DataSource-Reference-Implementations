package com.infragistics.androidodatavirtualdatasourcesample;

import android.support.v7.app.AppCompatActivity;
import android.os.Bundle;
import android.view.View;
import android.widget.Button;
import android.widget.LinearLayout;

import com.infragistics.controls.DataGridView;
import com.infragistics.controls.GridSelectionMode;
import com.infragistics.controls.NumericColumn;
import com.infragistics.controls.SortDescription;
import com.infragistics.controls.SortDirection;
import com.infragistics.controls.TextColumn;
import com.infragistics.odatavirtualdatasource.ODataVirtualDataSource;
import com.infragistics.odatavirtualdatasource.ODataVirtualDataSourceMetadataType;


public class MainActivity extends AppCompatActivity {

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);

        final DataGridView grid = new DataGridView(this);

        Button button = new Button(this);
        button.setText("Scroll to Row 500");
        button.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {
                grid.scrollToRowByIndex(500);
            }
        });
        grid.setAutoGenerateColumns(false);
        LinearLayout.LayoutParams buttonParams = new LinearLayout.LayoutParams(LinearLayout.LayoutParams.WRAP_CONTENT, LinearLayout.LayoutParams.WRAP_CONTENT);
        button.setLayoutParams(buttonParams);

        LinearLayout lin = new LinearLayout(this);
        lin.setOrientation(LinearLayout.VERTICAL);

        TextColumn customerId = new TextColumn();
        customerId.setKey("CustomerID");
        customerId.setPaddingLeft(20);
        customerId.getHeader().setPaddingLeft(20);

        customerId.setTitle("Customer ID");
        TextColumn shipName = new TextColumn();
        shipName.setKey("ShipName");
        shipName.setTitle("Ship Name");
        NumericColumn orderId = new NumericColumn();
        orderId.setKey("OrderID");
        orderId.setTitle("Order ID");
        TextColumn shipCountry = new TextColumn();
        shipCountry.setKey("ShipCountry");
        shipCountry.setTitle("Ship Country");

        grid.addColumn(customerId);
        grid.addColumn(shipName);
        grid.addColumn(orderId);
        grid.addColumn(shipCountry);
        grid.setRowHeight(50);

        final ODataVirtualDataSource virtualDataSource = new ODataVirtualDataSource(this);
        virtualDataSource.setMetadataType(ODataVirtualDataSourceMetadataType.MINIMAL);
        virtualDataSource.setBaseURI("http://services.odata.org/V4/Northwind/Northwind.svc");
        virtualDataSource.setEntitySet("Orders");
        virtualDataSource.setPageSizeRequested(50);
        virtualDataSource.setMaxCachedPages(5);

        grid.setDataSource(virtualDataSource);

        grid.setSelectionMode(GridSelectionMode.MULTIPLE_ROW);

        virtualDataSource.getSortDescriptions()
                .add(new SortDescription("ShipName", SortDirection.ASCENDING));


        lin.addView(button);
        lin.addView(grid);



        setContentView(lin);

    }
}
