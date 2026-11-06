namespace GastronomiaMarketplace.Domain.Entities;

using GastronomiaMarketplace.Domain.Common;
using GastronomiaMarketplace.Domain.Enums;

public sealed class ConsentimentoLgpd : BaseEntity
{
    public Guid UsuarioId { get; private set; }
    public TipoConsentimento Tipo { get; private set; }
    public bool Ativo { get; private set; }
    public DateTime ConcedidoEmUtc { get; private set; }
    public DateTime? RevogadoEmUtc { get; private set; }
    public string TextoConsentimento { get; private set; } = null!;
    public string? IpOrigem { get; private set; }
    public string? UserAgent { get; private set; }
    public string? MotivoRevogacao { get; private set; }

    private ConsentimentoLgpd() { }

    public static ConsentimentoLgpd Conceder(Guid usuarioId, TipoConsentimento tipo, string texto, string? ip, string? ua)
        => new()
        {
            UsuarioId = usuarioId,
            Tipo = tipo,
            Ativo = true,
            ConcedidoEmUtc = DateTime.UtcNow,
            TextoConsentimento = texto,
            IpOrigem = ip,
            UserAgent = ua
        };

    public void Revogar(string? motivo = null)
    {
        if (!Ativo) return;
        Ativo = false;
        RevogadoEmUtc = DateTime.UtcNow;
        MotivoRevogacao = motivo;
        AtualizadoEmUtc = DateTime.UtcNow;
    }

    public void Reconceder(string novoTexto, string? ip, string? ua)
    {
        Ativo = true;
        RevogadoEmUtc = null;
        TextoConsentimento = novoTexto;
        IpOrigem = ip;
        UserAgent = ua;
        ConcedidoEmUtc = DateTime.UtcNow;
        AtualizadoEmUtc = DateTime.UtcNow;
    }
}
