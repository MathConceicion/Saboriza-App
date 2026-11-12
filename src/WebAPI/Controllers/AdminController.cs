namespace GastronomiaMarketplace.WebAPI.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GastronomiaMarketplace.Domain.Enums;
using GastronomiaMarketplace.Infrastructure.Persistence;

[ApiController]
[Route("api/admin")]
public sealed class AdminController : ControllerBase
{
    private readonly AppDbContext _db;

    public AdminController(AppDbContext db) => _db = db;

    [HttpGet("vendedores")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> ListarVendedores([FromQuery] StatusVendedor? status, CancellationToken ct)
    {
        var query = _db.Vendedores.AsNoTracking().AsQueryable();
        if (status.HasValue) query = query.Where(v => v.Status == status.Value);
        var result = await query
            .OrderBy(v => v.CriadoEmUtc)
            .Select(v => new
            {
                vendedorId = v.Id,
                nomeLoja = v.NomeLoja,
                categoriaNome = v.CategoriaLoja.Nome,
                enderecoComercial = v.EnderecoComercial,
                cep = v.Cep,
                telefone = v.Telefone,
                cnpj = v.Cnpj,
                fotoUrl = v.FotoUrl,
                status = v.Status.ToString(),
                criadoEmUtc = v.CriadoEmUtc,
                usuarioEmail = v.Usuario.Email
            })
            .ToListAsync(ct);
        return Ok(result);
    }

    [HttpGet("produtos")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> ListarProdutos([FromQuery] bool? aprovado, CancellationToken ct)
    {
        var query = _db.Produtos.AsNoTracking().AsQueryable();
        if (aprovado.HasValue) query = query.Where(p => p.Aprovado == aprovado.Value);
        var result = await query
            .OrderBy(p => p.CriadoEmUtc)
            .Select(p => new
            {
                produtoId = p.Id,
                lojaId = p.LojaId,
                lojaNome = p.Loja.Nome,
                nome = p.Nome,
                descricao = p.Descricao,
                preco = p.Preco,
                quantidadeEmEstoque = p.QuantidadeEmEstoque,
                ativo = p.Ativo,
                aprovado = p.Aprovado
            })
            .ToListAsync(ct);
        return Ok(result);
    }

    [HttpPatch("vendedores/{vendedorId:guid}/aprovar")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> AprovarVendedor(Guid vendedorId, CancellationToken ct)
    {
        var vendedor = await _db.Vendedores.Include(v => v.CategoriaLoja).FirstOrDefaultAsync(v => v.Id == vendedorId, ct);
        if (vendedor is null) return NotFound(new { error = "Vendedor não encontrado." });
        vendedor.Aprovar();
        var factory = NetTopologySuite.NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);
        var point = factory.CreatePoint(new NetTopologySuite.Geometries.Coordinate(vendedor.Longitude, vendedor.Latitude));
        var loja = new Domain.Entities.Loja(vendedor.Id, vendedor.NomeLoja, vendedor.CategoriaLojaId, vendedor.EnderecoComercial, vendedor.Cep, point, vendedor.FotoUrl, vendedor.Cnpj, vendedor.Telefone, vendedor.DiasFuncionamentoJson, vendedor.Aberta);
        loja.Aprovar();
        if (!string.IsNullOrWhiteSpace(vendedor.HorarioFuncionamentoJson))
        {
            var horario = Domain.ValueObjects.HorarioFuncionamento.FromJson(vendedor.HorarioFuncionamentoJson);
            if (horario is not null) loja.DefinirHorarioFuncionamento(horario.Abertura.ToString("HH:mm"), horario.Fechamento.ToString("HH:mm"));
        }
        _db.Lojas.Add(loja);
        await _db.SaveChangesAsync(ct);
        return Ok(new { vendedorId = vendedor.Id, lojaId = loja.Id, status = "Aprovado" });
    }

    [HttpPatch("produtos/{produtoId:guid}/aprovar")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> AprovarProduto(Guid produtoId, CancellationToken ct)
    {
        var produto = await _db.Produtos.FirstOrDefaultAsync(p => p.Id == produtoId, ct);
        if (produto is null) return NotFound(new { error = "Produto não encontrado." });
        produto.Aprovar();
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpPatch("configuracoes/taxa-comissao")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> AtualizarTaxa([FromBody] AtualizarTaxaDto dto, CancellationToken ct)
    {
        if (dto.NovaTaxaPercentual < 0 || dto.NovaTaxaPercentual > 30) return BadRequest(new { error = "Taxa deve estar entre 0 e 30%." });
        var config = await _db.ConfiguracoesGlobais.FirstAsync(ct);
        config.AtualizarTaxa(dto.NovaTaxaPercentual, Guid.Empty);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpGet("audit-logs")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> ListarAuditLogs([FromQuery] string? entidade, [FromQuery] string? entidadeId, [FromQuery] Guid? usuarioId, CancellationToken ct)
    {
        var query = _db.AuditLogs.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(entidade)) query = query.Where(a => a.Entidade == entidade);
        if (!string.IsNullOrWhiteSpace(entidadeId)) query = query.Where(a => a.EntidadeId == entidadeId);
        if (usuarioId.HasValue) query = query.Where(a => a.UsuarioId == usuarioId.Value);
        var result = await query.OrderByDescending(a => a.DataUtc).Take(100).ToListAsync(ct);
        return Ok(result);
    }

    [HttpGet("relatorios/faturamento")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Faturamento([FromQuery] DateTime de, [FromQuery] DateTime ate, CancellationToken ct)
    {
        var baseQuery = _db.Pedidos.AsNoTracking().Where(p => p.Status == Domain.Enums.StatusPedido.Finalizado).Where(p => p.CriadoEmUtc >= de && p.CriadoEmUtc <= ate).Where(p => p.TaxaComissaoValor != null);
        var valores = await baseQuery.Select(p => new { Bruto = p.ValorBruto.Valor, Comissao = p.TaxaComissaoValor!.Valor, Liquido = p.ValorLiquidoVendedor!.Valor, p.LojaId, LojaNome = p.Loja.Nome }).ToListAsync(ct);
        var totalBruto = valores.Sum(x => x.Bruto);
        var totalComissao = valores.Sum(x => x.Comissao);
        var totalLiquido = valores.Sum(x => x.Liquido);
        var totalPedidos = valores.Count;
        var porLoja = valores.GroupBy(x => new { x.LojaId, x.LojaNome }).Select(g => new { lojaId = g.Key.LojaId, lojaNome = g.Key.LojaNome, bruto = g.Sum(x => x.Bruto), comissao = g.Sum(x => x.Comissao), liquido = g.Sum(x => x.Liquido), qtdPedidos = g.Count() }).OrderByDescending(x => x.bruto).ToList();
        return Ok(new { de, ate, faturamentoBrutoTotal = totalBruto, comissaoTotalRetida = totalComissao, repasseTotalVendedores = totalLiquido, totalPedidos, porLoja });
    }
}

public sealed record AtualizarTaxaDto(decimal NovaTaxaPercentual);
