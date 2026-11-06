namespace GastronomiaMarketplace.Infrastructure.Persistence.Interceptors;

using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using GastronomiaMarketplace.Application.Common.Interfaces;
using GastronomiaMarketplace.Domain.Common;
using GastronomiaMarketplace.Domain.Entities;

public sealed class AuditableEntityInterceptor : SaveChangesInterceptor
{
    private readonly ICurrentUser _currentUser;
    private readonly IHttpContextAccessor _http;

    public AuditableEntityInterceptor(ICurrentUser currentUser, IHttpContextAccessor http)
    {
        _currentUser = currentUser;
        _http = http;
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken ct = default)
    {
        var ctx = eventData.Context;
        if (ctx is null) return await base.SavingChangesAsync(eventData, result, ct);
        var auditEntries = new List<AuditLog>();
        var ip = _http.HttpContext?.Connection.RemoteIpAddress?.ToString();
        var ua = _http.HttpContext?.Request.Headers.UserAgent.ToString();
        var usuarioId = _currentUser.UserId;
        foreach (var entry in ctx.ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.Entity is AuditLog) continue;
            if (entry.State == EntityState.Added)
                entry.Property(nameof(BaseEntity.CriadoEmUtc)).CurrentValue = DateTime.UtcNow;
            if (entry.State == EntityState.Modified)
                entry.Property(nameof(BaseEntity.AtualizadoEmUtc)).CurrentValue = DateTime.UtcNow;
            if (entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            {
                var entidade = entry.Entity.GetType().Name;
                var entidadeId = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "Id")?.CurrentValue?.ToString() ?? "?";
                var acao = entry.State.ToString().ToUpperInvariant();
                string? antigos = null, novos = null;
                var jsonOptions = new JsonSerializerOptions { NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowNamedFloatingPointLiterals, ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles };
                static object? NormalizeValue(object? value)
                {
                    if (value is NetTopologySuite.Geometries.Geometry g) return g.AsText();
                    return value;
                }
                if (entry.State == EntityState.Modified)
                {
                    var oldValues = new Dictionary<string, object?>();
                    var newValues = new Dictionary<string, object?>();
                    foreach (var prop in entry.Properties)
                    {
                        var original = prop.OriginalValue;
                        var current = prop.CurrentValue;
                        if (!Equals(original, current))
                        {
                            oldValues[prop.Metadata.Name] = NormalizeValue(original);
                            newValues[prop.Metadata.Name] = NormalizeValue(current);
                        }
                    }
                    if (oldValues.Count > 0)
                    {
                        antigos = JsonSerializer.Serialize(oldValues, jsonOptions);
                        novos = JsonSerializer.Serialize(newValues, jsonOptions);
                    }
                    else
                    {
                        continue;
                    }
                }
                else if (entry.State == EntityState.Added)
                {
                    var newValues = entry.Properties.ToDictionary(p => p.Metadata.Name, p => NormalizeValue(p.CurrentValue));
                    novos = JsonSerializer.Serialize(newValues, jsonOptions);
                }
                else if (entry.State == EntityState.Deleted)
                {
                    var oldValues = entry.Properties.ToDictionary(p => p.Metadata.Name, p => NormalizeValue(p.OriginalValue));
                    antigos = JsonSerializer.Serialize(oldValues, jsonOptions);
                }
                auditEntries.Add(new AuditLog(
                    usuarioId: usuarioId,
                    acao: acao,
                    entidade: entidade,
                    entidadeId: entidadeId,
                    valoresAntigosJson: antigos,
                    valoresNovosJson: novos,
                    ipOrigem: ip,
                    userAgent: ua
                ));
            }
        }
        if (auditEntries.Count > 0)
            ctx.Set<AuditLog>().AddRange(auditEntries);
        return await base.SavingChangesAsync(eventData, result, ct);
    }
}
