// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using Microsoft.Extensions.Logging;
using SimplyDraft.Core.Abstractions.Engine;
using SimplyDraft.Core.Abstractions.Infrastructure;
using SimplyDraft.Core.Abstractions.UI;
using SimplyDraft.UI.ViewModels;
using SimplyDraft.UI.Views;

namespace SimplyDraft.UI.Factories;

public sealed class EditorWindowFactory : IWindowFactory<EditorWindow>
{
    private readonly IScriptingEngine _scripting;
    private readonly IMarkupEngine _markup;
    private readonly IRenderEngine _renderer;
    private readonly IExportService _exportService;
    private readonly ILibrary _library;
    private readonly IDialogService _dialog;
    private readonly IAppSettingsProvider _settings;
    private readonly ILogger<EditorWindow> _viewLogger;
    private readonly ILogger<EditorWindowViewModel> _viewModelLogger;

    public EditorWindowFactory(
        IScriptingEngine scripting,
        IMarkupEngine markup,
        IRenderEngine renderer,
        IExportService exportService,
        ILibrary library,
        IDialogService dialog,
        IAppSettingsProvider settings,
        ILogger<EditorWindow> viewLogger,
        ILogger<EditorWindowViewModel> viewModelLogger)
    {
        _scripting = scripting ?? throw new ArgumentNullException(nameof(scripting));
        _markup = markup ?? throw new ArgumentNullException(nameof(markup));
        _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
        _exportService = exportService ?? throw new ArgumentNullException(nameof(exportService));
        _library = library ?? throw new ArgumentNullException(nameof(library));
        _dialog = dialog ?? throw new ArgumentNullException(nameof(dialog));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _viewLogger = viewLogger ?? throw new ArgumentNullException(nameof(viewLogger));
        _viewModelLogger = viewModelLogger ?? throw new ArgumentNullException(nameof(viewModelLogger));
    }

    public EditorWindow Create()
        => new(
            new EditorWindowViewModel(_scripting, _markup, _exportService, _library, _dialog, _settings, _viewModelLogger),
            _renderer,
            _settings,
            _viewLogger);
}