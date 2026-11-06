namespace GastronomiaMarketplace.Application.Usuarios.Commands.AnonimizarUsuario;

using MediatR;
using Microsoft.EntityFrameworkCore;
using GastronomiaMarketplace.Application.Common.Interfaces;
using GastronomiaMarketplace.Application.Common.Models;
using GastronomiaMarketplace.Domain.Enums;
using GastronomiaMarketplace.Domain.Entities;

public sealed record AnonimizarUsuarioCommand(Guid UsuarioId, Guid SolicitanteId, bool IsAdmin) : IRequest<Result>;

public sealed class AnonimizarUsuarioCommandHandler : IRequestHandler<AnonimizarUsuarioCommand, Result>
{
    private readonly IAppDbContext _db;

    public AnonimizarUsuarioCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Result> Handle(AnonimizarUsuarioCommand request, CancellationToken ct)
    {
        if (request.UsuarioId != request.SolicitanteId && !request.IsAdmin)
            return Result.Failure("Apenas o titular ou Admin pode solicitar anonimização.");
        var usuario = await _db.Usuarios.FirstOrDefaultAsync(u => u.Id == request.UsuarioId, ct);
        if (usuario is null) return Result.Failure("Usuário não encontrado.");
        if (usuario.IsAnonimizado) return Result.Failure("Usuário já anonimizado.");
        var temPedidoAtivo = await _db.Pedidos.AnyAsync(p =>
            p.ClienteId == request.UsuarioId &&
            (p.Status == StatusPedido.Pendente || p.Status == StatusPedido.EmPreparo || p.Status == StatusPedido.ProntoParaRetirada), ct);
        if (temPedidoAtivo)
            return Result.Failure("Não é possível anonimizar enquanto houver pedidos em andamento. Finalize ou cancele os pedidos.");
        var hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(usuario.Id.ToString())))[..8].ToLower();
        usuario.Anonimizar(hash);
        var pedidos = await _db.Pedidos.Where(p => p.ClienteId == request.UsuarioId).ToListAsync(ct);
        foreach (var p in pedidos)
            p.AnonimizarSnapshotCliente(hash);
        var consentimentos = await _db.ConsentimentosLgpd.Where(c => c.UsuarioId == request.UsuarioId).ToListAsync(ct);
        foreach (var c in consentimentos) c.Revogar("Titular solicitou anonimização");
        _db.AuditLogs.Add(new AuditLog(
            usuarioId: request.SolicitanteId,
            acao: "ANONIMIZACAO_LGPD",
            entidade: "Usuario",
            entidadeId: usuario.Id.ToString(),
            detalhes: $"Usuario {usuario.Id} anonimizado (hash={hash}) por {request.SolicitanteId} (IsAdmin={request.IsAdmin})"
        ));
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
