// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using Microsoft.Extensions.Logging;
using SimplyDraft.Core.Abstractions.Engine;
using SimplyDraft.Core.Abstractions.Infrastructure;
using SimplyDraft.Core.Abstractions.UI;
using SimplyDraft.Core.Export;
using SimplyDraft.UI.ViewModels;
using SimplyDraft.UI.Views;

namespace SimplyDraft.UI.Factories;

public sealed class BatchCreateWindowFactory : IWindowFactory<BatchCreateWindow>
{
    private readonly IBatchGenerator _batch;
    private readonly ExporterCatalog _exporterCatalog;
    private readonly ILibrary _library;
    private readonly IFilePickerService _filePicker;
    private readonly ILibraryPaths _libraryPaths;
    private readonly IAppSettingsProvider _settings;
    private readonly ILogger<BatchCreateWindowViewModel> _viewModelLogger;

    public BatchCreateWindowFactory(
        IBatchGenerator batch,
        ExporterCatalog exporterCatalog,
        ILibrary library,
        IFilePickerService filePicker,
        ILibraryPaths libraryPaths,
        IAppSettingsProvider settings,
        ILogger<BatchCreateWindowViewModel> viewModelLogger)
    {
        _batch = batch ?? throw new ArgumentNullException(nameof(batch));
        _exporterCatalog = exporterCatalog ?? throw new ArgumentNullException(nameof(exporterCatalog));
        _library = library ?? throw new ArgumentNullException(nameof(library));
        _filePicker = filePicker ?? throw new ArgumentNullException(nameof(filePicker));
        _libraryPaths = libraryPaths ?? throw new ArgumentNullException(nameof(libraryPaths));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _viewModelLogger = viewModelLogger ?? throw new ArgumentNullException(nameof(viewModelLogger));
    }

    public BatchCreateWindow Create()
        => new(new BatchCreateWindowViewModel(
            _batch, _exporterCatalog, _library, _filePicker, _libraryPaths, _settings, _viewModelLogger));
}