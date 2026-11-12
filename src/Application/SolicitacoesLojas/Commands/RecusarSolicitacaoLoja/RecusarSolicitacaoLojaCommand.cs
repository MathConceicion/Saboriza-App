namespace GastronomiaMarketplace.Application.SolicitacoesLojas.Commands.RecusarSolicitacaoLoja;

using MediatR;
using Microsoft.EntityFrameworkCore;
using GastronomiaMarketplace.Application.Common.Interfaces;
using GastronomiaMarketplace.Application.Common.Models;
using GastronomiaMarketplace.Domain.Entities;
using GastronomiaMarketplace.Domain.Enums;

public sealed record RecusarSolicitacaoLojaCommand(
    Guid VendedorId,
    string Motivo,
    Guid AtorUsuarioId
) : IRequest<Result>;

public sealed class RecusarSolicitacaoLojaCommandHandler : IRequestHandler<RecusarSolicitacaoLojaCommand, Result>
{
    private readonly IAppDbContext _db;

    public RecusarSolicitacaoLojaCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Result> Handle(RecusarSolicitacaoLojaCommand request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Motivo))
            return Result.Failure("O motivo da recusa é obrigatório.");

        var vendedor = await _db.Vendedores
            .Include(v => v.Usuario)
            .FirstOrDefaultAsync(v => v.Id == request.VendedorId, ct);

        if (vendedor is null) return Result.Failure("Solicitação de loja não encontrada.");
        if (vendedor.Status != StatusVendedor.PendenteAprovacao && vendedor.Status != StatusVendedor.EmAnalise)
            return Result.Failure($"Solicitação não pode ser recusada. Status atual: {vendedor.Status}.");

        vendedor.Rejeitar(request.Motivo.Trim());

        var notificacao = new Notificacao(
            "Sua solicitação de loja foi recusada",
            $"A loja {vendedor.NomeLoja} não foi aprovada. Motivo: {request.Motivo.Trim()}. Você pode enviar uma nova solicitação.",
            TipoNotificacao.LojaRecusada,
            vendedor.UsuarioId,
            "/vender"
        );
        _db.Notificacoes.Add(notificacao);

        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
