namespace GastronomiaMarketplace.Application.Lojas.Queries.ListarLojasAdmin;

using MediatR;
using Microsoft.EntityFrameworkCore;
using GastronomiaMarketplace.Application.Common.Interfaces;
using GastronomiaMarketplace.Application.Common.Models;
using GastronomiaMarketplace.Domain.Enums;

public sealed class ListarLojasAdminQueryHandler : IRequestHandler<ListarLojasAdminQuery, PaginatedList<LojaAdminDto>>
{
    private readonly IAppDbContext _db;

    public ListarLojasAdminQueryHandler(IAppDbContext db) => _db = db;

    public async Task<PaginatedList<LojaAdminDto>> Handle(ListarLojasAdminQuery request, CancellationToken ct)
    {
        var query = _db.Lojas
            .AsNoTracking()
            .Include(l => l.Vendedor).ThenInclude(v => v.Usuario)
            .Include(l => l.CategoriaLoja)
            .Where(l => !l.IsDeleted);

        if (!string.IsNullOrWhiteSpace(request.Busca))
        {
            var busca = request.Busca.ToLower();
            query = query.Where(l =>
                l.Nome.ToLower().Contains(busca) ||
                l.Vendedor.Usuario.NomeCompleto.ToLower().Contains(busca) ||
                l.Vendedor.Usuario.Email.ToLower().Contains(busca));
        }

        if (request.Ativo.HasValue) query = query.Where(l => l.Ativo == request.Ativo.Value);
        if (request.Aprovado.HasValue) query = query.Where(l => l.Aprovado == request.Aprovado.Value);

        query = query.OrderByDescending(l => l.CriadoEmUtc);

        var total = await query.CountAsync(ct);
        var lojas = await query
            .Skip((request.Pagina - 1) * request.Tamanho)
            .Take(request.Tamanho)
            .ToListAsync(ct);

        var lojaIds = lojas.Select(l => l.Id).ToList();
        var produtosPorLoja = await _db.Produtos
            .AsNoTracking()
            .Where(p => lojaIds.Contains(p.LojaId) && !p.IsDeleted)
            .GroupBy(p => p.LojaId)
            .Select(g => new { LojaId = g.Key, Total = g.Count() })
            .ToDictionaryAsync(g => g.LojaId, g => g.Total, ct);

        var items = lojas.Select(l => new LojaAdminDto(
            l.Id,
            l.Nome,
            l.Vendedor.Usuario.NomeCompleto,
            l.Vendedor.Usuario.Email,
            l.CategoriaLoja.Nome,
            l.EnderecoComercial,
            l.Ativo,
            l.Aprovado,
            produtosPorLoja.TryGetValue(l.Id, out var totalProdutos) ? totalProdutos : 0,
            0,
            0m,
            l.CriadoEmUtc
        )).ToList();

        return PaginatedList<LojaAdminDto>.Create(items, total, request.Pagina, request.Tamanho);
    }
}
