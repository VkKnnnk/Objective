namespace PriceService.Models
{
    public class Apartment
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public float Price { get; set; }
        public float? PriceMortgageMonthly { get; set; }
        public string Url { get; set; }
    }
}
