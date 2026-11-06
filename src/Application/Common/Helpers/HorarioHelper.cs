namespace GastronomiaMarketplace.Application.Common.Helpers;

using GastronomiaMarketplace.Domain.ValueObjects;

public static class HorarioHelper
{
    public static TimeZoneInfo FusoBrasilia()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time");
        }
    }

    public static bool? AbertoAgora(string? horarioJson, DateTime utcAgora)
    {
        var horario = HorarioFuncionamento.FromJson(horarioJson);
        if (horario is null) return null;
        var agoraBrasilia = TimeOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(utcAgora, FusoBrasilia()));
        return horario.AbertoEm(agoraBrasilia);
    }

    public static (string Abertura, string Fechamento)? ExtrairHorario(string? horarioJson)
    {
        var horario = HorarioFuncionamento.FromJson(horarioJson);
        if (horario is null) return null;
        return (horario.Abertura.ToString("HH:mm"), horario.Fechamento.ToString("HH:mm"));
    }
}
