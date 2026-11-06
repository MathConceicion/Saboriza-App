namespace GastronomiaMarketplace.Domain.ValueObjects;

using GastronomiaMarketplace.Domain.Common;

public sealed class Coordenada : ValueObject
{
    public double Latitude { get; }
    public double Longitude { get; }

    public Coordenada(double latitude, double longitude)
    {
        if (latitude < -90 || latitude > 90) throw new ArgumentException("Latitude inválida.", nameof(latitude));
        if (longitude < -180 || longitude > 180) throw new ArgumentException("Longitude inválida.", nameof(longitude));
        Latitude = latitude;
        Longitude = longitude;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Latitude;
        yield return Longitude;
    }
}
