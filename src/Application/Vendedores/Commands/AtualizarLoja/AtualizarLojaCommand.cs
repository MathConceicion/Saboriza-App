namespace GastronomiaMarketplace.Application.Vendedores.Commands.AtualizarLoja;

using MediatR;
using GastronomiaMarketplace.Application.Common.Models;

public sealed record AtualizarLojaCommand(
    Guid LojaId,
    string Nome,
    string? Descricao,
    string? FotoUrl,
    string? HorarioAbertura = null,
    string? HorarioFechamento = null) : IRequest<Result<Guid>>;
