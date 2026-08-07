// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using SimplyDraft.Core.Domains.Library;
using SimplyDraft.Core.Domains.UI;

namespace SimplyDraft.Core.Abstractions.UI;

public interface IDialogService
{
    Task<string?> PromptAsync(string title, string prompt, string initial = "");
    Task<int> ChooseAsync(string title, string message, params string[] buttons);
    Task<LibraryItem?> PickTemplateAsync(IEnumerable<LibraryItem> templates);
    Task<NewTemplateSelection?> OpenNewTemplateAsync(IReadOnlyList<string> seedTemplateNames);
    Task<IReadOnlyList<VariableDeclaration>?> EditVariablesAsync(IReadOnlyList<VariableDeclaration> current);
}