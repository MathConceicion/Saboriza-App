namespace GastronomiaMarketplace.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using GastronomiaMarketplace.Domain.Entities;

public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> b)
    {
        b.ToTable("AuditLogs");
        b.HasKey(a => a.Id);
        b.Property(a => a.Id).HasColumnType("uuid").HasDefaultValueSql("gen_random_uuid()");
        b.Property(a => a.UsuarioId).HasColumnType("uuid");
        b.Property(a => a.Acao).HasColumnType("varchar(100)").IsRequired();
        b.Property(a => a.Entidade).HasColumnType("varchar(100)").IsRequired();
        b.Property(a => a.EntidadeId).HasColumnType("varchar(100)").IsRequired();
        b.Property(a => a.DataUtc).HasColumnType("timestamptz").IsRequired();
        b.Property(a => a.ValoresAntigosJson).HasColumnType("jsonb");
        b.Property(a => a.ValoresNovosJson).HasColumnType("jsonb");
        b.Property(a => a.IpOrigem).HasColumnType("varchar(45)");
        b.Property(a => a.UserAgent).HasColumnType("varchar(500)");
        b.Property(a => a.Detalhes).HasColumnType("text");
        b.Property(a => a.CriadoEmUtc).HasColumnType("timestamptz").IsRequired();
        b.HasIndex(a => new { a.Entidade, a.EntidadeId });
        b.HasIndex(a => new { a.UsuarioId, a.DataUtc });
        b.HasIndex(a => a.DataUtc);
    }
}
