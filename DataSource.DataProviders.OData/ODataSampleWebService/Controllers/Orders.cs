using Microsoft.AspNet.OData;
using ODataSampleWebService.DataSource;
using ODataSampleWebService.Models;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;

namespace ODataSampleWebService.Controllers
{
    public class OrdersController : ODataController
    {
        [EnableQuery]
        public IHttpActionResult Get()
        {
            var results = DemoDataSources.Instance.Orders.AsQueryable();

            return Ok(results);
        }

        // GET odata/Orders('key')
        [EnableQuery]
        public IHttpActionResult Get([FromODataUri]int key)
        {
            IEnumerable<Order> filteredOrders = DemoDataSources.Instance.Orders.Where(item => item.OrderID == key);

            if (filteredOrders.Count() == 0)
            {
                return NotFound();
            }

            return Ok(filteredOrders.Single());
        }
    }
}