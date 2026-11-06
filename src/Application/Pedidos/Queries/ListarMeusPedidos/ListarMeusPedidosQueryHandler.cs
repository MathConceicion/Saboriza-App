namespace GastronomiaMarketplace.Application.Pedidos.Queries.ListarMeusPedidos;

using MediatR;
using Microsoft.EntityFrameworkCore;
using GastronomiaMarketplace.Application.Common.Interfaces;

public sealed class ListarMeusPedidosQueryHandler : IRequestHandler<ListarMeusPedidosQuery, IReadOnlyList<MeuPedidoDto>>
{
    private readonly IAppDbContext _db;

    public ListarMeusPedidosQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<MeuPedidoDto>> Handle(ListarMeusPedidosQuery request, CancellationToken ct)
    {
        return await _db.Pedidos
            .AsNoTracking()
            .Where(p => p.ClienteId == request.ClienteId)
            .OrderByDescending(p => p.CriadoEmUtc)
            .Select(p => new MeuPedidoDto(
                p.Id,
                p.LojaId,
                p.Loja.Nome,
                (int)p.Status,
                p.ValorBruto.Valor,
                p.CriadoEmUtc,
                p.PixSimuladoEmUtc != null,
                p.Itens.Select(i => $"{i.Quantidade}x {i.ProdutoNomeSnapshot}").ToList()))
            .ToListAsync(ct);
    }
}
