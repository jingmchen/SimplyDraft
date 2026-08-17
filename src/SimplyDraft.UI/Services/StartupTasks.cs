// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using Microsoft.Extensions.Logging;
using SimplyDraft.Core.Abstractions.Infrastructure;
using SimplyDraft.Core.Abstractions.UI;
using SimplyDraft.Core.Configuration.AppSettings;

namespace SimplyDraft.UI.Services;

public sealed class StartupTasks : IStartupTasks
{
    private readonly ILibrary _library;
    private readonly ILibraryPaths _libraryPaths;
    private readonly ISettingsProvider<AppSettings> _settings;
    private readonly ILogger<StartupTasks> _logger;

    public StartupTasks(
        ILibrary library,
        ILibraryPaths libraryPaths,
        ISettingsProvider<AppSettings> settings,
        ILogger<StartupTasks> logger)
    {
        _library = library ?? throw new ArgumentNullException(nameof(library));
        _libraryPaths = libraryPaths ?? throw new ArgumentNullException(nameof(libraryPaths));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    public async Task Run()
    {
        if (_libraryPaths.ToSeed)
        {
            try
            {
                int added = await _library.SeedMissingTemplatesAsync();
                _logger.LogInformation("New library — added {Count} bundled example template(s).", added);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Example seeding skipped");
            }
        }

        try
        {
            _library.PurgeTrash(_settings.Current.Library.TrashPurgeDays);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Trash purge skipped");
        }
    }
}