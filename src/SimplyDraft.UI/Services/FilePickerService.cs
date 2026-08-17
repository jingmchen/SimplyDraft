// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using Avalonia.Platform.Storage;
using SimplyDraft.Core.Abstractions.UI;
using SimplyDraft.Core.Domains.FilePicker;
using SimplyDraft.UI.Common;

namespace SimplyDraft.UI.Services;

public sealed class FilePickerService : IFilePickerService
{
    public async Task<string?> PickFileAsync(string title, IReadOnlyList<FileFilter> filters)
    {
        if (UIWindows.Active is not { } owner)
            throw new InvalidOperationException("File Picker invoked with no active application window.");
        
        var files = await owner.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = title,
            AllowMultiple = false,
            FileTypeFilter = filters
                .Select(f => new FilePickerFileType(f.Name){Patterns = f.Patterns})
                .ToArray()
        });

        return files is [var first, ..]
            ? first.TryGetLocalPath()
            : null;
    }

    public async Task<string?> PickFolderAsync(string title)
    {
        if (UIWindows.Active is not { } owner)
            throw new InvalidOperationException("File Picker invoked with no active application window.");
        
        var folders = await owner.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = title,
            AllowMultiple = false
        });

        return folders is [var first, ..]
            ? first.TryGetLocalPath()
            : null;
    }
}