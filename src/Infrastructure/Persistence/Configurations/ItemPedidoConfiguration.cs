namespace GastronomiaMarketplace.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using GastronomiaMarketplace.Domain.Entities;
using GastronomiaMarketplace.Domain.ValueObjects;

public sealed class ItemPedidoConfiguration : IEntityTypeConfiguration<ItemPedido>
{
    public void Configure(EntityTypeBuilder<ItemPedido> b)
    {
        b.ToTable("ItensDoPedido");
        b.HasKey(i => i.Id);
        b.Property(i => i.Id).HasColumnType("uuid").HasDefaultValueSql("gen_random_uuid()");
        b.Property(i => i.PedidoId).HasColumnType("uuid").IsRequired();
        b.Property(i => i.ProdutoId).HasColumnType("uuid").IsRequired();
        b.Property(i => i.ProdutoNomeSnapshot).HasColumnType("varchar(150)").IsRequired();
        b.Property(i => i.PrecoUnitario)
            .HasConversion(v => v.Valor, v => new Dinheiro(v, "BRL"))
            .HasColumnType("numeric(10,2)")
            .IsRequired();
        b.Property(i => i.Quantidade).HasColumnType("integer").IsRequired();
        b.Property(i => i.Subtotal)
            .HasConversion(v => v.Valor, v => new Dinheiro(v, "BRL"))
            .HasColumnType("numeric(10,2)")
            .IsRequired();
        b.Property(i => i.ProdutoAtivo).HasColumnType("boolean").HasDefaultValue(true);
        b.Property(i => i.CriadoEmUtc).HasColumnType("timestamptz").IsRequired();
        b.HasOne(i => i.Pedido).WithMany(p => p.Itens).HasForeignKey(i => i.PedidoId).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(i => i.PedidoId);
        b.HasIndex(i => i.ProdutoId);
        b.HasQueryFilter(i => !i.IsDeleted);
    }
}
