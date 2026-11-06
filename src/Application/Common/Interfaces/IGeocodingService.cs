namespace GastronomiaMarketplace.Application.Common.Interfaces;

public interface IGeocodingService
{
    Task<CoordenadaResultado?> GeocodificarAsync(string endereco, string cep, CancellationToken ct);
    Task<CoordenadaResultado?> GeocodificarCepAsync(string cep, CancellationToken ct);
}

public sealed record CoordenadaResultado(double Latitude, double Longitude);
