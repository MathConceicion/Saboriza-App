namespace GastronomiaMarketplace.Application.Lojas.Queries.ListarLojasProximas;

using MediatR;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using GastronomiaMarketplace.Application.Common.Interfaces;
using GastronomiaMarketplace.Application.Common.Models;

using GastronomiaMarketplace.Application.Common.Helpers;

public sealed class ListarLojasProximasQueryHandler : IRequestHandler<ListarLojasProximasQuery, PaginatedList<LojaProximaDto>>
{
    private readonly IAppDbContext _db;
    private readonly IClock _clock;

    public ListarLojasProximasQueryHandler(IAppDbContext db, IClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<PaginatedList<LojaProximaDto>> Handle(ListarLojasProximasQuery request, CancellationToken ct)
    {
        if (request.RaioKm <= 0 || request.RaioKm > 50)
            throw new Common.Exceptions.ValidationException([new FluentValidation.Results.ValidationFailure("RaioKm", "Raio deve estar entre 0 e 50 km.")]);
        var geometryFactory = NetTopologySuite.NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);
        var pontoCliente = geometryFactory.CreatePoint(new Coordinate(request.Longitude, request.Latitude));
        var raioMetros = request.RaioKm * 1000;
        var query = _db.Lojas
            .AsNoTracking()
            .Where(l => l.Ativo && l.Aprovado)
            .Where(l => l.Localizacao.IsWithinDistance(pontoCliente, raioMetros))
            .Where(l => request.CategoriaLojaId == null || l.CategoriaLojaId == request.CategoriaLojaId.Value);
        var total = await query.CountAsync(ct);
        var raw = await query
            .OrderBy(l => l.Localizacao.Distance(pontoCliente))
            .Skip((request.Pagina - 1) * request.TamanhoPagina)
            .Take(request.TamanhoPagina)
            .Select(l => new
            {
                l.Id,
                l.Nome,
                CategoriaNome = l.CategoriaLoja.Nome,
                l.EnderecoComercial,
                l.FotoUrl,
                l.Descricao,
                Distancia = l.Localizacao.Distance(pontoCliente) / 1000.0,
                Localizacao = l.Localizacao,
                l.HorarioFuncionamentoJson
            })
            .ToListAsync(ct);
        var agoraUtc = _clock.UtcNow;
        var items = raw.Select(x =>
        {
            var horario = HorarioHelper.ExtrairHorario(x.HorarioFuncionamentoJson);
            return new LojaProximaDto(
                x.Id,
                x.Nome,
                x.CategoriaNome,
                x.EnderecoComercial,
                x.Distancia,
                x.Localizacao.Y,
                x.Localizacao.X,
                HorarioHelper.AbertoAgora(x.HorarioFuncionamentoJson, agoraUtc),
                x.FotoUrl,
                x.Descricao,
                horario?.Abertura,
                horario?.Fechamento);
        }).ToList();
        return new PaginatedList<LojaProximaDto>(items, total, request.Pagina, request.TamanhoPagina);
    }
}
