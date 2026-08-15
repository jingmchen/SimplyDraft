// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using System.Text;

namespace SimplyDraft.Core.Abstractions.Infrastructure;

public interface IAtomicFileWriter
{
    Task WriteToAsync(string path, string contents, Encoding? encoding = null);
}