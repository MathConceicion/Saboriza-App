namespace GastronomiaMarketplace.Application.Agenda.Queries.ListarAgendaDoDia;

using MediatR;

public sealed record ListarAgendaDoDiaQuery(Guid LojaId, Guid VendedorId, bool IsAdmin, DateTime? Data, Domain.Enums.StatusPedido? Status) : IRequest<IReadOnlyList<ItemAgendaDto>>;

public sealed record ItemAgendaDto(
    Guid AgendaId,
    Guid PedidoId,
    string ClienteNome,
    DateTime AprovadoEmUtc,
    Domain.Enums.StatusPedido Status,
    IReadOnlyList<string> ItensResumo,
    decimal ValorBruto,
    int PosicaoFila
);
