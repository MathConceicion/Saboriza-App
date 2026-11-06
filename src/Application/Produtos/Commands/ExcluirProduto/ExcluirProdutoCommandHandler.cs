namespace GastronomiaMarketplace.Application.Produtos.Commands.ExcluirProduto;

using MediatR;
using Microsoft.EntityFrameworkCore;
using GastronomiaMarketplace.Application.Common.Interfaces;
using GastronomiaMarketplace.Application.Common.Models;

public sealed class ExcluirProdutoCommandHandler : IRequestHandler<ExcluirProdutoCommand, Result<Guid>>
{
    private readonly IAppDbContext _db;

    public ExcluirProdutoCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Result<Guid>> Handle(ExcluirProdutoCommand request, CancellationToken ct)
    {
        var produto = await _db.Produtos.FirstOrDefaultAsync(p => p.Id == request.ProdutoId, ct);
        if (produto is null || produto.LojaId != request.LojaId)
            return Result<Guid>.Failure("Produto não encontrado na sua loja.");
        produto.MarcarComoDeletado();
        await _db.SaveChangesAsync(ct);
        return Result<Guid>.Success(produto.Id);
    }
}
