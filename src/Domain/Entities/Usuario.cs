namespace GastronomiaMarketplace.Domain.Entities;

using GastronomiaMarketplace.Domain.Common;
using GastronomiaMarketplace.Domain.Enums;
using GastronomiaMarketplace.Domain.Events;
using GastronomiaMarketplace.Domain.Exceptions;

public sealed class Usuario : AggregateRoot
{
    public string NomeCompleto { get; private set; } = null!;
    public string Email { get; private set; } = null!;
    public string SenhaHash { get; private set; } = null!;
    public string? Cpf { get; private set; }
    public string? Telefone { get; private set; }
    public string? EnderecoAtual { get; private set; }
    public string? Cep { get; private set; }
    public PapelUsuario Papel { get; private set; } = PapelUsuario.Cliente;
    public string? AlergiasSeveras { get; private set; }
    public string? RestricoesAlimentares { get; private set; }
    public string? ObservacoesSaude { get; private set; }
    public bool IsAnonimizado { get; private set; }
    public bool IsBloqueado { get; private set; }
    public DateTime? BloqueadoEmUtc { get; private set; }
    public Guid? BloqueadoPorId { get; private set; }
    public string? MotivoBloqueio { get; private set; }
    public Vendedor? Vendedor { get; private set; }

    private Usuario() { }

    public Usuario(string nomeCompleto, string email, string senhaHash, string? cpf = null)
    {
        NomeCompleto = nomeCompleto;
        Email = email.ToLowerInvariant();
        SenhaHash = senhaHash;
        Cpf = cpf;
        Papel = PapelUsuario.Cliente;
    }

    public void PromoverParaVendedor()
    {
        if (Papel == PapelUsuario.Admin) throw new RegraDeNegocioException("Admin não pode virar vendedor.");
        if (Papel == PapelUsuario.Vendedor) throw new RegraDeNegocioException("Usuário já é vendedor.");
        Papel = PapelUsuario.Vendedor;
        AtualizadoEmUtc = DateTime.UtcNow;
        AddDomainEvent(new ContaUpgradedParaVendedorEvent(Id));
    }

    public void AtualizarDadosSaude(string? alergias, string? restricoes, string? observacoes)
    {
        if (alergias?.Length > 2000) throw new RegraDeNegocioException("Alergias excede 2000 caracteres.");
        if (restricoes?.Length > 2000) throw new RegraDeNegocioException("Restrições excede 2000 caracteres.");
        AlergiasSeveras = alergias?.Trim();
        RestricoesAlimentares = restricoes?.Trim();
        ObservacoesSaude = observacoes?.Trim();
        AtualizadoEmUtc = DateTime.UtcNow;
    }

    public void RemoverDadosSaude()
    {
        AlergiasSeveras = null;
        RestricoesAlimentares = null;
        ObservacoesSaude = null;
        AtualizadoEmUtc = DateTime.UtcNow;
    }

    public void Anonimizar(string hash)
    {
        NomeCompleto = $"Titular Anonimizado {hash}";
        Email = $"anon_{hash}@anon.local";
        Cpf = null;
        Telefone = null;
        EnderecoAtual = null;
        AlergiasSeveras = null;
        RestricoesAlimentares = null;
        ObservacoesSaude = null;
        SenhaHash = "ANONIMIZADO";
        IsAnonimizado = true;
        AtualizadoEmUtc = DateTime.UtcNow;
        AddDomainEvent(new UsuarioAnonimizadoEvent(Id, hash));
        MarcarComoDeletado();
    }

    public void AtualizarPerfil(string? nomeCompleto, string? telefone, string? enderecoAtual, string? cep)
    {
        if (!string.IsNullOrWhiteSpace(nomeCompleto)) NomeCompleto = nomeCompleto.Trim();
        if (telefone is not null) Telefone = telefone.Trim();
        if (enderecoAtual is not null) EnderecoAtual = enderecoAtual.Trim();
        if (cep is not null) Cep = cep.Trim();
        AtualizadoEmUtc = DateTime.UtcNow;
    }

    public void Bloquear(DateTime bloqueadoEmUtc, Guid bloqueadoPorId, string? motivo)
    {
        if (IsBloqueado) throw new RegraDeNegocioException("Usuário já está bloqueado.");
        if (Papel == PapelUsuario.Admin) throw new RegraDeNegocioException("Não é possível bloquear um administrador.");
        IsBloqueado = true;
        BloqueadoEmUtc = bloqueadoEmUtc;
        BloqueadoPorId = bloqueadoPorId;
        MotivoBloqueio = motivo;
        AtualizadoEmUtc = bloqueadoEmUtc;
    }

    public void Desbloquear()
    {
        if (!IsBloqueado) throw new RegraDeNegocioException("Usuário não está bloqueado.");
        IsBloqueado = false;
        BloqueadoEmUtc = null;
        BloqueadoPorId = null;
        MotivoBloqueio = null;
        AtualizadoEmUtc = DateTime.UtcNow;
    }
}
