namespace GastronomiaMarketplace.Domain.Common;

public interface IDomainEvent
{
    DateTime OcorreuEmUtc { get; }
}
