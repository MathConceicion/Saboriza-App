namespace GastronomiaMarketplace.Application.Usuarios.Queries.ListarUsuariosAdmin;

using MediatR;
using Microsoft.EntityFrameworkCore;
using GastronomiaMarketplace.Application.Common.Interfaces;
using GastronomiaMarketplace.Application.Common.Models;

public sealed record ListarUsuariosAdminQuery(
    string? Busca,
    string? Papel,
    bool? Bloqueado,
    int Pagina = 1,
    int Tamanho = 20
) : IRequest<PaginatedList<UsuarioAdminDto>>;

public sealed record UsuarioAdminDto(
    Guid Id,
    string NomeCompleto,
    string Email,
    string? Cpf,
    string Papel,
    bool IsBloqueado,
    DateTime? BloqueadoEmUtc,
    string? MotivoBloqueio,
    DateTime CriadoEmUtc,
    int TotalLojas
);
