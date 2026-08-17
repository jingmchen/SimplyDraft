// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using SimplyDraft.Core.Abstractions.UI;
using SimplyDraft.Core.Domains.Documents;
using SimplyDraft.Core.Domains.Library;
using SimplyDraft.UI.Common;
using SimplyDraft.UI.Views;

namespace SimplyDraft.UI.Services;

public sealed class WindowService : IWindowService
{
    private readonly IWindowFactory<EditorWindow> _editorWindowFactory;
    private readonly IWindowFactory<GenerateChildWindow> _generateChildWindowFactory;
    private readonly IWindowFactory<SettingsWindow> _settingsWindowFactory;

    public WindowService(
        IWindowFactory<EditorWindow> editorWindowFactory,
        IWindowFactory<GenerateChildWindow> generateChildWindowFactory,
        IWindowFactory<SettingsWindow> settingsWindowFactory)
    {
        _editorWindowFactory = editorWindowFactory ?? throw new ArgumentNullException(nameof(editorWindowFactory));
        _generateChildWindowFactory = generateChildWindowFactory ?? throw new ArgumentNullException(nameof(generateChildWindowFactory));
        _settingsWindowFactory = settingsWindowFactory ?? throw new ArgumentNullException(nameof(settingsWindowFactory));
    }

    // ─── PUBLIC METHODS ────────────────────────
    public void OpenEditor(LibraryItem item)
    {
        var window = _editorWindowFactory.Create();
        window.Load(item);
        window.Show();
    }

    public async Task<string?> OpenGenerateChildAsync(TemplateDocument tmplate, string childName)
    {
        if (UIWindows.Active is not { } owner)
            return null;
        
        var window = _generateChildWindowFactory.Create();
        window.Load(tmplate, childName);
        return await window.ShowDialog<string?>(owner);
    }

    public async Task<bool> OpenSettingsAsync()
        => UIWindows.Active is { } owner && await _settingsWindowFactory.Create().ShowDialog<bool>(owner);
}