namespace GastronomiaMarketplace.Application.Notificacoes.Commands.MarcarNotificacaoLida;

using MediatR;
using Microsoft.EntityFrameworkCore;
using GastronomiaMarketplace.Application.Common.Interfaces;
using GastronomiaMarketplace.Application.Common.Models;

public sealed record MarcarNotificacaoLidaCommand(
    Guid NotificacaoId,
    Guid UsuarioId
) : IRequest<Result>;

public sealed class MarcarNotificacaoLidaCommandHandler : IRequestHandler<MarcarNotificacaoLidaCommand, Result>
{
    private readonly IAppDbContext _db;

    public MarcarNotificacaoLidaCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Result> Handle(MarcarNotificacaoLidaCommand request, CancellationToken ct)
    {
        var notificacao = await _db.Notificacoes.FirstOrDefaultAsync(n => n.Id == request.NotificacaoId && n.UsuarioId == request.UsuarioId, ct);
        if (notificacao is null) return Result.Failure("Notificação não encontrada.");

        notificacao.MarcarComoLida();
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
