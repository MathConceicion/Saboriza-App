namespace GastronomiaMarketplace.Application.Lojas.Commands.SuspenderLoja;

using MediatR;
using Microsoft.EntityFrameworkCore;
using GastronomiaMarketplace.Application.Common.Interfaces;
using GastronomiaMarketplace.Application.Common.Models;
using GastronomiaMarketplace.Domain.Entities;
using GastronomiaMarketplace.Domain.Enums;

public sealed record SuspenderLojaCommand(
    Guid LojaId,
    Guid AtorUsuarioId
) : IRequest<Result>;

public sealed class SuspenderLojaCommandHandler : IRequestHandler<SuspenderLojaCommand, Result>
{
    private readonly IAppDbContext _db;

    public SuspenderLojaCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Result> Handle(SuspenderLojaCommand request, CancellationToken ct)
    {
        var loja = await _db.Lojas
            .Include(l => l.Vendedor).ThenInclude(v => v.Usuario)
            .FirstOrDefaultAsync(l => l.Id == request.LojaId, ct);
        if (loja is null) return Result.Failure("Loja não encontrada.");
        if (!loja.Ativo) return Result.Failure("Loja já está inativa.");

        loja.Desativar();

        var notificacao = new Notificacao(
            "Sua loja foi suspensa",
            $"A loja {loja.Nome} foi suspensa pelo administrador da plataforma.",
            TipoNotificacao.LojaSuspensa,
            loja.Vendedor.UsuarioId,
            $"/loja/{loja.Id}"
        );
        _db.Notificacoes.Add(notificacao);

        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
