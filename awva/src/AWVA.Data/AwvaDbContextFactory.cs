using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AWVA.Data;

/// <summary>
/// Used only by `dotnet ef migrations add` / `dotnet ef database update` at design time.
/// The running API configures the real connection string via appsettings/DI instead.
/// </summary>
public class AwvaDbContextFactory : IDesignTimeDbContextFactory<AwvaDbContext>
{
    public AwvaDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AwvaDbContext>();
        optionsBuilder.UseSqlServer(
            "Server=(localdb)\\mssqllocaldb;Database=AWVA;Trusted_Connection=True;TrustServerCertificate=True;");
        return new AwvaDbContext(optionsBuilder.Options);
    }
}
