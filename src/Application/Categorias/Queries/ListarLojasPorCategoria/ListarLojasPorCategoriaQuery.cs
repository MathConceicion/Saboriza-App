namespace GastronomiaMarketplace.Application.Categorias.Queries.ListarLojasPorCategoria;

using MediatR;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using GastronomiaMarketplace.Application.Common.Interfaces;
using GastronomiaMarketplace.Application.Lojas.Queries.ListarLojasProximas;

public sealed record ListarLojasPorCategoriaQuery(string Slug, double? Lat, double? Lng, double RaioKm = 50, int Pagina = 1, int Tamanho = 20) : IRequest<Common.Models.PaginatedList<LojaProximaDto>>;

public sealed class ListarLojasPorCategoriaQueryHandler : IRequestHandler<ListarLojasPorCategoriaQuery, Common.Models.PaginatedList<LojaProximaDto>>
{
    private readonly IAppDbContext _db;
    private readonly Common.Interfaces.IClock _clock;
    public ListarLojasPorCategoriaQueryHandler(IAppDbContext db, Common.Interfaces.IClock clock)
    {
        _db = db;
        _clock = clock;
    }
    public async Task<Common.Models.PaginatedList<LojaProximaDto>> Handle(ListarLojasPorCategoriaQuery request, CancellationToken ct)
    {
        var categoria = await _db.CategoriasLoja.AsNoTracking().FirstOrDefaultAsync(c => c.Slug == request.Slug, ct);
        if (categoria is null) throw new Common.Exceptions.NotFoundException($"Categoria {request.Slug} não encontrada.");
        var query = _db.Lojas.AsNoTracking().Include(l => l.CategoriaLoja).Where(l => l.Ativo && l.Aprovado && l.CategoriaLojaId == categoria.Id);
        if (request.Lat.HasValue && request.Lng.HasValue)
        {
            var factory = NetTopologySuite.NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);
            var ponto = factory.CreatePoint(new Coordinate(request.Lng.Value, request.Lat.Value));
            var raioMetros = request.RaioKm * 1000;
            var filtrada = query.Where(l => l.Localizacao.IsWithinDistance(ponto, raioMetros));
            var totalComDistancia = await filtrada.CountAsync(ct);
            var brutoComDistancia = await filtrada
                .OrderBy(l => l.Localizacao.Distance(ponto))
                .Skip((request.Pagina - 1) * request.Tamanho)
                .Take(request.Tamanho)
                .Select(l => new
                {
                    l.Id,
                    l.Nome,
                    CategoriaNome = l.CategoriaLoja.Nome,
                    l.EnderecoComercial,
                    l.FotoUrl,
                    l.Descricao,
                    Distancia = l.Localizacao.Distance(ponto) / 1000.0,
                    Localizacao = l.Localizacao,
                    l.HorarioFuncionamentoJson
                })
                .ToListAsync(ct);
            var agoraUtc = _clock.UtcNow;
            var itensComDistancia = brutoComDistancia.Select(x =>
            {
                var horario = Common.Helpers.HorarioHelper.ExtrairHorario(x.HorarioFuncionamentoJson);
                return new LojaProximaDto(
                    x.Id,
                    x.Nome,
                    x.CategoriaNome,
                    x.EnderecoComercial,
                    x.Distancia,
                    x.Localizacao.Y,
                    x.Localizacao.X,
                    Common.Helpers.HorarioHelper.AbertoAgora(x.HorarioFuncionamentoJson, agoraUtc),
                    x.FotoUrl,
                    x.Descricao,
                    horario?.Abertura,
                    horario?.Fechamento);
            }).ToList();
            return new Common.Models.PaginatedList<LojaProximaDto>(itensComDistancia, totalComDistancia, request.Pagina, request.Tamanho);
        }
        var total = await query.CountAsync(ct);
        var bruto = await query
            .OrderBy(l => l.Nome)
            .Skip((request.Pagina - 1) * request.Tamanho)
            .Take(request.Tamanho)
            .ToListAsync(ct);
        var agoraUtcSemDistancia = _clock.UtcNow;
        var itens = bruto.Select(l =>
        {
            var horario = Common.Helpers.HorarioHelper.ExtrairHorario(l.HorarioFuncionamentoJson);
            return new LojaProximaDto(
                l.Id,
                l.Nome,
                l.CategoriaLoja.Nome,
                l.EnderecoComercial,
                0,
                l.Localizacao.Y,
                l.Localizacao.X,
                Common.Helpers.HorarioHelper.AbertoAgora(l.HorarioFuncionamentoJson, agoraUtcSemDistancia),
                l.FotoUrl,
                l.Descricao,
                horario?.Abertura,
                horario?.Fechamento);
        }).ToList();
        return new Common.Models.PaginatedList<LojaProximaDto>(itens, total, request.Pagina, request.Tamanho);
    }
}
