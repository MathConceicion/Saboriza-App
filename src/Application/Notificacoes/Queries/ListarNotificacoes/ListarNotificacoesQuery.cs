namespace GastronomiaMarketplace.Application.Notificacoes.Queries.ListarNotificacoes;

using MediatR;
using Microsoft.EntityFrameworkCore;
using GastronomiaMarketplace.Application.Common.Interfaces;
using GastronomiaMarketplace.Application.Common.Models;

public sealed record ListarNotificacoesQuery(
    Guid UsuarioId,
    bool? ApenasNaoLidas,
    int Pagina = 1,
    int Tamanho = 20
) : IRequest<PaginatedList<NotificacaoDto>>;

public sealed record NotificacaoDto(
    Guid Id,
    string Titulo,
    string Mensagem,
    int Tipo,
    bool Lida,
    DateTime? LidaEmUtc,
    string? Url,
    DateTime CriadoEmUtc
);
