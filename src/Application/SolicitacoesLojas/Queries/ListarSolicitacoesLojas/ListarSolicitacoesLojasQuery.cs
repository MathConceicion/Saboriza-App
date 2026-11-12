namespace GastronomiaMarketplace.Application.SolicitacoesLojas.Queries.ListarSolicitacoesLojas;

using MediatR;
using GastronomiaMarketplace.Application.Common.Models;

public sealed record ListarSolicitacoesLojasQuery(
    string? Status,
    string? Busca,
    int Pagina = 1,
    int Tamanho = 20
) : IRequest<PaginatedList<SolicitacaoLojaDto>>;

public sealed record SolicitacaoLojaDto(
    Guid VendedorId,
    string NomeLoja,
    string NomeProprietario,
    string EmailProprietario,
    string Categoria,
    string EnderecoComercial,
    string Cep,
    string Telefone,
    string? Cnpj,
    string? FotoUrl,
    StatusVendedorDto Status,
    string? MotivoRejeicao,
    DateTime CriadoEmUtc,
    DateTime? AtualizadoEmUtc
);

public sealed record StatusVendedorDto(int Valor, string Nome);
