namespace GastronomiaMarketplace.Domain.Entities;

using GastronomiaMarketplace.Domain.Common;
using NetTopologySuite.Geometries;

public sealed class Loja : AggregateRoot
{
    public Guid VendedorId { get; private set; }
    public Vendedor Vendedor { get; private set; } = null!;
    public string Nome { get; private set; } = null!;
    public Guid CategoriaLojaId { get; private set; }
    public CategoriaLoja CategoriaLoja { get; private set; } = null!;
    public string EnderecoComercial { get; private set; } = null!;
    public string Cep { get; private set; } = null!;
    public Point Localizacao { get; private set; } = null!;
    public bool Ativo { get; private set; }
    public bool Aprovado { get; private set; }
    public string? Descricao { get; private set; }
    public string? FotoUrl { get; private set; }
    public string? HorarioFuncionamentoJson { get; private set; }
    public decimal? TaxaComissaoPercentual { get; private set; }

    private Loja() { }

    public Loja(Guid vendedorId, string nome, Guid categoriaId, string endereco, string cep, Point localizacao, string? fotoUrl = null)
    {
        VendedorId = vendedorId;
        Nome = nome;
        CategoriaLojaId = categoriaId;
        EnderecoComercial = endereco;
        Cep = cep;
        Localizacao = localizacao;
        FotoUrl = fotoUrl?.Trim();
        Ativo = true;
        Aprovado = false;
    }

    public void Aprovar()
    {
        Aprovado = true;
        AtualizadoEmUtc = DateTime.UtcNow;
    }

    public void Rejeitar()
    {
        Aprovado = false;
        Ativo = false;
        AtualizadoEmUtc = DateTime.UtcNow;
    }

    public void Desativar()
    {
        Ativo = false;
        AtualizadoEmUtc = DateTime.UtcNow;
    }

    public void Ativar()
    {
        Ativo = true;
        AtualizadoEmUtc = DateTime.UtcNow;
    }

    public void AtualizarPerfil(string nome, string? descricao, string? fotoUrl)
    {
        if (string.IsNullOrWhiteSpace(nome)) throw new Exceptions.RegraDeNegocioException("Nome da loja obrigatório.");
        Nome = nome.Trim();
        Descricao = string.IsNullOrWhiteSpace(descricao) ? null : descricao.Trim();
        FotoUrl = string.IsNullOrWhiteSpace(fotoUrl) ? null : fotoUrl.Trim();
        AtualizadoEmUtc = DateTime.UtcNow;
    }

    public void AtualizarTaxaComissao(decimal? taxa)
    {
        if (taxa.HasValue && (taxa < 0 || taxa > 30)) throw new Exceptions.RegraDeNegocioException("Taxa deve estar entre 0 e 30%.");
        TaxaComissaoPercentual = taxa;
        AtualizadoEmUtc = DateTime.UtcNow;
    }

    public void DefinirHorarioFuncionamento(string abertura, string fechamento)
    {
        var horario = ValueObjects.HorarioFuncionamento.Parse(abertura, fechamento);
        HorarioFuncionamentoJson = horario.ToJson();
        AtualizadoEmUtc = DateTime.UtcNow;
    }
}
