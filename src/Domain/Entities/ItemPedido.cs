namespace GastronomiaMarketplace.Domain.Entities;

using GastronomiaMarketplace.Domain.Common;
using GastronomiaMarketplace.Domain.ValueObjects;

public sealed class ItemPedido : BaseEntity
{
    public Guid PedidoId { get; private set; }
    public Pedido Pedido { get; private set; } = null!;
    public Guid ProdutoId { get; private set; }
    public string ProdutoNomeSnapshot { get; private set; } = null!;
    public Dinheiro PrecoUnitario { get; private set; } = Dinheiro.Zero("BRL");
    public int Quantidade { get; private set; }
    public Dinheiro Subtotal { get; private set; } = Dinheiro.Zero("BRL");
    public bool ProdutoAtivo { get; private set; }

    private ItemPedido() { }

    public ItemPedido(Guid produtoId, string produtoNomeSnapshot, decimal precoUnitario, int quantidade, bool produtoAtivo = true)
    {
        ProdutoId = produtoId;
        ProdutoNomeSnapshot = produtoNomeSnapshot;
        PrecoUnitario = new Dinheiro(precoUnitario, "BRL");
        Quantidade = quantidade;
        Subtotal = new Dinheiro(precoUnitario * quantidade, "BRL");
        ProdutoAtivo = produtoAtivo;
    }
}
