namespace GastronomiaMarketplace.Infrastructure;

using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using GastronomiaMarketplace.Application.Common.Interfaces;
using GastronomiaMarketplace.Infrastructure.Clock;
using GastronomiaMarketplace.Infrastructure.Geolocation;
using GastronomiaMarketplace.Infrastructure.Identity;
using GastronomiaMarketplace.Infrastructure.Persistence;
using GastronomiaMarketplace.Infrastructure.Persistence.Interceptors;
using GastronomiaMarketplace.Infrastructure.Security.Sanitization;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDataProtection()
            .PersistKeysToFileSystem(new DirectoryInfo(configuration["DataProtection:KeyPath"] ?? "./keys"))
            .SetApplicationName("GastronomiaMarketplace")
            .SetDefaultKeyLifetime(TimeSpan.FromDays(90));
        services.AddScoped<AuditableEntityInterceptor>();
        services.AddScoped<IHtmlSanitizerService, HtmlSanitizerService>();
        services.AddScoped<IGeocodingService, MockGeocodingService>();
        services.AddScoped<IViaCepService, ViaCepService>();
        services.AddSingleton<IClock, SystemClock>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUser>();
        services.AddDbContext<AppDbContext>((sp, opts) =>
        {
            var connStr = configuration.GetConnectionString("Default") ?? "Host=localhost;Port=5433;Database=gastronomia;Username=postgres;Password=postgres";
            opts.UseNpgsql(connStr, o => o.UseNetTopologySuite());
            opts.AddInterceptors(sp.GetRequiredService<AuditableEntityInterceptor>());
        });
        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());
        services.Configure<JwtOptions>(configuration.GetSection("Jwt"));
        return services;
    }
}
