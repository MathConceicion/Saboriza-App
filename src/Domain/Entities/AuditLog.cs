namespace GastronomiaMarketplace.Domain.Entities;

using GastronomiaMarketplace.Domain.Common;

public sealed class AuditLog : BaseEntity
{
    public Guid? UsuarioId { get; private set; }
    public string Acao { get; private set; } = null!;
    public string Entidade { get; private set; } = null!;
    public string EntidadeId { get; private set; } = null!;
    public DateTime DataUtc { get; private set; }
    public string? ValoresAntigosJson { get; private set; }
    public string? ValoresNovosJson { get; private set; }
    public string? IpOrigem { get; private set; }
    public string? UserAgent { get; private set; }
    public string? Detalhes { get; private set; }

    private AuditLog() { }

    public AuditLog(Guid? usuarioId, string acao, string entidade, string entidadeId, string? valoresAntigosJson = null, string? valoresNovosJson = null, string? ipOrigem = null, string? userAgent = null, string? detalhes = null)
    {
        UsuarioId = usuarioId;
        Acao = acao;
        Entidade = entidade;
        EntidadeId = entidadeId;
        DataUtc = DateTime.UtcNow;
        ValoresAntigosJson = valoresAntigosJson;
        ValoresNovosJson = valoresNovosJson;
        IpOrigem = ipOrigem;
        UserAgent = userAgent;
        Detalhes = detalhes;
    }
}
