using Microsoft.EntityFrameworkCore;

namespace SaifRestaurantApi.Data;

public class PostgresAppDBContext(DbContextOptions<PostgresAppDBContext> options)
    : AppDBContext(options)
{
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<SaifRestaurantApi.Models.Reservation>()
            .Property(reservation => reservation.Date)
            .HasColumnType("date");
    }
}
