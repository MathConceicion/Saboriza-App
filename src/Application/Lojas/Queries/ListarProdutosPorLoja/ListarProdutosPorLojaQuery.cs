namespace GastronomiaMarketplace.Application.Lojas.Queries.ListarProdutosPorLoja;

using MediatR;
using Microsoft.EntityFrameworkCore;
using GastronomiaMarketplace.Application.Common.Interfaces;

public sealed record ListarProdutosPorLojaQuery(Guid LojaId, bool ApenasAtivosAprovados = true) : IRequest<IReadOnlyList<ProdutoDto>>;

public sealed record ProdutoDto(Guid Id, string Nome, string? Descricao, decimal Preco, int QuantidadeEmEstoque, bool Ativo, bool Aprovado, string? ImagemUrl);

public sealed class ListarProdutosPorLojaQueryHandler : IRequestHandler<ListarProdutosPorLojaQuery, IReadOnlyList<ProdutoDto>>
{
    private readonly IAppDbContext _db;
    public ListarProdutosPorLojaQueryHandler(IAppDbContext db) => _db = db;
    public async Task<IReadOnlyList<ProdutoDto>> Handle(ListarProdutosPorLojaQuery request, CancellationToken ct)
    {
        var query = _db.Produtos.AsNoTracking().Where(p => p.LojaId == request.LojaId);
        if (request.ApenasAtivosAprovados) query = query.Where(p => p.Ativo && p.Aprovado);
        return await query.Select(p => new ProdutoDto(p.Id, p.Nome, p.Descricao, p.Preco, p.QuantidadeEmEstoque, p.Ativo, p.Aprovado, p.ImagemUrl)).ToListAsync(ct);
    }
}
