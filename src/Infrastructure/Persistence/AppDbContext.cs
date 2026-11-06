namespace GastronomiaMarketplace.Infrastructure.Persistence;

using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using GastronomiaMarketplace.Application.Common.Interfaces;
using GastronomiaMarketplace.Domain.Entities;

public sealed class AppDbContext : DbContext, IAppDbContext
{
    private readonly IDataProtectionProvider? _dpProvider;

    public AppDbContext(DbContextOptions<AppDbContext> options, IDataProtectionProvider? dpProvider = null) : base(options)
    {
        _dpProvider = dpProvider;
    }

    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Vendedor> Vendedores => Set<Vendedor>();
    public DbSet<Loja> Lojas => Set<Loja>();
    public DbSet<CategoriaLoja> CategoriasLoja => Set<CategoriaLoja>();
    public DbSet<Produto> Produtos => Set<Produto>();
    public DbSet<Pedido> Pedidos => Set<Pedido>();
    public DbSet<ItemPedido> ItensPedido => Set<ItemPedido>();
    public DbSet<ItemAgenda> ItensAgenda => Set<ItemAgenda>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<ConsentimentoLgpd> ConsentimentosLgpd => Set<ConsentimentoLgpd>();
    public DbSet<ConfiguracaoGlobal> ConfiguracoesGlobais => Set<ConfiguracaoGlobal>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new Configurations.CategoriaLojaConfiguration());
        modelBuilder.ApplyConfiguration(new Configurations.VendedorConfiguration());
        modelBuilder.ApplyConfiguration(new Configurations.LojaConfiguration());
        modelBuilder.ApplyConfiguration(new Configurations.ProdutoConfiguration());
        modelBuilder.ApplyConfiguration(new Configurations.ItemPedidoConfiguration());
        modelBuilder.ApplyConfiguration(new Configurations.ItemAgendaConfiguration());
        modelBuilder.ApplyConfiguration(new Configurations.AuditLogConfiguration());
        modelBuilder.ApplyConfiguration(new Configurations.ConsentimentoLgpdConfiguration());
        modelBuilder.ApplyConfiguration(new Configurations.ConfiguracaoGlobalConfiguration());
        modelBuilder.ApplyConfiguration(new Configurations.PedidoConfiguration());
        if (_dpProvider is not null)
            modelBuilder.ApplyConfiguration(new Configurations.UsuarioConfiguration(_dpProvider));
        else
            modelBuilder.ApplyConfiguration(new Configurations.UsuarioConfiguration());
    }
}
