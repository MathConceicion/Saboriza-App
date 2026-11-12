namespace GastronomiaMarketplace.Application.Lojas.Commands.ReativarLoja;

using MediatR;
using Microsoft.EntityFrameworkCore;
using GastronomiaMarketplace.Application.Common.Interfaces;
using GastronomiaMarketplace.Application.Common.Models;

public sealed record ReativarLojaCommand(
    Guid LojaId,
    Guid AtorUsuarioId
) : IRequest<Result>;

public sealed class ReativarLojaCommandHandler : IRequestHandler<ReativarLojaCommand, Result>
{
    private readonly IAppDbContext _db;

    public ReativarLojaCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Result> Handle(ReativarLojaCommand request, CancellationToken ct)
    {
        var loja = await _db.Lojas.FirstOrDefaultAsync(l => l.Id == request.LojaId, ct);
        if (loja is null) return Result.Failure("Loja não encontrada.");
        if (loja.Ativo) return Result.Failure("Loja já está ativa.");

        loja.Ativar();
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
