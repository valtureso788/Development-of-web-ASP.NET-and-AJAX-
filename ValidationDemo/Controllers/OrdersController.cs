using Microsoft.AspNetCore.Mvc;
using ValidationDemo.Models.Requests;

namespace ValidationDemo.Controllers;

/// <summary>
/// Демонстрация двух паттернов:
/// 1. IValidatableObject (DateRangeRequest) — межполевая валидация.
/// 2. Вложенные объекты (OrderRequest с CustomerDto и OrderItemDto).
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    /// <summary>
    /// Запрос отчёта за диапазон дат.
    /// To должно быть больше From, диапазон не более 365 дней — IValidatableObject.
    /// </summary>
    [HttpPost("report")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public IActionResult GetReport([FromBody] DateRangeRequest request)
    {
        return Ok(new
        {
            message = "Отчёт сформирован",
            from = request.From,
            to = request.To,
            days = (request.To - request.From).TotalDays
        });
    }

    /// <summary>
    /// Создание заказа. Демонстрация валидации вложенных объектов.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public IActionResult CreateOrder([FromBody] OrderRequest request)
    {
        // Пример ручного добавления ошибки в ModelState и возврата ValidationProblem
        if (request.Customer.Email.EndsWith("@spam.com", StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError("Customer.Email", "Email домена @spam.com не принимается");
            return ValidationProblem(ModelState);
        }

        return CreatedAtAction(nameof(CreateOrder), new
        {
            message = "Заказ создан",
            customerEmail = request.Customer.Email,
            itemCount = request.Items.Count
        });
    }
}
