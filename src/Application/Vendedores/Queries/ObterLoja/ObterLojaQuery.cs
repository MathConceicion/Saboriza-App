namespace GastronomiaMarketplace.Application.Vendedores.Queries.ObterLoja;

using MediatR;
using GastronomiaMarketplace.Application.Common.Models;

public sealed record ObterLojaQuery(Guid LojaId) : IRequest<Result<LojaDto>>;

public sealed record LojaDto(
    Guid LojaId,
    string Nome,
    string CategoriaNome,
    string EnderecoComercial,
    string Cep,
    string? Descricao,
    string? FotoUrl,
    bool Ativo,
    string? Cnpj,
    string? Telefone,
    bool Aberta,
    IReadOnlyList<string>? DiasFuncionamento = null,
    string? HorarioAbertura = null,
    string? HorarioFechamento = null,
    bool? AbertoAgora = null);
