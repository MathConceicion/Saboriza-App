namespace GastronomiaMarketplace.WebAPI.Controllers;

using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using GastronomiaMarketplace.Application.Common.Interfaces;
using GastronomiaMarketplace.Application.Lojas.Queries.ListarLojasProximas;

[ApiController]
[Route("api/lojas")]
public sealed class LojasController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IGeocodingService _geocoding;

    public LojasController(IMediator mediator, IGeocodingService geocoding)
    {
        _mediator = mediator;
        _geocoding = geocoding;
    }

    [HttpGet("proximas")]
    [AllowAnonymous]
    public async Task<IActionResult> ListarProximas(
        [FromQuery] string? cep = null,
        [FromQuery] string? endereco = null,
        [FromQuery] double? lat = null,
        [FromQuery] double? lng = null,
        [FromQuery] double raioKm = 5,
        [FromQuery] Guid? categoriaId = null,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanho = 20,
        CancellationToken ct = default)
    {
        double qLat, qLng;
        if (lat.HasValue && lng.HasValue)
        {
            qLat = lat.Value; qLng = lng.Value;
        }
        else if (!string.IsNullOrWhiteSpace(cep) || !string.IsNullOrWhiteSpace(endereco))
        {
            var coord = await _geocoding.GeocodificarAsync(endereco ?? "", cep ?? "", ct);
            if (coord is null) return BadRequest(new { error = "Endereço/CEP não pôde ser geocodificado." });
            qLat = coord.Latitude; qLng = coord.Longitude;
        }
        else
        {
            return BadRequest(new { error = "Informe lat/lng ou cep/endereco." });
        }
        var query = new ListarLojasProximasQuery(qLat, qLng, raioKm, categoriaId, pagina, tamanho);
        var result = await _mediator.Send(query, ct);
        return Ok(result);
    }
}
