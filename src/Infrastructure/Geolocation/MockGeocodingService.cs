namespace GastronomiaMarketplace.Infrastructure.Geolocation;

using GastronomiaMarketplace.Application.Common.Interfaces;

public sealed class MockGeocodingService : IGeocodingService
{
    private static readonly Dictionary<string, CoordenadaResultado> CepMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["01310-100"] = new(-23.561414, -46.656013),
        ["01311-000"] = new(-23.556580, -46.662321),
        ["04567-000"] = new(-23.588050, -46.632070),
        ["05001-000"] = new(-23.534050, -46.682070),
        ["05410-002"] = new(-23.567120, -46.687500),
    };

    public Task<CoordenadaResultado?> GeocodificarAsync(string endereco, string cep, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(cep) && CepMap.TryGetValue(NormalizarCep(cep), out var coord))
            return Task.FromResult<CoordenadaResultado?>(coord);
        if (!string.IsNullOrWhiteSpace(endereco))
        {
            var key = CepMap.Keys.FirstOrDefault(k => endereco.Contains(k, StringComparison.OrdinalIgnoreCase));
            if (key is not null) return Task.FromResult<CoordenadaResultado?>(CepMap[key]);
        }
        if (!string.IsNullOrWhiteSpace(cep))
            return Task.FromResult<CoordenadaResultado?>(null);
        return Task.FromResult<CoordenadaResultado?>(null);
    }

    public Task<CoordenadaResultado?> GeocodificarCepAsync(string cep, CancellationToken ct)
    {
        if (CepMap.TryGetValue(NormalizarCep(cep), out var coord))
            return Task.FromResult<CoordenadaResultado?>(coord);
        return Task.FromResult<CoordenadaResultado?>(null);
    }

    private static string NormalizarCep(string cep)
    {
        var digitos = new string(cep.Where(char.IsDigit).ToArray());
        if (digitos.Length == 8) return string.Concat(digitos.AsSpan(0, 5), "-", digitos.AsSpan(5, 3));
        return cep.Trim();
    }
}
