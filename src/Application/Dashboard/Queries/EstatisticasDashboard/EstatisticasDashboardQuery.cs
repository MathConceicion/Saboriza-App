namespace GastronomiaMarketplace.Application.Dashboard.Queries.EstatisticasDashboard;

using MediatR;
using GastronomiaMarketplace.Application.Common.Interfaces;
using GastronomiaMarketplace.Application.Common.Models;

public sealed record EstatisticasDashboardQuery() : IRequest<EstatisticasDashboardDto>;

public sealed record EstatisticasDashboardDto(
    int TotalUsuarios,
    int TotalLojas,
    int LojasAguardandoAprovacao,
    int LojasAprovadas,
    int LojasRecusadas,
    int LojasSuspensas,
    int TotalProdutos,
    int TotalVendas,
    decimal FaturamentoGeral,
    int NovosUsuarios30Dias,
    int NovasLojas30Dias,
    decimal Vendas30Dias
);
