// ============================================================
// ПРОЕКТ 2: JWT Bearer аутентификация
// Демонстрирует: выдача access/refresh токенов, rotation,
//               revocation через jti-список, роли в claims
// ============================================================
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// ── Конфигурация (в продакшне — через appsettings / secrets) ──
var jwtKey      = builder.Configuration["Jwt:Key"]      ?? "super-secret-key-at-least-32-chars!!";
var jwtIssuer   = builder.Configuration["Jwt:Issuer"]   ?? "jwt-demo";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "jwt-demo-clients";

// ── 1. JWT Bearer аутентификация ────────────────────────────
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,  ValidIssuer   = jwtIssuer,
            ValidateAudience         = true,  ValidAudience = jwtAudience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero // убираем стандартные 5 мин. допуска
        };

        // Проверяем, не отозван ли токен (по jti)
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = ctx =>
            {
                var jti = ctx.Principal?.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
                if (jti is not null && RevokedTokens.IsRevoked(jti))
                    ctx.Fail("Token revoked");
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

// POST /token — логин, выдача access + refresh токена
app.MapPost("/token", (TokenRequest req) =>
{
    if (!UserStore.Verify(req.Username, req.Password))
        return Results.Unauthorized();

    var user    = UserStore.Users[req.Username];
    var access  = TokenService.CreateAccessToken(
        user.Username, user.Roles, jwtKey, jwtIssuer, jwtAudience,
        TimeSpan.FromMinutes(15));

    var refresh = TokenService.CreateRefreshToken();
    RefreshStore.Save(user.Username, refresh, DateTimeOffset.UtcNow.AddDays(7));

    return Results.Ok(new
    {
        access_token  = access,
        refresh_token = refresh,
        token_type    = "Bearer",
        expires_in    = 900 // 15 мин в секундах
    });
}).AllowAnonymous();

// POST /token/refresh — ротация токенов (refresh token rotation)
app.MapPost("/token/refresh", (RefreshRequest req) =>
{
    var entry = RefreshStore.Get(req.RefreshToken);
    if (entry is null || entry.Expires < DateTimeOffset.UtcNow)
        return Results.Unauthorized();

    // Отзываем старый refresh (rotation — защита от replay-атак)
    RefreshStore.Revoke(req.RefreshToken);

    var user   = UserStore.Users[entry.Username];
    var access = TokenService.CreateAccessToken(
        user.Username, user.Roles, jwtKey, jwtIssuer, jwtAudience,
        TimeSpan.FromMinutes(15));

    var newRefresh = TokenService.CreateRefreshToken();
    RefreshStore.Save(user.Username, newRefresh, DateTimeOffset.UtcNow.AddDays(7));

    return Results.Ok(new
    {
        access_token  = access,
        refresh_token = newRefresh,
        expires_in    = 900
    });
}).AllowAnonymous();

// POST /logout — отзыв токенов
app.MapPost("/logout", (HttpContext ctx, LogoutRequest req) =>
{
    // Отзываем access-токен по jti
    var jti = ctx.User.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
    if (jti is not null)
        RevokedTokens.Revoke(jti);

    // Отзываем refresh-токен (если передан)
    if (req.RefreshToken is not null)
        RefreshStore.Revoke(req.RefreshToken);

    return Results.Ok(new { message = "Токены отозваны" });
}).RequireAuthorization();

// GET /me — кто я?
app.MapGet("/me", (HttpContext ctx) =>
    Results.Ok(new
    {
        sub   = ctx.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value,
        roles = ctx.User.FindAll("role").Select(c => c.Value),
        jti   = ctx.User.FindFirst(JwtRegisteredClaimNames.Jti)?.Value
    })
).RequireAuthorization();

// GET /admin — только для роли admin
app.MapGet("/admin", (HttpContext ctx) =>
    Results.Ok(new
    {
        message = "Добро пожаловать, администратор!",
        sub     = ctx.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
    })
).RequireAuthorization(policy => policy.RequireClaim("role", "admin"));

app.Run();

// ── Типы ────────────────────────────────────────────────────
record TokenRequest(string Username, string Password);
record RefreshRequest(string RefreshToken);
record LogoutRequest(string? RefreshToken);
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

static class RefreshStore
{
    public record Entry(string Username, DateTimeOffset Expires);
    private static readonly Dictionary<string, Entry> _map = [];

    public static void Save(string user, string token, DateTimeOffset exp) =>
        _map[token] = new Entry(user, exp);

    public static Entry? Get(string token) =>
        _map.TryGetValue(token, out var e) ? e : null;

    public static void Revoke(string token) => _map.Remove(token);
}

static class RevokedTokens
{
    private static readonly HashSet<string> _set = [];
    public static bool IsRevoked(string jti) => _set.Contains(jti);
    public static void Revoke(string jti) => _set.Add(jti);
}

static class TokenService
{
    public static string CreateAccessToken(
        string username, string[] roles,
        string key, string issuer, string audience,
        TimeSpan lifetime)
    {
        var jti = Guid.NewGuid().ToString("N");
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, username),
            new(JwtRegisteredClaimNames.Jti, jti),
            new(JwtRegisteredClaimNames.Iat,
                DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
                ClaimValueTypes.Integer64)
        };
        claims.AddRange(roles.Select(r => new Claim("role", r)));

        var secKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var creds  = new SigningCredentials(secKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer:             issuer,
            audience:           audience,
            claims:             claims,
            notBefore:          DateTime.UtcNow,
            expires:            DateTime.UtcNow.Add(lifetime),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public static string CreateRefreshToken()
    {
        var bytes = new byte[64];
        System.Security.Cryptography.RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes);
    }
}
