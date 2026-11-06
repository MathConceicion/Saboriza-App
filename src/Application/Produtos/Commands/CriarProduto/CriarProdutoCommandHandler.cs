namespace GastronomiaMarketplace.Application.Produtos.Commands.CriarProduto;

using MediatR;
using GastronomiaMarketplace.Application.Common.Interfaces;
using GastronomiaMarketplace.Application.Common.Models;
using GastronomiaMarketplace.Domain.Entities;

public sealed class CriarProdutoCommandHandler : IRequestHandler<CriarProdutoCommand, Result<Guid>>
{
    private readonly IAppDbContext _db;
    private readonly IHtmlSanitizerService _sanitizer;

    public CriarProdutoCommandHandler(IAppDbContext db, IHtmlSanitizerService sanitizer)
    {
        _db = db;
        _sanitizer = sanitizer;
    }

    public async Task<Result<Guid>> Handle(CriarProdutoCommand request, CancellationToken ct)
    {
        var nomeSanitizado = _sanitizer.Sanitize(request.Nome);
        var descricaoSanitizada = _sanitizer.Sanitize(request.Descricao);
        if (string.IsNullOrWhiteSpace(nomeSanitizado))
            return Result<Guid>.Failure("Nome contém conteúdo não permitido.");
        var produto = new Produto(request.LojaId, nomeSanitizado, descricaoSanitizada, request.Preco, request.QuantidadeEmEstoque, request.ImagemUrl);
        produto.Aprovar();
        _db.Produtos.Add(produto);
        await _db.SaveChangesAsync(ct);
        return Result<Guid>.Success(produto.Id);
    }
}
