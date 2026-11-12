namespace GastronomiaMarketplace.Application.Usuarios.Commands.BloquearUsuario;

using MediatR;
using Microsoft.EntityFrameworkCore;
using GastronomiaMarketplace.Application.Common.Interfaces;
using GastronomiaMarketplace.Application.Common.Models;
using GastronomiaMarketplace.Domain.Entities;
using GastronomiaMarketplace.Domain.Enums;

public sealed record BloquearUsuarioCommand(
    Guid UsuarioId,
    Guid AtorUsuarioId,
    string Motivo
) : IRequest<Result>;

public sealed class BloquearUsuarioCommandHandler : IRequestHandler<BloquearUsuarioCommand, Result>
{
    private readonly IAppDbContext _db;

    public BloquearUsuarioCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Result> Handle(BloquearUsuarioCommand request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Motivo))
            return Result.Failure("O motivo do bloqueio é obrigatório.");

        var usuario = await _db.Usuarios.FirstOrDefaultAsync(u => u.Id == request.UsuarioId, ct);
        if (usuario is null) return Result.Failure("Usuário não encontrado.");
        if (usuario.Papel == PapelUsuario.Admin)
            return Result.Failure("Não é possível bloquear um administrador.");
        if (usuario.IsBloqueado) return Result.Failure("Usuário já está bloqueado.");

        usuario.Bloquear(DateTime.UtcNow, request.AtorUsuarioId, request.Motivo.Trim());

        var notificacao = new Notificacao(
            "Sua conta foi bloqueada",
            $"Sua conta na plataforma Saboriza foi bloqueada. Motivo: {request.Motivo.Trim()}.",
            TipoNotificacao.UsuarioBloqueado,
            usuario.Id
        );
        _db.Notificacoes.Add(notificacao);

        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
