// ============================================================
// ПРОЕКТ 1: Cookie-аутентификация
// Демонстрирует: login/logout, claims, роли, CSRF-защиту,
//               revocation через session-id
// ============================================================
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Antiforgery;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// ── 1. Cookie-аутентификация ────────────────────────────────
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name        = ".MyApp.Auth";
        options.Cookie.HttpOnly    = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest; // Always в продакшне
        options.Cookie.SameSite    = SameSiteMode.Lax;
        options.ExpireTimeSpan     = TimeSpan.FromHours(8);
        options.SlidingExpiration  = true;
        options.LoginPath          = "/login";
        options.LogoutPath         = "/logout";
        options.AccessDeniedPath   = "/forbidden";

        // Проверяем при каждом запросе, не отозвана ли сессия
        options.Events = new CookieAuthenticationEvents
        {
            OnValidatePrincipal = ctx =>
            {
                var sid = ctx.Principal?.FindFirst("sid")?.Value;
                if (sid is null || RevokedSessions.IsRevoked(sid))
                    ctx.RejectPrincipal();
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddAntiforgery();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

// GET /login — возвращает CSRF-токен (в реальном приложении — HTML-форма)
app.MapGet("/login", (HttpContext ctx) =>
{
    var antiforgery = ctx.RequestServices.GetRequiredService<IAntiforgery>();
    var tokens = antiforgery.GetAndStoreTokens(ctx);
    return Results.Ok(new
    {
        message    = "Отправьте POST /login с username, password",
        csrf_token = tokens.RequestToken
    });
}).AllowAnonymous();

// POST /login — выдаёт cookie
app.MapPost("/login", async (HttpContext ctx, LoginDto dto) =>
{
    if (!UserStore.Verify(dto.Username, dto.Password))
        return Results.Unauthorized();

    var user = UserStore.Users[dto.Username];
    var sid  = Guid.NewGuid().ToString("N"); // session-id для revocation

    var claims = new List<Claim>
    {
        new(ClaimTypes.Name,           user.Username),
        new(ClaimTypes.NameIdentifier, user.Username),
        new("sid",                     sid)
    };
    claims.AddRange(user.Roles.Select(r => new Claim(ClaimTypes.Role, r)));

    var identity  = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
    var principal = new ClaimsPrincipal(identity);

    await ctx.SignInAsync(
        CookieAuthenticationDefaults.AuthenticationScheme,
        principal,
        new AuthenticationProperties { IsPersistent = true });

    return Results.Ok(new { message = "Вошли", username = user.Username });
}).AllowAnonymous().DisableAntiforgery();

// POST /logout — отзывает сессию и удаляет cookie
app.MapPost("/logout", async (HttpContext ctx) =>
{
    var sid = ctx.User.FindFirst("sid")?.Value;
    if (sid is not null)
        RevokedSessions.Revoke(sid); // инвалидируем сессию

    await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Ok(new { message = "Вышли" });
}).RequireAuthorization().DisableAntiforgery();

// GET /me — информация о текущем пользователе
app.MapGet("/me", (HttpContext ctx) =>
    Results.Ok(new
    {
        username = ctx.User.Identity?.Name,
        roles    = ctx.User.FindAll(ClaimTypes.Role).Select(c => c.Value),
        sid      = ctx.User.FindFirst("sid")?.Value
    })
).RequireAuthorization();

// GET /admin — только для роли admin
app.MapGet("/admin", (HttpContext ctx) =>
    Results.Ok(new { message = "Добро пожаловать в админку!", user = ctx.User.Identity?.Name })
).RequireAuthorization(policy => policy.RequireRole("admin"));

// GET /forbidden — страница отказа в доступе
app.MapGet("/forbidden", () => Results.Json(
    new { error = "Доступ запрещён" }, statusCode: 403)
).AllowAnonymous();

app.Run();

// ── Типы ────────────────────────────────────────────────────
record LoginDto(string Username, string Password);
record AppUser(string Username, string PasswordHash, string[] Roles);

static class UserStore
{
    public static readonly Dictionary<string, AppUser> Users = new()
    {
        ["alice"] = new AppUser("alice", Hash("p@ss"), ["user", "admin"]),
        ["bob"]   = new AppUser("bob",   Hash("p@ss"), ["user"])
    };

    public static string Hash(string s) =>
        Convert.ToBase64String(System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(s)));

    public static bool Verify(string username, string password) =>
        Users.TryGetValue(username, out var u) && u.PasswordHash == Hash(password);
}

static class RevokedSessions
{
    private static readonly HashSet<string> _revoked = [];
    public static bool IsRevoked(string sid) => _revoked.Contains(sid);
    public static void Revoke(string sid) => _revoked.Add(sid);
}
