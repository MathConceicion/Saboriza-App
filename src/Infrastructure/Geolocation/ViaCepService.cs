namespace GastronomiaMarketplace.Infrastructure.Geolocation;

using GastronomiaMarketplace.Application.Common.Interfaces;

public sealed class ViaCepService : IViaCepService
{
    private readonly IHttpClientFactory _httpClientFactory;

    public ViaCepService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<ViaCepResponse?> ObterEnderecoPorCepAsync(string cep, CancellationToken ct)
    {
        var cepNormalizado = NormalizarCep(cep);
        if (string.IsNullOrEmpty(cepNormalizado)) return null;

        var client = _httpClientFactory.CreateClient("ViaCep");
        var response = await client.GetStringAsync($"https://viacep.com.br/ws/{cepNormalizado}/json", ct);

        viacep_net ViaeCepResult;
        try
        {
            ViaeCepResult = System.Text.Json.JsonSerializer.Deserialize<viacep_net>(response) ?? throw new InvalidDataException("Resposta vviaCEP inválida.");
        }
        catch
        {
            return null;
        }

        if (ViaeCepResult?.erro == "true") return null;

        return new ViaCepResponse(
            Logradouro: ViaeCepResult.logradouro,
            Bairro: ViaeCepResult.bairro,
            Localidade: ViaeCepResult.localidade,
            Uf: ViaeCepResult.uf,
            Cep: ViaeCepResult.cep
        );
    }

    private static string NormalizarCep(string cep)
    {
        var digitos = new string(cep.Where(char.IsDigit).ToArray());
        if (digitos.Length == 8)
            return string.Concat(digitos.AsSpan(0, 5), "-", digitos.AsSpan(5, 3));
        return cep.Trim();
    }

    private sealed class viacep_net
    {
        public string? cep { get; set; }
        public string? logradouro { get; set; }
        public string? bairro { get; set; }
        public string? localidade { get; set; }
        public string? uf { get; set; }
        public string? erro { get; set; }
    }
}