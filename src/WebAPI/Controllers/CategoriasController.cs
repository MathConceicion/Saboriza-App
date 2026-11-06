namespace GastronomiaMarketplace.WebAPI.Controllers;

using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using GastronomiaMarketplace.Application.Categorias.Queries.ListarCategorias;
using GastronomiaMarketplace.Application.Categorias.Queries.ListarLojasPorCategoria;

[ApiController]
[Route("api/categorias")]
public sealed class CategoriasController : ControllerBase
{
    private readonly IMediator _mediator;
    public CategoriasController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Listar(CancellationToken ct)
    {
        var result = await _mediator.Send(new ListarCategoriasQuery(), ct);
        return Ok(result);
    }

    [HttpGet("{slug}/lojas")]
    [AllowAnonymous]
    public async Task<IActionResult> LojasPorCategoria(string slug, [FromQuery] double? lat, [FromQuery] double? lng, [FromQuery] double raioKm = 50, [FromQuery] int pagina = 1, [FromQuery] int tamanho = 20, CancellationToken ct = default)
    {
        var query = new ListarLojasPorCategoriaQuery(slug, lat, lng, raioKm, pagina, tamanho);
        var result = await _mediator.Send(query, ct);
        return Ok(result);
    }
}
