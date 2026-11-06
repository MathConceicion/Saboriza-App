namespace GastronomiaMarketplace.Application.Lojas.Queries.ObterLojaPublica;

using MediatR;
using Microsoft.EntityFrameworkCore;
using GastronomiaMarketplace.Application.Common.Helpers;
using GastronomiaMarketplace.Application.Common.Interfaces;
using GastronomiaMarketplace.Application.Common.Models;

public sealed record ObterLojaPublicaQuery(Guid LojaId) : IRequest<Result<LojaPublicaDto>>;

public sealed record LojaPublicaDto(
    Guid LojaId,
    string Nome,
    string Categoria,
    string Endereco,
    string? Descricao,
    string? FotoUrl,
    string? HorarioAbertura,
    string? HorarioFechamento,
    bool? AbertoAgora);

public sealed class ObterLojaPublicaQueryHandler : IRequestHandler<ObterLojaPublicaQuery, Result<LojaPublicaDto>>
{
    private readonly IAppDbContext _db;
    private readonly IClock _clock;

    public ObterLojaPublicaQueryHandler(IAppDbContext db, IClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<Result<LojaPublicaDto>> Handle(ObterLojaPublicaQuery request, CancellationToken ct)
    {
        var bruto = await _db.Lojas
            .AsNoTracking()
            .Where(l => l.Id == request.LojaId && l.Ativo && l.Aprovado)
            .Select(l => new
            {
                l.Id,
                l.Nome,
                Categoria = l.CategoriaLoja.Nome,
                l.EnderecoComercial,
                l.Descricao,
                l.FotoUrl,
                l.HorarioFuncionamentoJson
            })
            .FirstOrDefaultAsync(ct);
        if (bruto is null) return Result<LojaPublicaDto>.Failure("Loja não encontrada.");
        var horario = HorarioHelper.ExtrairHorario(bruto.HorarioFuncionamentoJson);
        var dto = new LojaPublicaDto(
            bruto.Id,
            bruto.Nome,
            bruto.Categoria,
            bruto.EnderecoComercial,
            bruto.Descricao,
            bruto.FotoUrl,
            horario?.Abertura,
            horario?.Fechamento,
            HorarioHelper.AbertoAgora(bruto.HorarioFuncionamentoJson, _clock.UtcNow));
        return Result<LojaPublicaDto>.Success(dto);
    }
}
