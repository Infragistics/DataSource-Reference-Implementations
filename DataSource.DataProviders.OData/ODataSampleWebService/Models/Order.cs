﻿using System;
using System.ComponentModel.DataAnnotations;

namespace ODataSampleWebService.Models
{
    public class Order
    {
        [Key]
        public int OrderID { get; set; }
        public string CustomerID { get; set; }
        public int EmployeeID { get; set; }
        public double Freight { get; set; }
        public DateTime OrderDate { get; set; }
        public DateTime RequiredDate { get; set; }
        public string ShipAddress { get; set; }
        public string ShipCity { get; set; }
        public string ShipCountry { get; set; }
        public string ShipName { get; set; }
        public string ShipPostalCode { get; set; }
        public string ShipRegion { get; set; }
        public int ShipVia { get; set; }
        public DateTime ShippedDate { get; set; }
    }
}