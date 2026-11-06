namespace GastronomiaMarketplace.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using GastronomiaMarketplace.Domain.Entities;

public sealed class ConsentimentoLgpdConfiguration : IEntityTypeConfiguration<ConsentimentoLgpd>
{
    public void Configure(EntityTypeBuilder<ConsentimentoLgpd> b)
    {
        b.ToTable("ConsentimentosLgpd");
        b.HasKey(c => c.Id);
        b.Property(c => c.Id).HasColumnType("uuid").HasDefaultValueSql("gen_random_uuid()");
        b.Property(c => c.UsuarioId).HasColumnType("uuid").IsRequired();
        b.Property(c => c.Tipo).HasColumnType("smallint").IsRequired();
        b.Property(c => c.Ativo).HasColumnType("boolean").IsRequired();
        b.Property(c => c.ConcedidoEmUtc).HasColumnType("timestamptz").IsRequired();
        b.Property(c => c.RevogadoEmUtc).HasColumnType("timestamptz");
        b.Property(c => c.TextoConsentimento).HasColumnType("text").IsRequired();
        b.Property(c => c.IpOrigem).HasColumnType("varchar(45)");
        b.Property(c => c.UserAgent).HasColumnType("varchar(500)");
        b.Property(c => c.MotivoRevogacao).HasColumnType("varchar(500)");
        b.Property(c => c.CriadoEmUtc).HasColumnType("timestamptz").IsRequired();
        b.Property(c => c.AtualizadoEmUtc).HasColumnType("timestamptz");
        b.HasIndex(c => new { c.UsuarioId, c.Tipo });
        b.HasOne<Usuario>().WithMany().HasForeignKey(c => c.UsuarioId).OnDelete(DeleteBehavior.Cascade);
    }
}
