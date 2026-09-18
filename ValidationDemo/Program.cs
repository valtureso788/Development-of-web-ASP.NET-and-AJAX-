using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using ValidationDemo.Infrastructure;
using ValidationDemo.Validation;

var builder = WebApplication.CreateBuilder(args);

// ─── Controllers ──────────────────────────────────────────────────────────────
builder.Services.AddControllers();

// ─── Swagger / OpenAPI ────────────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "ValidationDemo API",
        Version = "v1",
        Description = "Демонстрация валидации и обработки ошибок в ASP.NET Core"
    });
    // Включаем XML-комментарии для Swagger
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
        options.IncludeXmlComments(xmlPath);
});

// ─── FluentValidation ─────────────────────────────────────────────────────────
// Автоматическая валидация через pipeline (заменяет [ApiController] для FluentValidation-моделей)
builder.Services.AddFluentValidationAutoValidation();
// Регистрация всех валидаторов из сборки CreateProductValidator
builder.Services.AddValidatorsFromAssemblyContaining<CreateProductValidator>();

// ─── ProblemDetails ───────────────────────────────────────────────────────────
// Автоматическая обработка 401, 403, 404, 500 и т.д.
builder.Services.AddProblemDetails();
// Кастомная фабрика: добавляет traceId, timestamp, environment ко всем ProblemDetails
builder.Services.AddSingleton<ProblemDetailsFactory, CustomProblemDetailsFactory>();

// ─── Global Exception Handling ────────────────────────────────────────────────
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

var app = builder.Build();

// ─── Middleware pipeline ──────────────────────────────────────────────────────
// ВАЖНО: UseExceptionHandler должен быть первым!
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "ValidationDemo API v1");
        c.RoutePrefix = string.Empty; // Swagger UI на корне: http://localhost:PORT/
    });
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();
