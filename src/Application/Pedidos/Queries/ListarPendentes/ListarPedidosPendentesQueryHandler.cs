namespace GastronomiaMarketplace.Application.Pedidos.Queries.ListarPendentes;

using MediatR;
using Microsoft.EntityFrameworkCore;
using GastronomiaMarketplace.Application.Common.Interfaces;
using GastronomiaMarketplace.Domain.Enums;

public sealed class ListarPedidosPendentesQueryHandler : IRequestHandler<ListarPedidosPendentesQuery, IReadOnlyList<PedidoPendenteDto>>
{
    private readonly IAppDbContext _db;

    public ListarPedidosPendentesQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<PedidoPendenteDto>> Handle(ListarPedidosPendentesQuery request, CancellationToken ct)
    {
        return await _db.Pedidos
            .AsNoTracking()
            .Where(p => p.LojaId == request.LojaId && p.Status == StatusPedido.Pendente)
            .OrderBy(p => p.CriadoEmUtc)
            .Select(p => new PedidoPendenteDto(
                p.Id,
                p.ClienteNomeSnapshot ?? string.Empty,
                p.ValorBruto.Valor,
                p.CriadoEmUtc,
                p.PixSimuladoEmUtc != null,
                p.Itens.Select(i => $"{i.Quantidade}x {i.ProdutoNomeSnapshot}").ToList()))
            .ToListAsync(ct);
    }
}
