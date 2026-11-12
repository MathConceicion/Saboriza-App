namespace GastronomiaMarketplace.Application.Produtos.Queries.ListarProdutosAdmin;

using MediatR;
using Microsoft.EntityFrameworkCore;
using GastronomiaMarketplace.Application.Common.Interfaces;
using GastronomiaMarketplace.Application.Common.Models;

public sealed record ListarProdutosAdminQuery(
    string? Busca,
    Guid? LojaId,
    bool? Ativo,
    bool? Aprovado,
    int Pagina = 1,
    int Tamanho = 20
) : IRequest<PaginatedList<ProdutoAdminDto>>;

public sealed record ProdutoAdminDto(
    Guid Id,
    string Nome,
    string? Descricao,
    decimal Preco,
    int QuantidadeEmEstoque,
    bool Ativo,
    bool Aprovado,
    Guid LojaId,
    string LojaNome,
    string? ImagemUrl,
    DateTime CriadoEmUtc
);
