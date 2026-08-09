// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using Microsoft.Extensions.Logging;
using SimplyDraft.Core.Abstractions.Infrastructure;
using SimplyDraft.Core.Abstractions.UI;
using SimplyDraft.UI.ViewModels;
using SimplyDraft.UI.Views;

namespace SimplyDraft.UI.Factories;

public sealed class SettingsWindowFactory : IWindowFactory<SettingsWindow>
{
    private readonly IAppSettingsProvider _settings;
    private readonly ILogger<SettingsWindowViewModel> _viewModelLogger;

    public SettingsWindowFactory(
        IAppSettingsProvider settings,
        ILogger<SettingsWindowViewModel> viewModelLogger)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _viewModelLogger = viewModelLogger ?? throw new ArgumentNullException(nameof(viewModelLogger));
    }

    public SettingsWindow Create()
        => new(new SettingsWindowViewModel(_settings, _viewModelLogger));
}