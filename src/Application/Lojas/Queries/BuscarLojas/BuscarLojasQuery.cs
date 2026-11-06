namespace GastronomiaMarketplace.Application.Lojas.Queries.BuscarLojas;

using MediatR;
using Microsoft.EntityFrameworkCore;
using GastronomiaMarketplace.Application.Common.Interfaces;

public sealed record BuscarLojasQuery(string? Termo = null, int Limite = 50) : IRequest<IReadOnlyList<LojaBuscaDto>>;

public sealed record LojaBuscaDto(
    Guid LojaId,
    string Nome,
    string Categoria,
    string? FotoUrl
);

public sealed class BuscarLojasQueryHandler : IRequestHandler<BuscarLojasQuery, IReadOnlyList<LojaBuscaDto>>
{
    private readonly IAppDbContext _db;

    public BuscarLojasQueryHandler(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<LojaBuscaDto>> Handle(BuscarLojasQuery request, CancellationToken ct)
    {
        var limite = Math.Clamp(request.Limite, 1, 50);
        var termo = request.Termo?.Trim();
        var query = _db.Lojas
            .AsNoTracking()
            .Where(l => l.Ativo && l.Aprovado);
        if (!string.IsNullOrWhiteSpace(termo))
        {
            var termoNormalizado = termo.ToLowerInvariant();
            query = query.Where(l => l.Nome.ToLower().Contains(termoNormalizado));
        }
        return await query
            .OrderBy(l => l.Nome)
            .Take(limite)
            .Select(l => new LojaBuscaDto(
                l.Id,
                l.Nome,
                l.CategoriaLoja.Nome,
                l.FotoUrl))
            .ToListAsync(ct);
    }
}
