namespace GastronomiaMarketplace.WebAPI.Controllers;

using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using GastronomiaMarketplace.Application.Lojas.Queries.ListarProdutosPorLoja;
using GastronomiaMarketplace.Application.Lojas.Queries.ObterLojaPublica;
using GastronomiaMarketplace.Application.Common.Interfaces;

[ApiController]
[Route("api/lojas")]
public sealed class CatalogoController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUser _currentUser;
    public CatalogoController(IMediator mediator, ICurrentUser currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    [HttpGet("{lojaId:guid}/produtos")]
    [AllowAnonymous]
    public async Task<IActionResult> Produtos(Guid lojaId, CancellationToken ct)
    {
        var apenasAtivosAprovados = !_currentUser.IsAdmin && (_currentUser.VendorId is null || _currentUser.LojaId != lojaId);
        var query = new ListarProdutosPorLojaQuery(lojaId, apenasAtivosAprovados);
        var result = await _mediator.Send(query, ct);
        return Ok(result);
    }

    [HttpGet("{lojaId:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> Obter(Guid lojaId, CancellationToken ct)
    {
        var result = await _mediator.Send(new ObterLojaPublicaQuery(lojaId), ct);
        return result.IsSuccess ? Ok(result.Value) : NotFound(new { error = result.Error });
    }
}
