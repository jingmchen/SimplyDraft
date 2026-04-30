using Microsoft.Extensions.Logging;

namespace SimplyDraft.App.Services.FileExplorer;

public sealed class FileWatcherService : IDisposable
{
    private readonly ILogger<FileWatcherService> _logger;
    private readonly TimeSpan _debounce;
    private FileSystemWatcher? _watcher;
    private Timer? _debounceTimer;
    private FileSystemEventArgs? _pendingEvent;
    private bool _disposed;
    private readonly object _lock = new();

    // Raised once per debounce window
    public event EventHandler<FileSystemEventArgs>? Changed;

    public FileWatcherService(ILogger<FileWatcherService> logger, TimeSpan? debounce = null)
    {
        _logger = logger;
        _debounce = debounce ?? TimeSpan.FromMilliseconds(400);
    }

    // ─── PUBLIC API ────────────────────────────
    // Starts watching
    public void Start(string rootPath)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        Stop(); // Stop previously active watcher

        if (!Directory.Exists(rootPath)) return;

        var watcher = new FileSystemWatcher(rootPath)
        {
            NotifyFilter =
                NotifyFilters.FileName |
                NotifyFilters.DirectoryName |
                NotifyFilters.LastWrite |
                NotifyFilters.Size |
                NotifyFilters.Attributes,
            IncludeSubdirectories = true,
            EnableRaisingEvents = true,
            InternalBufferSize = 65_536
        };

        watcher.Changed += OnRawEvent;
        watcher.Created += OnRawEvent;
        watcher.Deleted += OnRawEvent;
        watcher.Renamed += OnRawEvent;
        watcher.Error += OnError;

        lock (_lock) {_watcher = watcher;}
    }

    public void Stop()
    {
        lock (_lock)
        {
            _debounceTimer?.Dispose();
            _debounceTimer = null;
            _pendingEvent = null;

            if (_watcher is not null)
            {
                _watcher.EnableRaisingEvents = false;
                _watcher.Changed -= OnRawEvent;
                _watcher.Created -= OnRawEvent;
                _watcher.Deleted -= OnRawEvent;
                _watcher.Renamed -= OnRawEvent;
                _watcher.Error -= OnError;
                _watcher.Dispose();
                _watcher = null;
            }
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Stop();
    }

    // ─── PRIVATE METHODS ───────────────────────
    private void OnRawEvent(object sender, FileSystemEventArgs e)
    {
        lock (_lock)
        {
            _pendingEvent = e;

            // Reset debounce window each time a new event arrives
            _debounceTimer?.Dispose();
            _debounceTimer = new Timer(FireDebounced, null, _debounce, Timeout.InfiniteTimeSpan);
        }
    }

    private void FireDebounced(object? _)
    {
        FileSystemEventArgs? evt;
        lock (_lock)
        {
            evt = _pendingEvent;
            _pendingEvent = null;
        }

        if (evt is not null)
            Changed?.Invoke(this, evt);
    }

    private void OnError(object sender, ErrorEventArgs e)
    {
        var ex = e.GetException();
        _logger.LogError(
            "[FileWatcher] Error: {ExceptionType}: {ExceptionMessage}",
            ex.GetType().Name, ex.Message
        );
    }
}