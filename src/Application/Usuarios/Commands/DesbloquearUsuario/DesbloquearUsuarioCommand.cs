namespace GastronomiaMarketplace.Application.Usuarios.Commands.DesbloquearUsuario;

using MediatR;
using Microsoft.EntityFrameworkCore;
using GastronomiaMarketplace.Application.Common.Interfaces;
using GastronomiaMarketplace.Application.Common.Models;

public sealed record DesbloquearUsuarioCommand(
    Guid UsuarioId,
    Guid AtorUsuarioId
) : IRequest<Result>;

public sealed class DesbloquearUsuarioCommandHandler : IRequestHandler<DesbloquearUsuarioCommand, Result>
{
    private readonly IAppDbContext _db;

    public DesbloquearUsuarioCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Result> Handle(DesbloquearUsuarioCommand request, CancellationToken ct)
    {
        var usuario = await _db.Usuarios.FirstOrDefaultAsync(u => u.Id == request.UsuarioId, ct);
        if (usuario is null) return Result.Failure("Usuário não encontrado.");
        if (!usuario.IsBloqueado) return Result.Failure("Usuário não está bloqueado.");

        usuario.Desbloquear();
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
