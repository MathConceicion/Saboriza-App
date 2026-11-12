namespace GastronomiaMarketplace.Application.SolicitacoesLojas.Commands.DefinirEmAnalise;

using MediatR;
using Microsoft.EntityFrameworkCore;
using GastronomiaMarketplace.Application.Common.Interfaces;
using GastronomiaMarketplace.Application.Common.Models;
using GastronomiaMarketplace.Domain.Enums;

public sealed record DefinirEmAnaliseCommand(
    Guid VendedorId,
    Guid AtorUsuarioId
) : IRequest<Result>;

public sealed class DefinirEmAnaliseCommandHandler : IRequestHandler<DefinirEmAnaliseCommand, Result>
{
    private readonly IAppDbContext _db;

    public DefinirEmAnaliseCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Result> Handle(DefinirEmAnaliseCommand request, CancellationToken ct)
    {
        var vendedor = await _db.Vendedores.FirstOrDefaultAsync(v => v.Id == request.VendedorId, ct);
        if (vendedor is null) return Result.Failure("Solicitação de loja não encontrada.");
        if (vendedor.Status != StatusVendedor.PendenteAprovacao)
            return Result.Failure($"Solicitação não pode ser enviada para análise. Status atual: {vendedor.Status}.");

        vendedor.DefinirEmAnalise();
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
