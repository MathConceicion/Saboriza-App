namespace GastronomiaMarketplace.Application.Common.Models;

public sealed record Result<T>(bool IsSuccess, T? Value, string? Error, IReadOnlyDictionary<string, string[]>? ValidationErrors)
{
    public static Result<T> Success(T value) => new(true, value, null, null);
    public static Result<T> Failure(string error) => new(false, default, error, null);
}

public sealed record Result(bool IsSuccess, string? Error, IReadOnlyDictionary<string, string[]>? ValidationErrors)
{
    public static Result Success() => new(true, null, null);
    public static Result Failure(string error) => new(false, error, null);
}
