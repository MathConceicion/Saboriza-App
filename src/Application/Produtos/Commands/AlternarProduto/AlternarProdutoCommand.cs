namespace GastronomiaMarketplace.Application.Produtos.Commands.AlternarProduto;

using MediatR;
using GastronomiaMarketplace.Application.Common.Models;

public sealed record AlternarProdutoCommand(Guid ProdutoId, Guid LojaId, bool Ativo) : IRequest<Result<Guid>>;
