// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using SimplyDraft.Core.Domains.Library;

namespace SimplyDraft.Core.Abstractions.UI;

public interface ILibraryActions
{
    event Action? Changed;
    event Action<string>? StatusReported;
    event Action<string>? SectionRequested;

    Task NewTemplateAsync();
    Task NewChildAsync(LibraryItem? target);

    void Open(LibraryItem? item);
    void Duplicate(LibraryItem? item);
    Task RenameAsync(LibraryItem? item);
    Task DeleteAsync(LibraryItem? item);

    void MoveToTrash(LibraryItem item);
    Task ExportAsync(LibraryItem? item);
    Task BatchAsync(LibraryItem? item);
    void Reveal(LibraryItem? item);
}