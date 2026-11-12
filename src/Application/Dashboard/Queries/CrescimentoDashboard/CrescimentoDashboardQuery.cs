namespace GastronomiaMarketplace.Application.Dashboard.Queries.CrescimentoDashboard;

using MediatR;
using Microsoft.EntityFrameworkCore;
using GastronomiaMarketplace.Application.Common.Interfaces;
using GastronomiaMarketplace.Domain.Enums;

public sealed record CrescimentoDashboardQuery(int Meses = 6) : IRequest<IReadOnlyList<PontoCrescimentoDto>>;

public sealed record PontoCrescimentoDto(
    string Mes,
    int Usuarios,
    int Lojas,
    decimal Vendas
);

public sealed class CrescimentoDashboardQueryHandler : IRequestHandler<CrescimentoDashboardQuery, IReadOnlyList<PontoCrescimentoDto>>
{
    private readonly IAppDbContext _db;

    public CrescimentoDashboardQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<PontoCrescimentoDto>> Handle(CrescimentoDashboardQuery request, CancellationToken ct)
    {
        var agora = DateTime.UtcNow;
        var resultado = new List<PontoCrescimentoDto>();

        for (int i = request.Meses - 1; i >= 0; i--)
        {
            var inicioMes = new DateTime(agora.Year, agora.Month, 1).AddMonths(-i);
            var fimMes = inicioMes.AddMonths(1).AddDays(-1);

            var usuarios = await _db.Usuarios
                .Where(u => !u.IsDeleted && !u.IsAnonimizado && u.CriadoEmUtc >= inicioMes && u.CriadoEmUtc <= fimMes)
                .CountAsync(ct);

            var lojas = await _db.Vendedores
                .Where(v => v.CriadoEmUtc >= inicioMes && v.CriadoEmUtc <= fimMes)
                .CountAsync(ct);

            var vendas = await _db.Pedidos
                .Where(p => p.Status == StatusPedido.Finalizado && p.CriadoEmUtc >= inicioMes && p.CriadoEmUtc <= fimMes)
                .Select(p => p.ValorBruto.Valor)
                .SumAsync(ct);

            resultado.Add(new PontoCrescimentoDto(
                inicioMes.ToString("MMM/yyyy"),
                usuarios,
                lojas,
                vendas
            ));
        }

        return resultado;
    }
}
