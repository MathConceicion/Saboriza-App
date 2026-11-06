namespace GastronomiaMarketplace.Infrastructure.Security.DataProtection;

using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

public sealed class EncryptedConverter : ValueConverter<string?, string?>
{
    public EncryptedConverter(IDataProtector protector, ConverterMappingHints? hints = null)
        : base(
            v => v == null ? null : protector.Protect(v),
            v => v == null ? null : protector.Unprotect(v),
            hints)
    { }
}

public static class ProtectorFactory
{
    public static IDataProtector ParaAlergias(IDataProtectionProvider provider)
        => provider.CreateProtector("GastronomiaMarketplace.Alergias.v1");

    public static IDataProtector ParaRestricoes(IDataProtectionProvider provider)
        => provider.CreateProtector("GastronomiaMarketplace.Restricoes.v1");
}
