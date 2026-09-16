using System.Collections.Concurrent;
using ASP_pystoy.Models;

namespace ASP_pystoy.Services;

public interface IAppLifetimeMonitor
{
    DateTime HostStartTime { get; }
    TimeSpan Uptime => DateTime.UtcNow - HostStartTime;
    string HostStatus { get; }
    IReadOnlyList<HostLifetimeEvent> LifecycleEvents { get; }
    IReadOnlyList<RequestLogEntry> RecentRequests { get; }
    IReadOnlyList<FeedbackFormModel> SubmittedForms { get; }

    void RecordEvent(string eventName, string description);
    void RecordRequest(RequestLogEntry logEntry);
    void RecordFormSubmission(FeedbackFormModel form);
    void SetHostStatus(string status);
}

public class AppLifetimeMonitor : IAppLifetimeMonitor
{
    public DateTime HostStartTime { get; } = DateTime.UtcNow;
    public string HostStatus { get; private set; } = "Starting";

    private readonly List<HostLifetimeEvent> _events = new();
    private readonly ConcurrentQueue<RequestLogEntry> _recentRequests = new();
    private readonly ConcurrentQueue<FeedbackFormModel> _forms = new();
    private readonly object _lock = new();

    public IReadOnlyList<HostLifetimeEvent> LifecycleEvents
    {
        get { lock (_lock) { return _events.ToList(); } }
    }

    public IReadOnlyList<RequestLogEntry> RecentRequests => _recentRequests.ToArray().Reverse().Take(20).ToList();
    public IReadOnlyList<FeedbackFormModel> SubmittedForms => _forms.ToArray().Reverse().Take(10).ToList();

    public void RecordEvent(string eventName, string description)
    {
        lock (_lock)
        {
            _events.Add(new HostLifetimeEvent
            {
                EventName = eventName,
                Timestamp = DateTime.UtcNow,
                Description = description
            });
        }
    }

    public void RecordRequest(RequestLogEntry logEntry)
    {
        _recentRequests.Enqueue(logEntry);
        while (_recentRequests.Count > 50)
        {
            _recentRequests.TryDequeue(out _);
        }
    }

    public void RecordFormSubmission(FeedbackFormModel form)
    {
        _forms.Enqueue(form);
        while (_forms.Count > 50)
        {
            _forms.TryDequeue(out _);
        }
    }

    public void SetHostStatus(string status)
    {
        HostStatus = status;
    }
}
