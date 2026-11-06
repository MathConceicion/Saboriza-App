namespace GastronomiaMarketplace.Domain.Events;

using GastronomiaMarketplace.Domain.Common;

public sealed record PedidoCriadoEvent(Guid PedidoId, Guid LojaId, Guid ClienteId) : DomainEvent(DateTime.UtcNow);
