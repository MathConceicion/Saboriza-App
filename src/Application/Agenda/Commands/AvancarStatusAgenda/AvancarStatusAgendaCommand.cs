namespace GastronomiaMarketplace.Application.Agenda.Commands.AvancarStatusAgenda;

using MediatR;
using GastronomiaMarketplace.Application.Common.Models;
using GastronomiaMarketplace.Domain.Enums;

public sealed record AvancarStatusAgendaCommand(Guid AgendaId, Guid VendedorId, bool IsAdmin, StatusPedido NovoStatus) : IRequest<Result>;
