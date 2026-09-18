using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace ValidationDemo.Infrastructure;

/// <summary>
/// Кастомная фабрика ProblemDetails.
/// Добавляет к каждому ответу: traceId, timestamp, environment (в режиме разработки).
/// </summary>
public class CustomProblemDetailsFactory : ProblemDetailsFactory
{
    private readonly IHostEnvironment _env;

    public CustomProblemDetailsFactory(IHostEnvironment env) => _env = env;

    public override ProblemDetails CreateProblemDetails(
        HttpContext httpContext,
        int? statusCode = null,
        string? title = null,
        string? type = null,
        string? detail = null,
        string? instance = null)
    {
        var pd = new ProblemDetails
        {
            Status = statusCode ?? 500,
            Title = title,
            Type = type,
            Detail = detail,
            Instance = instance ?? httpContext.Request.Path
        };

        Enrich(pd, httpContext);
        return pd;
    }

    public override ValidationProblemDetails CreateValidationProblemDetails(
        HttpContext httpContext,
        ModelStateDictionary modelStateDictionary,
        int? statusCode = null,
        string? title = null,
        string? type = null,
        string? detail = null,
        string? instance = null)
    {
        var vpd = new ValidationProblemDetails(modelStateDictionary)
        {
            Status = statusCode ?? 400,
            Title = title ?? "Validation failed",
            Type = type,
            Detail = detail,
            Instance = instance ?? httpContext.Request.Path
        };

        Enrich(vpd, httpContext);
        return vpd;
    }

    /// <summary>
    /// Обогащает ProblemDetails дополнительными полями (extensions).
    /// </summary>
    private void Enrich(ProblemDetails pd, HttpContext ctx)
    {
        pd.Extensions["traceId"] = ctx.TraceIdentifier;
        pd.Extensions["timestamp"] = DateTime.UtcNow;

        if (_env.IsDevelopment())
            pd.Extensions["environment"] = "Development";
    }
}
