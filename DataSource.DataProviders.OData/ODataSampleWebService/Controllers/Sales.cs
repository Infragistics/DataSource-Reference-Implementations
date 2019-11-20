using Microsoft.AspNet.OData;
using Microsoft.AspNet.OData.Query;
using ODataSampleWebService.DataSource;
using ODataSampleWebService.Models;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Http;

namespace ODataSampleWebService.Controllers
{
    public class SalesController : ODataController
    {
        [EnableQuery]
        public IHttpActionResult Get()
        {
            var results = DemoDataSources.Instance.Sales.AsQueryable();

            return Ok(results);
        }

        // GET odata/Sales('key')
        [EnableQuery]
        public IHttpActionResult Get([FromODataUri]int key)
        {
            IEnumerable<Sale> filteredSales = DemoDataSources.Instance.Sales.Where(item => item.ProductID == key);

            if (filteredSales.Count() == 0)
            {
                return NotFound();
            }
            
            return Ok(filteredSales.Single());
        }
    }
}