namespace GastronomiaMarketplace.WebAPI.Controllers;

using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using GastronomiaMarketplace.Application.Common.Interfaces;
using GastronomiaMarketplace.Application.Pedidos.Commands.AprovarPedido;
using GastronomiaMarketplace.Application.Pedidos.Commands.CriarPedido;
using GastronomiaMarketplace.Application.Pedidos.Queries.ListarMeusPedidos;
using GastronomiaMarketplace.Infrastructure.Persistence;

[ApiController]
[Route("api/pedidos")]
public sealed class PedidosController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUser _currentUser;
    private readonly AppDbContext _db;
    private readonly IAuthorizationService _auth;

    public PedidosController(IMediator mediator, ICurrentUser currentUser, AppDbContext db, IAuthorizationService auth)
    {
        _mediator = mediator;
        _currentUser = currentUser;
        _db = db;
        _auth = auth;
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Criar([FromBody] CriarPedidoDto dto, CancellationToken ct)
    {
        var cmd = new CriarPedidoCommand(_currentUser.UserId!.Value, dto.LojaId, dto.Itens.Select(i => new CriarPedidoItemDto(i.ProdutoId, i.Quantidade)).ToList());
        var result = await _mediator.Send(cmd, ct);
        return result.IsSuccess ? Ok(new { pedidoId = result.Value }) : BadRequest(new { error = result.Error });
    }

    [HttpGet("meus")]
    [Authorize]
    public async Task<IActionResult> Meus(CancellationToken ct)
    {
        var result = await _mediator.Send(new ListarMeusPedidosQuery(_currentUser.UserId!.Value), ct);
        return Ok(result);
    }

    [HttpPost("{id:guid}/aprovar")]
    [Authorize(Policy = "VendedorAprovado")]
    public async Task<IActionResult> Aprovar(Guid id, CancellationToken ct)
    {
        var pedidoResource = await _db.Pedidos.AsNoTracking().Where(p => p.Id == id).Select(p => new Policies.PedidoResource(p.Id, p.Loja.VendedorId)).FirstOrDefaultAsync(ct);
        if (pedidoResource is not null)
        {
            var authResult = await _auth.AuthorizeAsync(User, pedidoResource, "VendorOwnerOrAdmin");
            if (!authResult.Succeeded) return Forbid();
        }
        var cmd = new AprovarPedidoCommand(
            PedidoId: id,
            AtorUsuarioId: _currentUser.UserId!.Value,
            AtorVendorId: _currentUser.VendorId,
            IsAdmin: _currentUser.IsAdmin
        );
        var result = await _mediator.Send(cmd, ct);
        return result.IsSuccess
            ? Ok(new { pedidoId = result.Value, status = "EmPreparo" })
            : BadRequest(new { error = result.Error });
    }

    [HttpPost("{pedidoId:guid}/simular-pix")]
    [Authorize]
    [EnableRateLimiting("pix")]
    public async Task<IActionResult> SimularPix(Guid pedidoId, CancellationToken ct)
    {
        var pedido = await _db.Pedidos.Include(p => p.Loja).FirstOrDefaultAsync(p => p.Id == pedidoId, ct);
        if (pedido is null) return NotFound(new { error = "Pedido não encontrado." });
        if (pedido.ClienteId != _currentUser.UserId) return Forbid();
        if (pedido.PixSimuladoEmUtc is not null) return BadRequest(new { error = "PIX já simulado." });
        var taxaGlobal = await _db.ConfiguracoesGlobais.AsNoTracking().FirstAsync(ct);
        var taxa = pedido.Loja.TaxaComissaoPercentual ?? taxaGlobal.TaxaComissaoPadraoPercentual;
        var pixPayload = GerarBrCodeFicticio(pedido.Id, pedido.ValorBruto.Valor);
        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            pedido.RegistrarPagamentoPix(taxa, pixPayload);
            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
        return Ok(new
        {
            pixCopiaECola = pixPayload,
            qrCodeBase64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(pixPayload)),
            valorBruto = pedido.ValorBruto.Valor,
            taxaComissao = pedido.TaxaComissaoValor!.Valor,
            valorLiquido = pedido.ValorLiquidoVendedor!.Valor,
            status = "PAGO_FICTICIO"
        });
    }

    private static string GerarBrCodeFicticio(Guid pedidoId, decimal valor)
    {
        var valorStr = valor.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
        var refId = pedidoId.ToString("N")[..12].ToUpper();
        return $"00020126580014BR.GOV.BCB.PIX0136{refId}52040000530398654{valorStr}5802BR5913GASTRONOMIA6009SAO PAULO6207{refId}6304ABCD";
    }
}

public sealed record CriarPedidoDto(Guid LojaId, List<CriarPedidoItemInput> Itens);
public sealed record CriarPedidoItemInput(Guid ProdutoId, int Quantidade);
