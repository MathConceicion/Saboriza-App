namespace GastronomiaMarketplace.Domain.Entities;

using GastronomiaMarketplace.Domain.Common;
using GastronomiaMarketplace.Domain.Enums;
using GastronomiaMarketplace.Domain.Exceptions;

public sealed class ItemAgenda : BaseEntity
{
    public Guid PedidoId { get; private set; }
    public Pedido Pedido { get; private set; } = null!;
    public Guid LojaId { get; private set; }
    public Guid VendedorId { get; private set; }
    public StatusPedido Status { get; private set; }
    public DateTime AprovadoEmUtc { get; private set; }
    public DateTime? ProntoEmUtc { get; private set; }

    private ItemAgenda() { }

    private ItemAgenda(Guid pedidoId, Guid lojaId, Guid vendedorId, DateTime aprovadoEmUtc)
    {
        PedidoId = pedidoId;
        LojaId = lojaId;
        VendedorId = vendedorId;
        Status = StatusPedido.EmPreparo;
        AprovadoEmUtc = aprovadoEmUtc;
        CriadoEmUtc = aprovadoEmUtc;
    }

    public static ItemAgenda Criar(Guid pedidoId, Guid lojaId, Guid vendedorId, DateTime aprovadoEmUtc)
    {
        if (pedidoId == Guid.Empty) throw new RegraDeNegocioException("PedidoId obrigatório para agenda.");
        if (lojaId == Guid.Empty) throw new RegraDeNegocioException("LojaId obrigatório para agenda.");
        return new ItemAgenda(pedidoId, lojaId, vendedorId, aprovadoEmUtc);
    }

    public void AvancarParaProntoRetirada(DateTime prontoEmUtc)
    {
        if (Status != StatusPedido.EmPreparo)
            throw new RegraDeNegocioException($"Agenda só avança de EmPreparo para ProntoParaRetirada. Atual: {Status}");
        Status = StatusPedido.ProntoParaRetirada;
        ProntoEmUtc = prontoEmUtc;
        AtualizadoEmUtc = prontoEmUtc;
    }

    public void Finalizar(DateTime finalizadoEmUtc)
    {
        if (Status != StatusPedido.ProntoParaRetirada)
            throw new RegraDeNegocioException($"Agenda só finaliza de ProntoParaRetirada. Atual: {Status}");
        Status = StatusPedido.Finalizado;
        AtualizadoEmUtc = finalizadoEmUtc;
    }

    public void Cancelar()
    {
        Status = StatusPedido.Cancelado;
        AtualizadoEmUtc = DateTime.UtcNow;
    }
}
