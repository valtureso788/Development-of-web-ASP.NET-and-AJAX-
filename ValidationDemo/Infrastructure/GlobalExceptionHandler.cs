using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace ValidationDemo.Infrastructure;

/// <summary>
/// Глобальный обработчик необработанных исключений.
/// Реализует IExceptionHandler — маппинг типов исключений на HTTP-статусы.
/// </summary>
public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
        => _logger = logger;

    public async ValueTask<bool> TryHandleAsync(
        HttpContext ctx,
        Exception ex,
        CancellationToken ct)
    {
        _logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);

        // Маппинг типов исключений на HTTP-статусы
        var (status, title) = ex switch
        {
            KeyNotFoundException         => (StatusCodes.Status404NotFound,            "Not Found"),
            ArgumentException            => (StatusCodes.Status400BadRequest,          "Bad Request"),
            UnauthorizedAccessException  => (StatusCodes.Status403Forbidden,           "Forbidden"),
            NotImplementedException      => (StatusCodes.Status501NotImplemented,      "Not Implemented"),
            _                            => (StatusCodes.Status500InternalServerError, "Internal Server Error")
        };

        var pd = new ProblemDetails
        {
            Status   = status,
            Title    = title,
            Detail   = ex.Message,
            Instance = ctx.Request.Path
        };
        pd.Extensions["traceId"] = ctx.TraceIdentifier;
        pd.Extensions["timestamp"] = DateTime.UtcNow;
        pd.Extensions["exceptionType"] = ex.GetType().Name;

        ctx.Response.StatusCode  = status;
        ctx.Response.ContentType = "application/problem+json";

        await ctx.Response.WriteAsJsonAsync(pd, ct);
        return true; // исключение обработано, дальше не пробрасываем
    }
}
