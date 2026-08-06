// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using Microsoft.Extensions.Logging;
using SimplyDraft.Core.Abstractions.Infrastructure;
using SimplyDraft.Infrastructure.Constants;

namespace SimplyDraft.Infrastructure.Services;

public sealed partial class LibraryWatcher : ILibraryWatcher
{
    private const int DebounceMilliseconds = 500;
    private readonly ILogger<LibraryWatcher> _logger;
    private readonly ILibraryPaths _paths;
    private FileSystemWatcher? _templateFolderWatcher;
    private FileSystemWatcher? _childrenFolderWatcher;
    private int _pending;
    private volatile bool _disposed;
    public event Action? Changed;

    public LibraryWatcher(ILogger<LibraryWatcher> logger, ILibraryPaths paths)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _paths = paths ?? throw new ArgumentNullException(nameof(paths));
        Attach();
    }

    public void Rebuild()
        => Attach();

    public void Dispose()
    {
        _disposed = true;
        _templateFolderWatcher?.Dispose();
        _childrenFolderWatcher?.Dispose();
    }

    private void Attach()
    {
        _templateFolderWatcher?.Dispose();
        _childrenFolderWatcher?.Dispose();
        _templateFolderWatcher = MakeWatcher(
            _paths.TemplatesFolder, InfrastructureConstants.FileExtension.Template
        );
        _childrenFolderWatcher = MakeWatcher(
            _paths.ChildrenFolder, InfrastructureConstants.FileExtension.Children
        );

    }

    private FileSystemWatcher? MakeWatcher(string directory, string filter)
    {
        try
        {
            var watcher = new FileSystemWatcher(directory, filter)
            {
                IncludeSubdirectories = true
            };
            watcher.Created += (_, _) => ScheduleChanged();
            watcher.Changed += (_, _) => ScheduleChanged();
            watcher.Deleted += (_, _) => ScheduleChanged();
            watcher.Renamed += (_, _) => ScheduleChanged();
            watcher.Error += (_, _) => ScheduleChanged();
            watcher.EnableRaisingEvents = true;
            return watcher;
        }
        catch (Exception ex)
        {
            LogFailedToCreateWatcher(ex, directory);
            return null;
        }
    }

    private void ScheduleChanged()
    {
        if (_disposed)
            return;
        
        if (Interlocked.Exchange(ref _pending, 1) != 0)
            return;
        
        _ = Task.Run(async () =>
        {
            await Task.Delay(DebounceMilliseconds);
            Interlocked.Exchange(ref _pending, 0);
            if (_disposed) Changed?.Invoke();
        });
    }

    [LoggerMessage(
        EventId = 1001, Level = LogLevel.Warning, Message = "Unable to create LibraryWatcher for {path}"
    )]
    private partial void LogFailedToCreateWatcher(Exception ex, string path);
}