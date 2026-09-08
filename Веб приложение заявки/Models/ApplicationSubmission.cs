using System.ComponentModel.DataAnnotations;

namespace ApplicationPortal.Models;

public class ApplicationSubmission
{
    public string Id { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string Status { get; set; } = "На рассмотрении";

    [Required(ErrorMessage = "Пожалуйста, укажите ФИО")]
    [StringLength(150, MinimumLength = 2, ErrorMessage = "ФИО должно содержать от 2 до 150 символов")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Пожалуйста, выберите дату рождения")]
    public string BirthDate { get; set; } = string.Empty;

    public int? Age { get; set; }

    [Required(ErrorMessage = "Пожалуйста, укажите пол")]
    public string Gender { get; set; } = string.Empty;

    [Required(ErrorMessage = "Пожалуйста, укажите город проживания")]
    public string City { get; set; } = string.Empty;

    [Required(ErrorMessage = "Пожалуйста, укажите контактный телефон")]
    public string Phone { get; set; } = string.Empty;

    [Required(ErrorMessage = "Пожалуйста, укажите email")]
    [EmailAddress(ErrorMessage = "Укажите корректный адрес электронной почты")]
    public string Email { get; set; } = string.Empty;

    public string? Telegram { get; set; }

    [Required(ErrorMessage = "Пожалуйста, выберите сферу деятельности")]
    public string Category { get; set; } = string.Empty;

    [Required(ErrorMessage = "Пожалуйста, укажите желаемую должность")]
    public string DesiredRole { get; set; } = string.Empty;

    [Required(ErrorMessage = "Пожалуйста, укажите опыт работы")]
    public string Experience { get; set; } = string.Empty;

    public string WorkFormat { get; set; } = "Удалённая работа";

    public string? SalaryExpectation { get; set; }

    public List<string> Skills { get; set; } = new();

    public string? AboutMe { get; set; }

    public string? PhotoDataUrl { get; set; }
}
