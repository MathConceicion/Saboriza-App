namespace GastronomiaMarketplace.Application.Produtos.Commands.AlternarProduto;

using MediatR;
using Microsoft.EntityFrameworkCore;
using GastronomiaMarketplace.Application.Common.Interfaces;
using GastronomiaMarketplace.Application.Common.Models;

public sealed class AlternarProdutoCommandHandler : IRequestHandler<AlternarProdutoCommand, Result<Guid>>
{
    private readonly IAppDbContext _db;

    public AlternarProdutoCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Result<Guid>> Handle(AlternarProdutoCommand request, CancellationToken ct)
    {
        var produto = await _db.Produtos.FirstOrDefaultAsync(p => p.Id == request.ProdutoId, ct);
        if (produto is null || produto.LojaId != request.LojaId)
            return Result<Guid>.Failure("Produto não encontrado na sua loja.");
        if (request.Ativo) produto.Ativar();
        else produto.Desativar();
        await _db.SaveChangesAsync(ct);
        return Result<Guid>.Success(produto.Id);
    }
}
