namespace GastronomiaMarketplace.Application.Vendedores.Commands.SolicitarUpgrade;

using MediatR;
using Microsoft.EntityFrameworkCore;
using GastronomiaMarketplace.Application.Common.Interfaces;
using GastronomiaMarketplace.Application.Common.Models;
using GastronomiaMarketplace.Domain.Entities;
using GastronomiaMarketplace.Domain.Enums;

public sealed record SolicitarUpgradeCommand(
    Guid UsuarioId,
    string NomeLoja,
    Guid CategoriaLojaId,
    string EnderecoComercial,
    string Cep,
    string Telefone,
    string? Cnpj,
    string? FotoUrl = null,
    string? HorarioAbertura = null,
    string? HorarioFechamento = null
) : IRequest<Result<Guid>>;

public sealed class SolicitarUpgradeCommandHandler : IRequestHandler<SolicitarUpgradeCommand, Result<Guid>>
{
    private readonly IAppDbContext _db;
    private readonly IGeocodingService _geo;
    private readonly IViaCepService _viaCep;

    public SolicitarUpgradeCommandHandler(IAppDbContext db, IGeocodingService geo, IViaCepService viaCep)
    {
        _db = db;
        _geo = geo;
        _viaCep = viaCep;
    }

    public async Task<Result<Guid>> Handle(SolicitarUpgradeCommand request, CancellationToken ct)
    {
        var usuario = await _db.Usuarios.FirstOrDefaultAsync(u => u.Id == request.UsuarioId, ct);
        if (usuario is null) return Result<Guid>.Failure("Usuário não encontrado.");
        if (usuario.Papel == PapelUsuario.Vendedor) return Result<Guid>.Failure("Usuário já é vendedor.");
        if (usuario.Papel == PapelUsuario.Admin) return Result<Guid>.Failure("Admin não pode ser vendedor.");
        var categoriaExiste = await _db.CategoriasLoja.AnyAsync(c => c.Id == request.CategoriaLojaId, ct);
        if (!categoriaExiste) return Result<Guid>.Failure("Categoria inválida.");
        var nomeExiste = await _db.Vendedores.AnyAsync(v => v.NomeLoja.ToLower() == request.NomeLoja.ToLower(), ct);
        if (nomeExiste) return Result<Guid>.Failure("Já existe loja com este nome.");
        var enderecoViaCep = await _viaCep.ObterEnderecoPorCepAsync(request.Cep, ct);
        if (enderecoViaCep is null) return Result<Guid>.Failure("CEP n�o encontrado.");
        var coord = await _geo.GeocodificarAsync(request.EnderecoComercial, request.Cep, ct);
        if (coord is null) return Result<Guid>.Failure("Endereço não pôde ser geocodificado.");
        var temAbertura = !string.IsNullOrWhiteSpace(request.HorarioAbertura);
        var temFechamento = !string.IsNullOrWhiteSpace(request.HorarioFechamento);
        if (temAbertura != temFechamento) return Result<Guid>.Failure("Informe abertura e fechamento juntos.");
        string? horarioJson = null;
        if (temAbertura && temFechamento)
        {
            try
            {
                horarioJson = Domain.ValueObjects.HorarioFuncionamento.Parse(request.HorarioAbertura!, request.HorarioFechamento!).ToJson();
            }
            catch (Domain.Exceptions.RegraDeNegocioException ex)
            {
                return Result<Guid>.Failure(ex.Message);
            }
        }
        var vendedor = new Vendedor(
            usuarioId: request.UsuarioId,
            nomeLoja: request.NomeLoja,
            categoriaLojaId: request.CategoriaLojaId,
            enderecoComercial: request.EnderecoComercial,
            cep: request.Cep,
            telefone: request.Telefone,
            cnpj: request.Cnpj,
            latitude: coord.Latitude,
            longitude: coord.Longitude,
            fotoUrl: request.FotoUrl,
            horarioFuncionamentoJson: horarioJson
        );
        _db.Vendedores.Add(vendedor);
        usuario.PromoverParaVendedor();
        await _db.SaveChangesAsync(ct);
        return Result<Guid>.Success(vendedor.Id);
    }
}
