=====================================================
Package notes for DataPresenter.DataSources.OData.dll
=====================================================

This package contains a reference implementation of an OData data source that is built on the AsyncPagingDataSourceBase class delivered in InfragisticsWPF4.DataPresenter.DataSources.Async.v16.1 (shipped as part of Infragistics NetAdvantage 16.1)  

This reference implementation also depends on the Simple.OData.Client NuGet package (not authored by Infragistics), and has been tested with v4.13.0 of that package.

When installed into a Visual Studio WPF project this package will add project references to the following Infragistics assemblies which are assumed to be installed in the local machine's GAC as the result of Trial or licensed installation of Infragistics NetAdvantage 16.1 (they are NOT included in this package):

- InfragisticsWPF4.DataPresenter.DataSources.Async.v16.1
- InfragisticsWPF4.DataVisualization.v16.1
- InfragisticsWPF4.v16.1

When installed into a Visual Studio WPF project, this package will also download the Simple.OData.Client package and add a reference to that package's primary assembly:
- Simple.OData.Client.Net40

as well as a number of Microsoft-authored packages on which it depends.

If you would like to explore a sample project that shows the OData data source being used with the XamDataPresenter you can download a sample project here: 
https://github.com/Infragistics/DataSource-Reference-Implementations/tree/master/DataPresenter.DataSources.OData/DataPresenter.DataSources.OData.SampleApp 

-END-
