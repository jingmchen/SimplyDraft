// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using Avalonia.Platform.Storage;
using SimplyDraft.Core.Abstractions.UI;
using SimplyDraft.Core.Domains.UI;
using SimplyDraft.UI.Common;

namespace SimplyDraft.UI.Services;

public sealed class FilePickerService : IFilePickerService
{
    public async Task<string?> PickFileAsync(string title, IReadOnlyList<FileFilter> filters)
    {
        if (UIWindows.Active is not { } owner)
            return null;
        
        var files = await owner.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = title,
            AllowMultiple = false,
            FileTypeFilter = filters
                .Select(f => new FilePickerFileType(f.Name){Patterns = f.Patterns.ToArray()})
                .ToArray()
        });

        return files is [var first, ..]
            ? first.TryGetLocalPath()
            : null;
    }

    public async Task<string?> PickFolderAsync(string title)
    {
        if (UIWindows.Active is not { } owner)
            return null;
        
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