using System.Diagnostics;
using ASP_pystoy.Models;
using ASP_pystoy.Services;

namespace ASP_pystoy.Middleware;

public class RequestLifecycleMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLifecycleMiddleware> _logger;

    public RequestLifecycleMiddleware(RequestDelegate next, ILogger<RequestLifecycleMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IAppLifetimeMonitor monitor)
    {
        var stopwatch = Stopwatch.StartNew();
        var requestId = Guid.NewGuid().ToString("N")[..8];
        var timestamp = DateTime.UtcNow;
        var path = context.Request.Path.Value ?? "/";
        var method = context.Request.Method;
        var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

        context.Items["RequestId"] = requestId;
        context.Items["StartTime"] = timestamp;

        _logger.LogInformation("[MIDDLEWARE IN] -> Request {RequestId}: {Method} {Path} at {Time:HH:mm:ss.fff}",
            requestId, method, path, timestamp);

        // Добавляем кастомные заголовки жизненного цикла в ответ
        context.Response.OnStarting(() =>
        {
            context.Response.Headers["X-Request-Id"] = requestId;
            context.Response.Headers["X-Lifecycle-Stage"] = "RequestLifecycleMiddleware";
            context.Response.Headers["X-Elapsed-Ms"] = stopwatch.ElapsedMilliseconds.ToString();
            return Task.CompletedTask;
        });

        try
        {
            // Передаем управление дальше по конвейеру middleware
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();
            var statusCode = context.Response.StatusCode;

            _logger.LogInformation("[MIDDLEWARE OUT] <- Request {RequestId}: {StatusCode} completed in {Elapsed} ms",
                requestId, statusCode, stopwatch.Elapsed.TotalMilliseconds);

            // Сохраняем в монитор жизненного цикла для визуализации
            monitor.RecordRequest(new RequestLogEntry
            {
                Id = requestId,
                Method = method,
                Path = path,
                StatusCode = statusCode,
                ElapsedMilliseconds = Math.Round(stopwatch.Elapsed.TotalMilliseconds, 2),
                Timestamp = timestamp,
                ClientIp = clientIp,
                Details = context.Items.ContainsKey("FormCategory") ? $"Категория: {context.Items["FormCategory"]}" : null
            });
        }
    }
}

public static class RequestLifecycleMiddlewareExtensions
{
    public static IApplicationBuilder UseRequestLifecycle(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<RequestLifecycleMiddleware>();
    }
}
