namespace GastronomiaMarketplace.WebAPI.Controllers;

using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GastronomiaMarketplace.Application.Common.Interfaces;
using GastronomiaMarketplace.Application.SolicitacoesLojas.Queries.ListarSolicitacoesLojas;
using GastronomiaMarketplace.Application.SolicitacoesLojas.Commands.AprovarSolicitacaoLoja;
using GastronomiaMarketplace.Application.SolicitacoesLojas.Commands.RecusarSolicitacaoLoja;
using GastronomiaMarketplace.Application.SolicitacoesLojas.Commands.DefinirEmAnalise;
using GastronomiaMarketplace.Application.Usuarios.Queries.ListarUsuariosAdmin;
using GastronomiaMarketplace.Application.Usuarios.Commands.BloquearUsuario;
using GastronomiaMarketplace.Application.Usuarios.Commands.DesbloquearUsuario;
using GastronomiaMarketplace.Application.Lojas.Queries.ListarLojasAdmin;
using GastronomiaMarketplace.Application.Lojas.Commands.SuspenderLoja;
using GastronomiaMarketplace.Application.Lojas.Commands.ReativarLoja;
using GastronomiaMarketplace.Application.Produtos.Queries.ListarProdutosAdmin;
using GastronomiaMarketplace.Application.Produtos.Commands.DesativarProdutoAdmin;
using GastronomiaMarketplace.Application.Produtos.Commands.AtivarProdutoAdmin;
using GastronomiaMarketplace.Application.Dashboard.Queries.EstatisticasDashboard;
using GastronomiaMarketplace.Application.Dashboard.Queries.CrescimentoDashboard;
using GastronomiaMarketplace.Application.Notificacoes.Queries.ListarNotificacoes;
using GastronomiaMarketplace.Application.Notificacoes.Queries.ContarNaoLidas;
using GastronomiaMarketplace.Application.Notificacoes.Commands.MarcarNotificacaoLida;
using GastronomiaMarketplace.Domain.Enums;
using GastronomiaMarketplace.Infrastructure.Persistence;

[ApiController]
[Route("api/admin")]
public sealed class AdminController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IMediator _mediator;
    private readonly ICurrentUser _currentUser;

    public AdminController(AppDbContext db, IMediator mediator, ICurrentUser currentUser)
    {
        _db = db;
        _mediator = mediator;
        _currentUser = currentUser;
    }

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
        var loja = new Domain.Entities.Loja(vendedor.Id, vendedor.NomeLoja, vendedor.CategoriaLojaId, vendedor.EnderecoComercial, vendedor.Cep, point, vendedor.FotoUrl);
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
        config.AtualizarTaxa(dto.NovaTaxaPercentual, _currentUser.UserId ?? Guid.Empty);
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

    [HttpGet("solicitacoes-lojas")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> ListarSolicitacoesLojas(
        [FromQuery] string? status,
        [FromQuery] string? busca,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanho = 20,
        CancellationToken ct = default)
    {
        var query = new ListarSolicitacoesLojasQuery(status, busca, pagina, tamanho);
        var result = await _mediator.Send(query, ct);
        return Ok(result);
    }

    [HttpPatch("solicitacoes-lojas/{vendedorId:guid}/aprovar")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> AprovarSolicitacaoLoja(Guid vendedorId, CancellationToken ct)
    {
        var cmd = new AprovarSolicitacaoLojaCommand(vendedorId, _currentUser.UserId ?? Guid.Empty);
        var result = await _mediator.Send(cmd, ct);
        return result.IsSuccess ? Ok(new { vendedorId = result.Value }) : BadRequest(new { error = result.Error });
    }

    [HttpPatch("solicitacoes-lojas/{vendedorId:guid}/recusar")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> RecusarSolicitacaoLoja(Guid vendedorId, [FromBody] RecusarSolicitacaoDto dto, CancellationToken ct)
    {
        var cmd = new RecusarSolicitacaoLojaCommand(vendedorId, dto.Motivo, _currentUser.UserId ?? Guid.Empty);
        var result = await _mediator.Send(cmd, ct);
        return result.IsSuccess ? NoContent() : BadRequest(new { error = result.Error });
    }

    [HttpPatch("solicitacoes-lojas/{vendedorId:guid}/analise")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> DefinirEmAnaliseSolicitacaoLoja(Guid vendedorId, CancellationToken ct)
    {
        var cmd = new DefinirEmAnaliseCommand(vendedorId, _currentUser.UserId ?? Guid.Empty);
        var result = await _mediator.Send(cmd, ct);
        return result.IsSuccess ? NoContent() : BadRequest(new { error = result.Error });
    }

    [HttpGet("usuarios")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> ListarUsuariosAdmin(
        [FromQuery] string? busca,
        [FromQuery] string? papel,
        [FromQuery] bool? bloqueado,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanho = 20,
        CancellationToken ct = default)
    {
        var query = new ListarUsuariosAdminQuery(busca, papel, bloqueado, pagina, tamanho);
        var result = await _mediator.Send(query, ct);
        return Ok(result);
    }

    [HttpPatch("usuarios/{usuarioId:guid}/bloquear")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> BloquearUsuario(Guid usuarioId, [FromBody] BloquearUsuarioDto dto, CancellationToken ct)
    {
        var cmd = new BloquearUsuarioCommand(usuarioId, _currentUser.UserId ?? Guid.Empty, dto.Motivo);
        var result = await _mediator.Send(cmd, ct);
        return result.IsSuccess ? NoContent() : BadRequest(new { error = result.Error });
    }

    [HttpPatch("usuarios/{usuarioId:guid}/desbloquear")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> DesbloquearUsuario(Guid usuarioId, CancellationToken ct)
    {
        var cmd = new DesbloquearUsuarioCommand(usuarioId, _currentUser.UserId ?? Guid.Empty);
        var result = await _mediator.Send(cmd, ct);
        return result.IsSuccess ? NoContent() : BadRequest(new { error = result.Error });
    }

    [HttpGet("lojas")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> ListarLojasAdmin(
        [FromQuery] string? busca,
        [FromQuery] bool? ativo,
        [FromQuery] bool? aprovado,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanho = 20,
        CancellationToken ct = default)
    {
        var query = new ListarLojasAdminQuery(busca, ativo, aprovado, pagina, tamanho);
        var result = await _mediator.Send(query, ct);
        return Ok(result);
    }

    [HttpPatch("lojas/{lojaId:guid}/suspender")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> SuspenderLoja(Guid lojaId, CancellationToken ct)
    {
        var cmd = new SuspenderLojaCommand(lojaId, _currentUser.UserId ?? Guid.Empty);
        var result = await _mediator.Send(cmd, ct);
        return result.IsSuccess ? NoContent() : BadRequest(new { error = result.Error });
    }

    [HttpPatch("lojas/{lojaId:guid}/reativar")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> ReativarLoja(Guid lojaId, CancellationToken ct)
    {
        var cmd = new ReativarLojaCommand(lojaId, _currentUser.UserId ?? Guid.Empty);
        var result = await _mediator.Send(cmd, ct);
        return result.IsSuccess ? NoContent() : BadRequest(new { error = result.Error });
    }

    [HttpGet("produtos-admin")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> ListarProdutosAdmin(
        [FromQuery] string? busca,
        [FromQuery] Guid? lojaId,
        [FromQuery] bool? ativo,
        [FromQuery] bool? aprovado,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanho = 20,
        CancellationToken ct = default)
    {
        var query = new ListarProdutosAdminQuery(busca, lojaId, ativo, aprovado, pagina, tamanho);
        var result = await _mediator.Send(query, ct);
        return Ok(result);
    }

    [HttpPatch("produtos-admin/{produtoId:guid}/desativar")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> DesativarProdutoAdmin(Guid produtoId, CancellationToken ct)
    {
        var cmd = new DesativarProdutoAdminCommand(produtoId, _currentUser.UserId ?? Guid.Empty);
        var result = await _mediator.Send(cmd, ct);
        return result.IsSuccess ? NoContent() : BadRequest(new { error = result.Error });
    }

    [HttpPatch("produtos-admin/{produtoId:guid}/ativar")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> AtivarProdutoAdmin(Guid produtoId, CancellationToken ct)
    {
        var cmd = new AtivarProdutoAdminCommand(produtoId, _currentUser.UserId ?? Guid.Empty);
        var result = await _mediator.Send(cmd, ct);
        return result.IsSuccess ? NoContent() : BadRequest(new { error = result.Error });
    }

    [HttpGet("dashboard/estatisticas")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> EstatisticasDashboard(CancellationToken ct)
    {
        var query = new EstatisticasDashboardQuery();
        var result = await _mediator.Send(query, ct);
        return Ok(result);
    }

    [HttpGet("dashboard/crescimento")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> CrescimentoDashboard([FromQuery] int meses = 6, CancellationToken ct = default)
    {
        var query = new CrescimentoDashboardQuery(meses);
        var result = await _mediator.Send(query, ct);
        return Ok(result);
    }

    [HttpGet("notificacoes")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> ListarNotificacoes(
        [FromQuery] bool? apenasNaoLidas,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanho = 20,
        CancellationToken ct = default)
    {
        var usuarioId = _currentUser.UserId ?? Guid.Empty;
        var query = new ListarNotificacoesQuery(usuarioId, apenasNaoLidas, pagina, tamanho);
        var result = await _mediator.Send(query, ct);
        return Ok(result);
    }

    [HttpGet("notificacoes/contar-nao-lidas")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> ContarNaoLidas(CancellationToken ct)
    {
        var usuarioId = _currentUser.UserId ?? Guid.Empty;
        var query = new ContarNaoLidasQuery(usuarioId);
        var result = await _mediator.Send(query, ct);
        return result.IsSuccess ? Ok(new { naoLidas = result.Value }) : BadRequest(new { error = result.Error });
    }

    [HttpPatch("notificacoes/{notificacaoId:guid}/ler")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> MarcarNotificacaoLida(Guid notificacaoId, CancellationToken ct)
    {
        var usuarioId = _currentUser.UserId ?? Guid.Empty;
        var cmd = new MarcarNotificacaoLidaCommand(notificacaoId, usuarioId);
        var result = await _mediator.Send(cmd, ct);
        return result.IsSuccess ? NoContent() : BadRequest(new { error = result.Error });
    }
}

public sealed record AtualizarTaxaDto(decimal NovaTaxaPercentual);
public sealed record RecusarSolicitacaoDto(string Motivo);
public sealed record BloquearUsuarioDto(string Motivo);

