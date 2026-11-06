namespace GastronomiaMarketplace.Application.Common.Interfaces;

public interface IViaCepService
{
    Task<ViaCepResponse?> ObterEnderecoPorCepAsync(string cep, CancellationToken ct);
}