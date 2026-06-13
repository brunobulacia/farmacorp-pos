using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace FarmacorpPOS.Infrastructure;

public class FarmacorpDbContextFactory : IDesignTimeDbContextFactory<FarmacorpDbContext>
{
    public FarmacorpDbContext CreateDbContext(string[] args)
    {
        var consolePath = Path.GetFullPath(
            Path.Combine(Directory.GetCurrentDirectory(), "..", "FarmacorpPOS.Console"));

        var config = new ConfigurationBuilder()
            .SetBasePath(consolePath)
            .AddJsonFile("appsettings.json", optional: true)
            .Build();

        var connectionString = config.GetConnectionString("DefaultConnection")
            ?? "Server=localhost,1433;Database=FarmacorpPOS;User Id=sa;Password=Farmacorp123!;TrustServerCertificate=True";

        var options = new DbContextOptionsBuilder<FarmacorpDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new FarmacorpDbContext(options);
    }
}
