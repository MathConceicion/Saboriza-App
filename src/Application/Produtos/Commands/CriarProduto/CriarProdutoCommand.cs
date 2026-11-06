namespace GastronomiaMarketplace.Application.Produtos.Commands.CriarProduto;

using MediatR;
using GastronomiaMarketplace.Application.Common.Models;

public sealed record CriarProdutoCommand(
    Guid LojaId,
    string Nome,
    string? Descricao,
    decimal Preco,
    int QuantidadeEmEstoque,
    string? ImagemUrl
) : IRequest<Result<Guid>>;
