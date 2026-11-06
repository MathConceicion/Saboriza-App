namespace GastronomiaMarketplace.WebAPI.Middlewares;

using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using GastronomiaMarketplace.Application.Common.Exceptions;

public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext ctx)
    {
        try
        {
            await _next(ctx);
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning("Validação falhou: {Errors}", ex.Errors);
            ctx.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            ctx.Response.ContentType = "application/problem+json";
            var problem = new
            {
                type = "https://httpstatuses.com/400",
                title = "Erro de validação",
                status = 400,
                errors = ex.Errors
            };
            await ctx.Response.WriteAsync(JsonSerializer.Serialize(problem));
        }
        catch (NotFoundException ex)
        {
            ctx.Response.StatusCode = (int)HttpStatusCode.NotFound;
            await ctx.Response.WriteAsJsonAsync(new { error = ex.Message });
        }
        catch (ForbiddenException ex)
        {
            ctx.Response.StatusCode = (int)HttpStatusCode.Forbidden;
            await ctx.Response.WriteAsJsonAsync(new { error = ex.Message });
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Conflito de concorrência");
            ctx.Response.StatusCode = (int)HttpStatusCode.Conflict;
            await ctx.Response.WriteAsJsonAsync(new { error = "Conflito de concorrência. Recarregue e tente novamente.", detail = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro não tratado");
            ctx.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            await ctx.Response.WriteAsJsonAsync(new { error = "Erro interno. Tente novamente mais tarde." });
        }
    }
}
