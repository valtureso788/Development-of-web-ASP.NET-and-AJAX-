using System.ComponentModel.DataAnnotations;

namespace ValidationDemo.Models.Dtos;

public class CustomerDto
{
    [Required(ErrorMessage = "Имя клиента обязательно")]
    [StringLength(100, MinimumLength = 2)]
    public string Name { get; set; } = "";

    [Required, EmailAddress]
    public string Email { get; set; } = "";

    [Phone]
    public string? Phone { get; set; }
}
