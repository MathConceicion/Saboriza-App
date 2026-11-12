namespace GastronomiaMarketplace.Application.Agenda.Queries.ListarAgendaDoDia;

using MediatR;
using Microsoft.EntityFrameworkCore;
using GastronomiaMarketplace.Application.Common.Interfaces;

public sealed class ListarAgendaDoDiaQueryHandler : IRequestHandler<ListarAgendaDoDiaQuery, IReadOnlyList<ItemAgendaDto>>
{
    private readonly IAppDbContext _db;

    public ListarAgendaDoDiaQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<ItemAgendaDto>> Handle(ListarAgendaDoDiaQuery request, CancellationToken ct)
    {
        var dia = request.Data?.Date ?? DateTime.UtcNow.Date;
        var inicio = DateTime.SpecifyKind(dia, DateTimeKind.Utc);
        var fim = inicio.AddDays(1);
        var baseQuery = _db.ItensAgenda
            .AsNoTracking()
            .Where(a => a.LojaId == request.LojaId)
            .Where(a => a.AprovadoEmUtc >= inicio && a.AprovadoEmUtc < fim);
        if (request.Status.HasValue)
            baseQuery = baseQuery.Where(a => a.Status == request.Status.Value);
        var itens = await baseQuery
            .Include(a => a.Pedido)
            .ThenInclude(p => p.Itens)
            .OrderBy(a => a.AprovadoEmUtc)
            .ThenBy(a => a.CriadoEmUtc)
            .Select(a => new ItemAgendaDto(
                a.Id,
                a.PedidoId,
                a.Pedido.ClienteNomeSnapshot ?? string.Empty,
                a.AprovadoEmUtc,
                a.Status,
                a.Pedido.Itens.Select(i => $"{i.Quantidade}x {i.ProdutoNomeSnapshot}").ToList(),
                a.Pedido.ValorBruto.Valor,
                0
            ))
            .ToListAsync(ct);
        var comPosicao = itens.Select((dto, idx) => dto with { PosicaoFila = idx + 1 }).ToList();
        return comPosicao;
    }
}
