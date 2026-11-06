namespace GastronomiaMarketplace.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using GastronomiaMarketplace.Domain.Entities;
using GastronomiaMarketplace.Domain.ValueObjects;

public sealed class PedidoConfiguration : IEntityTypeConfiguration<Pedido>
{
    public void Configure(EntityTypeBuilder<Pedido> b)
    {
        b.ToTable("Pedidos");
        b.HasKey(p => p.Id);
        b.Property(p => p.Id).HasColumnType("uuid").HasDefaultValueSql("gen_random_uuid()");
        b.Property(p => p.ClienteId).HasColumnType("uuid").IsRequired();
        b.Property(p => p.LojaId).HasColumnType("uuid").IsRequired();
        b.Property(p => p.Status).HasColumnType("smallint").IsRequired();
        b.Property(p => p.ValorBruto)
            .HasConversion(v => v.Valor, v => new Dinheiro(v, "BRL"))
            .HasColumnType("numeric(10,2)")
            .IsRequired();
        b.Property(p => p.TaxaComissaoPercentual).HasColumnType("numeric(5,2)");
        b.Property(p => p.TaxaComissaoValor)
            .HasConversion(v => v == null ? (decimal?)null : v.Valor, v => v == null ? null : new Dinheiro(v.Value, "BRL"))
            .HasColumnType("numeric(10,2)");
        b.Property(p => p.ValorLiquidoVendedor)
            .HasConversion(v => v == null ? (decimal?)null : v.Valor, v => v == null ? null : new Dinheiro(v.Value, "BRL"))
            .HasColumnType("numeric(10,2)");
        b.Property(p => p.PixPayload).HasColumnType("text");
        b.Property(p => p.PixSimuladoEmUtc).HasColumnType("timestamptz");
        b.Property(p => p.AprovadoEmUtc).HasColumnType("timestamptz");
        b.Property(p => p.MotivoCancelamento).HasColumnType("varchar(500)");
        b.Property(p => p.ClienteNomeSnapshot).HasColumnType("varchar(200)");
        b.Property(p => p.CriadoEmUtc).HasColumnType("timestamptz").IsRequired();
        b.Property(p => p.AtualizadoEmUtc).HasColumnType("timestamptz");
        b.Property(p => p.IsDeleted).HasColumnType("boolean").HasDefaultValue(false);
        b.HasOne(p => p.Cliente).WithMany().HasForeignKey(p => p.ClienteId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(p => p.Loja).WithMany().HasForeignKey(p => p.LojaId).OnDelete(DeleteBehavior.Restrict);
        b.HasMany(p => p.Itens).WithOne(i => i.Pedido).HasForeignKey(i => i.PedidoId).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(p => p.ClienteId);
        b.HasIndex(p => p.LojaId);
        b.HasIndex(p => p.Status);
        b.HasIndex(p => p.CriadoEmUtc);
        b.HasQueryFilter(p => !p.IsDeleted);
    }
}
