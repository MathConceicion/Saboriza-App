namespace GastronomiaMarketplace.Application.Common.Interfaces;

public interface IHtmlSanitizerService
{
    string Sanitize(string? html);
}
