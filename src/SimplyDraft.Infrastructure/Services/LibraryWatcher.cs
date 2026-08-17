// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using Microsoft.Extensions.Logging;
using SimplyDraft.Core.Abstractions.Infrastructure;
using SimplyDraft.Core.Logging;
using SimplyDraft.Infrastructure.Constants;

namespace SimplyDraft.Infrastructure.Services;

public sealed partial class LibraryWatcher : ILibraryWatcher
{
    private const int DebounceMilliseconds = 500;
    private readonly ILibraryPaths _libraryPaths;
    private readonly ILogger<LibraryWatcher> _logger;
    private FileSystemWatcher? _templateFolderWatcher;
    private FileSystemWatcher? _childrenFolderWatcher;
    private int _pending;
    private volatile bool _disposed;
    public event Action? Changed;

    public LibraryWatcher(ILibraryPaths libraryPaths, ILogger<LibraryWatcher> logger)
    {
        _libraryPaths = libraryPaths ?? throw new ArgumentNullException(nameof(libraryPaths));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
            _libraryPaths.TemplatesFolder, InfrastructureConstants.FileExtension.Template);
        _childrenFolderWatcher = MakeWatcher(
            _libraryPaths.ChildrenFolder, InfrastructureConstants.FileExtension.Children);

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
        EventId = LogEventIDs.Infrastructure.LibraryWatcher.FailedToCreateWatcher,
        Level = LogLevel.Warning,
        Message = "Unable to create LibraryWatcher for {Path}")]
    private partial void LogFailedToCreateWatcher(Exception ex, string path);
}