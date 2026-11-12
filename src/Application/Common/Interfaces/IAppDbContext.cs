namespace GastronomiaMarketplace.Application.Common.Interfaces;

using Microsoft.EntityFrameworkCore;
using GastronomiaMarketplace.Domain.Entities;

public interface IAppDbContext
{
    DbSet<Usuario> Usuarios { get; }
    DbSet<Vendedor> Vendedores { get; }
    DbSet<Loja> Lojas { get; }
    DbSet<CategoriaLoja> CategoriasLoja { get; }
    DbSet<Produto> Produtos { get; }
    DbSet<Pedido> Pedidos { get; }
    DbSet<ItemPedido> ItensPedido { get; }
    DbSet<ItemAgenda> ItensAgenda { get; }
    DbSet<AuditLog> AuditLogs { get; }
    DbSet<ConsentimentoLgpd> ConsentimentosLgpd { get; }
    DbSet<ConfiguracaoGlobal> ConfiguracoesGlobais { get; }
    DbSet<Notificacao> Notificacoes { get; }
    Task<int> SaveChangesAsync(CancellationToken ct);
}
