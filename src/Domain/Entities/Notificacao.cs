namespace GastronomiaMarketplace.Domain.Entities;

using GastronomiaMarketplace.Domain.Common;
using GastronomiaMarketplace.Domain.Enums;

public sealed class Notificacao : BaseEntity
{
    public Guid? UsuarioId { get; private set; }
    public string Titulo { get; private set; } = null!;
    public string Mensagem { get; private set; } = null!;
    public TipoNotificacao Tipo { get; private set; }
    public bool Lida { get; private set; }
    public DateTime? LidaEmUtc { get; private set; }
    public string? Url { get; private set; }
    public string? EntidadeTipo { get; private set; }
    public string? EntidadeId { get; private set; }

    private Notificacao() { }

    public Notificacao(string titulo, string mensagem, TipoNotificacao tipo, Guid? usuarioId = null, string? url = null, string? entidadeTipo = null, string? entidadeId = null)
    {
        Titulo = titulo;
        Mensagem = mensagem;
        Tipo = tipo;
        UsuarioId = usuarioId;
        Url = url;
        EntidadeTipo = entidadeTipo;
        EntidadeId = entidadeId;
        Lida = false;
    }

    public void MarcarComoLida()
    {
        if (Lida) return;
        Lida = true;
        LidaEmUtc = DateTime.UtcNow;
        AtualizadoEmUtc = DateTime.UtcNow;
    }
}

public enum TipoNotificacao
{
    SolicitacaoLoja = 1,
    NovoUsuario = 2,
    LojaAprovada = 3,
    LojaRecusada = 4,
    LojaSuspensa = 5,
    UsuarioBloqueado = 6,
    Sistema = 7
}
