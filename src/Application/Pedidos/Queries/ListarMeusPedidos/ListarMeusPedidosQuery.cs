namespace GastronomiaMarketplace.Application.Pedidos.Queries.ListarMeusPedidos;

using MediatR;

public sealed record ListarMeusPedidosQuery(Guid ClienteId) : IRequest<IReadOnlyList<MeuPedidoDto>>;

public sealed record MeuPedidoDto(
    Guid PedidoId,
    Guid LojaId,
    string LojaNome,
    int Status,
    decimal ValorBruto,
    DateTime CriadoEmUtc,
    bool PixSimulado,
    IReadOnlyList<string> ItensResumo);
