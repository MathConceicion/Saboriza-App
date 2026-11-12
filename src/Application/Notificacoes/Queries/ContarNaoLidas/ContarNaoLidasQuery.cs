namespace GastronomiaMarketplace.Application.Notificacoes.Queries.ContarNaoLidas;

using MediatR;
using Microsoft.EntityFrameworkCore;
using GastronomiaMarketplace.Application.Common.Interfaces;
using GastronomiaMarketplace.Application.Common.Models;

public sealed record ContarNaoLidasQuery(Guid UsuarioId) : IRequest<Result<int>>;

public sealed class ContarNaoLidasQueryHandler : IRequestHandler<ContarNaoLidasQuery, Result<int>>
{
    private readonly IAppDbContext _db;

    public ContarNaoLidasQueryHandler(IAppDbContext db) => _db = db;

    public async Task<Result<int>> Handle(ContarNaoLidasQuery request, CancellationToken ct)
    {
        var query = _db.Notificacoes.Where(n => n.UsuarioId == request.UsuarioId && !n.Lida);
        var count = await query.CountAsync(ct);
        return Result<int>.Success(count);
    }
}
