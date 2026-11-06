namespace GastronomiaMarketplace.Domain.Events;

using GastronomiaMarketplace.Domain.Common;

public sealed record TaxaComissaoAlteradaEvent(decimal NovaTaxa, Guid AdminId) : DomainEvent(DateTime.UtcNow);
