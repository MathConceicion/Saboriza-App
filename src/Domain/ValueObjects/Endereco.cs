namespace GastronomiaMarketplace.Domain.ValueObjects;

using GastronomiaMarketplace.Domain.Common;

public sealed class Endereco : ValueObject
{
    public string Logradouro { get; }
    public string Cep { get; }
    public string Cidade { get; }
    public string Estado { get; }

    public Endereco(string logradouro, string cep, string cidade, string estado)
    {
        if (string.IsNullOrWhiteSpace(logradouro)) throw new ArgumentException("Logradouro obrigatório.", nameof(logradouro));
        if (string.IsNullOrWhiteSpace(cep)) throw new ArgumentException("CEP obrigatório.", nameof(cep));
        Logradouro = logradouro.Trim();
        Cep = cep.Trim();
        Cidade = cidade.Trim();
        Estado = estado.Trim();
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Logradouro;
        yield return Cep;
        yield return Cidade;
        yield return Estado;
    }
}
