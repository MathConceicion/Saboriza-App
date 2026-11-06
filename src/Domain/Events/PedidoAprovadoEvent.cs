namespace GastronomiaMarketplace.Domain.Events;

using GastronomiaMarketplace.Domain.Common;

public sealed record PedidoAprovadoEvent(Guid PedidoId, Guid LojaId, DateTime AprovadoEmUtc) : DomainEvent(DateTime.UtcNow);
