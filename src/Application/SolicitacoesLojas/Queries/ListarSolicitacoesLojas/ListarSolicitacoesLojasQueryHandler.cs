namespace GastronomiaMarketplace.Application.SolicitacoesLojas.Queries.ListarSolicitacoesLojas;

using MediatR;
using Microsoft.EntityFrameworkCore;
using GastronomiaMarketplace.Application.Common.Interfaces;
using GastronomiaMarketplace.Application.Common.Models;
using GastronomiaMarketplace.Domain.Enums;

public sealed class ListarSolicitacoesLojasQueryHandler : IRequestHandler<ListarSolicitacoesLojasQuery, PaginatedList<SolicitacaoLojaDto>>
{
    private readonly IAppDbContext _db;

    public ListarSolicitacoesLojasQueryHandler(IAppDbContext db) => _db = db;

    public async Task<PaginatedList<SolicitacaoLojaDto>> Handle(ListarSolicitacoesLojasQuery request, CancellationToken ct)
    {
        var query = _db.Vendedores
            .AsNoTracking()
            .Include(v => v.Usuario)
            .Include(v => v.CategoriaLoja)
            .Where(v => v.Status == StatusVendedor.PendenteAprovacao || v.Status == StatusVendedor.EmAnalise || v.Status == StatusVendedor.Aprovado || v.Status == StatusVendedor.Rejeitado);

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            var statusParse = int.TryParse(request.Status, out var statusVal);
            if (statusParse) query = query.Where(v => v.Status == (StatusVendedor)statusVal);
        }

        if (!string.IsNullOrWhiteSpace(request.Busca))
        {
            var busca = request.Busca.ToLower();
            query = query.Where(v =>
                v.NomeLoja.ToLower().Contains(busca) ||
                v.Usuario.NomeCompleto.ToLower().Contains(busca) ||
                v.Usuario.Email.ToLower().Contains(busca));
        }

        query = query.OrderByDescending(v => v.CriadoEmUtc);

        var total = await query.CountAsync(ct);
        var items = await query
            .Skip((request.Pagina - 1) * request.Tamanho)
            .Take(request.Tamanho)
            .Select(v => new SolicitacaoLojaDto(
                v.Id,
                v.NomeLoja,
                v.Usuario.NomeCompleto,
                v.Usuario.Email,
                v.CategoriaLoja.Nome,
                v.EnderecoComercial,
                v.Cep,
                v.Telefone,
                v.Cnpj,
                v.FotoUrl,
                new StatusVendedorDto((int)v.Status, v.Status.ToString()),
                v.MotivoRejeicao,
                v.CriadoEmUtc,
                v.AtualizadoEmUtc
            ))
            .ToListAsync(ct);

        return PaginatedList<SolicitacaoLojaDto>.Create(items, total, request.Pagina, request.Tamanho);
    }
}
