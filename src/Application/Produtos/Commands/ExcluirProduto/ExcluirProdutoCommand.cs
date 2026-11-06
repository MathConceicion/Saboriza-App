namespace GastronomiaMarketplace.Application.Produtos.Commands.ExcluirProduto;

using MediatR;
using GastronomiaMarketplace.Application.Common.Models;

public sealed record ExcluirProdutoCommand(Guid ProdutoId, Guid LojaId) : IRequest<Result<Guid>>;
