namespace GastronomiaMarketplace.Application.Pedidos.Commands.AprovarPedido;

using MediatR;
using GastronomiaMarketplace.Application.Common.Models;

public sealed record AprovarPedidoCommand(
    Guid PedidoId,
    Guid AtorUsuarioId,
    Guid? AtorVendorId,
    bool IsAdmin
) : IRequest<Result<Guid>>;
