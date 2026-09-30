# Как Работать с бд связью и сортировкой
```c#
// Controllers/UsersController.cs  — АНТИПРИМЕР
        public IActionResult Index(string sort, string q, int page = 1)
        {
            var query = _db.Users.AsQueryable();

            // поиск
            if (!string.IsNullOrEmpty(q))
                query = query.Where(u => u.Name.Contains(q));

            // сортировка — классическая ошибка
            if (sort == "name")
                query = query.OrderBy(u => u.Name);
            else if (sort == "name_desc")
                query = query.OrderByDescending(u => u.Name);
            else if (sort == "email")
                query = query.OrderBy(u => u.Email);
            // ещё 20 полей → ещё 40 веток

            int pageSize = 10;
            var users = query.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            return View(users);
        }

        // Dtos/QueryDto.cs
using System.ComponentModel.DataAnnotations;

public class QueryDto
    {
        [Range(1, int.MaxValue, ErrorMessage = "Page must be >= 1")]
        public int Page { get; set; } = 1;

        [Range(1, 100, ErrorMessage = "Limit must be 1..100")]
        public int Limit { get; set; } = 10;

        // "name:asc,createdAt:desc"
        public string? Sort { get; set; }

        [StringLength(200)]
        public string? Q { get; set; }
    }

    // Querying/QueryObject.cs
    public enum SortDirection { Asc, Desc }
    public record SortRule(string Field, SortDirection Direction);

    public class QueryObject
    {
        public int Skip { get; }
        public int Take { get; }
        public int Page { get; }
        public int Limit { get; }
        public string? Search { get; }
        public IReadOnlyList<SortRule> Sorts { get; }

        public QueryObject(QueryDto dto,
                           IEnumerable<string> allowedSortFields)
        {
            Page = dto.Page < 1 ? 1 : dto.Page;
            Limit = Math.Clamp(dto.Limit, 1, 100);
            Skip = (Page - 1) * Limit;
            Take = Limit;
            Search = string.IsNullOrWhiteSpace(dto.Q) ? null : dto.Q.Trim();
            Sorts = ParseSort(dto.Sort, allowedSortFields);
        }

        private static IReadOnlyList<SortRule> ParseSort(
            string? sort, IEnumerable<string> allowed)
        {
            if (string.IsNullOrWhiteSpace(sort))
                return new[] { new SortRule("CreatedAt", SortDirection.Desc) };

            var allowedSet = allowed.ToHashSet(StringComparer.OrdinalIgnoreCase);
            var result = new List<SortRule>();

            foreach (var part in sort.Split(',', StringSplitOptions.RemoveEmptyEntries))
            {
                var bits = part.Split(':', 2);
                var field = bits[0].Trim();
                var dir = bits.Length > 1 && bits[1].Equals("asc", StringComparison.OrdinalIgnoreCase)
                            ? SortDirection.Asc
                            : SortDirection.Desc;

                if (!allowedSet.Contains(field))
                    throw new ArgumentException($"Sort field '{field}' not allowed.");

                result.Add(new SortRule(field, dir));
            }
            return result;
        }

        if (!allowedSet.Contains(field)) throw new ArgumentException(...);
    }


    // Querying/QueryableExtensions.cs
using System.Linq.Expressions;

public static class QueryableExtensions
    {
        public static IQueryable<T> ApplySort<T>(
            this IQueryable<T> source, IEnumerable<SortRule> sorts)
        {
            IOrderedQueryable<T>? ordered = null;

            foreach (var rule in sorts)
            {
                var param = Expression.Parameter(typeof(T), "x");
                var prop = Expression.PropertyOrField(param, rule.Field);
                var lambda = Expression.Lambda(prop, param);

                string method =
                    ordered is null
                        ? (rule.Direction == SortDirection.Asc ? "OrderBy" : "OrderByDescending")
                        : (rule.Direction == SortDirection.Asc ? "ThenBy" : "ThenByDescending");

                var call = Expression.Call(
                    typeof(Queryable), method,
                    new[] { typeof(T), prop.Type },
                    ordered?.Expression ?? source.Expression,
                    Expression.Quote(lambda));

                ordered = (IOrderedQueryable<T>)source.Provider.CreateQuery<T>(call);
            }

            return ordered ?? source;
        }
    }

    // Repositories/UserRepository.cs
    public class UserRepository
    {
        private static readonly string[] AllowedSort =
            { "Id", "Name", "Email", "CreatedAt" };

        private readonly AppDbContext _db;
        public UserRepository(AppDbContext db) => _db = db;

        public async Task<PagedResult<User>> FindManyAsync(
            QueryDto dto, CancellationToken ct = default)
        {
            var qo = new QueryObject(dto, AllowedSort);

            IQueryable<User> query = _db.Users.AsNoTracking();

            if (qo.Search is { } s)
                query = query.Where(u => u.Name.Contains(s) || u.Email.Contains(s));

            query = query.ApplySort(qo.Sorts);

            var total = await query.CountAsync(ct);

            var items = await query
                .Skip(qo.Skip)
                .Take(qo.Take)
                .ToListAsync(ct);

            return new PagedResult<User>(items, qo.Page, qo.Limit, total);
        }
    }

    // Querying/PagedResult.cs
    public record PagedResult<T>(
        IReadOnlyList<T> Data,
        Meta Meta);

    public record Meta(int Page, int Limit, int Total, int TotalPages, bool HasNext, bool HasPrev)
    {
        public static Meta Create(int page, int limit, int total) => new(
            page,
            limit,
            total,
            (int)Math.Ceiling(total / (double)limit),
            page * limit < total,
            page > 1);
    }

    // В конструкторе PagedResult<T>:
    public PagedResult(IReadOnlyList<T> data, int page, int limit, int total)
         :this(data, Meta.Create(page, limit, total)) { }
    }

    {
    "data": [ /* ... */ ],
    "meta": {
        "page": 2, "limit": 10, "total": 143,
            "totalPages": 15, "hasNext": true, "hasPrev": true }


[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly UserRepository _repo;
    public UsersController(UserRepository repo) => _repo = repo;

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] QueryDto dto, CancellationToken ct)
    {
        var result = await _repo.FindManyAsync(dto, ct);
        return Ok(result);
    }
}

public async Task<CursorPage<User>> FindWithCursorAsync(
    int? cursorId, int limit = 20, CancellationToken ct = default)
{
    var query = _db.Users
        .AsNoTracking()
        .OrderByDescending(u => u.Id);

    if (cursorId is { } id)
        query = (IOrderedQueryable<User>)query.Where(u => u.Id < id);

    var items = await query.Take(limit + 1).ToListAsync(ct);

    var hasNext = items.Count > limit;
    var data = hasNext ? items.Take(limit).ToList() : items;
    var next = hasNext ? data[^1].Id.ToString() : null;

    return new CursorPage<User>(data, next);
}
```

# Etag

/////////////////////////////////////////
```c#
ETag: "33a64df551425fcc55e4d42a148795d9f25f89d4" // strong etag
ETag: W / "0815" // weak etag

// etag хеш содержимого
// etag бд версий

// меняется при каждом изменении
// для разных представлений разный etag
// генерируется сервером


If - None - Match-- > GET, HEAD // выполняется если ресурс не существует

If-Match --> PUT, PATCH, DELETE // выполняется если ресурс существует

If-Modified-Since --> GET

If-Unmodified-Since --> PUT, PATCH


1. GET /resource/1
   ← 200 OK
     ETag: "v5"

2.Клиент модифицирует данные

3. PUT /resource/1
   If-Match: "v5"
   { ... }

   ├─ если сервер видит, что текущий ETag = "v5" → 200 OK, новый ETag: "v6"
   └─ если текущий ETag ≠ "v5" (кто-то изменил) → 412 Precondition Failed



// Без etag

    // NaiveServer — ПЛОХОЙ пример, показываем lost update
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// "Хранилище" в памяти
var document = new Document { Text = "Исходный текст", Version = 1 };

// Просто отдаём документ
app.MapGet("/doc", () => Results.Ok(document));

// Просто перезаписываем — БЕЗ проверки версии
app.MapPut("/doc", (Document incoming) =>
{
document.Text = incoming.Text;
document.Version++;
return Results.Ok(document);
});

app.Run();

record Document
{
    public string Text { get; set; } = "";
    public int Version { get; set; }
}


// пример через консоль

#A и B читают документ — оба видят version=1
curl http://localhost:5000/doc        # {"text":"Исходный текст","version":1}
curl http://localhost:5000/doc        # {"text":"Исходный текст","version":1}

#A сохраняет
curl - X PUT http://localhost:5000/doc \
  -H 'Content-Type: application/json' \
  -d '{"text":"Правка от A"}'

#B сохраняет поверх — ничего не подозревая
curl - X PUT http://localhost:5000/doc \
  -H 'Content-Type: application/json' \
  -d '{"text":"Правка от B"}'

#Итог: правка A потеряна
curl http://localhost:5000/doc        # {"text":"Правка от B","version":3}



// с etag

// Step1 — сервер учится отдавать ETag
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var document = new Document { Text = "Исходный текст", Version = 1 };

// Формируем ETag из номера версии
static string GetETag(Document doc) => $"\"v{doc.Version}\"";   // кавычки обязательны

app.MapGet("/doc", (HttpContext ctx) =>
{
    var etag = GetETag(document);

    // 1. Отдаём ETag в заголовке ответа
    ctx.Response.Headers.ETag = etag;

    // 2. Если клиент прислал If-None-Match с тем же ETag — отдаём 304
    var ifNoneMatch = ctx.Request.Headers.IfNoneMatch.ToString();
    if (ifNoneMatch == etag)
        return Results.StatusCode(304);       // тело не отправляем

    return Results.Ok(document);
});

app.MapPut("/doc", (Document incoming) =>
{
    // пока без проверки — добавим на следующем шаге
    document.Text = incoming.Text;
    document.Version++;
    return Results.Ok(document);
});

app.Run();

// работа через консоль

curl - i http://localhost:5000/doc
#HTTP/1.1 200 OK
#ETag: "v1"
#{"text":"Исходный текст","version":1}

curl - i http://localhost:5000/doc -H 'If-None-Match: "v1"'
#HTTP/1.1 304 Not Modified      ← тело не передаётся, экономим трафик


// проверка if-match

// Step2 — добавляем оптимистичную блокировку
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var document = new Document { Text = "Исходный текст", Version = 1 };

static string GetETag(Document doc) => $"\"v{doc.Version}\"";

app.MapGet("/doc", (HttpContext ctx) =>
{
    var etag = GetETag(document);
    ctx.Response.Headers.ETag = etag;

    if (ctx.Request.Headers.IfNoneMatch.ToString() == etag)
        return Results.StatusCode(304);

    return Results.Ok(document);
});

app.MapPut("/doc", (HttpContext ctx, Document incoming) =>
{
    var currentETag = GetETag(document);
    var clientETag = ctx.Request.Headers.IfMatch.ToString();

    // --- ШАГ 1: клиент обязан сообщить, с какой версией он работал ---
    if (string.IsNullOrEmpty(clientETag))
    {
        return Results.Json(new
        {
            error = "Требуется заголовок If-Match",
            currentETag
        }, statusCode: 428);
    }

    // --- ШАГ 2: сравниваем версию клиента с текущей ---
    if (clientETag != currentETag)
    {
        // Кто-то успел изменить документ после того, как клиент его прочитал
        return Results.Json(new
        {
            error = "Документ был изменён другим клиентом",
            currentETag,
            yourETag = clientETag
        }, statusCode: 412);
    }

    // --- ШАГ 3: версии совпали — можно безопасно применить изменение ---
    document.Text = incoming.Text;
    document.Version++;

    ctx.Response.Headers.ETag = GetETag(document);   // сообщаем новый ETag
    return Results.Ok(document);
});

app.Run();

// работа через консоль

#A и B читают документ, оба получают ETag: "v1"
curl - i http://localhost:5000/doc        # ETag: "v1"

#A успешно сохраняет
curl -X PUT http://localhost:5205/doc \
  -H 'Content-Type: application/json'\
  - H 'If-Match: "v1"' \
  -d '{"text":"Правка от A"}'
#→ 200 OK, ETag: "v2"

#B пытается сохранить со СТАРЫМ ETag "v1"
curl -i -X PUT http://localhost:5000/doc \
  -H 'Content-Type: application/json' \
  -H 'If-Match: "v1"' \
  -d '{"text":"Правка от B"}'
#→ HTTP/1.1 412 Precondition Failed
#{"error":"Документ был изменён другим клиентом","currentETag":"\"v2\"", ...}


// обработка ошибки 412 у клиента


    // Client.cs — правильное поведение клиента
using System.Net;
using System.Net.Http.Json;

async Task\<Document\> UpdateDocumentAsync(HttpClient http, string newText)
{
    const int maxRetries = 3;

    for (int attempt = 1; attempt <= maxRetries; attempt++)
    {
        // 1. Читаем актуальную версию + её ETag
        var getRes = await http.GetAsync("/doc");
        var doc = await getRes.Content.ReadFromJsonAsync<Document>();
        var etag = getRes.Headers.ETag!.ToString();   // например: "v1" (с кавычками)

        Console.WriteLine($"Попытка {attempt}: работаем с версией {etag}");

        // 2. Отправляем изменение, привязав его к прочитанной версии
        var request = new HttpRequestMessage(HttpMethod.Put, "/doc")
        {
            Content = JsonContent.Create(new { text = newText })
        };
        request.Headers.TryAddWithoutValidation("If-Match", etag);   // ← ключевой заголовок

        var putRes = await http.SendAsync(request);

        // 3. Если кто-то опередил — повторяем весь цикл
        if (putRes.StatusCode == HttpStatusCode.PreconditionFailed)
        {
            Console.WriteLine("Конфликт версий, повторяем...");
            continue;
        }

        if (!putRes.IsSuccessStatusCode)
            throw new Exception($"HTTP {(int)putRes.StatusCode}");

        return (await putRes.Content.ReadFromJsonAsync<Document>())!;
    }

    throw new Exception("Не удалось сохранить: слишком много конфликтов");
}


// через контроллеры

\[ApiController\]
\[Route("api/doc")\]
public class DocController : ControllerBase
{
    private static Document _doc = new() { Text = "Исходный текст", Version = 1 };

    private static string GetETag(Document d) => $"\"v{d.Version}\"";

    [HttpGet]
    public IActionResult Get()
    {
        var etag = GetETag(_doc);
        Response.Headers.ETag = etag;

        if (Request.Headers.IfNoneMatch.ToString() == etag)
            return StatusCode(304);

        return Ok(_doc);
    }

    [HttpPut]
    public IActionResult Put([FromBody] Document incoming)
    {
        var currentETag = GetETag(_doc);
        var clientETag = Request.Headers.IfMatch.ToString();

        if (string.IsNullOrEmpty(clientETag))
            return StatusCode(428, new { error = "Требуется If-Match", currentETag });

        if (clientETag != currentETag)
            return StatusCode(412, new
            {
                error = "Документ был изменён другим клиентом",
                currentETag,
                yourETag = clientETag
            });

        _doc.Text = incoming.Text;
        _doc.Version++;

        Response.Headers.ETag = GetETag(_doc);
        return Ok(_doc);
    }
}

```