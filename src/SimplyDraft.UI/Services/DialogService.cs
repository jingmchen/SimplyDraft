// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using SimplyDraft.Core.Abstractions.UI;
using SimplyDraft.Core.Domains.Library;
using SimplyDraft.Core.Domains.UI;
using SimplyDraft.UI.Common;
using SimplyDraft.UI.Views.Dialogs;

namespace SimplyDraft.UI.Services;

public sealed class DialogService : IDialogService
{
    public async Task<string?> PromptAsync(string title, string prompt, string initial = "")
        => UIWindows.Active is { } owner ? await InputDialog.ShowAsync(owner, title, prompt, initial) : null;

    public async Task<int> ChooseAsync(string title, string message, params string[] buttons)
        => UIWindows.Active is { } owner ? await ChoiceDialog.ShowAsync(owner, title, message, buttons) : 0;
    
    public async Task<LibraryItem?> PickTemplateAsync(IEnumerable<LibraryItem> templates)
        => UIWindows.Active is { } owner ? await TemplatePickerDialog.ShowAsync(owner, templates) : null;

    public async Task<NewTemplateSelection?> OpenNewTemplateAsync(IReadOnlyList<string> seedTemplateNames)
    {
        ArgumentNullException.ThrowIfNull(seedTemplateNames);
        return UIWindows.Active is { } owner
            ? await NewTemplateDialog.ShowAsync(owner, seedTemplateNames)
            : null;
    }

    public async Task<IReadOnlyList<VariableDeclaration>?> EditVariablesAsync(IReadOnlyList<VariableDeclaration> current)
    {
        ArgumentNullException.ThrowIfNull(current);
        if (UIWindows.Active is not { } owner)
            return null;
        return await VariableManagerDialog.ShowAsync(owner, current);
    }
}