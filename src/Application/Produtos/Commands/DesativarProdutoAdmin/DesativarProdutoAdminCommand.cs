namespace GastronomiaMarketplace.Application.Produtos.Commands.DesativarProdutoAdmin;

using MediatR;
using Microsoft.EntityFrameworkCore;
using GastronomiaMarketplace.Application.Common.Interfaces;
using GastronomiaMarketplace.Application.Common.Models;

public sealed record DesativarProdutoAdminCommand(
    Guid ProdutoId,
    Guid AtorUsuarioId
) : IRequest<Result>;

public sealed class DesativarProdutoAdminCommandHandler : IRequestHandler<DesativarProdutoAdminCommand, Result>
{
    private readonly IAppDbContext _db;

    public DesativarProdutoAdminCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Result> Handle(DesativarProdutoAdminCommand request, CancellationToken ct)
    {
        var produto = await _db.Produtos.FirstOrDefaultAsync(p => p.Id == request.ProdutoId, ct);
        if (produto is null) return Result.Failure("Produto não encontrado.");
        if (!produto.Ativo) return Result.Failure("Produto já está inativo.");

        produto.Desativar();
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
