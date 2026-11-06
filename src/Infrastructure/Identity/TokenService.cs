namespace GastronomiaMarketplace.Infrastructure.Identity;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using GastronomiaMarketplace.Application.Common.Interfaces;
using GastronomiaMarketplace.Domain.Enums;

public sealed class JwtOptions
{
    public string Issuer { get; set; } = "gastronomia-marketplace";
    public string Audience { get; set; } = "gastronomia-marketplace-client";
    public string SecretKey { get; set; } = null!;
    public int ExpiracaoMinutos { get; set; } = 60;
    public int RefreshExpiracaoDias { get; set; } = 7;
}

public sealed class TokenService : ITokenService
{
    private readonly JwtOptions _opts;
    private readonly SymmetricSecurityKey _key;
    private readonly SigningCredentials _creds;

    public TokenService(IOptions<JwtOptions> opts)
    {
        _opts = opts.Value;
        _key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_opts.SecretKey));
        _creds = new SigningCredentials(_key, SecurityAlgorithms.HmacSha256);
    }

    public string GerarToken(ITokenService.UsuarioTokenDto usuario)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, usuario.UsuarioId.ToString()),
            new(JwtRegisteredClaimNames.Email, usuario.Email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.Name, usuario.NomeCompleto),
        };
        claims.Add(new("role", "Cliente"));
        if (usuario.Papel == PapelUsuario.Vendedor)
            claims.Add(new("role", "Vendedor"));
        if (usuario.Papel == PapelUsuario.Admin || usuario.IsAdmin)
        {
            claims.Add(new("role", "Admin"));
            claims.Add(new("is_admin", "true"));
        }
        if (usuario.VendorId.HasValue && usuario.LojaStatus == "Aprovado")
        {
            claims.Add(new("vendor_id", usuario.VendorId.Value.ToString()));
            if (usuario.LojaId.HasValue)
                claims.Add(new("loja_id", usuario.LojaId.Value.ToString()));
            claims.Add(new("loja_status", usuario.LojaStatus!));
        }
        var token = new JwtSecurityToken(
            issuer: _opts.Issuer,
            audience: _opts.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddMinutes(_opts.ExpiracaoMinutos),
            signingCredentials: _creds
        );
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GerarRefreshToken(Guid usuarioId)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, usuarioId.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("token_type", "refresh")
        };
        var token = new JwtSecurityToken(
            issuer: _opts.Issuer,
            audience: _opts.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddDays(_opts.RefreshExpiracaoDias),
            signingCredentials: _creds
        );
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
