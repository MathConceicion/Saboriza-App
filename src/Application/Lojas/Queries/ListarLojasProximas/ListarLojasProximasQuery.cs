namespace GastronomiaMarketplace.Application.Lojas.Queries.ListarLojasProximas;

using MediatR;
using GastronomiaMarketplace.Application.Common.Models;

public sealed record ListarLojasProximasQuery(
    double Latitude,
    double Longitude,
    double RaioKm = 5.0,
    Guid? CategoriaLojaId = null,
    int Pagina = 1,
    int TamanhoPagina = 20
) : IRequest<PaginatedList<LojaProximaDto>>;

public sealed record LojaProximaDto(
    Guid LojaId,
    string Nome,
    string Categoria,
    string Endereco,
    double DistanciaKm,
    double Latitude,
    double Longitude,
    bool? AbertoAgora,
    string? FotoUrl = null,
    string? Descricao = null,
    string? HorarioAbertura = null,
    string? HorarioFechamento = null
);
