// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using System.Text;

namespace SimplyDraft.Core.Abstractions.Infrastructure;

public interface IAtomicFileAsync
{
    Task WriteAsync(string path, string contents, Encoding? encoding = null);
    Task MoveAsync(string sourcePath, string destinationPath, bool overwrite = false);
    Task DeleteAsync(string path);
    Task FlushAsync();
}