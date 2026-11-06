namespace GastronomiaMarketplace.Application.Pedidos.Queries.ListarPendentes;

using MediatR;

public sealed record ListarPedidosPendentesQuery(Guid LojaId) : IRequest<IReadOnlyList<PedidoPendenteDto>>;

public sealed record PedidoPendenteDto(
    Guid PedidoId,
    string ClienteNome,
    decimal ValorBruto,
    DateTime CriadoEmUtc,
    bool PixSimulado,
    IReadOnlyList<string> ItensResumo);
