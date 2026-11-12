namespace GastronomiaMarketplace.Application.SolicitacoesLojas.Commands.AprovarSolicitacaoLoja;

using MediatR;
using Microsoft.EntityFrameworkCore;
using GastronomiaMarketplace.Application.Common.Interfaces;
using GastronomiaMarketplace.Application.Common.Models;
using GastronomiaMarketplace.Domain.Entities;
using GastronomiaMarketplace.Domain.Enums;

public sealed record AprovarSolicitacaoLojaCommand(
    Guid VendedorId,
    Guid AtorUsuarioId
) : IRequest<Result<Guid>>;

public sealed class AprovarSolicitacaoLojaCommandHandler : IRequestHandler<AprovarSolicitacaoLojaCommand, Result<Guid>>
{
    private readonly IAppDbContext _db;

    public AprovarSolicitacaoLojaCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Result<Guid>> Handle(AprovarSolicitacaoLojaCommand request, CancellationToken ct)
    {
        var vendedor = await _db.Vendedores
            .Include(v => v.Usuario)
            .FirstOrDefaultAsync(v => v.Id == request.VendedorId, ct);

        if (vendedor is null) return Result<Guid>.Failure("Solicitação de loja não encontrada.");
        if (vendedor.Status != StatusVendedor.PendenteAprovacao && vendedor.Status != StatusVendedor.EmAnalise)
            return Result<Guid>.Failure($"Solicitação não pode ser aprovada. Status atual: {vendedor.Status}.");

        vendedor.Aprovar();

        var lojaExiste = await _db.Lojas.AnyAsync(l => l.VendedorId == request.VendedorId, ct);
        if (!lojaExiste)
        {
            var netTopo = NetTopologySuite.NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);
            var point = netTopo.CreatePoint(new NetTopologySuite.Geometries.Coordinate(vendedor.Longitude, vendedor.Latitude));
            var loja = new Loja(vendedor.Id, vendedor.NomeLoja, vendedor.CategoriaLojaId, vendedor.EnderecoComercial, vendedor.Cep, point, vendedor.FotoUrl);
            loja.Aprovar();
            _db.Lojas.Add(loja);
        }

        var notificacao = new Notificacao(
            "Sua loja foi aprovada",
            $"Parabéns! A loja {vendedor.NomeLoja} foi aprovada e já está ativa na plataforma.",
            TipoNotificacao.LojaAprovada,
            vendedor.UsuarioId,
            $"/loja/{vendedor.Id}"
        );
        _db.Notificacoes.Add(notificacao);

        await _db.SaveChangesAsync(ct);
        return Result<Guid>.Success(vendedor.Id);
    }
}
