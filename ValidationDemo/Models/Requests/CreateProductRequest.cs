namespace ValidationDemo.Models.Requests;

/// <summary>
/// Запрос создания продукта — валидируется через FluentValidation.
/// </summary>
public class CreateProductRequest
{
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
    public int CategoryId { get; set; }
    public string? Description { get; set; }
}
