namespace GastronomiaMarketplace.WebAPI.Controllers;

using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GastronomiaMarketplace.Application.Common.Interfaces;
using GastronomiaMarketplace.Application.Usuarios.Commands.AnonimizarUsuario;
using GastronomiaMarketplace.Application.Usuarios.Queries.ExportarDados;
using GastronomiaMarketplace.Domain.Enums;

[ApiController]
[Route("api/usuarios")]
public sealed class UsuariosController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUser _currentUser;

    public UsuariosController(IMediator mediator, ICurrentUser currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    [HttpDelete("{id:guid}/anonimizar")]
    [Authorize(Policy = "SelfOrAdmin")]
    public async Task<IActionResult> Anonimizar(Guid id, CancellationToken ct)
    {
        var cmd = new AnonimizarUsuarioCommand(id, _currentUser.UserId!.Value, _currentUser.IsAdmin);
        var result = await _mediator.Send(cmd, ct);
        return result.IsSuccess ? NoContent() : BadRequest(new { error = result.Error });
    }

    [HttpGet("{id:guid}/exportar-dados")]
    [Authorize(Policy = "SelfOrAdmin")]
    public async Task<IActionResult> Exportar(Guid id, CancellationToken ct)
    {
        var query = new ExportarDadosPortabilidadeQuery(id, _currentUser.UserId!.Value, _currentUser.IsAdmin);
        var result = await _mediator.Send(query, ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return File(System.Text.Encoding.UTF8.GetBytes(result.Value!), "application/json", $"dados-portabilidade-{id}-{DateTime.UtcNow:yyyyMMdd}.json");
    }

    [HttpPost("me/consentimentos/{tipo}/revogar")]
    [Authorize]
    public async Task<IActionResult> Revogar(string tipo, [FromBody] RevogarDto dto, CancellationToken ct)
    {
        if (!Enum.TryParse<TipoConsentimento>(tipo, true, out var tipoEnum))
            return BadRequest(new { error = "Tipo inválido. Use: DadosSaude, Geolocalizacao, Marketing" });
        var handler = HttpContext.RequestServices.GetRequiredService<IAppDbContext>();
        var consent = await handler.ConsentimentosLgpd.FirstOrDefaultAsync(c => c.UsuarioId == _currentUser.UserId!.Value && c.Tipo == tipoEnum && c.Ativo, ct);
        if (consent is null) return BadRequest(new { error = "Consentimento ativo não encontrado." });
        consent.Revogar(dto.Motivo);
        if (tipoEnum == TipoConsentimento.DadosSaude)
        {
            var usuario = await handler.Usuarios.FirstAsync(u => u.Id == _currentUser.UserId!.Value, ct);
            usuario.RemoverDadosSaude();
        }
        await handler.SaveChangesAsync(ct);
        return NoContent();
    }
}

public sealed record RevogarDto(string? Motivo);
