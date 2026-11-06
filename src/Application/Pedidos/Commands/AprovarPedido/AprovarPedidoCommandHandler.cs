namespace GastronomiaMarketplace.Application.Pedidos.Commands.AprovarPedido;

using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using GastronomiaMarketplace.Application.Common.Interfaces;
using GastronomiaMarketplace.Application.Common.Models;
using GastronomiaMarketplace.Domain.Entities;
using GastronomiaMarketplace.Domain.Enums;

public sealed class AprovarPedidoCommandHandler : IRequestHandler<AprovarPedidoCommand, Result<Guid>>
{
    private readonly IAppDbContext _db;
    private readonly ILogger<AprovarPedidoCommandHandler> _logger;
    private readonly IClock _clock;

    public AprovarPedidoCommandHandler(IAppDbContext db, ILogger<AprovarPedidoCommandHandler> logger, IClock clock)
    {
        _db = db;
        _logger = logger;
        _clock = clock;
    }

    public async Task<Result<Guid>> Handle(AprovarPedidoCommand request, CancellationToken ct)
    {
        var pedido = await _db.Pedidos
            .Include(p => p.Itens)
            .Include(p => p.Loja)
            .FirstOrDefaultAsync(p => p.Id == request.PedidoId, ct);

        if (pedido is null)
            return Result<Guid>.Failure($"Pedido {request.PedidoId} não encontrado.");

        if (!request.IsAdmin)
        {
            if (request.AtorVendorId is null)
                return Result<Guid>.Failure("Usuário não possui vínculo de vendedor.");
            if (pedido.Loja.VendedorId != request.AtorVendorId.Value)
                return Result<Guid>.Failure("Vendedor não autorizado a aprovar pedidos de outra loja.");
        }

        if (pedido.Status != StatusPedido.Pendente)
            return Result<Guid>.Failure($"Pedido não está pendente. Status atual: {pedido.Status}.");

        var produtoIds = pedido.Itens.Select(i => i.ProdutoId).ToList();
        var produtos = await _db.Produtos
            .Where(p => produtoIds.Contains(p.Id))
            .ToListAsync(ct);

        foreach (var item in pedido.Itens)
        {
            var produto = produtos.FirstOrDefault(p => p.Id == item.ProdutoId);
            if (produto is null)
                return Result<Guid>.Failure($"Produto {item.ProdutoId} não encontrado.");
            if (!produto.Ativo)
                return Result<Guid>.Failure($"Produto '{produto.Nome}' está inativo.");
            if (!produto.Aprovado)
                return Result<Guid>.Failure($"Produto '{produto.Nome}' ainda não foi aprovado pelo Admin.");
            if (produto.QuantidadeEmEstoque < item.Quantidade)
                return Result<Guid>.Failure($"Estoque insuficiente para '{produto.Nome}'. Disponível: {produto.QuantidadeEmEstoque}, solicitado: {item.Quantidade}.");
            produto.DebitarEstoque(item.Quantidade);
        }

        var agora = _clock.UtcNow;
        pedido.Aprovar(request.AtorVendorId ?? pedido.Loja.VendedorId, agora);

        var jaNaAgenda = await _db.ItensAgenda.AnyAsync(a => a.PedidoId == pedido.Id, ct);
        if (jaNaAgenda)
            return Result<Guid>.Failure("Pedido já está na agenda de produção.");

        var itemAgenda = ItemAgenda.Criar(
            pedidoId: pedido.Id,
            lojaId: pedido.LojaId,
            vendedorId: pedido.Loja.VendedorId,
            aprovadoEmUtc: agora
        );
        _db.ItensAgenda.Add(itemAgenda);

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Conflito de concorrência ao aprovar pedido {PedidoId}", request.PedidoId);
            return Result<Guid>.Failure("Conflito de concorrência: estoque foi modificado por outro processo. Tente novamente.");
        }
        catch (DbUpdateException ex) when (ex.InnerException != null && ex.InnerException.Message.Contains("IX_ItensAgenda_PedidoId"))
        {
            _logger.LogWarning(ex, "Pedido {PedidoId} já inserido na agenda por requisição concorrente", request.PedidoId);
            return Result<Guid>.Failure("Pedido já foi aprovado por outro processo.");
        }

        _logger.LogInformation("Pedido {PedidoId} aprovado e inserido na agenda {AgendaId} por {Ator}", pedido.Id, itemAgenda.Id, request.AtorUsuarioId);
        return Result<Guid>.Success(pedido.Id);
    }
}
