namespace GastronomiaMarketplace.WebAPI.Extensions;

using Microsoft.EntityFrameworkCore;
using GastronomiaMarketplace.Infrastructure.Persistence;

public static class MigrationExtensions
{
    public static async Task ApplyMigrationsAsync(this WebApplication app, IConfiguration configuration)
    {
        var aplicar = configuration.GetValue<bool>("ApplyMigrations");
        if (!aplicar) return;
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var strategy = db.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () => await db.Database.MigrateAsync());
    }
}
