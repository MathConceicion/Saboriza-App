namespace GastronomiaMarketplace.WebAPI.Policies;

using Microsoft.AspNetCore.Authorization;

public sealed class VendorOwnerRequirement : IAuthorizationRequirement { }

public interface IOwnedByVendor
{
    Guid VendorId { get; }
}

public sealed record PedidoResource(Guid PedidoId, Guid VendorId) : IOwnedByVendor;
public sealed record ProdutoResource(Guid ProdutoId, Guid VendorId) : IOwnedByVendor;
