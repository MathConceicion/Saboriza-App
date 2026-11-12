namespace GastronomiaMarketplace.Application.Produtos.Queries.ListarProdutosAdmin;

using MediatR;
using Microsoft.EntityFrameworkCore;
using GastronomiaMarketplace.Application.Common.Interfaces;
using GastronomiaMarketplace.Application.Common.Models;

public sealed class ListarProdutosAdminQueryHandler : IRequestHandler<ListarProdutosAdminQuery, PaginatedList<ProdutoAdminDto>>
{
    private readonly IAppDbContext _db;

    public ListarProdutosAdminQueryHandler(IAppDbContext db) => _db = db;

    public async Task<PaginatedList<ProdutoAdminDto>> Handle(ListarProdutosAdminQuery request, CancellationToken ct)
    {
        var query = _db.Produtos
            .AsNoTracking()
            .Include(p => p.Loja)
            .Where(p => !p.IsDeleted);

        if (!string.IsNullOrWhiteSpace(request.Busca))
        {
            var busca = request.Busca.ToLower();
            query = query.Where(p => p.Nome.ToLower().Contains(busca));
        }

        if (request.LojaId.HasValue) query = query.Where(p => p.LojaId == request.LojaId.Value);
        if (request.Ativo.HasValue) query = query.Where(p => p.Ativo == request.Ativo.Value);
        if (request.Aprovado.HasValue) query = query.Where(p => p.Aprovado == request.Aprovado.Value);

        query = query.OrderByDescending(p => p.CriadoEmUtc);

        var total = await query.CountAsync(ct);
        var items = await query
            .Skip((request.Pagina - 1) * request.Tamanho)
            .Take(request.Tamanho)
            .Select(p => new ProdutoAdminDto(
                p.Id,
                p.Nome,
                p.Descricao,
                p.Preco,
                p.QuantidadeEmEstoque,
                p.Ativo,
                p.Aprovado,
                p.LojaId,
                p.Loja.Nome,
                p.ImagemUrl,
                p.CriadoEmUtc
            ))
            .ToListAsync(ct);

        return PaginatedList<ProdutoAdminDto>.Create(items, total, request.Pagina, request.Tamanho);
    }
}
