namespace ASP_pystoy.Models;

public class FeedbackFormModel
{
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? Category { get; set; }
    public string? Message { get; set; }
    public int Priority { get; set; } = 1;
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public string? RequestId { get; set; }
}

public class RequestLogEntry
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];
    public string Method { get; set; } = "";
    public string Path { get; set; } = "";
    public int StatusCode { get; set; }
    public double ElapsedMilliseconds { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string ClientIp { get; set; } = "";
    public string? Details { get; set; }
}

public class HostLifetimeEvent
{
    public string EventName { get; set; } = "";
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string Description { get; set; } = "";
}
