using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SaifRestaurantApi.Models;

namespace SaifRestaurantApi.Data
{
    public class AppDBContext : IdentityDbContext<AppUser>
    {
        public AppDBContext(DbContextOptions<AppDBContext> options) : this((DbContextOptions)options)
        {
        }

        protected AppDBContext(DbContextOptions options) : base(options)
        {
        }
        public DbSet<OrderTicket> OrderTickets { get; set; }
        public DbSet<Reservation> Reservations { get; set; }
        public DbSet<OrderDish> OrderDishes { get; set; }
    }
}
