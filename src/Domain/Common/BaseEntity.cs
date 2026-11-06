namespace GastronomiaMarketplace.Domain.Common;

public abstract class BaseEntity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
    public DateTime CriadoEmUtc { get; protected set; } = DateTime.UtcNow;
    public DateTime? AtualizadoEmUtc { get; protected set; }
    public bool IsDeleted { get; private set; }

    public void MarcarComoDeletado() => IsDeleted = true;
}
