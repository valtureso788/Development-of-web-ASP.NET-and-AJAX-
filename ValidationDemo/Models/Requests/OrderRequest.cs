using System.ComponentModel.DataAnnotations;
using ValidationDemo.Models.Dtos;

namespace ValidationDemo.Models.Requests;

/// <summary>
/// Запрос заказа — демонстрация валидации вложенных объектов.
/// </summary>
public class OrderRequest
{
    [Required(ErrorMessage = "Данные покупателя обязательны")]
    public CustomerDto Customer { get; set; } = new();

    [MinLength(1, ErrorMessage = "Заказ должен содержать хотя бы один товар")]
    public List<OrderItemDto> Items { get; set; } = new();
}
