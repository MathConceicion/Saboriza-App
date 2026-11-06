namespace GastronomiaMarketplace.WebAPI.Services;

public sealed class UploadImagemService
{
    private static readonly string[] ExtensoesPermitidas = [".jpg", ".jpeg", ".png", ".webp"];
    private const long TamanhoMaximoBytes = 5 * 1024 * 1024;

    private readonly IWebHostEnvironment _env;

    public UploadImagemService(IWebHostEnvironment env) => _env = env;

    public async Task<(bool Ok, string? Url, string? Erro)> SalvarAsync(IFormFile arquivo, HttpRequest request, CancellationToken ct)
    {
        if (arquivo is null || arquivo.Length == 0) return (false, null, "Nenhum arquivo enviado.");
        if (arquivo.Length > TamanhoMaximoBytes) return (false, null, "Imagem deve ter até 5MB.");
        var extensao = Path.GetExtension(arquivo.FileName).ToLowerInvariant();
        if (!ExtensoesPermitidas.Contains(extensao)) return (false, null, "Formato inválido. Use JPG, PNG ou WEBP.");
        var pasta = Path.Combine(_env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot"), "imagens");
        Directory.CreateDirectory(pasta);
        var nomeArquivo = $"{Guid.NewGuid():N}{extensao}";
        await using (var destino = System.IO.File.Create(Path.Combine(pasta, nomeArquivo)))
        {
            await arquivo.CopyToAsync(destino, ct);
        }
        return (true, $"{request.Scheme}://{request.Host}/imagens/{nomeArquivo}", null);
    }
}
