namespace GastronomiaMarketplace.Domain.Entities;

using GastronomiaMarketplace.Domain.Common;
using GastronomiaMarketplace.Domain.Exceptions;

public sealed class ConfiguracaoGlobal : AggregateRoot
{
    public decimal TaxaComissaoPadraoPercentual { get; private set; }
    public DateTime VigenciaDesde { get; private set; }

    private ConfiguracaoGlobal() { }

    public ConfiguracaoGlobal(decimal taxa)
    {
        if (taxa < 0 || taxa > 30) throw new RegraDeNegocioException("Taxa deve estar entre 0 e 30%.");
        TaxaComissaoPadraoPercentual = taxa;
        VigenciaDesde = DateTime.UtcNow;
    }

    public void AtualizarTaxa(decimal novaTaxa, Guid adminId)
    {
        if (novaTaxa < 0 || novaTaxa > 30) throw new RegraDeNegocioException("Taxa deve estar entre 0 e 30%.");
        TaxaComissaoPadraoPercentual = novaTaxa;
        AtualizadoEmUtc = DateTime.UtcNow;
        AddDomainEvent(new Events.TaxaComissaoAlteradaEvent(novaTaxa, adminId));
    }
}
