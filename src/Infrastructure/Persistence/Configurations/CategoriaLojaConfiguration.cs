namespace GastronomiaMarketplace.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using GastronomiaMarketplace.Domain.Entities;

public sealed class CategoriaLojaConfiguration : IEntityTypeConfiguration<CategoriaLoja>
{
    public void Configure(EntityTypeBuilder<CategoriaLoja> b)
    {
        b.ToTable("CategoriasLoja");
        b.HasKey(c => c.Id);
        b.Property(c => c.Id).HasColumnType("uuid").HasDefaultValueSql("gen_random_uuid()");
        b.Property(c => c.Nome).HasColumnType("varchar(100)").IsRequired();
        b.Property(c => c.Slug).HasColumnType("varchar(100)").IsRequired();
        b.HasIndex(c => c.Slug).IsUnique();
        b.Property(c => c.IconeUrl).HasColumnType("varchar(500)");
        b.Property(c => c.OrdemExibicao).HasColumnType("integer").IsRequired();
        b.Property(c => c.Ativo).HasColumnType("boolean").HasDefaultValue(true);
        b.Property(c => c.CriadoEmUtc).HasColumnType("timestamptz").IsRequired();
        b.Property(c => c.AtualizadoEmUtc).HasColumnType("timestamptz");
    }
}
