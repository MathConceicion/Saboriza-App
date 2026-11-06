namespace GastronomiaMarketplace.Application.Common.Interfaces;

public interface ITokenService
{
    string GerarToken(UsuarioTokenDto usuario);
    string GerarRefreshToken(Guid usuarioId);

    sealed record UsuarioTokenDto(
        Guid UsuarioId,
        string Email,
        string NomeCompleto,
        Domain.Enums.PapelUsuario Papel,
        bool IsAdmin,
        Guid? VendorId,
        Guid? LojaId,
        string? LojaStatus
    );
}
