namespace GastronomiaMarketplace.Application.Common.Interfaces;

public interface ICurrentUser
{
    Guid? UserId { get; }
    string? Email { get; }
    Guid? VendorId { get; }
    Guid? LojaId { get; }
    bool IsAdmin { get; }
    bool IsVendedor { get; }
    bool IsAuthenticated { get; }
    IReadOnlyList<string> Roles { get; }
}
