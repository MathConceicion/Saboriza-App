namespace GastronomiaMarketplace.WebAPI.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using GastronomiaMarketplace.WebAPI.Services;

[ApiController]
[Route("api/uploads")]
public sealed class UploadsController : ControllerBase
{
    private readonly UploadImagemService _upload;

    public UploadsController(UploadImagemService upload) => _upload = upload;

    [HttpPost("imagens")]
    [Authorize]
    [RequestSizeLimit(6_000_000)]
    public async Task<IActionResult> EnviarImagem(IFormFile arquivo, CancellationToken ct)
    {
        var resultado = await _upload.SalvarAsync(arquivo, Request, ct);
        return resultado.Ok ? Ok(new { imagemUrl = resultado.Url }) : BadRequest(new { error = resultado.Erro });
    }
}
