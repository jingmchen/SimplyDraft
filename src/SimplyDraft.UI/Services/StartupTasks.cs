// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using Microsoft.Extensions.Logging;
using SimplyDraft.Core.Abstractions.Infrastructure;
using SimplyDraft.Core.Abstractions.UI;

namespace SimplyDraft.UI.Services;

public sealed class StartupTasks : IStartupTasks
{
    private readonly ILogger<StartupTasks> _logger;
    private readonly IAppSettingsProvider _settings;
    private readonly ILibraryPaths _libraryPaths;
    private readonly ILibrary _library;

    public StartupTasks(ILogger<StartupTasks> logger, IAppSettingsProvider settings, ILibraryPaths libraryPaths, ILibrary library)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _libraryPaths = libraryPaths ?? throw new ArgumentNullException(nameof(libraryPaths));
        _library = library ?? throw new ArgumentNullException(nameof(library));
    }
    
    public void Run()
    {
        if (_libraryPaths.ToSeed)
        {
            try
            {
                int added = _library.SeedMissingTemplates();
                _logger.LogInformation("New library — added {Count} bundled example template(s).", added);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Example seeding skipped");
            }
        }

        try
        {
            _library.PurgeTrash(_settings.Current.LibrarySection.TrashPurgeDays);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Trash purge skipped");
        }
    }
}