// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using SimplyDraft.Core.Domains.UI;

namespace SimplyDraft.Core.Abstractions.UI;

public interface IFilePickerService
{
    Task<string?> PickFileAsync(string title, IReadOnlyList<FileFilter> filters);
    Task<string?> PickFolderAsync(string title);
}