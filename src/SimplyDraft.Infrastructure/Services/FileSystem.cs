// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using SimplyDraft.Core.Abstractions.Infrastructure;

namespace SimplyDraft.Infrastructure.Services;

public sealed class FileSystem : IFileSystem
{
    public Task<string> ReadAllTextAsync(string path, CancellationToken ct)
        => File.ReadAllTextAsync(path, ct);

    public Task WriteAllTextAsync(string path, string text, CancellationToken ct)
        => File.WriteAllTextAsync(path, text, ct);

    public bool FileExists(string path)
        => File.Exists(path);

    public void CreateDirectory(string path)
        => Directory.CreateDirectory(path);
}