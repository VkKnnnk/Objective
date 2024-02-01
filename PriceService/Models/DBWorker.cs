using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PriceService.Models
{
    public static class DBWorker
    {
        public static async Task<bool> CheckDBConnection()
        {
            using (PrinzipDBContext db = new PrinzipDBContext())
            {
                return await db.Database.CanConnectAsync();
            }
        }
        public static async Task<List<Apartment>> GetApartments()
        {
            using (PrinzipDBContext db = new PrinzipDBContext())
            {
                return await db.Apartments.ToListAsync();
            }
        }
        public static async Task AddNewApartment(Apartment apartment)
        {
            using (PrinzipDBContext db = new PrinzipDBContext())
            {
                await db.Apartments.AddAsync(apartment);
                await db.SaveChangesAsync();
            }
        }
        public static async Task UpdateMonitoringApartment(Apartment apartment, bool status)
        {
            using (PrinzipDBContext db = new PrinzipDBContext())
            {
                db.Apartments.Attach(apartment);
                apartment.IsMonitorng = status;
                await db.SaveChangesAsync();
            }
        }
        public static async Task UpdatePriceApartment(Apartment apartment, double price)
        {
            using (PrinzipDBContext db = new PrinzipDBContext())
            {
                db.Apartments.Attach(apartment);
                apartment.Price = price;
                await db.SaveChangesAsync();
            }
        }
        public static async Task UpdatePriceMortgageMonthlyApartment(Apartment apartment, double? priceMortgageMonthly)
        {
            using (PrinzipDBContext db = new PrinzipDBContext())
            {
                db.Apartments.Attach(apartment);
                apartment.PriceMortgageMonthly = priceMortgageMonthly;
                await db.SaveChangesAsync();
            }
        }
    }
}
