namespace GastronomiaMarketplace.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using GastronomiaMarketplace.Domain.Entities;

public sealed class ConfiguracaoGlobalConfiguration : IEntityTypeConfiguration<ConfiguracaoGlobal>
{
    public void Configure(EntityTypeBuilder<ConfiguracaoGlobal> b)
    {
        b.ToTable("ConfiguracoesGlobais");
        b.HasKey(c => c.Id);
        b.Property(c => c.Id).HasColumnType("uuid").HasDefaultValueSql("gen_random_uuid()");
        b.Property(c => c.TaxaComissaoPadraoPercentual).HasColumnType("numeric(5,2)").IsRequired();
        b.Property(c => c.VigenciaDesde).HasColumnType("timestamptz").IsRequired();
        b.Property(c => c.AtualizadoEmUtc).HasColumnType("timestamptz");
        b.Property(c => c.CriadoEmUtc).HasColumnType("timestamptz").IsRequired();
    }
}
