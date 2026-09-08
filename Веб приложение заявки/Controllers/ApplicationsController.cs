using Microsoft.AspNetCore.Mvc;
using ApplicationPortal.Models;
using ApplicationPortal.Services;

namespace ApplicationPortal.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ApplicationsController : ControllerBase
{
    private readonly ApplicationStorageService _storageService;

    public ApplicationsController(ApplicationStorageService storageService)
    {
        _storageService = storageService;
    }

    /// <summary>
    /// Получение списка всех поданных заявок
    /// </summary>
    [HttpGet]
    public IActionResult GetAll()
    {
        var list = _storageService.GetAll();
        return Ok(new { success = true, count = list.Count, data = list });
    }

    /// <summary>
    /// Получение анкеты по ID
    /// </summary>
    [HttpGet("{id}")]
    public IActionResult GetById(string id)
    {
        var app = _storageService.GetById(id);
        if (app == null)
        {
            return NotFound(new { success = false, message = $"Заявка с номером {id} не найдена" });
        }

        return Ok(new { success = true, data = app });
    }

    /// <summary>
    /// Создание или обновление заявки (AJAX Endpoint)
    /// </summary>
    [HttpPost]
    public IActionResult SubmitApplication([FromBody] ApplicationSubmission model)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState
                .Where(x => x.Value?.Errors.Count > 0)
                .ToDictionary(
                    k => k.Key,
                    v => v.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? Array.Empty<string>()
                );

            return BadRequest(new
            {
                success = false,
                message = "Пожалуйста, проверьте правильность заполнения полей формы.",
                errors
            });
        }

        // Автоматический расчёт возраста по дате рождения, если указана
        if (DateTime.TryParse(model.BirthDate, out var bDate))
        {
            var today = DateTime.Today;
            var age = today.Year - bDate.Year;
            if (bDate.Date > today.AddYears(-age)) age--;
            model.Age = Math.Max(0, age);
        }

        var saved = _storageService.Save(model);

        return Ok(new
        {
            success = true,
            message = "Заявка успешно зарегистрирована в системе ASP.NET!",
            data = saved
        });
    }

    /// <summary>
    /// Удаление заявки по ID
    /// </summary>
    [HttpDelete("{id}")]
    public IActionResult Delete(string id)
    {
        var deleted = _storageService.Delete(id);
        if (!deleted)
        {
            return NotFound(new { success = false, message = "Заявка не найдена" });
        }

        return Ok(new { success = true, message = "Заявка удалена" });
    }

    /// <summary>
    /// Очистка всех заявок
    /// </summary>
    [HttpDelete]
    public IActionResult ClearAll()
    {
        _storageService.ClearAll();
        return Ok(new { success = true, message = "Все заявки очищены" });
    }
}
