namespace GastronomiaMarketplace.Application.Produtos.Commands.EditarProduto;

using MediatR;
using Microsoft.EntityFrameworkCore;
using GastronomiaMarketplace.Application.Common.Interfaces;
using GastronomiaMarketplace.Application.Common.Models;

public sealed class EditarProdutoCommandHandler : IRequestHandler<EditarProdutoCommand, Result<Guid>>
{
    private readonly IAppDbContext _db;
    private readonly IHtmlSanitizerService _sanitizer;

    public EditarProdutoCommandHandler(IAppDbContext db, IHtmlSanitizerService sanitizer)
    {
        _db = db;
        _sanitizer = sanitizer;
    }

    public async Task<Result<Guid>> Handle(EditarProdutoCommand request, CancellationToken ct)
    {
        var produto = await _db.Produtos.FirstOrDefaultAsync(p => p.Id == request.ProdutoId, ct);
        if (produto is null || produto.LojaId != request.LojaId)
            return Result<Guid>.Failure("Produto não encontrado na sua loja.");
        if (request.Preco <= 0)
            return Result<Guid>.Failure("Preço deve ser maior que zero.");
        if (request.QuantidadeEmEstoque < 0)
            return Result<Guid>.Failure("Estoque não pode ser negativo.");
        var nomeSanitizado = _sanitizer.Sanitize(request.Nome);
        var descricaoSanitizada = _sanitizer.Sanitize(request.Descricao);
        if (string.IsNullOrWhiteSpace(nomeSanitizado))
            return Result<Guid>.Failure("Nome contém conteúdo não permitido.");
        produto.Atualizar(nomeSanitizado, descricaoSanitizada, request.Preco, request.ImagemUrl);
        produto.AjustarEstoque(request.QuantidadeEmEstoque);
        await _db.SaveChangesAsync(ct);
        return Result<Guid>.Success(produto.Id);
    }
}
