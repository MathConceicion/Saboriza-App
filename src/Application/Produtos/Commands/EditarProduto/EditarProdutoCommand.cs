namespace GastronomiaMarketplace.Application.Produtos.Commands.EditarProduto;

using MediatR;
using GastronomiaMarketplace.Application.Common.Models;

public sealed record EditarProdutoCommand(
    Guid ProdutoId,
    Guid LojaId,
    string Nome,
    string? Descricao,
    decimal Preco,
    int QuantidadeEmEstoque,
    string? ImagemUrl) : IRequest<Result<Guid>>;
