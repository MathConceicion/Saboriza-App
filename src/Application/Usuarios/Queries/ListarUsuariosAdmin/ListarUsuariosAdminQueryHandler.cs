namespace GastronomiaMarketplace.Application.Usuarios.Queries.ListarUsuariosAdmin;

using MediatR;
using Microsoft.EntityFrameworkCore;
using GastronomiaMarketplace.Application.Common.Interfaces;
using GastronomiaMarketplace.Application.Common.Models;
using GastronomiaMarketplace.Domain.Enums;

public sealed class ListarUsuariosAdminQueryHandler : IRequestHandler<ListarUsuariosAdminQuery, PaginatedList<UsuarioAdminDto>>
{
    private readonly IAppDbContext _db;

    public ListarUsuariosAdminQueryHandler(IAppDbContext db) => _db = db;

    public async Task<PaginatedList<UsuarioAdminDto>> Handle(ListarUsuariosAdminQuery request, CancellationToken ct)
    {
        var query = _db.Usuarios
            .AsNoTracking()
            .Include(u => u.Vendedor)
            .Where(u => !u.IsAnonimizado && !u.IsDeleted);

        if (!string.IsNullOrWhiteSpace(request.Busca))
        {
            var busca = request.Busca.ToLower();
            query = query.Where(u => u.NomeCompleto.ToLower().Contains(busca) || u.Email.ToLower().Contains(busca));
        }

        if (!string.IsNullOrWhiteSpace(request.Papel))
        {
            var papelParse = int.TryParse(request.Papel, out var papelVal);
            if (papelParse) query = query.Where(u => u.Papel == (PapelUsuario)papelVal);
        }

        if (request.Bloqueado.HasValue)
        {
            query = request.Bloqueado.Value
                ? query.Where(u => u.IsBloqueado)
                : query.Where(u => !u.IsBloqueado);
        }

        query = query.OrderByDescending(u => u.CriadoEmUtc);

        var total = await query.CountAsync(ct);
        var items = await query
            .Skip((request.Pagina - 1) * request.Tamanho)
            .Take(request.Tamanho)
            .Select(u => new UsuarioAdminDto(
                u.Id,
                u.NomeCompleto,
                u.Email,
                u.Cpf,
                u.Papel.ToString(),
                u.IsBloqueado,
                u.BloqueadoEmUtc,
                u.MotivoBloqueio,
                u.CriadoEmUtc,
                u.Vendedor == null ? 0 : 1
            ))
            .ToListAsync(ct);

        return PaginatedList<UsuarioAdminDto>.Create(items, total, request.Pagina, request.Tamanho);
    }
}
