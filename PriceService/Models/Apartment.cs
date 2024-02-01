using System;
using System.Collections.Generic;

#nullable disable

namespace PriceService.Models
{
    public partial class Apartment
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public double Price { get; set; }
        public double? PriceMortgageMonthly { get; set; }
        public string Url { get; set; }
        public bool? IsMonitorng { get; set; }
    }
}
