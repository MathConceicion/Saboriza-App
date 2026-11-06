namespace GastronomiaMarketplace.Domain.Common;

public abstract record DomainEvent(DateTime OcorreuEmUtc) : IDomainEvent;
