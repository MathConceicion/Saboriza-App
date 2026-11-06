namespace GastronomiaMarketplace.Application.Pedidos.Commands.CriarPedido;

using MediatR;
using Microsoft.EntityFrameworkCore;
using GastronomiaMarketplace.Application.Common.Interfaces;
using GastronomiaMarketplace.Application.Common.Models;
using GastronomiaMarketplace.Domain.Entities;

public sealed class CriarPedidoCommandHandler : IRequestHandler<CriarPedidoCommand, Result<Guid>>
{
    private readonly IAppDbContext _db;

    public CriarPedidoCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Result<Guid>> Handle(CriarPedidoCommand request, CancellationToken ct)
    {
        var loja = await _db.Lojas.FirstOrDefaultAsync(l => l.Id == request.LojaId, ct);
        if (loja is null) return Result<Guid>.Failure("Loja não encontrada.");
        var cliente = await _db.Usuarios.FirstOrDefaultAsync(u => u.Id == request.ClienteId, ct);
        if (cliente is null) return Result<Guid>.Failure("Cliente não encontrado.");
        var produtoIds = request.Itens.Select(i => i.ProdutoId).ToList();
        var produtos = await _db.Produtos.Where(p => produtoIds.Contains(p.Id)).ToListAsync(ct);
        var itensPedido = new List<ItemPedido>();
        foreach (var dto in request.Itens)
        {
            var produto = produtos.FirstOrDefault(p => p.Id == dto.ProdutoId);
            if (produto is null) return Result<Guid>.Failure($"Produto {dto.ProdutoId} não encontrado.");
            if (!produto.Ativo || !produto.Aprovado) return Result<Guid>.Failure($"Produto {produto.Nome} não disponível.");
            itensPedido.Add(new ItemPedido(produto.Id, produto.Nome, produto.Preco, dto.Quantidade, produto.Ativo));
        }
        var pedido = Pedido.Criar(request.ClienteId, request.LojaId, itensPedido, cliente.NomeCompleto);
        _db.Pedidos.Add(pedido);
        await _db.SaveChangesAsync(ct);
        return Result<Guid>.Success(pedido.Id);
    }
}
