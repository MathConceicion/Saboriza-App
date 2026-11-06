namespace GastronomiaMarketplace.Application.Usuarios.Queries.ExportarDados;

using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using GastronomiaMarketplace.Application.Common.Interfaces;
using GastronomiaMarketplace.Application.Common.Models;

public sealed record ExportarDadosPortabilidadeQuery(Guid UsuarioId, Guid SolicitanteId, bool IsAdmin) : IRequest<Result<string>>;

public sealed class ExportarDadosPortabilidadeQueryHandler : IRequestHandler<ExportarDadosPortabilidadeQuery, Result<string>>
{
    private readonly IAppDbContext _db;

    public ExportarDadosPortabilidadeQueryHandler(IAppDbContext db) => _db = db;

    public async Task<Result<string>> Handle(ExportarDadosPortabilidadeQuery request, CancellationToken ct)
    {
        if (request.UsuarioId != request.SolicitanteId && !request.IsAdmin)
            return Result<string>.Failure("Apenas titular ou Admin.");
        var usuario = await _db.Usuarios
            .AsNoTracking()
            .Include(u => u.Vendedor)
            .FirstOrDefaultAsync(u => u.Id == request.UsuarioId, ct);
        if (usuario is null) return Result<string>.Failure("Usuário não encontrado.");
        if (usuario.IsAnonimizado) return Result<string>.Failure("Dados já anonimizados.");
        var consentimentos = await _db.ConsentimentosLgpd
            .AsNoTracking().Where(c => c.UsuarioId == request.UsuarioId).ToListAsync(ct);
        var pedidos = await _db.Pedidos
            .AsNoTracking()
            .Where(p => p.ClienteId == request.UsuarioId)
            .Include(p => p.Itens)
            .Include(p => p.Loja)
            .OrderByDescending(p => p.CriadoEmUtc)
            .ToListAsync(ct);
        var export = new
        {
            exportadoEm = DateTime.UtcNow,
            titular = new
            {
                id = usuario.Id,
                nomeCompleto = usuario.NomeCompleto,
                email = usuario.Email,
                cpf = usuario.Cpf,
                telefone = usuario.Telefone,
                enderecoAtual = usuario.EnderecoAtual,
                criadoEm = usuario.CriadoEmUtc
            },
            dadosSaude = new
            {
                alergiasSeveras = usuario.AlergiasSeveras,
                restricoesAlimentares = usuario.RestricoesAlimentares,
                observacoesSaude = usuario.ObservacoesSaude
            },
            consentimentos = consentimentos.Select(c => new
            {
                tipo = c.Tipo.ToString(),
                concedidoEm = c.ConcedidoEmUtc,
                revogadoEm = c.RevogadoEmUtc,
                ativo = c.Ativo,
                textoConsentimento = c.TextoConsentimento
            }),
            pedidos = pedidos.Select(p => new
            {
                id = p.Id,
                loja = p.Loja.Nome,
                status = p.Status.ToString(),
                criadoEm = p.CriadoEmUtc,
                valorBruto = p.ValorBruto.Valor,
                itens = p.Itens.Select(i => new
                {
                    produto = i.ProdutoNomeSnapshot,
                    quantidade = i.Quantidade,
                    precoUnitario = i.PrecoUnitario.Valor
                })
            }),
            vendedor = usuario.Vendedor is null ? null : new
            {
                nomeLoja = usuario.Vendedor.NomeLoja,
                enderecoComercial = usuario.Vendedor.EnderecoComercial,
                status = usuario.Vendedor.Status.ToString()
            }
        };
        var json = JsonSerializer.Serialize(export, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });
        return Result<string>.Success(json);
    }
}
