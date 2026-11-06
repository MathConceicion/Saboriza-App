namespace GastronomiaMarketplace.WebAPI.Controllers;

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using GastronomiaMarketplace.Application.Common.Interfaces;
using GastronomiaMarketplace.Domain.Enums;
using GastronomiaMarketplace.Infrastructure.Identity;
using GastronomiaMarketplace.Infrastructure.Persistence;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ITokenService _tokenService;
    private readonly JwtOptions _jwtOpts;
    private readonly IWebHostEnvironment _env;
    private readonly ICurrentUser _currentUser;

    public AuthController(AppDbContext db, ITokenService tokenService, IOptions<JwtOptions> jwtOpts, IWebHostEnvironment env, ICurrentUser currentUser)
    {
        _db = db;
        _tokenService = tokenService;
        _jwtOpts = jwtOpts.Value;
        _env = env;
        _currentUser = currentUser;
    }

    private void SetAuthCookies(string accessToken, string refreshToken)
    {
        var isProd = !_env.IsDevelopment();
        var accessName = isProd ? "__Host-access_token" : "access_token";
        var refreshName = isProd ? "__Host-refresh_token" : "refresh_token";
        Response.Cookies.Append(accessName, accessToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = isProd,
            SameSite = SameSiteMode.Strict,
            Path = "/",
            MaxAge = TimeSpan.FromMinutes(_jwtOpts.ExpiracaoMinutos)
        });
        Response.Cookies.Append(refreshName, refreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = isProd,
            SameSite = SameSiteMode.Strict,
            Path = "/api/auth",
            MaxAge = TimeSpan.FromDays(_jwtOpts.RefreshExpiracaoDias)
        });
    }

    [HttpPost("registro")]
    [AllowAnonymous]
    [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("registro")]
    public async Task<IActionResult> Registrar([FromBody] RegistrarDto dto, CancellationToken ct)
    {
        if (await _db.Usuarios.AnyAsync(u => u.Email == dto.Email.ToLower(), ct))
            return BadRequest(new { error = "Email já cadastrado." });
        if (!string.IsNullOrWhiteSpace(dto.Cpf) && await _db.Usuarios.AnyAsync(u => u.Cpf == dto.Cpf, ct))
            return BadRequest(new { error = "CPF já cadastrado." });
        var hash = PasswordHasher.Hash(dto.Senha);
        var usuario = new Domain.Entities.Usuario(dto.Nome, dto.Email, hash, dto.Cpf);
        _db.Usuarios.Add(usuario);
        await _db.SaveChangesAsync(ct);
        var tokenDto = new ITokenService.UsuarioTokenDto(usuario.Id, usuario.Email, usuario.NomeCompleto, usuario.Papel, false, null, null, null);
        var accessToken = _tokenService.GerarToken(tokenDto);
        var refreshToken = _tokenService.GerarRefreshToken(usuario.Id);
        SetAuthCookies(accessToken, refreshToken);
        return Ok(new { user = new { id = usuario.Id, email = usuario.Email, nome = usuario.NomeCompleto, roles = new[] { "Cliente" }, vendorId = (Guid?)null, lojaId = (Guid?)null, isAdmin = false } });
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto, CancellationToken ct)
    {
        var usuario = await _db.Usuarios.Include(u => u.Vendedor).ThenInclude(v => v!.Loja).FirstOrDefaultAsync(u => u.Email == dto.Email.ToLower(), ct);
        if (usuario is null || !PasswordHasher.Verify(dto.Senha, usuario.SenhaHash))
            return Unauthorized(new { error = "Credenciais inválidas." });
        var vendorId = usuario.Vendedor?.Status == StatusVendedor.Aprovado ? usuario.Vendedor.Id : (Guid?)null;
        var lojaId = usuario.Vendedor?.Loja?.Id;
        var lojaStatus = usuario.Vendedor?.Status.ToString();
        var tokenDto = new ITokenService.UsuarioTokenDto(usuario.Id, usuario.Email, usuario.NomeCompleto, usuario.Papel, usuario.Papel == PapelUsuario.Admin, vendorId, lojaId, lojaStatus);
        var accessToken = _tokenService.GerarToken(tokenDto);
        var refreshToken = _tokenService.GerarRefreshToken(usuario.Id);
        SetAuthCookies(accessToken, refreshToken);
        var roles = new List<string> { "Cliente" };
        if (usuario.Papel == PapelUsuario.Vendedor) roles.Add("Vendedor");
        if (usuario.Papel == PapelUsuario.Admin) roles.Add("Admin");
        return Ok(new { user = new { id = usuario.Id, email = usuario.Email, nome = usuario.NomeCompleto, roles, vendorId, lojaId, isAdmin = usuario.Papel == PapelUsuario.Admin } });
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("refresh")]
    public async Task<IActionResult> Refresh(CancellationToken ct)
    {
        var refreshToken = Request.Cookies["__Host-refresh_token"] ?? Request.Cookies["refresh_token"];
        if (string.IsNullOrEmpty(refreshToken)) return Unauthorized(new { error = "Refresh token ausente." });
        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        System.Security.Claims.ClaimsPrincipal? principal = null;
        try
        {
            var key = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(_jwtOpts.SecretKey));
            principal = handler.ValidateToken(refreshToken, new Microsoft.IdentityModel.Tokens.TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = _jwtOpts.Issuer,
                ValidateAudience = true,
                ValidAudience = _jwtOpts.Audience,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(2)
            }, out _);
        }
        catch { return Unauthorized(new { error = "Refresh token inválido." }); }
        var userIdStr = principal.FindFirst("sub")?.Value ?? principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? principal.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;
        if (!Guid.TryParse(userIdStr, out var userId)) return Unauthorized(new { error = "Refresh token inválido." });
        var usuario = await _db.Usuarios.Include(u => u.Vendedor).ThenInclude(v => v!.Loja).FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (usuario is null) return Unauthorized();
        var vendorId2 = usuario.Vendedor?.Status == StatusVendedor.Aprovado ? usuario.Vendedor.Id : (Guid?)null;
        var lojaId2 = usuario.Vendedor?.Loja?.Id;
        var lojaStatus2 = usuario.Vendedor?.Status.ToString();
        var tokenDto = new ITokenService.UsuarioTokenDto(usuario.Id, usuario.Email, usuario.NomeCompleto, usuario.Papel, usuario.Papel == PapelUsuario.Admin, vendorId2, lojaId2, lojaStatus2);
        var novoAccessToken = _tokenService.GerarToken(tokenDto);
        var novoRefreshToken = _tokenService.GerarRefreshToken(userId);
        SetAuthCookies(novoAccessToken, novoRefreshToken);
        var roles2 = new List<string> { "Cliente" };
        if (usuario.Papel == PapelUsuario.Vendedor) roles2.Add("Vendedor");
        if (usuario.Papel == PapelUsuario.Admin) roles2.Add("Admin");
        return Ok(new { user = new { id = usuario.Id, email = usuario.Email, nome = usuario.NomeCompleto, roles = roles2, vendorId = vendorId2, lojaId = lojaId2, isAdmin = usuario.Papel == PapelUsuario.Admin } });
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    public IActionResult Logout()
    {
        var isProd = !_env.IsDevelopment();
        var accessName = isProd ? "__Host-access_token" : "access_token";
        var refreshName = isProd ? "__Host-refresh_token" : "refresh_token";
        Response.Cookies.Delete(accessName);
        Response.Cookies.Delete(refreshName, new CookieOptions { Path = "/api/auth" });
        Response.Cookies.Delete("__Host-access_token");
        Response.Cookies.Delete("__Host-refresh_token", new CookieOptions { Path = "/api/auth" });
        Response.Cookies.Delete("access_token");
        Response.Cookies.Delete("refresh_token", new CookieOptions { Path = "/api/auth" });
        return NoContent();
    }

    [HttpGet("me")]
    [Authorize]
    public IActionResult Me()
    {
        var nome = User.FindFirstValue(System.Security.Claims.ClaimTypes.Name) ?? _currentUser.Email;
        return Ok(new { id = _currentUser.UserId, email = _currentUser.Email, nome, roles = _currentUser.Roles, vendorId = _currentUser.VendorId, lojaId = _currentUser.LojaId, isAdmin = _currentUser.IsAdmin, isVendedor = _currentUser.IsVendedor });
    }

    [HttpGet("antiforgery/token")]
    [AllowAnonymous]
    public IActionResult ObterTokenCsrf([FromServices] Microsoft.AspNetCore.Antiforgery.IAntiforgery antiforgery)
    {
        var tokens = antiforgery.GetAndStoreTokens(HttpContext);
        return Ok(new { token = tokens.RequestToken });
    }
}

public sealed record RegistrarDto(string Nome, string Email, string Senha, string? Cpf);
public sealed record LoginDto(string Email, string Senha);
