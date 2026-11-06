namespace GastronomiaMarketplace.Domain.Events;

using GastronomiaMarketplace.Domain.Common;

public sealed record ContaUpgradedParaVendedorEvent(Guid UsuarioId) : DomainEvent(DateTime.UtcNow);
