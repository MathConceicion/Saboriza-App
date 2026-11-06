namespace GastronomiaMarketplace.Domain.Exceptions;

public sealed class RegraDeNegocioException : DomainException
{
    public RegraDeNegocioException(string message) : base(message) { }
}
