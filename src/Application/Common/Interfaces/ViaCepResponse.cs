namespace GastronomiaMarketplace.Application.Common.Interfaces;

public sealed record ViaCepResponse(
    string Logradouro,
    string Bairro,
    string Localidade,
    string Uf,
    string Cep
);