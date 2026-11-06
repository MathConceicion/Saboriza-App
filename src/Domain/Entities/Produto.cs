namespace GastronomiaMarketplace.Domain.Entities;

using GastronomiaMarketplace.Domain.Common;
using GastronomiaMarketplace.Domain.Exceptions;

public sealed class Produto : BaseEntity
{
    public Guid LojaId { get; private set; }
    public Loja Loja { get; private set; } = null!;
    public Guid? CategoriaLojaId { get; private set; }
    public string Nome { get; private set; } = null!;
    public string? Descricao { get; private set; }
    public decimal Preco { get; private set; }
    public int QuantidadeEmEstoque { get; private set; }
    public bool Ativo { get; private set; }
    public bool Aprovado { get; private set; }
    public string? ImagemUrl { get; private set; }
    public uint xmin { get; private set; }

    private Produto() { }

    public Produto(Guid lojaId, string nome, string? descricao, decimal preco, int quantidadeEmEstoque, string? imagemUrl = null)
    {
        LojaId = lojaId;
        Nome = nome;
        Descricao = descricao;
        Preco = decimal.Round(preco, 2, MidpointRounding.AwayFromZero);
        QuantidadeEmEstoque = quantidadeEmEstoque;
        Ativo = true;
        Aprovado = false;
        ImagemUrl = imagemUrl;
    }

    public void DebitarEstoque(int quantidade)
    {
        if (quantidade <= 0) throw new RegraDeNegocioException("Quantidade deve ser positiva.");
        if (QuantidadeEmEstoque < quantidade) throw new RegraDeNegocioException($"Estoque insuficiente. Disponível: {QuantidadeEmEstoque}.");
        QuantidadeEmEstoque -= quantidade;
        AtualizadoEmUtc = DateTime.UtcNow;
    }

    public void EstornarEstoque(int quantidade)
    {
        if (quantidade <= 0) throw new RegraDeNegocioException("Quantidade deve ser positiva.");
        QuantidadeEmEstoque += quantidade;
        AtualizadoEmUtc = DateTime.UtcNow;
    }

    public void Aprovar()
    {
        Aprovado = true;
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

    public void AjustarEstoque(int quantidade)
    {
        if (quantidade < 0) throw new RegraDeNegocioException("Estoque não pode ser negativo.");
        QuantidadeEmEstoque = quantidade;
        AtualizadoEmUtc = DateTime.UtcNow;
    }

    public void Atualizar(string nome, string? descricao, decimal preco, string? imagemUrl)
    {
        Nome = nome;
        Descricao = descricao;
        Preco = decimal.Round(preco, 2, MidpointRounding.AwayFromZero);
        ImagemUrl = imagemUrl;
        AtualizadoEmUtc = DateTime.UtcNow;
    }
}
