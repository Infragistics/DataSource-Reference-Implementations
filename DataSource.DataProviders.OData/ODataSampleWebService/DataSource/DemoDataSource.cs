using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ODataSampleWebService.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Hosting;

namespace ODataSampleWebService.DataSource
{
    public class DemoDataSources
    {
        private Random _random = new Random();

        private static DemoDataSources instance = null;
        public static DemoDataSources Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new DemoDataSources();
                }
                return instance;
            }
        }
        public List<Sale> Sales { get; set; }
        public List<Order> Orders { get; set; }

        private DemoDataSources()
        {
            InitializeSalesData();

            InitializeOrderData();
        }

        public void InitializeSalesData()
        {
            Sales = new List<Sale>();

            string[] names = new string[] {
                "Intel CPU", "AMD CPU",
                "Intel Motherboard", "AMD Motherboard", "Nvidia Motherboard",
                "Nvidia GPU", "Gigabyte GPU", "Asus GPU", "AMD GPU", "MSI GPU",
                "Corsair Memory", "Patriot Memory", "Skill Memory",
                "Samsung HDD", "WD HDD", "Seagate HDD", "Intel HDD", "Asus HDD",
                "Samsung SSD", "WD SSD", "Seagate SSD", "Intel SSD", "Asus SSD",
                "Samsung Monitor", "Asus Monitor", "LG Monitor", "HP Monitor"
            };

            string[] countries = new string[] { "USA", "UK", "France", "Canada", "Poland",
                "Denmark", "Croatia", "Australia", "Seychelles",
                "Sweden", "Germany", "Japan", "Ireland",
                "Barbados", "Jamaica", "Cuba", "Spain"
            };

            string[] status = new string[] { "Packing", "Shipped", "Delivered" };

            for (var i = 0; i < 1000; i++)
            {
                var countryIndex = (int)Math.Round(GetRandomNumber(0, countries.Length - 1));
                var nameIndex = (int)Math.Round(GetRandomNumber(0, names.Length - 1));
                var statusIndex = (int)Math.Round(GetRandomNumber(0, status.Length - 1));

                var sale = new Sale();
                sale.ProductID = 1001 + i;
                sale.ProductPrice = GetRandomNumber(10000, 90000) / 100;
                sale.ProductName = names[nameIndex];
                sale.Country = countries[countryIndex];
                sale.Margin = (int)GetRandomNumber(2, 5);
                sale.OrderDate = GetRandomDate();
                sale.OrderItems = (int)GetRandomNumber(4, 30);
                sale.OrderValue = Math.Round(sale.ProductPrice * sale.OrderItems);
                sale.Profit = Math.Round((sale.ProductPrice * sale.Margin / 100) * sale.OrderItems);
                sale.Status = status[statusIndex];

                Sales.Add(sale);
            }
        }

        public void InitializeOrderData()
        {
            Orders = new List<Order>();

            var nortwindPath = HostingEnvironment.MapPath("~/App_Data/northwind.json");

            // read JSON directly from a file
            using (StreamReader file = File.OpenText(nortwindPath))
            {
                using (JsonTextReader reader = new JsonTextReader(file))
                {
                    JArray data = (JArray)JToken.ReadFrom(reader);

                    foreach (var item in data)
                    {
                        var order = JsonConvert.DeserializeObject<Order>(item.ToString());
                        Orders.Add(order);
                    }
                }
            }
        }

        private double GetRandomNumber(double min, double max)
        {
            return Math.Round(min + _random.NextDouble() * (max - min));
        }

        private DateTime GetRandomDate()
        {
            var month = (int)GetRandomNumber(1, 12);
            var day = (int)GetRandomNumber(1, 27);
            var year = (int)GetRandomNumber(2000, 2019);
            return new DateTime(year, month, day);
        }
    }
}