namespace GastronomiaMarketplace.WebAPI.Controllers;

using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using GastronomiaMarketplace.Application.Common.Interfaces;
using GastronomiaMarketplace.Application.Produtos.Commands.AlternarProduto;
using GastronomiaMarketplace.Application.Produtos.Commands.CriarProduto;
using GastronomiaMarketplace.Application.Produtos.Commands.EditarProduto;
using GastronomiaMarketplace.Application.Produtos.Commands.ExcluirProduto;
using GastronomiaMarketplace.WebAPI.Services;

[ApiController]
[Route("api/produtos")]
public sealed class ProdutosController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUser _currentUser;
    private readonly UploadImagemService _upload;

    public ProdutosController(IMediator mediator, ICurrentUser currentUser, UploadImagemService upload)
    {
        _mediator = mediator;
        _currentUser = currentUser;
        _upload = upload;
    }

    [HttpPost]
    [Authorize(Policy = "VendedorAprovado")]
    public async Task<IActionResult> Criar([FromBody] CriarProdutoDto dto, CancellationToken ct)
    {
        var lojaId = _currentUser.LojaId;
        if (lojaId is null) return BadRequest(new { error = "Vendedor sem loja." });
        var cmd = new CriarProdutoCommand(lojaId.Value, dto.Nome, dto.Descricao, dto.Preco, dto.QuantidadeEmEstoque, dto.ImagemUrl);
        var result = await _mediator.Send(cmd, ct);
        return result.IsSuccess ? Ok(new { produtoId = result.Value }) : BadRequest(new { error = result.Error });
    }

    [HttpPost("imagens")]
    [Authorize(Policy = "VendedorAprovado")]
    [RequestSizeLimit(6_000_000)]
    public async Task<IActionResult> EnviarImagem(IFormFile arquivo, CancellationToken ct)
    {
        var resultado = await _upload.SalvarAsync(arquivo, Request, ct);
        return resultado.Ok ? Ok(new { imagemUrl = resultado.Url }) : BadRequest(new { error = resultado.Erro });
    }

    [HttpPut("{produtoId:guid}")]
    [Authorize(Policy = "VendedorAprovado")]
    public async Task<IActionResult> Editar(Guid produtoId, [FromBody] EditarProdutoDto dto, CancellationToken ct)
    {
        var lojaId = _currentUser.LojaId;
        if (lojaId is null) return BadRequest(new { error = "Vendedor sem loja." });
        var cmd = new EditarProdutoCommand(produtoId, lojaId.Value, dto.Nome, dto.Descricao, dto.Preco, dto.QuantidadeEmEstoque, dto.ImagemUrl);
        var result = await _mediator.Send(cmd, ct);
        return result.IsSuccess ? NoContent() : BadRequest(new { error = result.Error });
    }

    [HttpPatch("{produtoId:guid}/desativar")]
    [Authorize(Policy = "VendedorAprovado")]
    public async Task<IActionResult> Desativar(Guid produtoId, CancellationToken ct)
    {
        var lojaId = _currentUser.LojaId;
        if (lojaId is null) return BadRequest(new { error = "Vendedor sem loja." });
        var result = await _mediator.Send(new AlternarProdutoCommand(produtoId, lojaId.Value, false), ct);
        return result.IsSuccess ? NoContent() : BadRequest(new { error = result.Error });
    }

    [HttpPatch("{produtoId:guid}/ativar")]
    [Authorize(Policy = "VendedorAprovado")]
    public async Task<IActionResult> Ativar(Guid produtoId, CancellationToken ct)
    {
        var lojaId = _currentUser.LojaId;
        if (lojaId is null) return BadRequest(new { error = "Vendedor sem loja." });
        var result = await _mediator.Send(new AlternarProdutoCommand(produtoId, lojaId.Value, true), ct);
        return result.IsSuccess ? NoContent() : BadRequest(new { error = result.Error });
    }

    [HttpDelete("{produtoId:guid}")]
    [Authorize(Policy = "VendedorAprovado")]
    public async Task<IActionResult> Excluir(Guid produtoId, CancellationToken ct)
    {
        var lojaId = _currentUser.LojaId;
        if (lojaId is null) return BadRequest(new { error = "Vendedor sem loja." });
        var result = await _mediator.Send(new ExcluirProdutoCommand(produtoId, lojaId.Value), ct);
        return result.IsSuccess ? NoContent() : BadRequest(new { error = result.Error });
    }
}

public sealed record CriarProdutoDto(string Nome, string? Descricao, decimal Preco, int QuantidadeEmEstoque, string? ImagemUrl);

public sealed record EditarProdutoDto(string Nome, string? Descricao, decimal Preco, int QuantidadeEmEstoque, string? ImagemUrl);
