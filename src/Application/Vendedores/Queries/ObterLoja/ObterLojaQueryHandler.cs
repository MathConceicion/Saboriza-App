namespace GastronomiaMarketplace.Application.Vendedores.Queries.ObterLoja;

using MediatR;
using Microsoft.EntityFrameworkCore;
using GastronomiaMarketplace.Application.Common.Interfaces;
using GastronomiaMarketplace.Application.Common.Models;

using GastronomiaMarketplace.Application.Common.Helpers;

public sealed class ObterLojaQueryHandler : IRequestHandler<ObterLojaQuery, Result<LojaDto>>
{
    private readonly IAppDbContext _db;
    private readonly IClock _clock;

    public ObterLojaQueryHandler(IAppDbContext db, IClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<Result<LojaDto>> Handle(ObterLojaQuery request, CancellationToken ct)
    {
        var bruto = await _db.Lojas
            .AsNoTracking()
            .Where(l => l.Id == request.LojaId)
            .Select(l => new
            {
                l.Id,
                l.Nome,
                CategoriaNome = l.CategoriaLoja.Nome,
                l.EnderecoComercial,
                l.Cep,
                l.Descricao,
                l.FotoUrl,
                l.Ativo,
                l.HorarioFuncionamentoJson
            })
            .FirstOrDefaultAsync(ct);
        if (bruto is null) return Result<LojaDto>.Failure("Loja não encontrada.");
        var horario = HorarioHelper.ExtrairHorario(bruto.HorarioFuncionamentoJson);
        var dto = new LojaDto(
            bruto.Id,
            bruto.Nome,
            bruto.CategoriaNome,
            bruto.EnderecoComercial,
            bruto.Cep,
            bruto.Descricao,
            bruto.FotoUrl,
            bruto.Ativo,
            horario?.Abertura,
            horario?.Fechamento,
            HorarioHelper.AbertoAgora(bruto.HorarioFuncionamentoJson, _clock.UtcNow));
        return Result<LojaDto>.Success(dto);
    }
}
