namespace GastronomiaMarketplace.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using GastronomiaMarketplace.Domain.Entities;

public sealed class VendedorConfiguration : IEntityTypeConfiguration<Vendedor>
{
    public void Configure(EntityTypeBuilder<Vendedor> b)
    {
        b.ToTable("Vendedores");
        b.HasKey(v => v.Id);
        b.Property(v => v.Id).HasColumnType("uuid").HasDefaultValueSql("gen_random_uuid()");
        b.Property(v => v.UsuarioId).HasColumnType("uuid").IsRequired();
        b.HasIndex(v => v.UsuarioId).IsUnique();
        b.Property(v => v.NomeLoja).HasColumnType("varchar(150)").IsRequired();
        b.Property(v => v.CategoriaLojaId).HasColumnType("uuid").IsRequired();
        b.Property(v => v.EnderecoComercial).HasColumnType("varchar(500)").IsRequired();
        b.Property(v => v.Cep).HasColumnType("varchar(9)").IsRequired();
        b.Property(v => v.Telefone).HasColumnType("varchar(20)").IsRequired();
        b.Property(v => v.Cnpj).HasColumnType("varchar(14)");
        b.Property(v => v.FotoUrl).HasColumnType("varchar(500)");
        b.Property(v => v.Latitude).HasColumnType("double precision").IsRequired();
        b.Property(v => v.Longitude).HasColumnType("double precision").IsRequired();
        b.Property(v => v.Status).HasColumnType("smallint").IsRequired();
        b.Property(v => v.MotivoRejeicao).HasColumnType("varchar(500)");
        b.Property(v => v.HorarioFuncionamentoJson).HasColumnType("jsonb");
        b.Property(v => v.DiasFuncionamentoJson).HasColumnType("jsonb");
        b.Property(v => v.Aberta).HasColumnType("boolean").HasDefaultValue(true);
        b.Property(v => v.CriadoEmUtc).HasColumnType("timestamptz").IsRequired();
        b.Property(v => v.AtualizadoEmUtc).HasColumnType("timestamptz");
        b.Property(v => v.IsDeleted).HasColumnType("boolean").HasDefaultValue(false);
        b.HasOne(v => v.Usuario).WithOne(u => u.Vendedor).HasForeignKey<Vendedor>(v => v.UsuarioId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(v => v.CategoriaLoja).WithMany().HasForeignKey(v => v.CategoriaLojaId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(v => v.CategoriaLojaId);
        b.HasIndex(v => v.Status);
        b.HasQueryFilter(v => !v.IsDeleted);
    }
}
