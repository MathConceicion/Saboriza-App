namespace GastronomiaMarketplace.Application.Pedidos.Commands.CriarPedido;

using MediatR;
using GastronomiaMarketplace.Application.Common.Models;

public sealed record CriarPedidoCommand(
    Guid ClienteId,
    Guid LojaId,
    IReadOnlyList<CriarPedidoItemDto> Itens
) : IRequest<Result<Guid>>;

public sealed record CriarPedidoItemDto(Guid ProdutoId, int Quantidade);
