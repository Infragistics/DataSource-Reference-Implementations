using System;
using System.ComponentModel.DataAnnotations;

namespace ODataSampleWebService.Models
{
    public class Sale
    {
        [Key]
        public int ProductID { get; set; }
        public string Country { get; set; }
        public double Margin { get; set; }
        public DateTime OrderDate { get; set; }
        public int OrderItems { get; set; }
        public double OrderValue { get; set; }
        public string ProductName { get; set; }
        public double ProductPrice { get; set; }
        public double Profit { get; set; }
        public string Status { get; set; }
    }
}