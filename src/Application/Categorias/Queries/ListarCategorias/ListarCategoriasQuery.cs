namespace GastronomiaMarketplace.Application.Categorias.Queries.ListarCategorias;

using MediatR;
using Microsoft.EntityFrameworkCore;
using GastronomiaMarketplace.Application.Common.Interfaces;

public sealed record ListarCategoriasQuery : IRequest<IReadOnlyList<CategoriaDto>>;

public sealed record CategoriaDto(Guid Id, string Nome, string Slug, string? IconeUrl, int OrdemExibicao);

public sealed class ListarCategoriasQueryHandler : IRequestHandler<ListarCategoriasQuery, IReadOnlyList<CategoriaDto>>
{
    private readonly IAppDbContext _db;
    public ListarCategoriasQueryHandler(IAppDbContext db) => _db = db;
    public async Task<IReadOnlyList<CategoriaDto>> Handle(ListarCategoriasQuery request, CancellationToken ct)
    {
        return await _db.CategoriasLoja.AsNoTracking().Where(c => c.Ativo).OrderBy(c => c.OrdemExibicao).Select(c => new CategoriaDto(c.Id, c.Nome, c.Slug, c.IconeUrl, c.OrdemExibicao)).ToListAsync(ct);
    }
}
