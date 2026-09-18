using Microsoft.AspNetCore.Mvc;
using ValidationDemo.Models.Requests;

namespace ValidationDemo.Controllers;

/// <summary>
/// Демонстрация кастомного ValidationAttribute [NotInFuture].
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class EventsController : ControllerBase
{
    /// <summary>
    /// Создание события.
    /// Дата не может быть в будущем — проверяется кастомным атрибутом [NotInFuture].
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public IActionResult CreateEvent([FromBody] EventRequest request)
    {
        return CreatedAtAction(nameof(CreateEvent), new
        {
            message = "Событие создано",
            title = request.Title,
            date = request.Date
        });
    }
}
