namespace GastronomiaMarketplace.Infrastructure.Persistence.Seed;

using Microsoft.EntityFrameworkCore;
using GastronomiaMarketplace.Domain.Entities;

public static class CategoriaSeed
{
    public static readonly (string Nome, string Slug, int Ordem)[] Categorias =
    [
        ("Pizzarias", "pizzarias", 1),
        ("Hambúrgueres", "hamburgueres", 2),
        ("Comida Japonesa", "comida-japonesa", 3),
        ("Sorveterias", "sorveterias", 4),
        ("Docerias", "docerias", 5),
        ("Padarias Artesanais", "padarias-artesanais", 6),
        ("Comida Brasileira", "comida-brasileira", 7),
        ("Saudável / Vegana", "saudavel-vegana", 8),
    ];

    public static async Task SeedAsync(AppDbContext db, CancellationToken ct)
    {
        if (await db.CategoriasLoja.AnyAsync(ct)) return;
        foreach (var (nome, slug, ordem) in Categorias)
            db.CategoriasLoja.Add(new CategoriaLoja(nome, slug, ordem));
        await db.SaveChangesAsync(ct);
    }
}
