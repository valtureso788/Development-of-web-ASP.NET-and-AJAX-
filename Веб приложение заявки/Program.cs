using ApplicationPortal.Services;

var builder = WebApplication.CreateBuilder(args);

// Регистрация сервисов
builder.Services.AddControllers();
builder.Services.AddSingleton<ApplicationStorageService>();

var app = builder.Build();

// Настройка HTTP конвейера
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseRouting();

app.MapControllers();
app.MapFallbackToFile("index.html");

Console.WriteLine("=================================================");
Console.WriteLine(" Веб-приложение заявок на ASP.NET Core запущено!");
Console.WriteLine(" Адрес: http://localhost:5000");
Console.WriteLine("=================================================");

app.Run("http://localhost:5000");
