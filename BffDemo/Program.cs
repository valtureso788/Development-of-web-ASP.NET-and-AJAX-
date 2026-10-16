// ============================================================
// ПРОЕКТ 3: BFF (Backend for Frontend) — Identity Boundary
// Демонстрирует: Cookie снаружи + JWT внутри.
// BFF хранит токены, SPA работает только с HttpOnly cookie.
// ============================================================
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

var jwtKey      = builder.Configuration["Jwt:Key"]      ?? "super-secret-key-at-least-32-chars!!";
var jwtIssuer   = builder.Configuration["Jwt:Issuer"]   ?? "bff-demo";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "bff-clients";

// ── 1. Две схемы: Cookie (снаружи) + JWT (внутри) ──────────
builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultScheme          = "Smart";
        options.DefaultChallengeScheme = "Smart";
    })
    // Smart Policy Scheme — выбирает схему автоматически
    .AddPolicyScheme("Smart", "Smart", options =>
    {
        options.ForwardDefaultSelector = ctx =>
            ctx.Request.Headers.Authorization.ToString()
                .StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                ? JwtBearerDefaults.AuthenticationScheme
                : CookieAuthenticationDefaults.AuthenticationScheme;
    })
    // Cookie — для браузерных клиентов
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
    {
        options.Cookie.Name         = ".Bff.Session";
        options.Cookie.HttpOnly     = true;
        options.Cookie.SameSite     = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.ExpireTimeSpan      = TimeSpan.FromHours(8);
    })
    // JWT Bearer — для M2M клиентов
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,  ValidIssuer   = jwtIssuer,
            ValidateAudience         = true,  ValidAudience = jwtAudience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

// ── 2. Политики авторизации ─────────────────────────────────
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("BrowserOnly", p => p
        .AddAuthenticationSchemes(CookieAuthenticationDefaults.AuthenticationScheme)
        .RequireAuthenticatedUser());

    options.AddPolicy("ApiOnly", p => p
        .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
        .RequireAuthenticatedUser());
});

builder.Services.AddAntiforgery();
builder.Services.AddHttpClient();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

// ── BFF endpoints (Cookie-схема) ────────────────────────────

// POST /bff/login — принимает credentials, выдаёт HttpOnly cookie
app.MapPost("/bff/login", async (HttpContext ctx, LoginDto dto) =>
{
    if (!UserStore.Verify(dto.Username, dto.Password))
        return Results.Unauthorized();

    var user = UserStore.Users[dto.Username];

    // Генерируем JWT для внутреннего использования
    var access = TokenService.CreateAccessToken(
        user.Username, user.Roles, jwtKey, jwtIssuer, jwtAudience,
        TimeSpan.FromMinutes(15));

    // Сохраняем токен на сервере — браузер НЕ видит его (identity boundary)
    var sid = Guid.NewGuid().ToString("N");
    SessionTokenStore.Save(sid, access, DateTimeOffset.UtcNow.AddMinutes(15));

    // Cookie содержит только sid, без токена
    var claims = new List<Claim>
    {
        new(ClaimTypes.NameIdentifier, dto.Username),
        new("sid", sid)
    };
    await ctx.SignInAsync(
        CookieAuthenticationDefaults.AuthenticationScheme,
        new ClaimsPrincipal(new ClaimsIdentity(claims,
            CookieAuthenticationDefaults.AuthenticationScheme)));

    return Results.Ok(new { message = "Вошли через BFF", username = dto.Username });
}).AllowAnonymous().DisableAntiforgery();

// POST /bff/logout
app.MapPost("/bff/logout", async (HttpContext ctx) =>
{
    var sid = ctx.User.FindFirst("sid")?.Value;
    if (sid is not null) SessionTokenStore.Remove(sid);

    await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Ok(new { message = "Вышли" });
}).RequireAuthorization("BrowserOnly").DisableAntiforgery();

// GET /bff/me — BFF проксирует к API, токен браузеру не виден
app.MapGet("/bff/me", (HttpContext ctx) =>
{
    if (ctx.User.Identity?.IsAuthenticated != true)
        return Results.Unauthorized();

    var sid    = ctx.User.FindFirst("sid")!.Value;
    var access = SessionTokenStore.GetValidAccess(sid);

    if (access is null)
        return Results.Unauthorized();

    // Симулируем ответ внутреннего API
    var handler = new JwtSecurityTokenHandler();
    var jwt     = handler.ReadJwtToken(access);

    return Results.Ok(new
    {
        source   = "BFF: Cookie снаружи → JWT внутри (identity boundary)",
        username = ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
        roles    = jwt.Claims.Where(c => c.Type == "role").Select(c => c.Value),
        jti      = jwt.Id,
        expires  = jwt.ValidTo
    });
}).RequireAuthorization("BrowserOnly");

// ── API endpoints (JWT-схема, M2M) ──────────────────────────

// POST /api/token — прямая выдача JWT для машинных клиентов
app.MapPost("/api/token", (LoginDto dto) =>
{
    if (!UserStore.Verify(dto.Username, dto.Password))
        return Results.Unauthorized();

    var user   = UserStore.Users[dto.Username];
    var access = TokenService.CreateAccessToken(
        user.Username, user.Roles, jwtKey, jwtIssuer, jwtAudience,
        TimeSpan.FromMinutes(15));

    return Results.Ok(new { access_token = access, token_type = "Bearer", expires_in = 900 });
}).AllowAnonymous();

// GET /api/data — только JWT
app.MapGet("/api/data", (HttpContext ctx) =>
    Results.Ok(new
    {
        source = "Внутренний API (только JWT Bearer)",
        sub    = ctx.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value,
        roles  = ctx.User.FindAll("role").Select(c => c.Value)
    })
).RequireAuthorization("ApiOnly");

// GET /api/admin — только admin через JWT
app.MapGet("/api/admin", (HttpContext ctx) =>
    Results.Ok(new { message = "Админ-API", sub = ctx.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value })
).RequireAuthorization(p =>
{
    p.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme);
    p.RequireAuthenticatedUser();
    p.RequireClaim("role", "admin");
});

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

    public static bool Verify(string u, string p) =>
        Users.TryGetValue(u, out var user) && user.PasswordHash == Hash(p);
}

// BFF хранит токены на сервере — identity boundary
static class SessionTokenStore
{
    private record Entry(string AccessToken, DateTimeOffset Expires);
    private static readonly Dictionary<string, Entry> _map = [];

    public static void Save(string sid, string access, DateTimeOffset exp) =>
        _map[sid] = new Entry(access, exp);

    public static string? GetValidAccess(string sid) =>
        _map.TryGetValue(sid, out var e) && e.Expires > DateTimeOffset.UtcNow
            ? e.AccessToken
            : null;

    public static void Remove(string sid) => _map.Remove(sid);
}

static class TokenService
{
    public static string CreateAccessToken(
        string username, string[] roles,
        string key, string issuer, string audience,
        TimeSpan lifetime)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, username),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
        };
        claims.AddRange(roles.Select(r => new Claim("role", r)));

        var creds = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(issuer, audience, claims,
            notBefore:          DateTime.UtcNow,
            expires:            DateTime.UtcNow.Add(lifetime),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
