namespace GastronomiaMarketplace.Domain.ValueObjects;

using GastronomiaMarketplace.Domain.Common;

public sealed class Dinheiro : ValueObject
{
    public decimal Valor { get; }
    public string Moeda { get; }

    public Dinheiro(decimal valor, string moeda)
    {
        if (valor < 0) throw new ArgumentException("Valor não pode ser negativo.", nameof(valor));
        if (string.IsNullOrWhiteSpace(moeda)) throw new ArgumentException("Moeda obrigatória.", nameof(moeda));
        Valor = decimal.Round(valor, 2, MidpointRounding.AwayFromZero);
        Moeda = moeda.ToUpperInvariant();
    }

    public static Dinheiro Zero(string moeda) => new(0m, moeda);
    public Dinheiro Somar(Dinheiro outro) => Moeda == outro.Moeda ? new(Valor + outro.Valor, Moeda) : throw new InvalidOperationException("Moedas diferentes.");
    public Dinheiro Multiplicar(decimal fator) => new(Valor * fator, Moeda);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Valor;
        yield return Moeda;
    }
}
