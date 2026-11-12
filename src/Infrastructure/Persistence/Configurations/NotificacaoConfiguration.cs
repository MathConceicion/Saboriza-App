namespace GastronomiaMarketplace.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using GastronomiaMarketplace.Domain.Entities;

public sealed class NotificacaoConfiguration : IEntityTypeConfiguration<Notificacao>
{
    public void Configure(EntityTypeBuilder<Notificacao> b)
    {
        b.ToTable("Notificacoes");
        b.HasKey(n => n.Id);
        b.Property(n => n.Id).HasColumnType("uuid").HasDefaultValueSql("gen_random_uuid()");
        b.Property(n => n.UsuarioId).HasColumnType("uuid");
        b.Property(n => n.Titulo).HasColumnType("varchar(200)").IsRequired();
        b.Property(n => n.Mensagem).HasColumnType("text").IsRequired();
        b.Property(n => n.Tipo).HasColumnType("smallint").IsRequired();
        b.Property(n => n.Lida).HasColumnType("boolean").HasDefaultValue(false);
        b.Property(n => n.LidaEmUtc).HasColumnType("timestamptz");
        b.Property(n => n.Url).HasColumnType("varchar(500)");
        b.Property(n => n.EntidadeTipo).HasColumnType("varchar(100)");
        b.Property(n => n.EntidadeId).HasColumnType("varchar(100)");
        b.Property(n => n.CriadoEmUtc).HasColumnType("timestamptz").IsRequired();
        b.Property(n => n.AtualizadoEmUtc).HasColumnType("timestamptz");
        b.Property(n => n.IsDeleted).HasColumnType("boolean").HasDefaultValue(false);
        b.HasIndex(n => n.UsuarioId);
        b.HasIndex(n => n.Lida);
        b.HasIndex(n => n.CriadoEmUtc);
        b.HasQueryFilter(n => !n.IsDeleted);
    }
}
