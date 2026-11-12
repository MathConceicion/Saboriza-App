namespace GastronomiaMarketplace.Application.Lojas.Queries.ListarLojasAdmin;

using MediatR;
using Microsoft.EntityFrameworkCore;
using GastronomiaMarketplace.Application.Common.Interfaces;
using GastronomiaMarketplace.Application.Common.Models;

public sealed record ListarLojasAdminQuery(
    string? Busca,
    bool? Ativo,
    bool? Aprovado,
    int Pagina = 1,
    int Tamanho = 20
) : IRequest<PaginatedList<LojaAdminDto>>;

public sealed record LojaAdminDto(
    Guid Id,
    string Nome,
    string NomeProprietario,
    string EmailProprietario,
    string Categoria,
    string EnderecoComercial,
    bool Ativo,
    bool Aprovado,
    int TotalProdutos,
    int TotalVendas,
    decimal Faturamento,
    DateTime CriadoEmUtc
);
