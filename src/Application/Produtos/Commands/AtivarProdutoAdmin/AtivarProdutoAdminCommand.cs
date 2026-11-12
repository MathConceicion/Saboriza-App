namespace GastronomiaMarketplace.Application.Produtos.Commands.AtivarProdutoAdmin;

using MediatR;
using Microsoft.EntityFrameworkCore;
using GastronomiaMarketplace.Application.Common.Interfaces;
using GastronomiaMarketplace.Application.Common.Models;

public sealed record AtivarProdutoAdminCommand(
    Guid ProdutoId,
    Guid AtorUsuarioId
) : IRequest<Result>;

public sealed class AtivarProdutoAdminCommandHandler : IRequestHandler<AtivarProdutoAdminCommand, Result>
{
    private readonly IAppDbContext _db;

    public AtivarProdutoAdminCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Result> Handle(AtivarProdutoAdminCommand request, CancellationToken ct)
    {
        var produto = await _db.Produtos.FirstOrDefaultAsync(p => p.Id == request.ProdutoId, ct);
        if (produto is null) return Result.Failure("Produto não encontrado.");
        if (produto.Ativo) return Result.Failure("Produto já está ativo.");

        produto.Ativar();
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
