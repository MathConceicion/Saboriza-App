namespace GastronomiaMarketplace.Application.Pedidos.Commands.RecusarPedido;

using MediatR;
using GastronomiaMarketplace.Application.Common.Models;

public sealed record RecusarPedidoCommand(
    Guid PedidoId,
    Guid AtorUsuarioId,
    Guid? AtorVendorId,
    bool IsAdmin,
    string? Motivo
) : IRequest<Result<Guid>>;
