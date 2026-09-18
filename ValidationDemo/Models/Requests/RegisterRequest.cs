using System.ComponentModel.DataAnnotations;

namespace ValidationDemo.Models.Requests;

/// <summary>
/// Запрос на регистрацию — демонстрация стандартных Data Annotations.
/// </summary>
public class RegisterRequest
{
    [Required(ErrorMessage = "Email обязателен")]
    [EmailAddress(ErrorMessage = "Некорректный формат email")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Пароль обязателен")]
    [StringLength(100, MinimumLength = 8,
        ErrorMessage = "Пароль должен содержать от 8 до 100 символов")]
    [RegularExpression(@"^(?=.*[A-Za-z])(?=.*\d).+$",
        ErrorMessage = "Нужны буквы и цифры")]
    public string Password { get; set; } = "";

    [Required(ErrorMessage = "Подтверждение пароля обязательно")]
    [Compare(nameof(Password), ErrorMessage = "Пароли не совпадают")]
    public string ConfirmPassword { get; set; } = "";

    [Range(18, 120, ErrorMessage = "Возраст должен быть от 18 до 120 лет")]
    public int Age { get; set; }
}
