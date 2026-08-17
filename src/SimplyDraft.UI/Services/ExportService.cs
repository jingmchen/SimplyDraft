// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using Avalonia.Platform.Storage;
using Microsoft.Extensions.Logging;
using SimplyDraft.Core.Abstractions.Engine;
using SimplyDraft.Core.Abstractions.Infrastructure;
using SimplyDraft.Core.Abstractions.UI;
using SimplyDraft.Core.Common;
using SimplyDraft.Core.Configuration.AppSettings;
using SimplyDraft.Core.Domains.Documents;
using SimplyDraft.Core.Domains.Export;
using SimplyDraft.Core.Domains.Generation;
using SimplyDraft.Core.Domains.Library;
using SimplyDraft.Core.Enums;
using SimplyDraft.Core.Export;
using SimplyDraft.UI.Common;

namespace SimplyDraft.UI.Services;

public sealed class ExportService : IExportService
{
    private readonly IScriptingEngine _scriptingEngine;
    private readonly ExporterCatalog _exporterCatalog;
    private readonly ExportOptions _exportOptions;
    private readonly ILibrary _library;
    private readonly IDialogService _dialog;
    private readonly ISettingsProvider<AppSettings> _settings;
    private readonly ILogger<ExportService> _logger;

    public ExportService(
        IScriptingEngine scriptingEngine,
        ExporterCatalog exporterCatalog,
        ILibrary library,
        IDialogService dialog,
        ISettingsProvider<AppSettings> settings,
        ILogger<ExportService> logger)
    {
        _scriptingEngine = scriptingEngine ?? throw new ArgumentNullException(nameof(scriptingEngine));
        _exporterCatalog = exporterCatalog ?? throw new ArgumentNullException(nameof(exporterCatalog));
        _library = library ?? throw new ArgumentNullException(nameof(library));
        _dialog = dialog ?? throw new ArgumentNullException(nameof(dialog));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _exportOptions = new ExportOptions
        {
            NewLine = _settings.Current.Export.TxtNewLine,
            WriteBom = _settings.Current.Export.TxtBom
        };
    }

    public async Task<string?> ExportAsync(
        string suggestedBaseName,
        DocumentKind formatKind,
        GenerationResult result,
        FrontMatter templateFm,
        string? baseDirectory = null)
    {
        if (!result.Success)
        {
            await _dialog.ChooseAsync(
                "Export failed",
                "The document could not be generated:\n\n" + string.Join("\n", result.Diagnostics),
                "OK");
            return null;
        }

        if (UIWindows.Active is not { } owner)
            return null;
        
        try
        {
            var exporter = _exporterCatalog.Resolve(formatKind);
            var file = await owner.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Export — " + exporter.DisplayName,
                SuggestedFileName = FileNameSanitizer.Sanitize(suggestedBaseName) + exporter.FileExtension,
                DefaultExtension = exporter.FileExtension,
                FileTypeChoices = [
                    new FilePickerFileType(exporter.DisplayName)
                    {
                        Patterns = ["*." + exporter.FileExtension]
                    }
                ]
            });

            var path = file?.TryGetLocalPath();

            if (path is null)
                return null;

            await exporter.ExportAsync(
                GeneratedDocument.From(result.Text, templateFm, baseDirectory),
                path,
                _exportOptions,
                CancellationToken.None);

            _logger.LogInformation("Exported \"{Name}\" -> {Path}", suggestedBaseName, path);
            return path;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Export failed for \"{Name}\"", suggestedBaseName);
            await _dialog.ChooseAsync("Export failed", ex.Message, "OK");
            return null;
        }
    }
    
    public (GenerationResult Result, FrontMatter TemplateFm, string Name) GenerateItem(LibraryItem item, GenerationMode mode)
        => _scriptingEngine.GenerateItem(
            _library,
            item,
            mode,
            _settings.Current.Generation.Policy,
            _settings.Current.Generation.Culture);
}