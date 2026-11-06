namespace GastronomiaMarketplace.Infrastructure.Persistence.Configurations;

using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using GastronomiaMarketplace.Domain.Entities;
using GastronomiaMarketplace.Infrastructure.Security.DataProtection;

public sealed class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
{
    private readonly IDataProtectionProvider? _dpProvider;

    public UsuarioConfiguration() { }

    public UsuarioConfiguration(IDataProtectionProvider dpProvider) => _dpProvider = dpProvider;

    public void Configure(EntityTypeBuilder<Usuario> b)
    {
        b.ToTable("Usuarios");
        b.HasKey(u => u.Id);
        b.Property(u => u.Id).HasColumnType("uuid").HasDefaultValueSql("gen_random_uuid()");
        b.Property(u => u.Email).HasColumnType("varchar(254)").IsRequired();
        b.HasIndex(u => u.Email).IsUnique();
        b.Property(u => u.NomeCompleto).HasColumnType("varchar(200)").IsRequired();
        b.Property(u => u.Cpf).HasColumnType("varchar(11)");
        b.HasIndex(u => u.Cpf).IsUnique();
        b.Property(u => u.Papel).HasColumnType("smallint").IsRequired();
        if (_dpProvider is not null)
        {
            var alergiasProtector = ProtectorFactory.ParaAlergias(_dpProvider);
            b.Property(u => u.AlergiasSeveras)
                .HasColumnType("text")
                .HasConversion(new EncryptedConverter(alergiasProtector))
                .IsRequired(false);
            var restricoesProtector = ProtectorFactory.ParaRestricoes(_dpProvider);
            b.Property(u => u.RestricoesAlimentares)
                .HasColumnType("text")
                .HasConversion(new EncryptedConverter(restricoesProtector))
                .IsRequired(false);
            b.Property(u => u.ObservacoesSaude)
                .HasColumnType("text")
                .HasConversion(new EncryptedConverter(alergiasProtector))
                .IsRequired(false);
        }
        else
        {
            b.Property(u => u.AlergiasSeveras).HasColumnType("text").IsRequired(false);
            b.Property(u => u.RestricoesAlimentares).HasColumnType("text").IsRequired(false);
            b.Property(u => u.ObservacoesSaude).HasColumnType("text").IsRequired(false);
        }
        b.Property(u => u.EnderecoAtual).HasColumnType("varchar(500)");
        b.Property(u => u.Cep).HasColumnType("varchar(9)");
        b.Property(u => u.Telefone).HasColumnType("varchar(20)");
        b.Property(u => u.SenhaHash).HasColumnType("varchar(255)").IsRequired();
        b.Property(u => u.IsAnonimizado).HasColumnType("boolean").HasDefaultValue(false);
        b.Property(u => u.CriadoEmUtc).HasColumnType("timestamptz").IsRequired();
        b.Property(u => u.AtualizadoEmUtc).HasColumnType("timestamptz");
        b.Property(u => u.IsDeleted).HasColumnType("boolean").HasDefaultValue(false);
        b.HasQueryFilter(u => !u.IsDeleted);
    }
}
