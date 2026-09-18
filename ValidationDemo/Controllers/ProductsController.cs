using Microsoft.AspNetCore.Mvc;
using ValidationDemo.Models.Requests;

namespace ValidationDemo.Controllers;

/// <summary>
/// Демонстрация FluentValidation и GlobalExceptionHandler.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    // Имитация "базы данных" для демонстрации
    private static readonly Dictionary<int, string> _products = new()
    {
        { 1, "Карандаш" },
        { 2, "Ноутбук премиум класса" },
        { 50, "Средний товар" }
    };

    /// <summary>
    /// Создание продукта. Валидация через FluentValidation (CreateProductValidator).
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public IActionResult CreateProduct([FromBody] CreateProductRequest request)
    {
        // FluentValidation срабатывает автоматически через AddFluentValidationAutoValidation()
        // Сюда попадаем только при валидных данных

        // Пример: возврат 409 Conflict через ProblemDetails
        if (_products.Values.Any(n => n.Equals(request.Name, StringComparison.OrdinalIgnoreCase)))
        {
            var pd = new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Conflict",
                Detail = $"Продукт с именем '{request.Name}' уже существует",
                Instance = HttpContext.Request.Path
            };
            pd.Extensions["traceId"] = HttpContext.TraceIdentifier;
            return new ObjectResult(pd) { StatusCode = StatusCodes.Status409Conflict };
        }

        var newId = _products.Count + 1;
        _products[newId] = request.Name;

        return CreatedAtAction(nameof(GetProduct), new { id = newId }, new
        {
            id = newId,
            name = request.Name,
            price = request.Price,
            categoryId = request.CategoryId
        });
    }

    /// <summary>
    /// Получение продукта по id.
    /// Если id > 100 — бросает KeyNotFoundException → GlobalExceptionHandler → 404 ProblemDetails.
    /// Если id == 0  — бросает ArgumentException   → GlobalExceptionHandler → 400 ProblemDetails.
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public IActionResult GetProduct(int id)
    {
        if (id <= 0)
            throw new ArgumentException($"Id должен быть положительным числом, получен: {id}");

        if (!_products.TryGetValue(id, out var name))
            throw new KeyNotFoundException($"Продукт с id {id} не найден");

        return Ok(new { id, name });
    }

    /// <summary>
    /// Демо: ручной возврат 404 через Problem() helper.
    /// </summary>
    [HttpGet("{id:int}/manual-problem")]
    public IActionResult GetWithManualProblem(int id)
    {
        if (!_products.TryGetValue(id, out var name))
            return Problem(
                title: "Product not found",
                detail: $"Product with id {id} does not exist",
                statusCode: StatusCodes.Status404NotFound,
                type: "https://example.com/errors/not-found");

        return Ok(new { id, name });
    }
}
