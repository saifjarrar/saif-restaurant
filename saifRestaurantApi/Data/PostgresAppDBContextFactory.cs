using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SaifRestaurantApi.Data;

public class PostgresAppDBContextFactory : IDesignTimeDbContextFactory<PostgresAppDBContext>
{
    public PostgresAppDBContext CreateDbContext(string[] args)
    {
        var connection = Environment.GetEnvironmentVariable("ConnectionStrings__Postgres")
            ?? throw new InvalidOperationException("Set ConnectionStrings__Postgres before running PostgreSQL EF commands.");

        var options = new DbContextOptionsBuilder<PostgresAppDBContext>()
            .UseNpgsql(connection)
            .Options;

        return new PostgresAppDBContext(options);
    }
}
