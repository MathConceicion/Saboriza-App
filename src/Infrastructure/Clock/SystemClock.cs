namespace GastronomiaMarketplace.Infrastructure.Clock;

using GastronomiaMarketplace.Application.Common.Interfaces;

public sealed class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
