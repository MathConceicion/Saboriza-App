namespace GastronomiaMarketplace.Domain.Entities;

using System.Text.Json;
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
    public string? Cnpj { get; private set; }
    public string? Telefone { get; private set; }
    public string? DiasFuncionamentoJson { get; private set; }
    public bool Aberta { get; private set; }
    public decimal? TaxaComissaoPercentual { get; private set; }

    private Loja() { }

    public Loja(Guid vendedorId, string nome, Guid categoriaId, string endereco, string cep, Point localizacao, string? fotoUrl = null, string? cnpj = null, string? telefone = null, string? diasFuncionamentoJson = null, bool aberta = true)
    {
        VendedorId = vendedorId;
        Nome = nome;
        CategoriaLojaId = categoriaId;
        EnderecoComercial = endereco;
        Cep = cep;
        Localizacao = localizacao;
        FotoUrl = fotoUrl?.Trim();
        Cnpj = string.IsNullOrWhiteSpace(cnpj) ? null : cnpj.Trim();
        Telefone = string.IsNullOrWhiteSpace(telefone) ? null : telefone.Trim();
        DiasFuncionamentoJson = diasFuncionamentoJson;
        Aberta = aberta;
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

    public void AtualizarPerfil(string nome, string? descricao, string? fotoUrl)
    {
        if (string.IsNullOrWhiteSpace(nome)) throw new Exceptions.RegraDeNegocioException("Nome da loja obrigatório.");
        Nome = nome.Trim();
        Descricao = string.IsNullOrWhiteSpace(descricao) ? null : descricao.Trim();
        FotoUrl = string.IsNullOrWhiteSpace(fotoUrl) ? null : fotoUrl.Trim();
        AtualizadoEmUtc = DateTime.UtcNow;
    }

    public void DefinirInformacoesLoja(string? cnpj, string? telefone, string? enderecoComercial, string? cep)
    {
        Cnpj = string.IsNullOrWhiteSpace(cnpj) ? null : cnpj.Trim();
        Telefone = string.IsNullOrWhiteSpace(telefone) ? null : telefone.Trim();
        if (!string.IsNullOrWhiteSpace(enderecoComercial)) EnderecoComercial = enderecoComercial.Trim();
        if (!string.IsNullOrWhiteSpace(cep)) Cep = cep.Trim();
        AtualizadoEmUtc = DateTime.UtcNow;
    }

    public void DefinirDiasFuncionamento(string? diasFuncionamentoJson)
    {
        if (!string.IsNullOrWhiteSpace(diasFuncionamentoJson))
        {
            using var doc = JsonDocument.Parse(diasFuncionamentoJson);
            if (doc.RootElement.ValueKind != JsonValueKind.Array) throw new Exceptions.RegraDeNegocioException("Dias de funcionamento deve ser uma lista.");
        }
        DiasFuncionamentoJson = diasFuncionamentoJson;
        AtualizadoEmUtc = DateTime.UtcNow;
    }

    public void AlternarStatusAberta()
    {
        Aberta = !Aberta;
        AtualizadoEmUtc = DateTime.UtcNow;
    }

    public void DefinirStatusAberta(bool aberta)
    {
        Aberta = aberta;
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
