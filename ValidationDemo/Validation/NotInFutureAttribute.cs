using System.ComponentModel.DataAnnotations;

namespace ValidationDemo.Validation;

/// <summary>
/// Кастомный атрибут валидации: дата не может быть в будущем.
/// Наследует ValidationAttribute и переопределяет IsValid.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public class NotInFutureAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is DateTime dt && dt > DateTime.UtcNow)
            return new ValidationResult(
                ErrorMessage ?? "Дата не может быть в будущем",
                new[] { validationContext.MemberName! });

        return ValidationResult.Success;
    }
}
