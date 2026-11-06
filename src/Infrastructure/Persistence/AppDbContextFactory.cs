namespace GastronomiaMarketplace.Infrastructure.Persistence;

using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.DependencyInjection;

public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var services = new ServiceCollection();
        services.AddDataProtection().SetApplicationName("GastronomiaMarketplace");
        var provider = services.BuildServiceProvider();
        var dpProvider = provider.GetService<IDataProtectionProvider>();
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Port=5433;Database=gastronomia;Username=postgres;Password=postgres", o => o.UseNetTopologySuite());
        return new AppDbContext(optionsBuilder.Options, dpProvider);
    }
}
