using System.Diagnostics;
using ASP_pystoy.Middleware;
using ASP_pystoy.Models;
using ASP_pystoy.Services;

var builder = WebApplication.CreateBuilder(args);

// ==========================================
// 1. РЕГИСТРАЦИЯ ЗАВИСИМОСТЕЙ (DI LIFETIMES)
// ==========================================

// Монитор состояния хоста и истории запросов (Singleton)
builder.Services.AddSingleton<IAppLifetimeMonitor, AppLifetimeMonitor>();

// Демонстрация 3-х жизненных циклов DI:
// 1. Transient: создается каждый раз заново
builder.Services.AddTransient<IOperationTransient, Operation>();

// 2. Scoped: создается один раз на каждый HTTP-запрос
builder.Services.AddScoped<IOperationScoped, Operation>();

// 3. Singleton: создается один раз на всё время жизни хоста
builder.Services.AddSingleton<IOperationSingleton, Operation>();

// Сервис, использующий все три операции для демонстрации их поведения
builder.Services.AddTransient<IOperationService, OperationService>();

var app = builder.Build();

// ==========================================
// 2. ЖИЗНЕННЫЙ ЦИКЛ ХОСТА (IHostApplicationLifetime)
// ==========================================
var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
var monitor = app.Services.GetRequiredService<IAppLifetimeMonitor>();
var logger = app.Services.GetRequiredService<ILogger<Program>>();

lifetime.ApplicationStarted.Register(() =>
{
    monitor.SetHostStatus("Running");
    monitor.RecordEvent("ApplicationStarted", "Приложение успешно запущено и готово к приему входящих запросов.");
    
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine("""
╔═══════════════════════════════════════════════════════════════════════════════════╗
║                   [HOST LIFECYCLE: ApplicationStarted]                            ║
║  Хост приложения ASP.NET Core полностью сконфигурирован и успешно запущен!        ║
║  Службы DI, кастомные Middleware и обработчики маршрутов активны.                  ║
╚═══════════════════════════════════════════════════════════════════════════════════╝
""");
    Console.ResetColor();
});

lifetime.ApplicationStopping.Register(() =>
{
    monitor.SetHostStatus("Stopping");
    monitor.RecordEvent("ApplicationStopping", "Инициирован процесс корректной остановки хоста (Graceful Shutdown).");

    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine("""
╔═══════════════════════════════════════════════════════════════════════════════════╗
║                   [HOST LIFECYCLE: ApplicationStopping]                           ║
║  Получен сигнал завершения работы приложения (SIGINT/Ctrl+C).                     ║
║  Завершение активных запросов и освобождение ресурсов...                          ║
╚═══════════════════════════════════════════════════════════════════════════════════╝
""");
    Console.ResetColor();
});

lifetime.ApplicationStopped.Register(() =>
{
    monitor.SetHostStatus("Stopped");
    monitor.RecordEvent("ApplicationStopped", "Все службы остановлены. Приложение полностью завершило работу.");

    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine("""
╔═══════════════════════════════════════════════════════════════════════════════════╗
║                   [HOST LIFECYCLE: ApplicationStopped]                            ║
║  Хост приложения ASP.NET Core завершил работу. Ресурсы освобождены.               ║
╚═══════════════════════════════════════════════════════════════════════════════════╝
""");
    Console.ResetColor();
});

// ==========================================
// 3. КОНВЕЙЕР ОБРАБОТКИ ЗАПРОСОВ (MIDDLEWARE)
// ==========================================

// 1) Кастомный Middleware для логирования жизненного цикла каждого HTTP-запроса
app.UseRequestLifecycle();

// 2) Кастомный Middleware для перехвата и валидации данных формы
app.UseFormProcessing();

// 3) Раздача статических файлов и главной страницы из wwwroot
app.UseDefaultFiles();
app.UseStaticFiles();

// ==========================================
// 4. МАРШРУТЫ И КОНЕЧНЫЕ ТОЧКИ (ENDPOINTS)
// ==========================================

// API для получения данных жизненного цикла хоста, DI и журнала запросов
app.MapGet("/api/lifecycle", (
    IOperationTransient transientOp,
    IOperationScoped scopedOp,
    IOperationSingleton singletonOp,
    IOperationService opService,
    IAppLifetimeMonitor appMonitor) =>
{
    return Results.Ok(new
    {
        hostStatus = appMonitor.HostStatus,
        uptime = $"{appMonitor.Uptime.Hours:D2}ч {appMonitor.Uptime.Minutes:D2}м {appMonitor.Uptime.Seconds:D2}с",
        hostStartTime = appMonitor.HostStartTime,
        events = appMonitor.LifecycleEvents,
        di = new
        {
            transient1 = transientOp.OperationId.ToString(),
            transient2 = opService.TransientOperation.OperationId.ToString(),
            scoped1 = scopedOp.OperationId.ToString(),
            scoped2 = opService.ScopedOperation.OperationId.ToString(),
            singleton1 = singletonOp.OperationId.ToString(),
            singleton2 = opService.SingletonOperation.OperationId.ToString()
        },
        recentRequests = appMonitor.RecentRequests
    });
});

// Обработчик отправки формы (поддерживает обычную отправку и AJAX)
app.MapPost("/submit", (
    HttpContext context,
    IOperationTransient transientOp,
    IOperationScoped scopedOp,
    IOperationSingleton singletonOp,
    IOperationService opService,
    IAppLifetimeMonitor appMonitor) =>
{
    // Проверяем ошибки валидации, зафиксированные Middleware
    if (context.Items.TryGetValue("ValidationError", out var validationError) && validationError != null)
    {
        return Results.BadRequest(new
        {
            success = false,
            message = validationError.ToString()
        });
    }

    var name = context.Items["FormName"]?.ToString() ?? "Не указано";
    var email = context.Items["FormEmail"]?.ToString() ?? "Не указано";
    var category = context.Items["FormCategory"]?.ToString() ?? "Общие вопросы";
    var message = context.Items["FormMessage"]?.ToString() ?? "";
    var requestId = context.Items["RequestId"]?.ToString() ?? Guid.NewGuid().ToString("N")[..8];

    var formEntry = new FeedbackFormModel
    {
        Name = name,
        Email = email,
        Category = category,
        Message = message,
        RequestId = requestId,
        SubmittedAt = DateTime.UtcNow
    };

    appMonitor.RecordFormSubmission(formEntry);

    var isAjax = context.Request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
                 context.Request.Headers.Accept.ToString().Contains("application/json");

    var responseData = new
    {
        success = true,
        message = "Форма успешно принята и обработана сервером!",
        requestId,
        middlewareValidated = context.Items.ContainsKey("MiddlewareProcessed"),
        receivedData = new
        {
            name,
            email,
            category,
            messageLength = message.Length
        },
        transientOperationId = transientOp.OperationId.ToString(),
        scopedOperationId = scopedOp.OperationId.ToString(),
        singletonOperationId = singletonOp.OperationId.ToString()
    };

    if (isAjax)
    {
        return Results.Ok(responseData);
    }

    // Для стандартной HTML-отправки формы без AJAX - страница подтверждения
    var html = $"""
    <!DOCTYPE html>
    <html lang="ru">
    <head>
        <meta charset="UTF-8">
        <title>Форма обработана</title>
        <link href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.2/dist/css/bootstrap.min.css" rel="stylesheet">
    </head>
    <body class="bg-dark text-light d-flex align-items-center justify-content-center" style="min-height: 100vh;">
        <div class="card bg-secondary text-white p-4 shadow-lg" style="max-width: 550px; width: 100%; border-radius: 1rem;">
            <h3 class="text-success mb-3">✓ Данные формы приняты!</h3>
            <p><strong>Имя:</strong> {name}</p>
            <p><strong>Email:</strong> {email}</p>
            <p><strong>Категория:</strong> {category}</p>
            <p><strong>Request ID:</strong> <code>{requestId}</code></p>
            <hr/>
            <h5>Жизненные циклы этого запроса:</h5>
            <p class="small"><strong>Transient ID:</strong> <code>{transientOp.OperationId}</code></p>
            <p class="small"><strong>Scoped ID:</strong> <code>{scopedOp.OperationId}</code></p>
            <p class="small"><strong>Singleton ID:</strong> <code>{singletonOp.OperationId}</code></p>
            <a href="/" class="btn btn-primary mt-3">← Вернуться назад</a>
        </div>
    </body>
    </html>
    """;

    return Results.Content(html, "text/html; charset=utf-8");
});

app.Run();
