using System.ComponentModel.DataAnnotations;

namespace ValidationDemo.Models.Dtos;

public class OrderItemDto
{
    [Required(ErrorMessage = "Название товара обязательно")]
    [StringLength(200)]
    public string ProductName { get; set; } = "";

    [Range(1, 10_000, ErrorMessage = "Количество должно быть от 1 до 10 000")]
    public int Quantity { get; set; }

    [Range(0.01, 1_000_000, ErrorMessage = "Цена должна быть от 0.01 до 1 000 000")]
    public decimal UnitPrice { get; set; }
}
