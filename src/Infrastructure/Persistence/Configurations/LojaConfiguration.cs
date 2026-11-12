namespace GastronomiaMarketplace.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using GastronomiaMarketplace.Domain.Entities;

public sealed class LojaConfiguration : IEntityTypeConfiguration<Loja>
{
    public void Configure(EntityTypeBuilder<Loja> b)
    {
        b.ToTable("Lojas");
        b.HasKey(l => l.Id);
        b.Property(l => l.Id).HasColumnType("uuid").HasDefaultValueSql("gen_random_uuid()");
        b.Property(l => l.Nome).HasColumnType("varchar(150)").IsRequired();
        b.Property(l => l.EnderecoComercial).HasColumnType("varchar(500)").IsRequired();
        b.Property(l => l.Cep).HasColumnType("varchar(9)").IsRequired();
        b.Property(l => l.Localizacao)
            .HasColumnType("geography (point)")
            .IsRequired();
        b.HasIndex(l => l.Localizacao).HasMethod("GIST");
        b.Property(l => l.Ativo).HasColumnType("boolean").HasDefaultValue(true);
        b.Property(l => l.Aprovado).HasColumnType("boolean").HasDefaultValue(false);
        b.HasOne(l => l.CategoriaLoja).WithMany(c => c.Lojas).HasForeignKey(l => l.CategoriaLojaId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(l => l.Vendedor).WithOne(v => v.Loja).HasForeignKey<Loja>(l => l.VendedorId).OnDelete(DeleteBehavior.Cascade);
        b.Property(l => l.HorarioFuncionamentoJson).HasColumnType("jsonb");
        b.Property(l => l.Cnpj).HasColumnType("varchar(14)");
        b.Property(l => l.Telefone).HasColumnType("varchar(20)");
        b.Property(l => l.DiasFuncionamentoJson).HasColumnType("jsonb");
        b.Property(l => l.Aberta).HasColumnType("boolean").HasDefaultValue(true);
        b.Property(l => l.TaxaComissaoPercentual).HasColumnType("numeric(5,2)");
        b.Property(l => l.Descricao).HasColumnType("text");
        b.Property(l => l.FotoUrl).HasColumnType("varchar(500)");
        b.Property(l => l.CriadoEmUtc).HasColumnType("timestamptz").IsRequired();
        b.Property(l => l.AtualizadoEmUtc).HasColumnType("timestamptz");
        b.Property(l => l.IsDeleted).HasColumnType("boolean").HasDefaultValue(false);
        b.HasQueryFilter(l => !l.IsDeleted);
    }
}
