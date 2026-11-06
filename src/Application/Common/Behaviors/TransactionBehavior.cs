namespace GastronomiaMarketplace.Application.Common.Behaviors;

using MediatR;
using Microsoft.EntityFrameworkCore;
using GastronomiaMarketplace.Application.Common.Interfaces;

public sealed class TransactionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IAppDbContext _db;

    public TransactionBehavior(IAppDbContext db) => _db = db;

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        var isCommand = request.GetType().Name.EndsWith("Command");
        if (!isCommand) return await next();
        if (_db is DbContext ef && ef.Database.CurrentTransaction is not null)
            return await next();
        await using var tx = await ((DbContext)_db).Database.BeginTransactionAsync(ct);
        try
        {
            var response = await next();
            await tx.CommitAsync(ct);
            return response;
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }
}
