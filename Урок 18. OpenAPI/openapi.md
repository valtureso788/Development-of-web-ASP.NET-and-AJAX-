//Open Api
Поле 		Назначение 				Пример в C#
Title 		Название API в документации 		"Todo Management API"
Version 	Версия API (не версия спецификации) 	"1.0.0"
Description 	Подробное описание с Markdown		"API для управления задачами"
Contact 	Контактные данные команды 		new OpenApiContact { Name = "Support", Email = "support@example.com" }
License 	Условия использования 			new OpenApiLicense { Name = "MIT", Url = new Uri(...) }

```c3
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Todo Management API",
        Version = "v1",
        Description = "API для управления задачами. Поддерживает фильтрацию и создание.",
        Contact = new OpenApiContact
        {
            Name = "API Support",
            Email = "support@example.com"
        },
        License = new OpenApiLicense
        {
            Name = "MIT",
            Url = new Uri("https://opensource.org/licenses/MIT")
        }
    });
});
```