var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// ──────────────────────────────────────────────────
//  «Хранилище» в памяти
// ──────────────────────────────────────────────────
var document = new Document { Text = "Исходный текст", Version = 1 };

// Формируем ETag из номера версии (кавычки обязательны по RFC)
static string GetETag(Document doc) => $"\"v{doc.Version}\"";

// ──────────────────────────────────────────────────
//  GET /doc
//  Возвращает документ + ETag; поддерживает If-None-Match → 304
// ──────────────────────────────────────────────────
app.MapGet("/doc", (HttpContext ctx) =>
{
    var etag = GetETag(document);

    // Всегда отправляем ETag в ответе
    ctx.Response.Headers.ETag = etag;

    // Если клиент прислал тот же ETag — ничего не изменилось
    var ifNoneMatch = ctx.Request.Headers.IfNoneMatch.ToString();
    if (ifNoneMatch == etag)
        return Results.StatusCode(304);   // Not Modified, тело не передаём

    return Results.Ok(document);
});

// ──────────────────────────────────────────────────
//  PUT /doc
//  Оптимистичная блокировка через If-Match:
//    428 — заголовок не прислан
//    412 — версия устарела
//    200 — обновлено, новый ETag в ответе
// ──────────────────────────────────────────────────
app.MapPut("/doc", (HttpContext ctx, Document incoming) =>
{
    var currentETag = GetETag(document);
    var clientETag  = ctx.Request.Headers.IfMatch.ToString();

    // Шаг 1: клиент обязан сообщить, с какой версией работал
    if (string.IsNullOrEmpty(clientETag))
    {
        return Results.Json(new
        {
            error      = "Требуется заголовок If-Match",
            currentETag
        }, statusCode: 428);              // Precondition Required
    }

    // Шаг 2: версия клиента должна совпасть с текущей
    if (clientETag != currentETag)
    {
        return Results.Json(new
        {
            error      = "Документ был изменён другим клиентом",
            currentETag,
            yourETag   = clientETag
        }, statusCode: 412);              // Precondition Failed
    }

    // Шаг 3: всё ок — применяем изменение
    document.Text = incoming.Text;
    document.Version++;

    ctx.Response.Headers.ETag = GetETag(document);  // новый ETag
    return Results.Ok(document);
});

app.Run();

// ──────────────────────────────────────────────────
//  Модель
// ──────────────────────────────────────────────────
record Document
{
    public string Text    { get; set; } = "";
    public int    Version { get; set; }
}
