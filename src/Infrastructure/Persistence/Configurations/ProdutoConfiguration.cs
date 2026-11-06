namespace GastronomiaMarketplace.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using GastronomiaMarketplace.Domain.Entities;

public sealed class ProdutoConfiguration : IEntityTypeConfiguration<Produto>
{
    public void Configure(EntityTypeBuilder<Produto> b)
    {
        b.ToTable("Produtos");
        b.HasKey(p => p.Id);
        b.Property(p => p.Id).HasColumnType("uuid").HasDefaultValueSql("gen_random_uuid()");
        b.Property(p => p.Nome).HasColumnType("varchar(150)").IsRequired();
        b.Property(p => p.Descricao).HasColumnType("text");
        b.Property(p => p.Preco).HasColumnType("numeric(10,2)").IsRequired();
        b.Property(p => p.QuantidadeEmEstoque).HasColumnType("integer").IsRequired();
        b.Property(p => p.Ativo).HasColumnType("boolean").HasDefaultValue(true);
        b.Property(p => p.Aprovado).HasColumnType("boolean").HasDefaultValue(false);
        b.Property(p => p.ImagemUrl).HasColumnType("varchar(1000)");
        b.Property(p => p.CriadoEmUtc).HasColumnType("timestamptz").IsRequired();
        b.Property(p => p.AtualizadoEmUtc).HasColumnType("timestamptz");
        b.Property(p => p.IsDeleted).HasColumnType("boolean").HasDefaultValue(false);
        b.Property(p => p.xmin)
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .IsRowVersion()
            .IsConcurrencyToken();
        b.HasOne(p => p.Loja).WithMany().HasForeignKey(p => p.LojaId).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(p => p.LojaId);
        b.HasQueryFilter(p => !p.IsDeleted);
    }
}
