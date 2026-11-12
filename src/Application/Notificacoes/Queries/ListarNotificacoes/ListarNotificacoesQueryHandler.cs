namespace GastronomiaMarketplace.Application.Notificacoes.Queries.ListarNotificacoes;

using MediatR;
using Microsoft.EntityFrameworkCore;
using GastronomiaMarketplace.Application.Common.Interfaces;
using GastronomiaMarketplace.Application.Common.Models;
using GastronomiaMarketplace.Domain.Entities;

public sealed class ListarNotificacoesQueryHandler : IRequestHandler<ListarNotificacoesQuery, PaginatedList<NotificacaoDto>>
{
    private readonly IAppDbContext _db;

    public ListarNotificacoesQueryHandler(IAppDbContext db) => _db = db;

    public async Task<PaginatedList<NotificacaoDto>> Handle(ListarNotificacoesQuery request, CancellationToken ct)
    {
        IQueryable<Domain.Entities.Notificacao> query = _db.Notificacoes
            .AsNoTracking()
            .Where(n => n.UsuarioId == request.UsuarioId);

        if (request.ApenasNaoLidas.HasValue && request.ApenasNaoLidas.Value)
            query = query.Where(n => !n.Lida);

        query = query.OrderByDescending(n => n.CriadoEmUtc);

        var total = await query.CountAsync(ct);
        var items = await query
            .Skip((request.Pagina - 1) * request.Tamanho)
            .Take(request.Tamanho)
            .Select(n => new NotificacaoDto(
                n.Id,
                n.Titulo,
                n.Mensagem,
                (int)n.Tipo,
                n.Lida,
                n.LidaEmUtc,
                n.Url,
                n.CriadoEmUtc
            ))
            .ToListAsync(ct);

        return PaginatedList<NotificacaoDto>.Create(items, total, request.Pagina, request.Tamanho);
    }
}
