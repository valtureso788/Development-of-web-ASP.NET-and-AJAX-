using ASP_pystoy.Models;
using ASP_pystoy.Services;

namespace ASP_pystoy.Middleware;

public class FormProcessingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<FormProcessingMiddleware> _logger;

    public FormProcessingMiddleware(RequestDelegate next, ILogger<FormProcessingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IAppLifetimeMonitor monitor)
    {
        // Проверяем, направлен ли запрос на обработку формы
        if (context.Request.Path.StartsWithSegments("/submit", StringComparison.OrdinalIgnoreCase) &&
            HttpMethods.IsPost(context.Request.Method))
        {
            _logger.LogInformation("[FORM MIDDLEWARE] Перехвачена отправка формы. Анализ данных запроса...");

            // Проверяем тип содержимого
            if (context.Request.HasFormContentType)
            {
                var form = await context.Request.ReadFormAsync();
                var name = form["name"].ToString();
                var email = form["email"].ToString();
                var category = form["category"].ToString();
                var message = form["message"].ToString();

                _logger.LogInformation("[FORM MIDDLEWARE] Получены поля: Имя='{Name}', Email='{Email}', Категория='{Category}'",
                    name, email, category);

                context.Items["FormName"] = name;
                context.Items["FormEmail"] = email;
                context.Items["FormCategory"] = category;
                context.Items["FormMessage"] = message;
                context.Items["MiddlewareProcessed"] = true;

                // Базовая валидация на уровне Middleware
                if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email))
                {
                    _logger.LogWarning("[FORM MIDDLEWARE VALIDATION] Ошибка валидации: Имя или Email не заполнены!");
                    context.Items["ValidationError"] = "Поля 'Имя' и 'Email' обязательны для заполнения!";
                }
            }
        }

        await _next(context);
    }
}

public static class FormProcessingMiddlewareExtensions
{
    public static IApplicationBuilder UseFormProcessing(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<FormProcessingMiddleware>();
    }
}
