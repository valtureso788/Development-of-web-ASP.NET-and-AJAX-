using FluentValidation;
using ValidationDemo.Models.Requests;

namespace ValidationDemo.Validation;

/// <summary>
/// FluentValidation-валидатор для CreateProductRequest.
/// Демонстрирует: NotEmpty, MinimumLength, MaximumLength, GreaterThan, LessThan, условие When.
/// </summary>
public class CreateProductValidator : AbstractValidator<CreateProductRequest>
{
    public CreateProductValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Имя обязательно")
            .MinimumLength(3).WithMessage("Имя должно содержать не менее 3 символов")
            .MaximumLength(100).WithMessage("Имя не должно превышать 100 символов");

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("Цена должна быть больше 0")
            .LessThan(1_000_000).WithMessage("Цена не должна превышать 1 000 000");

        RuleFor(x => x.CategoryId)
            .GreaterThan(0).WithMessage("Категория обязательна");

        // Условная валидация: если цена > 10000 — название должно быть длиннее
        When(x => x.Price > 10_000, () =>
        {
            RuleFor(x => x.Name)
                .MinimumLength(10)
                .WithMessage("Дорогие товары (>10 000) требуют названия не менее 10 символов");
        });
    }
}
