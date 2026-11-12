namespace GastronomiaMarketplace.Application.Pedidos.Commands.RecusarPedido;

using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using GastronomiaMarketplace.Application.Common.Interfaces;
using GastronomiaMarketplace.Application.Common.Models;
using GastronomiaMarketplace.Domain.Enums;

public sealed class RecusarPedidoCommandHandler : IRequestHandler<RecusarPedidoCommand, Result<Guid>>
{
    private readonly IAppDbContext _db;
    private readonly ILogger<RecusarPedidoCommandHandler> _logger;
    private readonly IClock _clock;

    public RecusarPedidoCommandHandler(IAppDbContext db, ILogger<RecusarPedidoCommandHandler> logger, IClock clock)
    {
        _db = db;
        _logger = logger;
        _clock = clock;
    }

    public async Task<Result<Guid>> Handle(RecusarPedidoCommand request, CancellationToken ct)
    {
        var pedido = await _db.Pedidos
            .Include(p => p.Loja)
            .FirstOrDefaultAsync(p => p.Id == request.PedidoId, ct);

        if (pedido is null)
            return Result<Guid>.Failure($"Pedido {request.PedidoId} não encontrado.");

        if (!request.IsAdmin)
        {
            if (request.AtorVendorId is null)
                return Result<Guid>.Failure("Usuário não possui vínculo de vendedor.");
            if (pedido.Loja.VendedorId != request.AtorVendorId.Value)
                return Result<Guid>.Failure("Vendedor não autorizado a recusar pedidos de outra loja.");
        }

        if (pedido.Status != StatusPedido.Pendente)
            return Result<Guid>.Failure($"Pedido não está pendente. Status atual: {pedido.Status}.");

        var motivo = string.IsNullOrWhiteSpace(request.Motivo) ? null : request.Motivo.Trim();
        pedido.Recusar(motivo, _clock.UtcNow);

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Conflito de concorrência ao recusar pedido {PedidoId}", request.PedidoId);
            return Result<Guid>.Failure("Conflito de concorrência: pedido foi modificado por outro processo. Tente novamente.");
        }

        _logger.LogInformation("Pedido {PedidoId} recusado por {Ator}", pedido.Id, request.AtorUsuarioId);
        return Result<Guid>.Success(pedido.Id);
    }
}
