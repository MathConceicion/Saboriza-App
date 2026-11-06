namespace GastronomiaMarketplace.Infrastructure.Identity;

using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using GastronomiaMarketplace.Application.Common.Interfaces;

public sealed class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _http;

    public CurrentUser(IHttpContextAccessor http) => _http = http;

    private ClaimsPrincipal? User => _http.HttpContext?.User;

    public Guid? UserId => Guid.TryParse(User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? User?.FindFirstValue("sub"), out var g) ? g : null;
    public string? Email => User?.FindFirstValue(ClaimTypes.Email) ?? User?.FindFirstValue("email");
    public Guid? VendorId => Guid.TryParse(User?.FindFirstValue("vendor_id"), out var g) ? g : null;
    public Guid? LojaId => Guid.TryParse(User?.FindFirstValue("loja_id"), out var g) ? g : null;
    public bool IsAdmin => User?.IsInRole("Admin") == true || User?.FindFirstValue("is_admin") == "true";
    public bool IsVendedor => User?.IsInRole("Vendedor") == true;
    public bool IsAuthenticated => User?.Identity?.IsAuthenticated == true;
    public IReadOnlyList<string> Roles => User?.FindAll("role").Select(c => c.Value).ToList() ?? [];
}
