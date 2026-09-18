using Microsoft.AspNetCore.Mvc;
using ValidationDemo.Models.Requests;

namespace ValidationDemo.Controllers;

/// <summary>
/// Демонстрация валидации через Data Annotations (RegisterRequest).
/// [ApiController] автоматически возвращает 400 ValidationProblemDetails при невалидном ModelState.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    /// <summary>
    /// Регистрация пользователя.
    /// Валидируется: Email, Password (длина + regex), ConfirmPassword (Compare), Age (Range).
    /// </summary>
    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public IActionResult Register([FromBody] RegisterRequest request)
    {
        // ModelState уже проверен [ApiController] — сюда попадаем только при валидных данных
        return Ok(new
        {
            message = "Регистрация успешна",
            email = request.Email,
            age = request.Age
        });
    }
}
