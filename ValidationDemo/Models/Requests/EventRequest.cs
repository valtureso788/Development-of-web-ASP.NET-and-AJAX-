using ValidationDemo.Validation;

namespace ValidationDemo.Models.Requests;

/// <summary>
/// Запрос события — демонстрация кастомного ValidationAttribute [NotInFuture].
/// </summary>
public class EventRequest
{
    /// <summary>
    /// Дата события (не может быть в будущем).
    /// </summary>
    [NotInFuture(ErrorMessage = "Дата события не может быть в будущем")]
    public DateTime Date { get; set; }

    public string Title { get; set; } = "";
}
