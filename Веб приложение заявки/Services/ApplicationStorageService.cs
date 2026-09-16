using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using ApplicationPortal.Models;

namespace ApplicationPortal.Services;

public class ApplicationStorageService
{
    private readonly string _storagePath;
    private readonly object _lock = new();

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
        PropertyNameCaseInsensitive = true
    };

    public ApplicationStorageService(IWebHostEnvironment env)
    {
        var dataDir = Path.Combine(env.ContentRootPath, "App_Data");
        if (!Directory.Exists(dataDir))
        {
            Directory.CreateDirectory(dataDir);
        }
        _storagePath = Path.Combine(dataDir, "applications.json");
    }

    public List<ApplicationSubmission> GetAll()
    {
        lock (_lock)
        {
            if (!File.Exists(_storagePath))
            {
                return new List<ApplicationSubmission>();
            }

            try
            {
                var json = File.ReadAllText(_storagePath);
                var items = JsonSerializer.Deserialize<List<ApplicationSubmission>>(json, _jsonOptions);
                return items ?? new List<ApplicationSubmission>();
            }
            catch
            {
                return new List<ApplicationSubmission>();
            }
        }
    }

    public ApplicationSubmission? GetById(string id)
    {
        return GetAll().FirstOrDefault(x => string.Equals(x.Id, id, StringComparison.OrdinalIgnoreCase));
    }

    public ApplicationSubmission Save(ApplicationSubmission app)
    {
        lock (_lock)
        {
            var items = GetAll();

            if (string.IsNullOrWhiteSpace(app.Id))
            {
                var random = new Random();
                var num = random.Next(1000, 9999);
                app.Id = $"APP-{DateTime.UtcNow.Year}-{num}";
                app.CreatedAt = DateTime.UtcNow;
                items.Insert(0, app);
            }
            else
            {
                var existingIndex = items.FindIndex(x => string.Equals(x.Id, app.Id, StringComparison.OrdinalIgnoreCase));
                if (existingIndex >= 0)
                {
                    app.CreatedAt = items[existingIndex].CreatedAt;
                    items[existingIndex] = app;
                }
                else
                {
                    items.Insert(0, app);
                }
            }

            var json = JsonSerializer.Serialize(items, _jsonOptions);
            File.WriteAllText(_storagePath, json);

            return app;
        }
    }

    public bool Delete(string id)
    {
        lock (_lock)
        {
            var items = GetAll();
            var countBefore = items.Count;
            items.RemoveAll(x => string.Equals(x.Id, id, StringComparison.OrdinalIgnoreCase));

            if (items.Count != countBefore)
            {
                var json = JsonSerializer.Serialize(items, _jsonOptions);
                File.WriteAllText(_storagePath, json);
                return true;
            }

            return false;
        }
    }

    public void ClearAll()
    {
        lock (_lock)
        {
            if (File.Exists(_storagePath))
            {
                File.WriteAllText(_storagePath, "[]");
            }
        }
    }
}
