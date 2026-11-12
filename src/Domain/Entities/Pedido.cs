namespace GastronomiaMarketplace.Domain.Entities;

using GastronomiaMarketplace.Domain.Common;
using GastronomiaMarketplace.Domain.Enums;
using GastronomiaMarketplace.Domain.Events;
using GastronomiaMarketplace.Domain.Exceptions;
using GastronomiaMarketplace.Domain.ValueObjects;

public sealed class Pedido : AggregateRoot
{
    public Guid ClienteId { get; private set; }
    public Usuario Cliente { get; private set; } = null!;
    public Guid LojaId { get; private set; }
    public Loja Loja { get; private set; } = null!;
    public StatusPedido Status { get; private set; }
    public Dinheiro ValorBruto { get; private set; } = Dinheiro.Zero("BRL");
    public decimal? TaxaComissaoPercentual { get; private set; }
    public Dinheiro? TaxaComissaoValor { get; private set; }
    public Dinheiro? ValorLiquidoVendedor { get; private set; }
    public string? PixPayload { get; private set; }
    public DateTime? PixSimuladoEmUtc { get; private set; }
    public DateTime? AprovadoEmUtc { get; private set; }
    public string? MotivoCancelamento { get; private set; }
    public string? ClienteNomeSnapshot { get; private set; }

    private readonly List<ItemPedido> _itens = [];
    public IReadOnlyCollection<ItemPedido> Itens => _itens.AsReadOnly();

    private Pedido() { }

    private Pedido(Guid clienteId, Guid lojaId, List<ItemPedido> itens, string? clienteNomeSnapshot = null)
    {
        ClienteId = clienteId;
        LojaId = lojaId;
        Status = StatusPedido.Pendente;
        _itens = itens;
        ClienteNomeSnapshot = clienteNomeSnapshot;
        RecalcularTotais();
        AddDomainEvent(new PedidoCriadoEvent(Id, LojaId, ClienteId));
    }

    public static Pedido Criar(Guid clienteId, Guid lojaId, List<ItemPedido> itens, string? clienteNomeSnapshot = null)
    {
        if (itens.Count == 0) throw new RegraDeNegocioException("Pedido deve conter ao menos um item.");
        if (itens.Any(i => !i.ProdutoAtivo)) throw new RegraDeNegocioException("Produto inativo não pode ser pedido.");
        return new Pedido(clienteId, lojaId, itens, clienteNomeSnapshot);
    }

    public void Aprovar(Guid atorVendorId, DateTime aprovadoEmUtc)
    {
        if (Status != StatusPedido.Pendente)
            throw new RegraDeNegocioException($"Transição inválida: {Status} -> EmPreparo.");
        Status = StatusPedido.EmPreparo;
        AprovadoEmUtc = aprovadoEmUtc;
        AtualizadoEmUtc = aprovadoEmUtc;
        AddDomainEvent(new PedidoAprovadoEvent(Id, LojaId, aprovadoEmUtc));
    }

    public void Recusar(string? motivo, DateTime recusadoEmUtc)
    {
        if (Status != StatusPedido.Pendente)
            throw new RegraDeNegocioException($"Pedido só pode ser recusado enquanto Pendente. Status atual: {Status}.");
        Status = StatusPedido.Cancelado;
        MotivoCancelamento = motivo;
        AtualizadoEmUtc = recusadoEmUtc;
    }

    public void MarcarProntoParaRetirada()
    {
        if (Status != StatusPedido.EmPreparo)
            throw new RegraDeNegocioException($"Transição inválida: {Status} -> ProntoParaRetirada.");
        Status = StatusPedido.ProntoParaRetirada;
        AtualizadoEmUtc = DateTime.UtcNow;
    }

    public void Finalizar()
    {
        if (Status != StatusPedido.ProntoParaRetirada)
            throw new RegraDeNegocioException($"Transição inválida: {Status} -> Finalizado.");
        Status = StatusPedido.Finalizado;
        AtualizadoEmUtc = DateTime.UtcNow;
    }

    public void Cancelar(string? motivo = null)
    {
        if (Status == StatusPedido.Finalizado || Status == StatusPedido.Cancelado)
            throw new RegraDeNegocioException($"Pedido não pode ser cancelado a partir de {Status}.");
        Status = StatusPedido.Cancelado;
        MotivoCancelamento = motivo;
        AtualizadoEmUtc = DateTime.UtcNow;
    }

    public void RegistrarPagamentoPix(decimal taxaPercentual, string pixPayload)
    {
        if (Status != StatusPedido.EmPreparo && Status != StatusPedido.Pendente)
            throw new RegraDeNegocioException("PIX só pode ser simulado para pedido Pendente ou Em Preparo.");
        if (TaxaComissaoValor is not null)
            throw new RegraDeNegocioException("Comissão já calculada.");
        var bruto = ValorBruto.Valor;
        var comissaoValor = decimal.Round(bruto * (taxaPercentual / 100m), 2, MidpointRounding.AwayFromZero);
        var liquido = bruto - comissaoValor;
        TaxaComissaoPercentual = taxaPercentual;
        TaxaComissaoValor = new Dinheiro(comissaoValor, "BRL");
        ValorLiquidoVendedor = new Dinheiro(liquido, "BRL");
        PixPayload = pixPayload;
        PixSimuladoEmUtc = DateTime.UtcNow;
        if (TaxaComissaoValor.Valor + ValorLiquidoVendedor.Valor != ValorBruto.Valor)
            throw new InvalidOperationException("Invariante financeiro violado: bruto != comissao + liquido");
    }

    public void AnonimizarSnapshotCliente(string hash)
    {
        ClienteNomeSnapshot = $"Anonimo {hash}";
    }

    private void RecalcularTotais()
    {
        ValorBruto = new Dinheiro(_itens.Sum(i => i.Subtotal.Valor), "BRL");
    }
}
