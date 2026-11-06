namespace GastronomiaMarketplace.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using GastronomiaMarketplace.Domain.Entities;

public sealed class ItemAgendaConfiguration : IEntityTypeConfiguration<ItemAgenda>
{
    public void Configure(EntityTypeBuilder<ItemAgenda> b)
    {
        b.ToTable("ItensAgenda");
        b.HasKey(a => a.Id);
        b.Property(a => a.Id).HasColumnType("uuid").HasDefaultValueSql("gen_random_uuid()");
        b.Property(a => a.PedidoId).HasColumnType("uuid").IsRequired();
        b.HasIndex(a => a.PedidoId).IsUnique();
        b.Property(a => a.LojaId).HasColumnType("uuid").IsRequired();
        b.Property(a => a.VendedorId).HasColumnType("uuid").IsRequired();
        b.Property(a => a.Status).HasColumnType("smallint").IsRequired();
        b.Property(a => a.AprovadoEmUtc).HasColumnType("timestamptz").IsRequired();
        b.Property(a => a.ProntoEmUtc).HasColumnType("timestamptz");
        b.Property(a => a.CriadoEmUtc).HasColumnType("timestamptz").IsRequired();
        b.Property(a => a.AtualizadoEmUtc).HasColumnType("timestamptz");
        b.HasOne(a => a.Pedido).WithOne().HasForeignKey<ItemAgenda>(a => a.PedidoId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<Domain.Entities.Loja>().WithMany().HasForeignKey(a => a.LojaId).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(a => new { a.LojaId, a.AprovadoEmUtc });
        b.HasIndex(a => new { a.LojaId, a.Status, a.AprovadoEmUtc });
        b.HasIndex(a => new { a.VendedorId, a.Status });
        b.HasQueryFilter(a => !a.IsDeleted);
    }
}
