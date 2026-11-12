namespace GastronomiaMarketplace.Application.Dashboard.Queries.EstatisticasDashboard;

using MediatR;
using Microsoft.EntityFrameworkCore;
using GastronomiaMarketplace.Application.Common.Interfaces;
using GastronomiaMarketplace.Domain.Enums;
public sealed class EstatisticasDashboardQueryHandler : IRequestHandler<EstatisticasDashboardQuery, EstatisticasDashboardDto>
{
    private readonly IAppDbContext _db;

    public EstatisticasDashboardQueryHandler(IAppDbContext db) => _db = db;

    public async Task<EstatisticasDashboardDto> Handle(EstatisticasDashboardQuery request, CancellationToken ct)
    {
        var agora = DateTime.UtcNow;
        var trintaDiasAtras = agora.AddDays(-30);

        var totalUsuarios = await _db.Usuarios.Where(u => !u.IsDeleted && !u.IsAnonimizado).CountAsync(ct);
        var totalLojas = await _db.Lojas.Where(l => !l.IsDeleted).CountAsync(ct);
        var lojasAguardando = await _db.Vendedores.Where(v => v.Status == StatusVendedor.PendenteAprovacao).CountAsync(ct);
        var lojasAprovadas = await _db.Vendedores.Where(v => v.Status == StatusVendedor.Aprovado).CountAsync(ct);
        var lojasRecusadas = await _db.Vendedores.Where(v => v.Status == StatusVendedor.Rejeitado).CountAsync(ct);
        var lojasSuspensas = await _db.Lojas.Where(l => !l.IsDeleted && !l.Ativo).CountAsync(ct);
        var totalProdutos = await _db.Produtos.Where(p => !p.IsDeleted).CountAsync(ct);
        var totalVendas = await _db.Pedidos.Where(p => p.Status == StatusPedido.Finalizado).CountAsync(ct);
        var faturamentoGeral = await _db.Pedidos
            .Where(p => p.Status == StatusPedido.Finalizado && p.ValorLiquidoVendedor != null)
            .Select(p => p.ValorBruto.Valor)
            .SumAsync(ct);
        var novosUsuarios30Dias = await _db.Usuarios.Where(u => !u.IsDeleted && !u.IsAnonimizado && u.CriadoEmUtc >= trintaDiasAtras).CountAsync(ct);
        var novasLojas30Dias = await _db.Vendedores.Where(v => v.CriadoEmUtc >= trintaDiasAtras).CountAsync(ct);
        var vendas30Dias = await _db.Pedidos
            .Where(p => p.Status == StatusPedido.Finalizado && p.CriadoEmUtc >= trintaDiasAtras)
            .Select(p => p.ValorBruto.Valor)
            .SumAsync(ct);

        return new EstatisticasDashboardDto(
            totalUsuarios,
            totalLojas,
            lojasAguardando,
            lojasAprovadas,
            lojasRecusadas,
            lojasSuspensas,
            totalProdutos,
            totalVendas,
            faturamentoGeral,
            novosUsuarios30Dias,
            novasLojas30Dias,
            vendas30Dias
        );
    }
}
