// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using Microsoft.Extensions.Logging;
using SimplyDraft.Core.Abstractions.Engine;
using SimplyDraft.Core.Abstractions.Infrastructure;
using SimplyDraft.Core.Abstractions.UI;
using SimplyDraft.Core.Configuration.AppSettings;
using SimplyDraft.UI.ViewModels;
using SimplyDraft.UI.Views;

namespace SimplyDraft.UI.Factories;

public sealed class GenerateChildWindowFactory : IWindowFactory<GenerateChildWindow>
{
    private readonly IScriptingEngine _scripting;
    private readonly IMarkupEngine _markup;
    private readonly IRenderEngine _renderer;
    private readonly ILibrary _library;
    private readonly IDialogService _dialog;
    private readonly ISettingsProvider<AppSettings> _settings;
    private readonly ILogger<GenerateChildWindowViewModel> _viewModelLogger;

    public GenerateChildWindowFactory(
        IScriptingEngine scripting,
        IMarkupEngine markup,
        IRenderEngine renderer,
        ILibrary library,
        IDialogService dialog,
        ISettingsProvider<AppSettings> settings,
        ILogger<GenerateChildWindowViewModel> viewModelLogger)
    {
        _scripting = scripting ?? throw new ArgumentNullException(nameof(scripting));
        _markup = markup ?? throw new ArgumentNullException(nameof(markup));
        _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
        _library = library ?? throw new ArgumentNullException(nameof(library));
        _dialog = dialog ?? throw new ArgumentNullException(nameof(dialog));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _viewModelLogger = viewModelLogger ?? throw new ArgumentNullException(nameof(viewModelLogger));
    }

    public GenerateChildWindow Create()
        => new(
            new GenerateChildWindowViewModel(_scripting, _markup, _library, _dialog, _settings, _viewModelLogger),
            _renderer);
}