using System.Text;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using GastronomiaMarketplace.Application;
using GastronomiaMarketplace.Infrastructure;
using GastronomiaMarketplace.Infrastructure.Identity;
using GastronomiaMarketplace.WebAPI.Extensions;
using GastronomiaMarketplace.WebAPI.Middlewares;
using GastronomiaMarketplace.WebAPI.Policies;

var builder = WebApplication.CreateBuilder(args);
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
var jwtOpts = builder.Configuration.GetSection("Jwt").Get<JwtOptions>() ?? throw new InvalidOperationException("Configuração Jwt ausente.");
if (string.IsNullOrWhiteSpace(jwtOpts.SecretKey) || jwtOpts.SecretKey.Length < 32)
    throw new InvalidOperationException("Configuração Jwt:SecretKey ausente ou fraca. Defina via variável de ambiente Jwt__SecretKey com ao menos 32 caracteres.");
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Gastronomia Marketplace API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new()
    {
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Informe: Bearer {seu token JWT}"
    });
    c.AddSecurityRequirement(new()
    {
        {
            new() { Reference = new() { Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, Id = "Bearer" } },
            Array.Empty<string>()
        }
    });
});
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHttpClient("ViaCep", c => c.BaseAddress = new Uri("https://viacep.com.br/"));
builder.Services.AddScoped<GastronomiaMarketplace.WebAPI.Services.UploadImagemService>();
builder.Services.AddAuthentication(opts =>
{
    opts.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    opts.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    opts.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(opts =>
{
    opts.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
    opts.SaveToken = true;
    opts.MapInboundClaims = false;
    opts.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = jwtOpts.Issuer,
        ValidateAudience = true,
        ValidAudience = jwtOpts.Audience,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOpts.SecretKey)),
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromMinutes(2),
        NameClaimType = "sub",
        RoleClaimType = "role"
    };
    opts.Events = new JwtBearerEvents
    {
        OnMessageReceived = ctx =>
        {
            if (ctx.Request.Cookies.TryGetValue("__Host-access_token", out var token) || ctx.Request.Cookies.TryGetValue("access_token", out token))
                ctx.Token = token;
            return Task.CompletedTask;
        },
        OnAuthenticationFailed = ctx =>
        {
            ctx.Response.Headers.Append("WWW-Authenticate", $"Bearer error=\"invalid_token\", error_description=\"{ctx.Exception.Message}\"");
            return Task.CompletedTask;
        }
    };
});
builder.Services.AddAuthorization(opts =>
{
    opts.AddPolicy("Authenticated", p => p.RequireAuthenticatedUser());
    opts.AddPolicy("AdminOnly", p => p.RequireRole("Admin"));
    opts.AddPolicy("VendedorAprovado", p =>
    {
        p.RequireAuthenticatedUser();
        p.RequireRole("Vendedor");
        p.RequireClaim("vendor_id");
        p.RequireClaim("loja_status", "Aprovado");
    });
    opts.AddPolicy("VendorOwnerOrAdmin", p =>
    {
        p.RequireAuthenticatedUser();
        p.Requirements.Add(new VendorOwnerRequirement());
    });
    opts.AddPolicy("SelfOrAdmin", p => p.RequireAuthenticatedUser());
});
builder.Services.AddScoped<Microsoft.AspNetCore.Authorization.IAuthorizationHandler, VendorOwnerHandler>();
builder.Services.AddScoped<Microsoft.AspNetCore.Authorization.IAuthorizationHandler, AdminBypassHandler>();
builder.Services.AddRateLimitingPolicies();
builder.Services.AddCors(opts =>
{
    opts.AddPolicy("Frontend", p => p
        .WithOrigins(builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? ["http://localhost:5173"])
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials());
});
builder.Services.AddAntiforgery(opts =>
{
    opts.HeaderName = "X-CSRF-TOKEN";
    var isProdAntiforgery = !builder.Environment.IsDevelopment();
    opts.Cookie.Name = isProdAntiforgery ? "__Host-XSRF-TOKEN" : "XSRF-TOKEN";
    opts.Cookie.HttpOnly = false;
    opts.Cookie.SecurePolicy = isProdAntiforgery ? CookieSecurePolicy.Always : CookieSecurePolicy.None;
    opts.Cookie.SameSite = SameSiteMode.Strict;
});
builder.Services.AddHealthChecks().AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy());
var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseHttpsRedirection();
app.UseCors("Frontend");
var webRoot = builder.Environment.WebRootPath ?? Path.Combine(builder.Environment.ContentRootPath, "wwwroot");
Directory.CreateDirectory(Path.Combine(webRoot, "imagens"));
app.UseStaticFiles(new Microsoft.AspNetCore.Builder.StaticFileOptions { FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(webRoot) });
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();
app.MapControllers().RequireRateLimiting("default");
app.MapHealthChecks("/health");
await app.ApplyMigrationsAsync(builder.Configuration);
app.Run();
