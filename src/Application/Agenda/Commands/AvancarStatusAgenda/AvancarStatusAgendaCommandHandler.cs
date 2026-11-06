namespace GastronomiaMarketplace.Application.Agenda.Commands.AvancarStatusAgenda;

using MediatR;
using Microsoft.EntityFrameworkCore;
using GastronomiaMarketplace.Application.Common.Interfaces;
using GastronomiaMarketplace.Application.Common.Models;
using GastronomiaMarketplace.Domain.Enums;

public sealed class AvancarStatusAgendaCommandHandler : IRequestHandler<AvancarStatusAgendaCommand, Result>
{
    private readonly IAppDbContext _db;
    private readonly IClock _clock;

    public AvancarStatusAgendaCommandHandler(IAppDbContext db, IClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<Result> Handle(AvancarStatusAgendaCommand request, CancellationToken ct)
    {
        var item = await _db.ItensAgenda
            .Include(a => a.Pedido)
            .FirstOrDefaultAsync(a => a.Id == request.AgendaId, ct);
        if (item is null) return Result.Failure("Item da agenda não encontrado.");
        if (!request.IsAdmin && item.VendedorId != request.VendedorId)
            return Result.Failure("Vendedor não autorizado para esta agenda.");
        var agora = _clock.UtcNow;
        if (request.NovoStatus == StatusPedido.ProntoParaRetirada)
        {
            item.AvancarParaProntoRetirada(agora);
            item.Pedido.MarcarProntoParaRetirada();
        }
        else if (request.NovoStatus == StatusPedido.Finalizado)
        {
            item.Finalizar(agora);
            item.Pedido.Finalizar();
        }
        else
        {
            return Result.Failure("Transição de agenda inválida.");
        }
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
