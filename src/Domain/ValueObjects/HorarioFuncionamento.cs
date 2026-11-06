namespace GastronomiaMarketplace.Domain.ValueObjects;

using System.Globalization;
using System.Text.Json;
using GastronomiaMarketplace.Domain.Common;
using GastronomiaMarketplace.Domain.Exceptions;

public sealed class HorarioFuncionamento : ValueObject
{
    public TimeOnly Abertura { get; }
    public TimeOnly Fechamento { get; }

    public HorarioFuncionamento(TimeOnly abertura, TimeOnly fechamento)
    {
        Abertura = abertura;
        Fechamento = fechamento;
    }

    public static HorarioFuncionamento Parse(string abertura, string fechamento)
    {
        if (!TimeOnly.TryParseExact(abertura?.Trim(), "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var ab))
            throw new RegraDeNegocioException("Horário de abertura inválido. Use HH:mm.");
        if (!TimeOnly.TryParseExact(fechamento?.Trim(), "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var fe))
            throw new RegraDeNegocioException("Horário de fechamento inválido. Use HH:mm.");
        return new HorarioFuncionamento(ab, fe);
    }

    public static HorarioFuncionamento? FromJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (!root.TryGetProperty("abertura", out var ab) || !root.TryGetProperty("fechamento", out var fe))
                return null;
            return Parse(ab.GetString() ?? string.Empty, fe.GetString() ?? string.Empty);
        }
        catch (Exception ex) when (ex is JsonException || ex is RegraDeNegocioException)
        {
            return null;
        }
    }

    public string ToJson() => JsonSerializer.Serialize(new { abertura = Abertura.ToString("HH:mm"), fechamento = Fechamento.ToString("HH:mm") });

    public bool AbertoEm(TimeOnly agora)
    {
        if (Abertura == Fechamento) return true;
        if (Fechamento > Abertura) return agora >= Abertura && agora < Fechamento;
        return agora >= Abertura || agora < Fechamento;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Abertura;
        yield return Fechamento;
    }
}
