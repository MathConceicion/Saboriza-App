namespace GastronomiaMarketplace.Domain.Entities;

using GastronomiaMarketplace.Domain.Common;

public sealed class CategoriaLoja : BaseEntity
{
    public string Nome { get; private set; } = null!;
    public string Slug { get; private set; } = null!;
    public string? IconeUrl { get; private set; }
    public int OrdemExibicao { get; private set; }
    public bool Ativo { get; private set; } = true;

    public ICollection<Loja> Lojas { get; private set; } = [];

    private CategoriaLoja() { }

    public CategoriaLoja(string nome, string slug, int ordemExibicao, string? iconeUrl = null)
    {
        Nome = nome;
        Slug = slug;
        OrdemExibicao = ordemExibicao;
        IconeUrl = iconeUrl;
        Ativo = true;
    }
}
