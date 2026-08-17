// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using SimplyDraft.Core.Domains.Documents;
using SimplyDraft.Core.Domains.Library;

namespace SimplyDraft.Core.Abstractions.UI;

public interface IWindowService
{
    void OpenEditor(LibraryItem item);
    Task<string?> OpenGenerateChildAsync(TemplateDocument tmplate, string childName);
    Task<bool> OpenSettingsAsync();
}