namespace GastronomiaMarketplace.Domain.Events;

using GastronomiaMarketplace.Domain.Common;

public sealed record UsuarioAnonimizadoEvent(Guid UsuarioId, string Hash) : DomainEvent(DateTime.UtcNow);
