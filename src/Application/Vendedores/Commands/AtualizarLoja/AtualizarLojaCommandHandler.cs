namespace GastronomiaMarketplace.Application.Vendedores.Commands.AtualizarLoja;

using MediatR;
using Microsoft.EntityFrameworkCore;
using GastronomiaMarketplace.Application.Common.Interfaces;
using GastronomiaMarketplace.Application.Common.Models;

public sealed class AtualizarLojaCommandHandler : IRequestHandler<AtualizarLojaCommand, Result<Guid>>
{
    private readonly IAppDbContext _db;

    public AtualizarLojaCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Result<Guid>> Handle(AtualizarLojaCommand request, CancellationToken ct)
    {
        var loja = await _db.Lojas.FirstOrDefaultAsync(l => l.Id == request.LojaId, ct);
        if (loja is null) return Result<Guid>.Failure("Loja não encontrada.");
        try
        {
            loja.AtualizarPerfil(request.Nome, request.Descricao, request.FotoUrl);
            loja.DefinirInformacoesLoja(request.Cnpj, request.Telefone, request.EnderecoComercial, request.Cep);
            loja.DefinirDiasFuncionamento(request.DiasFuncionamentoJson);
            if (request.Aberta.HasValue) loja.DefinirStatusAberta(request.Aberta.Value);
            var temAbertura = !string.IsNullOrWhiteSpace(request.HorarioAbertura);
            var temFechamento = !string.IsNullOrWhiteSpace(request.HorarioFechamento);
            if (temAbertura != temFechamento) return Result<Guid>.Failure("Informe abertura e fechamento juntos.");
            if (temAbertura && temFechamento) loja.DefinirHorarioFuncionamento(request.HorarioAbertura!, request.HorarioFechamento!);
        }
        catch (Domain.Exceptions.RegraDeNegocioException ex)
        {
            return Result<Guid>.Failure(ex.Message);
        }
        await _db.SaveChangesAsync(ct);
        return Result<Guid>.Success(loja.Id);
    }
}
