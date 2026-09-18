using System.ComponentModel.DataAnnotations;

namespace ValidationDemo.Models.Requests;

/// <summary>
/// Запрос диапазона дат — демонстрация IValidatableObject (межполевая валидация).
/// </summary>
public class DateRangeRequest : IValidatableObject
{
    [Required]
    public DateTime From { get; set; }

    [Required]
    public DateTime To { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        // Правило 1: To должно быть больше From
        if (To <= From)
            yield return new ValidationResult(
                "To должно быть больше From",
                new[] { nameof(To) });

        // Правило 2: диапазон не должен превышать 365 дней
        if ((To - From).TotalDays > 365)
            yield return new ValidationResult(
                "Диапазон не должен превышать год",
                new[] { nameof(From), nameof(To) });
    }
}
