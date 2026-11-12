namespace GastronomiaMarketplace.WebAPI.Controllers;

using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using GastronomiaMarketplace.Application.Agenda.Commands.AvancarStatusAgenda;
using GastronomiaMarketplace.Application.Agenda.Queries.ListarAgendaDoDia;
using GastronomiaMarketplace.Application.Common.Interfaces;
using GastronomiaMarketplace.Application.Pedidos.Queries.ListarPendentes;
using GastronomiaMarketplace.Application.Vendedores.Commands.AtualizarLoja;
using GastronomiaMarketplace.Application.Vendedores.Queries.ObterLoja;
using GastronomiaMarketplace.Application.Vendedores.Commands.SolicitarUpgrade;
using GastronomiaMarketplace.Domain.Enums;

[ApiController]
[Route("api/vendedor")]
public sealed class VendedorController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUser _currentUser;

    public VendedorController(IMediator mediator, ICurrentUser currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    [HttpPost("solicitar-upgrade")]
    [Authorize]
    public async Task<IActionResult> SolicitarUpgrade([FromBody] SolicitarUpgradeDto dto, CancellationToken ct)
    {
        var cmd = new SolicitarUpgradeCommand(_currentUser.UserId!.Value, dto.NomeLoja, dto.CategoriaLojaId, dto.EnderecoComercial, dto.Cep, dto.Telefone, dto.Cnpj, dto.FotoUrl, dto.HorarioAbertura, dto.HorarioFechamento, dto.DiasFuncionamentoJson, dto.Aberta);
        var result = await _mediator.Send(cmd, ct);
        return result.IsSuccess ? Ok(new { vendedorId = result.Value, status = "PendenteAprovacao" }) : BadRequest(new { error = result.Error });
    }

    [HttpGet("pedidos-pendentes")]
    [Authorize(Policy = "VendedorAprovado")]
    public async Task<IActionResult> ListarPendentes(CancellationToken ct)
    {
        if (_currentUser.LojaId is null && !_currentUser.IsAdmin) return BadRequest(new { error = "Vendedor sem loja vinculada." });
        var lojaId = _currentUser.LojaId ?? Guid.Empty;
        var result = await _mediator.Send(new ListarPedidosPendentesQuery(lojaId), ct);
        return Ok(result);
    }

    [HttpGet("loja")]
    [Authorize(Policy = "VendedorAprovado")]
    public async Task<IActionResult> ObterLoja(CancellationToken ct)
    {
        if (_currentUser.LojaId is null) return BadRequest(new { error = "Vendedor sem loja vinculada." });
        var result = await _mediator.Send(new ObterLojaQuery(_currentUser.LojaId.Value), ct);
        return result.IsSuccess ? Ok(result.Value) : NotFound(new { error = result.Error });
    }

    [HttpPut("loja")]
    [Authorize(Policy = "VendedorAprovado")]
    public async Task<IActionResult> AtualizarLoja([FromBody] AtualizarLojaDto dto, CancellationToken ct)
    {
        if (_currentUser.LojaId is null) return BadRequest(new { error = "Vendedor sem loja vinculada." });
        var result = await _mediator.Send(new AtualizarLojaCommand(_currentUser.LojaId.Value, dto.Nome, dto.Descricao, dto.FotoUrl, dto.Cnpj, dto.Telefone, dto.EnderecoComercial, dto.Cep, dto.DiasFuncionamentoJson, dto.Aberta, dto.HorarioAbertura, dto.HorarioFechamento), ct);
        return result.IsSuccess ? NoContent() : BadRequest(new { error = result.Error });
    }

    [HttpGet("agenda")]
    [Authorize(Policy = "VendedorAprovado")]
    public async Task<IActionResult> ListarAgenda([FromQuery] DateTime? data, [FromQuery] StatusPedido? status, CancellationToken ct)
    {
        if (_currentUser.VendorId is null && !_currentUser.IsAdmin) return Forbid();
        var lojaId = _currentUser.LojaId;
        if (lojaId is null && !_currentUser.IsAdmin) return BadRequest(new { error = "Vendedor sem loja vinculada." });
        var targetLojaId = lojaId ?? Guid.Empty;
        if (_currentUser.IsAdmin && targetLojaId == Guid.Empty)
            return BadRequest(new { error = "Admin deve informar lojaId via query." });
        var query = new ListarAgendaDoDiaQuery(targetLojaId, _currentUser.VendorId ?? Guid.Empty, _currentUser.IsAdmin, data, status);
        var result = await _mediator.Send(query, ct);
        return Ok(result);
    }

    [HttpPatch("agenda/{agendaId:guid}/avancar")]
    [Authorize(Policy = "VendedorAprovado")]
    public async Task<IActionResult> Avancar(Guid agendaId, [FromBody] AvancarDto dto, CancellationToken ct)
    {
        if (!Enum.TryParse<StatusPedido>(dto.NovoStatus, true, out var novoStatus))
            return BadRequest(new { error = "NovoStatus inválido. Use ProntoParaRetirada ou Finalizado." });
        var cmd = new AvancarStatusAgendaCommand(agendaId, _currentUser.VendorId ?? Guid.Empty, _currentUser.IsAdmin, novoStatus);
        var result = await _mediator.Send(cmd, ct);
        return result.IsSuccess ? NoContent() : BadRequest(new { error = result.Error });
    }
}

public sealed record SolicitarUpgradeDto(string NomeLoja, Guid CategoriaLojaId, string EnderecoComercial, string Cep, string Telefone, string? Cnpj, string? FotoUrl, string? HorarioAbertura = null, string? HorarioFechamento = null, string? DiasFuncionamentoJson = null, bool? Aberta = null);
public sealed record AtualizarLojaDto(string Nome, string? Descricao, string? FotoUrl, string? Cnpj, string? Telefone, string? EnderecoComercial, string? Cep, string? DiasFuncionamentoJson, bool? Aberta, string? HorarioAbertura = null, string? HorarioFechamento = null);
public sealed record AvancarDto(string NovoStatus);
